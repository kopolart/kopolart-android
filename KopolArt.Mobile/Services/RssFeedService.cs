using System.Xml.Linq;
using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services
{
    public class RssFeedService : IRssFeedService
    {
        /// <summary>Upper bound of posts summarised in one digest notification.</summary>
        public const int MaxDigestItems = 10;

        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(25);
        private readonly ISettingsService? _settingsService;

        public RssFeedService(ISettingsService? settingsService = null)
        {
            _settingsService = settingsService;
        }

        public async Task<List<RssFeedItem>> FetchFeedAsync(CancellationToken cancellationToken = default)
        {
            return await FetchFeedDirectAsync(cancellationToken);
        }

        public async Task<RssFeedItem?> GetLatestNewPostAsync(CancellationToken cancellationToken = default)
        {
            bool isEnabled = _settingsService?.Settings.NotifyNewContent ?? Preferences.Get(Constants.Keys.NotifyNewContent, true);
            return await GetLatestNewPostDirectAsync(isEnabled, cancellationToken);
        }

        public async Task<List<RssFeedItem>> GetUnseenNewPostsAsync(CancellationToken cancellationToken = default)
        {
            bool isEnabled = _settingsService?.Settings.NotifyNewContent ?? Preferences.Get(Constants.Keys.NotifyNewContent, true);
            return await GetUnseenNewPostsDirectAsync(isEnabled, cancellationToken);
        }

        public void MarkPostAsNotified(RssFeedItem post)
        {
            MarkPostAsNotifiedDirect(post);
        }

        public bool CanNotifyNow()
        {
            bool isEnabled = _settingsService?.Settings.NotifyNewContent ?? Preferences.Get(Constants.Keys.NotifyNewContent, true);
            return CanNotifyNowDirect(isEnabled);
        }

        public async Task<Dictionary<string, int>> GetRecentCategoryCountsAsync(TimeSpan? window = null, CancellationToken cancellationToken = default)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["colors"] = 0,
                ["color-palettes"] = 0,
                ["posts"] = 0,
                ["wallpapers"] = 0
            };

            var maxAge = window ?? TimeSpan.FromDays(7);
            var items = await FetchFeedAsync(cancellationToken);
            var nowUtc = DateTime.UtcNow;

            foreach (var item in items)
            {
                if (!item.PublishDate.HasValue) continue;
                var pubUtc = item.PublishDate.Value.ToUniversalTime();
                var age = nowUtc - pubUtc;
                if (age.TotalSeconds >= -300 && age <= maxAge)
                {
                    var catKey = MatchCategoryKey(item.Link);
                    if (catKey != null && counts.ContainsKey(catKey))
                    {
                        counts[catKey]++;
                    }
                }
            }

            return counts;
        }

        public static string? MatchCategoryKey(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            if (url.Contains("/color-palettes", StringComparison.OrdinalIgnoreCase))
                return "color-palettes";

            if (url.Contains("/colors", StringComparison.OrdinalIgnoreCase))
                return "colors";

            if (url.Contains("/posts", StringComparison.OrdinalIgnoreCase))
                return "posts";

            if (url.Contains("/wallpapers", StringComparison.OrdinalIgnoreCase))
                return "wallpapers";

            return null;
        }

        public static bool CanNotifyNowDirect(bool isEnabled)
        {
            if (!isEnabled)
                return false;

            string lastNotifiedUtcStr = Preferences.Get(Constants.Keys.RssLastNotifiedUtc, string.Empty);
            if (!string.IsNullOrWhiteSpace(lastNotifiedUtcStr) &&
                DateTime.TryParse(lastNotifiedUtcStr, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var lastNotifiedUtc))
            {
                // Strict rule: at most one notification within 3 days
                if ((DateTime.UtcNow - lastNotifiedUtc).TotalDays < 3)
                {
                    return false;
                }
            }

            return true;
        }

        public static void MarkPostAsNotifiedDirect(RssFeedItem post)
        {
            if (post == null) return;
            string id = !string.IsNullOrWhiteSpace(post.Guid) ? post.Guid : post.Link;
            Preferences.Set(Constants.Keys.RssLastNotifiedPostGuid, id);
            Preferences.Set(Constants.Keys.RssLastNotifiedUtc, DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
        }

        public static async Task<RssFeedItem?> GetLatestNewPostDirectAsync(bool isEnabled, CancellationToken cancellationToken = default)
        {
            var posts = await GetUnseenNewPostsDirectAsync(isEnabled, cancellationToken);
            return posts.Count > 0 ? posts[0] : null;
        }

        /// <summary>
        /// Returns every feed item published after the last announced post (newest first), so a
        /// single digest notification can report all of them instead of only the top item.
        /// </summary>
        public static async Task<List<RssFeedItem>> GetUnseenNewPostsDirectAsync(
            bool isEnabled,
            CancellationToken cancellationToken = default,
            int maxItems = MaxDigestItems)
        {
            var unseen = new List<RssFeedItem>();

            if (!CanNotifyNowDirect(isEnabled))
                return unseen;

            var items = await FetchFeedDirectAsync(cancellationToken);
            if (items.Count == 0)
                return unseen;

            // Rule: On the first time this feature runs, record the current latest post as baseline
            // so existing posts are NOT spammed to the user as "new"!
            bool baselineInit = Preferences.Get(Constants.Keys.RssBaselineInitialized, false);
            string lastNotifiedId = Preferences.Get(Constants.Keys.RssLastNotifiedPostGuid, string.Empty);

            if (!baselineInit || string.IsNullOrWhiteSpace(lastNotifiedId))
            {
                Preferences.Set(Constants.Keys.RssLastNotifiedPostGuid, GetItemId(items[0]));
                Preferences.Set(Constants.Keys.RssBaselineInitialized, true);
                return unseen;
            }

            int limit = Math.Max(1, Math.Min(maxItems, items.Count));
            bool reachedAnnouncedPost = false;

            foreach (var item in items)
            {
                // Deduplication rule: stop at the last post we already announced to the user
                if (IsSamePost(item, lastNotifiedId))
                {
                    reachedAnnouncedPost = true;
                    break;
                }

                unseen.Add(item);
                if (unseen.Count >= limit)
                    break;
            }

            // The stored post is no longer present in the feed (feed rotated or republished):
            // report only the newest post instead of dumping the whole feed as "new".
            if (!reachedAnnouncedPost && unseen.Count >= items.Count)
            {
                return new List<RssFeedItem> { items[0] };
            }

            return unseen;
        }

        private static string GetItemId(RssFeedItem item)
        {
            return !string.IsNullOrWhiteSpace(item.Guid) ? item.Guid : item.Link;
        }

        private static bool IsSamePost(RssFeedItem item, string postId)
        {
            if (string.IsNullOrWhiteSpace(postId))
                return false;

            return string.Equals(postId, GetItemId(item), StringComparison.OrdinalIgnoreCase)
                || string.Equals(postId, item.Link, StringComparison.OrdinalIgnoreCase)
                || string.Equals(postId, item.Guid, StringComparison.OrdinalIgnoreCase);
        }

        private static readonly object _cacheLock = new();
        private static List<RssFeedItem>? _cachedFeed;
        private static DateTime _cacheTimeUtc = DateTime.MinValue;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        public static async Task<List<RssFeedItem>> FetchFeedDirectAsync(CancellationToken cancellationToken = default)
        {
            lock (_cacheLock)
            {
                if (_cachedFeed != null && (DateTime.UtcNow - _cacheTimeUtc) < CacheDuration)
                {
                    return new List<RssFeedItem>(_cachedFeed);
                }
            }

            var result = new List<RssFeedItem>();

            // Fast offline check - if offline, return cached feed or empty list immediately without waiting
            try
            {
                if (Connectivity.Current.NetworkAccess == NetworkAccess.None)
                {
                    lock (_cacheLock)
                    {
                        return _cachedFeed != null ? new List<RssFeedItem>(_cachedFeed) : result;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RssFeedService] Connectivity check warning: {ex.Message}");
            }
            try
            {
                using var handler = new HttpClientHandler
                {
                    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
                };
                using var client = new HttpClient(handler) { Timeout = DefaultTimeout };
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", Constants.Network.AppUserAgent);

                var xmlString = await client.GetStringAsync(Constants.Urls.RssFeed, cancellationToken);
                if (string.IsNullOrWhiteSpace(xmlString))
                    return result;

                var doc = XDocument.Parse(xmlString);
                var items = doc.Descendants("item");

                XNamespace mediaNs = "http://search.yahoo.com/mrss/";

                foreach (var item in items)
                {
                    var title = item.Element("title")?.Value?.Trim() ?? string.Empty;
                    var link = item.Element("link")?.Value?.Trim() ?? string.Empty;
                    var desc = item.Element("description")?.Value?.Trim() ?? string.Empty;
                    var guid = item.Element("guid")?.Value?.Trim();
                    var category = item.Element("category")?.Value?.Trim();
                    var pubDateStr = item.Element("pubDate")?.Value?.Trim();

                    DateTime? pubDate = null;
                    if (!string.IsNullOrWhiteSpace(pubDateStr) &&
                        DateTime.TryParse(pubDateStr, System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out var parsedDate))
                    {
                        pubDate = parsedDate;
                    }

                    // Extract image URL from media:content or enclosure
                    string? imageUrl = item.Element(mediaNs + "content")?.Attribute("url")?.Value?.Trim()
                        ?? item.Element("enclosure")?.Attribute("url")?.Value?.Trim();

                    if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(link))
                    {
                        result.Add(new RssFeedItem
                        {
                            Title = title,
                            Link = link,
                            Description = desc,
                            PublishDate = pubDate,
                            ImageUrl = imageUrl,
                            Category = category,
                            Guid = !string.IsNullOrWhiteSpace(guid) ? guid : link
                        });
                    }
                }

                if (result.Count > 0)
                {
                    lock (_cacheLock)
                    {
                        _cachedFeed = new List<RssFeedItem>(result);
                        _cacheTimeUtc = DateTime.UtcNow;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RssFeedService] Error fetching RSS feed: {ex.Message}");
                lock (_cacheLock)
                {
                    if (_cachedFeed != null)
                        return new List<RssFeedItem>(_cachedFeed);
                }
            }

            return result;
        }
    }
}
