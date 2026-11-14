using Android.Content;
using Android.OS;
using KopolArt.Mobile.Models;
using KopolArt.Mobile.Services;

namespace KopolArt.Mobile.Platforms.Android
{
    public class AndroidAppInstaller : IAppInstaller
    {
        public bool CanInstallDirectly()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var context = global::Android.App.Application.Context;
                return context.PackageManager?.CanRequestPackageInstalls() ?? false;
            }
            return true;
        }

        public void RequestInstallPermission()
        {
            OpenUnknownSourcesSettings(global::Android.App.Application.Context);
        }

        public void Install(string localFilePath)
        {
            try
            {
                var context = global::Android.App.Application.Context;
                var file = new Java.IO.File(localFilePath);

                if (!file.Exists())
                {
                    // Fallback to cache directory file if relative name was passed
                    file = new Java.IO.File(context.CacheDir, Path.GetFileName(localFilePath));
                }

                if (!file.Exists())
                {
                    throw new FileNotFoundException("APK file was not found", localFilePath);
                }

                // Check install permission for Android O+ (API 26+)
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    if (context.PackageManager != null && !context.PackageManager.CanRequestPackageInstalls())
                    {
                        OpenUnknownSourcesSettings(context);

                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            var alertService = IPlatformApplication.Current?.Services.GetService<ICustomAlertService>();
                            if (alertService != null)
                            {
                                await alertService.ShowAlertAsync(
                                    "مجوز نصب برنامه‌ها",
                                    "برای نصب به‌روزرسانی، لطفاً در بخش تنظیمات اجازه نصب برنامه را به کپل‌آرت بدهید و دوباره امتحان کنید.",
                                    "متوجه شدم",
                                    AlertType.Warning);
                            }
                        });
                        return;
                    }
                }

                var apkUri = AndroidX.Core.Content.FileProvider.GetUriForFile(
                    context,
                    $"{context.PackageName}.fileprovider",
                    file);

                var intent = new Intent(Intent.ActionView);
                intent.SetDataAndType(apkUri, "application/vnd.android.package-archive");
                intent.AddFlags(ActivityFlags.NewTask);
                intent.AddFlags(ActivityFlags.GrantReadUriPermission);

                if (Platform.CurrentActivity != null)
                {
                    Platform.CurrentActivity.StartActivity(intent);
                }
                else
                {
                    context.StartActivity(intent);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidAppInstaller] Error installing APK: {ex}");
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    var alertService = IPlatformApplication.Current?.Services.GetService<ICustomAlertService>();
                    if (alertService != null)
                    {
                        await alertService.ShowAlertAsync(
                            "خطا در نصب",
                            "اجرای برنامه نصب‌کننده با خطا مواجه شد.",
                            "تأیید",
                            AlertType.Error);
                    }
                });
            }
        }

        private static void OpenUnknownSourcesSettings(Context context)
        {
            try
            {
                var settingsIntent = new Intent(global::Android.Provider.Settings.ActionManageUnknownAppSources);
                settingsIntent.SetData(global::Android.Net.Uri.Parse($"package:{context.PackageName}"));
                settingsIntent.AddFlags(ActivityFlags.NewTask);

                if (Platform.CurrentActivity != null)
                {
                    Platform.CurrentActivity.StartActivity(settingsIntent);
                }
                else
                {
                    context.StartActivity(settingsIntent);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidAppInstaller] Error starting unknown apps settings: {ex}");
                // Fallback to general settings
                try
                {
                    var generalSettingsIntent = new Intent(global::Android.Provider.Settings.ActionSettings);
                    generalSettingsIntent.AddFlags(ActivityFlags.NewTask);
                    if (Platform.CurrentActivity != null)
                        Platform.CurrentActivity.StartActivity(generalSettingsIntent);
                    else
                        context.StartActivity(generalSettingsIntent);
                }
                catch { /* ignore */ }
            }
        }
    }
}
