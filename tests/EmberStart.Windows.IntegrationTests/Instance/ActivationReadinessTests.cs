using System.IO.Pipes;
using EmberStart.Core.Activation;
using EmberStart.Core.Instance;
using EmberStart.Windows.Instance;
using EmberStart.Windows.Security;

namespace EmberStart.Windows.IntegrationTests.Instance;

public sealed class ActivationReadinessTests
{
    [StandardUserFact]
    public async Task Secondary_ReadinessCompletesFalseWithoutWaitingForDisposal()
    {
        var identity = CreateIdentity();
        using var primary = SingleInstanceCoordinator.Create(identity);
        using var secondary = SingleInstanceCoordinator.Create(identity);
        Assert.False(secondary.IsPrimary);
        Assert.True(secondary.Ready.IsCompletedSuccessfully);
        Assert.False(await secondary.Ready);
        Assert.Throws<InvalidOperationException>(() => secondary.StartListening(Accept));
    }

    [StandardUserFact]
    public async Task ConcurrentStarts_PublishOneListenerAndAcceptOneRequest()
    {
        var identity = CreateIdentity();
        using var primary = SingleInstanceCoordinator.Create(identity);
        var calls = 0;
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var starts = Enumerable.Range(0, 16).Select(_ => Task.Run(async () =>
        {
            await release.Task;
            primary.StartListening((request, _) =>
            {
                Interlocked.Increment(ref calls);
                return Accept(request, CancellationToken.None);
            });
            return primary.ListenerCompletion;
        })).ToArray();
        release.SetResult();
        var listeners = await Task.WhenAll(starts).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.All(listeners, listener => Assert.Same(primary.ListenerCompletion, listener));
        Assert.True(await primary.Ready);
        Assert.Equal(ActivationListenerHealth.Listening, primary.ListenerHealth);
        using var secondary = SingleInstanceCoordinator.Create(identity);
        Assert.True((await secondary.SendAsync(
            ActivationRequest.CreateSimple(ActivationCommand.Show, ActivationSource.CommandLine))).Accepted);
        Assert.Equal(1, Volatile.Read(ref calls));
    }

    [StandardUserFact]
    public async Task PipeCollision_CompletesInitialReadinessFalse()
    {
        var identity = CreateIdentity();
        using var owner = new NamedPipeServerStream(identity.Names.PipeName, PipeDirection.InOut,
            1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        using var primary = SingleInstanceCoordinator.Create(identity);
        primary.StartListening(Accept);
        Assert.False(await primary.Ready.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.Equal(ActivationListenerHealth.Faulted, primary.ListenerHealth);
        var completion = primary.ListenerCompletion;
        primary.StartListening(Accept);
        Assert.Same(completion, primary.ListenerCompletion);
    }

    [StandardUserFact]
    public async Task DisposeBeforeStart_CompletesReadinessFalseAndRejectsStart()
    {
        using var primary = SingleInstanceCoordinator.Create(CreateIdentity());
        Assert.False(primary.Ready.IsCompleted);
        primary.Dispose();
        Assert.False(await primary.Ready.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.Throws<ObjectDisposedException>(() => primary.StartListening(Accept));
    }

    [StandardUserFact]
    public async Task InitialSuccess_RemainsHistoricalAfterCurrentReadinessStops()
    {
        using var primary = SingleInstanceCoordinator.Create(CreateIdentity());
        Assert.False(primary.IsReady);
        Assert.False(primary.Ready.IsCompleted);
        primary.StartListening(Accept);
        Assert.True(await primary.Ready);
        Assert.True(primary.IsReady);
        primary.Dispose();
        await primary.ListenerCompletion.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.False(primary.IsReady);
        Assert.True(await primary.Ready);
        Assert.Equal(ActivationListenerHealth.Stopped, primary.ListenerHealth);
    }

    [StandardUserFact]
    public async Task ConcurrentDisposals_ReleaseOwnershipAndStopListener()
    {
        var identity = CreateIdentity();
        using var primary = SingleInstanceCoordinator.Create(identity);
        primary.StartListening(Accept);
        await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(primary.Dispose)))
            .WaitAsync(TimeSpan.FromSeconds(5));
        await primary.ListenerCompletion.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.False(primary.IsReady);
        using var replacement = SingleInstanceCoordinator.Create(identity);
        Assert.True(replacement.IsPrimary);
        replacement.StartListening(Accept);
        Assert.True(replacement.IsReady);
    }

    [StandardUserFact]
    public async Task StartRacingDisposal_CannotLeaveAnUnownedListener()
    {
        for (var iteration = 0; iteration < 10; iteration++)
        {
            var identity = CreateIdentity();
            using var primary = SingleInstanceCoordinator.Create(identity);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var start = Task.Run(async () =>
            {
                await release.Task;
                try
                {
                    primary.StartListening(Accept);
                }
                catch (ObjectDisposedException)
                {
                    // Disposal won the race: startup must not create any listener.
                }
            });
            var stop = Task.Run(async () =>
            {
                await release.Task;
                primary.Dispose();
            });
            release.SetResult();
            await Task.WhenAll(start, stop).WaitAsync(TimeSpan.FromSeconds(5));
            await primary.ListenerCompletion.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.True(primary.Ready.IsCompletedSuccessfully);
            Assert.False(primary.IsReady);
            Assert.Equal(ActivationListenerHealth.Stopped, primary.ListenerHealth);
            Assert.Throws<ObjectDisposedException>(() => primary.StartListening(Accept));
            using var replacement = SingleInstanceCoordinator.Create(identity);
            Assert.True(replacement.IsPrimary);
            replacement.StartListening(Accept);
            Assert.True(replacement.IsReady);
        }
    }

    private static Task<ActivationResponse> Accept(ActivationRequest request, CancellationToken _) =>
        Task.FromResult(new ActivationResponse(request.RequestId, true, "Accepted"));

    private static CurrentSessionIdentity CreateIdentity()
    {
        Assert.True(ProcessIntegrityGuard.EvaluateCurrentProcess().MayBecomeResident,
            "Readiness tests require a medium-integrity Windows host.");
        var current = CurrentSessionIdentity.Create();
        var suffix = Guid.NewGuid().ToString("N");
        return current with
        {
            Names = new InstanceIdentity($"Local\\EmberStart.Readiness.{suffix}", $"EmberStart.Readiness.{suffix}")
        };
    }
}
