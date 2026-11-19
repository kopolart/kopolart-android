using System.Diagnostics.CodeAnalysis;
using KopolArt.Mobile.Models;
using KopolArt.Mobile.Services;
using KopolArt.Mobile.Views;

namespace KopolArt.Mobile.Helpers
{
    /// <summary>
    /// Coordinates saving WebView pages as self-contained offline HTML archives.
    /// </summary>
    public static class BrowserOfflineSaveCoordinator
    {
        [SuppressMessage(
            "Reliability",
            "CA2000:Dispose objects before losing scope",
            Justification = "The native WebView owns the callback and invokes it asynchronously; disposing it here would invalidate the callback.")]
        public static void SavePageForOffline(
            WebView? webBrowser,
            string url,
            string title,
            IOfflinePageService? offlineService,
            ICustomAlertService? customAlertService)
        {
            if (string.IsNullOrWhiteSpace(url) ||
                url.StartsWith("about:", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

#if ANDROID
            if (webBrowser?.Handler?.PlatformView is Android.Webkit.WebView nativeWebView)
            {
                try
                {
                    Android.Widget.Toast.MakeText(Android.App.Application.Context, "در حال ذخیره‌سازی صفحه آفلاین...", Android.Widget.ToastLength.Short)?.Show();
                    if (offlineService == null)
                    {
                        return;
                    }

                    string pageId = Guid.NewGuid().ToString("N");
                    string offlineDir = NavigationHelper.OfflinePagesDirectory;
                    Directory.CreateDirectory(offlineDir);
                    string mhtPath = Path.Combine(offlineDir, $"{pageId}.mht");
                    string htmlPath = Path.Combine(offlineDir, $"{pageId}.html");

                    nativeWebView.SaveWebArchive(mhtPath, false, new SaveWebArchiveValueCallback(savedPath =>
                    {
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            try
                            {
                                if (!string.IsNullOrWhiteSpace(savedPath) && File.Exists(savedPath))
                                {
                                    bool converted = await OfflineHtmlConverter.ConvertMhtmlToHtmlAsync(savedPath, htmlPath, url, title);
                                    string finalPath = (converted && File.Exists(htmlPath)) ? htmlPath : savedPath;

                                    long fileSizeBytes = 0;
                                    try
                                    {
                                        fileSizeBytes = new FileInfo(finalPath).Length;
                                    }
                                    catch { }

                                    var item = new OfflinePageItem
                                    {
                                        Id = pageId,
                                        Title = string.IsNullOrWhiteSpace(title) ? "صفحه آفلاین" : title,
                                        OriginalUrl = url,
                                        FilePath = finalPath,
                                        SavedAtUtc = DateTime.UtcNow,
                                        FileSizeBytes = fileSizeBytes
                                    };

                                    await offlineService.AddOfflinePageAsync(item);

                                    bool openList = false;
                                    if (customAlertService != null)
                                    {
                                        openList = await customAlertService.ShowConfirmAsync(
                                            "ذخیره صفحه آفلاین",
                                            "این صفحه با موفقیت برای مطالعه آفلاین در حافظه دستگاه ذخیره شد.",
                                            "مشاهده صفحات ذخیره‌شده",
                                            "ادامه مرور",
                                            AlertType.Success);
                                    }

                                    if (openList && Shell.Current != null)
                                    {
                                        await Shell.Current.GoToAsync(nameof(OfflinePagesPage));
                                    }
                                }
                                else
                                {
                                    if (customAlertService != null)
                                    {
                                        await customAlertService.ShowAlertAsync(
                                            "خطا در ذخیره‌سازی",
                                            "امکان ذخیره فایل صفحه آفلاین در این لحظه وجود نداشت.",
                                            "متوجه شدم",
                                            AlertType.Error);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[BrowserOfflineSaveCoordinator] Post-save error: {ex.Message}");
                            }
                        });
                    }));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[BrowserOfflineSaveCoordinator] SaveWebArchive error: {ex.Message}");
                }
            }
#endif
        }
    }
}
