using Android.App;
using Android.Content;
using Android.Database;
using Android.OS;
using KopolArt.Mobile.Services;
using Uri = Android.Net.Uri;

namespace KopolArt.Mobile.Platforms.Android.Services
{
    public class AndroidUpdateDownloader : IUpdateDownloader
    {
        private const string ApkFileName = "app-update.apk";
        private const string NotificationTitle = "بروزرسانی کپل‌آرت";
        private const string NotificationDescription = "در حال دریافت نسخه جدید برنامه...";

        public Task<string> DownloadUpdateAsync(
            string url,
            Action<double> progressCallback,
            CancellationToken cancellationToken)
        {
            return DownloadUpdateAsync(
                url,
                (progress, bytesRead, totalBytes) => progressCallback(progress),
                cancellationToken);
        }

        public async Task<string> DownloadUpdateAsync(
            string url,
            Action<double, long, long> progressCallback,
            CancellationToken cancellationToken)
        {
            var context = global::Android.App.Application.Context;
            var externalDownloadsDir = context.GetExternalFilesDir(global::Android.OS.Environment.DirectoryDownloads)
                                      ?? context.CacheDir;
            if (externalDownloadsDir == null)
            {
                throw new InvalidOperationException("پوشه ذخیره‌سازی فایل‌ها در دسترس نیست.");
            }

            if (!externalDownloadsDir.Exists())
            {
                externalDownloadsDir.Mkdirs();
            }

            var destinationFile = new Java.IO.File(externalDownloadsDir, ApkFileName);
            if (destinationFile.Exists())
            {
                try
                {
                    destinationFile.Delete();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AndroidUpdateDownloader] Error deleting old APK: {ex.Message}");
                }
            }

            // Ensure notification permission is requested on Android 13+ (API 33+)
            MainActivity.RequestNotificationPermission(granted =>
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidUpdateDownloader] Notification permission: {granted}");
            });

            // Primary: In-app HttpClient streaming download with real-time byte tracking and notification
            try
            {
                return await DownloadViaHttpClientWithNotificationAsync(
                    context,
                    url,
                    destinationFile,
                    progressCallback,
                    cancellationToken);
            }
            catch (System.OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidUpdateDownloader] HttpClient error, attempting DownloadManager fallback: {ex.Message}");
            }

            // Secondary fallback: Android DownloadManager
            try
            {
                var downloadManager = (DownloadManager?)context.GetSystemService(Context.DownloadService);
                if (downloadManager != null)
                {
                    return await DownloadViaDownloadManagerAsync(
                        downloadManager,
                        url,
                        destinationFile,
                        progressCallback,
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidUpdateDownloader] DownloadManager fallback error: {ex.Message}");
            }

            throw new InvalidOperationException("امکان دانلود به‌روزرسانی وجود ندارد.");
        }

        private static async Task<string> DownloadViaDownloadManagerAsync(
            DownloadManager downloadManager,
            string url,
            Java.IO.File destinationFile,
            Action<double, long, long> progressCallback,
            CancellationToken cancellationToken)
        {
            var request = new DownloadManager.Request(Uri.Parse(url));
            request.SetTitle(NotificationTitle);
            request.SetDescription(NotificationDescription);
            request.SetMimeType("application/vnd.android.package-archive");
            request.SetNotificationVisibility(DownloadVisibility.VisibleNotifyCompleted);
            request.SetDestinationUri(Uri.FromFile(destinationFile));
            request.SetAllowedOverMetered(true);
            request.SetAllowedOverRoaming(true);

            long downloadId = downloadManager.Enqueue(request);

            try
            {
                var query = new DownloadManager.Query();
                query.SetFilterById(downloadId);

                bool isDone = false;
                while (!isDone)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using (ICursor? cursor = downloadManager.InvokeQuery(query))
                    {
                        if (cursor != null && cursor.MoveToFirst())
                        {
                            int statusIndex = cursor.GetColumnIndex(DownloadManager.ColumnStatus);
                            int downloadedIndex = cursor.GetColumnIndex(DownloadManager.ColumnBytesDownloadedSoFar);
                            int totalIndex = cursor.GetColumnIndex(DownloadManager.ColumnTotalSizeBytes);
                            int reasonIndex = cursor.GetColumnIndex(DownloadManager.ColumnReason);

                            int status = statusIndex >= 0 ? cursor.GetInt(statusIndex) : -1;
                            long downloadedBytes = downloadedIndex >= 0 ? cursor.GetLong(downloadedIndex) : 0;
                            long totalBytes = totalIndex >= 0 ? cursor.GetLong(totalIndex) : -1;

                            if (totalBytes > 0)
                            {
                                double progress = Math.Clamp((double)downloadedBytes / totalBytes, 0.0, 1.0);
                                progressCallback(progress, downloadedBytes, totalBytes);
                            }
                            else
                            {
                                progressCallback(-1, downloadedBytes, totalBytes);
                            }

                            if (status == (int)DownloadStatus.Successful)
                            {
                                progressCallback(1.0, downloadedBytes, totalBytes);
                                isDone = true;
                                break;
                            }
                            else if (status == (int)DownloadStatus.Failed)
                            {
                                int reason = reasonIndex >= 0 ? cursor.GetInt(reasonIndex) : -1;
                                throw new InvalidOperationException($"دانلود به‌روزرسانی توسط سیستم با خطا مواجه شد. کد خطا: {reason}");
                            }
                        }
                    }

                    await Task.Delay(300, cancellationToken);
                }

                if (!destinationFile.Exists() || destinationFile.Length() == 0)
                {
                    throw new FileNotFoundException("فایل به‌روزرسانی یافت نشد یا حجم آن صفر است.");
                }

                return destinationFile.AbsolutePath;
            }
            catch (System.OperationCanceledException)
            {
                try
                {
                    downloadManager.Remove(downloadId);
                    if (destinationFile.Exists())
                    {
                        destinationFile.Delete();
                    }
                }
                catch { /* ignored */ }
                throw;
            }
            catch (Exception)
            {
                try
                {
                    downloadManager.Remove(downloadId);
                    if (destinationFile.Exists())
                    {
                        destinationFile.Delete();
                    }
                }
                catch { /* ignored */ }
                throw;
            }
        }

        private static async Task<string> DownloadViaHttpClientWithNotificationAsync(
            Context context,
            string url,
            Java.IO.File destinationFile,
            Action<double, long, long> progressCallback,
            CancellationToken cancellationToken)
        {
            var notifService = new AndroidDownloadNotificationService(context);
            int notifId = 8899;

            PowerManager? powerManager = (PowerManager?)context.GetSystemService(Context.PowerService);
            PowerManager.WakeLock? wakeLock = null;
            try
            {
                wakeLock = powerManager?.NewWakeLock(WakeLockFlags.Partial, "KopolArt:UpdateDownloadWakeLock");
                wakeLock?.Acquire((long)TimeSpan.FromMinutes(15).TotalMilliseconds);
            }
            catch { /* ignored */ }

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", Constants.Network.AppUserAgent);

            notifService.ShowProgressNotification(notifId, NotificationTitle, -1);

            try
            {
                using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                long totalBytes = response.Content.Headers.ContentLength ?? -1L;

                await using (var fileStream = new FileStream(destinationFile.AbsolutePath, FileMode.Create, FileAccess.Write, FileShare.None, 32768, true))
                await using (var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken))
                {
                    var buffer = new byte[32768];
                    long totalRead = 0;
                    int lastReportedPercent = -1;
                    long lastCallbackTicks = 0;
                    int read;

                    // Initial progress report
                    progressCallback(totalBytes > 0 ? 0.0 : -1.0, 0, totalBytes);

                    while ((read = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                        totalRead += read;

                        long nowTicks = System.Environment.TickCount64;
                        bool shouldReportUi = (nowTicks - lastCallbackTicks >= 50) || (totalBytes > 0 && totalRead == totalBytes);

                        if (totalBytes > 0)
                        {
                            int percent = (int)(totalRead * 100L / totalBytes);
                            double progress = Math.Clamp((double)totalRead / totalBytes, 0.0, 1.0);

                            if (shouldReportUi)
                            {
                                lastCallbackTicks = nowTicks;
                                progressCallback(progress, totalRead, totalBytes);
                            }

                            if (percent != lastReportedPercent)
                            {
                                lastReportedPercent = percent;
                                notifService.ShowProgressNotification(notifId, NotificationTitle, percent);
                            }
                        }
                        else
                        {
                            if (shouldReportUi)
                            {
                                lastCallbackTicks = nowTicks;
                                progressCallback(-1, totalRead, -1);
                            }
                        }
                    }

                    // Final progress report
                    if (totalBytes > 0)
                    {
                        progressCallback(1.0, totalRead, totalBytes);
                    }
                    else
                    {
                        progressCallback(-1, totalRead, -1);
                    }
                }

                if (!destinationFile.Exists() || destinationFile.Length() == 0)
                {
                    throw new FileNotFoundException("فایل دانلود شده یافت نشد یا حجم آن صفر است.");
                }

                notifService.ShowCompleteNotification(notifId, NotificationTitle, destinationFile.AbsolutePath, "application/vnd.android.package-archive");
                return destinationFile.AbsolutePath;
            }
            catch (System.OperationCanceledException)
            {
                notifService.CancelNotification(notifId);
                if (destinationFile.Exists())
                {
                    try { destinationFile.Delete(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[AndroidUpdateDownloader] Failed to delete partial download file: {ex.Message}"); }
                }
                throw;
            }
            catch (Exception)
            {
                notifService.CancelNotification(notifId);
                if (destinationFile.Exists())
                {
                    try { destinationFile.Delete(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[AndroidUpdateDownloader] Failed to delete partial download file: {ex.Message}"); }
                }
                throw;
            }
            finally
            {
                try
                {
                    if (wakeLock?.IsHeld == true)
                    {
                        wakeLock.Release();
                    }
                }
                catch { /* ignored */ }
            }
        }
    }
}
