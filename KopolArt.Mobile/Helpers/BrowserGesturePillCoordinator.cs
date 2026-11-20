namespace KopolArt.Mobile.Helpers
{
    /// <summary>
    /// Coordinates swipe navigation gesture pill indicator animations.
    /// </summary>
    public class BrowserGesturePillCoordinator : IDisposable
    {
        private CancellationTokenSource? _gesturePillCts;

        public async Task ShowGesturePillAsync(
            bool isBack,
            bool isReturningToDashboard,
            Border? backGesturePill,
            Border? forwardGesturePill,
            Label? backGestureLabel,
            Label? backGestureIcon)
        {
            try
            {
                _gesturePillCts?.Cancel();
                _gesturePillCts?.Dispose();
                _gesturePillCts = new CancellationTokenSource();
                var token = _gesturePillCts.Token;

                var pill = isBack ? backGesturePill : forwardGesturePill;
                var otherPill = isBack ? forwardGesturePill : backGesturePill;
                if (otherPill != null) otherPill.IsVisible = false;

                if (pill == null) return;

                if (isBack)
                {
                    if (backGestureLabel != null) backGestureLabel.Text = isReturningToDashboard ? "داشبورد برنامه" : "صفحه قبل";
                    if (backGestureIcon != null) backGestureIcon.Text = isReturningToDashboard ? "⌂" : "›";
                    pill.TranslationX = 60;
                }
                else
                {
                    pill.TranslationX = -60;
                }

                pill.Opacity = 0;
                pill.Scale = 0.85;
                pill.IsVisible = true;

                await Task.WhenAll(
                    pill.FadeToAsync(1, 150, Easing.CubicOut),
                    pill.TranslateToAsync(0, pill.TranslationY, 170, Easing.SpringOut),
                    pill.ScaleToAsync(1.0, 170, Easing.CubicOut)
                );

                if (token.IsCancellationRequested) return;

                await Task.Delay(380, token);

                if (token.IsCancellationRequested) return;

                double targetX = isBack ? 40 : -40;
                await Task.WhenAll(
                    pill.FadeToAsync(0, 180, Easing.CubicIn),
                    pill.TranslateToAsync(targetX, pill.TranslationY, 180, Easing.CubicIn),
                    pill.ScaleToAsync(0.9, 180, Easing.CubicIn)
                );

                if (!token.IsCancellationRequested)
                {
                    pill.IsVisible = false;
                }
            }
            catch (TaskCanceledException) { /* expected: pill hide timer cancelled */ }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BrowserGesturePillCoordinator] Gesture pill animation error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _gesturePillCts?.Cancel();
            _gesturePillCts?.Dispose();
            _gesturePillCts = null;
        }
    }
}
