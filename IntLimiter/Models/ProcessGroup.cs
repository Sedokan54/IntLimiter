using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace NetLimiterClone.Models
{
    public class ProcessGroup : INotifyPropertyChanged
    {
        private string _groupName = string.Empty;
        private bool _isExpanded = true;
        private bool _isSelected = false;
        private ProcessGroupType _groupType = ProcessGroupType.Application;
        private long _downloadSpeed = 0;
        private long _uploadSpeed = 0;
        private long _totalDownload = 0;
        private long _totalUpload = 0;
        private long _downloadLimit = 0;
        private long _uploadLimit = 0;
        private bool _isLimited = false;
        private ImageSource? _groupIcon;

        public string GroupName
        {
            get => _groupName;
            set => SetProperty(ref _groupName, value);
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public ProcessGroupType GroupType
        {
            get => _groupType;
            set => SetProperty(ref _groupType, value);
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

        public ImageSource? GroupIcon
        {
            get => _groupIcon;
            set => SetProperty(ref _groupIcon, value);
        }

        public ObservableCollection<ProcessInfo> Processes { get; } = new();

        // Calculated properties
        public int ProcessCount => Processes.Count;
        public string DownloadSpeedFormatted => FormatSpeed(DownloadSpeed);
        public string UploadSpeedFormatted => FormatSpeed(UploadSpeed);
        public string TotalDownloadFormatted => FormatBytes(TotalDownload);
        public string TotalUploadFormatted => FormatBytes(TotalUpload);
        public string DownloadLimitFormatted => DownloadLimit > 0 ? FormatSpeed(DownloadLimit) : "No limit";
        public string UploadLimitFormatted => UploadLimit > 0 ? FormatSpeed(UploadLimit) : "No limit";

        public void UpdateFromProcesses()
        {
            DownloadSpeed = Processes.Sum(p => p.DownloadSpeed);
            UploadSpeed = Processes.Sum(p => p.UploadSpeed);
            TotalDownload = Processes.Sum(p => p.TotalDownload);
            TotalUpload = Processes.Sum(p => p.TotalUpload);
            
            // Update limited status
            IsLimited = Processes.Any(p => p.IsLimited) || DownloadLimit > 0 || UploadLimit > 0;
            
            OnPropertyChanged(nameof(ProcessCount));
        }

        public void AddProcess(ProcessInfo process)
        {
            if (!Processes.Contains(process))
            {
                Processes.Add(process);
                UpdateFromProcesses();
            }
        }

        public void RemoveProcess(ProcessInfo process)
        {
            if (Processes.Remove(process))
            {
                UpdateFromProcesses();
            }
        }

        public void ClearProcesses()
        {
            Processes.Clear();
            UpdateFromProcesses();
        }

        public void ApplyLimitToAllProcesses(long downloadLimit, long uploadLimit)
        {
            DownloadLimit = downloadLimit;
            UploadLimit = uploadLimit;
            
            foreach (var process in Processes)
            {
                process.DownloadLimit = downloadLimit;
                process.UploadLimit = uploadLimit;
                process.IsLimited = downloadLimit > 0 || uploadLimit > 0;
            }
            
            UpdateFromProcesses();
        }

        public void RemoveLimitFromAllProcesses()
        {
            DownloadLimit = 0;
            UploadLimit = 0;
            
            foreach (var process in Processes)
            {
                process.DownloadLimit = 0;
                process.UploadLimit = 0;
                process.IsLimited = false;
            }
            
            UpdateFromProcesses();
        }

        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            double number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:N2} {suffixes[counter]}";
        }

        private static string FormatSpeed(long bps)
        {
            return FormatBytes(bps) + "/s";
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

    public enum ProcessGroupType
    {
        Application,    // Group by application name (e.g., all Chrome processes)
        Service,        // Group by service type
        User,           // Group by user
        Custom          // Custom user-defined groups
    }
}