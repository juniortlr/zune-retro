using EmberStart.Core.Activation;
using EmberStart.Core.Instance;
using EmberStart.Windows.Instance;

namespace EmberStart.Windows.IntegrationTests.Instance;

public sealed class SingleInstanceCoordinatorTests
{
    [Fact]
    public async Task Secondary_SendsRequestToPrimaryAndReceivesMatchingResponse()
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
        var response = await secondary.SendAsync(request);

        Assert.True(response.Accepted);
        Assert.Equal(request.RequestId, response.RequestId);
    }
}
