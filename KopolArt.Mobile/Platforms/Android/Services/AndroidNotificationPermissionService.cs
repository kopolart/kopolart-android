using AndroidX.Core.Content;
using Android.Content.PM;
using KopolArt.Mobile.Services;

namespace KopolArt.Mobile.Platforms.Android.Services
{
    public class AndroidNotificationPermissionService : INotificationPermissionService
    {
        public Task<bool> IsNotificationPermissionGrantedAsync()
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                // Prior to Android 13 (API 33), notification permission is granted by default at install time
                return Task.FromResult(true);
            }

            var context = global::Android.App.Application.Context;
            var granted = ContextCompat.CheckSelfPermission(
                context,
                global::Android.Manifest.Permission.PostNotifications) == Permission.Granted;

            return Task.FromResult(granted);
        }

        public Task<bool> RequestNotificationPermissionAsync()
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                return Task.FromResult(true);
            }

            var tcs = new TaskCompletionSource<bool>();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                MainActivity.RequestNotificationPermission(granted =>
                {
                    tcs.TrySetResult(granted);
                });
            });

            return tcs.Task;
        }
    }
}
