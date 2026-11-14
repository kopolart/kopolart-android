namespace KopolArt.Mobile.Helpers
{
    public static class CategoryBadgeHelper
    {
        public const string CategoryAll = "همه";
        public const string CategoryWallpapers = "والپیپر";
        public const string CategoryPosts = "پست";
        public const string CategoryColors = "رنگ";
        public const string CategoryTools = "ابزار";
        public const string CategoryOther = "غیره";

        public static readonly string[] AllFilterCategories =
        [
            CategoryAll,
            CategoryWallpapers,
            CategoryPosts,
            CategoryColors,
            CategoryTools,
            CategoryOther
        ];

        public static readonly string[] DownloadFilterCategories =
        [
            CategoryAll,
            CategoryWallpapers,
            CategoryPosts,
            CategoryColors,
            CategoryOther
        ];

        public static string GetDownloadCategory(string? url, string? fileName = null)
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                var lowerUrl = url.ToLowerInvariant();

                // 1. Colors (رنگ)
                if (lowerUrl.Contains("/color-swatches/") ||
                    lowerUrl.Contains("/solid-color-wallpapers/") ||
                    lowerUrl.Contains("images/palettes/") ||
                    lowerUrl.Contains("/color-palettes-wallpaper/") ||
                    lowerUrl.Contains("/color-palette-image/") ||
                    lowerUrl.Contains("/color-cards/") ||
                    lowerUrl.Contains("/solid-color-custom-wallpaper/") ||
                    lowerUrl.Contains("/custom-color-image/") ||
                    lowerUrl.Contains("/custom-palette-image/") ||
                    lowerUrl.Contains("/colors/") ||
                    lowerUrl.Contains("/color-palettes/"))
                {
                    return CategoryColors;
                }

                // 2. Wallpapers (والپیپر)
                if (lowerUrl.Contains("/wallpapers/previews/") ||
                    lowerUrl.Contains("/images/wallpapers/"))
                {
                    return CategoryWallpapers;
                }

                // 3. Posts (پست)
                if (lowerUrl.Contains("/images/posts/downloads/"))
                {
                    return CategoryPosts;
                }
            }

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                var lowerFile = fileName.ToLowerInvariant();

                if (lowerFile.Contains("color-swatches") ||
                    lowerFile.Contains("solid-color-wallpaper") ||
                    lowerFile.Contains("palette") ||
                    lowerFile.Contains("color-cards") ||
                    lowerFile.Contains("custom-color") ||
                    lowerFile.Contains("custom-palette"))
                {
                    return CategoryColors;
                }

                if (lowerFile.Contains("wallpaper"))
                {
                    return CategoryWallpapers;
                }

                if (lowerFile.Contains("post"))
                {
                    return CategoryPosts;
                }
            }

            return CategoryOther;
        }

        public static string GetCategory(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return CategoryOther;
            }

            var lowerUrl = url.ToLowerInvariant();

            if (lowerUrl.Contains("/colors/") || lowerUrl.Contains("/color-palettes/"))
            {
                return CategoryColors;
            }

            if (lowerUrl.Contains("/posts/"))
            {
                return CategoryPosts;
            }

            if (lowerUrl.Contains("/tools/"))
            {
                return CategoryTools;
            }

            if (lowerUrl.Contains("/wallpapers/"))
            {
                return CategoryWallpapers;
            }

            return CategoryOther;
        }

        public static Color GetBadgeTextColor(string category, bool isDark)
        {
            return category switch
            {
                CategoryWallpapers => isDark ? Color.FromArgb("#C084FC") : Color.FromArgb("#9333EA"),
                CategoryPosts => isDark ? Color.FromArgb("#60A5FA") : Color.FromArgb("#249CF0"),
                CategoryColors => isDark ? Color.FromArgb("#FBBF24") : Color.FromArgb("#D97706"),
                CategoryTools => isDark ? Color.FromArgb("#34D399") : Color.FromArgb("#059669"),
                _ => isDark ? Color.FromArgb("#94A3B8") : Color.FromArgb("#535E7B"),
            };
        }

        public static Color GetBadgeBackgroundColor(string category, bool isDark)
        {
            return category switch
            {
                CategoryWallpapers => isDark ? Color.FromArgb("#309333EA") : Color.FromArgb("#169333EA"),
                CategoryPosts => isDark ? Color.FromArgb("#30249CF0") : Color.FromArgb("#16249CF0"),
                CategoryColors => isDark ? Color.FromArgb("#30D97706") : Color.FromArgb("#16D97706"),
                CategoryTools => isDark ? Color.FromArgb("#30059669") : Color.FromArgb("#16059669"),
                _ => isDark ? Color.FromArgb("#30626F8F") : Color.FromArgb("#16535E7B"),
            };
        }

        public static Color GetBadgeBorderColor(string category, bool isDark)
        {
            return category switch
            {
                CategoryWallpapers => isDark ? Color.FromArgb("#559333EA") : Color.FromArgb("#339333EA"),
                CategoryPosts => isDark ? Color.FromArgb("#55249CF0") : Color.FromArgb("#33249CF0"),
                CategoryColors => isDark ? Color.FromArgb("#55D97706") : Color.FromArgb("#33D97706"),
                CategoryTools => isDark ? Color.FromArgb("#55059669") : Color.FromArgb("#33059669"),
                _ => isDark ? Color.FromArgb("#55626F8F") : Color.FromArgb("#33535E7B"),
            };
        }
    }
}
