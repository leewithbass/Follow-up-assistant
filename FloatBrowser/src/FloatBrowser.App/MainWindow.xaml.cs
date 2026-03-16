using System.Windows;
using System.Windows.Input;
using FloatBrowser.App.Application;
using FloatBrowser.App.ViewModels;
using FloatBrowser.App.Views;

namespace FloatBrowser.App;

public partial class MainWindow : Window
{
    private readonly AppHost _host;
    private readonly MainWindowViewModel _viewModel;
    private readonly IBrowserService _browserService;

    public MainWindow()
    {
        InitializeComponent();
        _host = ((App)Application.Current).Host ?? throw new InvalidOperationException("AppHost not initialized");
        _browserService = _host.CreateBrowserServiceAsync().GetAwaiter().GetResult();
        _viewModel = new MainWindowViewModel(_browserService, _host.BookmarkService, _host.Config);
        DataContext = _viewModel;
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _host.WindowStateService.ApplyToWindow(this, _host.Config.Window);
        WindowStyle = _viewModel.IsBorderless ? WindowStyle.None : WindowStyle.SingleBorderWindow;
        await _browserService.InitializeAsync(BrowserView);
        await _viewModel.InitializeAsync();
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainWindowViewModel.IsTopmost)) Topmost = _viewModel.IsTopmost;
            if (args.PropertyName == nameof(MainWindowViewModel.OpacityValue)) Opacity = _viewModel.OpacityValue;
            if (args.PropertyName == nameof(MainWindowViewModel.IsBorderless)) WindowStyle = _viewModel.IsBorderless ? WindowStyle.None : WindowStyle.SingleBorderWindow;
        };
        var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        await _host.GlobalInputHookService.RegisterAsync(handle, _host.Config.Hotkeys);
        _host.GlobalInputHookService.ActionTriggered += GlobalInputHookService_ActionTriggered;
    }

    private async void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _host.Config.Window.Topmost = _viewModel.IsTopmost;
        _host.Config.Window.Borderless = _viewModel.IsBorderless;
        _host.Config.Window.Opacity = _viewModel.OpacityValue;
        _host.WindowStateService.CaptureFromWindow(this, _host.Config.Window);
        var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        await _host.GlobalInputHookService.UnregisterAsync(handle);
    }

    private async void GlobalInputHookService_ActionTriggered(object? sender, AppAction e)
    {
        switch (e)
        {
            case AppAction.Back: await _viewModel.BackAsync(); break;
            case AppAction.Forward: await _viewModel.ForwardAsync(); break;
            case AppAction.Refresh: await _viewModel.RefreshAsync(); break;
            case AppAction.Home: await _viewModel.HomeAsync(); break;
            case AppAction.PlayPauseMedia: await _browserService.ToggleMediaPlayPauseAsync(); break;
            case AppAction.ToggleVisibility:
                Visibility = Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
                break;
        }
    }

    private async void OnAddressEnter(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await _viewModel.OpenAsync();
        }
    }

    private async void OnOpenBookmarks(object sender, RoutedEventArgs e)
    {
        var win = new BookmarksWindow(_host.BookmarkService);
        win.Owner = this;
        if (win.ShowDialog() == true && !string.IsNullOrWhiteSpace(win.SelectedUrl))
        {
            _viewModel.AddressText = win.SelectedUrl;
            await _viewModel.OpenAsync();
        }
    }

    private void OnOpenSettings(object sender, RoutedEventArgs e)
    {
        var win = new SettingsWindow(_host.SettingsService, _host.Config);
        win.Owner = this;
        win.ShowDialog();
    }
}
