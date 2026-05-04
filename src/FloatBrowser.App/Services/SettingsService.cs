using System.IO;
using FloatBrowser.App.Application;
using FloatBrowser.App.Config;
using FloatBrowser.App.Domain;
using FloatBrowser.App.Infrastructure;

namespace FloatBrowser.App.Services;

public class SettingsService : ISettingsService
{
    private readonly IJsonFileStorage _storage;
    private readonly ILogger _logger;
    private readonly string _configPath;
    private readonly string _appFolder;

    public SettingsService(IJsonFileStorage storage, ILogger logger)
    {
        _storage = storage;
        _logger = logger;
        _appFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FloatBrowser");
        _configPath = Path.Combine(_appFolder, "settings.json");
    }

    public async Task<AppConfiguration> LoadAsync()
    {
        try
        {
            var config = await _storage.ReadAsync<AppConfiguration>(_configPath);
            config ??= AppConfiguration.CreateDefault();
            MigrateLegacyHomeUrl(config);
            return config;
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Load settings failed; using defaults.", ex);
            return AppConfiguration.CreateDefault();
        }
    }

    public async Task SaveAsync(AppConfiguration config)
    {
        try
        {
            await _storage.WriteAsync(_configPath, config);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Save settings failed.", ex);
        }
    }

    public async Task<AppConfiguration> RestoreDefaultsAsync()
    {
        var defaults = AppConfiguration.CreateDefault();
        await SaveAsync(defaults);
        return defaults;
    }

    public string GetWebViewUserDataFolder()
    {
        var path = Path.Combine(_appFolder, "WebView2UserData");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void MigrateLegacyHomeUrl(AppConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.Browser.HomeUrl) ||
            string.Equals(config.Browser.HomeUrl, AppDefaults.LegacyDefaultHomeUrl, StringComparison.OrdinalIgnoreCase))
        {
            config.Browser.HomeUrl = AppDefaults.DefaultHomeUrl;
        }
    }
}
