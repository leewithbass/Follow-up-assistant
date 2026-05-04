using FloatBrowser.App.Domain;
using Microsoft.Web.WebView2.Wpf;

namespace FloatBrowser.App.Application;

public enum AppAction
{
    None,
    Back,
    Forward,
    Refresh,
    ToggleVisibility,
    Home,
    PlayPauseMedia
}

public interface ISettingsService
{
    Task<AppConfiguration> LoadAsync();
    Task SaveAsync(AppConfiguration config);
    Task<AppConfiguration> RestoreDefaultsAsync();
    string GetWebViewUserDataFolder();
}

public interface IBookmarkService
{
    Task<IReadOnlyList<BookmarkItem>> GetAllAsync();
    Task AddAsync(BookmarkItem item);
    Task UpdateAsync(BookmarkItem original, BookmarkItem updated);
    Task DeleteAsync(BookmarkItem item);
}

public interface IBrowserService
{
    Task InitializeAsync(WebView2 webView);
    Task<bool> NavigateAsync(string url);
    Task GoBackAsync();
    Task GoForwardAsync();
    Task RefreshAsync();
    Task StopAsync();
    Task GoHomeAsync();
    Task<string> GetCurrentUrlAsync();
    Task<string> GetCurrentTitleAsync();
    Task<bool> ToggleMediaPlayPauseAsync();
    bool CanGoBack { get; }
    bool CanGoForward { get; }
    bool IsLoading { get; }
    event EventHandler? BrowserStateChanged;
}

public interface IGlobalInputHookService : IDisposable
{
    event EventHandler<AppAction>? ActionTriggered;
    Task RegisterAsync(IntPtr windowHandle, HotkeyConfig config);
    Task UnregisterAsync(IntPtr windowHandle);
}

public interface IWindowStateService
{
    void ApplyToWindow(System.Windows.Window window, WindowConfig config);
    void CaptureFromWindow(System.Windows.Window window, WindowConfig config);
}
