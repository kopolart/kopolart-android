using Android.Content;
using Android.Provider;
using KopolArt.Mobile.Models;
using KopolArt.Mobile.Services;
using Environment = Android.OS.Environment;
using Stream = System.IO.Stream;

namespace KopolArt.Mobile.Platforms.Android.Services
{
    public class AndroidDownloadStorageService
    {
        private readonly Context _context;
        private readonly ISettingsService? _settingsService;

        public AndroidDownloadStorageService(Context context, ISettingsService? settingsService = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _settingsService = settingsService ?? IPlatformApplication.Current?.Services.GetService<ISettingsService>()
                             ?? Application.Current?.Handler?.MauiContext?.Services.GetService<ISettingsService>();
        }

        public async Task<string?> SaveFileAsync(
            Stream inputStream,
            string fileName,
            string mimeType,
            long totalBytes,
            Func<int, Task>? onProgress = null,
            bool allowApk = true,
            ActiveDownloadSession? session = null,
            CancellationToken cancellationToken = default)
        {
            if (allowApk && DownloadFileNameResolver.IsApkPayload(fileName, mimeType))
            {
                try
                {
                    return await SaveToCache(inputStream, fileName, totalBytes, onProgress, session, cancellationToken);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving APK to cache: {ex}");
                    return null;
                }
            }

            string? tempFilePath = null;
            Stream effectiveStream = inputStream;

            try
            {
                // Ensure the stream is seekable so fallback never receives an exhausted or corrupt stream
                if (!inputStream.CanSeek)
                {
                    string cacheFolder = _context.CacheDir?.AbsolutePath ?? Path.GetTempPath();
                    tempFilePath = Path.Combine(cacheFolder, $"dl_buffer_{Guid.NewGuid():N}.tmp");
                    await using (var tempFs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await CopyStreamWithProgress(inputStream, tempFs, totalBytes, onProgress, session, cancellationToken);
                    }
                    effectiveStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                    // The network transfer already reported progress; the local copy below
                    // must not restart the indicator from zero.
                    onProgress = null;
                }
                else
                {
                    effectiveStream.Seek(0, SeekOrigin.Begin);
                }

                // If custom SAF folder is configured, try saving to it first
                string? customFolderUri = _settingsService?.Settings?.CustomDownloadFolderUri;
                if (!string.IsNullOrWhiteSpace(customFolderUri))
                {
                    try
                    {
                        if (effectiveStream.CanSeek)
                        {
                            effectiveStream.Seek(0, SeekOrigin.Begin);
                        }

                        var safUri = await SaveViaDocumentTree(effectiveStream, customFolderUri, fileName, mimeType, totalBytes, onProgress, session, cancellationToken);
                        if (!string.IsNullOrWhiteSpace(safUri))
                            return safUri;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AndroidDownloadStorageService] Custom SAF save failed: {ex.Message}. Falling back to MediaStore...");
                    }
                }

                if (effectiveStream.CanSeek)
                {
                    effectiveStream.Seek(0, SeekOrigin.Begin);
                }

                try
                {
                    // MediaStore.Downloads only exists on Android 10 (API 29) and newer.
                    // Android 8/9 write straight into the public Downloads folder.
                    var mediaStoreUri = OperatingSystem.IsAndroidVersionAtLeast(29)
                        ? await SaveViaMediaStore(effectiveStream, fileName, mimeType, totalBytes, onProgress, session, cancellationToken)
                        : await SaveToLegacyPublicDownloads(effectiveStream, fileName, mimeType, totalBytes, onProgress, session, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(mediaStoreUri))
                        return mediaStoreUri;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"MediaStore save failed: {ex.Message}. Attempting fallback to app storage...");
                }

                // Fallback storage if MediaStore is unavailable or restricted
                try
                {
                    if (effectiveStream.CanSeek)
                    {
                        effectiveStream.Seek(0, SeekOrigin.Begin);
                    }
                    return await SaveToExternalFallback(effectiveStream, fileName, totalBytes, onProgress, session, cancellationToken);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Fallback save also failed: {ex.Message}");
                    return null;
                }
            }
            finally
            {
                if (tempFilePath != null)
                {
                    await effectiveStream.DisposeAsync();
                    try
                    {
                        if (File.Exists(tempFilePath))
                        {
                            File.Delete(tempFilePath);
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }

        private async Task<string?> SaveToCache(
            Stream inputStream,
            string fileName,
            long totalBytes,
            Func<int, Task>? onProgress,
            ActiveDownloadSession? session = null,
            CancellationToken cancellationToken = default)
        {
            var cacheDir = _context.CacheDir;
            if (cacheDir == null)
                throw new InvalidOperationException("Cache directory is not available.");

            var file = new Java.IO.File(cacheDir, fileName);
            if (file.Exists())
            {
                file.Delete();
            }

            try
            {
                await using var output = new FileStream(file.AbsolutePath, FileMode.Create, FileAccess.Write);
                await CopyStreamWithProgress(inputStream, output, totalBytes, onProgress, session, cancellationToken);
                return file.AbsolutePath;
            }
            catch
            {
                try
                {
                    if (file.Exists())
                        file.Delete();
                }
                catch
                {
                    // ignored
                }
                throw;
            }
        }

        [global::System.Runtime.Versioning.SupportedOSPlatform("android29.0")]
        private async Task<string?> SaveViaMediaStore(
            Stream inputStream,
            string fileName,
            string mimeType,
            long totalBytes,
            Func<int, Task>? onProgress,
            ActiveDownloadSession? session = null,
            CancellationToken cancellationToken = default)
        {
            var resolver = _context.ContentResolver;
            if (resolver == null)
                throw new InvalidOperationException("ContentResolver is not available.");

            string sanitizedName = DownloadFileNameResolver.SanitizeFileName(fileName);
            string effectiveMime = string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType;

            var values = new ContentValues();
            values.Put(MediaStore.IMediaColumns.DisplayName, sanitizedName);
            values.Put(MediaStore.IMediaColumns.MimeType, effectiveMime);
            values.Put(MediaStore.IMediaColumns.RelativePath, Environment.DirectoryDownloads + "/");
            values.Put(MediaStore.IMediaColumns.IsPending, 1);

            var collectionUri = MediaStore.Downloads.ExternalContentUri;
            if (collectionUri == null)
                throw new InvalidOperationException("MediaStore.Downloads.ExternalContentUri is null.");

            global::Android.Net.Uri? itemUri = null;
            try
            {
                itemUri = resolver.Insert(collectionUri, values);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"First MediaStore insert failed: {ex.Message}");
            }

            // If collision or error occurred, retry with unique name
            if (itemUri == null)
            {
                string uniqueName = DownloadFileNameResolver.GenerateUniqueFileName(sanitizedName);
                values.Put(MediaStore.IMediaColumns.DisplayName, uniqueName);
                try
                {
                    itemUri = resolver.Insert(collectionUri, values);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Second MediaStore insert failed: {ex.Message}");
                }
            }

            if (itemUri == null)
                throw new InvalidOperationException("MediaStore insert failed on both standard and unique name attempts.");

            try
            {
                await using var output = resolver.OpenOutputStream(itemUri)
                                         ?? throw new InvalidOperationException("Unable to open output stream.");

                await CopyStreamWithProgress(inputStream, output, totalBytes, onProgress, session, cancellationToken);

                values.Clear();
                values.Put(MediaStore.IMediaColumns.IsPending, 0);
                resolver.Update(itemUri, values, null, null);

                return itemUri.ToString();
            }
            catch
            {
                try
                {
                    resolver.Delete(itemUri, null, null);
                }
                catch
                {
                    // ignored
                }

                throw;
            }
        }

        /// <summary>
        /// Android 8/9 (API 26-28): scoped storage does not exist yet, so files are written
        /// directly into the public Downloads folder using WRITE_EXTERNAL_STORAGE and then
        /// announced to the media scanner so file managers and galleries list them.
        /// </summary>
        private async Task<string?> SaveToLegacyPublicDownloads(
            Stream inputStream,
            string fileName,
            string mimeType,
            long totalBytes,
            Func<int, Task>? onProgress,
            ActiveDownloadSession? session = null,
            CancellationToken cancellationToken = default)
        {
#pragma warning disable CA1422 // Deprecated only on API 29+; this path runs on API 26-28
            var downloadsDir = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDownloads);
#pragma warning restore CA1422
            if (downloadsDir == null)
                throw new InvalidOperationException("Public Downloads directory is not available.");

            if (!downloadsDir.Exists() && !downloadsDir.Mkdirs() && !downloadsDir.Exists())
                throw new InvalidOperationException("Public Downloads directory could not be created.");

            string sanitizedName = DownloadFileNameResolver.SanitizeFileName(fileName);
            var file = new Java.IO.File(downloadsDir, sanitizedName);
            if (file.Exists())
            {
                sanitizedName = DownloadFileNameResolver.GenerateUniqueFileName(sanitizedName);
                file = new Java.IO.File(downloadsDir, sanitizedName);
            }

            try
            {
                await using var output = new FileStream(file.AbsolutePath, FileMode.Create, FileAccess.Write);
                await CopyStreamWithProgress(inputStream, output, totalBytes, onProgress, session, cancellationToken);
            }
            catch
            {
                try
                {
                    if (file.Exists())
                        file.Delete();
                }
                catch
                {
                    // ignored
                }
                throw;
            }

            try
            {
                global::Android.Media.MediaScannerConnection.ScanFile(
                    _context,
                    [file.AbsolutePath],
                    [string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType],
                    null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidDownloadStorageService] Media scan failed: {ex.Message}");
            }

            return file.AbsolutePath;
        }

        private async Task<string?> SaveToExternalFallback(
            Stream inputStream,
            string fileName,
            long totalBytes,
            Func<int, Task>? onProgress,
            ActiveDownloadSession? session = null,
            CancellationToken cancellationToken = default)
        {
            var extDir = _context.GetExternalFilesDir(Environment.DirectoryDownloads);
            extDir ??= _context.FilesDir;
            if (extDir == null) return null;

            if (!extDir.Exists())
            {
                extDir.Mkdirs();
            }

            string sanitized = DownloadFileNameResolver.SanitizeFileName(fileName);
            var file = new Java.IO.File(extDir, sanitized);
            if (file.Exists())
            {
                sanitized = DownloadFileNameResolver.GenerateUniqueFileName(sanitized);
                file = new Java.IO.File(extDir, sanitized);
            }

            if (inputStream.CanSeek)
            {
                inputStream.Seek(0, SeekOrigin.Begin);
            }

            try
            {
                await using var output = new FileStream(file.AbsolutePath, FileMode.Create, FileAccess.Write);
                await CopyStreamWithProgress(inputStream, output, totalBytes, onProgress, session, cancellationToken);
                return file.AbsolutePath;
            }
            catch
            {
                try
                {
                    if (file.Exists())
                        file.Delete();
                }
                catch
                {
                    // ignored
                }
                throw;
            }
        }

        private async Task<string?> SaveViaDocumentTree(
            Stream inputStream,
            string treeUriString,
            string fileName,
            string mimeType,
            long totalBytes,
            Func<int, Task>? onProgress,
            ActiveDownloadSession? session = null,
            CancellationToken cancellationToken = default)
        {
            var treeUri = global::Android.Net.Uri.Parse(treeUriString);
            if (treeUri == null) return null;

            var resolver = _context.ContentResolver;
            if (resolver == null) throw new InvalidOperationException("ContentResolver is null.");

            var treeDocId = DocumentsContract.GetTreeDocumentId(treeUri);
            if (string.IsNullOrWhiteSpace(treeDocId)) return null;

            var parentDocUri = DocumentsContract.BuildDocumentUriUsingTree(treeUri, treeDocId);
            if (parentDocUri == null) return null;

            string sanitizedName = DownloadFileNameResolver.SanitizeFileName(fileName);
            string effectiveMime = string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType;

            global::Android.Net.Uri? targetDocUri = null;
            try
            {
                targetDocUri = DocumentsContract.CreateDocument(resolver, parentDocUri, effectiveMime, sanitizedName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidDownloadStorageService] CreateDocument first attempt failed: {ex.Message}");
            }

            if (targetDocUri == null)
            {
                string uniqueName = DownloadFileNameResolver.GenerateUniqueFileName(sanitizedName);
                try
                {
                    targetDocUri = DocumentsContract.CreateDocument(resolver, parentDocUri, effectiveMime, uniqueName);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AndroidDownloadStorageService] CreateDocument second attempt failed: {ex.Message}");
                }
            }

            if (targetDocUri == null)
            {
                System.Diagnostics.Debug.WriteLine("[AndroidDownloadStorageService] DocumentsContract.CreateDocument returned null.");
                return null;
            }

            try
            {
                await using var output = resolver.OpenOutputStream(targetDocUri)
                    ?? throw new InvalidOperationException("Unable to open output stream for target document.");

                await CopyStreamWithProgress(inputStream, output, totalBytes, onProgress, session, cancellationToken);
                return targetDocUri.ToString();
            }
            catch
            {
                try
                {
                    DocumentsContract.DeleteDocument(resolver, targetDocUri);
                }
                catch { /* ignored */ }
                throw;
            }
        }

        private static async Task CopyStreamWithProgress(
            Stream input,
            Stream output,
            long totalBytes,
            Func<int, Task>? onProgress,
            ActiveDownloadSession? session = null,
            CancellationToken cancellationToken = default)
        {
            long received = 0;
            var buffer = new byte[81920];
            int read;
            int lastProgress = -1;

            while (true)
            {
                // Wait if download is paused by user; cancellation releases the wait.
                if (session != null)
                {
                    await session.WaitWhilePausedAsync(cancellationToken).ConfigureAwait(false);
                }

                read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
                if (read <= 0) break;

                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                received += read;

                if (totalBytes > 0 && onProgress != null)
                {
                    int pct = (int)(received * 100L / totalBytes);
                    if (pct != lastProgress)
                    {
                        lastProgress = pct;
                        if (session != null) session.CurrentProgress = pct;
                        await onProgress(pct);
                    }
                }
            }

            await output.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
