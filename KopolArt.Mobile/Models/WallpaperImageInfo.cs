namespace KopolArt.Mobile.Models
{
    /// <summary>
    /// Pixel dimensions and detected MIME type of a downloaded image.
    /// </summary>
    public sealed class WallpaperImageInfo
    {
        public int Width { get; init; }
        public int Height { get; init; }
        public string MimeType { get; init; } = string.Empty;
    }
}
