using System.Text;
using static KopolArt.Mobile.Constants;

namespace KopolArt.Mobile.Helpers
{
    /// <summary>
    /// Loads and caches Base64-encoded Persian typography and font icons (kopolart) used by the
    /// offline renderer, so saved pages keep the website look without network requests.
    /// Uses the licensed Abar faces when they are bundled, otherwise the OFL Estedad faces.
    /// </summary>
    public static class OfflineFontHelper
    {
        private static string? _cachedFontsStyleTag;
        private static readonly object LockObj = new();

        public static string GetOfflineFontsStyleTag()
        {
            if (_cachedFontsStyleTag != null) return _cachedFontsStyleTag;

            lock (LockObj)
            {
                if (_cachedFontsStyleTag != null) return _cachedFontsStyleTag;

                string regularB64 = LoadFontAsBase64(Fonts.AbarVarWoff2);
                string regularFormat = "woff2";
                string regularWeight = "100 950";
                string mediumB64 = LoadFontAsBase64(Fonts.AbarStaticWoff);
                string mediumFormat = "woff";
                string boldB64 = LoadFontAsBase64(Fonts.AbarStaticBoldWoff);
                string boldFormat = "woff";
                string blackB64 = string.Empty;

                // Public builds ship no Abar assets, so fall back to the OFL Estedad faces.
                if (string.IsNullOrEmpty(regularB64))
                {
                    regularB64 = LoadFontAsBase64(Fonts.UiFontMediumFile);
                    regularFormat = "truetype";
                    regularWeight = "400";
                    mediumB64 = regularB64;
                    mediumFormat = "truetype";
                    boldB64 = LoadFontAsBase64(Fonts.UiFontBoldFile);
                    boldFormat = "truetype";
                    blackB64 = LoadFontAsBase64(Fonts.UiFontBlackFile);
                }

                string kopolartWoff2B64 = LoadFontAsBase64(Fonts.KopolArtFaWoff2);
                string kopolartWoffB64 = LoadFontAsBase64(Fonts.KopolArtFaWoff);

                if (string.IsNullOrEmpty(kopolartWoff2B64))
                {
                    kopolartWoff2B64 = LoadFontAsBase64(Fonts.KopolArtFaFont);
                }

                var sb = new StringBuilder();
                sb.AppendLine("<style id=\"__kopolart-offline-fonts\">");

                // 1. AbarV - main text family used across kopolart.ir
                if (!string.IsNullOrEmpty(regularB64))
                {
                    sb.AppendLine(FontFace("AbarV", regularB64, regularFormat, regularWeight));
                    // Alias AbarFont to AbarV for native fallbacks
                    sb.AppendLine(FontFace("AbarFont", regularB64, regularFormat, "normal"));
                }

                // 2. AbarS - static heading faces
                if (!string.IsNullOrEmpty(mediumB64))
                {
                    sb.AppendLine(FontFace("AbarS", mediumB64, mediumFormat, "500"));
                }

                if (!string.IsNullOrEmpty(boldB64))
                {
                    sb.AppendLine(FontFace("AbarS", boldB64, boldFormat, "700"));
                }

                if (!string.IsNullOrEmpty(blackB64))
                {
                    sb.AppendLine(FontFace("AbarS", blackB64, "truetype", "900"));
                }

                // 3. kopolart - Exact website icon font
                if (!string.IsNullOrEmpty(kopolartWoff2B64) || !string.IsNullOrEmpty(kopolartWoffB64))
                {
                    string srcParts = !string.IsNullOrEmpty(kopolartWoff2B64)
                        ? $"url('data:font/woff2;charset=utf-8;base64,{kopolartWoff2B64}') format('woff2')"
                        : $"url('data:font/woff;charset=utf-8;base64,{kopolartWoffB64}') format('woff')";

                    sb.AppendLine($@"@font-face {{
    font-family: 'kopolart';
    src: {srcParts};
    font-weight: 400;
    font-style: normal;
    font-display: block;
}}");

                    // Ensure ic-* icon selectors bind to kopolart icon font
                    sb.AppendLine(@"[class*="" ic-""], [class^=""ic-""], [class*="" ic-""]:before, [class^=""ic-""]:before {
    font-family: 'kopolart' !important;
    font-style: normal;
    font-variant: normal;
    text-transform: none;
    line-height: 1.1em;
    -webkit-font-smoothing: antialiased;
    -moz-osx-font-smoothing: grayscale;
}");
                }

                sb.AppendLine("</style>");

                _cachedFontsStyleTag = sb.ToString();
                return _cachedFontsStyleTag;
            }
        }

        private static string FontFace(string family, string base64, string format, string weight)
        {
            string mime = format switch
            {
                "woff2" => "woff2",
                "woff" => "woff",
                _ => "ttf"
            };

            return $@"@font-face {{
    font-family: '{family}';
    src: url('data:font/{mime};charset=utf-8;base64,{base64}') format('{format}');
    font-weight: {weight};
    font-style: normal;
    font-display: swap;
}}";
        }

        private static string LoadFontAsBase64(string filename)
        {
            try
            {
#if ANDROID
                var context = global::Android.App.Application.Context;
                using var stream = context?.Assets?.Open(filename);
                if (stream != null)
                {
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    return Convert.ToBase64String(ms.ToArray());
                }
#endif
                using var pkgStream = FileSystem.OpenAppPackageFileAsync(filename).GetAwaiter().GetResult();
                using var pkgMs = new MemoryStream();
                pkgStream.CopyTo(pkgMs);
                return Convert.ToBase64String(pkgMs.ToArray());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OfflineFontHelper] Load font error for '{filename}': {ex.Message}");
                return string.Empty;
            }
        }
    }
}
