using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services
{
    public class SearchSuggestionService : ISearchSuggestionService
    {
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);
        private const int MinTermLength = 2;

        private readonly HttpClient _httpClient;
        private readonly ConcurrentDictionary<string, (DateTime CachedAt, List<SearchSuggestionItem> Items)> _cache = new(StringComparer.OrdinalIgnoreCase);

        public SearchSuggestionService()
        {
            _httpClient = new HttpClient
            {
                Timeout = RequestTimeout
            };

            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", Constants.Network.AppUserAgent);
        }

        public async Task<List<SearchSuggestionItem>> GetSuggestionsAsync(
            string term,
            SearchCategoryType category,
            CancellationToken cancellationToken = default)
        {
            var cleanTerm = term?.Trim() ?? string.Empty;
            if (cleanTerm.Length < MinTermLength)
            {
                return [];
            }

            var part = MapCategoryToPart(category);
            var cacheKey = $"{part}:{cleanTerm}";

            // 1. Check in-memory cache
            if (_cache.TryGetValue(cacheKey, out var cached) &&
                (DateTime.UtcNow - cached.CachedAt) < CacheDuration)
            {
                return cached.Items;
            }

            // 2. Connectivity check - fail fast without blocking UI
            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    return [];
                }
            }
            catch
            {
                // Continue if network check throws on certain platform versions
            }

            // 3. Make HTTP request to website API
            try
            {
                var encodedTerm = Uri.EscapeDataString(cleanTerm);
                var baseHost = Constants.Urls.Home.TrimEnd('/');
                var requestUrl = $"{baseHost}/api/search?part={part}&term={encodedTerm}";

                using var response = await _httpClient.GetAsync(requestUrl, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return [];
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return [];
                }

                var items = JsonSerializer.Deserialize(json, AppJsonContext.Default.ListSearchSuggestionItem) ?? [];

                // 4. Sanitize and populate full URLs
                foreach (var item in items)
                {
                    if (!string.IsNullOrWhiteSpace(item.Image))
                    {
                        item.FullImageUrl = item.Image.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                            ? item.Image
                            : $"{baseHost}/{item.Image.TrimStart('/')}";
                    }

                    if (!string.IsNullOrWhiteSpace(item.Url))
                    {
                        item.FullTargetUrl = item.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                            ? item.Url
                            : $"{baseHost}/{item.Url.TrimStart('/')}";
                    }
                    else
                    {
                        var categoryUrl = MapCategoryToSearchUrl(category);
                        item.FullTargetUrl = $"{categoryUrl}?query={Uri.EscapeDataString(item.Label)}";
                    }
                }

                // 5. Store in cache (clean old entries if growing large)
                if (_cache.Count > 100)
                {
                    _cache.Clear();
                }
                _cache[cacheKey] = (DateTime.UtcNow, items);

                return items;
            }
            catch (OperationCanceledException)
            {
                return [];
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SearchSuggestionService] Error fetching suggestions: {ex.Message}");
                return [];
            }
        }

        private static string MapCategoryToPart(SearchCategoryType category) => category switch
        {
            SearchCategoryType.Palettes => "color-palettes",
            SearchCategoryType.Colors => "colors",
            SearchCategoryType.Wallpapers => "wallpapers",
            _ => "posts"
        };

        private static string MapCategoryToSearchUrl(SearchCategoryType category) => category switch
        {
            SearchCategoryType.Palettes => Constants.Urls.ColorPalettesSearch,
            SearchCategoryType.Colors => Constants.Urls.ColorsSearch,
            SearchCategoryType.Wallpapers => Constants.Urls.WallpapersSearch,
            _ => Constants.Urls.PostsSearch
        };
    }
}
