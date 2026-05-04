using System.Windows;
using FloatBrowser.App.Application;
using FloatBrowser.App.Domain;
using FloatBrowser.App.ViewModels;

namespace FloatBrowser.App.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(ISettingsService settingsService, AppConfiguration config)
    {
        InitializeComponent();
        DataContext = new SettingsViewModel(settingsService, config);
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
