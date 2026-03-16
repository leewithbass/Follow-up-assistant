using System.Windows.Threading;
using System.Windows.Interop;
using FloatBrowser.App.Application;
using FloatBrowser.App.Domain;
using FloatBrowser.App.Interop;

namespace FloatBrowser.App.Services;

public class GlobalInputHookService : IGlobalInputHookService
{
    private readonly Dictionary<int, AppAction> _hotkeys = new();
    private HwndSource? _source;
    private Dispatcher? _dispatcher;

    public event EventHandler<AppAction>? ActionTriggered;

    public Task RegisterAsync(IntPtr windowHandle, HotkeyConfig config)
    {
        _source = HwndSource.FromHwnd(windowHandle);
        _source?.AddHook(WndProc);
        _dispatcher = _source?.Dispatcher;

        RegisterHotKey(1, config.Back, AppAction.Back, windowHandle);
        RegisterHotKey(2, config.Forward, AppAction.Forward, windowHandle);
        RegisterHotKey(3, config.Refresh, AppAction.Refresh, windowHandle);
        RegisterHotKey(4, config.ToggleVisibility, AppAction.ToggleVisibility, windowHandle);
        RegisterHotKey(5, config.Home, AppAction.Home, windowHandle);
        RegisterHotKey(6, config.PlayPauseMedia, AppAction.PlayPauseMedia, windowHandle);
        return Task.CompletedTask;
    }

    public Task UnregisterAsync(IntPtr windowHandle)
    {
        foreach (var hotkeyId in _hotkeys.Keys)
        {
            NativeMethods.UnregisterHotKey(windowHandle, hotkeyId);
        }
        _hotkeys.Clear();

        if (_source is not null)
        {
            _source.RemoveHook(WndProc);
        }

        return Task.CompletedTask;
    }

    private void RegisterHotKey(int id, string gesture, AppAction action, IntPtr handle)
    {
        ParseGesture(gesture, out var mod, out var key);
        NativeMethods.RegisterHotKey(handle, id, (uint)mod, (uint)key);
        _hotkeys[id] = action;
    }

    private static void ParseGesture(string gesture, out NativeMethods.HotKeyModifiers modifiers, out uint virtualKey)
    {
        modifiers = NativeMethods.HotKeyModifiers.None;
        virtualKey = 0x20;
        foreach (var part in gesture.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl": modifiers |= NativeMethods.HotKeyModifiers.Control; break;
                case "alt": modifiers |= NativeMethods.HotKeyModifiers.Alt; break;
                case "shift": modifiers |= NativeMethods.HotKeyModifiers.Shift; break;
                case "win": modifiers |= NativeMethods.HotKeyModifiers.Win; break;
                case "left": virtualKey = 0x25; break;
                case "right": virtualKey = 0x27; break;
                case "space": virtualKey = 0x20; break;
                default:
                    if (part.Length == 1)
                    {
                        virtualKey = (uint)char.ToUpperInvariant(part[0]);
                    }
                    break;
            }
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            if (_hotkeys.TryGetValue(id, out var action))
            {
                RaiseActionTriggered(action);
                handled = true;
            }
        }

        return IntPtr.Zero;
    }

    private void RaiseActionTriggered(AppAction action)
    {
        if (_dispatcher is not null && !_dispatcher.CheckAccess())
        {
            _dispatcher.Invoke(() => ActionTriggered?.Invoke(this, action));
            return;
        }

        ActionTriggered?.Invoke(this, action);
    }

    public void Dispose()
    {
    }
}
