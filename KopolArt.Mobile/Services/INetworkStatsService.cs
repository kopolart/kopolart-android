namespace KopolArt.Mobile.Services
{
    public interface INetworkStatsService
    {
        long TotalBytesConsumed { get; }
        long SessionBytesConsumed { get; }
        string FormattedTotalBytes { get; }
        string FormattedSessionBytes { get; }

        void RecordTrafficDelta();
        void ResetAllNetworkStats();
    }
}
