using Android.Appwidget;
using Android.Content;
using Android.Content.PM;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Widget;
using KopolArt.Mobile.Platforms.Android.Widgets;
using KopolArt.Mobile.Services;

namespace KopolArt.Mobile.Platforms.Android.Services
{
    public class AndroidHomeScreenService : IHomeScreenService
    {
        public bool IsPinWidgetSupported()
        {
            try
            {
                var context = global::Android.App.Application.Context;
                var appWidgetManager = AppWidgetManager.GetInstance(context);
                return appWidgetManager?.IsRequestPinAppWidgetSupported ?? false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidHomeScreenService] IsPinWidgetSupported error: {ex.Message}");
                return false;
            }
        }

        public Task<bool> RequestPinWidgetAsync()
        {
            try
            {
                var context = global::Android.App.Application.Context;
                var appWidgetManager = AppWidgetManager.GetInstance(context);
                if (appWidgetManager == null || !appWidgetManager.IsRequestPinAppWidgetSupported)
                {
                    return Task.FromResult(false);
                }

                var myProvider = new ComponentName(context, Java.Lang.Class.FromType(typeof(KopolArtWidgetProvider)));

                Bundle? bundle = null;
                try
                {
                    bundle = new Bundle();
                    var remoteViews = new RemoteViews(context.PackageName, Resource.Layout.widget_quick_actions);
                    bundle.PutParcelable(AppWidgetManager.ExtraAppwidgetPreview, remoteViews);
                }
                catch
                {
                    bundle = null;
                }

                var success = appWidgetManager.RequestPinAppWidget(myProvider, bundle, null);
                return Task.FromResult(success);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidHomeScreenService] RequestPinWidgetAsync error: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        public bool IsPinShortcutSupported()
        {
            try
            {
                var context = global::Android.App.Application.Context;
                var shortcutManager = (ShortcutManager?)context.GetSystemService(Context.ShortcutService);
                return shortcutManager?.IsRequestPinShortcutSupported ?? false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidHomeScreenService] IsPinShortcutSupported error: {ex.Message}");
                return false;
            }
        }

        public Task<bool> RequestPinShortcutAsync(string shortcutId, string label, string action)
        {
            try
            {
                var context = global::Android.App.Application.Context;
                var shortcutManager = (ShortcutManager?)context.GetSystemService(Context.ShortcutService);
                if (shortcutManager == null || !shortcutManager.IsRequestPinShortcutSupported)
                {
                    return Task.FromResult(false);
                }

                var intent = new Intent(context, typeof(MainActivity));
                intent.SetAction(Intent.ActionView);
                intent.PutExtra(ShortcutHelper.ExtraShortcutAction, action);
                intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);

                var iconRes = action switch
                {
                    ShortcutHelper.ActionBookmarks => Resource.Drawable.ic_shortcut_bookmark,
                    ShortcutHelper.ActionDownloads => Resource.Drawable.ic_shortcut_download,
                    ShortcutHelper.ActionSettings => Resource.Drawable.ic_shortcut_settings,
                    _ => Resource.Drawable.ic_shortcut_browse
                };

                var shortcut = new ShortcutInfo.Builder(context, shortcutId)
                    .SetShortLabel(label)
                    .SetLongLabel($"ورود به {label} در کپل‌آرت")
                    .SetIcon(Icon.CreateWithResource(context, iconRes))
                    .SetIntent(intent)
                    .Build();

                var success = shortcutManager.RequestPinShortcut(shortcut, null);
                return Task.FromResult(success);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidHomeScreenService] RequestPinShortcutAsync error: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        public void NotifyWidgetUpdate()
        {
            try
            {
                var context = global::Android.App.Application.Context;
                var appWidgetManager = AppWidgetManager.GetInstance(context);
                if (appWidgetManager == null) return;

                var component = new ComponentName(context, Java.Lang.Class.FromType(typeof(KopolArtWidgetProvider)));
                var widgetIds = appWidgetManager.GetAppWidgetIds(component);
                if (widgetIds != null && widgetIds.Length > 0)
                {
                    foreach (var id in widgetIds)
                    {
                        KopolArtWidgetProvider.UpdateAppWidget(context, appWidgetManager, id);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidHomeScreenService] NotifyWidgetUpdate error: {ex.Message}");
            }
        }
    }
}
