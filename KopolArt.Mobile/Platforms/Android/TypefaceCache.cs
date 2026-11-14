#if ANDROID
using Android.Graphics;
using Android.Widget;

namespace KopolArt.Mobile.Platforms.Android
{
    /// <summary>
    /// Caches and applies native Android Typefaces directly from APK assets to ensure
    /// custom Persian typography and font icons never unload during orientation changes or theme updates.
    /// </summary>
    public static class TypefaceCache
    {
        private static Typeface? _abarBlack;
        private static Typeface? _abarBold;
        private static Typeface? _abarMedium;
        private static Typeface? _kopolArtFa;
        private static bool _initialized;
        private static readonly object InitLock = new();

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<TextView, FontPreservingLayoutChangeListener> _attachedViews = new();

        public static void Initialize()
        {
            if (_initialized) return;
            lock (InitLock)
            {
                if (_initialized) return;
                try
                {
                    var context = global::Android.App.Application.Context;
                    var assets = context?.Assets;
                    if (assets != null)
                    {
                        _abarBlack = LoadAssetSafe(assets, Constants.Fonts.UiFontBlackFile);
                        _abarBold = LoadAssetSafe(assets, Constants.Fonts.UiFontBoldFile);
                        _abarMedium = LoadAssetSafe(assets, Constants.Fonts.UiFontMediumFile);
                        _kopolArtFa = LoadAssetSafe(assets, Constants.Fonts.KopolArtFaFont);
                        _initialized = true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[TypefaceCache] Initialization error: {ex.Message}");
                }
            }
        }

        private static Typeface? LoadAssetSafe(global::Android.Content.Res.AssetManager assets, string filename)
        {
            try
            {
                return Typeface.CreateFromAsset(assets, filename);
            }
            catch
            {
                try
                {
                    return Typeface.CreateFromAsset(assets, $"Fonts/{filename}");
                }
                catch
                {
                    try
                    {
                        return Typeface.CreateFromAsset(assets, $"fonts/{filename}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[TypefaceCache] Failed to load {filename}: {ex.Message}");
                        return null;
                    }
                }
            }
        }

        public static Typeface? GetTypeface(string? fontFamily, FontAttributes attributes = FontAttributes.None)
        {
            if (!_initialized)
            {
                Initialize();
            }

            if (string.IsNullOrWhiteSpace(fontFamily))
            {
                // Default app font is UiFontMediumFile (per Styles.xaml)
                return (attributes & FontAttributes.Bold) != 0 ? (_abarBold ?? _abarMedium) : _abarMedium;
            }

            // 1. Icon font check (Priority 1: must never resolve to text font)
            if (fontFamily.Equals(Constants.Fonts.KopolArtFaAlias, StringComparison.OrdinalIgnoreCase) ||
                fontFamily.Equals(Constants.Fonts.KopolArtFaFont, StringComparison.OrdinalIgnoreCase) ||
                fontFamily.IndexOf("kopol", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return _kopolArtFa;
            }

            // 2. Black font check
            if (fontFamily.Equals(Constants.Fonts.UiFontBlackAlias, StringComparison.OrdinalIgnoreCase) ||
                fontFamily.Equals(Constants.Fonts.UiFontBlackFile, StringComparison.OrdinalIgnoreCase) ||
                fontFamily.IndexOf("black", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return _abarBlack ?? _abarBold ?? _abarMedium;
            }

            // 3. Bold font check
            if (fontFamily.Equals(Constants.Fonts.UiFontBoldAlias, StringComparison.OrdinalIgnoreCase) ||
                fontFamily.Equals(Constants.Fonts.UiFontBoldFile, StringComparison.OrdinalIgnoreCase) ||
                fontFamily.IndexOf("bold", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return _abarBold ?? _abarMedium;
            }

            // 4. Medium / regular font check
            if (fontFamily.Equals(Constants.Fonts.UiFontMediumAlias, StringComparison.OrdinalIgnoreCase) ||
                fontFamily.Equals(Constants.Fonts.UiFontRegularAlias, StringComparison.OrdinalIgnoreCase) ||
                fontFamily.Equals(Constants.Fonts.UiFontMediumFile, StringComparison.OrdinalIgnoreCase) ||
                fontFamily.IndexOf("medium", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fontFamily.IndexOf("regular", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fontFamily.IndexOf("abar", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return (attributes & FontAttributes.Bold) != 0 ? (_abarBold ?? _abarMedium) : _abarMedium;
            }

            return (attributes & FontAttributes.Bold) != 0 ? (_abarBold ?? _abarMedium) : _abarMedium;
        }

        public static void ApplyTypeface(TextView? textView, string? fontFamily, FontAttributes attributes = FontAttributes.None)
        {
            if (textView == null) return;
            try
            {
                var tf = GetTypeface(fontFamily, attributes);
                if (tf != null && !ReferenceEquals(textView.Typeface, tf))
                {
                    textView.Typeface = tf;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TypefaceCache] ApplyTypeface error: {ex.Message}");
            }
        }

        public static void AttachFontPreserver(TextView? textView, Func<string?> getFontFamily, Func<FontAttributes> getFontAttributes)
        {
            if (textView == null) return;
            lock (_attachedViews)
            {
                if (_attachedViews.TryGetValue(textView, out _))
                {
                    return; // Already attached
                }

                var listener = new FontPreservingLayoutChangeListener(textView, getFontFamily, getFontAttributes);
                textView.AddOnLayoutChangeListener(listener);
                _attachedViews.Add(textView, listener);
            }
        }

        private class FontPreservingLayoutChangeListener : Java.Lang.Object, global::Android.Views.View.IOnLayoutChangeListener
        {
            private readonly WeakReference<TextView> _textViewRef;
            private readonly Func<string?> _getFontFamily;
            private readonly Func<FontAttributes> _getFontAttributes;

            public FontPreservingLayoutChangeListener(TextView textView, Func<string?> getFontFamily, Func<FontAttributes> getFontAttributes)
            {
                _textViewRef = new WeakReference<TextView>(textView);
                _getFontFamily = getFontFamily;
                _getFontAttributes = getFontAttributes;
            }

            public void OnLayoutChange(global::Android.Views.View? v, int left, int top, int right, int bottom, int oldLeft, int oldTop, int oldRight, int oldBottom)
            {
                if (_textViewRef.TryGetTarget(out var textView))
                {
                    ApplyTypeface(textView, _getFontFamily(), _getFontAttributes());
                }
            }
        }
    }
}
#endif
