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
                        var (black, bold, medium) = LoadUiFamily(
                            assets,
                            Constants.Fonts.UiFontBlackFile,
                            Constants.Fonts.UiFontBoldFile,
                            Constants.Fonts.UiFontMediumFile);

                        if (medium != null)
                        {
                            bold ??= medium;
                            black ??= bold ?? medium;
                        }
                        else
                        {
                            // The APK carries the other family than the one compiled into the
                            // app (a stale build): use what is actually packaged instead of
                            // silently dropping to the system font.
                            var alternate = AlternateFamily;
                            (black, bold, medium) = LoadUiFamily(assets, alternate.Black, alternate.Bold, alternate.Medium);
                            if (medium != null)
                            {
                                bold ??= medium;
                                black ??= bold ?? medium;
                            }
                            System.Diagnostics.Debug.WriteLine("[TypefaceCache] Compiled UI family missing from APK assets; using the packaged family.");
                        }

                        _abarBlack = black ?? bold ?? medium;
                        _abarBold = bold ?? medium;
                        _abarMedium = medium;
                        _kopolArtFa = LoadAssetSafe(assets, Constants.Fonts.KopolArtFaFont);
                        _initialized = true;

                        global::Android.Util.Log.Info("KopolArtFonts",
                            $"Loaded: black={_abarBlack != null} bold={_abarBold != null} medium={_abarMedium != null} icons={_kopolArtFa != null} (compiled family: {Constants.Fonts.UiFontMediumFile})");

                        if (medium == null)
                        {
                            var rootAssets = assets.List("") ?? Array.Empty<string>();
                            global::Android.Util.Log.Warn("KopolArtFonts", "No UI font loaded. APK asset root: " + string.Join(", ", rootAssets));
                            System.Diagnostics.Debug.WriteLine("[TypefaceCache] No UI font found in APK assets; text will use the system font.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[TypefaceCache] Initialization error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// The family that was NOT compiled into the app, used only when the APK turns out
        /// to contain that one instead (the app then still shows a real app font).
        /// </summary>
        private static (string Black, string Bold, string Medium) AlternateFamily =>
            Constants.Fonts.UiFontMediumFile.StartsWith("abar", StringComparison.OrdinalIgnoreCase)
                ? ("estedad-black.ttf", "estedad-extrabold.ttf", "estedad-regular.ttf")
                : ("abar-black.ttf", "abar-bold.ttf", "abar-medium.ttf");

        private static (Typeface? Black, Typeface? Bold, Typeface? Medium) LoadUiFamily(
            global::Android.Content.Res.AssetManager assets, string blackFile, string boldFile, string mediumFile) =>
            (LoadAssetSafe(assets, blackFile), LoadAssetSafe(assets, boldFile), LoadAssetSafe(assets, mediumFile));

        private static Typeface? LoadAssetSafe(global::Android.Content.Res.AssetManager assets, string filename)
        {
            var tf = TryCreateFromAsset(assets, filename)
                ?? TryCreateFromAsset(assets, $"Fonts/{filename}")
                ?? TryCreateFromAsset(assets, $"fonts/{filename}");
            if (tf != null) return tf;

            // Resilient stream fallback: if Typeface.CreateFromAsset fails (e.g. on compressed APK assets
            // where file descriptor memory-mapping fails natively on some Android versions/ROMs), extract the
            // asset stream directly to the app cache directory and load it via Typeface.CreateFromFile().
            try
            {
                var fallback = LoadFromStreamFallback(assets, filename);
                global::Android.Util.Log.Info("KopolArtFonts", $"Stream fallback for {filename}: {(fallback != null ? "OK" : "null")}");
                return fallback;
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("KopolArtFonts", $"Failed to load {filename}: {ex}");
                System.Diagnostics.Debug.WriteLine($"[TypefaceCache] Failed to load {filename}: {ex.Message}");
                return null;
            }
        }

        private static Typeface? TryCreateFromAsset(global::Android.Content.Res.AssetManager assets, string path)
        {
            try
            {
                var tf = Typeface.CreateFromAsset(assets, path);
                if (tf != null) return tf;
                global::Android.Util.Log.Warn("KopolArtFonts", $"CreateFromAsset({path}) returned null");
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Warn("KopolArtFonts", $"CreateFromAsset({path}) threw: {ex.Message}");
            }
            return null;
        }

        private static Typeface? LoadFromStreamFallback(global::Android.Content.Res.AssetManager assets, string filename)
        {
            try
            {
                var context = global::Android.App.Application.Context;
                if (context == null) return null;

                Stream? stream = null;
                try { stream = assets.Open(filename); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[TypefaceCache] Failed to open font asset at assets root: {ex.Message}"); }
                if (stream == null) { try { stream = assets.Open($"Fonts/{filename}"); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[TypefaceCache] Failed to open font asset under Fonts/: {ex.Message}"); } }
                if (stream == null) { try { stream = assets.Open($"fonts/{filename}"); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[TypefaceCache] Failed to open font asset under fonts/: {ex.Message}"); } }

                if (stream == null)
                {
                    global::Android.Util.Log.Warn("KopolArtFonts", $"Asset stream not found for {filename} (tried root, Fonts/, fonts/)");
                    return null;
                }

                using (stream)
                {
                    var cacheDir = System.IO.Path.Combine(context.CacheDir?.AbsolutePath ?? System.IO.Path.GetTempPath(), "kopolart_fonts");
                    Directory.CreateDirectory(cacheDir);
                    var targetFile = System.IO.Path.Combine(cacheDir, filename);

                    var fileInfo = new FileInfo(targetFile);
                    if (!fileInfo.Exists || fileInfo.Length == 0)
                    {
                        var tempFile = targetFile + ".tmp";
                        using (var fs = File.Create(tempFile))
                        {
                            stream.CopyTo(fs);
                        }
                        if (File.Exists(targetFile)) File.Delete(targetFile);
                        File.Move(tempFile, targetFile);
                    }

                    var fromFile = Typeface.CreateFromFile(targetFile);
                    if (fromFile == null)
                        global::Android.Util.Log.Warn("KopolArtFonts", $"CreateFromFile returned null for {targetFile} ({new FileInfo(targetFile).Length} bytes)");
                    return fromFile;
                }
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("KopolArtFonts", $"LoadFromStreamFallback error for {filename}: {ex}");
                System.Diagnostics.Debug.WriteLine($"[TypefaceCache] LoadFromStreamFallback error for {filename}: {ex.Message}");
                return null;
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

            // 1. Icon font check (Priority 1: must never resolve to text font).
            // Exact matches only: a substring test on "kopol" would also swallow the
            // KopolFont* text aliases and hand every label the glyph-only icon font.
            if (fontFamily.Equals(Constants.Fonts.KopolArtFaAlias, StringComparison.OrdinalIgnoreCase) ||
                fontFamily.Equals(Constants.Fonts.KopolArtFaFont, StringComparison.OrdinalIgnoreCase) ||
                fontFamily.Equals("kopolart-fa", StringComparison.OrdinalIgnoreCase))
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
                fontFamily.IndexOf("abar", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fontFamily.IndexOf("estedad", StringComparison.OrdinalIgnoreCase) >= 0)
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
