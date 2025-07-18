using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NetLimiterClone.Models
{
    public class AppSettings : INotifyPropertyChanged
    {
        private int _updateInterval = 1000;
        private bool _startWithWindows = false;
        private bool _startMinimized = false;
        private bool _minimizeToTray = true;
        private bool _showNotifications = true;
        private ThemeMode _themeMode = ThemeMode.Dark;
        private string _selectedNetworkAdapter = "All Adapters";
        private bool _enableEtwMonitoring = false;
        private bool _showSystemProcesses = false;
        private bool _showInactiveProcesses = true;
        private int _chartUpdateInterval = 1000;
        private int _chartHistorySeconds = 60;
        private string _databasePath = "netlimiter.db";
        private bool _enableDatabaseLogging = true;
        private int _maxLogDays = 30;

        public int UpdateInterval
        {
            get => _updateInterval;
            set => SetProperty(ref _updateInterval, Math.Max(100, Math.Min(10000, value)));
        }

        public bool StartWithWindows
        {
            get => _startWithWindows;
            set => SetProperty(ref _startWithWindows, value);
        }

        public bool StartMinimized
        {
            get => _startMinimized;
            set => SetProperty(ref _startMinimized, value);
        }

        public bool MinimizeToTray
        {
            get => _minimizeToTray;
            set => SetProperty(ref _minimizeToTray, value);
        }

        public bool ShowNotifications
        {
            get => _showNotifications;
            set => SetProperty(ref _showNotifications, value);
        }

        public ThemeMode ThemeMode
        {
            get => _themeMode;
            set => SetProperty(ref _themeMode, value);
        }

        public string SelectedNetworkAdapter
        {
            get => _selectedNetworkAdapter;
            set => SetProperty(ref _selectedNetworkAdapter, value ?? "All Adapters");
        }

        public bool EnableEtwMonitoring
        {
            get => _enableEtwMonitoring;
            set => SetProperty(ref _enableEtwMonitoring, value);
        }

        public bool ShowSystemProcesses
        {
            get => _showSystemProcesses;
            set => SetProperty(ref _showSystemProcesses, value);
        }

        public bool ShowInactiveProcesses
        {
            get => _showInactiveProcesses;
            set => SetProperty(ref _showInactiveProcesses, value);
        }

        public int ChartUpdateInterval
        {
            get => _chartUpdateInterval;
            set => SetProperty(ref _chartUpdateInterval, Math.Max(100, Math.Min(5000, value)));
        }

        public int ChartHistorySeconds
        {
            get => _chartHistorySeconds;
            set => SetProperty(ref _chartHistorySeconds, Math.Max(30, Math.Min(300, value)));
        }

        public string DatabasePath
        {
            get => _databasePath;
            set => SetProperty(ref _databasePath, value ?? "netlimiter.db");
        }

        public bool EnableDatabaseLogging
        {
            get => _enableDatabaseLogging;
            set => SetProperty(ref _enableDatabaseLogging, value);
        }

        public int MaxLogDays
        {
            get => _maxLogDays;
            set => SetProperty(ref _maxLogDays, Math.Max(1, Math.Min(365, value)));
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

        public AppSettings Clone()
        {
            return new AppSettings
            {
                UpdateInterval = UpdateInterval,
                StartWithWindows = StartWithWindows,
                StartMinimized = StartMinimized,
                MinimizeToTray = MinimizeToTray,
                ShowNotifications = ShowNotifications,
                ThemeMode = ThemeMode,
                SelectedNetworkAdapter = SelectedNetworkAdapter,
                EnableEtwMonitoring = EnableEtwMonitoring,
                ShowSystemProcesses = ShowSystemProcesses,
                ShowInactiveProcesses = ShowInactiveProcesses,
                ChartUpdateInterval = ChartUpdateInterval,
                ChartHistorySeconds = ChartHistorySeconds,
                DatabasePath = DatabasePath,
                EnableDatabaseLogging = EnableDatabaseLogging,
                MaxLogDays = MaxLogDays
            };
        }
    }

    public enum ThemeMode
    {
        Light,
        Dark,
        Auto
    }
}