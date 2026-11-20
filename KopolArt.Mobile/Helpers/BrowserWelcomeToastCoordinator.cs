namespace KopolArt.Mobile.Helpers
{
    public class BrowserWelcomeToastCoordinator : IDisposable
    {
        private CancellationTokenSource? _welcomeToastCts;
        private IDispatcherTimer? _usageToastTimer;

        public void ShowToast(Border? toastBorder, Label? toastText, Func<string> getUsageTime, IDispatcher? dispatcher)
        {
            if (toastBorder == null || toastText == null) return;

            _welcomeToastCts?.Cancel();
            _welcomeToastCts?.Dispose();
            _welcomeToastCts = new CancellationTokenSource();
            var token = _welcomeToastCts.Token;

            _usageToastTimer?.Stop();

            toastText.Text = $"مدت گردش در سایت: {getUsageTime()}";

            if (dispatcher != null)
            {
                _usageToastTimer = dispatcher.CreateTimer();
                _usageToastTimer.Interval = TimeSpan.FromSeconds(1);
                _usageToastTimer.Tick += (s, e) =>
                {
                    toastText.Text = $"مدت گردش در سایت: {getUsageTime()}";
                };
                _usageToastTimer.Start();
            }

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    toastBorder.Opacity = 0;
                    toastBorder.TranslationY = -60;
                    toastBorder.IsVisible = true;

                    await Task.WhenAll(
                        toastBorder.FadeToAsync(1.0, 350, Easing.CubicOut),
                        toastBorder.TranslateToAsync(0, 0, 350, Easing.CubicOut)
                    );

                    await Task.Delay(6500, token);

                    if (token.IsCancellationRequested) return;

                    _usageToastTimer?.Stop();
                    _usageToastTimer = null;

                    await Task.WhenAll(
                        toastBorder.FadeToAsync(0, 300, Easing.CubicIn),
                        toastBorder.TranslateToAsync(0, -60, 300, Easing.CubicIn)
                    );

                    toastBorder.IsVisible = false;
                }
                catch (TaskCanceledException)
                {
                    _usageToastTimer?.Stop();
                    _usageToastTimer = null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[BrowserWelcomeToastCoordinator] Failed to stop usage toast timer: {ex.Message}");
                }
            });
        }

        public void DismissToast(Border? toastBorder)
        {
            _welcomeToastCts?.Cancel();
            _usageToastTimer?.Stop();
            _usageToastTimer = null;

            if (toastBorder == null) return;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await Task.WhenAll(
                        toastBorder.FadeToAsync(0, 200, Easing.CubicIn),
                        toastBorder.TranslateToAsync(0, -60, 200, Easing.CubicIn)
                    );
                    toastBorder.IsVisible = false;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[BrowserWelcomeToastCoordinator] Failed to hide welcome toast: {ex.Message}");
                }
            });
        }

        public void Dispose()
        {
            _usageToastTimer?.Stop();
            _usageToastTimer = null;

            _welcomeToastCts?.Cancel();
            _welcomeToastCts?.Dispose();
            _welcomeToastCts = null;
        }
    }
}
