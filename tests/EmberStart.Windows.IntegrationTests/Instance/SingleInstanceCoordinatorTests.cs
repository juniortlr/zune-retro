using EmberStart.Core.Activation;
using EmberStart.Core.Instance;
using EmberStart.Windows.Instance;
using EmberStart.Windows.Security;

namespace EmberStart.Windows.IntegrationTests.Instance;

public sealed class SingleInstanceCoordinatorTests
{
    [Fact]
    public async Task Secondary_EnforcesCurrentProcessIntegrityPolicy()
    {
        var current = CurrentSessionIdentity.Create();
        var suffix = Guid.NewGuid().ToString("N");
        var identity = current with
        {
            Names = new InstanceIdentity(
                $"Local\\EmberStart.Tests.{suffix}",
                $"EmberStart.Tests.{suffix}"),
        };

        using var primary = SingleInstanceCoordinator.Create(identity);
        Assert.True(primary.IsPrimary);
        primary.StartListening((request, _) =>
            Task.FromResult(new ActivationResponse(request.RequestId, true, "Accepted")));

        using var secondary = SingleInstanceCoordinator.Create(identity);
        Assert.False(secondary.IsPrimary);

        var request = ActivationRequest.CreateSimple(
            ActivationCommand.Hide,
            ActivationSource.CommandLine);

        if (ProcessIntegrityGuard.EvaluateCurrentProcess().MayBecomeResident)
        {
            var response = await secondary.SendAsync(request);
            Assert.True(response.Accepted);
            Assert.Equal(request.RequestId, response.RequestId);
        }
        else
        {
            await Assert.ThrowsAnyAsync<IOException>(() => secondary.SendAsync(request));
        }
    }
}
