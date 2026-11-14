namespace KopolArt.Mobile.Views;

public partial class SocialPromoPopup : ContentView
{
    public SocialPromoPopup()
    {
        InitializeComponent();
        SizeChanged += OnPopupSizeChanged;
    }

    private void OnPopupSizeChanged(object? sender, EventArgs e)
    {
        if (Height > 0)
        {
            bool isLandscape = Width > Height;
            PromoCard.MaximumHeightRequest = isLandscape
                ? Math.Max(200, Height - 32)
                : Math.Min(420, Height - 48);
        }
    }

    public void Show()
    {
        IsVisible = true;
        Opacity = 0;
        var showAnim = new Animation(v => Opacity = v, 0, 1, Easing.CubicOut);
        showAnim.Commit(this, "FadeIn", length: 350);
    }

    public event EventHandler? Closed;

    public void Hide()
    {
        var hideAnim = new Animation(v => Opacity = v, 1, 0, Easing.CubicIn);
        hideAnim.Commit(this, "FadeOut", length: 250, finished: (_, _) =>
        {
            IsVisible = false;
            Closed?.Invoke(this, EventArgs.Empty);
        });
    }

    private void OnCloseTapped(object? sender, TappedEventArgs e) => Hide();

    private void OnBackdropTapped(object? sender, TappedEventArgs e) => Hide();

    /// <summary>
    /// Opens a social link in its native app, falling back to the browser.
    /// Shared by all social button handlers.
    /// </summary>
    private static async Task OpenSocialAsync(string url)
    {
        try
        {
            await Launcher.OpenAsync(new Uri(url));
        }
        catch
        {
            // Fallback: open in browser
            try
            {
                await Browser.OpenAsync(url, BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open social url '{url}': {ex.Message}");
            }
        }
    }

    private async void OnInstagramTapped(object? sender, TappedEventArgs e)
        => await OpenSocialAsync(Constants.Urls.Instagram);

    private async void OnTelegramTapped(object? sender, TappedEventArgs e)
        => await OpenSocialAsync(Constants.Urls.Telegram);

    private async void OnEitaTapped(object? sender, TappedEventArgs e)
        => await OpenSocialAsync(Constants.Urls.Eita);

    private async void OnBaleTapped(object? sender, TappedEventArgs e)
        => await OpenSocialAsync(Constants.Urls.Bale);
}
