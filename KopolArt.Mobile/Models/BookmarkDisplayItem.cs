namespace KopolArt.Mobile.Models
{
    public class BookmarkDisplayItem
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public Color BadgeTextColor { get; set; } = Colors.Gray;
        public Color BadgeBackgroundColor { get; set; } = Colors.Transparent;
        public Color BadgeBorderColor { get; set; } = Colors.Transparent;
        public string RelativeTime { get; set; } = string.Empty;
        public string FullPersianDate { get; set; } = string.Empty;
        public DateTime SavedAtUtc { get; set; }
    }
}
