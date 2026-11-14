namespace KopolArt.Mobile.Models
{
    /// <summary>
    /// Normalized crop rectangle (0..1) inside the source image.
    /// The platform service converts it to pixel coordinates when decoding.
    /// </summary>
    public readonly record struct WallpaperCrop(double X, double Y, double Width, double Height)
    {
        public static WallpaperCrop Full => new(0, 0, 1, 1);
    }
}
