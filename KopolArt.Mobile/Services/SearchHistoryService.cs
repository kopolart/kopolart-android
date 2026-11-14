using System.Diagnostics;
using System.Text.Json;
using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services
{
    public class SearchHistoryService : ISearchHistoryService
    {
        private const int MaxRecentSearches = 5;
        private readonly object _lock = new();

        public List<string> GetRecentSearches()
        {
            lock (_lock)
            {
                try
                {
                    var raw = Preferences.Default.Get(Constants.Keys.RecentSearchQueries, string.Empty);
                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        return new List<string>();
                    }

                    var items = JsonSerializer.Deserialize(raw, AppJsonContext.Default.ListString);
                    if (items == null)
                    {
                        return new List<string>();
                    }

                    return items
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Take(MaxRecentSearches)
                        .ToList();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SearchHistoryService] GetRecentSearches error: {ex.Message}");
                    return new List<string>();
                }
            }
        }

        public Task<List<string>> GetRecentSearchesAsync()
        {
            return Task.FromResult(GetRecentSearches());
        }

        public void AddRecentSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return;

            query = query.Trim();
            lock (_lock)
            {
                try
                {
                    var current = GetRecentSearches();
                    current.RemoveAll(s => string.Equals(s, query, StringComparison.OrdinalIgnoreCase));
                    current.Insert(0, query);

                    while (current.Count > MaxRecentSearches)
                    {
                        current.RemoveAt(current.Count - 1);
                    }

                    SaveRecentSearchesInternal(current);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SearchHistoryService] AddRecentSearch error: {ex.Message}");
                }
            }
        }

        public Task AddRecentSearchAsync(string query)
        {
            AddRecentSearch(query);
            return Task.CompletedTask;
        }

        public void RemoveRecentSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return;

            query = query.Trim();
            lock (_lock)
            {
                try
                {
                    var current = GetRecentSearches();
                    var removed = current.RemoveAll(s => string.Equals(s, query, StringComparison.OrdinalIgnoreCase));
                    if (removed > 0)
                    {
                        SaveRecentSearchesInternal(current);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SearchHistoryService] RemoveRecentSearch error: {ex.Message}");
                }
            }
        }

        public Task RemoveRecentSearchAsync(string query)
        {
            RemoveRecentSearch(query);
            return Task.CompletedTask;
        }

        public void ClearAllRecentSearches()
        {
            lock (_lock)
            {
                try
                {
                    Preferences.Default.Remove(Constants.Keys.RecentSearchQueries);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SearchHistoryService] ClearAllRecentSearches error: {ex.Message}");
                }
            }
        }

        public Task ClearAllRecentSearchesAsync()
        {
            ClearAllRecentSearches();
            return Task.CompletedTask;
        }

        public Task ImportRecentSearchesAsync(IEnumerable<string> searches, bool overwrite = false)
        {
            if (searches == null)
                return Task.CompletedTask;

            lock (_lock)
            {
                try
                {
                    var validSearches = searches
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    if (overwrite)
                    {
                        var limited = validSearches.Take(MaxRecentSearches).ToList();
                        SaveRecentSearchesInternal(limited);
                    }
                    else
                    {
                        var existing = GetRecentSearches();
                        var merged = existing.Concat(validSearches)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .Take(MaxRecentSearches)
                            .ToList();

                        SaveRecentSearchesInternal(merged);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SearchHistoryService] ImportRecentSearchesAsync error: {ex.Message}");
                }
            }

            return Task.CompletedTask;
        }

        private void SaveRecentSearchesInternal(List<string> queries)
        {
            try
            {
                var clean = queries
                    .Where(q => !string.IsNullOrWhiteSpace(q))
                    .Select(q => q.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(MaxRecentSearches)
                    .ToList();

                var json = JsonSerializer.Serialize(clean, AppJsonContext.Default.ListString);
                Preferences.Default.Set(Constants.Keys.RecentSearchQueries, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SearchHistoryService] SaveRecentSearchesInternal error: {ex.Message}");
            }
        }
    }
}
