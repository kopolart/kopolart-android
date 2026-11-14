namespace KopolArt.Mobile.Models
{
    public class AppSettings
    {
        public bool ShowLoadingOnNavigation { get; set; } = true;
        public bool OpenDashboardAtStartup { get; set; } = true;
        public bool RestoreLastPageAtStartup { get; set; } = false;
        public bool AutoClearCacheOnExit { get; set; } = false;
        public string AppThemeMode { get; set; } = "System"; // "System", "Dark", "Light"
        public string LastVisitedUrl { get; set; } = Constants.Urls.Home;
        public string LastRunAppVersion { get; set; } = string.Empty;
        public bool HasCompletedOnboarding { get; set; } = false;
        public string? CustomDownloadFolderUri { get; set; }
        public string? CustomDownloadFolderName { get; set; }
        public bool DontShowDownloadPromptAgain { get; set; } = false;
        public int WidgetOpacity { get; set; } = 100;
        public bool NotifyNewContent { get; set; } = true;
    }
}
