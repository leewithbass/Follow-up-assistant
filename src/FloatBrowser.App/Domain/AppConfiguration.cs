using FloatBrowser.App.Config;

namespace FloatBrowser.App.Domain;

public class AppConfiguration
{
    public WindowConfig Window { get; set; } = new();
    public BrowserConfig Browser { get; set; } = new();
    public HotkeyConfig Hotkeys { get; set; } = new();

    public static AppConfiguration CreateDefault() => new();
}

public class WindowConfig
{
    public double Width { get; set; } = AppDefaults.DefaultWidth;
    public double Height { get; set; } = AppDefaults.DefaultHeight;
    public double Left { get; set; } = 100;
    public double Top { get; set; } = 100;
    public bool Topmost { get; set; } = AppDefaults.DefaultTopmost;
    public bool Borderless { get; set; } = AppDefaults.DefaultBorderless;
    public double Opacity { get; set; } = AppDefaults.DefaultOpacity;
}

public class BrowserConfig
{
    public string HomeUrl { get; set; } = AppDefaults.DefaultHomeUrl;
}

public class HotkeyConfig
{
    public string Back { get; set; } = AppDefaults.DefaultBackHotkey;
    public string Forward { get; set; } = AppDefaults.DefaultForwardHotkey;
    public string Refresh { get; set; } = AppDefaults.DefaultRefreshHotkey;
    public string ToggleVisibility { get; set; } = AppDefaults.DefaultToggleHotkey;
    public string Home { get; set; } = AppDefaults.DefaultHomeHotkey;
    public string PlayPauseMedia { get; set; } = AppDefaults.DefaultPlayPauseHotkey;
    public string MouseXButton1 { get; set; } = AppDefaults.DefaultMouseXButton1;
    public string MouseXButton2 { get; set; } = AppDefaults.DefaultMouseXButton2;
}
