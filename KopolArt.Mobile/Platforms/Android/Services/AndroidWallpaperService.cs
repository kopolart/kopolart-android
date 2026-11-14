using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Android.Graphics;
using KopolArt.Mobile.Models;
using KopolArt.Mobile.Services;

namespace KopolArt.Mobile.Platforms.Android.Services
{
    /// <summary>
    /// Android implementation of <see cref="IWallpaperService"/>.
    /// All heavy bitmap work runs on a background thread; the original downloaded
    /// file is never modified and its bytes are only ever read.
    /// </summary>
    public sealed class AndroidWallpaperService : IWallpaperService
    {
        private const int MaxPreviewWidth = 1080;
        private const int MaxPreviewQuality = 90;
        private const int MaxSampleSize = 8;

        private static global::Android.Content.Context Context =>
            global::Android.App.Application.Context;

        public bool IsSupported
        {
            get
            {
                try
                {
                    var manager = global::Android.App.WallpaperManager.GetInstance(Context);
                    return manager?.IsWallpaperSupported == true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AndroidWallpaperService] IsSupported error: {ex.Message}");
                    return false;
                }
            }
        }

        public Task<WallpaperImageInfo?> InspectAsync(string localPath) =>
            Task.Run(() => ReadImageInfo(localPath));

        public Task<WallpaperPreview?> CreatePreviewAsync(string localPath) =>
            Task.Run(() => CreatePreview(localPath));

        public Task<WallpaperSetResult> SetAsync(string localPath, WallpaperCrop crop, WallpaperTarget target) =>
            Task.Run(() => ApplyWallpaper(localPath, crop, target));

        public void DeletePreviewFile(string? previewFilePath)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(previewFilePath) && File.Exists(previewFilePath))
                {
                    File.Delete(previewFilePath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AndroidWallpaperService] DeletePreviewFile error: {ex.Message}");
            }
        }

        private static WallpaperImageInfo? ReadImageInfo(string localPath)
        {
            try
            {
                using var stream = OpenRead(localPath);
                if (stream == null)
                    return null;

                using var options = new BitmapFactory.Options { InJustDecodeBounds = true };
                BitmapFactory.DecodeStream(stream, null, options);

                if (options.OutWidth <= 0 || options.OutHeight <= 0)
                    return null;

                // EXIF rotation decides the image the user actually sees, so
                // portrait checks and crop math must run against oriented dims.
                bool swapAxes = IsQuarterTurn(ReadExifOrientation(localPath));

                return new WallpaperImageInfo
                {
                    Width = swapAxes ? options.OutHeight : options.OutWidth,
                    Height = swapAxes ? options.OutWidth : options.OutHeight,
                    MimeType = options.OutMimeType ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AndroidWallpaperService] Inspect error: {ex.Message}");
                return null;
            }
        }

        private static WallpaperPreview? CreatePreview(string localPath)
        {
            Bitmap? decoded = null;
            Bitmap? cropped = null;
            Bitmap? scaled = null;

            try
            {
                var info = ReadImageInfo(localPath);
                if (info == null)
                    return null;

                var (screenWidth, screenHeight) = GetScreenSize();
                var baseCrop = ComputeCoverCrop(info.Width, info.Height, (double)screenHeight / screenWidth);

                int sampleSize = ComputeSampleSize(info.Width, MaxPreviewWidth);
                using var stream = OpenRead(localPath);
                if (stream == null)
                    return null;

                using var decodeOptions = new BitmapFactory.Options { InSampleSize = sampleSize };
                decoded = BitmapFactory.DecodeStream(stream, null, decodeOptions);
                if (decoded == null)
                    return null;

                decoded = OrientToExif(decoded, localPath);

                cropped = CropBitmap(decoded, baseCrop);
                if (cropped == null)
                    return null;

                int previewWidth = Math.Min(cropped.Width, MaxPreviewWidth);
                int previewHeight = Math.Max(1, (int)Math.Round(previewWidth * (double)cropped.Height / cropped.Width));
                scaled = Bitmap.CreateScaledBitmap(cropped, previewWidth, previewHeight, true);
                var outputBitmap = scaled ?? cropped;

                string previewPath = GetPreviewFilePath(localPath);
                using (var output = File.Create(previewPath))
                {
                    outputBitmap.Compress(Bitmap.CompressFormat.Jpeg!, MaxPreviewQuality, output);
                }

                return new WallpaperPreview
                {
                    PreviewFilePath = previewPath,
                    SourceWidth = info.Width,
                    SourceHeight = info.Height,
                    BaseCrop = baseCrop
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AndroidWallpaperService] CreatePreview error: {ex.Message}");
                return null;
            }
            finally
            {
                scaled?.Recycle();
                cropped?.Recycle();
                decoded?.Recycle();
            }
        }

        private static WallpaperSetResult ApplyWallpaper(string localPath, WallpaperCrop crop, WallpaperTarget target)
        {
            Bitmap? source = null;
            Bitmap? cropped = null;
            Bitmap? scaled = null;

            try
            {
                var info = ReadImageInfo(localPath);
                if (info == null)
                {
                    using var probe = OpenRead(localPath);
                    return Failed(probe == null ? WallpaperSetStatus.FileMissing : WallpaperSetStatus.DecodeFailed);
                }

                var manager = global::Android.App.WallpaperManager.GetInstance(Context);
                if (manager == null || !manager.IsWallpaperSupported)
                    return Failed(WallpaperSetStatus.NotSupported);

                if (!manager.IsSetWallpaperAllowed)
                    return Failed(WallpaperSetStatus.NotAllowed);

                var (screenWidth, screenHeight) = GetScreenSize();
                int sampleSize = ComputeCropSampleSize(info, crop, screenWidth, screenHeight);

                using var stream = OpenRead(localPath);
                if (stream == null)
                    return Failed(WallpaperSetStatus.FileMissing);

                using var decodeOptions = new BitmapFactory.Options { InSampleSize = sampleSize };
                source = BitmapFactory.DecodeStream(stream, null, decodeOptions);
                if (source == null)
                    return Failed(WallpaperSetStatus.DecodeFailed);

                source = OrientToExif(source, localPath);

                cropped = CropBitmap(source, crop);
                if (cropped == null)
                    return Failed(WallpaperSetStatus.DecodeFailed);

                int targetWidth = Math.Min(cropped.Width, Math.Max(screenWidth, 1080));
                int targetHeight = Math.Max(1, (int)Math.Round(targetWidth * (double)cropped.Height / cropped.Width));
                if (targetWidth != cropped.Width || targetHeight != cropped.Height)
                {
                    scaled = Bitmap.CreateScaledBitmap(cropped, targetWidth, targetHeight, true);
                }

                var bitmap = scaled ?? cropped;

                bool applied;
                try
                {
                    applied = manager.SetBitmap(bitmap, null, true, ToWallpaperFlags(target)) != 0;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AndroidWallpaperService] SetBitmap error: {ex}");
                    return Failed(WallpaperSetStatus.Unknown);
                }

                if (applied)
                    return new WallpaperSetResult { Status = WallpaperSetStatus.Success };

                // Some OEMs reject the lock-screen flag even though the home screen works.
                if (target != WallpaperTarget.HomeScreen)
                {
                    try
                    {
                        applied = manager.SetBitmap(bitmap, null, true, global::Android.App.WallpaperManagerFlags.System) != 0;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[AndroidWallpaperService] Home-screen fallback error: {ex.Message}");
                        applied = false;
                    }

                    if (applied)
                        return new WallpaperSetResult { Status = WallpaperSetStatus.HomeScreenOnly };
                }

                return Failed(WallpaperSetStatus.Unknown);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AndroidWallpaperService] ApplyWallpaper error: {ex}");
                return Failed(WallpaperSetStatus.Unknown);
            }
            finally
            {
                scaled?.Recycle();
                cropped?.Recycle();
                source?.Recycle();
            }
        }

        private static WallpaperSetResult Failed(WallpaperSetStatus status) => new() { Status = status };

        private static global::Android.App.WallpaperManagerFlags ToWallpaperFlags(WallpaperTarget target) => target switch
        {
            WallpaperTarget.LockScreen => global::Android.App.WallpaperManagerFlags.Lock,
            WallpaperTarget.Both => global::Android.App.WallpaperManagerFlags.System | global::Android.App.WallpaperManagerFlags.Lock,
            _ => global::Android.App.WallpaperManagerFlags.System
        };

        private static int ComputeCropSampleSize(WallpaperImageInfo info, WallpaperCrop crop, int screenWidth, int screenHeight)
        {
            double cropPixelWidth = info.Width * Math.Clamp(crop.Width, 0.01, 1.0);
            double cropPixelHeight = info.Height * Math.Clamp(crop.Height, 0.01, 1.0);
            double comfortableWidth = screenWidth * 1.5;
            double comfortableHeight = screenHeight * 1.5;

            int sampleSize = 1;
            while (sampleSize < MaxSampleSize
                   && cropPixelWidth / (sampleSize * 2.0) >= comfortableWidth
                   && cropPixelHeight / (sampleSize * 2.0) >= comfortableHeight)
            {
                sampleSize *= 2;
            }

            return sampleSize;
        }

        private static int ComputeSampleSize(int width, int maxWidth)
        {
            int sampleSize = 1;
            while (sampleSize < MaxSampleSize && width / (sampleSize * 2) >= maxWidth)
            {
                sampleSize *= 2;
            }

            return sampleSize;
        }

        private static WallpaperCrop ComputeCoverCrop(int width, int height, double targetAspect)
        {
            if (width <= 0 || height <= 0 || targetAspect <= 0)
                return WallpaperCrop.Full;

            double sourceAspect = (double)height / width;
            double cropWidth = 1.0;
            double cropHeight = 1.0;

            if (sourceAspect > targetAspect)
                cropHeight = targetAspect / sourceAspect;
            else
                cropWidth = sourceAspect / targetAspect;

            cropWidth = Math.Clamp(cropWidth, 0.01, 1.0);
            cropHeight = Math.Clamp(cropHeight, 0.01, 1.0);

            return new WallpaperCrop((1 - cropWidth) / 2, (1 - cropHeight) / 2, cropWidth, cropHeight);
        }

        private static Bitmap? CropBitmap(Bitmap source, WallpaperCrop crop)
        {
            int x = (int)Math.Floor(source.Width * Math.Clamp(crop.X, 0.0, 1.0));
            int y = (int)Math.Floor(source.Height * Math.Clamp(crop.Y, 0.0, 1.0));
            x = Math.Clamp(x, 0, Math.Max(0, source.Width - 1));
            y = Math.Clamp(y, 0, Math.Max(0, source.Height - 1));

            int width = (int)Math.Round(source.Width * Math.Clamp(crop.Width, 0.0, 1.0));
            int height = (int)Math.Round(source.Height * Math.Clamp(crop.Height, 0.0, 1.0));
            width = Math.Clamp(width, 1, source.Width - x);
            height = Math.Clamp(height, 1, source.Height - y);

            return Bitmap.CreateBitmap(source, x, y, width, height);
        }

        private static (int Width, int Height) GetScreenSize()
        {
            var metrics = Context.Resources?.DisplayMetrics;
            int width = metrics?.WidthPixels ?? 1080;
            int height = metrics?.HeightPixels ?? 1920;
            return (Math.Max(1, width), Math.Max(1, height));
        }

        private static string GetPreviewFilePath(string localPath)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(localPath));
            string fileName = $"wallpaper-preview-{Convert.ToHexString(hash, 0, 8).ToLowerInvariant()}.jpg";
            return System.IO.Path.Combine(FileSystem.CacheDirectory, fileName);
        }

        private static bool IsQuarterTurn(int orientation) => orientation is 5 or 6 or 7 or 8;

        private static int ReadExifOrientation(string localPath)
        {
            try
            {
                using var stream = OpenRead(localPath);
                if (stream == null)
                    return 1;

                using var exif = new global::Android.Media.ExifInterface(stream);
                return exif.GetAttributeInt(global::Android.Media.ExifInterface.TagOrientation, 1);
            }
            catch (Exception ex)
            {
                // Formats without EXIF support (or corrupted headers) keep their decoded pixels.
                Debug.WriteLine($"[AndroidWallpaperService] EXIF read error: {ex.Message}");
                return 1;
            }
        }

        private static Bitmap OrientToExif(Bitmap bitmap, string localPath)
        {
            int orientation = ReadExifOrientation(localPath);
            if (orientation is < 2 or > 8)
                return bitmap;

            try
            {
                var matrix = new Matrix();
                switch (orientation)
                {
                    case 2: matrix.SetScale(-1, 1); break;
                    case 3: matrix.SetRotate(180); break;
                    case 4: matrix.SetScale(1, -1); break;
                    case 5: matrix.SetRotate(90); matrix.PostScale(-1, 1); break;
                    case 6: matrix.SetRotate(90); break;
                    case 7: matrix.SetRotate(-90); matrix.PostScale(-1, 1); break;
                    case 8: matrix.SetRotate(-90); break;
                    default: return bitmap;
                }

                var oriented = Bitmap.CreateBitmap(bitmap, 0, 0, bitmap.Width, bitmap.Height, matrix, true);
                if (!ReferenceEquals(oriented, bitmap))
                    bitmap.Recycle();

                return oriented ?? bitmap;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AndroidWallpaperService] EXIF orient error: {ex.Message}");
                return bitmap;
            }
        }

        private static Stream? OpenRead(string localPath)
        {
            if (string.IsNullOrWhiteSpace(localPath))
                return null;

            try
            {
                if (localPath.StartsWith("content://", StringComparison.OrdinalIgnoreCase)
                    || localPath.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                {
                    var uri = global::Android.Net.Uri.Parse(localPath);
                    return uri == null ? null : Context.ContentResolver?.OpenInputStream(uri);
                }

                return File.Exists(localPath) ? File.OpenRead(localPath) : null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AndroidWallpaperService] OpenRead error: {ex.Message}");
                return null;
            }
        }
    }
}
