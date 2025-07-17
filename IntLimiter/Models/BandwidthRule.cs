using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NetLimiterClone.Models
{
    public class BandwidthRule : INotifyPropertyChanged
    {
        private int _processId;
        private string _processName = string.Empty;
        private string _processPath = string.Empty;
        private long _downloadLimit;
        private long _uploadLimit;
        private bool _isEnabled;
        private BandwidthPriority _priority;
        private DateTime _createdAt;
        private DateTime _lastModified;

        public int ProcessId
        {
            get => _processId;
            set => SetProperty(ref _processId, value);
        }

        public string ProcessName
        {
            get => _processName;
            set => SetProperty(ref _processName, value);
        }

        public string ProcessPath
        {
            get => _processPath;
            set => SetProperty(ref _processPath, value);
        }

        public long DownloadLimit
        {
            get => _downloadLimit;
            set
            {
                if (SetProperty(ref _downloadLimit, value))
                {
                    LastModified = DateTime.Now;
                }
            }
        }

        public long UploadLimit
        {
            get => _uploadLimit;
            set
            {
                if (SetProperty(ref _uploadLimit, value))
                {
                    LastModified = DateTime.Now;
                }
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (SetProperty(ref _isEnabled, value))
                {
                    LastModified = DateTime.Now;
                }
            }
        }

        public BandwidthPriority Priority
        {
            get => _priority;
            set
            {
                if (SetProperty(ref _priority, value))
                {
                    LastModified = DateTime.Now;
                }
            }
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        public DateTime LastModified
        {
            get => _lastModified;
            set => SetProperty(ref _lastModified, value);
        }

        public string DownloadLimitFormatted => FormatBandwidth(_downloadLimit);
        public string UploadLimitFormatted => FormatBandwidth(_uploadLimit);

        public BandwidthRule()
        {
            _createdAt = DateTime.Now;
            _lastModified = DateTime.Now;
            _priority = BandwidthPriority.Normal;
        }

        public BandwidthRule(int processId, string processName, string processPath)
        {
            ProcessId = processId;
            ProcessName = processName;
            ProcessPath = processPath;
            _createdAt = DateTime.Now;
            _lastModified = DateTime.Now;
            _priority = BandwidthPriority.Normal;
        }

        private string FormatBandwidth(long bytesPerSecond)
        {
            if (bytesPerSecond <= 0)
                return "Unlimited";

            if (bytesPerSecond >= 1024 * 1024 * 1024)
                return $"{bytesPerSecond / (1024.0 * 1024 * 1024):F1} GB/s";
            if (bytesPerSecond >= 1024 * 1024)
                return $"{bytesPerSecond / (1024.0 * 1024):F1} MB/s";
            if (bytesPerSecond >= 1024)
                return $"{bytesPerSecond / 1024.0:F1} KB/s";
            return $"{bytesPerSecond} B/s";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public enum BandwidthPriority
    {
        Low,
        Normal,
        High,
        Critical
    }
}