namespace KopolArt.Mobile.Models
{
    public class OnboardingSlideModel
    {
        public int SlideIndex { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconGlyph { get; set; } = string.Empty;

        public string BadgeText { get; set; } = string.Empty;
        public bool IsNotificationSlide { get; set; }
    }
}
