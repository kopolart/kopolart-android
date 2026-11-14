using KopolArt.Mobile.Services;

namespace KopolArt.Mobile.Platforms.Android.Services
{
    public class AndroidDownloadFolderPickerService : IDownloadFolderPickerService
    {
        public Task<(string? UriString, string? DisplayName)?> PickFolderAsync()
        {
            return MainActivity.PickFolderAsync();
        }
    }
}
