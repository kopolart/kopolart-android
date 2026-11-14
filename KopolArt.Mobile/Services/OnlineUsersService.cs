using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services
{
    public class OnlineUsersService : IOnlineUsersService
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        private int? _cachedCount;
        private DateTime _lastFetchUtc = DateTime.MinValue;

        public OnlineUsersService()
        {
            _httpClient = new HttpClient
            {
                Timeout = RequestTimeout
            };

            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", Constants.Network.AppUserAgent);
        }

        public async Task<int?> GetOnlineUsersCountAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            // 1. Check in-memory cache if not forced
            if (!forceRefresh && _cachedCount.HasValue && (DateTime.UtcNow - _lastFetchUtc) < CacheDuration)
            {
                return _cachedCount;
            }

            // 2. Connectivity check - if offline, return cached value or null immediately without blocking
            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    return _cachedCount;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OnlineUsersService] Connectivity check warning: {ex.Message}");
            }

            // 3. Acquire lock to prevent duplicate parallel requests
            if (!await _semaphore.WaitAsync(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false))
            {
                return _cachedCount;
            }

            try
            {
                // Double check after lock
                if (!forceRefresh && _cachedCount.HasValue && (DateTime.UtcNow - _lastFetchUtc) < CacheDuration)
                {
                    return _cachedCount;
                }

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(RequestTimeout);

                // Cache-busting URL to prevent OkHttp / Android OS / proxy caching of GET response
                var cacheBuster = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var requestUrl = $"{Constants.Urls.TopBarInfoUrl}?_t={cacheBuster}";

                using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.CacheControl = new CacheControlHeaderValue
                {
                    NoCache = true,
                    NoStore = true,
                    MustRevalidate = true,
                    MaxAge = TimeSpan.Zero
                };
                request.Headers.Pragma.ParseAdd("no-cache");

                using var responseMessage = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cts.Token).ConfigureAwait(false);
                if (!responseMessage.IsSuccessStatusCode)
                {
                    return _cachedCount;
                }

                var response = await responseMessage.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(response))
                {
                    return _cachedCount;
                }

                // Trim-safe source-generated deserialization
                var data = JsonSerializer.Deserialize(response, AppJsonContext.Default.OnlineUsersInfo);
                if (data != null)
                {
                    _cachedCount = data.Online;
                    _lastFetchUtc = DateTime.UtcNow;
                    return _cachedCount;
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("[OnlineUsersService] Request timed out or was cancelled.");
            }
            catch (Exception ex)
            {
                // Never throw or crash the app on offline or server issues
                Debug.WriteLine($"[OnlineUsersService] Failed to fetch online users: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }

            return _cachedCount;
        }
    }
}
