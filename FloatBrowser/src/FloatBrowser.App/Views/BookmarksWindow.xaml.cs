using System.Windows;
using FloatBrowser.App.Application;
using FloatBrowser.App.Domain;
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
        _viewModel.OpenRequested += (_, item) =>
        {
            SelectedUrl = item.Url;
            DialogResult = true;
        };
        DataContext = _viewModel;
        Loaded += async (_, _) => await _viewModel.InitializeAsync();
    }

    private async void OnDelete(object sender, RoutedEventArgs e)
    {
        var selectedItems = BookmarksGrid.SelectedItems.Cast<BookmarkItem>().ToList();
        if (selectedItems.Count == 0 && _viewModel.SelectedItem is not null)
        {
            selectedItems.Add(_viewModel.SelectedItem);
        }

        if (selectedItems.Count == 0)
        {
            return;
        }

        var confirmMessage = selectedItems.Count > 1 ? "Confirm delete selected bookmarks?" : "Confirm delete this bookmark?";
        if (MessageBox.Show(confirmMessage, "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        await _viewModel.DeleteSelectedAsync(selectedItems);
    }

    private async void OnOpen(object sender, RoutedEventArgs e) => await _viewModel.OpenAsync();

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
