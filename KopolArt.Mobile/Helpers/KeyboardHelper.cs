namespace KopolArt.Mobile.Helpers
{
    public static class KeyboardHelper
    {
        public static void HideKeyboard(VisualElement? visualElement = null)
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        visualElement?.Unfocus();

#if ANDROID
                        var activity = Platform.CurrentActivity;
                        if (activity != null)
                        {
                            var imm = (Android.Views.InputMethods.InputMethodManager?)activity.GetSystemService(Android.Content.Context.InputMethodService);
                            var currentFocus = activity.CurrentFocus;
                            if (imm != null && currentFocus != null)
                            {
                                imm.HideSoftInputFromWindow(currentFocus.WindowToken, Android.Views.InputMethods.HideSoftInputFlags.None);
                            }
                            else if (imm != null && activity.Window?.DecorView != null)
                            {
                                imm.HideSoftInputFromWindow(activity.Window.DecorView.WindowToken, Android.Views.InputMethods.HideSoftInputFlags.None);
                            }
                        }
#endif
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[KeyboardHelper] Inner hide error: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[KeyboardHelper] Hide keyboard error: {ex.Message}");
            }
        }
    }
}
