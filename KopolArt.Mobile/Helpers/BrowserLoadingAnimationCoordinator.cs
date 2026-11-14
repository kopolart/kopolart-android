namespace KopolArt.Mobile.Helpers
{
    /// <summary>
    /// Coordinates loading pulse and animated dots for the browser page.
    /// Also manages loading view orientation adjustments between portrait and landscape.
    /// </summary>
    public class BrowserLoadingAnimationCoordinator : IDisposable
    {
        private IDispatcherTimer? _dotsTimer;
        private int _dotsIndex;
        private static readonly string[] DotFrames = [".", "..", "..."];

        public void UpdateLoadingViewOrientation(
            double width,
            double height,
            Image? loadingLogo,
            Label? loadingLabel,
            VerticalStackLayout? loadingContentStack,
            VerticalStackLayout? welcomeLoadingCard,
            Label? welcomeUserNameLabel,
            Label? welcomeTitleLabel,
            Label? welcomeSubtitleLabel,
            Label? appVersionLabel)
        {
            if (width <= 0 || height <= 0) return;

            bool isLandscape = width > height;

            if (isLandscape)
            {
                if (loadingLogo != null) { loadingLogo.WidthRequest = 96; loadingLogo.HeightRequest = 96; }
                if (loadingLabel != null) loadingLabel.FontSize = 24;
                if (loadingContentStack != null) loadingContentStack.Spacing = 6;
                if (welcomeLoadingCard != null) welcomeLoadingCard.Spacing = 2;
                if (welcomeUserNameLabel != null) welcomeUserNameLabel.FontSize = 13;
                if (welcomeTitleLabel != null) welcomeTitleLabel.FontSize = 13;
                if (welcomeSubtitleLabel != null) welcomeSubtitleLabel.FontSize = 11;
                if (appVersionLabel != null) appVersionLabel.Margin = new Thickness(0, 0, 0, 6);
            }
            else
            {
                if (loadingLogo != null) { loadingLogo.WidthRequest = 220; loadingLogo.HeightRequest = 220; }
                if (loadingLabel != null) loadingLabel.FontSize = 36;
                if (loadingContentStack != null) loadingContentStack.Spacing = 14;
                if (welcomeLoadingCard != null) welcomeLoadingCard.Spacing = 4;
                if (welcomeUserNameLabel != null) welcomeUserNameLabel.FontSize = 16;
                if (welcomeTitleLabel != null) welcomeTitleLabel.FontSize = 16;
                if (welcomeSubtitleLabel != null) welcomeSubtitleLabel.FontSize = 13;
                if (appVersionLabel != null) appVersionLabel.Margin = new Thickness(0, 0, 0, 15);
            }
        }

        public void StartLoadingAnimation(Label? loadingLabel, IDispatcher? dispatcher)
        {
            StartPulseAnimation(loadingLabel);
            StartDotsAnimation(loadingLabel, dispatcher);
        }

        public void StopLoadingAnimation(Label? loadingLabel)
        {
            StopPulseAnimation(loadingLabel);
            StopDotsAnimation(loadingLabel);
        }

        private void StartDotsAnimation(Label? loadingLabel, IDispatcher? dispatcher)
        {
            if (loadingLabel == null || dispatcher == null) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_dotsTimer == null)
                {
                    _dotsTimer = dispatcher.CreateTimer();
                    _dotsTimer.Interval = TimeSpan.FromMilliseconds(400);
                    _dotsTimer.Tick += (s, e) =>
                    {
                        _dotsIndex = (_dotsIndex + 1) % DotFrames.Length;
                        loadingLabel.Text = DotFrames[_dotsIndex];
                    };
                }
                _dotsIndex = 0;
                loadingLabel.Text = DotFrames[0];
                if (!_dotsTimer.IsRunning)
                {
                    _dotsTimer.Start();
                }
            });
        }

        private void StopDotsAnimation(Label? loadingLabel)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _dotsTimer?.Stop();
                if (loadingLabel != null) loadingLabel.Text = "...";
            });
        }

        private static void StartPulseAnimation(Label? loadingLabel)
        {
            if (loadingLabel == null) return;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                loadingLabel.CancelAnimations();
                var pulseAnimation = new Animation();
                var pulseIn = new Animation(v => loadingLabel.Opacity = v, 0.4, 1.0, Easing.CubicInOut);
                var pulseOut = new Animation(v => loadingLabel.Opacity = v, 1.0, 0.4, Easing.CubicInOut);
                pulseAnimation.Add(0, 0.5, pulseIn);
                pulseAnimation.Add(0.5, 1.0, pulseOut);
                pulseAnimation.Commit(loadingLabel, "PulseAnimation", length: 2000, repeat: () => true);
            });
        }

        private static void StopPulseAnimation(Label? loadingLabel)
        {
            if (loadingLabel == null) return;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                loadingLabel.CancelAnimations();
                loadingLabel.Opacity = 1.0;
            });
        }

        public void Dispose()
        {
            _dotsTimer?.Stop();
            _dotsTimer = null;
        }
    }
}
