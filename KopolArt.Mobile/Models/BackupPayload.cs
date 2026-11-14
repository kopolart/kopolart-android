namespace KopolArt.Mobile.Models
{
    public class BackupPayload
    {
        public AppSettings? Settings { get; set; }
        public List<BookmarkItem>? Bookmarks { get; set; }
        public List<HistoryItem>? History { get; set; }
        public List<DownloadItem>? Downloads { get; set; }
        public UsageBackupData? Usage { get; set; }
        public List<OfflinePageItem>? OfflinePages { get; set; }
        public List<string>? RecentSearches { get; set; }
    }
}
