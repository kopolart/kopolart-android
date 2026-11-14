using Android.Content;

namespace KopolArt.Mobile.Platforms.Android
{
    public partial class MainActivity
    {
        // ─── Folder picker (SAF ACTION_OPEN_DOCUMENT_TREE) ─────────────────────
        public const int FolderSelectCode = 101;
        private static TaskCompletionSource<(string? UriString, string? DisplayName)?>? _folderPickerTcs;

        public static Task<(string? UriString, string? DisplayName)?> PickFolderAsync()
        {
            var activity = Instance;
            if (activity == null)
            {
                return Task.FromResult<(string? UriString, string? DisplayName)?>(null);
            }

            _folderPickerTcs?.TrySetCanceled();
            _folderPickerTcs = new TaskCompletionSource<(string? UriString, string? DisplayName)?>();

            try
            {
                var intent = new Intent(Intent.ActionOpenDocumentTree);
                intent.AddFlags(ActivityFlags.GrantReadUriPermission
                              | ActivityFlags.GrantWriteUriPermission
                              | ActivityFlags.GrantPersistableUriPermission
                              | ActivityFlags.GrantPrefixUriPermission);

                activity.StartActivityForResult(intent, FolderSelectCode);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainActivity] PickFolderAsync error: {ex.Message}");
                _folderPickerTcs.TrySetResult(null);
            }

            return _folderPickerTcs.Task;
        }

    }
}
