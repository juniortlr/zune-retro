using EmberStart.Core.Geometry;

namespace EmberStart.Windows.Display;

public sealed record WindowPlacementResult(
    PhysicalRect MonitorBounds,
    PhysicalRect WorkArea,
    PhysicalRect WindowBounds,
    uint Dpi,
    bool AcceptedAnchor);
