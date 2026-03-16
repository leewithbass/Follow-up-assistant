using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FloatBrowser.App.Application;
using FloatBrowser.App.Domain;
using FloatBrowser.App.ViewModels;

namespace FloatBrowser.App.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(ISettingsService settingsService, AppConfiguration config)
    {
        InitializeComponent();
        var vm = new SettingsViewModel(settingsService, config);
        DataContext = new SettingsWindowWrapper(vm);
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}

public partial class SettingsWindowWrapper : ObservableObject
{
    private readonly SettingsViewModel _inner;
    public ObservableCollection<string> MouseActions { get; } = ["PlayPauseMedia", "Back", "Forward", "Refresh", "None"];

    public SettingsWindowWrapper(SettingsViewModel inner) => _inner = inner;

    public string Back { get => _inner.Back; set => _inner.Back = value; }
    public string Forward { get => _inner.Forward; set => _inner.Forward = value; }
    public string Refresh { get => _inner.Refresh; set => _inner.Refresh = value; }
    public string ToggleVisibility { get => _inner.ToggleVisibility; set => _inner.ToggleVisibility = value; }
    public string Home { get => _inner.Home; set => _inner.Home = value; }
    public string PlayPauseMedia { get => _inner.PlayPauseMedia; set => _inner.PlayPauseMedia = value; }
    public string MouseXButton1 { get => _inner.MouseXButton1; set => _inner.MouseXButton1 = value; }
    public string MouseXButton2 { get => _inner.MouseXButton2; set => _inner.MouseXButton2 = value; }
    public string HomeUrl { get => _inner.HomeUrl; set => _inner.HomeUrl = value; }
    public string Message => _inner.Message;

    public IAsyncRelayCommand SaveCommand => _inner.SaveCommand;
    public IAsyncRelayCommand RestoreDefaultsCommand => _inner.RestoreDefaultsCommand;
}
