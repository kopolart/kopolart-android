namespace KopolArt.Mobile.Models
{
    /// <summary>
    /// Prepared, screen-aspect preview of a wallpaper candidate.
    /// <see cref="BaseCrop"/> maps the preview back onto the source image so the
    /// user's zoom/pan can be converted into a normalized crop for the platform.
    /// </summary>
    public sealed class WallpaperPreview
    {
        public string PreviewFilePath { get; init; } = string.Empty;
        public int SourceWidth { get; init; }
        public int SourceHeight { get; init; }
        public WallpaperCrop BaseCrop { get; init; } = WallpaperCrop.Full;

        public double FrameAspect => SourceWidth <= 0 || SourceHeight <= 0 || BaseCrop.Width <= 0 || BaseCrop.Height <= 0
            ? 16.0 / 9.0
            : (SourceHeight * BaseCrop.Height) / (SourceWidth * BaseCrop.Width);
    }
}
