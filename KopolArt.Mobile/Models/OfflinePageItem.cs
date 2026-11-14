namespace KopolArt.Mobile.Models
{
    public class OfflinePageItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = string.Empty;
        public string OriginalUrl { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;
        public long FileSizeBytes { get; set; }
    }
}
