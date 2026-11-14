using Android.Content;

namespace KopolArt.Mobile.Platforms.Android
{
    public partial class MainActivity
    {
        // ─── Deep link (new intent while app is running) ──────────────────────
        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);

            ExtractShortcutAction(intent);
            ExtractUpdateNotificationAction(intent);

            if (intent?.Data != null)
            {
                var url = intent.Data.ToString();
                if (!string.IsNullOrEmpty(url) && IsKopolArtUrl(url))
                {
                    var handler = DeepLinkReceived;
                    if (handler != null)
                    {
                        handler.Invoke(this, url);
                    }
                    else
                    {
                        // No subscriber yet (page not Loaded) - queue the link
                        // so MainPage can consume it when it subscribes.
                        PendingDeepLinkUrl = url;
                    }
                }
            }
        }


        // ─── Private helpers ──────────────────────────────────────────────────
        private void ExtractDeepLinkUrl(Intent? intent)
        {
            if (intent is { Action: Intent.ActionView, Data: not null })
            {
                var url = intent.Data.ToString();
                if (!string.IsNullOrEmpty(url) && IsKopolArtUrl(url))
                    PendingDeepLinkUrl = url;
            }
        }

        private void ExtractShortcutAction(Intent? intent)
        {
            if (intent != null && intent.HasExtra(ShortcutHelper.ExtraShortcutAction))
            {
                var action = intent.GetStringExtra(ShortcutHelper.ExtraShortcutAction);
                if (!string.IsNullOrEmpty(action))
                {
                    if (ShortcutActionReceived != null)
                    {
                        ShortcutActionReceived.Invoke(this, action);
                    }
                    else
                    {
                        PendingShortcutAction = action;
                    }
                }
            }
        }

        private void ExtractUpdateNotificationAction(Intent? intent)
        {
            if (intent != null && intent.GetBooleanExtra("from_update_notification", false))
            {
                const string updateUrl = "https://kopolart.ir/download-app/android";
                var handler = DeepLinkReceived;
                if (handler != null)
                {
                    handler.Invoke(this, updateUrl);
                }
                else
                {
                    PendingDeepLinkUrl = updateUrl;
                }
            }
            else if (intent != null && intent.GetBooleanExtra("from_new_post_notification", false))
            {
                string? targetPostUrl = intent.GetStringExtra("target_post_url");
                if (!string.IsNullOrWhiteSpace(targetPostUrl))
                {
                    var handler = DeepLinkReceived;
                    if (handler != null)
                    {
                        handler.Invoke(this, targetPostUrl);
                    }
                    else
                    {
                        PendingDeepLinkUrl = targetPostUrl;
                    }
                }
            }
        }

        private static bool IsKopolArtUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var host = uri.Host.ToLowerInvariant();
                return host is "kopolart.ir" or "www.kopolart.ir";
            }
            catch
            {
                return false;
            }
        }
    }
}
