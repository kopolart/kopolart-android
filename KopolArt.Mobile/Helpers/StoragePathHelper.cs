namespace KopolArt.Mobile.Helpers
{
    public static class StoragePathHelper
    {
        public const string DefaultDownloadFolderDisplay = "پوشه دانلودهای سیستم (پیش‌فرض)";

        /// <summary>
        /// Resolves complex Android SAF Document, Tree, or MediaStore URIs into clean, user-friendly display paths.
        /// </summary>
        public static string ResolveUriToDisplayPath(string? localFilePath, string? fileName = null)
        {
            if (string.IsNullOrWhiteSpace(localFilePath))
                return DefaultDownloadFolderDisplay;

#if ANDROID
            try
            {
                if (localFilePath.StartsWith("content://com.android.externalstorage.documents", StringComparison.OrdinalIgnoreCase) ||
                    localFilePath.Contains(".documents", StringComparison.OrdinalIgnoreCase) ||
                    localFilePath.Contains("/tree/", StringComparison.OrdinalIgnoreCase) ||
                    localFilePath.Contains("/document/", StringComparison.OrdinalIgnoreCase))
                {
                    var uri = Android.Net.Uri.Parse(localFilePath);
                    if (uri != null)
                    {
                        string? docId = null;
                        try
                        {
                            docId = Android.Provider.DocumentsContract.GetDocumentId(uri);
                        }
                        catch { /* ignored */ }

                        if (string.IsNullOrWhiteSpace(docId))
                        {
                            try
                            {
                                docId = Android.Provider.DocumentsContract.GetTreeDocumentId(uri);
                            }
                            catch { /* ignored */ }
                        }

                        if (!string.IsNullOrWhiteSpace(docId))
                        {
                            if (docId.StartsWith("raw:", StringComparison.OrdinalIgnoreCase))
                            {
                                return FormatFriendlyPath(docId["raw:".Length..], folderOnly: true, fileName);
                            }

                            if (docId.StartsWith("primary:", StringComparison.OrdinalIgnoreCase))
                            {
                                string sub = docId["primary:".Length..].Trim('/');
                                return FormatFriendlyPath($"primary:{sub}", folderOnly: true, fileName);
                            }

                            int colon = docId.IndexOf(':');
                            if (colon > 0)
                            {
                                string volume = docId[..colon];
                                string sub = docId[(colon + 1)..].Trim('/');
                                return FormatFriendlyPath($"/storage/{volume}/{sub}", folderOnly: true, fileName);
                            }

                            if (docId.StartsWith("/"))
                            {
                                return FormatFriendlyPath(docId, folderOnly: true, fileName);
                            }
                        }

                        string decoded = Android.Net.Uri.Decode(localFilePath) ?? string.Empty;
                        int primaryIdx = decoded.IndexOf("primary:", StringComparison.OrdinalIgnoreCase);
                        if (primaryIdx >= 0)
                        {
                            string sub = decoded[(primaryIdx + "primary:".Length)..].Trim('/');
                            return FormatFriendlyPath($"primary:{sub}", folderOnly: true, fileName);
                        }
                    }
                }
                else if (localFilePath.StartsWith("content://media/external/downloads", StringComparison.OrdinalIgnoreCase) ||
                         localFilePath.StartsWith("content://downloads", StringComparison.OrdinalIgnoreCase) ||
                         localFilePath.StartsWith("content://media/", StringComparison.OrdinalIgnoreCase))
                {
                    var context = Android.App.Application.Context;
                    var uri = Android.Net.Uri.Parse(localFilePath);
                    if (uri != null && context != null)
                    {
                        // RelativePath and the IMediaColumns interface only exist on Android 10 (API 29)+.
                        string[] projection = OperatingSystem.IsAndroidVersionAtLeast(29)
                            ? [Android.Provider.MediaStore.IMediaColumns.Data, Android.Provider.MediaStore.IMediaColumns.RelativePath]
                            : [Android.Provider.MediaStore.IMediaColumns.Data];

                        using var cursor = context.ContentResolver?.Query(
                            uri,
                            projection,
                            null, null, null);

                        if (cursor != null && cursor.MoveToFirst())
                        {
                            if (OperatingSystem.IsAndroidVersionAtLeast(29))
                            {
                                int relIdx = cursor.GetColumnIndex(Android.Provider.MediaStore.IMediaColumns.RelativePath);
                                string? relPath = (relIdx >= 0 && !cursor.IsNull(relIdx)) ? cursor.GetString(relIdx) : null;
                                if (!string.IsNullOrWhiteSpace(relPath))
                                {
                                    return FormatFriendlyPath($"/storage/emulated/0/{relPath}", folderOnly: true, fileName);
                                }
                            }

                            int dataIdx = cursor.GetColumnIndex(Android.Provider.MediaStore.IMediaColumns.Data);
                            if (dataIdx >= 0 && !cursor.IsNull(dataIdx))
                            {
                                string? dataVal = cursor.GetString(dataIdx);
                                if (!string.IsNullOrWhiteSpace(dataVal) && dataVal.StartsWith("/"))
                                {
                                    return FormatFriendlyPath(dataVal, folderOnly: true, fileName);
                                }
                            }
                        }
                    }

                    return DefaultDownloadFolderDisplay;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StoragePathHelper] ResolveUriToDisplayPath error: {ex.Message}");
            }
#endif

            return FormatFriendlyPath(localFilePath, folderOnly: true, fileName);
        }

        /// <summary>
        /// Converts raw Android storage paths or URIs into clean, user-friendly paths:
        /// - Internal storage: Device/...
        /// - External SDCard: SDCard/...
        /// - Default system download directory: پوشه دانلودهای سیستم (پیش‌فرض)
        /// If folderOnly is true, any trailing file name is excluded.
        /// </summary>
        public static string FormatFriendlyPath(string? rawPath, bool folderOnly = true, string? fileName = null)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
                return DefaultDownloadFolderDisplay;

            string path = rawPath.Trim();

            // Already the default display text or contains it
            if (string.Equals(path, DefaultDownloadFolderDisplay, StringComparison.OrdinalIgnoreCase) ||
                path.Contains("پوشه دانلودهای سیستم", StringComparison.OrdinalIgnoreCase))
            {
                return DefaultDownloadFolderDisplay;
            }

            // MediaStore / system download URIs
            if (path.StartsWith("content://media/external/downloads", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("content://downloads", StringComparison.OrdinalIgnoreCase))
            {
                return DefaultDownloadFolderDisplay;
            }

            // Primary SAF tree document: primary:Documents/KopolArt
            if (path.StartsWith("primary:", StringComparison.OrdinalIgnoreCase))
            {
                string sub = path["primary:".Length..].Trim('/');
                return FormatPrimarySubPath(sub, folderOnly, fileName);
            }

            // Internal storage root or download: /storage/emulated/0/...
            if (path.StartsWith("/storage/emulated/0", StringComparison.OrdinalIgnoreCase))
            {
                string sub = path["/storage/emulated/0".Length..].Trim('/');
                return FormatPrimarySubPath(sub, folderOnly, fileName);
            }

            // External SD Card: /storage/{volume_id}/...
            if (path.StartsWith("/storage/", StringComparison.OrdinalIgnoreCase))
            {
                string rem = path["/storage/".Length..].TrimStart('/');
                int slash = rem.IndexOf('/');
                if (slash < 0)
                {
                    return "SDCard";
                }

                string sub = rem[(slash + 1)..].Trim('/');
                return FormatSdCardSubPath(sub, folderOnly, fileName);
            }

            // Volume colon syntax: e.g. 0123-4567:Folder
            int colonIdx = path.IndexOf(':');
            if (colonIdx > 0 && !path.StartsWith("content:", StringComparison.OrdinalIgnoreCase) && !path.StartsWith("http:", StringComparison.OrdinalIgnoreCase) && !path.StartsWith("https:", StringComparison.OrdinalIgnoreCase))
            {
                string vol = path[..colonIdx];
                string sub = path[(colonIdx + 1)..].Trim('/');
                if (vol.Equals("primary", StringComparison.OrdinalIgnoreCase))
                {
                    return FormatPrimarySubPath(sub, folderOnly, fileName);
                }
                return FormatSdCardSubPath(sub, folderOnly, fileName);
            }

            // Device/...
            if (path.StartsWith("Device/", StringComparison.OrdinalIgnoreCase) ||
                path.Equals("Device", StringComparison.OrdinalIgnoreCase))
            {
                string sub = path.Length > 6 ? path[7..].Trim('/') : string.Empty;
                return FormatPrimarySubPath(sub, folderOnly, fileName);
            }

            // SDCard/...
            if (path.StartsWith("SDCard/", StringComparison.OrdinalIgnoreCase) ||
                path.Equals("SDCard", StringComparison.OrdinalIgnoreCase))
            {
                string sub = path.Length > 6 ? path[7..].Trim('/') : string.Empty;
                return FormatSdCardSubPath(sub, folderOnly, fileName);
            }

            // Fallback for content:// URIs
            if (path.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
            {
                return DefaultDownloadFolderDisplay;
            }

            if (folderOnly)
            {
                path = StripFileName(path, fileName);
            }

            return string.IsNullOrWhiteSpace(path) ? DefaultDownloadFolderDisplay : path;
        }

        private static string FormatPrimarySubPath(string sub, bool folderOnly, string? fileName)
        {
            if (folderOnly)
            {
                sub = StripFileName(sub, fileName);
            }

            if (string.IsNullOrWhiteSpace(sub) ||
                sub.Equals("Download", StringComparison.OrdinalIgnoreCase) ||
                sub.Equals("Downloads", StringComparison.OrdinalIgnoreCase))
            {
                return DefaultDownloadFolderDisplay;
            }

            return $"Device/{sub}";
        }

        private static string FormatSdCardSubPath(string sub, bool folderOnly, string? fileName)
        {
            if (folderOnly)
            {
                sub = StripFileName(sub, fileName);
            }

            if (string.IsNullOrWhiteSpace(sub))
            {
                return "SDCard";
            }

            return $"SDCard/{sub}";
        }

        private static string StripFileName(string path, string? fileName)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;

            path = path.TrimEnd('/');

            if (!string.IsNullOrWhiteSpace(fileName) && path.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
            {
                int len = path.Length - fileName.Length;
                return path[..len].TrimEnd('/');
            }

            // If path contains a file with extension at the end (e.g. "path/to/file.ext")
            int lastSlash = path.LastIndexOf('/');
            if (lastSlash >= 0)
            {
                string lastPart = path[(lastSlash + 1)..];
                if (lastPart.Contains('.') && !lastPart.StartsWith('.'))
                {
                    return path[..lastSlash].TrimEnd('/');
                }
            }
            else if (path.Contains('.') && !path.StartsWith('.'))
            {
                // Just a file name without directory
                return string.Empty;
            }

            return path;
        }
    }
}
