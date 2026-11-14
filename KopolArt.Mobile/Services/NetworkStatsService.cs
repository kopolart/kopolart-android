using KopolArt.Mobile.Helpers;

namespace KopolArt.Mobile.Services
{
    public class NetworkStatsService : INetworkStatsService
    {
        private const string PrefKeyCumulativeBytes = "AppCumulativeNetworkBytes";
        private const string PrefKeyLastBootRx = "AppLastBootRxBytes";
        private const string PrefKeyLastBootTx = "AppLastBootTxBytes";

        private readonly object _lock = new();
        private long _cumulativeBytes;
        private long _initialSessionBytes;
        private long _lastSampleRx = -1;
        private long _lastSampleTx = -1;

        public NetworkStatsService()
        {
            _cumulativeBytes = Preferences.Get(PrefKeyCumulativeBytes, 0L);
            SampleInitialValues();
        }

        public long TotalBytesConsumed
        {
            get
            {
                lock (_lock)
                {
                    RecordTrafficDeltaInternal();
                    return _cumulativeBytes;
                }
            }
        }

        public long SessionBytesConsumed
        {
            get
            {
                lock (_lock)
                {
                    RecordTrafficDeltaInternal();
                    var total = _cumulativeBytes - _initialSessionBytes;
                    return Math.Max(0, total);
                }
            }
        }

        public string FormattedTotalBytes => PersianFormatHelper.FormatBytes(TotalBytesConsumed);
        public string FormattedSessionBytes => PersianFormatHelper.FormatBytes(SessionBytesConsumed);

        public void RecordTrafficDelta()
        {
            lock (_lock)
            {
                RecordTrafficDeltaInternal();
            }
        }

        public void ResetAllNetworkStats()
        {
            lock (_lock)
            {
                _cumulativeBytes = 0;
                Preferences.Set(PrefKeyCumulativeBytes, 0L);
                SampleInitialValues();
            }
        }

        private void SampleInitialValues()
        {
#if ANDROID
            try
            {
                int uid = Android.OS.Process.MyUid();
                long rx = Android.Net.TrafficStats.GetUidRxBytes(uid);
                long tx = Android.Net.TrafficStats.GetUidTxBytes(uid);

                if (rx != Android.Net.TrafficStats.Unsupported && tx != Android.Net.TrafficStats.Unsupported)
                {
                    _lastSampleRx = rx;
                    _lastSampleTx = tx;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NetworkStatsService] Sample error: {ex.Message}");
            }
#endif
            _initialSessionBytes = _cumulativeBytes;
        }

        private void RecordTrafficDeltaInternal()
        {
#if ANDROID
            try
            {
                int uid = Android.OS.Process.MyUid();
                long currentRx = Android.Net.TrafficStats.GetUidRxBytes(uid);
                long currentTx = Android.Net.TrafficStats.GetUidTxBytes(uid);

                if (currentRx == Android.Net.TrafficStats.Unsupported || currentTx == Android.Net.TrafficStats.Unsupported)
                    return;

                if (_lastSampleRx < 0 || _lastSampleTx < 0)
                {
                    _lastSampleRx = currentRx;
                    _lastSampleTx = currentTx;
                    return;
                }

                // Device reboot detection: if current reading is less than previous reading, device rebooted
                long deltaRx = currentRx >= _lastSampleRx ? currentRx - _lastSampleRx : currentRx;
                long deltaTx = currentTx >= _lastSampleTx ? currentTx - _lastSampleTx : currentTx;

                long deltaTotal = deltaRx + deltaTx;
                if (deltaTotal > 0)
                {
                    _cumulativeBytes += deltaTotal;
                    Preferences.Set(PrefKeyCumulativeBytes, _cumulativeBytes);
                }

                _lastSampleRx = currentRx;
                _lastSampleTx = currentTx;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NetworkStatsService] Delta calculation error: {ex.Message}");
            }
#endif
        }
    }
}
