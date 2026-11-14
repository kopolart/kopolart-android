namespace KopolArt.Mobile.Models
{
    public class AlertCardContext
    {
        public Grid RootGrid { get; init; } = null!;
        public Grid OverlayGrid { get; init; } = null!;
        public Border Backdrop { get; init; } = null!;
        public Border Card { get; init; } = null!;
        public VerticalStackLayout CardStack { get; init; } = null!;
        public bool IsDark { get; init; }
        public ContentPage? Page { get; init; }
        public EventHandler? SizeChangedHandler { get; set; }

        private bool _isDismissing;

        public async Task DismissAsync(Action? onComplete = null)
        {
            if (_isDismissing) return;
            _isDismissing = true;
            Backdrop.InputTransparent = true;

            if (Page != null && SizeChangedHandler != null)
            {
                Page.SizeChanged -= SizeChangedHandler;
            }

            try
            {
                await Task.WhenAll(
                    Card.ScaleToAsync(0.85, 150, Easing.CubicIn),
                    Card.FadeToAsync(0, 150),
                    Backdrop.FadeToAsync(0, 150)
                );
                RootGrid.Children.Remove(OverlayGrid);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AlertCardContext] Dismiss error: {ex.Message}");
                RootGrid.Children.Remove(OverlayGrid);
            }
            finally
            {
                onComplete?.Invoke();
            }
        }

        public async Task AnimateInAsync()
        {
            RootGrid.Children.Add(OverlayGrid);
            await Task.WhenAll(
                Backdrop.FadeToAsync(1, 200),
                Card.ScaleToAsync(1.0, 250, Easing.SpringOut),
                Card.FadeToAsync(1, 200)
            );
        }
    }
}
