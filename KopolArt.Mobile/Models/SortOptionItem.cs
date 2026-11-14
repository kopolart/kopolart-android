using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KopolArt.Mobile.Models
{
    public class SortOptionItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;

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
