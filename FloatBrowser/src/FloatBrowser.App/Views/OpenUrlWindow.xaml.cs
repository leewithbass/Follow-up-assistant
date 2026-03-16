using System.Windows;
using System.Windows.Input;
using FloatBrowser.App.ViewModels;

namespace FloatBrowser.App.Views;

public partial class OpenUrlWindow : Window
{
    public string Url { get; private set; }

    public OpenUrlWindow(string currentUrl)
    {
        InitializeComponent();
        Url = currentUrl;
        UrlTextBox.Text = currentUrl;
        Loaded += (_, _) =>
        {
            UrlTextBox.Focus();
            UrlTextBox.SelectAll();
        };
    }

    private void OnOpen(object sender, RoutedEventArgs e)
    {
        var normalized = MainWindowViewModel.NormalizeUrl(UrlTextBox.Text);
        if (normalized is null)
        {
            MessageBox.Show("请输入有效的网址。", "地址无效", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Url = normalized;
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void OnUrlTextBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            OnOpen(sender, e);
        }
    }
}
