namespace KopolArt.Mobile.Models
{
    /// <summary>
    /// Tracks a single in-flight download so the notification actions can pause,
    /// resume or cancel it without restarting the transfer.
    /// </summary>
    public class ActiveDownloadSession
    {
        private readonly object _stateLock = new();
        private readonly CancellationTokenSource _cancellation = new();
        private readonly SemaphoreSlim _resumeSignal = new(0, 1);
        private readonly Action<int, bool> _onStateChanged;

        private bool _isPaused;

        public int Id { get; }
        public string FileName { get; }
        public int CurrentProgress { get; set; } = -1;
        public CancellationToken CancellationToken => _cancellation.Token;

        public bool IsPaused
        {
            get
            {
                lock (_stateLock)
                {
                    return _isPaused;
                }
            }
        }

        public ActiveDownloadSession(int id, string fileName, Action<int, bool> onStateChanged)
        {
            Id = id;
            FileName = fileName;
            _onStateChanged = onStateChanged;
        }

        /// <summary>
        /// Blocks only while the session is paused; returns immediately otherwise.
        /// </summary>
        public async Task WaitWhilePausedAsync(CancellationToken cancellationToken)
        {
            while (IsPaused)
            {
                await _resumeSignal.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public void Pause()
        {
            lock (_stateLock)
            {
                if (_isPaused)
                    return;

                _isPaused = true;
            }

            _onStateChanged(CurrentProgress, true);
        }

        public void Resume()
        {
            lock (_stateLock)
            {
                if (!_isPaused)
                    return;

                _isPaused = false;
            }

            ReleaseWaiter();
            _onStateChanged(CurrentProgress, false);
        }

        public void Cancel()
        {
            lock (_stateLock)
            {
                _isPaused = false;
            }

            ReleaseWaiter();
            _cancellation.Cancel();
        }

        private void ReleaseWaiter()
        {
            try
            {
                _resumeSignal.Release();
            }
            catch (SemaphoreFullException)
            {
                // Nobody is blocked right now; the copy loop re-checks the pause flag each chunk.
            }
        }
    }
}
