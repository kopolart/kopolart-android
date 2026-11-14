using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace KopolArt.Mobile.Helpers
{
    /// <summary>
    /// Converts Chromium MHTML archive files (.mht) into standalone, self-contained HTML documents (.html).
    /// This bypasses Chromium's hardcoded security sandbox that permanently disables JavaScript in MHTML,
    /// while inlining all images as Base64 data URIs and embedding Persian typography (AbarFont) and icons (KopolArtFa).
    /// </summary>
    public static class OfflineHtmlConverter
    {
        public static async Task<bool> ConvertMhtmlToHtmlAsync(string mhtmlPath, string targetHtmlPath, string originalUrl, string title)
        {
            if (string.IsNullOrWhiteSpace(mhtmlPath) || !File.Exists(mhtmlPath))
                return false;

            try
            {
                string mhtmlContent = await File.ReadAllTextAsync(mhtmlPath, Encoding.Latin1);
                if (string.IsNullOrWhiteSpace(mhtmlContent))
                    return false;

                // 1. Locate MHTML boundary delimiter
                var boundaryMatch = Regex.Match(mhtmlContent, @"boundary=""?([^""\r\n;]+)""?", RegexOptions.IgnoreCase);
                if (!boundaryMatch.Success)
                    return false;

                string boundary = "--" + boundaryMatch.Groups[1].Value.Trim();
                string[] parts = mhtmlContent.Split(new[] { boundary }, StringSplitOptions.None);

                string? rawHtml = null;
                var base64Resources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var cssStylesheets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var part in parts)
                {
                    string trimmedPart = part.Trim();
                    if (string.IsNullOrEmpty(trimmedPart) || trimmedPart == "--")
                        continue;

                    int headerEnd = part.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                    int delimiterLen = 4;
                    if (headerEnd == -1)
                    {
                        headerEnd = part.IndexOf("\n\n", StringComparison.Ordinal);
                        delimiterLen = 2;
                    }

                    if (headerEnd == -1)
                        continue;

                    string headerSection = part.Substring(0, headerEnd);
                    string bodySection = part.Substring(headerEnd + delimiterLen).TrimEnd('\r', '\n', '-');

                    var headers = ParseHeaders(headerSection);
                    headers.TryGetValue("content-type", out var contentType);
                    headers.TryGetValue("content-transfer-encoding", out var transferEncoding);
                    headers.TryGetValue("content-location", out var contentLocation);
                    headers.TryGetValue("content-id", out var contentId);

                    contentType = contentType?.ToLowerInvariant() ?? string.Empty;
                    transferEncoding = transferEncoding?.ToLowerInvariant() ?? string.Empty;

                    // Main HTML Document
                    if (contentType.Contains("text/html"))
                    {
                        if (transferEncoding.Contains("quoted-printable"))
                        {
                            rawHtml = DecodeQuotedPrintable(bodySection);
                        }
                        else if (transferEncoding.Contains("base64"))
                        {
                            try
                            {
                                byte[] bytes = Convert.FromBase64String(Regex.Replace(bodySection, @"\s+", ""));
                                rawHtml = Encoding.UTF8.GetString(bytes);
                            }
                            catch
                            {
                                rawHtml = bodySection;
                            }
                        }
                        else
                        {
                            rawHtml = bodySection;
                        }
                    }
                    // Stylesheets
                    else if (contentType.Contains("text/css"))
                    {
                        string decodedCss = transferEncoding.Contains("quoted-printable")
                            ? DecodeQuotedPrintable(bodySection)
                            : bodySection;

                        if (!string.IsNullOrWhiteSpace(contentLocation))
                        {
                            cssStylesheets[contentLocation] = decodedCss;
                        }
                    }
                    // Images and font binaries
                    else if (contentType.StartsWith("image/") || contentType.Contains("font"))
                    {
                        if (transferEncoding.Contains("base64"))
                        {
                            string cleanBase64 = Regex.Replace(bodySection, @"\s+", "");
                            string dataUri = $"data:{contentType};base64,{cleanBase64}";

                            if (!string.IsNullOrWhiteSpace(contentLocation))
                            {
                                base64Resources[contentLocation] = dataUri;
                            }

                            if (!string.IsNullOrWhiteSpace(contentId))
                            {
                                string cleanCid = contentId.Trim('<', '>');
                                base64Resources[$"cid:{cleanCid}"] = dataUri;
                            }
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(rawHtml))
                    return false;

                // 2. In-line image & resource URLs with Base64 data URIs
                foreach (var (loc, dataUri) in base64Resources)
                {
                    rawHtml = rawHtml.Replace(loc, dataUri);
                }

                // 3. In-line external stylesheets into <style> blocks
                foreach (var (loc, cssContent) in cssStylesheets)
                {
                    string linkPattern = @"<link[^>]+href=[""']" + Regex.Escape(loc) + @"[""'][^>]*>";
                    string inlineStyle = $"<style id=\"__inlined_{loc.GetHashCode():X}\">\n{cssContent}\n</style>";
                    if (Regex.IsMatch(rawHtml, linkPattern, RegexOptions.IgnoreCase))
                    {
                        rawHtml = Regex.Replace(rawHtml, linkPattern, inlineStyle, RegexOptions.IgnoreCase);
                    }
                    else
                    {
                        // Fallback: append inside head if link tag was not matched directly
                        rawHtml = rawHtml.Replace("</head>", $"{inlineStyle}\n</head>", StringComparison.OrdinalIgnoreCase);
                    }
                }

                // 4. Inject base tag, Base64 fonts, and theme initializer
                string fontStyleTag = OfflineFontHelper.GetOfflineFontsStyleTag();
                string baseTag = string.IsNullOrWhiteSpace(originalUrl)
                    ? string.Empty
                    : $"<base href=\"{SecurityElement.Escape(originalUrl)}\">";

                string themeInitScript = @"<script id=""__kopolart-offline-theme"">
(function() {
    try {
        var match = document.cookie.match(/(?:^|;\s*)darkmode-kopolart=([^;]*)/);
        var isDark = match ? match[1] === 'dark' : false;
        if (!match && window.matchMedia) isDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
        if (isDark) {
            document.documentElement.setAttribute('data-theme', 'dark');
            document.documentElement.setAttribute('data-theme-mode', 'dark');
            document.documentElement.classList.add('dark', 'woodmart-dark');
            if (document.body) document.body.classList.add('dark', 'woodmart-dark');
        }
    } catch(e) {}
})();
</script>";

                string headInjectBlock = $"\n{baseTag}\n{fontStyleTag}\n{themeInitScript}\n";

                if (rawHtml.Contains("<head>", StringComparison.OrdinalIgnoreCase))
                {
                    int headIdx = rawHtml.IndexOf("<head>", StringComparison.OrdinalIgnoreCase) + 6;
                    rawHtml = rawHtml.Insert(headIdx, headInjectBlock);
                }
                else if (rawHtml.Contains("<html>", StringComparison.OrdinalIgnoreCase))
                {
                    int htmlIdx = rawHtml.IndexOf("<html>", StringComparison.OrdinalIgnoreCase) + 6;
                    rawHtml = rawHtml.Insert(htmlIdx, $"<head>{headInjectBlock}</head>");
                }
                else
                {
                    rawHtml = $"<head>{headInjectBlock}</head>\n" + rawHtml;
                }

                // 5. Save self-contained HTML file with UTF-8 BOM encoding
                string? targetDir = Path.GetDirectoryName(targetHtmlPath);
                if (!string.IsNullOrWhiteSpace(targetDir) && !Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                var utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
                await File.WriteAllTextAsync(targetHtmlPath, rawHtml, utf8WithBom);

                return File.Exists(targetHtmlPath) && new FileInfo(targetHtmlPath).Length > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OfflineHtmlConverter] Conversion error: {ex.Message}");
                return false;
            }
        }

        private static Dictionary<string, string> ParseHeaders(string headerSection)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using var reader = new StringReader(headerSection);
            string? line;
            string? currentHeader = null;

            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                if ((line.StartsWith(" ") || line.StartsWith("\t")) && currentHeader != null)
                {
                    // RFC header continuation / folding
                    headers[currentHeader] += " " + line.Trim();
                }
                else
                {
                    int colonIndex = line.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        currentHeader = line.Substring(0, colonIndex).Trim();
                        string val = line.Substring(colonIndex + 1).Trim();
                        headers[currentHeader] = val;
                    }
                }
            }

            return headers;
        }

        private static string DecodeQuotedPrintable(string input)
        {
            using var ms = new MemoryStream();
            int len = input.Length;
            for (int i = 0; i < len; i++)
            {
                char c = input[i];
                if (c == '=')
                {
                    if (i + 1 < len && (input[i + 1] == '\r' || input[i + 1] == '\n'))
                    {
                        // Soft line break: = followed by CRLF or LF
                        i++;
                        if (input[i] == '\r' && i + 1 < len && input[i + 1] == '\n')
                        {
                            i++;
                        }
                        continue;
                    }
                    if (i + 2 < len && IsHexDigit(input[i + 1]) && IsHexDigit(input[i + 2]))
                    {
                        try
                        {
                            string hex = input.Substring(i + 1, 2);
                            byte b = Convert.ToByte(hex, 16);
                            ms.WriteByte(b);
                            i += 2;
                            continue;
                        }
                        catch
                        {
                            // If invalid hex, write literal byte
                        }
                    }
                }

                byte[] charBytes = Encoding.Latin1.GetBytes(new[] { c });
                ms.Write(charBytes, 0, charBytes.Length);
            }

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static bool IsHexDigit(char c) =>
            (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
    }
}
