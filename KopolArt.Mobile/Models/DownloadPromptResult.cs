namespace KopolArt.Mobile.Models
{
    public enum DownloadPromptAction
    {
        Continue,
        ChangeFolder,
        Cancel
    }

    public class DownloadPromptResult
    {
        public DownloadPromptAction Action { get; set; } = DownloadPromptAction.Continue;
        public bool DontShowAgain { get; set; }
    }
}
