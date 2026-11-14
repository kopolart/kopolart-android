namespace KopolArt.Mobile.ViewModels
{
    public partial class BrowserViewModel
    {
        public void TriggerLoadingState()
        {
            SetLoadingState();
        }
        private void SetLoadingState()
        {
            IsLoading = true;
            IsError = false;
            IsBrowserVisible = true;
            RetryText = string.Empty;
            ShowRetryText = false;
            ShowWelcomeOnLoading = !_usageTrackerService.HasShownWelcomeOnLoading && !_isInitialLoadDone && HasWelcomeMessage;
        }
        private void SetSuccessState()
        {
            CancelRetry();
            IsLoading = false;
            IsError = false;
            IsBrowserVisible = true;
            RetryText = string.Empty;
            ShowRetryText = false;
            ShowVersion = false;
        }
        private void ShowError()
        {
            CancelRetry();
            IsPullRefreshing = false;
            IsLoading = false;
            IsError = true;
            IsBrowserVisible = false;
            RetryText = string.Empty;
            ShowRetryText = false;
        }
        private void ShowErrorOnMainThread()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!_disposed)
                    ShowError();
            });
        }
        private void SetRetryTextOnMainThread(string text)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_disposed)
                    return;
                RetryText = text;
                ShowRetryText = !string.IsNullOrWhiteSpace(text);
            });
        }
        private void RaiseReloadRequest()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!_disposed)
                    RequestReload?.Invoke(this, EventArgs.Empty);
            });
        }
        private void CancelRetry()
        {
            if (_retryCts is null)
                return;
            try
            {
                _retryCts.Cancel();
                _retryCts.Dispose();
            }
            finally
            {
                _retryCts = null;
            }
        }
    }
}
