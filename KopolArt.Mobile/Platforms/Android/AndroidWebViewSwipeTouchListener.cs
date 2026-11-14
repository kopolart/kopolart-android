using Android.Views;
using Android.Webkit;
using AView = Android.Views.View;
using AWebView = Android.Webkit.WebView;

namespace KopolArt.Mobile.Platforms.Android
{
    /// <summary>
    /// Native Android touch listener for WebView that reliably intercepts horizontal swipe gestures
    /// for forward and backward navigation, regardless of DOM structure, active links, or page loading states.
    /// </summary>
    public class AndroidWebViewSwipeTouchListener : Java.Lang.Object, AView.IOnTouchListener
    {
        private float _startX;
        private float _startY;
        private long _startTime;
        private bool _isTrackingGesture;
        private bool _disallowedParent;
        private bool _isLockedHorizontal;

        // Constants for tuning swipe responsiveness across all Android devices
        private const float MinSwipeDistanceDp = 48f;     // Minimum horizontal distance in dp for normal deliberate swipe
        private const float MinFlingDistanceDp = 30f;     // Minimum horizontal distance in dp for rapid flick/fling
        private const float MinFlingVelocityDpSec = 320f; // Minimum velocity in dp/s for rapid fling
        private const long MinDurationMs = 40;           // Ignore sub-40ms erratic finger twitches
        private const long MaxDurationMs = 900;          // Allow deliberate human swipes up to 900ms
        private const float DominanceRatio = 1.15f;      // Horizontal distance must be at least 1.15x vertical distance

        public bool OnTouch(AView? v, MotionEvent? e)
        {
            if (v == null || e == null) return false;

            switch (e.ActionMasked)
            {
                case MotionEventActions.Down:
                {
                    _startX = e.RawX;
                    _startY = e.RawY;
                    _startTime = e.EventTime;
                    _isTrackingGesture = true;
                    _disallowedParent = false;
                    _isLockedHorizontal = false;

                    // If user tapped on an active editable text input, do not intercept swipe
                    if (v is AWebView webView)
                    {
                        var hitTest = webView.GetHitTestResult();
                        if (hitTest != null && hitTest.Type == HitTestResult.EditTextType)
                        {
                            _isTrackingGesture = false;
                        }
                    }

                    // Always allow WebView to process Down so standard clicks and vertical scrolls are primed
                    return false;
                }

                case MotionEventActions.Move:
                {
                    if (!_isTrackingGesture) return false;

                    float dx = e.RawX - _startX;
                    float dy = e.RawY - _startY;
                    float absDx = Math.Abs(dx);
                    float absDy = Math.Abs(dy);

                    int touchSlop = v.Context != null ? ViewConfiguration.Get(v.Context)?.ScaledTouchSlop ?? 16 : 16;

                    // If vertical movement dominates early on, this is a vertical page scroll. Disqualify swipe.
                    if (absDy > touchSlop * 2 && absDy > absDx * 1.5f && !_isLockedHorizontal)
                    {
                        _isTrackingGesture = false;
                        return false;
                    }

                    // If horizontal movement emerges and exceeds touch slop, request parent (SwipeRefreshLayout) not to steal touch
                    if (absDx > touchSlop && absDx > absDy * 1.1f)
                    {
                        if (!_disallowedParent)
                        {
                            v.Parent?.RequestDisallowInterceptTouchEvent(true);
                            _disallowedParent = true;
                        }

                        // Once clearly horizontal and passing 2x slop, lock as horizontal swipe
                        if (absDx > touchSlop * 2)
                        {
                            _isLockedHorizontal = true;
                        }
                    }

                    // If locked as horizontal swipe, consume move event so web content does not drag/jitter
                    return _isLockedHorizontal;
                }

                case MotionEventActions.Up:
                {
                    if (!_isTrackingGesture) return false;
                    _isTrackingGesture = false;

                    // Skip if interaction was on an editable input field
                    if (v is AWebView webView)
                    {
                        var hitTest = webView.GetHitTestResult();
                        if (hitTest != null && hitTest.Type == HitTestResult.EditTextType)
                        {
                            return false;
                        }
                    }

                    float totalDx = e.RawX - _startX;
                    float totalDy = e.RawY - _startY;
                    float absDx = Math.Abs(totalDx);
                    float absDy = Math.Abs(totalDy);
                    long duration = e.EventTime - _startTime;

                    float density = v.Resources?.DisplayMetrics?.Density ?? 1f;
                    float absDxDp = absDx / density;
                    float absDyDp = absDy / density;
                    float screenWidth = v.Resources?.DisplayMetrics?.WidthPixels ?? 1080f;

                    float velocityDpPerSec = duration > 0 ? (absDxDp / (duration / 1000f)) : 0f;

                    bool isHorizontal = absDxDp > (absDyDp * DominanceRatio);
                    bool isDurationValid = duration >= MinDurationMs && duration <= MaxDurationMs;
                    bool isDistanceValid = absDxDp >= MinSwipeDistanceDp || (absDxDp >= MinFlingDistanceDp && velocityDpPerSec >= MinFlingVelocityDpSec);

                    if (isHorizontal && isDurationValid && isDistanceValid)
                    {
                        bool isBack = totalDx < 0;    // Right to Left (RTL back / dashboard)
                        bool isForward = totalDx > 0; // Left to Right (forward)

                        // Ensure swipe started from a valid region (avoiding edge-inverted gestures)
                        bool isStartZoneValid = (isBack && _startX >= screenWidth * 0.20f) ||
                                                (isForward && _startX <= screenWidth * 0.80f);

                        if (isStartZoneValid)
                        {
                            // Cancel touch in WebView so underlying link/button is NOT activated upon finger lift
                            var cancelEvent = MotionEvent.Obtain(e.DownTime, e.EventTime, MotionEventActions.Cancel, e.GetX(), e.GetY(), 0);
                            if (cancelEvent != null)
                            {
                                v.OnTouchEvent(cancelEvent);
                                cancelEvent.Recycle();
                            }

                            if (isBack)
                            {
                                NavigationBridge.TriggerSwipeBack();
                            }
                            else
                            {
                                NavigationBridge.TriggerSwipeForward();
                            }

                            return true; // Gesture consumed
                        }
                    }

                    return false;
                }

                case MotionEventActions.Cancel:
                {
                    _isTrackingGesture = false;
                    _disallowedParent = false;
                    _isLockedHorizontal = false;
                    return false;
                }

                default:
                    return false;
            }
        }
    }
}
