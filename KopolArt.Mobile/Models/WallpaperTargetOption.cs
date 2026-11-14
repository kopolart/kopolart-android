using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KopolArt.Mobile.Models
{
    public class WallpaperTargetOption : INotifyPropertyChanged
    {
        private bool _isSelected;

        public WallpaperTarget Target { get; init; }
        public string Title { get; init; } = string.Empty;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                    return;

                _isSelected = value;
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
