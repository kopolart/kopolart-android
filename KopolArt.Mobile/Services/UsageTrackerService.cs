using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services
{
    public class UsageTrackerService : IUsageTrackerService
    {
        private static readonly string SessionsFilePath = Path.Combine(FileSystem.AppDataDirectory, "usage_sessions.json");

        private readonly object _lock = new();
        private readonly Stopwatch _sessionStopwatch = new();
        private readonly INetworkStatsService _networkStatsService;
        private readonly IUsageMilestoneService _milestoneService;
        private readonly HttpClient _httpClient;
        private readonly List<UsageRecord> _sessions = [];
        private readonly SemaphoreSlim _fileSemaphore = new(1, 1);
        private readonly Timer _milestoneTimer;

        private long _priorUsageSeconds;
        private WelcomeInfo? _cachedWelcomeInfo;
        private UsageRecord? _currentSession;
        private string? _resolvedIp;
        private bool _isInitialized;
        private DateTime _lastDiskSaveUtc = DateTime.UtcNow;

        public string CurrentSessionIp => _currentSession?.IpAddress
            ?? (!string.IsNullOrWhiteSpace(_resolvedIp) ? _resolvedIp : (Connectivity.Current.NetworkAccess == NetworkAccess.Internet ? "در حال دریافت..." : "آفلاین / بدون اتصال"));
        public UsageRecord? CurrentSession => _currentSession;
        public bool HasShownWelcomeOnLoading { get; set; }

        public UsageTrackerService(INetworkStatsService networkStatsService, IUsageMilestoneService milestoneService)
        {
            _networkStatsService = networkStatsService;
            _milestoneService = milestoneService;
            _priorUsageSeconds = Preferences.Get(Constants.Keys.AppTotalUsageSeconds, 0L);

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(4)
            };

            // Pre-load saved sessions from storage immediately on startup
            _ = InitializeAsync();

            // Launch background task to resolve public IP
            _ = ResolvePublicIpAsync();

            // Check milestones on launch
            _ = _milestoneService.CheckMilestonesAsync(_priorUsageSeconds);

            // Periodic timer: every 20 seconds check milestones and update in-memory state
            _milestoneTimer = new Timer(OnTimerTick, null, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(20));
        }

        private void OnTimerTick(object? state)
        {
            try
            {
                // To eliminate excessive flash wear and battery drain, only commit to disk every 10 minutes or on pause/exit
                bool checkpoint = (DateTime.UtcNow - _lastDiskSaveUtc).TotalMinutes >= 10;
                UpdateSessionState(persistFile: checkpoint);
                if (checkpoint)
                {
                    _lastDiskSaveUtc = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UsageTrackerService] Timer tick error: {ex.Message}");
            }
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            await _fileSemaphore.WaitAsync();
            try
            {
                if (_isInitialized) return;

                if (File.Exists(SessionsFilePath))
                {
                    var json = await File.ReadAllTextAsync(SessionsFilePath);
                    var loaded = JsonSerializer.Deserialize(json, AppJsonContext.Default.ListUsageRecord);
                    if (loaded != null)
                    {
                        lock (_lock)
                        {
                            var activeCurrent = _currentSession;
                            _sessions.Clear();
                            _sessions.AddRange(loaded);

                            if (activeCurrent != null)
                            {
                                var existingIdx = _sessions.FindIndex(x => x.SessionId == activeCurrent.SessionId);
                                if (existingIdx >= 0)
                                {
                                    _sessions[existingIdx] = activeCurrent;
                                }
                                else
                                {
                                    _sessions.Insert(0, activeCurrent);
                                }
                            }
                        }
                    }
                }
                else
                {
                    lock (_lock)
                    {
                        if (_currentSession != null && !_sessions.Any(x => x.SessionId == _currentSession.SessionId))
                        {
                            _sessions.Insert(0, _currentSession);
                        }
                    }
                }

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UsageTrackerService] Initialize error: {ex.Message}");
            }
            finally
            {
                _fileSemaphore.Release();
            }
        }

        private async Task ResolvePublicIpAsync()
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                _resolvedIp = "آفلاین / بدون اتصال";
                if (_currentSession != null)
                    _currentSession.IpAddress = _resolvedIp;
                return;
            }

            try
            {
                // Lightweight and fast IP resolver with fallback
                string ip;
                try
                {
                    ip = (await _httpClient.GetStringAsync("https://api.ipify.org")).Trim();
                }
                catch
                {
                    ip = (await _httpClient.GetStringAsync("https://icanhazip.com")).Trim();
                }

                if (!string.IsNullOrWhiteSpace(ip))
                {
                    _resolvedIp = ip;
                    if (_currentSession != null)
                    {
                        _currentSession.IpAddress = ip;
                        SaveSessionUsage();
                    }
                }
            }
            catch
            {
                _resolvedIp = "نامشخص (عدم دسترسی)";
                if (_currentSession != null)
                {
                    _currentSession.IpAddress = _resolvedIp;
                }
            }
        }

        public async Task<List<UsageRecord>> GetUsageSessionsAsync(string? searchQuery = null)
        {
            await InitializeAsync();
            await _fileSemaphore.WaitAsync();
            try
            {
                var currentList = new List<UsageRecord>(_sessions);

                // Add or update currently active session at the top of the list
                if (_currentSession != null)
                {
                    _currentSession.DurationSeconds = (long)_sessionStopwatch.Elapsed.TotalSeconds;
                    _currentSession.DataBytesUsed = _networkStatsService.SessionBytesConsumed;

                    var idx = currentList.FindIndex(x => x.SessionId == _currentSession.SessionId);
                    if (idx >= 0)
                    {
                        currentList[idx] = _currentSession;
                    }
                    else
                    {
                        currentList.Insert(0, _currentSession);
                    }
                }

                IEnumerable<UsageRecord> query = currentList;
                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    var term = searchQuery.Trim();
                    query = query.Where(x =>
                        (x.IpAddress != null && x.IpAddress.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                        PersianFormatHelper.FormatPersianDate(x.StartTimeUtc).Contains(term) ||
                        PersianFormatHelper.FormatUsageTime(TimeSpan.FromSeconds(x.DurationSeconds)).Contains(term));
                }

                return query.OrderByDescending(x => x.StartTimeUtc).ToList();
            }
            finally
            {
                _fileSemaphore.Release();
            }
        }

        public async Task DeleteSessionAsync(string sessionId)
        {
            await InitializeAsync();
            await _fileSemaphore.WaitAsync();
            try
            {
                var removed = _sessions.RemoveAll(x => x.SessionId == sessionId);
                if (_currentSession?.SessionId == sessionId)
                {
                    _currentSession = null;
                }
                if (removed > 0)
                {
                    await SaveInternalAsync();
                }
            }
            finally
            {
                _fileSemaphore.Release();
            }
        }

        public async Task ClearSessionIpAsync(string sessionId)
        {
            await InitializeAsync();
            await _fileSemaphore.WaitAsync();
            try
            {
                lock (_lock)
                {
                    var target = _sessions.FirstOrDefault(x => x.SessionId == sessionId);
                    if (target != null)
                    {
                        target.IpAddress = "بدون IP";
                    }

                    if (_currentSession?.SessionId == sessionId)
                    {
                        _currentSession.IpAddress = "بدون IP";
                    }
                }

                await SaveInternalAsync();
            }
            finally
            {
                _fileSemaphore.Release();
            }
        }

        public async Task ResetAllUsageAsync()
        {
            await InitializeAsync();
            await _fileSemaphore.WaitAsync();
            try
            {
                _sessions.Clear();
                _priorUsageSeconds = 0;
                Preferences.Set(Constants.Keys.AppTotalUsageSeconds, 0L);
                Preferences.Set(Constants.Keys.AppLastVisitUtc, string.Empty);
                _milestoneService.ResetMilestones();
                _sessionStopwatch.Reset();
                _networkStatsService.ResetAllNetworkStats();

                _currentSession = null;

                await SaveInternalAsync();
            }
            finally
            {
                _fileSemaphore.Release();
            }
        }

        public void EndCurrentSession()
        {
            if (_currentSession == null) return;

            lock (_lock)
            {
                if (_sessionStopwatch.IsRunning)
                {
                    _sessionStopwatch.Stop();
                }
                _currentSession.EndTimeUtc = DateTime.UtcNow;
                _currentSession.DurationSeconds = (long)_sessionStopwatch.Elapsed.TotalSeconds;
                _currentSession.DataBytesUsed = _networkStatsService.SessionBytesConsumed;
                HasShownWelcomeOnLoading = false;
            }

            Task.Run(async () =>
            {
                await InitializeAsync();

                lock (_lock)
                {
                    var existingIdx = _sessions.FindIndex(x => x.SessionId == _currentSession.SessionId);
                    if (existingIdx >= 0)
                    {
                        _sessions[existingIdx] = _currentSession;
                    }
                    else
                    {
                        _sessions.Insert(0, _currentSession);
                    }
                }

                SaveSessionUsage();

                await _fileSemaphore.WaitAsync();
                try
                {
                    await SaveInternalAsync();
                }
                finally
                {
                    _fileSemaphore.Release();
                }
            });
        }

        // ─── IWelcomeTrackerService compatibility implementation ──────────────

        public WelcomeInfo GetWelcomeInfo()
        {
            if (_cachedWelcomeInfo != null)
                return _cachedWelcomeInfo;

            lock (_lock)
            {
                if (_cachedWelcomeInfo != null)
                    return _cachedWelcomeInfo;

                var lastVisitStr = Preferences.Get(Constants.Keys.AppLastVisitUtc, string.Empty);
                var greeting = PersianFormatHelper.GetTimeOfDayGreeting();

                if (string.IsNullOrEmpty(lastVisitStr) ||
                    !DateTime.TryParse(lastVisitStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var lastVisitUtc))
                {
                    _cachedWelcomeInfo = new WelcomeInfo
                    {
                        IsFirstLaunch = true,
                        Title = $"{greeting}، به کپل‌آرت خوش آمدی!",
                        Subtitle = "از همراهی شما بسیار سپاسگزاریم",
                        FullMessage = $"{greeting}، به کپل‌آرت خوش آمدی!",
                        ElapsedSinceLastVisit = null
                    };
                }
                else
                {
                    var now = DateTime.UtcNow;
                    var elapsed = now > lastVisitUtc ? now - lastVisitUtc : TimeSpan.Zero;
                    var formattedTime = PersianFormatHelper.FormatTimeSinceLastVisit(elapsed);

                    _cachedWelcomeInfo = new WelcomeInfo
                    {
                        IsFirstLaunch = false,
                        Title = $"{greeting}، به کپل‌آرت خوش برگشتی!",
                        Subtitle = $"آخرین بازدید: {formattedTime}",
                        FullMessage = $"{greeting}، به کپل‌آرت خوش برگشتی! آخرین بازدید: {formattedTime}",
                        ElapsedSinceLastVisit = elapsed
                    };
                }

                RecordVisit();
                return _cachedWelcomeInfo;
            }
        }

        public void StartBrowsingTracking()
        {
            lock (_lock)
            {
                if (_currentSession == null)
                {
                    var initialIp = !string.IsNullOrWhiteSpace(_resolvedIp)
                        ? _resolvedIp
                        : (Connectivity.Current.NetworkAccess == NetworkAccess.Internet ? "در حال دریافت..." : "آفلاین / بدون اتصال");

                    _currentSession = new UsageRecord
                    {
                        StartTimeUtc = DateTime.UtcNow,
                        IpAddress = initialIp,
                        DurationSeconds = 0,
                        DataBytesUsed = _networkStatsService.SessionBytesConsumed
                    };

                    if (!_sessions.Any(x => x.SessionId == _currentSession.SessionId))
                    {
                        _sessions.Insert(0, _currentSession);
                    }

                    if (string.IsNullOrWhiteSpace(_resolvedIp) || _resolvedIp == "در حال دریافت...")
                    {
                        _ = ResolvePublicIpAsync();
                    }
                }

                if (!_sessionStopwatch.IsRunning)
                {
                    _sessionStopwatch.Start();
                }
            }

            SaveSessionUsage();
        }

        public void PauseBrowsingTracking()
        {
            lock (_lock)
            {
                if (_sessionStopwatch.IsRunning)
                {
                    _sessionStopwatch.Stop();
                }

                if (_currentSession != null)
                {
                    _currentSession.DurationSeconds = (long)_sessionStopwatch.Elapsed.TotalSeconds;
                    _currentSession.DataBytesUsed = _networkStatsService.SessionBytesConsumed;
                    _currentSession.EndTimeUtc = DateTime.UtcNow;
                }
            }
            SaveSessionUsage();
        }

        public bool IsBrowsingActive
        {
            get
            {
                lock (_lock)
                {
                    return _sessionStopwatch.IsRunning;
                }
            }
        }

        public TimeSpan GetCurrentUsageDuration()
        {
            lock (_lock)
            {
                var totalSec = _priorUsageSeconds + _sessionStopwatch.Elapsed.TotalSeconds;
                return TimeSpan.FromSeconds(Math.Max(0, totalSec));
            }
        }

        public string GetFormattedUsageTime()
        {
            return PersianFormatHelper.FormatUsageTime(GetCurrentUsageDuration());
        }

        public void SaveSessionUsage()
        {
            _lastDiskSaveUtc = DateTime.UtcNow;
            UpdateSessionState(persistFile: true);
        }

        private void UpdateSessionState(bool persistFile)
        {
            try
            {
                long totalSec;
                lock (_lock)
                {
                    totalSec = (long)(_priorUsageSeconds + _sessionStopwatch.Elapsed.TotalSeconds);
                }

                Preferences.Set(Constants.Keys.AppTotalUsageSeconds, totalSec);
                _ = _milestoneService.CheckMilestonesAsync(totalSec);

                _networkStatsService.RecordTrafficDelta();

                if (_currentSession != null)
                {
                    lock (_lock)
                    {
                        _currentSession.DurationSeconds = (long)_sessionStopwatch.Elapsed.TotalSeconds;
                        _currentSession.DataBytesUsed = _networkStatsService.SessionBytesConsumed;

                        var existingIdx = _sessions.FindIndex(x => x.SessionId == _currentSession.SessionId);
                        if (existingIdx >= 0)
                        {
                            _sessions[existingIdx] = _currentSession;
                        }
                        else
                        {
                            _sessions.Insert(0, _currentSession);
                        }
                    }

                    if (persistFile)
                    {
                        Task.Run(async () =>
                        {
                            await InitializeAsync();

                            if (await _fileSemaphore.WaitAsync(500))
                            {
                                try
                                {
                                    await SaveInternalAsync();
                                }
                                finally
                                {
                                    _fileSemaphore.Release();
                                }
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UsageTrackerService] UpdateSessionState error: {ex.Message}");
            }
        }

        public void RecordVisit()
        {
            try
            {
                Preferences.Set(Constants.Keys.AppLastVisitUtc, DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
                SaveSessionUsage();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UsageTrackerService] RecordVisit error: {ex.Message}");
            }
        }

        public async Task ImportSessionsAsync(List<UsageRecord> incoming, long incomingTotalUsageSeconds, bool overwrite)
        {
            if (incoming == null) return;

            await InitializeAsync();
            await _fileSemaphore.WaitAsync();
            try
            {
                if (overwrite)
                {
                    _sessions.Clear();
                    _sessions.AddRange(incoming.OrderByDescending(x => x.StartTimeUtc));
                    _priorUsageSeconds = Math.Max(0, incomingTotalUsageSeconds);
                    Preferences.Set(Constants.Keys.AppTotalUsageSeconds, _priorUsageSeconds);
                }
                else
                {
                    foreach (var session in incoming)
                    {
                        if (!_sessions.Any(x => x.SessionId == session.SessionId))
                        {
                            _sessions.Add(session);
                        }
                    }
                    _sessions.Sort((a, b) => b.StartTimeUtc.CompareTo(a.StartTimeUtc));
                    _priorUsageSeconds = Math.Max(_priorUsageSeconds, incomingTotalUsageSeconds);
                    Preferences.Set(Constants.Keys.AppTotalUsageSeconds, _priorUsageSeconds);
                }

                if (_sessions.Count > 500)
                {
                    _sessions.RemoveRange(500, _sessions.Count - 500);
                }

                await SaveInternalAsync();
            }
            finally
            {
                _fileSemaphore.Release();
            }
        }

        private async Task SaveInternalAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_sessions, AppJsonContext.Default.ListUsageRecord);
                await File.WriteAllTextAsync(SessionsFilePath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UsageTrackerService] SaveInternalAsync error: {ex.Message}");
            }
        }
    }
}
