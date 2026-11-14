using Android.App;
using Android.App.Job;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using KopolArt.Mobile.Models;
using KopolArt.Mobile.Services;

namespace KopolArt.Mobile.Platforms.Android
{
    [Service(
        Name = "ir.kopolart.app.services.UpdateCheckJobService",
        Permission = "android.permission.BIND_JOB_SERVICE",
        Exported = true)]
    public class UpdateCheckJobService : JobService
    {
        public const string ChannelId = "kopolart_app_updates";
        public const string ChannelName = "به‌روزرسانی‌های کپل‌آرت";
        public const int NotificationId = 3001;

        public const string PrefKeyLastNotifiedVersion = "AppUpdate_LastNotifiedVersion";
        public const string PrefKeyLastNotifiedUtc = "AppUpdate_LastNotifiedUtc";

        public override bool OnStartJob(JobParameters? @params)
        {
            Task.Run(async () =>
            {
                bool ranCheck = false;

                try
                {
                    // The job is registered for a 24 hour period, but boot / app-update
                    // re-registration can run it earlier; keep at most one run per day.
                    if (!BackgroundCheckThrottle.ShouldRun())
                    {
                        System.Diagnostics.Debug.WriteLine("[UpdateCheckJobService] Skipped: a background check already ran recently.");
                        return;
                    }

                    ranCheck = true;

                    await CheckAndNotifyUpdateAsync();
                    await CheckAndNotifyNewContentAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[UpdateCheckJobService] Error during update check: {ex}");
                }
                finally
                {
                    if (ranCheck)
                    {
                        BackgroundCheckThrottle.MarkRan();
                    }

                    JobFinished(@params, false);
                }
            });

            return true; // Work is running asynchronously
        }

        public override bool OnStopJob(JobParameters? @params)
        {
            return false; // Do not reschedule immediately
        }

        private async Task CheckAndNotifyUpdateAsync()
        {
            string currentVersion = GetCurrentAppVersion();
            if (string.IsNullOrWhiteSpace(currentVersion))
                return;

            var updateInfo = await UpdateService.FetchUpdateInfoDirectAsync();
            if (updateInfo == null || string.IsNullOrWhiteSpace(updateInfo.Version))
                return;

            // Check if server version is newer than current version
            if (!UpdateService.IsNewerVersion(currentVersion, updateInfo.Version))
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateCheckJobService] App is up to date ({currentVersion} >= {updateInfo.Version})");
                return;
            }

            // Avoid spamming: Check if user was already notified for this exact version
            string lastNotified = Preferences.Get(PrefKeyLastNotifiedVersion, string.Empty);
            if (string.Equals(lastNotified, updateInfo.Version, StringComparison.OrdinalIgnoreCase))
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateCheckJobService] User already notified for version {updateInfo.Version}");
                return;
            }

            // Show notification
            ShowUpdateNotification(updateInfo);

            // Record that notification was shown
            Preferences.Set(PrefKeyLastNotifiedVersion, updateInfo.Version);
            Preferences.Set(PrefKeyLastNotifiedUtc, DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
        }

        private void ShowUpdateNotification(UpdateInfo updateInfo)
        {
            EnsureNotificationChannel();

            // Tapping notification opens MainActivity
            var mainIntent = new Intent(this, typeof(MainActivity));
            mainIntent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
            mainIntent.PutExtra("from_update_notification", true);
            mainIntent.PutExtra("update_target_version", updateInfo.Version);

            var pendingIntent = PendingIntent.GetActivity(
                this,
                0,
                mainIntent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            string title = "به‌روزرسانی جدید کپل‌آرت 🎉";
            string contentText = $"نسخه جدید {updateInfo.Version} هم‌اکنون در دسترس است. جهت مشاهده و دریافت لمس کنید.";

#pragma warning disable CS8602
            var builder = new NotificationCompat.Builder(this, ChannelId)
                .SetContentTitle(title)
                .SetContentText(contentText)
                .SetSmallIcon(Resource.Drawable.ic_stat_update)
                .SetColor(global::Android.Graphics.Color.ParseColor(Constants.Theme.LightColor))
                .SetAutoCancel(true)
                .SetOngoing(false)
                .SetPriority(NotificationCompat.PriorityHigh);

            if (pendingIntent != null)
            {
                builder.SetContentIntent(pendingIntent);
            }

            // BigTextStyle with release notes
            var latestRelease = updateInfo.Updates?.FirstOrDefault(u => string.Equals(u.Version, updateInfo.Version, StringComparison.OrdinalIgnoreCase))
                                ?? updateInfo.Updates?.FirstOrDefault();

            if (latestRelease != null && !string.IsNullOrWhiteSpace(latestRelease.ReleaseNotes))
            {
                var bigStyle = new NotificationCompat.BigTextStyle();
                bigStyle.SetBigContentTitle(title);
                bigStyle.BigText($"امکانات جدید نسخه {updateInfo.Version}:\n{latestRelease.ReleaseNotes}");
                builder.SetStyle(bigStyle);
            }

            // Direct action button to open download URL in external browser
            string downloadUrl = !string.IsNullOrWhiteSpace(updateInfo.Url) ? updateInfo.Url : "https://kopolart.ir/download-app/android";
            var browserIntent = new Intent(Intent.ActionView, global::Android.Net.Uri.Parse(downloadUrl));
            browserIntent.AddFlags(ActivityFlags.NewTask);
            var downloadPendingIntent = PendingIntent.GetActivity(
                this,
                1,
                browserIntent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            if (downloadPendingIntent != null)
            {
                builder.AddAction(Resource.Drawable.ic_stat_download, "دانلود نسخه جدید", downloadPendingIntent);
            }

            var notification = builder.Build();
#pragma warning restore CS8602
            if (notification != null)
            {
                try
                {
                    var managerCompat = NotificationManagerCompat.From(this);
                    if (managerCompat != null && managerCompat.AreNotificationsEnabled())
                    {
                        managerCompat.Notify(NotificationId, notification);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[UpdateCheckJobService] Post update notification failed: {ex.Message}");
                }
            }
        }

        private void EnsureNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(
                    ChannelId,
                    ChannelName,
                    NotificationImportance.Default)
                {
                    Description = "اطلاع‌رسانی هنگام انتشار نسخه جدید کپل‌آرت"
                };
                channel.EnableLights(true);
                channel.EnableVibration(true);

                var manager = (NotificationManager?)GetSystemService(NotificationService);
                manager?.CreateNotificationChannel(channel);
            }
        }

        private string GetCurrentAppVersion()
        {
            try
            {
                var packageInfo = PackageManager?.GetPackageInfo(PackageName ?? string.Empty, 0);
                if (!string.IsNullOrWhiteSpace(packageInfo?.VersionName))
                    return packageInfo.VersionName;
            }
            catch
            {
                // ignored
            }

            try
            {
                return AppInfo.Current.VersionString;
            }
            catch
            {
                // ignored
            }

            return "1.5.0";
        }
        private async Task CheckAndNotifyNewContentAsync()
        {
            try
            {
                bool isEnabled = Preferences.Get(Constants.Keys.NotifyNewContent, true);

                // Returns every post published after the last announced one (newest first).
                var unseenPosts = await RssFeedService.GetUnseenNewPostsDirectAsync(isEnabled);
                if (unseenPosts.Count == 0)
                    return;

                var notifier = new NewContentNotifier(this);
                bool shown = await notifier.ShowDigestAsync(unseenPosts);
                if (!shown)
                {
                    // Notification permission might be missing right now; do not mark these posts as
                    // announced so the next daily check can still report them.
                    return;
                }

                // Only the newest post is persisted as "last announced"; every older post was part of
                // this digest and sits after the stored id in the feed, so it is never reported again.
                RssFeedService.MarkPostAsNotifiedDirect(unseenPosts[0]);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateCheckJobService] Error checking new content: {ex.Message}");
            }
        }
    }
}
