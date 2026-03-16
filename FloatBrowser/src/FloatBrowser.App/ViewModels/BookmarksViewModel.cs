using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FloatBrowser.App.Application;
using FloatBrowser.App.Domain;

namespace FloatBrowser.App.ViewModels;

public partial class BookmarksViewModel : ObservableObject
{
    private readonly IBookmarkService _bookmarkService;

    public ObservableCollection<BookmarkItem> Items { get; } = [];
    [ObservableProperty] private BookmarkItem? selectedItem;

    public event EventHandler<BookmarkItem>? OpenRequested;

    public BookmarksViewModel(IBookmarkService bookmarkService)
    {
        _bookmarkService = bookmarkService;
    }

    public async Task InitializeAsync()
    {
        Items.Clear();
        foreach (var item in await _bookmarkService.GetAllAsync())
        {
            Items.Add(item);
        }
    }

    [RelayCommand]
    public Task OpenAsync()
    {
        if (SelectedItem is not null)
        {
            OpenRequested?.Invoke(this, SelectedItem);
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    public async Task DeleteAsync()
    {
        if (SelectedItem is null) return;
        await _bookmarkService.DeleteAsync(SelectedItem);
        await InitializeAsync();
    }
}
