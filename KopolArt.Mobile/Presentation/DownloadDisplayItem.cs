using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KopolArt.Mobile.Presentation
{
    public class DownloadDisplayItem : INotifyPropertyChanged
    {
        private bool _canSetAsWallpaper;

        public string Id { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public string LocalFilePath { get; set; } = string.Empty;
        public string FormattedSize { get; set; } = string.Empty;
        public string FormattedDate { get; set; } = string.Empty;
        public DateTime DownloadedAtUtc { get; set; }
        public bool FileExistsLocally { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public Color StatusColor { get; set; } = Colors.Gray;
        public string CategoryName { get; set; } = string.Empty;
        public Color BadgeTextColor { get; set; } = Colors.Gray;
        public Color BadgeBackgroundColor { get; set; } = Colors.Transparent;
        public Color BadgeBorderColor { get; set; } = Colors.Transparent;

        /// <summary>
        /// User-friendly label for where the file is stored (e.g. "پوشه بارگیری‌های دستگاه (Downloads)").
        /// </summary>
        public string StorageLocationDescription { get; set; } = string.Empty;

        /// <summary>
        /// True if there is a storage path to display or copy.
        /// </summary>
        public bool HasStorageLocation => !string.IsNullOrWhiteSpace(StorageLocationDescription);

        /// <summary>
        /// True when the item is a downloaded wallpaper that may support "set as wallpaper".
        /// The pixel dimensions are verified later by the background probe.
        /// </summary>
        public bool IsWallpaperCandidate { get; set; }

        /// <summary>
        /// True once the background probe confirmed the image is a usable portrait.
        /// </summary>
        public bool CanSetAsWallpaper
        {
            get => _canSetAsWallpaper;
            set
            {
                if (_canSetAsWallpaper == value)
                    return;

                _canSetAsWallpaper = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
