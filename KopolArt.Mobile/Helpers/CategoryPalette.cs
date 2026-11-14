namespace KopolArt.Mobile.Helpers
{
    /// <summary>
    /// Per-category accent palette shared by dashboard shortcuts and search categories.
    /// </summary>
    public sealed class CategoryPalette
    {
        public Color AccentLight { get; }
        public Color AccentDark { get; }
        public Color BadgeBackgroundLight { get; }
        public Color BadgeBackgroundDark { get; }
        public Color BorderLight { get; }
        public Color BorderDark { get; }

        private CategoryPalette(
            string accentLight,
            string accentDark,
            string badgeBackgroundLight,
            string badgeBackgroundDark,
            string borderLight,
            string borderDark)
        {
            AccentLight = Color.FromArgb(accentLight);
            AccentDark = Color.FromArgb(accentDark);
            BadgeBackgroundLight = Color.FromArgb(badgeBackgroundLight);
            BadgeBackgroundDark = Color.FromArgb(badgeBackgroundDark);
            BorderLight = Color.FromArgb(borderLight);
            BorderDark = Color.FromArgb(borderDark);
        }

        public static readonly CategoryPalette Colors = new("#D97706", "#FBBF24", "#1AD97706", "#30FBBF24", "#33D97706", "#55FBBF24");
        public static readonly CategoryPalette Palettes = new("#0D9488", "#2DD4BF", "#1A0D9488", "#302DD4BF", "#330D9488", "#552DD4BF");
        public static readonly CategoryPalette Posts = new("#249CF0", "#60A5FA", "#1A249CF0", "#3060A5FA", "#33249CF0", "#5560A5FA");
        public static readonly CategoryPalette Wallpapers = new("#9333EA", "#C084FC", "#1A9333EA", "#30C084FC", "#339333EA", "#55C084FC");
        public static readonly CategoryPalette Tools = new("#059669", "#34D399", "#1A059669", "#3034D399", "#33059669", "#5534D399");
    }
}
