using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services
{
    public interface ICustomAlertService
    {
        Task ShowAlertAsync(string title, string message, string buttonText = "تأیید", AlertType type = AlertType.Info);
        Task<bool> ShowConfirmAsync(string title, string message, string acceptText = "تأیید", string cancelText = "انصراف", AlertType type = AlertType.Question, bool isDestructive = false);
        Task<string?> ShowChoiceAsync(string title, string message, string primaryActionText, string secondaryActionText, string cancelText = "انصراف", AlertType type = AlertType.Question);
        Task<string?> ShowOptionsAsync(string title, string? message, IEnumerable<string> options, string cancelText = "انصراف", AlertType type = AlertType.Info);
        Task<string?> ShowGridOptionsAsync(string title, string? message, IEnumerable<AlertOptionItem> options, string cancelText = "انصراف", AlertType type = AlertType.Info, int columns = 2);
        Task<DownloadPromptResult?> ShowDownloadLocationPromptAsync(string currentFolderDisplay);
    }
}
