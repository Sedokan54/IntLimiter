using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Collections.Generic;

namespace NetLimiterClone.Models
{
    public class ProcessInfo : INotifyPropertyChanged
    {
        private int _processId;
        private string _processName = string.Empty;
        private string _processPath = string.Empty;
        private ImageSource? _icon;
        private bool _iconLoaded = false;
        private bool _detailsLoaded = false;
        private long _downloadSpeed;
        private long _uploadSpeed;
        private long _totalDownload;
        private long _totalUpload;
        private int _connectionCount;
        private bool _isSystemProcess;
        private long _downloadLimit;
        private long _uploadLimit;
        private bool _isLimited;

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

        public ImageSource? Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        public bool IconLoaded
        {
            get => _iconLoaded;
            set => SetProperty(ref _iconLoaded, value);
        }

        public bool DetailsLoaded
        {
            get => _detailsLoaded;
            set => SetProperty(ref _detailsLoaded, value);
        }

        public long DownloadSpeed
        {
            get => _downloadSpeed;
            set => SetProperty(ref _downloadSpeed, value);
        }

        public long UploadSpeed
        {
            get => _uploadSpeed;
            set => SetProperty(ref _uploadSpeed, value);
        }

        public long TotalDownload
        {
            get => _totalDownload;
            set => SetProperty(ref _totalDownload, value);
        }

        public long TotalUpload
        {
            get => _totalUpload;
            set => SetProperty(ref _totalUpload, value);
        }

        public int ConnectionCount
        {
            get => _connectionCount;
            set => SetProperty(ref _connectionCount, value);
        }

        public bool IsSystemProcess
        {
            get => _isSystemProcess;
            set => SetProperty(ref _isSystemProcess, value);
        }

        public long DownloadLimit
        {
            get => _downloadLimit;
            set => SetProperty(ref _downloadLimit, value);
        }

        public long UploadLimit
        {
            get => _uploadLimit;
            set => SetProperty(ref _uploadLimit, value);
        }

        public bool IsLimited
        {
            get => _isLimited;
            set => SetProperty(ref _isLimited, value);
        }

        public string DownloadSpeedFormatted => FormatBytes(_downloadSpeed) + "/s";
        public string UploadSpeedFormatted => FormatBytes(_uploadSpeed) + "/s";
        public string TotalDownloadFormatted => FormatBytes(_totalDownload);
        public string TotalUploadFormatted => FormatBytes(_totalUpload);
        public string DownloadLimitFormatted => _downloadLimit > 0 ? FormatBytes(_downloadLimit) + "/s" : "Unlimited";
        public string UploadLimitFormatted => _uploadLimit > 0 ? FormatBytes(_uploadLimit) + "/s" : "Unlimited";

        private string FormatBytes(long bytes)
        {
            if (bytes >= 1024 * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
            if (bytes >= 1024 * 1024)
                return $"{bytes / (1024.0 * 1024):F1} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F1} KB";
            return $"{bytes} B";
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
}