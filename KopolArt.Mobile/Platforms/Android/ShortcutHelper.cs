using Android.Content;
using Android.Content.PM;
using Android.Graphics.Drawables;

namespace KopolArt.Mobile.Platforms.Android
{
    public static class ShortcutHelper
    {
        public const string ExtraShortcutAction = "shortcut_action";
        public const string ActionBrowse = "browse";
        public const string ActionSettings = "settings";
        public const string ActionContact = "contact";
        public const string ActionBookmarks = "bookmarks";
        public const string ActionDownloads = "downloads";
        public const string ActionHistory = "history";
        public const string ActionUpdates = "updates";
        public const string ActionAbout = "about";

        public static void InitializeShortcuts(Context context)
        {
            try
            {
                var shortcutManager = (ShortcutManager?)context.GetSystemService(Context.ShortcutService);
                if (shortcutManager == null) return;

                var shortcuts = new List<ShortcutInfo>();

                // 1. Browse shortcut
                var browseIntent = new Intent(context, typeof(MainActivity));
                browseIntent.SetAction(Intent.ActionView);
                browseIntent.PutExtra(ExtraShortcutAction, ActionBrowse);
                browseIntent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);

                var browseShortcut = new ShortcutInfo.Builder(context, "shortcut_browse")
                    .SetShortLabel("شروع مرور")
                    .SetLongLabel("گشت و گذار در کپل‌آرت")
                    .SetIcon(Icon.CreateWithResource(context, Resource.Drawable.ic_shortcut_browse))
                    .SetIntent(browseIntent)
                    .Build();

                shortcuts.Add(browseShortcut);

                // 2. Bookmarks shortcut
                var bookmarksIntent = new Intent(context, typeof(MainActivity));
                bookmarksIntent.SetAction(Intent.ActionView);
                bookmarksIntent.PutExtra(ExtraShortcutAction, ActionBookmarks);
                bookmarksIntent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);

                var bookmarksShortcut = new ShortcutInfo.Builder(context, "shortcut_bookmarks")
                    .SetShortLabel("نشان‌شده‌ها")
                    .SetLongLabel("مشاهده آثار نشان‌شده")
                    .SetIcon(Icon.CreateWithResource(context, Resource.Drawable.ic_shortcut_bookmark))
                    .SetIntent(bookmarksIntent)
                    .Build();

                shortcuts.Add(bookmarksShortcut);

                // 2. Settings shortcut
                var settingsIntent = new Intent(context, typeof(MainActivity));
                settingsIntent.SetAction(Intent.ActionView);
                settingsIntent.PutExtra(ExtraShortcutAction, ActionSettings);
                settingsIntent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);

                var settingsShortcut = new ShortcutInfo.Builder(context, "shortcut_settings")
                    .SetShortLabel("تنظیمات")
                    .SetLongLabel("تنظیمات برنامه کپل‌آرت")
                    .SetIcon(Icon.CreateWithResource(context, Resource.Drawable.ic_shortcut_settings))
                    .SetIntent(settingsIntent)
                    .Build();

                shortcuts.Add(settingsShortcut);

                // 3. Contact Me shortcut
                var contactIntent = new Intent(context, typeof(MainActivity));
                contactIntent.SetAction(Intent.ActionView);
                contactIntent.PutExtra(ExtraShortcutAction, ActionContact);
                contactIntent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);

                var contactShortcut = new ShortcutInfo.Builder(context, "shortcut_contact")
                    .SetShortLabel("تماس با من")
                    .SetLongLabel("ارتباط مستقیم با مدیر کپل‌آرت")
                    .SetIcon(Icon.CreateWithResource(context, Resource.Drawable.ic_shortcut_contact))
                    .SetIntent(contactIntent)
                    .Build();

                shortcuts.Add(contactShortcut);

                shortcutManager.SetDynamicShortcuts(shortcuts);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShortcutHelper] Error initializing shortcuts: {ex.Message}");
            }
        }
    }
}
