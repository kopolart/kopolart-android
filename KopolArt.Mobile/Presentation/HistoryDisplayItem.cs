using System.ComponentModel;
using System.Runtime.CompilerServices;
using KopolArt.Mobile.Helpers;

namespace KopolArt.Mobile.Presentation
{
    public class HistoryDisplayItem : INotifyPropertyChanged
    {
        private bool _isBookmarked;

        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public Color BadgeTextColor { get; set; } = Colors.Gray;
        public Color BadgeBackgroundColor { get; set; } = Colors.Transparent;
        public Color BadgeBorderColor { get; set; } = Colors.Transparent;
        public string RelativeTime { get; set; } = string.Empty;
        public string FullPersianDate { get; set; } = string.Empty;
        public DateTime VisitedAtUtc { get; set; }

        public bool IsBookmarked
        {
            get => _isBookmarked;
            set
            {
                if (_isBookmarked != value)
                {
                    _isBookmarked = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FavoriteActionText));
                    OnPropertyChanged(nameof(FavoriteActionIcon));
                }
            }
        }

        public string FavoriteActionText => IsBookmarked ? "حذف نشان" : "نشان‌کردن";
        public string FavoriteActionIcon => IsBookmarked ? KopolArtFaIcons.BookmarkEmpty : KopolArtFaIcons.Bookmark;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
