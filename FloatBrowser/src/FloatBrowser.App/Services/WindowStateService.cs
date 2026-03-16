using FloatBrowser.App.Application;
using FloatBrowser.App.Config;
using FloatBrowser.App.Domain;

namespace FloatBrowser.App.Services;

public class WindowStateService : IWindowStateService
{
    public void ApplyToWindow(System.Windows.Window window, WindowConfig config)
    {
        window.Width = config.Width;
        window.Height = config.Height;
        window.Left = config.Left;
        window.Top = config.Top;
        window.Topmost = config.Topmost;
        window.Opacity = Math.Clamp(config.Opacity, AppDefaults.MinOpacity, AppDefaults.MaxOpacity);
        window.MinWidth = AppDefaults.MinWidth;
        window.MinHeight = AppDefaults.MinHeight;
    }

    public void CaptureFromWindow(System.Windows.Window window, WindowConfig config)
    {
        config.Width = window.Width;
        config.Height = window.Height;
        config.Left = window.Left;
        config.Top = window.Top;
        config.Topmost = window.Topmost;
        config.Opacity = window.Opacity;
    }
}
