using EmberStart.Core.Geometry;

namespace EmberStart.Core.Tests.Geometry;

public sealed class MonitorSelectionPolicyTests
{
    private static readonly MonitorWorkArea Left = new(
        "left",
        new PhysicalRect(-1920, 0, 0, 1080),
        new PhysicalRect(-1920, 0, 0, 1040),
        false);

    private static readonly MonitorWorkArea Primary = new(
        "primary",
        new PhysicalRect(0, 0, 2560, 1440),
        new PhysicalRect(0, 0, 2560, 1400),
        true);

    [Fact]
    public void Select_PrefersAnchorOverForegroundAndPointer()
    {
        var selected = MonitorSelectionPolicy.Select(
            [Left, Primary],
            new PhysicalRect(-1900, 1000, -1800, 1040),
            new PhysicalRect(100, 100, 800, 700),
            new PhysicalPoint(500, 500));

        Assert.Equal("left", selected.Id);
    }

    [Fact]
    public void Select_UsesForegroundThenPointerThenPrimary()
    {
        var foreground = MonitorSelectionPolicy.Select(
            [Left, Primary], null, new PhysicalRect(-100, 20, 600, 700), null);
        var pointer = MonitorSelectionPolicy.Select(
            [Left, Primary], null, null, new PhysicalPoint(-300, 500));
        var fallback = MonitorSelectionPolicy.Select([Left, Primary], null, null, null);

        Assert.Equal("primary", foreground.Id);
        Assert.Equal("left", pointer.Id);
        Assert.Equal("primary", fallback.Id);
    }

    [Fact]
    public void Place_ClampsMenuInsideNegativeCoordinateWorkArea()
    {
        var placed = MenuPlacementPolicy.Place(
            Left.WorkArea,
            desiredWidth: 704,
            desiredHeight: 640,
            margin: 8,
            new PhysicalRect(-1930, 1020, -1850, 1080),
            EmberStart.Core.Activation.TaskbarEdge.Bottom);

        Assert.True(placed.Left >= Left.WorkArea.Left + 8);
        Assert.True(placed.Right <= Left.WorkArea.Right - 8);
        Assert.True(placed.Bottom <= Left.WorkArea.Bottom - 8);
    }
}
