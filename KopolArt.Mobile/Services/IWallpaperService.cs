using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services
{
    public interface IWallpaperService
    {
        bool IsSupported { get; }

        Task<WallpaperImageInfo?> InspectAsync(string localPath);

        Task<WallpaperPreview?> CreatePreviewAsync(string localPath);

        Task<WallpaperSetResult> SetAsync(string localPath, WallpaperCrop crop, WallpaperTarget target);

        void DeletePreviewFile(string? previewFilePath);
    }
}
