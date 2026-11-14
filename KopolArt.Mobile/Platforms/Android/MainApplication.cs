using Android.App;
using Android.Runtime;

namespace KopolArt.Mobile.Platforms.Android
{
    [Application]
    public class MainApplication(IntPtr handle, JniHandleOwnership ownership) : MauiApplication(handle, ownership)
    {
        public override void OnCreate()
        {
            base.OnCreate();

            try
            {
                var themeMode = Preferences.Get("AppThemeMode", "System");
                if (themeMode == "Dark")
                {
                    AndroidX.AppCompat.App.AppCompatDelegate.DefaultNightMode = AndroidX.AppCompat.App.AppCompatDelegate.ModeNightYes;
                }
                else if (themeMode == "Light")
                {
                    AndroidX.AppCompat.App.AppCompatDelegate.DefaultNightMode = AndroidX.AppCompat.App.AppCompatDelegate.ModeNightNo;
                }
                else
                {
                    AndroidX.AppCompat.App.AppCompatDelegate.DefaultNightMode = AndroidX.AppCompat.App.AppCompatDelegate.ModeNightFollowSystem;
                }
            }
            catch
            {
                // ignored
            }
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}