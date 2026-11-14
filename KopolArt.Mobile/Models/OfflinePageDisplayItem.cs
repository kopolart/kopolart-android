namespace KopolArt.Mobile.Models
{
    public class OfflinePageDisplayItem
    {
        public OfflinePageItem Item { get; set; } = new();
        public string Title => string.IsNullOrWhiteSpace(Item.Title) ? "صفحه بدون عنوان" : Item.Title;
        public string OriginalUrl => Item.OriginalUrl;
        public string FilePath => Item.FilePath;
        public string FormattedDate { get; set; } = string.Empty;
        public string FormattedSize { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public Color CategoryTextColor { get; set; } = Colors.Gray;
        public Color CategoryBgColor { get; set; } = Colors.Transparent;
        public Color CategoryBorderColor { get; set; } = Colors.Transparent;
    }
}
