using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace EmberStart.Windows.Catalog;

public sealed class ShellIconHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    internal ShellIconHandle(nint icon)
        : base(ownsHandle: true)
    {
        SetHandle(icon);
    }

    protected override bool ReleaseHandle() => PInvoke.DestroyIcon((HICON)handle);
}
