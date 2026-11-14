namespace KopolArt.Mobile.Services
{
    public interface IDownloadFolderPickerService
    {
        Task<(string? UriString, string? DisplayName)?> PickFolderAsync();
    }
}
