using Android.App;
using Android.Content;
using AndroidX.Core.App;
using AndroidFileProvider = AndroidX.Core.Content.FileProvider;
using Uri = Android.Net.Uri;

namespace KopolArt.Mobile.Platforms.Android.Services
{
    public class AndroidDownloadNotificationService
    {
        private readonly Context _context;
        private const string ChannelId = "download_channel";
        private const string ChannelName = "دانلود فایل‌ها";

        public AndroidDownloadNotificationService(Context context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            EnsureNotificationChannel();
        }

        private void EnsureNotificationChannel()
        {
            var channel = new NotificationChannel(
                ChannelId,
                ChannelName,
                NotificationImportance.Low)
            {
                Description = "نمایش پیشرفت دانلود فایل‌ها"
            };

            var manager = (NotificationManager?)_context.GetSystemService(Context.NotificationService);
            manager?.CreateNotificationChannel(channel);
        }

        public void ShowProgressNotification(int id, string fileName, int progress, bool isPaused = false, string? customTitle = null)
        {
            bool indeterminate = progress < 0;
            string title = !string.IsNullOrWhiteSpace(customTitle)
                ? customTitle
                : (isPaused ? "دانلود متوقف شد" : "در حال دانلود");

            string contentText = indeterminate
                ? "در حال شروع دانلود..."
                : (isPaused ? $"{fileName} (متوقف شد - {progress}٪)" : $"{fileName} ({progress}٪)");

#pragma warning disable CS8602
            NotificationCompat.Builder builder = new NotificationCompat.Builder(_context, ChannelId)
                .SetContentTitle(title)
                .SetContentText(contentText)
                .SetSmallIcon(Resource.Drawable.ic_stat_download)
                .SetOnlyAlertOnce(true)
                .SetOngoing(true)
                .SetPriority(NotificationCompat.PriorityLow)!;
#pragma warning restore CS8602

            if (indeterminate)
                builder.SetProgress(0, 0, true);
            else
                builder.SetProgress(100, Math.Clamp(progress, 0, 100), false);

            // Action: Pause / Resume
            try
            {
                var actionIntent = new Intent(_context, typeof(DownloadActionReceiver));
                actionIntent.SetAction(isPaused ? DownloadActionReceiver.ActionResume : DownloadActionReceiver.ActionPause);
                actionIntent.PutExtra(DownloadActionReceiver.ExtraDownloadId, id);

                var actionPendingIntent = PendingIntent.GetBroadcast(
                    _context,
                    id,
                    actionIntent,
                    PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

                string actionLabel = isPaused ? "ادامه" : "توقف";
                builder.AddAction(isPaused ? Resource.Drawable.ic_stat_download : Resource.Drawable.ic_stat_download_done, actionLabel, actionPendingIntent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidDownloadNotificationService] Action button error: {ex.Message}");
            }

            // Action: Cancel
            try
            {
                var cancelIntent = new Intent(_context, typeof(DownloadActionReceiver));
                cancelIntent.SetAction(DownloadActionReceiver.ActionCancel);
                cancelIntent.PutExtra(DownloadActionReceiver.ExtraDownloadId, id);

                var cancelPendingIntent = PendingIntent.GetBroadcast(
                    _context,
                    id + 100000,
                    cancelIntent,
                    PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

                builder.AddAction(Resource.Drawable.ic_stat_content, "لغو", cancelPendingIntent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidDownloadNotificationService] Cancel action error: {ex.Message}");
            }

            NotificationManagerCompat.From(_context)?.Notify(id, builder.Build());
        }

        public void ShowCompleteNotification(
            int id,
            string fileName,
            string filePathOrUri,
            string mimeType,
            string? customTitle = null)
        {
            CancelNotification(id);

            var intent = CreateViewIntent(filePathOrUri, mimeType);
            PendingIntent? pendingIntent = null;

            if (intent != null)
            {
                pendingIntent = PendingIntent.GetActivity(
                    _context,
                    id,
                    intent,
                    PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
            }

            string title = !string.IsNullOrWhiteSpace(customTitle) ? customTitle : "دانلود تکمیل شد";

#pragma warning disable CS8602
            NotificationCompat.Builder builder = new NotificationCompat.Builder(_context, ChannelId)
                .SetContentTitle(title)
                .SetContentText($"{fileName} با موفقیت دریافت شد.")
                .SetSmallIcon(Resource.Drawable.ic_stat_download_done)
                .SetAutoCancel(true)
                .SetOngoing(false)
                .SetPriority(NotificationCompat.PriorityDefault)!;
#pragma warning restore CS8602

            if (pendingIntent != null)
                builder.SetContentIntent(pendingIntent);

            // Action: Open Folder ("باز کردن پوشه")
            try
            {
                var folderIntent = new Intent(DownloadManager.ActionViewDownloads);
                folderIntent.AddFlags(ActivityFlags.NewTask);
                var folderPendingIntent = PendingIntent.GetActivity(
                    _context,
                    id + 5000,
                    folderIntent,
                    PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

                builder.AddAction(Resource.Drawable.ic_shortcut_download, "باز کردن پوشه", folderPendingIntent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidDownloadNotificationService] Open folder error: {ex.Message}");
            }

            NotificationManagerCompat.From(_context)?.Notify(id, builder.Build());
        }

        public void CancelNotification(int id)
        {
            NotificationManagerCompat.From(_context)?.Cancel(id);
        }

        public Intent? CreateViewIntent(string filePathOrUri, string mimeType)
        {
            try
            {
                Uri? uri;

                if (filePathOrUri.StartsWith("content://", StringComparison.OrdinalIgnoreCase) ||
                    filePathOrUri.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                {
                    uri = Uri.Parse(filePathOrUri);
                }
                else if (filePathOrUri.StartsWith("/", StringComparison.Ordinal))
                {
                    var file = new Java.IO.File(filePathOrUri);
                    uri = AndroidFileProvider.GetUriForFile(
                        _context,
                        _context.PackageName + ".fileprovider",
                        file);
                }
                else
                {
                    uri = Uri.Parse(filePathOrUri);
                }

                if (uri == null)
                    return null;

                var viewIntent = new Intent(Intent.ActionView);
                viewIntent.SetDataAndType(uri, string.IsNullOrWhiteSpace(mimeType) ? "*/*" : mimeType);
                viewIntent.AddFlags(ActivityFlags.GrantReadUriPermission);
                viewIntent.AddFlags(ActivityFlags.NewTask);

                bool isApk = (!string.IsNullOrWhiteSpace(mimeType) && mimeType.Equals("application/vnd.android.package-archive", StringComparison.OrdinalIgnoreCase))
                             || (!string.IsNullOrWhiteSpace(filePathOrUri) && filePathOrUri.EndsWith(".apk", StringComparison.OrdinalIgnoreCase));

                if (isApk)
                {
                    return viewIntent;
                }

                viewIntent.ClipData = ClipData.NewRawUri((string?)null, uri);

                var chooser = Intent.CreateChooser(viewIntent, "Open file");
                chooser?.AddFlags(ActivityFlags.NewTask);
                return chooser;
            }
            catch
            {
                return null;
            }
        }
    }
}
