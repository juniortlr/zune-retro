using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace EmberStart.Windows.Activation;

public sealed class GlobalHotKeyRegistration : IDisposable
{
    public const int Identifier = 0x454D;
    public const uint HotKeyMessage = 0x0312;

    private HWND _window;
    private bool _registered;

    public bool TryRegister(nint windowHandle)
    {
        if (_registered)
        {
            return true;
        }

        _window = new HWND(windowHandle);
        _registered = PInvoke.RegisterHotKey(
            _window,
            Identifier,
            HOT_KEY_MODIFIERS.MOD_CONTROL |
            HOT_KEY_MODIFIERS.MOD_ALT |
            HOT_KEY_MODIFIERS.MOD_NOREPEAT,
            0x20);

        return _registered;
    }

    public static bool IsHotKeyMessage(int message, nint parameter) =>
        message == HotKeyMessage && parameter == Identifier;

    public void Dispose()
    {
        if (!_registered)
        {
            return;
        }

        _ = PInvoke.UnregisterHotKey(_window, Identifier);
        _registered = false;
    }
}
