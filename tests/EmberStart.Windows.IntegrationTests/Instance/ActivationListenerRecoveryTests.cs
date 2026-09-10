using System.Buffers.Binary;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using EmberStart.Core.Activation;
using EmberStart.Core.Instance;
using EmberStart.Windows.Instance;
using EmberStart.Windows.Security;

namespace EmberStart.Windows.IntegrationTests.Instance;

public sealed class ActivationListenerRecoveryTests
{
    [StandardUserFact]
    public async Task Listener_RecoversAfterMalformedStalledPartialAndDisconnectedClients()
    {
        EnsureSuitableHost();
        var identity = CreateIdentity();
        using var primary = SingleInstanceCoordinator.Create(identity);
        primary.StartListening(Accept);

        await SendAndObserveEofAsync(identity, Encoding.UTF8.GetBytes("{bad-json"));
        await StallAndObserveDeadlineEofAsync(identity);
        await SendPartialFrameAndDisconnectAsync(identity);
        await ConnectAndDisconnectAsync(identity);
        await AssertValidRequestSucceedsAsync(identity);
        Assert.Equal(ActivationListenerHealth.Listening, primary.ListenerHealth);
        Assert.True(primary.RejectedConnections >= 4);
    }

    [StandardUserFact]
    public async Task Listener_RecoversAfterHandlerThrows()
    {
        EnsureSuitableHost();
        var identity = CreateIdentity();
        var calls = 0;
        using var primary = SingleInstanceCoordinator.Create(identity);
        primary.StartListening((request, _) =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                throw new InvalidOperationException("deliberate handler failure");
            }

            return Task.FromResult(new ActivationResponse(request.RequestId, true, "Accepted"));
        });

        using (var first = SingleInstanceCoordinator.Create(identity))
        {
            var response = await first.SendAsync(
                ActivationRequest.CreateSimple(ActivationCommand.Show, ActivationSource.CommandLine));
            Assert.False(response.Accepted);
            Assert.Equal("HandlerFailed", response.Code);
        }

        await AssertValidRequestSucceedsAsync(identity);
        Assert.Equal(2, Volatile.Read(ref calls));
    }

    [StandardUserFact]
    public async Task Listener_BoundsUncooperativeHandler_ThenRecoversAfterItFinishes()
    {
        EnsureSuitableHost();
        var identity = CreateIdentity();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var exited = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        using var primary = SingleInstanceCoordinator.Create(identity);
        primary.StartListening(async (request, _) =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                await release.Task.ConfigureAwait(false);
                exited.SetResult();
            }

            return new ActivationResponse(request.RequestId, true, "Accepted");
        });

        try
        {
            // Give the test reader more time than the 500 ms handler budget. Production clients
            // can time out at this boundary; that outcome must never trigger an automatic retry.
            await using var client = CreateClient(identity);
            await ConnectAsync(client);
            var request = ActivationRequest.CreateSimple(ActivationCommand.Show, ActivationSource.CommandLine);
            await ActivationPipeProtocol.WriteRequestAsync(client, request, CancellationToken.None);
            var length = new byte[sizeof(int)];
            await client.ReadExactlyAsync(length).AsTask().WaitAsync(TimeSpan.FromSeconds(2));
            var payloadLength = BinaryPrimitives.ReadInt32LittleEndian(length);
            Assert.InRange(payloadLength, 1, ActivationPipeProtocol.MaximumMessageBytes);
            var frame = new byte[sizeof(int) + payloadLength];
            length.CopyTo(frame, 0);
            await client.ReadExactlyAsync(frame.AsMemory(sizeof(int))).AsTask().WaitAsync(TimeSpan.FromSeconds(2));
            await using var stream = new MemoryStream(frame);
            var response = await ActivationPipeProtocol.ReadResponseAsync(stream, CancellationToken.None);
            Assert.False(response.Accepted);
            Assert.Equal("HandlerTimedOut", response.Code);
            Assert.Equal(request.RequestId, response.RequestId);

            using var secondary = SingleInstanceCoordinator.Create(identity);
            for (var index = 0; index < 10; index++)
            {
                var busy = await secondary.SendAsync(
                    ActivationRequest.CreateSimple(ActivationCommand.Hide, ActivationSource.CommandLine));
                Assert.False(busy.Accepted);
                Assert.Equal("Busy", busy.Code);
            }

            Assert.Equal(1, Volatile.Read(ref calls));
        }
        finally
        {
            release.TrySetResult();
            await exited.Task.WaitAsync(TimeSpan.FromSeconds(2));
        }

        // The handler's Task may settle just after the signal; Busy remains a bounded outcome.
        using var recovery = SingleInstanceCoordinator.Create(identity);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        ActivationResponse recovered;
        do
        {
            recovered = await recovery.SendAsync(
                ActivationRequest.CreateSimple(ActivationCommand.Hide, ActivationSource.CommandLine), deadline.Token);
        }
        while (recovered.Code == "Busy");

        Assert.True(recovered.Accepted);
        Assert.Equal(2, Volatile.Read(ref calls));
        Assert.Equal(ActivationListenerHealth.Listening, primary.ListenerHealth);
    }

    [StandardUserFact]
    public void Listener_ReportsPipeCreationFailure_WithoutRetryLoop()
    {
        var identity = CreateIdentity();
        using var existing = new NamedPipeServerStream(identity.Names.PipeName, PipeDirection.InOut,
            1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        using var primary = SingleInstanceCoordinator.Create(identity);
        primary.StartListening(Accept);
        Assert.Equal(ActivationListenerHealth.Faulted, primary.ListenerHealth);
        Assert.True(primary.ListenerCompletion.IsCompletedSuccessfully);
        primary.StartListening(Accept);
        Assert.Equal(ActivationListenerHealth.Faulted, primary.ListenerHealth);
    }

    [StandardUserFact]
    public async Task Dispose_StopsWaitingListener_AndIsIdempotent()
    {
        using var primary = SingleInstanceCoordinator.Create(CreateIdentity());
        primary.StartListening(Accept);
        Assert.Equal(ActivationListenerHealth.Listening, primary.ListenerHealth);
        primary.Dispose();
        primary.Dispose();
        await primary.ListenerCompletion.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(ActivationListenerHealth.Stopped, primary.ListenerHealth);
    }

    private static CurrentSessionIdentity CreateIdentity()
    {
        var current = CurrentSessionIdentity.Create();
        var suffix = Guid.NewGuid().ToString("N");
        return current with
        {
            Names = new InstanceIdentity(
            $"Local\\EmberStart.Tests.{suffix}", $"EmberStart.Tests.{suffix}")
        };
    }

    private static void EnsureSuitableHost() => Assert.True(
        ProcessIntegrityGuard.EvaluateCurrentProcess().MayBecomeResident,
        "EDD-03 recovery tests require a current-user medium-integrity Windows host.");

    private static Task<ActivationResponse> Accept(ActivationRequest request, CancellationToken _) =>
        Task.FromResult(new ActivationResponse(request.RequestId, true, "Accepted"));

    private static async Task AssertValidRequestSucceedsAsync(CurrentSessionIdentity identity)
    {
        using var secondary = SingleInstanceCoordinator.Create(identity);
        Assert.False(secondary.IsPrimary);
        var request = ActivationRequest.CreateSimple(ActivationCommand.Hide, ActivationSource.CommandLine);
        var response = await secondary.SendAsync(request).WaitAsync(TimeSpan.FromSeconds(2));
        Assert.True(response.Accepted);
        Assert.Equal(request.RequestId, response.RequestId);
    }

    private static NamedPipeClientStream CreateClient(CurrentSessionIdentity identity) => new(
        ".", identity.Names.PipeName, PipeDirection.InOut,
        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly,
        TokenImpersonationLevel.Impersonation);

    private static Task ConnectAsync(NamedPipeClientStream client) =>
        client.ConnectAsync().WaitAsync(TimeSpan.FromSeconds(2));

    private static async Task SendAndObserveEofAsync(CurrentSessionIdentity identity, byte[] payload)
    {
        await using var client = CreateClient(identity);
        await ConnectAsync(client);
        var header = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);
        await client.WriteAsync(header);
        await client.WriteAsync(payload);
        await client.FlushAsync();
        Assert.Equal(0, await client.ReadAsync(new byte[1]).AsTask().WaitAsync(TimeSpan.FromSeconds(2)));
    }

    private static async Task StallAndObserveDeadlineEofAsync(CurrentSessionIdentity identity)
    {
        await using var client = CreateClient(identity);
        await ConnectAsync(client);
        Assert.Equal(0, await client.ReadAsync(new byte[1]).AsTask().WaitAsync(TimeSpan.FromSeconds(2)));
    }

    private static async Task SendPartialFrameAndDisconnectAsync(CurrentSessionIdentity identity)
    {
        await using var client = CreateClient(identity);
        await ConnectAsync(client);
        var header = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(header, 100);
        await client.WriteAsync(header);
        await client.WriteAsync(new byte[] { (byte)'{' });
        await client.FlushAsync();
    }

    private static async Task ConnectAndDisconnectAsync(CurrentSessionIdentity identity)
    {
        await using var client = CreateClient(identity);
        await ConnectAsync(client);
    }
}
