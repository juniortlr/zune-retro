namespace EmberStart.Core.Geometry;

public sealed record MonitorWorkArea(
    string Id,
    PhysicalRect Bounds,
    PhysicalRect WorkArea,
    bool IsPrimary);
