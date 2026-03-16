using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using FloatBrowser.App.Application;
using FloatBrowser.App.Domain;
using FloatBrowser.App.Interop;

namespace FloatBrowser.App.Services;

public class GlobalInputHookService : IGlobalInputHookService
{
    private readonly Dictionary<int, AppAction> _hotkeys = new();
    private readonly Dictionary<string, AppAction> _mouseActionMap = new();
    private HwndSource? _source;
    private IntPtr _mouseHook;
    private NativeMethods.LowLevelMouseProc? _mouseProc;

    public event EventHandler<AppAction>? ActionTriggered;

    public Task RegisterAsync(IntPtr windowHandle, HotkeyConfig config)
    {
        _source = HwndSource.FromHwnd(windowHandle);
        _source?.AddHook(WndProc);

        RegisterHotKey(1, config.Back, AppAction.Back, windowHandle);
        RegisterHotKey(2, config.Forward, AppAction.Forward, windowHandle);
        RegisterHotKey(3, config.Refresh, AppAction.Refresh, windowHandle);
        RegisterHotKey(4, config.ToggleVisibility, AppAction.ToggleVisibility, windowHandle);
        RegisterHotKey(5, config.Home, AppAction.Home, windowHandle);
        RegisterHotKey(6, config.PlayPauseMedia, AppAction.PlayPauseMedia, windowHandle);

        _mouseActionMap["XButton1"] = ParseMouseAction(config.MouseXButton1);
        _mouseActionMap["XButton2"] = ParseMouseAction(config.MouseXButton2);

        _mouseProc = MouseHookCallback;
        _mouseHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _mouseProc, NativeMethods.GetModuleHandle(Process.GetCurrentProcess().MainModule?.ModuleName), 0);
        return Task.CompletedTask;
    }

    private static AppAction ParseMouseAction(string value) => Enum.TryParse<AppAction>(value, out var action) ? action : AppAction.None;

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

        if (_mouseHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_mouseHook);
            _mouseHook = IntPtr.Zero;
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
                ActionTriggered?.Invoke(this, action);
                handled = true;
            }
        }

        return IntPtr.Zero;
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)NativeMethods.WM_XBUTTONDOWN)
        {
            var mouseData = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam).mouseData;
            var button = ((mouseData >> 16) & 0xffff) == 1 ? "XButton1" : "XButton2";
            if (_mouseActionMap.TryGetValue(button, out var action) && action != AppAction.None)
            {
                ActionTriggered?.Invoke(this, action);
            }
        }
        return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_mouseHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_mouseHook);
            _mouseHook = IntPtr.Zero;
        }
    }
}
