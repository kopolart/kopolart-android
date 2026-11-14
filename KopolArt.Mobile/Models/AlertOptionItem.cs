namespace KopolArt.Mobile.Models
{
    public class AlertOptionItem
    {
        public string Text { get; set; } = string.Empty;
        public string? IconGlyph { get; set; }
        public string? IconFontFamily { get; set; } = Constants.Fonts.KopolArtFaAlias;
        public string? ReturnValue { get; set; }

        public AlertOptionItem() { }

        public AlertOptionItem(string text, string? iconGlyph = null, string? returnValue = null, string? iconFontFamily = null)
        {
            Text = text;
            IconGlyph = iconGlyph;
            ReturnValue = returnValue ?? text;
            IconFontFamily = iconFontFamily ?? Constants.Fonts.KopolArtFaAlias;
        }
    }
}
