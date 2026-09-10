namespace EmberStart.Core.Activation;

public static class MenuVisibilityPolicy
{
    public static bool GetExpectedVisibility(bool currentlyVisible, ActivationCommand command) => command switch
    {
        ActivationCommand.Toggle => !currentlyVisible,
        ActivationCommand.Show => true,
        ActivationCommand.Hide => false,
        _ => throw new ArgumentOutOfRangeException(nameof(command)),
    };
}
