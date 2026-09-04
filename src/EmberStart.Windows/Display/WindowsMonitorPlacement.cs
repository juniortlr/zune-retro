using System.ComponentModel;
using System.Runtime.InteropServices;
using EmberStart.Core.Activation;
using EmberStart.Core.Geometry;

namespace EmberStart.Windows.Display;

public static partial class WindowsMonitorPlacement
{
    private const uint MonitorDefaultToNull = 0;
    private const uint MonitorDefaultToPrimary = 1;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpNoOwnerZOrder = 0x0200;
    private const uint MonitorInfoPrimary = 0x0001;

    public static WindowPlacementResult Place(
        nint windowHandle,
        ActivationRequest request,
        int desiredWidthDips = 704,
        int desiredHeightDips = 640,
        int marginDips = 8)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (windowHandle == nint.Zero)
        {
            throw new ArgumentException("A valid window handle is required.", nameof(windowHandle));
        }

        var foreground = CaptureForegroundRect(windowHandle);
        var monitor = nint.Zero;

        if (request.Anchor is { IsOrdered: true } requestedAnchor)
        {
            var nativeAnchor = NativeRect.From(requestedAnchor);
            monitor = MonitorFromRect(in nativeAnchor, MonitorDefaultToNull);
        }

        if (monitor == nint.Zero && foreground is { } foregroundRect)
        {
            var nativeForeground = NativeRect.From(foregroundRect);
            monitor = MonitorFromRect(in nativeForeground, MonitorDefaultToNull);
        }

        if (monitor == nint.Zero && GetCursorPos(out var cursor))
        {
            monitor = MonitorFromPoint(cursor, MonitorDefaultToNull);
        }

        monitor = monitor != nint.Zero
            ? monitor
            : MonitorFromPoint(new NativePoint(0, 0), MonitorDefaultToPrimary);

        var info = GetInfo(monitor);
        var monitorBounds = info.Monitor.ToPhysicalRect();
        var workArea = info.WorkArea.ToPhysicalRect();
        var acceptedAnchor = request.Anchor is { IsOrdered: true } anchor &&
            anchor.IntersectionArea(monitorBounds) > 0 &&
            anchor.Width <= monitorBounds.Width &&
            anchor.Height <= monitorBounds.Height;

        MoveWindow(windowHandle, workArea.Left, workArea.Top, 1, 1);
        var dpi = GetDpiForWindow(windowHandle);
        if (dpi == 0)
        {
            dpi = 96;
        }

        var desiredWidth = ScaleDip(desiredWidthDips, dpi);
        var desiredHeight = ScaleDip(desiredHeightDips, dpi);
        var margin = ScaleDip(marginDips, dpi);
        var edge = request.Edge ?? TaskbarEdge.Bottom;
        var placement = MenuPlacementPolicy.Place(
            workArea,
            desiredWidth,
            desiredHeight,
            margin,
            acceptedAnchor ? request.Anchor : null,
            edge);

        MoveWindow(
            windowHandle,
            placement.Left,
            placement.Top,
            checked((int)placement.Width),
            checked((int)placement.Height));

        return new WindowPlacementResult(monitorBounds, workArea, placement, dpi, acceptedAnchor);
    }

    public static IReadOnlyList<MonitorWorkArea> GetSelectionSnapshot()
    {
        var primary = MonitorFromPoint(new NativePoint(0, 0), MonitorDefaultToPrimary);
        var info = GetInfo(primary);
        var bounds = info.Monitor.ToPhysicalRect();
        return
        [
            new MonitorWorkArea(
                primary.ToString("X", System.Globalization.CultureInfo.InvariantCulture),
                bounds,
                info.WorkArea.ToPhysicalRect(),
                (info.Flags & MonitorInfoPrimary) != 0),
        ];
    }

    private static PhysicalRect? CaptureForegroundRect(nint emberWindow)
    {
        var foreground = GetForegroundWindow();
        if (foreground == nint.Zero || foreground == emberWindow || !GetWindowRect(foreground, out var rectangle))
        {
            return null;
        }

        var result = rectangle.ToPhysicalRect();
        return result.IsOrdered ? result : null;
    }

    private static NativeMonitorInfo GetInfo(nint monitor)
    {
        var info = new NativeMonitorInfo
        {
            Size = (uint)Marshal.SizeOf<NativeMonitorInfo>(),
        };

        if (!GetMonitorInfo(monitor, ref info))
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError(), "Could not read monitor work area.");
        }

        return info;
    }

    private static int ScaleDip(int value, uint dpi) =>
        checked((int)Math.Ceiling(value * (dpi / 96d)));

    private static void MoveWindow(nint window, int left, int top, int width, int height)
    {
        if (!SetWindowPos(
                window,
                nint.Zero,
                left,
                top,
                width,
                height,
                SwpNoZOrder | SwpNoActivate | SwpNoOwnerZOrder))
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError(), "Could not position the Ember Start window.");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct NativePoint(int X, int Y);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public static NativeRect From(PhysicalRect value) => new()
        {
            Left = value.Left,
            Top = value.Top,
            Right = value.Right,
            Bottom = value.Bottom,
        };

        public readonly PhysicalRect ToPhysicalRect() => new(Left, Top, Right, Bottom);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMonitorInfo
    {
        public uint Size;
        public NativeRect Monitor;
        public NativeRect WorkArea;
        public uint Flags;
    }

    [LibraryImport("user32.dll")]
    private static partial nint GetForegroundWindow();

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetWindowRect(nint window, out NativeRect rectangle);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetCursorPos(out NativePoint point);

    [LibraryImport("user32.dll")]
    private static partial nint MonitorFromPoint(NativePoint point, uint flags);

    [LibraryImport("user32.dll")]
    private static partial nint MonitorFromRect(in NativeRect rectangle, uint flags);

    [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetMonitorInfo(nint monitor, ref NativeMonitorInfo info);

    [LibraryImport("user32.dll")]
    private static partial uint GetDpiForWindow(nint window);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowPos(
        nint window,
        nint insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);
}
