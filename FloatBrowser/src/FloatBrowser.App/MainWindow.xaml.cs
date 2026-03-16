using System.IO;
using System.Windows;
using System.Windows.Interop;
using FloatBrowser.App.Application;
using FloatBrowser.App.Infrastructure;
using FloatBrowser.App.Interop;
using FloatBrowser.App.ViewModels;
using FloatBrowser.App.Views;

namespace FloatBrowser.App;

public partial class MainWindow : Window
{
    private const uint MenuOpenAddress = 0x1100;
    private const uint MenuHome = 0x1110;
    private const uint MenuBack = 0x1120;
    private const uint MenuForward = 0x1130;
    private const uint MenuRefresh = 0x1140;
    private const uint MenuStop = 0x1150;
    private const uint MenuAddBookmark = 0x1160;
    private const uint MenuBookmarks = 0x1170;
    private const uint MenuTopmost = 0x1180;
    private const uint MenuOpacity100 = 0x1190;
    private const uint MenuOpacity90 = 0x11A0;
    private const uint MenuOpacity80 = 0x11B0;
    private const uint MenuOpacity70 = 0x11C0;
    private const uint MenuSettings = 0x11D0;

    private readonly AppHost _host;
    private readonly MainWindowViewModel _viewModel;
    private readonly IBrowserService _browserService;
    private readonly ILogger _logger;
    private HwndSource? _hwndSource;
    private IntPtr _systemMenu;

    public MainWindow()
    {
        InitializeComponent();
        _host = ((App)System.Windows.Application.Current).Host ?? throw new InvalidOperationException("AppHost not initialized");
        var appFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FloatBrowser");
        _logger = new FileLogger(appFolder);
        _browserService = _host.CreateBrowserServiceAsync().GetAwaiter().GetResult();
        _viewModel = new MainWindowViewModel(_browserService, _host.BookmarkService, _host.Config);
        DataContext = _viewModel;
        SourceInitialized += MainWindow_SourceInitialized;
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        _hwndSource = (HwndSource)PresentationSource.FromVisual(this)!;
        _hwndSource.AddHook(WndProc);
        var handle = new WindowInteropHelper(this).Handle;
        _systemMenu = NativeMethods.GetSystemMenu(handle, false);
        BuildSystemMenu();
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _host.Config.Window.Borderless = false;
        _viewModel.IsBorderless = false;
        _host.WindowStateService.ApplyToWindow(this, _host.Config.Window);
        WindowStyle = WindowStyle.SingleBorderWindow;
        ResizeMode = ResizeMode.CanResize;
        Topmost = _viewModel.IsTopmost;
        Opacity = _viewModel.OpacityValue;

        await _browserService.InitializeAsync(BrowserView);
        await _viewModel.InitializeAsync();
        UpdateStatusBadge();

        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainWindowViewModel.IsTopmost))
            {
                Topmost = _viewModel.IsTopmost;
            }
            else if (args.PropertyName == nameof(MainWindowViewModel.OpacityValue))
            {
                Opacity = _viewModel.OpacityValue;
            }
            else if (args.PropertyName == nameof(MainWindowViewModel.StatusMessage))
            {
                UpdateStatusBadge();
            }
        };

        await ReloadInputHooksAsync();
        _host.GlobalInputHookService.ActionTriggered += GlobalInputHookService_ActionTriggered;
    }

    private async void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _host.Config.Window.Topmost = _viewModel.IsTopmost;
        _host.Config.Window.Borderless = false;
        _host.Config.Window.Opacity = _viewModel.OpacityValue;
        _host.WindowStateService.CaptureFromWindow(this, _host.Config.Window);
        var handle = new WindowInteropHelper(this).Handle;
        await _host.GlobalInputHookService.UnregisterAsync(handle);
        _hwndSource?.RemoveHook(WndProc);
    }

    private void BuildSystemMenu()
    {
        NativeMethods.AppendMenu(_systemMenu, NativeMethods.MF_SEPARATOR, 0, null);
        NativeMethods.AppendMenu(_systemMenu, NativeMethods.MF_STRING, MenuOpenAddress, "打开地址...");
        NativeMethods.AppendMenu(_systemMenu, NativeMethods.MF_STRING, MenuHome, "主页");
        NativeMethods.AppendMenu(_systemMenu, NativeMethods.MF_SEPARATOR, 0, null);
        NativeMethods.AppendMenu(_systemMenu, NativeMethods.MF_STRING, MenuBack, "后退");
        NativeMethods.AppendMenu(_systemMenu, NativeMethods.MF_STRING, MenuForward, "前进");
        NativeMethods.AppendMenu(_systemMenu, NativeMethods.MF_STRING, MenuRefresh, "刷新");
        NativeMethods.AppendMenu(_systemMenu, NativeMethods.MF_STRING, MenuStop, "停止");
        NativeMethods.AppendMenu(_systemMenu, NativeMethods.MF_SEPARATOR, 0, null);
        NativeMethods.AppendMenu(_systemMenu, NativeMethods.MF_STRING, MenuAddBookmark, "添加书签");
        NativeMethods.AppendMenu(_systemMenu, NativeMethods.MF_STRING, MenuBookmarks, "书签列表");
        NativeMethods.AppendMenu(_systemMenu, NativeMethods.MF_SEPARATOR, 0, null);
        NativeMethods.AppendMenu(_systemMenu, NativeMethods.MF_STRING, MenuTopmost, "置顶");
        NativeMethods.AppendMenu(_systemMenu, NativeMethods.MF_STRING, MenuOpacity100, "透明度 100%");
        NativeMethods.AppendMenu(_systemMenu, NativeMethods.MF_STRING, MenuOpacity90, "透明度 90%");
        NativeMethods.AppendMenu(_systemMenu, NativeMethods.MF_STRING, MenuOpacity80, "透明度 80%");
        NativeMethods.AppendMenu(_systemMenu, NativeMethods.MF_STRING, MenuOpacity70, "透明度 70%");
        NativeMethods.AppendMenu(_systemMenu, NativeMethods.MF_SEPARATOR, 0, null);
        NativeMethods.AppendMenu(_systemMenu, NativeMethods.MF_STRING, MenuSettings, "设置");
    }

    private void UpdateSystemMenuState()
    {
        SetMenuEnabled(MenuBack, _viewModel.CanGoBack);
        SetMenuEnabled(MenuForward, _viewModel.CanGoForward);
        SetMenuEnabled(MenuStop, _viewModel.IsLoading);

        SetMenuChecked(MenuTopmost, _viewModel.IsTopmost);
        SetMenuChecked(MenuOpacity100, _viewModel.OpacityValue >= 0.95);
        SetMenuChecked(MenuOpacity90, Math.Abs(_viewModel.OpacityValue - 0.9) < 0.01);
        SetMenuChecked(MenuOpacity80, Math.Abs(_viewModel.OpacityValue - 0.8) < 0.01);
        SetMenuChecked(MenuOpacity70, _viewModel.OpacityValue <= 0.71);
    }

    private void SetMenuEnabled(uint commandId, bool enabled)
    {
        NativeMethods.EnableMenuItem(_systemMenu, commandId, NativeMethods.MF_BYCOMMAND | (enabled ? NativeMethods.MF_ENABLED : NativeMethods.MF_GRAYED));
    }

    private void SetMenuChecked(uint commandId, bool isChecked)
    {
        NativeMethods.CheckMenuItem(_systemMenu, commandId, NativeMethods.MF_BYCOMMAND | (isChecked ? NativeMethods.MF_CHECKED : NativeMethods.MF_UNCHECKED));
    }

    private void UpdateStatusBadge()
    {
        StatusBadge.Visibility = string.IsNullOrWhiteSpace(_viewModel.StatusMessage) ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void GlobalInputHookService_ActionTriggered(object? sender, AppAction e)
    {
        try
        {
            await _logger.LogInfoAsync($"Handling mouse/hotkey action: {e}");

            switch (e)
            {
                case AppAction.Back:
                    await _viewModel.BackAsync();
                    break;
                case AppAction.Forward:
                    await _viewModel.ForwardAsync();
                    break;
                case AppAction.Refresh:
                    await _viewModel.RefreshAsync();
                    break;
                case AppAction.Home:
                    await _viewModel.HomeAsync();
                    break;
                case AppAction.PlayPauseMedia:
                    await _browserService.ToggleMediaPlayPauseAsync();
                    break;
                case AppAction.ToggleVisibility:
                    Visibility = Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
                    break;
            }
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync($"Handle action failed: {e}", ex);
        }
    }

    private async Task HandleSystemMenuCommandAsync(uint commandId)
    {
        switch (commandId)
        {
            case MenuOpenAddress:
                await OpenAddressAsync();
                break;
            case MenuHome:
                await _viewModel.HomeAsync();
                break;
            case MenuBack:
                await _viewModel.BackAsync();
                break;
            case MenuForward:
                await _viewModel.ForwardAsync();
                break;
            case MenuRefresh:
                await _viewModel.RefreshAsync();
                break;
            case MenuStop:
                await _viewModel.StopAsync();
                break;
            case MenuAddBookmark:
                await _viewModel.AddBookmarkAsync();
                break;
            case MenuBookmarks:
                await OpenBookmarksAsync();
                break;
            case MenuTopmost:
                _viewModel.IsTopmost = !_viewModel.IsTopmost;
                break;
            case MenuOpacity100:
                _viewModel.OpacityValue = 1.0;
                break;
            case MenuOpacity90:
                _viewModel.OpacityValue = 0.9;
                break;
            case MenuOpacity80:
                _viewModel.OpacityValue = 0.8;
                break;
            case MenuOpacity70:
                _viewModel.OpacityValue = 0.7;
                break;
            case MenuSettings:
                OpenSettings();
                break;
        }
    }

    private async Task OpenAddressAsync()
    {
        var currentUrl = await _browserService.GetCurrentUrlAsync();
        var dialog = new OpenUrlWindow(currentUrl) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            _viewModel.AddressText = dialog.Url;
            await _viewModel.OpenAsync();
        }
    }

    private async Task OpenBookmarksAsync()
    {
        var win = new BookmarksWindow(_host.BookmarkService) { Owner = this };
        if (win.ShowDialog() == true && !string.IsNullOrWhiteSpace(win.SelectedUrl))
        {
            _viewModel.AddressText = win.SelectedUrl;
            await _viewModel.OpenAsync();
        }
    }

    private void OpenSettings()
    {
        var win = new SettingsWindow(_host.SettingsService, _host.Config) { Owner = this };
        win.ShowDialog();
        _ = ReloadInputHooksAsync();
    }

    private async Task ReloadInputHooksAsync()
    {
        var handle = new WindowInteropHelper(this).Handle;
        await _host.GlobalInputHookService.UnregisterAsync(handle);
        await _host.GlobalInputHookService.RegisterAsync(handle, _host.Config.Hotkeys);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_INITMENUPOPUP)
        {
            UpdateSystemMenuState();
        }
        else if (msg == NativeMethods.WM_SYSCOMMAND)
        {
            var commandId = (uint)(wParam.ToInt64() & 0xFFF0);
            if (commandId >= MenuOpenAddress && commandId <= MenuSettings)
            {
                handled = true;
                _ = Dispatcher.InvokeAsync(() => HandleSystemMenuCommandAsync(commandId));
                return IntPtr.Zero;
            }
        }

        return IntPtr.Zero;
    }
}
