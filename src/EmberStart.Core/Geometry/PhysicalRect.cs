namespace EmberStart.Core.Geometry;

public readonly record struct PhysicalRect(int Left, int Top, int Right, int Bottom)
{
    public long Width => (long)Right - Left;

    public long Height => (long)Bottom - Top;

    public bool IsOrdered => Width > 0 && Height > 0;

    public bool Contains(PhysicalPoint point) =>
        point.X >= Left && point.X < Right && point.Y >= Top && point.Y < Bottom;

    public long IntersectionArea(PhysicalRect other)
    {
        var width = Math.Max(0L, Math.Min((long)Right, other.Right) - Math.Max((long)Left, other.Left));
        var height = Math.Max(0L, Math.Min((long)Bottom, other.Bottom) - Math.Max((long)Top, other.Top));
        return checked(width * height);
    }
}
