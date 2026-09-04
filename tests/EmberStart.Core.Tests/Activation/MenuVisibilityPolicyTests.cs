using EmberStart.Core.Activation;

namespace EmberStart.Core.Tests.Activation;

public sealed class MenuVisibilityPolicyTests
{
    [Theory]
    [InlineData(false, ActivationCommand.Toggle, true)]
    [InlineData(true, ActivationCommand.Toggle, false)]
    [InlineData(false, ActivationCommand.Show, true)]
    [InlineData(true, ActivationCommand.Show, true)]
    [InlineData(false, ActivationCommand.Hide, false)]
    [InlineData(true, ActivationCommand.Hide, false)]
    public void GetExpectedVisibility_IsDeterministic(
        bool current,
        ActivationCommand command,
        bool expected)
    {
        Assert.Equal(expected, MenuVisibilityPolicy.GetExpectedVisibility(current, command));
    }
}
