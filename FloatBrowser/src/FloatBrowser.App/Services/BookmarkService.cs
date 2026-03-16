using FloatBrowser.App.Application;
using FloatBrowser.App.Domain;
using FloatBrowser.App.Infrastructure;

namespace FloatBrowser.App.Services;

public class BookmarkService : IBookmarkService
{
    private readonly IJsonFileStorage _storage;
    private readonly ILogger _logger;
    private readonly string _bookmarkPath;

    public BookmarkService(IJsonFileStorage storage, ILogger logger)
    {
        _storage = storage;
        _logger = logger;
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FloatBrowser");
        _bookmarkPath = Path.Combine(folder, "bookmarks.json");
    }

    public async Task<IReadOnlyList<BookmarkItem>> GetAllAsync()
    {
        try
        {
            return await _storage.ReadAsync<List<BookmarkItem>>(_bookmarkPath) ?? [];
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Read bookmarks failed.", ex);
            return [];
        }
    }

    public async Task AddAsync(BookmarkItem item)
    {
        var list = (await GetAllAsync()).ToList();
        list.Add(item);
        await PersistAsync(list);
    }

    public async Task UpdateAsync(BookmarkItem original, BookmarkItem updated)
    {
        var list = (await GetAllAsync()).ToList();
        var idx = list.FindIndex(x => x.CreatedAt == original.CreatedAt && x.Url == original.Url);
        if (idx >= 0)
        {
            list[idx] = updated;
            await PersistAsync(list);
        }
    }

    public async Task DeleteAsync(BookmarkItem item)
    {
        var list = (await GetAllAsync()).ToList();
        list.RemoveAll(x => x.CreatedAt == item.CreatedAt && x.Url == item.Url);
        await PersistAsync(list);
    }

    private async Task PersistAsync(List<BookmarkItem> list)
    {
        try
        {
            await _storage.WriteAsync(_bookmarkPath, list);
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync("Save bookmarks failed.", ex);
        }
    }
}
