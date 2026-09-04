using System.IO.Pipes;
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

        _listener ??= ListenAsync(handler, _shutdown.Token);
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
        Func<ActivationRequest, CancellationToken, Task<ActivationResponse>> handler,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var server = CreateServer();
                await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

                if (!PipePeerValidator.IsClientAllowed(
                        server,
                        _identity.UserSid,
                        _identity.SessionId))
                {
                    continue;
                }

                var request = await ActivationPipeProtocol
                    .ReadRequestAsync(server, cancellationToken)
                    .ConfigureAwait(false);

                var response = request.ProtocolVersion == ActivationRequest.CurrentProtocolVersion
                    ? await handler(request, cancellationToken).ConfigureAwait(false)
                    : new ActivationResponse(request.RequestId, false, "UnsupportedProtocol");

                await ActivationPipeProtocol
                    .WriteResponseAsync(server, response, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (InvalidDataException)
            {
            }
        }
    }

    private NamedPipeServerStream CreateServer() => new(
        _identity.Names.PipeName,
        PipeDirection.InOut,
        maxNumberOfServerInstances: 1,
        PipeTransmissionMode.Byte,
        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly | PipeOptions.FirstPipeInstance,
        inBufferSize: ActivationPipeProtocol.MaximumMessageBytes,
        outBufferSize: ActivationPipeProtocol.MaximumMessageBytes);
}
