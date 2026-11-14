using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Helpers
{
    /// <summary>
    /// Decides which downloads may offer the "set as wallpaper" action.
    /// Kept separate from the ViewModel so the rules stay testable and reusable.
    /// </summary>
    public static class WallpaperEligibility
    {
        public const int MinimumImageWidth = 720;
        public const int MinimumImageHeight = 1080;

        private const double MinimumPortraitRatio = 1.05;

        public static bool IsWallpaperCandidate(
            string? downloadUrl,
            string? fileName,
            string? mimeType,
            bool fileExists)
        {
            if (!fileExists)
                return false;

            if (CategoryBadgeHelper.GetDownloadCategory(downloadUrl, fileName) != CategoryBadgeHelper.CategoryWallpapers)
                return false;

            return IsSupportedImage(fileName, mimeType);
        }

        public static bool IsSupportedImage(string? fileName, string? mimeType)
        {
            string extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();

            // Animated and vector formats are not usable as static wallpapers.
            if (extension is ".gif" or ".svg")
                return false;

            if (!string.IsNullOrWhiteSpace(mimeType))
            {
                string mime = mimeType.ToLowerInvariant();
                if (mime is "image/gif" or "image/svg+xml")
                    return false;

                if (mime.StartsWith("image/", StringComparison.Ordinal))
                    return true;
            }

            return extension is ".jpg" or ".jpeg" or ".png" or ".webp" or ".heic" or ".heif" or ".bmp";
        }

        public static bool IsUsablePortrait(WallpaperImageInfo? info)
        {
            if (info == null)
                return false;

            if (info.Width < MinimumImageWidth || info.Height < MinimumImageHeight)
                return false;

            return info.Height >= info.Width * MinimumPortraitRatio;
        }
    }
}
