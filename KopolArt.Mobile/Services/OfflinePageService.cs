using System.Text.Json;
using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services
{
    public class OfflinePageService : IOfflinePageService
    {
        private static readonly string OfflinePagesJsonPath = Path.Combine(FileSystem.AppDataDirectory, "offline_pages.json");
        private readonly List<OfflinePageItem> _items = [];
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private bool _isInitialized;

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            await _semaphore.WaitAsync();
            try
            {
                if (_isInitialized) return;

                if (File.Exists(OfflinePagesJsonPath))
                {
                    var json = await File.ReadAllTextAsync(OfflinePagesJsonPath);
                    var loaded = JsonSerializer.Deserialize(json, AppJsonContext.Default.ListOfflinePageItem);
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
                System.Diagnostics.Debug.WriteLine($"[OfflinePageService] Initialize error: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<OfflinePageItem>> GetOfflinePagesAsync(string? searchQuery = null, string? categoryFilter = null, OfflinePageSortOrder sortOrder = OfflinePageSortOrder.Newest)
        {
            await InitializeAsync();
            await _semaphore.WaitAsync();
            try
            {
                IEnumerable<OfflinePageItem> query = _items;

                if (!string.IsNullOrWhiteSpace(categoryFilter) && categoryFilter != CategoryBadgeHelper.CategoryAll)
                {
                    query = query.Where(x => string.Equals(CategoryBadgeHelper.GetCategory(x.OriginalUrl), categoryFilter, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    var term = searchQuery.Trim().ToLowerInvariant();
                    query = query.Where(x =>
                        (!string.IsNullOrEmpty(x.Title) && x.Title.ToLowerInvariant().Contains(term)) ||
                        (!string.IsNullOrEmpty(x.OriginalUrl) && x.OriginalUrl.ToLowerInvariant().Contains(term)));
                }

                query = sortOrder switch
                {
                    OfflinePageSortOrder.Oldest => query.OrderBy(x => x.SavedAtUtc),
                    OfflinePageSortOrder.TitleAsc => query.OrderBy(x => x.Title, StringComparer.CurrentCultureIgnoreCase),
                    OfflinePageSortOrder.SizeDesc => query.OrderByDescending(x => x.FileSizeBytes),
                    OfflinePageSortOrder.SizeAsc => query.OrderBy(x => x.FileSizeBytes),
                    _ => query.OrderByDescending(x => x.SavedAtUtc)
                };

                return query.ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<OfflinePageItem?> GetOfflinePageByIdAsync(string id)
        {
            await InitializeAsync();
            await _semaphore.WaitAsync();
            try
            {
                return _items.FirstOrDefault(x => x.Id == id);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> AddOfflinePageAsync(OfflinePageItem item)
        {
            await InitializeAsync();
            await _semaphore.WaitAsync();
            try
            {
                // Remove any existing entry with the same original URL if present
                var existing = _items.FirstOrDefault(x => string.Equals(x.OriginalUrl, item.OriginalUrl, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    _items.Remove(existing);
                    try
                    {
                        if (File.Exists(existing.FilePath) && existing.FilePath != item.FilePath)
                        {
                            File.Delete(existing.FilePath);
                        }
                    }
                    catch { }
                }

                _items.Insert(0, item);
                await SaveToFileAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OfflinePageService] Add error: {ex.Message}");
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> DeleteOfflinePageAsync(string id)
        {
            await InitializeAsync();
            await _semaphore.WaitAsync();
            try
            {
                var item = _items.FirstOrDefault(x => x.Id == id);
                if (item != null)
                {
                    _items.Remove(item);
                    try
                    {
                        if (File.Exists(item.FilePath))
                        {
                            File.Delete(item.FilePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[OfflinePageService] Delete file error: {ex.Message}");
                    }

                    await SaveToFileAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OfflinePageService] Delete error: {ex.Message}");
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<int> GetOfflinePagesCountAsync()
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

        public async Task ClearAllOfflinePagesAsync()
        {
            await InitializeAsync();
            await _semaphore.WaitAsync();
            try
            {
                foreach (var item in _items)
                {
                    try
                    {
                        if (File.Exists(item.FilePath))
                        {
                            File.Delete(item.FilePath);
                        }
                    }
                    catch { }
                }

                _items.Clear();
                await SaveToFileAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OfflinePageService] Clear error: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> ImportOfflinePagesAsync(List<OfflinePageItem> pages)
        {
            if (pages == null || pages.Count == 0) return true;

            await InitializeAsync();
            await _semaphore.WaitAsync();
            try
            {
                foreach (var page in pages)
                {
                    if (!_items.Any(x => x.Id == page.Id || x.OriginalUrl == page.OriginalUrl))
                    {
                        _items.Add(page);
                    }
                }

                await SaveToFileAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OfflinePageService] Import error: {ex.Message}");
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task SaveToFileAsync()
        {
            var json = JsonSerializer.Serialize(_items, AppJsonContext.Default.ListOfflinePageItem);
            await File.WriteAllTextAsync(OfflinePagesJsonPath, json);
        }
    }
}
