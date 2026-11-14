using Android.Content;
using Android.Widget;
using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Models;
using KopolArt.Mobile.Services;

namespace KopolArt.Mobile.Platforms.Android
{
    public static class ImageContextMenuHelper
    {
        public static void ShowImageContextMenu(Context context, string imageUrl, CustomDownloadListener? downloadListener)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var alertService = IPlatformApplication.Current?.Services.GetService<ICustomAlertService>()
                                    ?? Application.Current?.Handler?.MauiContext?.Services.GetService<ICustomAlertService>();

                    var options = new List<AlertOptionItem>
                    {
                        new("ذخیره تصویر", KopolArtFaIcons.DownloadCloud, "ذخیره تصویر"),
                        new("اشتراک‌گذاری", KopolArtFaIcons.ShareSquared, "اشتراک‌گذاری تصویر"),
                        new("کپی پیوند", KopolArtFaIcons.Copy, "کپی پیوند تصویر")
                    };

                    string? action = null;
                    if (alertService != null)
                    {
                        action = await alertService.ShowGridOptionsAsync(
                            "عملیات تصویر",
                            "عملیات مورد نظر برای این تصویر را انتخاب نمایید:",
                            options,
                            "انصراف",
                            AlertType.Question,
                            columns: 2);
                    }
                    else
                    {
                        var page = Shell.Current?.CurrentPage ?? Application.Current?.Windows.FirstOrDefault()?.Page;
                        if (page != null)
                        {
                            action = await page.DisplayActionSheetAsync(
                                "عملیات تصویر",
                                "انصراف",
                                null,
                                options.Select(o => o.ReturnValue ?? o.Text).ToArray());
                        }
                    }

                    if (action == "ذخیره تصویر")
                    {
                        SaveImage(context, imageUrl, downloadListener);
                    }
                    else if (action == "اشتراک‌گذاری تصویر")
                    {
                        await ShareImageAsync(imageUrl);
                    }
                    else if (action == "کپی پیوند تصویر")
                    {
                        await Clipboard.Default.SetTextAsync(imageUrl);
                        Toast.MakeText(context, "پیوند تصویر کپی شد", ToastLength.Short)?.Show();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ImageContextMenuHelper] Error showing context menu: {ex.Message}");
                }
            });
        }

        private static void SaveImage(Context context, string imageUrl, CustomDownloadListener? downloadListener)
        {
            try
            {
                if (downloadListener != null)
                {
                    downloadListener.OnDownloadStart(imageUrl, null, null, null, 0);
                }
                else
                {
                    Toast.MakeText(context, "دانلود تصویر در دسترس نیست", ToastLength.Short)?.Show();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ImageContextMenuHelper] Save image error: {ex.Message}");
                Toast.MakeText(context, "خطا در شروع ذخیره تصویر", ToastLength.Short)?.Show();
            }
        }

        private static async Task ShareImageAsync(string imageUrl)
        {
            try
            {
                if (imageUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    int commaIndex = imageUrl.IndexOf(',');
                    if (commaIndex > 0)
                    {
                        var data = imageUrl[(commaIndex + 1)..];
                        byte[] bytes = Convert.FromBase64String(data);
                        string ext = ".jpg";
                        if (imageUrl.Contains("image/png", StringComparison.OrdinalIgnoreCase)) ext = ".png";
                        else if (imageUrl.Contains("image/webp", StringComparison.OrdinalIgnoreCase)) ext = ".webp";
                        else if (imageUrl.Contains("image/gif", StringComparison.OrdinalIgnoreCase)) ext = ".gif";

                        string tempFile = Path.Combine(FileSystem.CacheDirectory, $"share_{Guid.NewGuid():N}{ext}");
                        await File.WriteAllBytesAsync(tempFile, bytes);

                        await Share.Default.RequestAsync(new ShareFileRequest
                        {
                            Title = "اشتراک‌گذاری تصویر",
                            File = new ShareFile(tempFile)
                        });
                        return;
                    }
                }
                else if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https"))
                {
                    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                    var bytes = await httpClient.GetByteArrayAsync(uri);
                    string extension = Path.GetExtension(uri.AbsolutePath);
                    if (string.IsNullOrWhiteSpace(extension) || extension.Length > 5)
                    {
                        extension = ".jpg";
                    }
                    string tempFile = Path.Combine(FileSystem.CacheDirectory, $"share_{Guid.NewGuid():N}{extension}");
                    await File.WriteAllBytesAsync(tempFile, bytes);

                    await Share.Default.RequestAsync(new ShareFileRequest
                    {
                        Title = "اشتراک‌گذاری تصویر",
                        File = new ShareFile(tempFile)
                    });
                    return;
                }

                // Fallback to sharing URL
                await Share.Default.RequestAsync(new ShareTextRequest
                {
                    Title = "اشتراک‌گذاری تصویر",
                    Uri = imageUrl
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ImageContextMenuHelper] Share image error: {ex.Message}");
                try
                {
                    await Share.Default.RequestAsync(new ShareTextRequest
                    {
                        Title = "اشتراک‌گذاری تصویر",
                        Uri = imageUrl
                    });
                }
                catch { }
            }
        }
    }
}
