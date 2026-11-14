namespace KopolArt.Mobile.Models
{
    public class DownloadItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string FileName { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime DownloadedAtUtc { get; set; } = DateTime.UtcNow;
        public string LocalFilePath { get; set; } = string.Empty;
        public string Status { get; set; } = "Completed"; // "Completed", "Failed"
    }
}
