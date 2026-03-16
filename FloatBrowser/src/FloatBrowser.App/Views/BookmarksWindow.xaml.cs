using System.Windows;
using FloatBrowser.App.Application;
using FloatBrowser.App.ViewModels;

namespace FloatBrowser.App.Views;

public partial class BookmarksWindow : Window
{
    private readonly BookmarksViewModel _viewModel;
    public string? SelectedUrl { get; private set; }

    public BookmarksWindow(IBookmarkService bookmarkService)
    {
        InitializeComponent();
        _viewModel = new BookmarksViewModel(bookmarkService);
        _viewModel.OpenRequested += (_, item) => { SelectedUrl = item.Url; DialogResult = true; };
        DataContext = _viewModel;
        Loaded += async (_, _) => await _viewModel.InitializeAsync();
    }

    private async void OnDelete(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("确认删除该书签吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            await _viewModel.DeleteAsync();
        }
    }

    private async void OnOpen(object sender, RoutedEventArgs e) => await _viewModel.OpenAsync();
    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
