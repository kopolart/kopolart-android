using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KopolArt.Mobile.Models
{
    public class UsageDisplayRecord : INotifyPropertyChanged
    {
        private string _sessionId = string.Empty;
        private string _formattedDate = string.Empty;
        private string _formattedDuration = string.Empty;
        private string _formattedData = string.Empty;
        private string _ipAddress = string.Empty;

        public string SessionId
        {
            get => _sessionId;
            set
            {
                if (_sessionId != value)
                {
                    _sessionId = value;
                    OnPropertyChanged();
                }
            }
        }

        public string FormattedDate
        {
            get => _formattedDate;
            set
            {
                if (_formattedDate != value)
                {
                    _formattedDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public string FormattedDuration
        {
            get => _formattedDuration;
            set
            {
                if (_formattedDuration != value)
                {
                    _formattedDuration = value;
                    OnPropertyChanged();
                }
            }
        }

        public string FormattedData
        {
            get => _formattedData;
            set
            {
                if (_formattedData != value)
                {
                    _formattedData = value;
                    OnPropertyChanged();
                }
            }
        }

        public string IpAddress
        {
            get => _ipAddress;
            set
            {
                if (_ipAddress != value)
                {
                    _ipAddress = value;
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
