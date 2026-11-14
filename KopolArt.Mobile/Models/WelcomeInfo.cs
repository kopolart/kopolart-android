namespace KopolArt.Mobile.Models
{
    public class WelcomeInfo
    {
        public bool IsFirstLaunch { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Subtitle { get; init; }
        public string FullMessage { get; init; } = string.Empty;
        public TimeSpan? ElapsedSinceLastVisit { get; init; }
    }
}
