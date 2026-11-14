using System.Text.Json;
using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services
{
    public class DownloadHistoryService : IDownloadHistoryService
    {
        private static readonly string DownloadsFilePath = Path.Combine(FileSystem.AppDataDirectory, "downloads.json");
        private readonly List<DownloadItem> _items = [];
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private bool _isInitialized;

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            await _semaphore.WaitAsync();
            try
            {
                if (_isInitialized) return;

                if (File.Exists(DownloadsFilePath))
                {
                    var json = await File.ReadAllTextAsync(DownloadsFilePath);
                    var loaded = JsonSerializer.Deserialize(json, AppJsonContext.Default.ListDownloadItem);
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
                System.Diagnostics.Debug.WriteLine($"[DownloadHistoryService] Initialize error: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task AddDownloadAsync(string fileName, string downloadUrl, string mimeType, long fileSizeBytes, string localFilePath, string status = "Completed")
        {
            await InitializeAsync();
            await _semaphore.WaitAsync();
            try
            {
                var item = new DownloadItem
                {
                    FileName = fileName,
                    DownloadUrl = downloadUrl,
                    MimeType = mimeType,
                    FileSizeBytes = fileSizeBytes,
                    DownloadedAtUtc = DateTime.UtcNow,
                    LocalFilePath = localFilePath,
                    Status = status
                };

                _items.Insert(0, item);

                if (_items.Count > 500)
                {
                    _items.RemoveRange(500, _items.Count - 500);
                }

                await SaveInternalAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DownloadHistoryService] AddDownload error: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<DownloadItem>> GetDownloadsAsync(string? searchQuery = null, DownloadSortOrder sortOrder = DownloadSortOrder.Newest)
        {
            await InitializeAsync();
            await _semaphore.WaitAsync();
            try
            {
                IEnumerable<DownloadItem> query = _items;
                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    var term = searchQuery.Trim();
                    query = query.Where(x =>
                        (x.FileName != null && x.FileName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                        (x.DownloadUrl != null && x.DownloadUrl.Contains(term, StringComparison.OrdinalIgnoreCase)));
                }

                query = sortOrder switch
                {
                    DownloadSortOrder.Oldest => query.OrderBy(x => x.DownloadedAtUtc),
                    DownloadSortOrder.SizeDesc => query.OrderByDescending(x => x.FileSizeBytes),
                    DownloadSortOrder.SizeAsc => query.OrderBy(x => x.FileSizeBytes),
                    DownloadSortOrder.NameAsc => query.OrderBy(x => x.FileName, StringComparer.CurrentCultureIgnoreCase),
                    _ => query.OrderByDescending(x => x.DownloadedAtUtc)
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

        public bool DoesLocalFileExist(DownloadItem item)
        {
            if (string.IsNullOrWhiteSpace(item.LocalFilePath))
                return false;

#if ANDROID
            try
            {
                if (item.LocalFilePath.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
                {
                    var context = Android.App.Application.Context;
                    var uri = Android.Net.Uri.Parse(item.LocalFilePath);
                    if (uri == null) return false;

                    using var cursor = context.ContentResolver?.Query(uri, [Android.Provider.IBaseColumns.Id], null, null, null);
                    return cursor != null && cursor.MoveToFirst();
                }

                return File.Exists(item.LocalFilePath);
            }
            catch
            {
                return false;
            }
#else
            return File.Exists(item.LocalFilePath);
#endif
        }

        public Task<bool> OpenDownloadedFileAsync(string localFilePath, string? mimeType)
        {
            if (string.IsNullOrWhiteSpace(localFilePath))
                return Task.FromResult(false);

#if ANDROID
            try
            {
                var context = Android.App.Application.Context;
                var notifService = new Platforms.Android.Services.AndroidDownloadNotificationService(context);
                var intent = notifService.CreateViewIntent(localFilePath, mimeType ?? "*/*");
                if (intent == null) return Task.FromResult(false);

                if (Platform.CurrentActivity != null)
                {
                    Platform.CurrentActivity.StartActivity(intent);
                }
                else
                {
                    intent.AddFlags(Android.Content.ActivityFlags.NewTask);
                    context.StartActivity(intent);
                }
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DownloadHistoryService] OpenDownloadedFileAsync error: {ex.Message}");
                return Task.FromResult(false);
            }
#else
            return Task.Run(async () =>
            {
                try
                {
                    return await Launcher.Default.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(localFilePath) });
                }
                catch
                {
                    return false;
                }
            });
#endif
        }

        public string ResolveDisplayPath(string? localFilePath, string? fileName = null)
        {
            return StoragePathHelper.ResolveUriToDisplayPath(localFilePath, fileName);
        }

        public async Task ImportDownloadsAsync(List<DownloadItem> incoming, bool overwrite)
        {
            if (incoming == null) return;

            await InitializeAsync();
            await _semaphore.WaitAsync();
            try
            {
                if (overwrite)
                {
                    _items.Clear();
                    _items.AddRange(incoming.OrderByDescending(x => x.DownloadedAtUtc));
                }
                else
                {
                    foreach (var item in incoming)
                    {
                        if (!_items.Any(x => x.Id == item.Id || (!string.IsNullOrEmpty(x.DownloadUrl) && string.Equals(x.DownloadUrl, item.DownloadUrl, StringComparison.OrdinalIgnoreCase))))
                        {
                            _items.Add(item);
                        }
                    }
                    _items.Sort((a, b) => b.DownloadedAtUtc.CompareTo(a.DownloadedAtUtc));
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

        public async Task<(int TotalCount, long TotalBytes)> GetDownloadStatsAsync()
        {
            await InitializeAsync();
            await _semaphore.WaitAsync();
            try
            {
                return (_items.Count, _items.Sum(x => x.FileSizeBytes));
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
                var json = JsonSerializer.Serialize(_items, AppJsonContext.Default.ListDownloadItem);
                await File.WriteAllTextAsync(DownloadsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DownloadHistoryService] Failed to save downloads: {ex.Message}");
            }
        }
    }
}
