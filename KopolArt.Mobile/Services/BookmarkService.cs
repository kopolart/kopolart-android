using System.Text.Json;
using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services
{
    public class BookmarkService : IBookmarkService
    {
        private static readonly string BookmarkFilePath = Path.Combine(FileSystem.AppDataDirectory, "bookmarks.json");
        private readonly List<BookmarkItem> _items = [];
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private bool _isInitialized;

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            await _semaphore.WaitAsync();
            try
            {
                if (_isInitialized) return;

                if (File.Exists(BookmarkFilePath))
                {
                    var json = await File.ReadAllTextAsync(BookmarkFilePath);
                    var loaded = JsonSerializer.Deserialize(json, AppJsonContext.Default.ListBookmarkItem);
                    if (loaded != null)
                    {
                        _items.Clear();
                        _items.AddRange(loaded.OrderByDescending(x => x.SavedAtUtc));
                    }
                }
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BookmarkService] Initialize error: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<BookmarkItem>> GetBookmarksAsync(string? searchQuery = null, string? categoryFilter = null, BookmarkSortOrder sortOrder = BookmarkSortOrder.Newest)
        {
            await InitializeAsync();
            await _semaphore.WaitAsync();
            try
            {
                IEnumerable<BookmarkItem> query = _items;

                if (!string.IsNullOrWhiteSpace(categoryFilter) && categoryFilter != CategoryBadgeHelper.CategoryAll)
                {
                    query = query.Where(x => string.Equals(CategoryBadgeHelper.GetCategory(x.Url), categoryFilter, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    var term = searchQuery.Trim().ToLowerInvariant();
                    query = query.Where(x =>
                        (!string.IsNullOrEmpty(x.Title) && x.Title.ToLowerInvariant().Contains(term)) ||
                        (!string.IsNullOrEmpty(x.Url) && x.Url.ToLowerInvariant().Contains(term)));
                }

                query = sortOrder switch
                {
                    BookmarkSortOrder.Oldest => query.OrderBy(x => x.SavedAtUtc),
                    BookmarkSortOrder.TitleAsc => query.OrderBy(x => x.Title, StringComparer.CurrentCultureIgnoreCase),
                    BookmarkSortOrder.TitleDesc => query.OrderByDescending(x => x.Title, StringComparer.CurrentCultureIgnoreCase),
                    _ => query.OrderByDescending(x => x.SavedAtUtc)
                };

                return query.ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> IsBookmarkedAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;

            await InitializeAsync();
            await _semaphore.WaitAsync();
            try
            {
                var normalized = NormalizeUrl(url);
                return _items.Any(x => NormalizeUrl(x.Url) == normalized);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> ToggleBookmarkAsync(string url, string title)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;

            await InitializeAsync();
            await _semaphore.WaitAsync();
            try
            {
                var normalized = NormalizeUrl(url);
                var existing = _items.FirstOrDefault(x => NormalizeUrl(x.Url) == normalized);

                if (existing != null)
                {
                    _items.Remove(existing);
                    await SaveInternalAsync();
                    return false; // Removed
                }
                else
                {
                    var cleanTitle = string.IsNullOrWhiteSpace(title) ? url : title.Trim();
                    _items.Insert(0, new BookmarkItem
                    {
                        Title = cleanTitle,
                        Url = url.Trim(),
                        SavedAtUtc = DateTime.UtcNow
                    });
                    await SaveInternalAsync();
                    return true; // Added
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task AddBookmarkAsync(string title, string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;

            await InitializeAsync();
            await _semaphore.WaitAsync();
            try
            {
                var normalized = NormalizeUrl(url);
                var existing = _items.FirstOrDefault(x => NormalizeUrl(x.Url) == normalized);
                var cleanTitle = string.IsNullOrWhiteSpace(title) ? url : title.Trim();

                if (existing != null)
                {
                    existing.Title = cleanTitle;
                    existing.SavedAtUtc = DateTime.UtcNow;
                }
                else
                {
                    _items.Insert(0, new BookmarkItem
                    {
                        Title = cleanTitle,
                        Url = url.Trim(),
                        SavedAtUtc = DateTime.UtcNow
                    });
                }

                await SaveInternalAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> RemoveBookmarkAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;

            await InitializeAsync();
            await _semaphore.WaitAsync();
            try
            {
                var normalized = NormalizeUrl(url);
                var existing = _items.FirstOrDefault(x => NormalizeUrl(x.Url) == normalized);
                if (existing != null)
                {
                    _items.Remove(existing);
                    await SaveInternalAsync();
                    return true;
                }
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task DeleteItemAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return;

            await InitializeAsync();
            await _semaphore.WaitAsync();
            try
            {
                var item = _items.FirstOrDefault(x => x.Id == id);
                if (item != null)
                {
                    _items.Remove(item);
                    await SaveInternalAsync();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task ClearAllAsync()
        {
            await InitializeAsync();
            await _semaphore.WaitAsync();
            try
            {
                _items.Clear();
                await SaveInternalAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task ImportBookmarksAsync(List<BookmarkItem> incoming, bool overwrite)
        {
            if (incoming == null) return;

            await InitializeAsync();
            await _semaphore.WaitAsync();
            try
            {
                if (overwrite)
                {
                    _items.Clear();
                    _items.AddRange(incoming.OrderByDescending(x => x.SavedAtUtc));
                }
                else
                {
                    foreach (var item in incoming)
                    {
                        var normalized = NormalizeUrl(item.Url);
                        if (!_items.Any(x => NormalizeUrl(x.Url) == normalized))
                        {
                            _items.Add(item);
                        }
                    }
                    _items.Sort((a, b) => b.SavedAtUtc.CompareTo(a.SavedAtUtc));
                }

                await SaveInternalAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<int> GetBookmarksCountAsync()
        {
            await InitializeAsync();
            await _semaphore.WaitAsync();
            try
            {
                return _items.Count;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private static string NormalizeUrl(string url)
        {
            return url.Trim().TrimEnd('/').ToLowerInvariant();
        }

        private async Task SaveInternalAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_items, AppJsonContext.Default.ListBookmarkItem);
                await File.WriteAllTextAsync(BookmarkFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BookmarkService] SaveInternal error: {ex.Message}");
            }
        }
    }
}
