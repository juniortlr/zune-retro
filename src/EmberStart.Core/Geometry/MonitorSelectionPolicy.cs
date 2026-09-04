namespace EmberStart.Core.Geometry;

public static class MonitorSelectionPolicy
{
    public static MonitorWorkArea Select(
        IReadOnlyList<MonitorWorkArea> monitors,
        PhysicalRect? anchor,
        PhysicalRect? foregroundWindow,
        PhysicalPoint? pointerLocation)
    {
        ArgumentNullException.ThrowIfNull(monitors);
        if (monitors.Count == 0)
        {
            throw new ArgumentException("At least one monitor is required.", nameof(monitors));
        }

        var selected = SelectByIntersection(monitors, anchor);
        selected ??= SelectByIntersection(monitors, foregroundWindow);
        selected ??= pointerLocation is { } point
            ? monitors.FirstOrDefault(monitor => monitor.Bounds.Contains(point))
            : null;

        return selected ?? monitors.FirstOrDefault(monitor => monitor.IsPrimary) ?? monitors[0];
    }

    private static MonitorWorkArea? SelectByIntersection(
        IReadOnlyList<MonitorWorkArea> monitors,
        PhysicalRect? candidate)
    {
        if (candidate is not { IsOrdered: true } rectangle)
        {
            return null;
        }

        return monitors
            .Select(monitor => new { Monitor = monitor, Area = monitor.Bounds.IntersectionArea(rectangle) })
            .Where(result => result.Area > 0)
            .OrderByDescending(result => result.Area)
            .ThenBy(result => result.Monitor.Id, StringComparer.Ordinal)
            .Select(result => result.Monitor)
            .FirstOrDefault();
    }
}
