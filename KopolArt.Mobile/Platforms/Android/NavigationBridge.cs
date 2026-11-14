using Android.Webkit;
using Java.Interop;

namespace KopolArt.Mobile.Platforms.Android
{
    public class NavigationBridge : Java.Lang.Object
    {
        public const string InterfaceName = "NavigationBridge";

        public static event Action<string?>? DoubleTapped;
        public static event Action<string?>? UrlChanged;
        public static event Action? LongTapped;
        public static event Action? SwipeBackTriggered;
        public static event Action? SwipeForwardTriggered;

        private static long _lastLongTapTimestamp;
        private static long _lastSwipeBackTimestamp;
        private static long _lastSwipeForwardTimestamp;

        public static void TriggerLongTap()
        {
            var now = Environment.TickCount64;
            if (now - _lastLongTapTimestamp < 1500) return;
            _lastLongTapTimestamp = now;
            LongTapped?.Invoke();
        }

        public static void TriggerSwipeBack()
        {
            var now = Environment.TickCount64;
            if (now - _lastSwipeBackTimestamp < 450) return;
            _lastSwipeBackTimestamp = now;
            SwipeBackTriggered?.Invoke();
        }

        public static void TriggerSwipeForward()
        {
            var now = Environment.TickCount64;
            if (now - _lastSwipeForwardTimestamp < 450) return;
            _lastSwipeForwardTimestamp = now;
            SwipeForwardTriggered?.Invoke();
        }

        public static void ReportUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url) || url.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
                return;

            UrlChanged?.Invoke(url);
        }

        [JavascriptInterface]
        [Export("onDoubleTap")]
        public void OnDoubleTap()
        {
            DoubleTapped?.Invoke(null);
        }

        [JavascriptInterface]
        [Export("onDoubleTapWithUrl")]
        public void OnDoubleTapWithUrl(string? url)
        {
            DoubleTapped?.Invoke(url);
        }

        [JavascriptInterface]
        [Export("onUrlChanged")]
        public void OnUrlChanged(string? url)
        {
            ReportUrl(url);
        }

        [JavascriptInterface]
        [Export("onLongTap")]
        public void OnLongTap()
        {
            TriggerLongTap();
        }

        [JavascriptInterface]
        [Export("onSwipeBack")]
        public void OnSwipeBack()
        {
            TriggerSwipeBack();
        }

        [JavascriptInterface]
        [Export("onSwipeForward")]
        public void OnSwipeForward()
        {
            TriggerSwipeForward();
        }
    }
}
