using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Widget;

namespace KopolArt.Mobile.Platforms.Android.Widgets
{
    [BroadcastReceiver(
        Name = "ir.kopolart.app.KopolArtWidgetProvider",
        Label = "@string/widget_quick_actions_title",
        Enabled = true,
        Exported = true)]
    [IntentFilter(new[] { AppWidgetManager.ActionAppwidgetUpdate })]
    [MetaData("android.appwidget.provider", Resource = "@xml/widget_quick_actions_info")]
    public class KopolArtWidgetProvider : AppWidgetProvider
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            base.OnReceive(context, intent);

            if (context == null || intent == null) return;

            var action = intent.Action;
            if (action == AppWidgetManager.ActionAppwidgetUpdate ||
                action == AppWidgetManager.ActionAppwidgetOptionsChanged ||
                action == AppWidgetManager.ActionAppwidgetEnabled)
            {
                try
                {
                    var appWidgetManager = AppWidgetManager.GetInstance(context);
                    if (appWidgetManager != null)
                    {
                        var component = new ComponentName(context, Java.Lang.Class.FromType(typeof(KopolArtWidgetProvider)));
                        var widgetIds = appWidgetManager.GetAppWidgetIds(component);
                        if (widgetIds != null && widgetIds.Length > 0)
                        {
                            foreach (var id in widgetIds)
                            {
                                UpdateAppWidget(context, appWidgetManager, id);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[KopolArtWidgetProvider] OnReceive error: {ex.Message}");
                }
            }
        }

        public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
        {
            base.OnUpdate(context, appWidgetManager, appWidgetIds);

            if (context == null || appWidgetManager == null || appWidgetIds == null)
                return;

            foreach (var appWidgetId in appWidgetIds)
            {
                UpdateAppWidget(context, appWidgetManager, appWidgetId);
            }
        }

        public override void OnAppWidgetOptionsChanged(Context? context, AppWidgetManager? appWidgetManager, int appWidgetId, Bundle? newOptions)
        {
            base.OnAppWidgetOptionsChanged(context, appWidgetManager, appWidgetId, newOptions);

            if (context != null && appWidgetManager != null)
            {
                UpdateAppWidget(context, appWidgetManager, appWidgetId);
            }
        }

        public static void UpdateAppWidget(Context context, AppWidgetManager appWidgetManager, int appWidgetId)
        {
            try
            {
                var views = new RemoteViews(context.PackageName, Resource.Layout.widget_quick_actions);

                // 1. Read Theme Mode & Opacity settings
                var themeMode = Preferences.Get("AppThemeMode", "System");
                int opacity = Preferences.Get("WidgetOpacity", 100);

                bool isDark;
                if (string.Equals(themeMode, "Dark", StringComparison.OrdinalIgnoreCase))
                {
                    isDark = true;
                }
                else if (string.Equals(themeMode, "Light", StringComparison.OrdinalIgnoreCase))
                {
                    isDark = false;
                }
                else
                {
                    // Auto (System) theme
                    var uiMode = context.Resources?.Configuration?.UiMode;
                    isDark = uiMode.HasValue && (uiMode.Value & global::Android.Content.Res.UiMode.NightMask) == global::Android.Content.Res.UiMode.NightYes;
                }

                int bgRes;
                if (isDark)
                {
                    bgRes = opacity switch
                    {
                        75 => Resource.Drawable.bg_widget_card_dark_75,
                        50 => Resource.Drawable.bg_widget_card_dark_50,
                        0 => Resource.Drawable.bg_widget_card_dark_0,
                        _ => Resource.Drawable.bg_widget_card_dark
                    };

                    views.SetTextColor(Resource.Id.widget_title, global::Android.Graphics.Color.ParseColor("#F5EFE6"));
                    var textSec = opacity == 0 ? global::Android.Graphics.Color.ParseColor("#FFFFFF") : global::Android.Graphics.Color.ParseColor("#A89E96");
                    views.SetTextColor(Resource.Id.txt_widget_browse, textSec);
                    views.SetTextColor(Resource.Id.txt_widget_bookmarks, textSec);
                    views.SetTextColor(Resource.Id.txt_widget_downloads, textSec);
                    views.SetTextColor(Resource.Id.txt_widget_settings, textSec);
                    views.SetTextColor(Resource.Id.txt_widget_contact, textSec);
                    views.SetTextColor(Resource.Id.txt_widget_history, textSec);
                    views.SetTextColor(Resource.Id.txt_widget_updates, textSec);
                    views.SetTextColor(Resource.Id.txt_widget_about, textSec);
                    views.SetInt(Resource.Id.widget_divider, "setBackgroundColor", global::Android.Graphics.Color.ParseColor("#3E3A36"));
                    views.SetInt(Resource.Id.widget_divider_secondary, "setBackgroundColor", global::Android.Graphics.Color.ParseColor("#3E3A36"));
                }
                else
                {
                    bgRes = opacity switch
                    {
                        75 => Resource.Drawable.bg_widget_card_light_75,
                        50 => Resource.Drawable.bg_widget_card_light_50,
                        0 => Resource.Drawable.bg_widget_card_light_0,
                        _ => Resource.Drawable.bg_widget_card_light
                    };

                    views.SetTextColor(Resource.Id.widget_title, global::Android.Graphics.Color.ParseColor("#2D2621"));
                    var textSec = opacity == 0 ? global::Android.Graphics.Color.ParseColor("#2D2621") : global::Android.Graphics.Color.ParseColor("#7A6A5E");
                    views.SetTextColor(Resource.Id.txt_widget_browse, textSec);
                    views.SetTextColor(Resource.Id.txt_widget_bookmarks, textSec);
                    views.SetTextColor(Resource.Id.txt_widget_downloads, textSec);
                    views.SetTextColor(Resource.Id.txt_widget_settings, textSec);
                    views.SetTextColor(Resource.Id.txt_widget_contact, textSec);
                    views.SetTextColor(Resource.Id.txt_widget_history, textSec);
                    views.SetTextColor(Resource.Id.txt_widget_updates, textSec);
                    views.SetTextColor(Resource.Id.txt_widget_about, textSec);
                    views.SetInt(Resource.Id.widget_divider, "setBackgroundColor", global::Android.Graphics.Color.ParseColor("#D4C2B0"));
                    views.SetInt(Resource.Id.widget_divider_secondary, "setBackgroundColor", global::Android.Graphics.Color.ParseColor("#D4C2B0"));
                }

                views.SetInt(Resource.Id.widget_root, "setBackgroundResource", bgRes);

                // 2. Adaptive Height: In 4x2 mode (height >= 105dp), show secondary actions row
                var options = appWidgetManager.GetAppWidgetOptions(appWidgetId);
                int minHeight = options?.GetInt(AppWidgetManager.OptionAppwidgetMinHeight) ?? 0;
                bool isTall = minHeight >= 105;

                views.SetViewVisibility(Resource.Id.widget_divider_secondary, isTall ? global::Android.Views.ViewStates.Visible : global::Android.Views.ViewStates.Gone);
                views.SetViewVisibility(Resource.Id.widget_row_secondary, isTall ? global::Android.Views.ViewStates.Visible : global::Android.Views.ViewStates.Gone);

                // Row 1 text labels: hidden in 4x1 (icon-only), visible in 4x2
                var textVisibility = isTall ? global::Android.Views.ViewStates.Visible : global::Android.Views.ViewStates.Gone;
                views.SetViewVisibility(Resource.Id.txt_widget_browse, textVisibility);
                views.SetViewVisibility(Resource.Id.txt_widget_bookmarks, textVisibility);
                views.SetViewVisibility(Resource.Id.txt_widget_downloads, textVisibility);
                views.SetViewVisibility(Resource.Id.txt_widget_settings, textVisibility);

                // 3. Header click -> open main dashboard
                var mainIntent = new Intent(context, typeof(MainActivity));
                mainIntent.SetAction(Intent.ActionMain);
                mainIntent.AddCategory(Intent.CategoryLauncher);
                mainIntent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
                var mainPendingIntent = PendingIntent.GetActivity(
                    context, 100, mainIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
                views.SetOnClickPendingIntent(Resource.Id.widget_header, mainPendingIntent);
                views.SetOnClickPendingIntent(Resource.Id.widget_app_icon, mainPendingIntent);
                views.SetOnClickPendingIntent(Resource.Id.widget_title, mainPendingIntent);

                // 4. Primary Row Action Buttons
                // Browse
                var browseIntent = CreateActionIntent(context, ShortcutHelper.ActionBrowse);
                var browsePendingIntent = PendingIntent.GetActivity(
                    context, 101, browseIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
                views.SetOnClickPendingIntent(Resource.Id.btn_widget_browse, browsePendingIntent);
                views.SetOnClickPendingIntent(Resource.Id.img_widget_browse, browsePendingIntent);

                // Bookmarks
                var bookmarksIntent = CreateActionIntent(context, ShortcutHelper.ActionBookmarks);
                var bookmarksPendingIntent = PendingIntent.GetActivity(
                    context, 102, bookmarksIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
                views.SetOnClickPendingIntent(Resource.Id.btn_widget_bookmarks, bookmarksPendingIntent);
                views.SetOnClickPendingIntent(Resource.Id.img_widget_bookmarks, bookmarksPendingIntent);

                // Downloads
                var downloadsIntent = CreateActionIntent(context, ShortcutHelper.ActionDownloads);
                var downloadsPendingIntent = PendingIntent.GetActivity(
                    context, 103, downloadsIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
                views.SetOnClickPendingIntent(Resource.Id.btn_widget_downloads, downloadsPendingIntent);
                views.SetOnClickPendingIntent(Resource.Id.img_widget_downloads, downloadsPendingIntent);

                // Settings
                var settingsIntent = CreateActionIntent(context, ShortcutHelper.ActionSettings);
                var settingsPendingIntent = PendingIntent.GetActivity(
                    context, 104, settingsIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
                views.SetOnClickPendingIntent(Resource.Id.btn_widget_settings, settingsPendingIntent);
                views.SetOnClickPendingIntent(Resource.Id.img_widget_settings, settingsPendingIntent);

                // 5. Secondary Row Action Buttons (4x2 mode)
                // Contact
                var contactIntent = CreateActionIntent(context, ShortcutHelper.ActionContact);
                var contactPendingIntent = PendingIntent.GetActivity(
                    context, 105, contactIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
                views.SetOnClickPendingIntent(Resource.Id.btn_widget_contact, contactPendingIntent);
                views.SetOnClickPendingIntent(Resource.Id.img_widget_contact, contactPendingIntent);

                // History
                var historyIntent = CreateActionIntent(context, ShortcutHelper.ActionHistory);
                var historyPendingIntent = PendingIntent.GetActivity(
                    context, 106, historyIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
                views.SetOnClickPendingIntent(Resource.Id.btn_widget_history, historyPendingIntent);
                views.SetOnClickPendingIntent(Resource.Id.img_widget_history, historyPendingIntent);

                // Updates
                var updatesIntent = CreateActionIntent(context, ShortcutHelper.ActionUpdates);
                var updatesPendingIntent = PendingIntent.GetActivity(
                    context, 107, updatesIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
                views.SetOnClickPendingIntent(Resource.Id.btn_widget_updates, updatesPendingIntent);
                views.SetOnClickPendingIntent(Resource.Id.img_widget_updates, updatesPendingIntent);

                // About
                var aboutIntent = CreateActionIntent(context, ShortcutHelper.ActionAbout);
                var aboutPendingIntent = PendingIntent.GetActivity(
                    context, 108, aboutIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
                views.SetOnClickPendingIntent(Resource.Id.btn_widget_about, aboutPendingIntent);
                views.SetOnClickPendingIntent(Resource.Id.img_widget_about, aboutPendingIntent);

                appWidgetManager.UpdateAppWidget(appWidgetId, views);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[KopolArtWidgetProvider] UpdateAppWidget error: {ex.Message}");
            }
        }

        private static Intent CreateActionIntent(Context context, string action)
        {
            var intent = new Intent(context, typeof(MainActivity));
            intent.SetAction(Intent.ActionView);
            intent.PutExtra(ShortcutHelper.ExtraShortcutAction, action);
            intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
            return intent;
        }
    }
}
