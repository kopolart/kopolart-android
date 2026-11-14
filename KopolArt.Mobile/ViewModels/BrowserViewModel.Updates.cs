using KopolArt.Mobile.Helpers;

namespace KopolArt.Mobile.ViewModels
{
    public partial class BrowserViewModel
    {
        public async Task CheckForUpdatesAsync()
        {
            if (!_updateService.ShouldCheckForUpdates())
                return;
            try
            {
                var updateInfo = await _updateService.FetchUpdateInfoAsync();
                if (updateInfo == null) return;
                var currentVersion = AppInfo.Current.VersionString;
                var newReleases = _updateService.GetNewReleases(updateInfo, currentVersion);
                if (newReleases.Count > 0)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (!_disposed)
                        {
                            RequestShowUpdate?.Invoke(this, (updateInfo, newReleases));
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BrowserViewModel] Update check failed: {ex.Message}");
            }
        }
        private async Task RetryAfterDelayAsync(int retryAttempt)
        {
            CancelRetry();
            _retryCts = new CancellationTokenSource();
            var token = _retryCts.Token;
            try
            {
                if (!_hasSuccessfullyLoadedPage)
                {
                    SetRetryTextOnMainThread($"در حال تلاش مجدد ({retryAttempt.ToPersianDigits()}/{MaxRetries.ToPersianDigits()})...");
                }
                var delay = BaseRetryDelayMs * retryAttempt;
                await Task.Delay(delay, token);
                if (token.IsCancellationRequested || _disposed)
                    return;
                if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
                {
                    RaiseReloadRequest();
                }
                else
                {
                    if (_hasSuccessfullyLoadedPage)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            IsLoading = false;
                            IsBrowserVisible = true;
                            IsError = false;
                        });
                    }
                    else
                    {
                        ShowErrorOnMainThread();
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // ignored
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Retry error: {ex.Message}");
                if (_hasSuccessfullyLoadedPage)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        IsLoading = false;
                        IsBrowserVisible = true;
                        IsError = false;
                    });
                }
                else
                {
                    ShowErrorOnMainThread();
                }
            }
        }
    }
}
