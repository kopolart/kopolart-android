namespace KopolArt.Mobile.Models
{
    public class BookmarkItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
