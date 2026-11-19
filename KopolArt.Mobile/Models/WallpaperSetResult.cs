namespace KopolArt.Mobile.Models
{
    public enum WallpaperSetStatus
    {
        Success,
        HomeScreenOnly,
        NotSupported,
        NotAllowed,
        FileMissing,
        DecodeFailed,
        Unknown
    }

    /// <summary>
    /// Outcome of a wallpaper apply request. <see cref="WallpaperSetStatus.HomeScreenOnly"/> is a partial
    /// success reported when the device refused the lock-screen change.
    /// </summary>
    public sealed class WallpaperSetResult
    {
        public WallpaperSetStatus Status { get; init; } = WallpaperSetStatus.Unknown;
    }
}
