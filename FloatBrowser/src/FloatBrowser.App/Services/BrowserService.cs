using System.Text.Json;
using FloatBrowser.App.Application;
using FloatBrowser.App.Domain;
using FloatBrowser.App.Infrastructure;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace FloatBrowser.App.Services;

public class BrowserService : IBrowserService
{
    private readonly ILogger _logger;
    private readonly ISettingsService _settingsService;
    private readonly AppConfiguration _config;
    private WebView2? _webView;

    public BrowserService(ILogger logger, ISettingsService settingsService, AppConfiguration config)
    {
        _logger = logger;
        _settingsService = settingsService;
        _config = config;
    }

    public bool CanGoBack => _webView?.CoreWebView2?.CanGoBack ?? false;
    public bool CanGoForward => _webView?.CoreWebView2?.CanGoForward ?? false;
    public bool IsLoading { get; private set; }
    public event EventHandler? BrowserStateChanged;

    public async Task InitializeAsync(WebView2 webView)
    {
        _webView = webView;
        try
        {
            var env = await CoreWebView2Environment.CreateAsync(userDataFolder: _settingsService.GetWebViewUserDataFolder());
            await _webView.EnsureCoreWebView2Async(env);
            _webView.CoreWebView2.NewWindowRequested += (_, e) =>
            {
                e.Handled = true;
                _webView.CoreWebView2.Navigate(e.Uri);
            };
            _webView.CoreWebView2.NavigationStarting += (_, _) => { IsLoading = true; BrowserStateChanged?.Invoke(this, EventArgs.Empty); };
            _webView.CoreWebView2.NavigationCompleted += (_, _) => { IsLoading = false; BrowserStateChanged?.Invoke(this, EventArgs.Empty); };
            _webView.CoreWebView2.HistoryChanged += (_, _) => BrowserStateChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("WebView2 initialize failed.", ex);
        }
    }

    public async Task<bool> NavigateAsync(string url)
    {
        if (_webView?.CoreWebView2 is null)
        {
            return false;
        }

        try
        {
            _webView.CoreWebView2.Navigate(url);
            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync($"Navigate failed: {url}", ex);
            return false;
        }
    }

    public Task GoBackAsync()
    {
        _ = _logger.LogInfoAsync($"Browser GoBack requested: canGoBack={CanGoBack}, hasCore={_webView?.CoreWebView2 is not null}");
        if (_webView?.CoreWebView2 is null)
        {
            return Task.CompletedTask;
        }

        if (CanGoBack)
        {
            _webView.CoreWebView2.GoBack();
            return Task.CompletedTask;
        }

        return ExecuteScriptWithLoggingAsync("history.back();", "Browser GoBack script fallback");
    }

    public Task GoForwardAsync()
    {
        _ = _logger.LogInfoAsync($"Browser GoForward requested: canGoForward={CanGoForward}, hasCore={_webView?.CoreWebView2 is not null}");
        if (_webView?.CoreWebView2 is null)
        {
            return Task.CompletedTask;
        }

        if (CanGoForward)
        {
            _webView.CoreWebView2.GoForward();
            return Task.CompletedTask;
        }

        return ExecuteScriptWithLoggingAsync("history.forward();", "Browser GoForward script fallback");
    }

    public Task RefreshAsync()
    {
        _ = _logger.LogInfoAsync($"Browser Refresh requested: hasCore={_webView?.CoreWebView2 is not null}, currentUrl={_webView?.Source}");
        if (_webView?.CoreWebView2 is null)
        {
            return Task.CompletedTask;
        }

        _webView.CoreWebView2.Reload();
        return ExecuteScriptWithLoggingAsync("window.location.reload();", "Browser Refresh script fallback");
    }

    public Task StopAsync()
    {
        _ = _logger.LogInfoAsync($"Browser Stop requested: hasCore={_webView?.CoreWebView2 is not null}");
        _webView?.CoreWebView2?.Stop();
        return Task.CompletedTask;
    }
    public Task GoHomeAsync() => NavigateAsync(_config.Browser.HomeUrl);
    public Task<string> GetCurrentUrlAsync() => Task.FromResult(_webView?.Source?.ToString() ?? string.Empty);
    public Task<string> GetCurrentTitleAsync() => Task.FromResult(_webView?.CoreWebView2?.DocumentTitle ?? string.Empty);

    public async Task<bool> ToggleMediaPlayPauseAsync()
    {
        if (_webView?.CoreWebView2 is null)
        {
            return false;
        }

        const string script = "(() => { try { const media = [...document.querySelectorAll('video, audio')]"
            + ".find(m => m && m.offsetParent !== null && !m.disabled) || document.querySelector('video, audio');"
            + " if (!media) return false; if (media.paused) { media.play(); } else { media.pause(); } return true; }"
            + " catch { return false; } })();";

        try
        {
            var resultJson = await _webView.CoreWebView2.ExecuteScriptAsync(script);
            return JsonSerializer.Deserialize<bool>(resultJson);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Toggle media play/pause failed.", ex);
            return false;
        }
    }

    private async Task ExecuteScriptWithLoggingAsync(string script, string operation)
    {
        if (_webView?.CoreWebView2 is null)
        {
            return;
        }

        try
        {
            await _webView.CoreWebView2.ExecuteScriptAsync(script);
            await _logger.LogInfoAsync($"{operation} executed.");
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync($"{operation} failed.", ex);
        }
    }
}
