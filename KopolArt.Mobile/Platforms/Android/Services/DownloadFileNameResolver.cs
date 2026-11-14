using Android.Webkit;
using Uri = Android.Net.Uri;

namespace KopolArt.Mobile.Platforms.Android.Services
{
    public static class DownloadFileNameResolver
    {
        public static bool IsApkPayload(string? fileName, string? mimeType)
        {
            return (!string.IsNullOrWhiteSpace(mimeType) && mimeType.Equals("application/vnd.android.package-archive", StringComparison.OrdinalIgnoreCase))
                   || (!string.IsNullOrWhiteSpace(fileName) && fileName.EndsWith(".apk", StringComparison.OrdinalIgnoreCase));
        }

        public static string ResolveFileName(
            string url,
            string? contentDisposition,
            string? mimeType)
        {
            if (!string.IsNullOrWhiteSpace(contentDisposition))
            {
                const string fileNameStar = "filename*=";
                int starIndex = contentDisposition.IndexOf(fileNameStar, StringComparison.OrdinalIgnoreCase);
                if (starIndex >= 0)
                {
                    string value = contentDisposition[(starIndex + fileNameStar.Length)..].Trim();
                    int semicolonIndex = value.IndexOf(';');
                    if (semicolonIndex >= 0)
                        value = value[..semicolonIndex];

                    value = value.Trim().Trim('"');

                    int encodingIndex = value.IndexOf("''", StringComparison.Ordinal);
                    if (encodingIndex >= 0)
                        value = value[(encodingIndex + 2)..];

                    value = Uri.Decode(value) ?? value;

                    if (!string.IsNullOrWhiteSpace(value))
                        return SanitizeFileName(value);
                }

                const string fileName = "filename=";
                int index = contentDisposition.IndexOf(fileName, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    string value = contentDisposition[(index + fileName.Length)..].Trim();
                    int semicolonIndex = value.IndexOf(';');
                    if (semicolonIndex >= 0)
                        value = value[..semicolonIndex];

                    value = value.Trim().Trim('"', '\'');

                    if (!string.IsNullOrWhiteSpace(value))
                        return SanitizeFileName(value);
                }
            }

            try
            {
                string? guessed = URLUtil.GuessFileName(url, contentDisposition, mimeType);
                if (!string.IsNullOrWhiteSpace(guessed))
                    return SanitizeFileName(guessed);
            }
            catch
            {
                // ignore
            }

            string extension = mimeType switch
            {
                "application/pdf" => ".pdf",
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "application/zip" => ".zip",
                "text/plain" => ".txt",
                _ => ".bin"
            };

            return "download_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + extension;
        }

        public static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "download_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".bin";

            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            name = name.Replace("/", "_").Replace("\\", "_").Trim();

            if (string.IsNullOrWhiteSpace(name))
                return "download_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".bin";

            return name;
        }

        public static string GenerateUniqueFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "download_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".bin";

            string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            string ext = Path.GetExtension(fileName);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");

            return $"{nameWithoutExt}_{timestamp}{ext}";
        }
    }
}
