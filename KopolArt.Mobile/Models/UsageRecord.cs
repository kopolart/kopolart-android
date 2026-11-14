namespace KopolArt.Mobile.Models
{
    public class UsageRecord
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime StartTimeUtc { get; set; } = DateTime.UtcNow;
        public DateTime? EndTimeUtc { get; set; }
        public long DurationSeconds { get; set; }
        public long DataBytesUsed { get; set; }
        public string IpAddress { get; set; } = "آفلاین / بدون اتصال";
    }
}
