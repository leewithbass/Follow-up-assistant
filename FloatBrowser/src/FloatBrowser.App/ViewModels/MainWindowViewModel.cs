using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FloatBrowser.App.Application;
using FloatBrowser.App.Config;
using FloatBrowser.App.Domain;

namespace FloatBrowser.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IBrowserService _browserService;
    private readonly IBookmarkService _bookmarkService;
    private readonly AppConfiguration _config;

    [ObservableProperty] private string addressText = string.Empty;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private bool canGoBack;
    [ObservableProperty] private bool canGoForward;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isTopmost;
    [ObservableProperty] private bool isBorderless;
    [ObservableProperty] private double opacityValue;

    public ObservableCollection<BookmarkItem> Bookmarks { get; } = [];

    public MainWindowViewModel(IBrowserService browserService, IBookmarkService bookmarkService, AppConfiguration config)
    {
        _browserService = browserService;
        _bookmarkService = bookmarkService;
        _config = config;
        IsTopmost = config.Window.Topmost;
        IsBorderless = config.Window.Borderless;
        OpacityValue = config.Window.Opacity;
        _browserService.BrowserStateChanged += (_, _) => RefreshBrowserState();
    }

    public async Task InitializeAsync()
    {
        AddressText = _config.Browser.HomeUrl;
        await HomeAsync();
        await LoadBookmarksAsync();
    }

    [RelayCommand]
    public async Task OpenAsync()
    {
        if (string.Equals(AddressText?.Trim(), AppDefaults.DefaultHomeUrl, StringComparison.OrdinalIgnoreCase))
        {
            await _browserService.GoHomeAsync();
            AddressText = AppDefaults.DefaultHomeUrl;
            StatusMessage = string.Empty;
            return;
        }

        var url = NormalizeUrl(AddressText ?? string.Empty);
        if (url is null)
        {
            StatusMessage = "不是有效的网址";
            return;
        }

        if (!await _browserService.NavigateAsync(url))
        {
            StatusMessage = "页面加载失败";
            return;
        }

        AddressText = url;
        StatusMessage = "加载中...";
    }

    [RelayCommand] public Task BackAsync() => _browserService.GoBackAsync();
    [RelayCommand] public Task ForwardAsync() => _browserService.GoForwardAsync();
    [RelayCommand] public Task RefreshAsync() => _browserService.RefreshAsync();
    [RelayCommand] public Task StopAsync() => _browserService.StopAsync();
    [RelayCommand]
    public async Task HomeAsync()
    {
        await _browserService.GoHomeAsync();
        AddressText = _config.Browser.HomeUrl;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    public async Task AddBookmarkAsync()
    {
        var item = new BookmarkItem
        {
            Title = await _browserService.GetCurrentTitleAsync(),
            Url = await _browserService.GetCurrentUrlAsync(),
            CreatedAt = DateTime.UtcNow
        };
        await _bookmarkService.AddAsync(item);
        await LoadBookmarksAsync();
    }

    public async Task LoadBookmarksAsync()
    {
        Bookmarks.Clear();
        foreach (var item in await _bookmarkService.GetAllAsync())
        {
            Bookmarks.Add(item);
        }
    }

    public static string? NormalizeUrl(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var value = raw.Trim();
        if (Uri.TryCreate(value, UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps))
        {
            return u.ToString();
        }

        if (Regex.IsMatch(value, "^[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$"))
        {
            var https = $"https://{value}";
            if (Uri.TryCreate(https, UriKind.Absolute, out _)) return https;
        }

        return null;
    }

    private async void RefreshBrowserState()
    {
        CanGoBack = _browserService.CanGoBack;
        CanGoForward = _browserService.CanGoForward;
        IsLoading = _browserService.IsLoading;
        if (!IsLoading)
        {
            AddressText = await _browserService.GetCurrentUrlAsync();
            StatusMessage = string.Empty;
        }
    }
}
