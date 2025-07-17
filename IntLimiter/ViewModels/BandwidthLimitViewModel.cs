using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using NetLimiterClone.Models;

namespace NetLimiterClone.ViewModels
{
    public class BandwidthLimitViewModel : INotifyPropertyChanged
    {
        private readonly ProcessInfo _processInfo;
        private bool _enableDownloadLimit;
        private bool _enableUploadLimit;
        private double _downloadLimitValue = 1;
        private double _uploadLimitValue = 1;
        private string _downloadLimitUnit = "MB/s";
        private string _uploadLimitUnit = "MB/s";
        private string _priority = "Normal";

        public event EventHandler<bool>? CloseRequested;

        public string ProcessName => _processInfo.ProcessName;
        public string ProcessPath => _processInfo.ProcessPath;
        public ImageSource? ProcessIcon => _processInfo.Icon;

        public bool EnableDownloadLimit
        {
            get => _enableDownloadLimit;
            set
            {
                if (SetProperty(ref _enableDownloadLimit, value))
                {
                    OnPropertyChanged(nameof(DownloadLimitDisplay));
                }
            }
        }

        public bool EnableUploadLimit
        {
            get => _enableUploadLimit;
            set
            {
                if (SetProperty(ref _enableUploadLimit, value))
                {
                    OnPropertyChanged(nameof(UploadLimitDisplay));
                }
            }
        }

        public double DownloadLimitValue
        {
            get => _downloadLimitValue;
            set
            {
                if (SetProperty(ref _downloadLimitValue, value))
                {
                    OnPropertyChanged(nameof(DownloadLimitDisplay));
                }
            }
        }

        public double UploadLimitValue
        {
            get => _uploadLimitValue;
            set
            {
                if (SetProperty(ref _uploadLimitValue, value))
                {
                    OnPropertyChanged(nameof(UploadLimitDisplay));
                }
            }
        }

        public string DownloadLimitUnit
        {
            get => _downloadLimitUnit;
            set
            {
                if (SetProperty(ref _downloadLimitUnit, value))
                {
                    OnPropertyChanged(nameof(DownloadLimitDisplay));
                }
            }
        }

        public string UploadLimitUnit
        {
            get => _uploadLimitUnit;
            set
            {
                if (SetProperty(ref _uploadLimitUnit, value))
                {
                    OnPropertyChanged(nameof(UploadLimitDisplay));
                }
            }
        }

        public string Priority
        {
            get => _priority;
            set => SetProperty(ref _priority, value);
        }

        public string DownloadLimitDisplay
        {
            get
            {
                if (!EnableDownloadLimit)
                    return "Unlimited";
                return $"Limit: {DownloadLimitValue:F1} {DownloadLimitUnit}";
            }
        }

        public string UploadLimitDisplay
        {
            get
            {
                if (!EnableUploadLimit)
                    return "Unlimited";
                return $"Limit: {UploadLimitValue:F1} {UploadLimitUnit}";
            }
        }

        public long DownloadLimitInBytes
        {
            get
            {
                if (!EnableDownloadLimit)
                    return 0;
                var multiplier = DownloadLimitUnit == "MB/s" ? 1024 * 1024 : 1024;
                return (long)(DownloadLimitValue * multiplier);
            }
        }

        public long UploadLimitInBytes
        {
            get
            {
                if (!EnableUploadLimit)
                    return 0;
                var multiplier = UploadLimitUnit == "MB/s" ? 1024 * 1024 : 1024;
                return (long)(UploadLimitValue * multiplier);
            }
        }

        public BandwidthPriority PriorityEnum
        {
            get
            {
                return Priority switch
                {
                    "Low" => BandwidthPriority.Low,
                    "High" => BandwidthPriority.High,
                    "Critical" => BandwidthPriority.Critical,
                    _ => BandwidthPriority.Normal
                };
            }
        }

        public ICommand SetPresetCommand { get; }
        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        public BandwidthLimitViewModel(ProcessInfo processInfo)
        {
            _processInfo = processInfo;

            // Initialize with existing limits if any
            if (processInfo.DownloadLimit > 0)
            {
                EnableDownloadLimit = true;
                var (value, unit) = ConvertBytesToDisplayValue(processInfo.DownloadLimit);
                DownloadLimitValue = value;
                DownloadLimitUnit = unit;
            }

            if (processInfo.UploadLimit > 0)
            {
                EnableUploadLimit = true;
                var (value, unit) = ConvertBytesToDisplayValue(processInfo.UploadLimit);
                UploadLimitValue = value;
                UploadLimitUnit = unit;
            }

            SetPresetCommand = new RelayCommand(SetPreset);
            OkCommand = new RelayCommand(Ok);
            CancelCommand = new RelayCommand(Cancel);
        }

        private (double value, string unit) ConvertBytesToDisplayValue(long bytes)
        {
            if (bytes >= 1024 * 1024)
            {
                return (bytes / (1024.0 * 1024), "MB/s");
            }
            else
            {
                return (bytes / 1024.0, "KB/s");
            }
        }

        private void SetPreset(object? parameter)
        {
            if (parameter is string presetStr && double.TryParse(presetStr, out var preset))
            {
                EnableDownloadLimit = true;
                EnableUploadLimit = true;
                DownloadLimitValue = preset;
                UploadLimitValue = preset;
                DownloadLimitUnit = "MB/s";
                UploadLimitUnit = "MB/s";
            }
        }

        private void Ok(object? parameter)
        {
            CloseRequested?.Invoke(this, true);
        }

        private void Cancel(object? parameter)
        {
            CloseRequested?.Invoke(this, false);
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