using System.Text.Json;
using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services
{
    public class UpdateService
    {
        private readonly HttpClient _httpClient;
        private readonly IAppInstaller _installer;
        private readonly IUpdateDownloader _downloader;

        public UpdateService(IAppInstaller installer, IUpdateDownloader downloader)
        {
            _installer = installer;
            _downloader = downloader;
            _httpClient = new HttpClient
            {
                // Bounds the update-JSON fetch and the APK time-to-first-byte
                // so a stalled connection can never hang the UI forever.
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", Constants.Network.AppUserAgent);
        }

        /// <summary>
        /// Silences update check if clicked "Remind later" within last 24 hours.
        /// </summary>
        public bool ShouldCheckForUpdates()
        {
            var lastRemindedStr = Preferences.Get(Constants.Keys.AppUpdateLastReminded, string.Empty);
            if (string.IsNullOrEmpty(lastRemindedStr))
                return true;

            if (DateTime.TryParse(lastRemindedStr, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var lastDate))
            {
                return (DateTime.UtcNow - lastDate).TotalHours >= 24;
            }

            return true;
        }

        /// <summary>
        /// Records that the user requested to be reminded later.
        /// </summary>
        public void MarkRemindedLater()
        {
            Preferences.Set(Constants.Keys.AppUpdateLastReminded,
                DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Fetches the update JSON from the server.
        /// </summary>
        public async Task<UpdateInfo?> FetchUpdateInfoAsync()
        {
            return await FetchUpdateInfoDirectAsync();
        }

        /// <summary>
        /// Direct fetch helper used by UpdateService and background UpdateCheckJobService without code duplication.
        /// </summary>
        public static async Task<UpdateInfo?> FetchUpdateInfoDirectAsync(string? userAgent = null)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent ?? Constants.Network.AppUserAgent);
                var response = await client.GetStringAsync(Constants.Urls.UpdateJsonUrl);
                if (string.IsNullOrWhiteSpace(response)) return null;
                // Source-generated serializer: safe with PublishTrimmed/AOT,
                // unlike reflection-based deserialization.
                return JsonSerializer.Deserialize(response, AppJsonContext.Default.UpdateInfo);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateService] Error fetching update JSON: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Attempts to parse and normalize version strings with support for prefixes ('v' / 'V'),
        /// 1 to 4 numeric segments, and builds/prerelease suffixes (e.g. "v1.4.5", "1.4", "2", "1.4.5-beta").
        /// </summary>
        public static bool TryNormalizeVersion(string? rawVersion, out Version version)
        {
            version = new Version(0, 0);
            if (string.IsNullOrWhiteSpace(rawVersion))
                return false;

            var cleaned = rawVersion.Trim();
            if (cleaned.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned[1..].Trim();
            }

            // Strip prerelease or build metadata suffixes (e.g. "1.4.5-beta", "1.4.5+10")
            int separatorIdx = cleaned.IndexOfAny(['-', '+']);
            if (separatorIdx >= 0)
            {
                cleaned = cleaned[..separatorIdx].Trim();
            }

            var parts = cleaned.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return false;

            var intParts = new int[Math.Max(2, parts.Length)];
            for (int i = 0; i < parts.Length && i < 4; i++)
            {
                if (int.TryParse(parts[i], out int val) && val >= 0)
                {
                    intParts[i] = val;
                }
                else
                {
                    return false;
                }
            }

            try
            {
                version = parts.Length switch
                {
                    1 => new Version(intParts[0], 0),
                    2 => new Version(intParts[0], intParts[1]),
                    3 => new Version(intParts[0], intParts[1], intParts[2]),
                    _ => new Version(intParts[0], intParts[1], intParts[2], intParts[3])
                };
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Compares two version strings numerically. Returns true if checkVersion is newer than currentVersion.
        /// </summary>
        public static bool IsNewerVersion(string currentVersion, string checkVersion)
        {
            if (TryNormalizeVersion(currentVersion, out var curr) && TryNormalizeVersion(checkVersion, out var check))
            {
                return check > curr;
            }

            return CompareSegments(checkVersion, currentVersion) > 0;
        }

        private static int CompareSegments(string v1, string v2)
        {
            var clean1 = (v1 ?? string.Empty).Trim().TrimStart('v', 'V');
            var clean2 = (v2 ?? string.Empty).Trim().TrimStart('v', 'V');

            var parts1 = clean1.Split('.');
            var parts2 = clean2.Split('.');
            int maxLen = Math.Max(parts1.Length, parts2.Length);

            for (int i = 0; i < maxLen; i++)
            {
                int num1 = i < parts1.Length && int.TryParse(parts1[i], out int n1) ? n1 : 0;
                int num2 = i < parts2.Length && int.TryParse(parts2[i], out int n2) ? n2 : 0;

                if (num1 != num2)
                    return num1.CompareTo(num2);
            }

            return 0;
        }

        /// <summary>
        /// Returns all release objects that are newer than the user's current version.
        /// </summary>
        public List<UpdateRelease> GetNewReleases(UpdateInfo updateInfo, string currentVersion)
        {
            var newReleases = new List<UpdateRelease>();
            if (updateInfo?.Updates == null) return newReleases;

            foreach (var release in updateInfo.Updates)
            {
                if (IsNewerVersion(currentVersion, release.Version))
                {
                    newReleases.Add(release);
                }
            }

            // Order by version descending (newest first)
            return newReleases.OrderByDescending(r => TryNormalizeVersion(r.Version, out var v) ? v : new Version(0, 0)).ToList();
        }

        /// <summary>
        /// Fetches all update releases from the server, ordered by version descending.
        /// </summary>
        public async Task<List<UpdateRelease>> GetAllReleasesAsync()
        {
            var updateInfo = await FetchUpdateInfoAsync();
            if (updateInfo?.Updates == null) return new List<UpdateRelease>();

            return updateInfo.Updates
                .OrderByDescending(r => TryNormalizeVersion(r.Version, out var v) ? v : new Version(0, 0))
                .ToList();
        }

        /// <summary>
        /// Downloads the APK file using the platform update downloader and reports progress.
        /// </summary>
        public Task<string> DownloadApkAsync(string url, Action<double> progressCallback, CancellationToken cancellationToken)
        {
            return _downloader.DownloadUpdateAsync(url, progressCallback, cancellationToken);
        }

        /// <summary>
        /// Downloads the APK file using the platform update downloader and reports detailed progress (progress, bytesRead, totalBytes).
        /// </summary>
        public Task<string> DownloadApkAsync(string url, Action<double, long, long> progressCallback, CancellationToken cancellationToken)
        {
            return _downloader.DownloadUpdateAsync(url, progressCallback, cancellationToken);
        }

        /// <summary>
        /// Checks if the app has permission to install packages directly.
        /// </summary>
        public bool CanInstallApkDirectly() => _installer.CanInstallDirectly();

        /// <summary>
        /// Opens the system settings screen to allow installing unknown apps.
        /// </summary>
        public void RequestInstallPermission() => _installer.RequestInstallPermission();

        /// <summary>
        /// Triggers the package installation using the platform installer.
        /// </summary>
        public void InstallApk(string localFilePath) => _installer.Install(localFilePath);
    }
}
