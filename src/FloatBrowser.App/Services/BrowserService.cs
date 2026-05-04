using System.Text.Json;
using System.Text;
using System.Net;
using System.Globalization;
using FloatBrowser.App.Application;
using FloatBrowser.App.Config;
using FloatBrowser.App.Domain;
using FloatBrowser.App.Infrastructure;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace FloatBrowser.App.Services;

public class BrowserService : IBrowserService
{
    private const string DisableBackgroundThrottlingArgs =
        "--disable-background-timer-throttling " +
        "--disable-renderer-backgrounding " +
        "--disable-backgrounding-occluded-windows";

    private readonly ILogger _logger;
    private readonly ISettingsService _settingsService;
    private readonly IBookmarkService _bookmarkService;
    private readonly AppConfiguration _config;
    private WebView2? _webView;
    private bool _isShowingBookmarksHome;

    public BrowserService(ILogger logger, ISettingsService settingsService, IBookmarkService bookmarkService, AppConfiguration config)
    {
        _logger = logger;
        _settingsService = settingsService;
        _bookmarkService = bookmarkService;
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
            var envOptions = new CoreWebView2EnvironmentOptions
            {
                AdditionalBrowserArguments = DisableBackgroundThrottlingArgs
            };
            var env = await CoreWebView2Environment.CreateAsync(
                userDataFolder: _settingsService.GetWebViewUserDataFolder(),
                options: envOptions);
            await _webView.EnsureCoreWebView2Async(env);
            await _logger.LogInfoAsync($"WebView2 initialized with browser args: {DisableBackgroundThrottlingArgs}");
            _webView.CoreWebView2.NewWindowRequested += (_, e) =>
            {
                e.Handled = true;
                _webView.CoreWebView2.Navigate(e.Uri);
            };
            _webView.CoreWebView2.NavigationStarting += (_, args) =>
            {
                if (!string.Equals(args.Uri, "about:blank", StringComparison.OrdinalIgnoreCase))
                {
                    _isShowingBookmarksHome = false;
                }

                IsLoading = true;
                BrowserStateChanged?.Invoke(this, EventArgs.Empty);
            };
            _webView.CoreWebView2.NavigationCompleted += (_, _) => { IsLoading = false; BrowserStateChanged?.Invoke(this, EventArgs.Empty); };
            _webView.CoreWebView2.HistoryChanged += (_, _) => BrowserStateChanged?.Invoke(this, EventArgs.Empty);
            _webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
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
            _isShowingBookmarksHome = false;
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

    public async Task GoHomeAsync()
    {
        var homeUrl = _config.Browser.HomeUrl;
        if (string.Equals(homeUrl, AppDefaults.DefaultHomeUrl, StringComparison.OrdinalIgnoreCase))
        {
            await NavigateBookmarksHomeAsync();
            return;
        }

        await NavigateAsync(homeUrl);
    }

    public Task<string> GetCurrentUrlAsync()
    {
        if (_isShowingBookmarksHome)
        {
            return Task.FromResult(AppDefaults.DefaultHomeUrl);
        }

        return Task.FromResult(_webView?.Source?.ToString() ?? string.Empty);
    }
    public Task<string> GetCurrentTitleAsync() => Task.FromResult(_webView?.CoreWebView2?.DocumentTitle ?? string.Empty);

    public async Task<bool> ToggleMediaPlayPauseAsync()
    {
        if (_webView?.CoreWebView2 is null)
        {
            return false;
        }

        const string script = """
            (() => {
              try {
                const mediaNodes = [...document.querySelectorAll('video, audio')];
                const preferred = mediaNodes.find(m => !m.paused && !m.ended)
                  || mediaNodes.find(m => m.readyState > 0)
                  || mediaNodes[0];

                if (preferred) {
                  if (preferred.paused) {
                    const playResult = preferred.play();
                    if (playResult && typeof playResult.catch === 'function') {
                      playResult.catch(() => {});
                    }
                  } else {
                    preferred.pause();
                  }
                  return true;
                }

                const activeButton = document.querySelector(
                  '[aria-label*="暂停"],[aria-label*="播放"],.bpx-player-ctrl-play,.bilibili-player-video-btn-start'
                );
                if (activeButton instanceof HTMLElement) {
                  activeButton.click();
                  return true;
                }

                document.dispatchEvent(new KeyboardEvent('keydown', { key: 'MediaPlayPause', code: 'MediaPlayPause', bubbles: true }));
                document.dispatchEvent(new KeyboardEvent('keyup', { key: 'MediaPlayPause', code: 'MediaPlayPause', bubbles: true }));
                return false;
              } catch {
                return false;
              }
            })();
            """;

        try
        {
            var resultJson = await _webView.CoreWebView2.ExecuteScriptAsync(script);
            var result = JsonSerializer.Deserialize<bool>(resultJson);
            await _logger.LogInfoAsync($"Browser ToggleMediaPlayPause requested: hasCore=True, currentUrl={_webView.Source}, result={result}");
            return result;
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

    private async Task NavigateBookmarksHomeAsync()
    {
        if (_webView?.CoreWebView2 is null)
        {
            return;
        }

        var bookmarks = await _bookmarkService.GetAllAsync();
        var html = BuildBookmarksHomeHtml(bookmarks);
        _isShowingBookmarksHome = true;
        _webView.NavigateToString(html);
        BrowserStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string BuildBookmarksHomeHtml(IReadOnlyList<BookmarkItem> bookmarks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html><head><meta charset=\"utf-8\"/><title>Bookmarks</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:'Segoe UI',sans-serif;background:#f7f8fa;color:#1f2937;margin:0;padding:20px;}");
        sb.AppendLine("h1{margin:0 0 12px;font-size:20px;}");
        sb.AppendLine(".toolbar{display:flex;gap:8px;align-items:center;margin-bottom:10px;}");
        sb.AppendLine(".toolbar button{border:1px solid #d1d5db;background:#fff;padding:6px 10px;border-radius:6px;cursor:pointer;font-size:13px;}");
        sb.AppendLine(".toolbar button.primary{background:#1f2937;border-color:#1f2937;color:#fff;}");
        sb.AppendLine(".toolbar button:disabled{opacity:0.5;cursor:not-allowed;}");
        sb.AppendLine(".meta{color:#6b7280;margin-bottom:18px;}");
        sb.AppendLine("ul{list-style:none;padding:0;margin:0;display:grid;gap:8px;}");
        sb.AppendLine("li{background:#fff;border:1px solid #e5e7eb;border-radius:8px;padding:10px 12px;display:grid;grid-template-columns:auto 1fr;gap:10px;align-items:start;}");
        sb.AppendLine(".select-col{display:none;margin-top:3px;}");
        sb.AppendLine("body.delete-mode .select-col{display:block;}");
        sb.AppendLine("a{color:#2563eb;text-decoration:none;font-weight:600;}");
        sb.AppendLine("a:hover{text-decoration:underline;}");
        sb.AppendLine("body.delete-mode a{color:#111827;pointer-events:none;text-decoration:none;}");
        sb.AppendLine(".url{font-size:12px;color:#6b7280;display:block;margin-top:4px;}");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine("<h1>Bookmarks</h1>");
        sb.AppendLine("<div class=\"toolbar\">");
        sb.AppendLine("<button id=\"toggle-delete\">Delete mode</button>");
        sb.AppendLine("<button id=\"select-all\" disabled>Select all</button>");
        sb.AppendLine("<button id=\"delete-selected\" class=\"primary\" disabled>Delete selected</button>");
        sb.AppendLine("</div>");
        sb.AppendLine($"<div class=\"meta\">Total: {bookmarks.Count}</div>");

        if (bookmarks.Count == 0)
        {
            sb.AppendLine("<p>No bookmarks yet. Use the system menu to add bookmarks.</p>");
        }
        else
        {
            sb.AppendLine("<ul>");
            foreach (var item in bookmarks.OrderByDescending(x => x.CreatedAt))
            {
                var title = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(item.Title) ? item.Url : item.Title);
                var url = WebUtility.HtmlEncode(item.Url);
                var createdAt = item.CreatedAt.ToString("O", CultureInfo.InvariantCulture);
                sb.AppendLine("<li>");
                sb.AppendLine($"<input class=\"select-col\" type=\"checkbox\" data-url=\"{url}\" data-created-at=\"{createdAt}\" />");
                sb.AppendLine($"<div><a href=\"{url}\">{title}</a><span class=\"url\">{url}</span></div>");
                sb.AppendLine("</li>");
            }

            sb.AppendLine("</ul>");
        }

        sb.AppendLine("<script>");
        sb.AppendLine("(() => {");
        sb.AppendLine("  const toggleBtn = document.getElementById('toggle-delete');");
        sb.AppendLine("  const selectAllBtn = document.getElementById('select-all');");
        sb.AppendLine("  const deleteBtn = document.getElementById('delete-selected');");
        sb.AppendLine("  const checkboxes = Array.from(document.querySelectorAll('input.select-col'));");
        sb.AppendLine("  let deleteMode = false;");
        sb.AppendLine("  const setDeleteMode = (enabled) => {");
        sb.AppendLine("    deleteMode = enabled;");
        sb.AppendLine("    document.body.classList.toggle('delete-mode', enabled);");
        sb.AppendLine("    toggleBtn.textContent = enabled ? 'Exit delete mode' : 'Delete mode';");
        sb.AppendLine("    selectAllBtn.disabled = !enabled || checkboxes.length === 0;");
        sb.AppendLine("    deleteBtn.disabled = !enabled || checkboxes.every(x => !x.checked);");
        sb.AppendLine("    if (!enabled) {");
        sb.AppendLine("      checkboxes.forEach(x => x.checked = false);");
        sb.AppendLine("    }");
        sb.AppendLine("  };");
        sb.AppendLine("  toggleBtn?.addEventListener('click', () => setDeleteMode(!deleteMode));");
        sb.AppendLine("  selectAllBtn?.addEventListener('click', () => {");
        sb.AppendLine("    checkboxes.forEach(x => x.checked = true);");
        sb.AppendLine("    deleteBtn.disabled = checkboxes.length === 0;");
        sb.AppendLine("  });");
        sb.AppendLine("  checkboxes.forEach(box => box.addEventListener('change', () => {");
        sb.AppendLine("    deleteBtn.disabled = !deleteMode || checkboxes.every(x => !x.checked);");
        sb.AppendLine("  }));");
        sb.AppendLine("  deleteBtn?.addEventListener('click', () => {");
        sb.AppendLine("    const items = checkboxes.filter(x => x.checked).map(x => ({");
        sb.AppendLine("      url: x.dataset.url || '',");
        sb.AppendLine("      createdAt: x.dataset.createdAt || ''");
        sb.AppendLine("    }));");
        sb.AppendLine("    if (items.length === 0 || !window.chrome?.webview) return;");
        sb.AppendLine("    deleteBtn.disabled = true;");
        sb.AppendLine("    window.chrome.webview.postMessage({ action: 'deleteBookmarks', items });");
        sb.AppendLine("  });");
        sb.AppendLine("})();");
        sb.AppendLine("</script>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        _ = HandleWebMessageAsync(e.WebMessageAsJson);
    }

    private async Task HandleWebMessageAsync(string webMessageJson)
    {
        try
        {
            using var document = JsonDocument.Parse(webMessageJson);
            var root = document.RootElement;
            if (!root.TryGetProperty("action", out var actionElement))
            {
                return;
            }

            var action = actionElement.GetString();
            if (!string.Equals(action, "deleteBookmarks", StringComparison.Ordinal))
            {
                return;
            }

            if (!root.TryGetProperty("items", out var itemsElement) || itemsElement.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var item in itemsElement.EnumerateArray())
            {
                var url = item.TryGetProperty("url", out var urlElement) ? urlElement.GetString() : null;
                var createdAtRaw = item.TryGetProperty("createdAt", out var createdAtElement) ? createdAtElement.GetString() : null;
                if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(createdAtRaw))
                {
                    continue;
                }

                if (!DateTime.TryParse(createdAtRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var createdAt))
                {
                    continue;
                }

                await _bookmarkService.DeleteAsync(new BookmarkItem { Url = url, CreatedAt = createdAt });
            }

            await NavigateBookmarksHomeAsync();
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Handle web message failed.", ex);
        }
    }
}
