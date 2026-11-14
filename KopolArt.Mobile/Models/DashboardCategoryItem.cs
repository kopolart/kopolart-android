using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using KopolArt.Mobile.Helpers;

namespace KopolArt.Mobile.Models
{
    public class DashboardCategoryItem : INotifyPropertyChanged
    {
        private Color _accentColor = Colors.Gray;
        private Color _badgeBgColor = Colors.Transparent;
        private Color _borderColor = Colors.Transparent;
        private int _badgeCount;
        private string? _badgeCountText;
        private bool _hasBadge;

        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string IconGlyph { get; set; } = string.Empty;
        public ICommand? OpenCommand { get; set; }

        public Color AccentColorLight { get; set; } = Colors.Gray;
        public Color AccentColorDark { get; set; } = Colors.Gray;
        public Color BadgeBgLight { get; set; } = Colors.Transparent;
        public Color BadgeBgDark { get; set; } = Colors.Transparent;
        public Color BorderLight { get; set; } = Colors.Transparent;
        public Color BorderDark { get; set; } = Colors.Transparent;

        public Color AccentColor
        {
            get => _accentColor;
            set
            {
                if (_accentColor != value)
                {
                    _accentColor = value;
                    OnPropertyChanged();
                }
            }
        }

        public Color BadgeBgColor
        {
            get => _badgeBgColor;
            set
            {
                if (_badgeBgColor != value)
                {
                    _badgeBgColor = value;
                    OnPropertyChanged();
                }
            }
        }

        public Color BorderColor
        {
            get => _borderColor;
            set
            {
                if (_borderColor != value)
                {
                    _borderColor = value;
                    OnPropertyChanged();
                }
            }
        }

        public int BadgeCount
        {
            get => _badgeCount;
            set
            {
                if (_badgeCount != value)
                {
                    _badgeCount = value;
                    HasBadge = value > 0;
                    BadgeCountText = value > 0 ? value.ToPersianDigits() : null;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasBadge
        {
            get => _hasBadge;
            private set
            {
                if (_hasBadge != value)
                {
                    _hasBadge = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? BadgeCountText
        {
            get => _badgeCountText;
            private set
            {
                if (_badgeCountText != value)
                {
                    _badgeCountText = value;
                    OnPropertyChanged();
                }
            }
        }

        public void ApplyTheme(bool isDark)
        {
            AccentColor = isDark ? AccentColorDark : AccentColorLight;
            BadgeBgColor = isDark ? BadgeBgDark : BadgeBgLight;
            BorderColor = isDark ? BorderDark : BorderLight;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
