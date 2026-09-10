using EmberStart.Windows.Security;
using Xunit.Abstractions;

namespace EmberStart.Windows.IntegrationTests.Instance;

public sealed class ActivationColdProcessTests(ITestOutputHelper output)
{
    [StandardUserFact]
    public async Task MutexBeforePipe_SendTimesOutWithoutTakingOver_ThenReadyOwnerAccepts()
    {
        RequireHost();
        var names = Guid.NewGuid();
        await using var primary = await ActivationFixtureProcess.StartAsync(names, output);
        await using var secondary = await ActivationFixtureProcess.StartAsync(names, output);
        Assert.NotEqual(primary.Id, secondary.Id);
        Assert.True((await primary.CommandAsync("create", "created")).IsPrimary);
        Assert.False((await secondary.CommandAsync("create", "created")).IsPrimary);

        var firstId = Guid.NewGuid();
        var unavailable = await secondary.CommandAsync($"send {firstId:N}", "result");
        Assert.Equal(firstId, unavailable.RequestId);
        Assert.False(unavailable.Accepted);
        Assert.True(unavailable.Code is "OperationCanceledException" or "TaskCanceledException" or "TimeoutException");
        Assert.InRange(unavailable.ElapsedMilliseconds!.Value, 400, 2000);
        Assert.False((await secondary.CommandAsync("probe-held", "probed")).IsPrimary);
        Assert.DoesNotContain(primary.Observed, item => item.Event == "applied");

        Assert.True((await primary.CommandAsync("listen", "listening")).Accepted);
        // New user operation, not an implicit replay of the timed-out operation.
        var secondId = Guid.NewGuid();
        var accepted = await secondary.CommandAsync($"send {secondId:N}", "result");
        Assert.True(accepted.Accepted);
        Assert.Equal(secondId, accepted.RequestId);
        Assert.Equal(secondId, (await primary.ReadAsync("handler-finished")).RequestId);
        Assert.Single(primary.Observed, item => item.Event == "applied");
    }

    [StandardUserFact]
    public async Task ExitedPrimary_SecondaryHandleMustCloseBeforeCanonicalCreationCanWin()
    {
        RequireHost();
        var names = Guid.NewGuid();
        await using var primary = await ActivationFixtureProcess.StartAsync(names, output);
        await using var secondary = await ActivationFixtureProcess.StartAsync(names, output);
        Assert.True((await primary.CommandAsync("create", "created")).IsPrimary);
        Assert.False((await secondary.CommandAsync("create", "created")).IsPrimary);
        await primary.StopAsync();

        // Process exit alone is insufficient: the secondary itself retains the named mutex.
        Assert.False((await secondary.CommandAsync("probe-held", "probed")).IsPrimary);
        await secondary.CommandAsync("release", "released");
        Assert.True((await secondary.CommandAsync("create", "created")).IsPrimary);
        Assert.True((await secondary.CommandAsync("listen", "listening")).Accepted);

        await using var client = await ActivationFixtureProcess.StartAsync(names, output);
        Assert.False((await client.CommandAsync("create", "created")).IsPrimary);
        var requestId = Guid.NewGuid();
        Assert.True((await client.CommandAsync($"send {requestId:N}", "result")).Accepted);
        Assert.Equal(requestId, (await secondary.ReadAsync("handler-finished")).RequestId);
    }

    [StandardUserFact]
    public async Task ConcurrentSecondaries_AfterReleasingHandles_OnlyOneCreatesPrimary()
    {
        RequireHost();
        var names = Guid.NewGuid();
        await using var original = await ActivationFixtureProcess.StartAsync(names, output);
        Assert.True((await original.CommandAsync("create", "created")).IsPrimary);
        var contenders = new List<ActivationFixtureProcess>();
        try
        {
            for (var index = 0; index < 5; index++)
            {
                var contender = await ActivationFixtureProcess.StartAsync(names, output);
                contenders.Add(contender);
                Assert.False((await contender.CommandAsync("create", "created")).IsPrimary);
            }
            Assert.Equal(5, contenders.Select(item => item.Id).Distinct().Count());
            await original.StopAsync();
            await Task.WhenAll(contenders.Select(item => item.CommandAsync("release", "released")));
            // All old handles are known closed before the test releases concurrent creation.
            var creation = await Task.WhenAll(contenders.Select(item => item.CommandAsync("create", "created")));
            var winnerEvent = Assert.Single(creation, item => item.IsPrimary == true);
            Assert.Equal(4, creation.Count(item => item.IsPrimary == false));
            var winner = Assert.Single(contenders, item => item.Id == winnerEvent.ProcessId);
            Assert.True((await winner.CommandAsync("listen", "listening")).Accepted);
            var clients = contenders.Where(item => item.Id != winner.Id).ToArray();
            var requestIds = clients.Select(_ => Guid.NewGuid()).ToArray();
            var responses = await Task.WhenAll(clients.Select((client, index) =>
                client.CommandAsync($"send {requestIds[index]:N}", "result")));
            for (var index = 0; index < responses.Length; index++)
            {
                Assert.True(responses[index].Accepted);
                Assert.Equal(requestIds[index], responses[index].RequestId);
            }
            await winner.StopAsync();
            var applied = winner.Observed.Where(item => item.Event == "applied").ToArray();
            Assert.Equal(4, applied.Length);
            Assert.All(requestIds, id => Assert.Single(applied, item => item.RequestId == id));
        }
        finally
        {
            foreach (var contender in contenders)
            {
                await contender.DisposeAsync();
            }
        }
    }

    [StandardUserFact]
    public async Task AppliedToggle_WithMissingTimelyAck_IsUncertainAndIsNotRepeated()
    {
        RequireHost();
        var names = Guid.NewGuid();
        await using var primary = await ActivationFixtureProcess.StartAsync(names, output);
        await using var secondary = await ActivationFixtureProcess.StartAsync(names, output);
        Assert.True((await primary.CommandAsync("create", "created")).IsPrimary);
        Assert.False((await secondary.CommandAsync("create", "created")).IsPrimary);
        Assert.True((await primary.CommandAsync("listen-delayed", "listening")).Accepted);
        var requestId = Guid.NewGuid();
        await secondary.WriteAsync($"send {requestId:N}");
        Assert.Equal(requestId, (await primary.ReadAsync("applied")).RequestId);
        var response = await secondary.ReadAsync("result");
        Assert.Equal(requestId, response.RequestId);
        Assert.False(response.Accepted);
        Assert.True(response.Code is "HandlerTimedOut" or "OperationCanceledException" or "TaskCanceledException" or "TimeoutException");
        await primary.WriteAsync("finish-handler");
        Assert.Equal(requestId, (await primary.ReadAsync("handler-finished")).RequestId);
        await secondary.StopAsync();
        await primary.StopAsync();
        Assert.Single(primary.Observed, item => item.Event == "applied" && item.RequestId == requestId);
        Assert.Single(secondary.Observed, item => item.Event == "sending" && item.RequestId == requestId);
    }

    private static void RequireHost() => Assert.True(ProcessIntegrityGuard.EvaluateCurrentProcess().MayBecomeResident,
        "Cold-process evidence requires a medium-integrity Windows host.");
}
