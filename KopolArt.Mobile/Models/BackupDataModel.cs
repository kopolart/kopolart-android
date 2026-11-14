namespace KopolArt.Mobile.Models
{
    public class BackupDataModel
    {
        public int SchemaVersion { get; set; } = 1;
        public string AppName { get; set; } = "KopolArt.Mobile";
        public string AppVersion { get; set; } = string.Empty;
        public DateTime ExportedAtUtc { get; set; } = DateTime.UtcNow;
        public BackupPayload Data { get; set; } = new();
    }
}

