namespace KopolArt.Mobile.Models
{
    public class UpdateReleaseDisplayItem
    {
        public string Version { get; set; } = string.Empty;
        public string VersionDisplay { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string PersianDate { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public bool IsCurrentVersion { get; set; }
        public bool HasDate => !string.IsNullOrWhiteSpace(PersianDate);
    }
}
