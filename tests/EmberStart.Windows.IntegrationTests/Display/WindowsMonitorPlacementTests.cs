using EmberStart.Windows.Display;

namespace EmberStart.Windows.IntegrationTests.Display;

public sealed class WindowsMonitorPlacementTests
{
    [Fact]
    public void GetSelectionSnapshot_ReturnsOrderedPrimaryWorkArea()
    {
        var monitors = WindowsMonitorPlacement.GetSelectionSnapshot();

        var monitor = Assert.Single(monitors);
        Assert.True(monitor.IsPrimary);
        Assert.True(monitor.Bounds.IsOrdered);
        Assert.True(monitor.WorkArea.IsOrdered);
        Assert.True(monitor.WorkArea.IntersectionArea(monitor.Bounds) > 0);
    }
}
