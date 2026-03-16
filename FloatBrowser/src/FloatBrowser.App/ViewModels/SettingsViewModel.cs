using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FloatBrowser.App.Application;
using FloatBrowser.App.Domain;

namespace FloatBrowser.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly AppConfiguration _config;

    [ObservableProperty] private string back = string.Empty;
    [ObservableProperty] private string forward = string.Empty;
    [ObservableProperty] private string refresh = string.Empty;
    [ObservableProperty] private string toggleVisibility = string.Empty;
    [ObservableProperty] private string home = string.Empty;
    [ObservableProperty] private string playPauseMedia = string.Empty;
    [ObservableProperty] private string homeUrl = string.Empty;
    [ObservableProperty] private string message = string.Empty;

    public SettingsViewModel(ISettingsService settingsService, AppConfiguration config)
    {
        _settingsService = settingsService;
        _config = config;
        LoadFromConfig();
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        var hotkeys = new[] { Back, Forward, Refresh, ToggleVisibility, Home, PlayPauseMedia };
        if (hotkeys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != hotkeys.Length)
        {
            Message = "快捷键重复，无法保存";
            return;
        }

        _config.Hotkeys.Back = Back;
        _config.Hotkeys.Forward = Forward;
        _config.Hotkeys.Refresh = Refresh;
        _config.Hotkeys.ToggleVisibility = ToggleVisibility;
        _config.Hotkeys.Home = Home;
        _config.Hotkeys.PlayPauseMedia = PlayPauseMedia;
        _config.Browser.HomeUrl = HomeUrl;

        await _settingsService.SaveAsync(_config);
        Message = "设置已保存";
    }

    [RelayCommand]
    public async Task RestoreDefaultsAsync()
    {
        var defaults = await _settingsService.RestoreDefaultsAsync();
        _config.Window = defaults.Window;
        _config.Browser = defaults.Browser;
        _config.Hotkeys = defaults.Hotkeys;
        LoadFromConfig();
        Message = "已恢复默认值";
    }

    private void LoadFromConfig()
    {
        Back = _config.Hotkeys.Back;
        Forward = _config.Hotkeys.Forward;
        Refresh = _config.Hotkeys.Refresh;
        ToggleVisibility = _config.Hotkeys.ToggleVisibility;
        Home = _config.Hotkeys.Home;
        PlayPauseMedia = _config.Hotkeys.PlayPauseMedia;
        HomeUrl = _config.Browser.HomeUrl;
        Message = string.Empty;
    }
}
