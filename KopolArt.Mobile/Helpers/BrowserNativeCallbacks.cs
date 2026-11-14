#if ANDROID
namespace KopolArt.Mobile.Helpers
{
    public sealed class SaveWebArchiveValueCallback : Java.Lang.Object, Android.Webkit.IValueCallback
    {
        private readonly Action<string?> _callback;
        public SaveWebArchiveValueCallback(Action<string?> callback) => _callback = callback;
        public void OnReceiveValue(Java.Lang.Object? value)
        {
            _callback(value?.ToString());
        }
    }

    public sealed class WebViewFindListener : Java.Lang.Object, Android.Webkit.WebView.IFindListener
    {
        private readonly Action<int, int, bool> _onResult;
        public WebViewFindListener(Action<int, int, bool> onResult) => _onResult = onResult;
        public void OnFindResultReceived(int activeMatchOrdinal, int numberOfMatches, bool isDoneCounting)
        {
            _onResult(activeMatchOrdinal, numberOfMatches, isDoneCounting);
        }
    }

    public sealed class WebViewChildScrollUpCallback : Java.Lang.Object, AndroidX.SwipeRefreshLayout.Widget.SwipeRefreshLayout.IOnChildScrollUpCallback
    {
        private readonly Func<bool> _canChildScrollUp;
        public WebViewChildScrollUpCallback(Func<bool> canChildScrollUp) => _canChildScrollUp = canChildScrollUp;
        public bool CanChildScrollUp(AndroidX.SwipeRefreshLayout.Widget.SwipeRefreshLayout parent, Android.Views.View? child)
        {
            return _canChildScrollUp();
        }
    }
}
#endif
