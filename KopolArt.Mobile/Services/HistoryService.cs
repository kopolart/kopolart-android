using System.Text.Json;
using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services
{
    public class HistoryService : IHistoryService
    {
        private static readonly string HistoryFilePath = Path.Combine(FileSystem.AppDataDirectory, "history.json");
        private readonly List<HistoryItem> _items = [];
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private bool _isInitialized;

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            await _semaphore.WaitAsync();
            try
            {
                if (_isInitialized) return;

                if (File.Exists(HistoryFilePath))
                {
                    var json = await File.ReadAllTextAsync(HistoryFilePath);
                    var loaded = JsonSerializer.Deserialize(json, AppJsonContext.Default.ListHistoryItem);
                    if (loaded != null)
                    {
                        _items.Clear();
                        _items.AddRange(loaded);
                    }
                }
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HistoryService] Initialize error: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task AddVisitAsync(string title, string url)
        {
            if (string.IsNullOrWhiteSpace(url) || url.StartsWith("about:", StringComparison.OrdinalIgnoreCase) || url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                return;

            await InitializeAsync();
            await _semaphore.WaitAsync();
            try
            {
                // Format clean title
                var cleanTitle = string.IsNullOrWhiteSpace(title) ? url : title.Trim();

                // Deduplicate if immediate last visit is identical URL
                var existing = _items.FirstOrDefault(x => string.Equals(x.Url, url, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    _items.Remove(existing);
                    existing.VisitedAtUtc = DateTime.UtcNow;
                    existing.Title = cleanTitle;
                    _items.Insert(0, existing);
                }
                else
                {
                    _items.Insert(0, new HistoryItem
                    {
                        Title = cleanTitle,
                        Url = url,
                        VisitedAtUtc = DateTime.UtcNow
                    });
                }

                // Keep max 500 records
                if (_items.Count > 500)
                {
                    _items.RemoveRange(500, _items.Count - 500);
                }

                await SaveInternalAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HistoryService] AddVisit error: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<HistoryItem>> GetHistoryAsync(string? searchQuery = null, HistorySortOrder sortOrder = HistorySortOrder.Newest)
        {
            await InitializeAsync();
            await _semaphore.WaitAsync();
            try
            {
                IEnumerable<HistoryItem> query = _items;
                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    var term = searchQuery.Trim();
                    query = query.Where(x =>
                        (x.Title != null && x.Title.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                        (x.Url != null && x.Url.Contains(term, StringComparison.OrdinalIgnoreCase)));
                }

                query = sortOrder switch
                {
                    HistorySortOrder.Oldest => query.OrderBy(x => x.VisitedAtUtc),
                    HistorySortOrder.TitleAsc => query.OrderBy(x => x.Title, StringComparer.CurrentCultureIgnoreCase),
                    _ => query.OrderByDescending(x => x.VisitedAtUtc)
                };

                return query.ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task DeleteItemAsync(string id)
        {
            await InitializeAsync();
            await _semaphore.WaitAsync();
            try
            {
                var removed = _items.RemoveAll(x => x.Id == id);
                if (removed > 0)
                {
                    await SaveInternalAsync();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task ClearHistoryAsync()
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

        public async Task ImportHistoryAsync(List<HistoryItem> incoming, bool overwrite)
        {
            if (incoming == null) return;

            await InitializeAsync();
            await _semaphore.WaitAsync();
            try
            {
                if (overwrite)
                {
                    _items.Clear();
                    _items.AddRange(incoming.OrderByDescending(x => x.VisitedAtUtc));
                }
                else
                {
                    foreach (var item in incoming)
                    {
                        if (!_items.Any(x => string.Equals(x.Url, item.Url, StringComparison.OrdinalIgnoreCase)))
                        {
                            _items.Add(item);
                        }
                    }
                    _items.Sort((a, b) => b.VisitedAtUtc.CompareTo(a.VisitedAtUtc));
                }

                if (_items.Count > 500)
                {
                    _items.RemoveRange(500, _items.Count - 500);
                }

                await SaveInternalAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<int> GetTotalVisitsCountAsync()
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

        private async Task SaveInternalAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_items, AppJsonContext.Default.ListHistoryItem);
                await File.WriteAllTextAsync(HistoryFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HistoryService] Failed to save history: {ex.Message}");
            }
        }
    }
}
