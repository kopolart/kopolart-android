namespace KopolArt.Mobile.Models
{
    public class HistoryItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime VisitedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
