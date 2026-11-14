using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Models;
using KopolArt.Mobile.Services;

namespace KopolArt.Mobile.Platforms.Android
{
    /// <summary>
    /// Single place where the "new post(s)" notification is built. Reused by the background job
    /// and by the hidden test action in the settings screen so both always look identical.
    /// </summary>
    public class NewContentNotifier : INewContentNotifier
    {
        public const string ChannelId = "kopolart_new_content_v2";
        public const string ChannelName = "مطالب و طرح‌های تازه کپل‌آرت";
        public const int NotificationId = 4001;

        /// <summary>Legacy channel (Default importance) that cannot be upgraded in place.</summary>
        private const string LegacyChannelId = "kopolart_new_content";

        private const int MaxInboxLines = 5;
        private const int RequestCodeOpenPost = 4001;
        private const int RequestCodeOpenAll = 4002;
        private static readonly TimeSpan ImageTimeout = TimeSpan.FromSeconds(6);

        private readonly Context _context;

        public NewContentNotifier(Context context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<bool> ShowDigestAsync(IReadOnlyList<RssFeedItem> posts)
        {
            if (posts == null || posts.Count == 0)
                return false;

            return await ShowAsync(posts, isTest: false);
        }

        public Task<bool> ShowTestAsync()
        {
            return ShowAsync(BuildTestPosts(), isTest: true);
        }

        private async Task<bool> ShowAsync(IReadOnlyList<RssFeedItem> posts, bool isTest)
        {
            try
            {
                EnsureChannel();

                var managerCompat = NotificationManagerCompat.From(_context);
                if (managerCompat == null || !managerCompat.AreNotificationsEnabled())
                {
                    System.Diagnostics.Debug.WriteLine("[NewContentNotifier] Notifications are not enabled for this app.");
                    return false;
                }

                var newest = posts[0];
                string title = BuildTitle(posts, isTest);
                string summary = BuildSummary(posts);

                var intent = new Intent(_context, typeof(MainActivity));
                intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
                intent.PutExtra("from_new_post_notification", true);
                intent.PutExtra("target_post_url", newest.Link);

                var openPendingIntent = PendingIntent.GetActivity(
                    _context,
                    RequestCodeOpenPost,
                    intent,
                    PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

#pragma warning disable CS8602
                var builder = new NotificationCompat.Builder(_context, ChannelId)
                    .SetContentTitle(title)
                    .SetContentText(newest.Title)
                    .SetSmallIcon(Resource.Drawable.ic_stat_content)
                    .SetColor(global::Android.Graphics.Color.ParseColor(Constants.Theme.LightColor))
                    .SetAutoCancel(true)
                    .SetOngoing(false)
                    .SetPriority(NotificationCompat.PriorityHigh)
                    .SetCategory(NotificationCompat.CategorySocial)
                    .SetVisibility(NotificationCompat.VisibilityPublic);

                if (openPendingIntent != null)
                {
                    builder.SetContentIntent(openPendingIntent);
                }

                global::Android.Graphics.Bitmap? bannerBitmap = await DownloadBitmapAsync(newest.ImageUrl);

                if (posts.Count == 1)
                {
                    // Single post: keep the rich big-picture layout.
                    if (bannerBitmap != null)
                    {
                        var bigPicStyle = new NotificationCompat.BigPictureStyle();
                        bigPicStyle.BigPicture(bannerBitmap);
                        bigPicStyle.SetBigContentTitle(title);
                        bigPicStyle.SetSummaryText(summary);
                        builder.SetStyle(bigPicStyle);
                    }
                    else
                    {
                        var bigTextStyle = new NotificationCompat.BigTextStyle();
                        bigTextStyle.SetBigContentTitle(title);
                        bigTextStyle.BigText($"{newest.Title}\n\n{summary}");
                        builder.SetStyle(bigTextStyle);
                    }
                }
                else
                {
                    // Digest: inbox-style summary with one line per post.
                    if (bannerBitmap != null)
                    {
                        builder.SetLargeIcon(bannerBitmap);
                    }

                    var inboxStyle = new NotificationCompat.InboxStyle();
                    inboxStyle.SetBigContentTitle(title);

                    int lineCount = Math.Min(posts.Count, MaxInboxLines);
                    for (int i = 0; i < lineCount; i++)
                    {
                        inboxStyle.AddLine($"• {posts[i].Title}");
                    }

                    int hiddenCount = posts.Count - lineCount;
                    inboxStyle.SetSummaryText(hiddenCount > 0
                        ? $"و {hiddenCount.ToPersianDigits()} مطلب دیگر"
                        : summary);

                    builder.SetStyle(inboxStyle);
                }

                if (openPendingIntent != null)
                {
                    builder.AddAction(Resource.Drawable.ic_stat_content, "مشاهده در برنامه", openPendingIntent);
                }

                if (posts.Count > 1)
                {
                    var allIntent = new Intent(_context, typeof(MainActivity));
                    allIntent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
                    allIntent.PutExtra("from_new_post_notification", true);
                    allIntent.PutExtra("target_post_url", Constants.Urls.Posts);

                    var allPendingIntent = PendingIntent.GetActivity(
                        _context,
                        RequestCodeOpenAll,
                        allIntent,
                        PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

                    if (allPendingIntent != null)
                    {
                        builder.AddAction(Resource.Drawable.ic_stat_content, "همه مطالب تازه", allPendingIntent);
                    }
                }

                var notification = builder.Build();
#pragma warning restore CS8602
                if (notification == null)
                    return false;

                managerCompat.Notify(NotificationId, notification);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NewContentNotifier] Failed to show notification: {ex.Message}");
                return false;
            }
        }

        private void EnsureChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
                return;

            var manager = (NotificationManager?)_context.GetSystemService(global::Android.Content.Context.NotificationService);
            if (manager == null)
                return;

            var channel = new NotificationChannel(
                ChannelId,
                ChannelName,
                NotificationImportance.High)
            {
                Description = "اطلاع‌رسانی هنگام انتشار محتوا، والپیپرها و طرح‌های تازه در کپل‌آرت"
            };
            channel.EnableLights(true);
            channel.EnableVibration(true);

            manager.CreateNotificationChannel(channel);

            // Channel importance can only be set on creation, so the old Default-importance channel
            // is deleted to avoid two identical entries in the system notification settings.
            manager.DeleteNotificationChannel(LegacyChannelId);
        }

        private static string BuildTitle(IReadOnlyList<RssFeedItem> posts, bool isTest)
        {
            string prefix = isTest ? "تست — " : string.Empty;

            if (posts.Count > 1)
            {
                // Persian digits, e.g. "۳ مطلب جدید در کپلآرت"
                return $"{prefix}{posts.Count.ToPersianDigits()} مطلب جدید در کپل‌آرت";
            }

            string category = posts[0].Category?.Trim() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(category)
                ? $"{prefix}مطلب جدید در {category}"
                : $"{prefix}مطلب جدید در کپل‌آرت";
        }

        private static string BuildSummary(IReadOnlyList<RssFeedItem> posts)
        {
            if (posts.Count > 1)
                return $"آخرین مطلب: {posts[0].Title}";

            string description = posts[0].Description ?? string.Empty;
            return !string.IsNullOrWhiteSpace(description) ? description : posts[0].Title;
        }

        private static async Task<global::Android.Graphics.Bitmap?> DownloadBitmapAsync(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return null;

            try
            {
                using var cts = new CancellationTokenSource(ImageTimeout);
                using var client = new HttpClient { Timeout = ImageTimeout };
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", Constants.Network.AppUserAgent);

                var imageBytes = await client.GetByteArrayAsync(imageUrl, cts.Token);
                if (imageBytes == null || imageBytes.Length == 0)
                    return null;

                return await global::Android.Graphics.BitmapFactory.DecodeByteArrayAsync(imageBytes, 0, imageBytes.Length);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NewContentNotifier] Image download failed: {ex.Message}");
                return null;
            }
        }

        private static List<RssFeedItem> BuildTestPosts()
        {
            return
            [
                new RssFeedItem
                {
                    Title = "عنوان نمونه مطلب جدید — اعلان آزمایشی",
                    Link = Constants.Urls.Posts,
                    Category = "پست",
                    Description = "این یک اعلان آزمایشی برای بررسی چیدمان اعلان مطالب تازه است.",
                    Guid = "test-post-1"
                },
                new RssFeedItem
                {
                    Title = "طرح رنگ تازه — نمونه دوم",
                    Link = Constants.Urls.Colors,
                    Category = "رنگ",
                    Guid = "test-post-2"
                },
                new RssFeedItem
                {
                    Title = "والپیپر تازه — نمونه سوم",
                    Link = Constants.Urls.Wallpapers,
                    Category = "والپیپر",
                    Guid = "test-post-3"
                }
            ];
        }
    }
}
