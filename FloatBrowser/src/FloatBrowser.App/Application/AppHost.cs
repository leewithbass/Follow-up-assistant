using FloatBrowser.App.Domain;
using FloatBrowser.App.Infrastructure;
using FloatBrowser.App.Services;

namespace FloatBrowser.App.Application;

public sealed class AppHost : IDisposable
{
    private readonly ILogger _logger;
    public ISettingsService SettingsService { get; }
    public IBookmarkService BookmarkService { get; }
    public IGlobalInputHookService GlobalInputHookService { get; }
    public IWindowStateService WindowStateService { get; }
    public AppConfiguration Config { get; private set; } = AppConfiguration.CreateDefault();

    public AppHost()
    {
        var appFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FloatBrowser");
        _logger = new FileLogger(appFolder);
        var storage = new JsonFileStorage();
        SettingsService = new SettingsService(storage, _logger);
        BookmarkService = new BookmarkService(storage, _logger);
        GlobalInputHookService = new GlobalInputHookService();
        WindowStateService = new WindowStateService();
    }

    public async Task InitializeAsync()
    {
        Config = await SettingsService.LoadAsync();
    }

    public async Task<IBrowserService> CreateBrowserServiceAsync()
    {
        await Task.CompletedTask;
        return new BrowserService(_logger, SettingsService, Config);
    }

    public async Task ShutdownAsync()
    {
        await SettingsService.SaveAsync(Config);
    }

    public void Dispose() => GlobalInputHookService.Dispose();
}
