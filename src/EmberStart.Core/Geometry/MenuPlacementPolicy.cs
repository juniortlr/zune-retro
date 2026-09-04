using EmberStart.Core.Activation;

namespace EmberStart.Core.Geometry;

public static class MenuPlacementPolicy
{
    public static PhysicalRect Place(
        PhysicalRect workArea,
        int desiredWidth,
        int desiredHeight,
        int margin,
        PhysicalRect? anchor,
        TaskbarEdge edge)
    {
        if (!workArea.IsOrdered)
        {
            throw new ArgumentException("Work area must be ordered.", nameof(workArea));
        }

        if (desiredWidth <= 0 || desiredHeight <= 0 || margin < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(desiredWidth), "Size must be positive and margin non-negative.");
        }

        var availableWidth = Math.Max(1L, workArea.Width - (2L * margin));
        var availableHeight = Math.Max(1L, workArea.Height - (2L * margin));
        var width = (int)Math.Min(desiredWidth, availableWidth);
        var height = (int)Math.Min(desiredHeight, availableHeight);

        var anchorRect = anchor is { IsOrdered: true } value ? value : workArea;
        long left;
        long top;

        switch (edge)
        {
            case TaskbarEdge.Top:
                left = anchorRect.Left;
                top = workArea.Top + margin;
                break;
            case TaskbarEdge.Right:
                left = workArea.Right - margin - width;
                top = anchorRect.Top;
                break;
            case TaskbarEdge.Left:
                left = workArea.Left + margin;
                top = anchorRect.Top;
                break;
            default:
                left = anchorRect.Left;
                top = workArea.Bottom - margin - height;
                break;
        }

        left = Math.Clamp(left, workArea.Left + (long)margin, workArea.Right - (long)margin - width);
        top = Math.Clamp(top, workArea.Top + (long)margin, workArea.Bottom - (long)margin - height);

        return new PhysicalRect((int)left, (int)top, (int)(left + width), (int)(top + height));
    }
}
