using System.IO.Pipes;
using System.ComponentModel;
using System.Security.Principal;
using EmberStart.Core.Activation;

namespace EmberStart.Windows.Instance;

public sealed class SingleInstanceCoordinator : IDisposable
{
    private readonly CurrentSessionIdentity _identity;
    private readonly Mutex _mutex;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly bool _createdMutex;
    private Task? _listener;
    private Task<ActivationResponse>? _handlerTask;
    private int _listenerHealth;
    private long _rejectedConnections;
    private bool _disposed;

    private SingleInstanceCoordinator(
        CurrentSessionIdentity identity,
        Mutex mutex,
        bool createdMutex)
    {
        _identity = identity;
        _mutex = mutex;
        _createdMutex = createdMutex;
    }

    public bool IsPrimary => _createdMutex;

    public ActivationListenerHealth ListenerHealth =>
        (ActivationListenerHealth)Volatile.Read(ref _listenerHealth);

    public Task ListenerCompletion => _listener ?? Task.CompletedTask;

    public long RejectedConnections => Interlocked.Read(ref _rejectedConnections);

    public static SingleInstanceCoordinator Create(CurrentSessionIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        var mutex = new Mutex(
            initiallyOwned: false,
            identity.Names.MutexName,
            out var createdNew);

        return new SingleInstanceCoordinator(identity, mutex, createdNew);
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
            var server = CreateServer();
            SetHealth(ActivationListenerHealth.Listening);
            _listener = ListenAsync(server, handler, _shutdown.Token);
        }
        catch (Exception exception) when (IsRecoverableFailure(exception))
        {
            // Object creation faults are terminal, not a tight retry loop against a squatted pipe.
            SetHealth(ActivationListenerHealth.Faulted);
            _listener = Task.CompletedTask;
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

        if (!PipePeerValidator.IsServerInSession(client, _identity.SessionId))
        {
            throw new UnauthorizedAccessException("The activation server is in another Windows session.");
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
        _shutdown.Cancel();

        try
        {
            _listener?.Wait(TimeSpan.FromSeconds(1));
        }
        catch (AggregateException exception) when (exception.InnerExceptions.All(
            inner => inner is OperationCanceledException or ObjectDisposedException))
        {
        }

        _mutex.Dispose();
        _shutdown.Dispose();
    }

    private async Task ListenAsync(
        NamedPipeServerStream server,
        Func<ActivationRequest, CancellationToken, Task<ActivationResponse>> handler,
        CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using (server)
                {
                    try
                    {
                        await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                        await HandleConnectionAsync(server, handler, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception exception) when (IsConnectionFailure(exception))
                    {
                        // A peer's decode/read/write/impersonation failure belongs to this connection only.
                        Interlocked.Increment(ref _rejectedConnections);
                    }
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    server = CreateServer();
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
            if (ListenerHealth != ActivationListenerHealth.Faulted)
            {
                SetHealth(ActivationListenerHealth.Stopped);
            }
        }
    }

    private async Task HandleConnectionAsync(
        NamedPipeServerStream server,
        Func<ActivationRequest, CancellationToken, Task<ActivationResponse>> handler,
        CancellationToken cancellationToken)
    {
        if (!PipePeerValidator.IsClientAllowed(server, _identity.UserSid, _identity.SessionId))
        {
            Interlocked.Increment(ref _rejectedConnections);
            return;
        }

        var request = await ActivationPipeProtocol.ReadRequestAsync(server, cancellationToken).ConfigureAwait(false);
        var response = await InvokeHandlerAsync(request, handler, cancellationToken).ConfigureAwait(false);
        await ActivationPipeProtocol.WriteResponseAsync(server, response, cancellationToken).ConfigureAwait(false);
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

    private NamedPipeServerStream CreateServer() => new(
        _identity.Names.PipeName,
        PipeDirection.InOut,
        maxNumberOfServerInstances: 1,
        PipeTransmissionMode.Byte,
        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly | PipeOptions.FirstPipeInstance,
        inBufferSize: ActivationPipeProtocol.MaximumMessageBytes,
        outBufferSize: ActivationPipeProtocol.MaximumMessageBytes);
}
