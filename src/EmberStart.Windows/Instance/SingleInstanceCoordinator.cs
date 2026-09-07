using System.IO.Pipes;
using System.ComponentModel;
using System.Security.Principal;
using System.Threading.Channels;
using EmberStart.Core.Activation;
using EmberStart.Windows.Security;

namespace EmberStart.Windows.Instance;

public sealed class SingleInstanceCoordinator : IDisposable
{
    private readonly CurrentSessionIdentity _identity;
    private readonly Mutex _mutex;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly bool _createdMutex;
    private const int MaximumConnections = 64;
    private readonly string _expectedServerImage;
    private readonly TaskCompletionSource<bool> _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly ActivationAdmissionLimiter _admission = new(TimeProvider.System);
    private readonly Channel<ActivationWork> _requests = Channel.CreateBounded<ActivationWork>(
        new BoundedChannelOptions(32) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });
    private Task? _listener;
    private Task<ActivationResponse>? _handlerTask;
    private int _listenerHealth;
    private long _rejectedConnections;
    private bool _disposed;

    private SingleInstanceCoordinator(
        CurrentSessionIdentity identity,
        Mutex mutex,
        bool createdMutex,
        string expectedServerImage)
    {
        _identity = identity;
        _mutex = mutex;
        _createdMutex = createdMutex;
        _expectedServerImage = expectedServerImage;
    }

    public bool IsPrimary => _createdMutex;

    public ActivationListenerHealth ListenerHealth =>
        (ActivationListenerHealth)Volatile.Read(ref _listenerHealth);

    public Task ListenerCompletion => _listener ?? Task.CompletedTask;

    public Task<bool> Ready => _ready.Task;

    public long RejectedConnections => Interlocked.Read(ref _rejectedConnections);

    public static SingleInstanceCoordinator Create(CurrentSessionIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        var actual = CurrentSessionIdentity.Create();
        if (identity.UserSid != actual.UserSid || identity.SessionId != actual.SessionId ||
            !ProcessIntegrityGuard.EvaluateCurrentProcess().MayBecomeResident)
        {
            throw new UnauthorizedAccessException("Activation requires the current medium-integrity user/session.");
        }

        var expectedImage = Environment.ProcessPath
            ?? throw new InvalidOperationException("The current executable identity is unavailable.");
        var mutex = NamedObjectSecurity.CreateMutex(identity, out var createdNew);

        return new SingleInstanceCoordinator(identity, mutex, createdNew, expectedImage);
    }

    public void StartListening(Func<ActivationRequest, CancellationToken, Task<ActivationResponse>> handler)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(handler);
        if (!IsPrimary)
        {
            throw new InvalidOperationException("Only the primary instance can listen for activation.");
        }

        if (_listener is not null)
        {
            return;
        }

        SetHealth(ActivationListenerHealth.Starting);
        try
        {
            // Publish Listening only after the first pipe has actually been created.
            var server = CreateServer(first: true);
            SetHealth(ActivationListenerHealth.Listening);
            var shutdownToken = _shutdown.Token;
            _listener = Task.Run(() => ListenAsync(server, handler, shutdownToken));
            _ready.TrySetResult(true);
        }
        catch (Exception exception) when (IsRecoverableFailure(exception))
        {
            // Object creation faults are terminal, not a tight retry loop against a squatted pipe.
            SetHealth(ActivationListenerHealth.Faulted);
            _listener = Task.CompletedTask;
            _ready.TrySetResult(false);
        }
    }

    public async Task<ActivationResponse> SendAsync(
        ActivationRequest request,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request);

        using var client = new NamedPipeClientStream(
            ".",
            _identity.Names.PipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly,
            TokenImpersonationLevel.Impersonation);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ActivationPipeProtocol.OperationTimeout);
        await client.ConnectAsync(timeout.Token).ConfigureAwait(false);

        if (!PipePeerValidator.IsServerAllowed(client, _identity, _expectedServerImage))
        {
            throw new UnauthorizedAccessException("The activation server identity is not trusted.");
        }

        await ActivationPipeProtocol.WriteRequestAsync(client, request, cancellationToken).ConfigureAwait(false);
        var response = await ActivationPipeProtocol.ReadResponseAsync(client, cancellationToken).ConfigureAwait(false);
        if (response.RequestId != request.RequestId)
        {
            throw new InvalidDataException("Activation response did not match the request.");
        }

        return response;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _ready.TrySetResult(false);
        _shutdown.Cancel();
        _requests.Writer.TryComplete();

        try
        {
            _listener?.Wait(TimeSpan.FromSeconds(1));
        }
        catch (AggregateException exception) when (exception.InnerExceptions.All(
            inner => inner is OperationCanceledException or ObjectDisposedException))
        {
        }

        if (_listener is { IsCompleted: false })
        {
            // Preserve the namespace and cancellation source until all pipe workers have stopped.
            _ = _listener.ContinueWith(_ => DisposeResources(), CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
        else
        {
            DisposeResources();
        }
    }

    private void DisposeResources()
    {
        _mutex.Dispose();
        _shutdown.Dispose();
    }

    private async Task ListenAsync(
        NamedPipeServerStream server,
        Func<ActivationRequest, CancellationToken, Task<ActivationResponse>> handler,
        CancellationToken cancellationToken)
    {
        var connections = new List<Task>();
        var consumer = ProcessRequestsAsync(handler, cancellationToken);
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (IOException) when (!cancellationToken.IsCancellationRequested)
                {
                    var failed = server;
                    server = CreateServer(first: false);
                    failed.Dispose();
                    Interlocked.Increment(ref _rejectedConnections);
                    continue;
                }
                connections.RemoveAll(task => task.IsCompleted);
                var connected = server;
                // Reserve the next instance before releasing the accepted handle. The pipe name
                // stays continuously owned, and FirstPipeInstance applies only to the first handle.
                server = CreateServer(first: false);
                if (connections.Count >= MaximumConnections)
                {
                    connected.Dispose();
                    Interlocked.Increment(ref _rejectedConnections);
                }
                else
                {
                    connections.Add(ServeConnectionAsync(connected, cancellationToken));
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (IsRecoverableFailure(exception))
        {
            SetHealth(ActivationListenerHealth.Faulted);
        }
        finally
        {
            server.Dispose();
            _shutdown.Cancel();
            _requests.Writer.TryComplete();
            await Task.WhenAll(connections).ConfigureAwait(false);
            await consumer.ConfigureAwait(false);
            if (ListenerHealth != ActivationListenerHealth.Faulted)
            {
                SetHealth(ActivationListenerHealth.Stopped);
            }
        }
    }

    private async Task ServeConnectionAsync(
        NamedPipeServerStream server,
        CancellationToken cancellationToken)
    {
        using (server)
        {
            try
            {
                // Impersonation validates the security context of the last pipe read. Bound the
                // frame before impersonating; unauthenticated peers never reach the work queue.
                var request = await ActivationPipeProtocol.ReadRequestAsync(server, cancellationToken).ConfigureAwait(false);
                if (!PipePeerValidator.TryValidateClient(server, _identity, out var peer))
                {
                    Interlocked.Increment(ref _rejectedConnections);
                    return;
                }

                var admitted = _admission.TryAdmit(peer!);
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                deadline.CancelAfter(ActivationPipeProtocol.OperationTimeout);
                var completion = new TaskCompletionSource<ActivationResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
                ActivationResponse response;
                if (!admitted)
                {
                    response = new ActivationResponse(request.RequestId, false, "RateLimited");
                }
                else if (!_requests.Writer.TryWrite(new ActivationWork(request, completion, deadline.Token)))
                {
                    response = new ActivationResponse(request.RequestId, false, "Busy");
                }
                else
                {
                    try
                    {
                        response = await completion.Task.WaitAsync(deadline.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        response = new ActivationResponse(request.RequestId, false, "HandlerTimedOut");
                    }
                }

                await ActivationPipeProtocol.WriteResponseAsync(server, response, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception) when (IsConnectionFailure(exception))
            {
                Interlocked.Increment(ref _rejectedConnections);
            }
            catch (Exception exception) when (IsRecoverableFailure(exception))
            {
                SetHealth(ActivationListenerHealth.Faulted);
                _shutdown.Cancel();
            }
        }
    }

    private async Task ProcessRequestsAsync(
        Func<ActivationRequest, CancellationToken, Task<ActivationResponse>> handler,
        CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var work in _requests.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                if (work.CancellationToken.IsCancellationRequested)
                {
                    work.Completion.TrySetCanceled(work.CancellationToken);
                    continue;
                }

                try
                {
                    work.Completion.TrySetResult(await InvokeHandlerAsync(work.Request, handler, work.CancellationToken)
                        .ConfigureAwait(false));
                }
                catch (OperationCanceledException) when (work.CancellationToken.IsCancellationRequested)
                {
                    work.Completion.TrySetCanceled(work.CancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (IsRecoverableFailure(exception))
        {
            SetHealth(ActivationListenerHealth.Faulted);
            _shutdown.Cancel();
        }
        finally
        {
            while (_requests.Reader.TryRead(out var pending))
            {
                pending.Completion.TrySetCanceled(cancellationToken);
            }
        }
    }

    private async Task<ActivationResponse> InvokeHandlerAsync(
        ActivationRequest request,
        Func<ActivationRequest, CancellationToken, Task<ActivationResponse>> handler,
        CancellationToken cancellationToken)
    {
        // Cancellation cannot stop arbitrary managed/native work. Keep at most one handler in flight,
        // even if it ignores its deadline, and never replay a possibly-applied activation automatically.
        if (_handlerTask is { IsCompleted: false })
        {
            return new ActivationResponse(request.RequestId, false, "Busy");
        }

        var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        operationCancellation.CancelAfter(ActivationPipeProtocol.OperationTimeout);
        _handlerTask = Task.Run(async () =>
        {
            using (operationCancellation)
            {
                try
                {
                    operationCancellation.Token.ThrowIfCancellationRequested();
                    var response = await handler(request, operationCancellation.Token).ConfigureAwait(false);
                    return response is not null && response.RequestId == request.RequestId &&
                        !string.IsNullOrWhiteSpace(response.Code) && response.Code.Length <= 64
                        ? response
                        : new ActivationResponse(request.RequestId, false, "HandlerFailed");
                }
                catch (OperationCanceledException) when (operationCancellation.IsCancellationRequested)
                {
                    return new ActivationResponse(request.RequestId, false, "HandlerTimedOut");
                }
                catch (Exception exception) when (IsRecoverableFailure(exception))
                {
                    return new ActivationResponse(request.RequestId, false, "HandlerFailed");
                }
            }
        }, CancellationToken.None);

        try
        {
            return await _handlerTask.WaitAsync(ActivationPipeProtocol.OperationTimeout, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            return new ActivationResponse(request.RequestId, false, "HandlerTimedOut");
        }
    }

    private void SetHealth(ActivationListenerHealth health) => Volatile.Write(ref _listenerHealth, (int)health);

    private static bool IsConnectionFailure(Exception exception) =>
        exception is IOException or InvalidDataException or OperationCanceledException or
            UnauthorizedAccessException or Win32Exception;

    private static bool IsRecoverableFailure(Exception exception) =>
        exception is not (OutOfMemoryException or AccessViolationException);

    private NamedPipeServerStream CreateServer(bool first) =>
        NamedObjectSecurity.CreatePipe(_identity, MaximumConnections + 2, first);

    private sealed record ActivationWork(ActivationRequest Request, TaskCompletionSource<ActivationResponse> Completion,
        CancellationToken CancellationToken);
}
