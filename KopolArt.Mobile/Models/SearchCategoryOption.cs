namespace KopolArt.Mobile.Models
{
    public enum SearchCategoryType
    {
        Posts,
        Colors,
        Palettes,
        Wallpapers
    }

    public class SearchCategoryOption
    {
        public SearchCategoryType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ShortTitle { get; set; } = string.Empty;
        public string SearchUrl { get; set; } = string.Empty;
        public string IconGlyph { get; set; } = string.Empty;
        public string Placeholder { get; set; } = string.Empty;
        public Color AccentColorLight { get; set; } = Colors.Transparent;
        public Color AccentColorDark { get; set; } = Colors.Transparent;

        public Color GetAccentColor(bool isDark) => isDark ? AccentColorDark : AccentColorLight;
    }
}
