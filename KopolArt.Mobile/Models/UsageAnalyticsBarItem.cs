using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KopolArt.Mobile.Models
{
    public class UsageAnalyticsBarItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public DateTime Date { get; set; }
        public string DateKey { get; set; } = string.Empty;
        public long DurationSeconds { get; set; }
        public long DataBytesUsed { get; set; }

        public string DayName { get; set; } = string.Empty;
        public string ShortDayName { get; set; } = string.Empty;
        public string DayNumber { get; set; } = string.Empty;
        public string FullDateString { get; set; } = string.Empty;
        public string FormattedDuration { get; set; } = "۰ ثانیه";
        public string FormattedData { get; set; } = "۰ بایت";

        public double BarHeightDp { get; set; } = 6.0;
        public bool IsPeak { get; set; }
        public bool IsToday { get; set; }
        public bool HasUsage => DurationSeconds > 0;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
