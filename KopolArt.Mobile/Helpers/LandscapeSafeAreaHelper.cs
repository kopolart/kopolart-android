namespace KopolArt.Mobile.Helpers
{
    /// <summary>
    /// Ensures XAML ContentPages maintain safe clearance from hardware display cutouts (notches / punch-hole cameras)
    /// and screen curves when the device is rotated horizontally (Landscape mode).
    /// In Portrait mode, it leaves page padding at 0 so standard portrait layout is 100% preserved.
    /// Also guarantees that custom Persian typography and font icons are consistently refreshed across orientation changes.
    /// </summary>
    public static class LandscapeSafeAreaHelper
    {
        /// <summary>
        /// Minimum safe horizontal padding (in dp) applied on both sides in landscape mode.
        /// Modern smartphone cutouts/punch-holes typically extend 32-48dp from the edge.
        /// 56dp provides generous clearance preventing any icon, card corner, or button from being cut.
        /// </summary>
        public const double DefaultLandscapeHorizontalPadding = 56.0;

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ContentPage, object> AttachedPages = new();

        /// <summary>
        /// Attaches orientation and size change monitoring to a ContentPage.
        /// Call once in the page constructor: LandscapeSafeAreaHelper.Attach(this);
        /// Set applySafePadding to false for full-bleed pages like WebView that handle their own safe areas.
        /// </summary>
        public static void Attach(ContentPage page, bool applySafePadding = true)
        {
            if (page == null) return;
            lock (AttachedPages)
            {
                if (AttachedPages.TryGetValue(page, out _)) return;
                AttachedPages.Add(page, true);
            }

            void Update()
            {
                if (MainThread.IsMainThread)
                {
                    if (applySafePadding)
                    {
                        ApplySafePadding(page);
                    }
                    else if (page.Padding.Left != 0 || page.Padding.Right != 0 || page.Padding.Top != 0 || page.Padding.Bottom != 0)
                    {
                        page.Padding = new Thickness(0);
                    }
                    RefreshPageFonts(page);
                }
                else
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (applySafePadding)
                        {
                            ApplySafePadding(page);
                        }
                        else if (page.Padding.Left != 0 || page.Padding.Right != 0 || page.Padding.Top != 0 || page.Padding.Bottom != 0)
                        {
                            page.Padding = new Thickness(0);
                        }
                        RefreshPageFonts(page);
                    });
                }
            }

            page.SizeChanged += (s, e) => Update();
            page.Loaded += (s, e) => Update();
            page.Appearing += (s, e) => Update();

            // Initial check if dimensions are already available
            if (page.Width > 0 && page.Height > 0)
            {
                Update();
            }
        }

        /// <summary>
        /// Applies safe padding to the ContentPage depending on orientation and display cutout metrics.
        /// </summary>
        public static void ApplySafePadding(ContentPage page)
        {
            if (page == null || page.Width <= 0 || page.Height <= 0) return;

            bool isLandscape = page.Width > page.Height;
            if (!isLandscape)
            {
                // In portrait, leave page padding at 0 so individual XAML layouts control their own padding
                if (page.Padding.Left != 0 || page.Padding.Right != 0)
                {
                    page.Padding = new Thickness(0);
                }
                return;
            }

            // In landscape, get physical cutout insets and existing system insets if running on Android
            double cutoutLeft = 0;
            double cutoutRight = 0;
            double systemLeft = 0;
            double systemRight = 0;

#if ANDROID
            try
            {
                var insets = Platforms.Android.MainActivity.GetLandscapeInsetsDp();
                cutoutLeft = insets.cutoutLeft;
                cutoutRight = insets.cutoutRight;
                systemLeft = insets.systemLeftInset;
                systemRight = insets.systemRightInset;
            }
            catch
            {
                // Fall back to default safe padding below
            }
#endif

            // Target physical clearance from the phone's physical screen edges:
            // 1. Must safely clear hardware camera cutouts (cutout + 20dp)
            // 2. Must maintain at least DefaultLandscapeHorizontalPadding (56dp)
            double minPhysicalLeft = Math.Max(cutoutLeft > 0 ? cutoutLeft + 20.0 : 0, DefaultLandscapeHorizontalPadding);
            double minPhysicalRight = Math.Max(cutoutRight > 0 ? cutoutRight + 20.0 : 0, DefaultLandscapeHorizontalPadding);

            // To guarantee that content is 100% horizontally centered on the physical device screen,
            // both physical edge margins must be equal:
            double symmetricTargetPhysicalMargin = Math.Max(minPhysicalLeft, minPhysicalRight);

            // Calculate the padding needed on the MAUI ContentPage, accounting for any
            // offsets already applied by Android (such as the system navigation bar on the right or left):
            double pagePaddingLeft = Math.Max(0, symmetricTargetPhysicalMargin - systemLeft);
            double pagePaddingRight = Math.Max(0, symmetricTargetPhysicalMargin - systemRight);

            var newPadding = new Thickness(pagePaddingLeft, 0, pagePaddingRight, 0);
            if (page.Padding != newPadding)
            {
                page.Padding = newPadding;
            }
        }

        /// <summary>
        /// Recursively traverses the visual element tree of a page or container and re-applies
        /// custom Persian typography and font icons to all Label, Button, Entry, and Editor controls.
        /// </summary>
        public static void RefreshPageFonts(Element? root)
        {
            if (root == null) return;
            try
            {
                WalkTreeAndApplyFonts(root);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LandscapeSafeAreaHelper] RefreshPageFonts error: {ex.Message}");
            }
        }

        private static void WalkTreeAndApplyFonts(Element element)
        {
#if ANDROID
            RefreshElementFont(element);
#endif

            if (element is IVisualTreeElement vte)
            {
                var children = vte.GetVisualChildren();
                if (children != null)
                {
                    foreach (var child in children)
                    {
                        if (child is Element childElement)
                        {
                            WalkTreeAndApplyFonts(childElement);
                        }
                    }
                }
            }
        }

#if ANDROID
        private static void RefreshElementFont(Element element)
        {
            try
            {
                if (element is Label label)
                {
                    if (label.Handler?.PlatformView is Android.Widget.TextView tv)
                    {
                        Platforms.Android.TypefaceCache.ApplyTypeface(tv, label.FontFamily, label.FontAttributes);
                    }
                }
                else if (element is Button button)
                {
                    if (button.Handler?.PlatformView is Android.Widget.TextView tv)
                    {
                        Platforms.Android.TypefaceCache.ApplyTypeface(tv, button.FontFamily, button.FontAttributes);
                    }
                }
                else if (element is Entry entry)
                {
                    if (entry.Handler?.PlatformView is Android.Widget.TextView tv)
                    {
                        Platforms.Android.TypefaceCache.ApplyTypeface(tv, entry.FontFamily, entry.FontAttributes);
                    }
                }
                else if (element is Editor editor)
                {
                    if (editor.Handler?.PlatformView is Android.Widget.TextView tv)
                    {
                        Platforms.Android.TypefaceCache.ApplyTypeface(tv, editor.FontFamily, editor.FontAttributes);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RefreshElementFont] Error: {ex.Message}");
            }
        }
#endif
    }
}
