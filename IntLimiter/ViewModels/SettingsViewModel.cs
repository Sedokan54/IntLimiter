using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using NetLimiterClone.Models;
using NetLimiterClone.Services;

namespace NetLimiterClone.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly SettingsService _settingsService;
        private readonly AppSettings _originalSettings;
        private AppSettings _currentSettings;

        public event EventHandler<bool>? CloseRequested;

        // Binding properties
        public int UpdateInterval
        {
            get => _currentSettings.UpdateInterval;
            set
            {
                if (_currentSettings.UpdateInterval != value)
                {
                    _currentSettings.UpdateInterval = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool StartWithWindows
        {
            get => _currentSettings.StartWithWindows;
            set
            {
                if (_currentSettings.StartWithWindows != value)
                {
                    _currentSettings.StartWithWindows = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool StartMinimized
        {
            get => _currentSettings.StartMinimized;
            set
            {
                if (_currentSettings.StartMinimized != value)
                {
                    _currentSettings.StartMinimized = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool MinimizeToTray
        {
            get => _currentSettings.MinimizeToTray;
            set
            {
                if (_currentSettings.MinimizeToTray != value)
                {
                    _currentSettings.MinimizeToTray = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowNotifications
        {
            get => _currentSettings.ShowNotifications;
            set
            {
                if (_currentSettings.ShowNotifications != value)
                {
                    _currentSettings.ShowNotifications = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ThemeMode
        {
            get => _currentSettings.ThemeMode.ToString();
            set
            {
                if (Enum.TryParse<ThemeMode>(value, out var themeMode) && _currentSettings.ThemeMode != themeMode)
                {
                    _currentSettings.ThemeMode = themeMode;
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedNetworkAdapter
        {
            get => _currentSettings.SelectedNetworkAdapter;
            set
            {
                if (_currentSettings.SelectedNetworkAdapter != value)
                {
                    _currentSettings.SelectedNetworkAdapter = value ?? "All Adapters";
                    OnPropertyChanged();
                }
            }
        }

        public bool EnableEtwMonitoring
        {
            get => _currentSettings.EnableEtwMonitoring;
            set
            {
                if (_currentSettings.EnableEtwMonitoring != value)
                {
                    _currentSettings.EnableEtwMonitoring = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowSystemProcesses
        {
            get => _currentSettings.ShowSystemProcesses;
            set
            {
                if (_currentSettings.ShowSystemProcesses != value)
                {
                    _currentSettings.ShowSystemProcesses = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowInactiveProcesses
        {
            get => _currentSettings.ShowInactiveProcesses;
            set
            {
                if (_currentSettings.ShowInactiveProcesses != value)
                {
                    _currentSettings.ShowInactiveProcesses = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ChartUpdateInterval
        {
            get => _currentSettings.ChartUpdateInterval;
            set
            {
                if (_currentSettings.ChartUpdateInterval != value)
                {
                    _currentSettings.ChartUpdateInterval = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ChartHistorySeconds
        {
            get => _currentSettings.ChartHistorySeconds;
            set
            {
                if (_currentSettings.ChartHistorySeconds != value)
                {
                    _currentSettings.ChartHistorySeconds = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DatabasePath
        {
            get => _currentSettings.DatabasePath;
            set
            {
                if (_currentSettings.DatabasePath != value)
                {
                    _currentSettings.DatabasePath = value ?? "netlimiter.db";
                    OnPropertyChanged();
                }
            }
        }

        public bool EnableDatabaseLogging
        {
            get => _currentSettings.EnableDatabaseLogging;
            set
            {
                if (_currentSettings.EnableDatabaseLogging != value)
                {
                    _currentSettings.EnableDatabaseLogging = value;
                    OnPropertyChanged();
                }
            }
        }

        public int MaxLogDays
        {
            get => _currentSettings.MaxLogDays;
            set
            {
                if (_currentSettings.MaxLogDays != value)
                {
                    _currentSettings.MaxLogDays = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> NetworkAdapters { get; }

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ResetCommand { get; }

        public SettingsViewModel(SettingsService settingsService)
        {
            _settingsService = settingsService;
            _originalSettings = settingsService.CurrentSettings;
            _currentSettings = _originalSettings.Clone();

            NetworkAdapters = new ObservableCollection<string>();
            LoadNetworkAdapters();

            OkCommand = new RelayCommand(Ok);
            CancelCommand = new RelayCommand(Cancel);
            ResetCommand = new RelayCommand(Reset);
        }

        private void LoadNetworkAdapters()
        {
            try
            {
                NetworkAdapters.Clear();
                NetworkAdapters.Add("All Adapters");

                using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_NetworkAdapter WHERE NetEnabled = true");
                foreach (ManagementObject adapter in searcher.Get())
                {
                    var name = adapter["Name"]?.ToString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        NetworkAdapters.Add(name);
                    }
                }
            }
            catch (Exception)
            {
                // If WMI fails, just use the default
                if (NetworkAdapters.Count == 0)
                {
                    NetworkAdapters.Add("All Adapters");
                }
            }

            // Ensure the selected adapter is in the list
            if (!NetworkAdapters.Contains(_currentSettings.SelectedNetworkAdapter))
            {
                SelectedNetworkAdapter = "All Adapters";
            }
        }

        private void Ok(object? parameter)
        {
            try
            {
                _settingsService.SaveSettings(_currentSettings);
                CloseRequested?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void Cancel(object? parameter)
        {
            CloseRequested?.Invoke(this, false);
        }

        private void Reset(object? parameter)
        {
            var result = System.Windows.MessageBox.Show(
                "Are you sure you want to reset all settings to default values?",
                "Reset Settings",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                var defaultSettings = new AppSettings();
                UpdateFromSettings(defaultSettings);
            }
        }

        private void UpdateFromSettings(AppSettings settings)
        {
            _currentSettings = settings.Clone();
            
            // Notify all properties changed
            OnPropertyChanged(nameof(UpdateInterval));
            OnPropertyChanged(nameof(StartWithWindows));
            OnPropertyChanged(nameof(StartMinimized));
            OnPropertyChanged(nameof(MinimizeToTray));
            OnPropertyChanged(nameof(ShowNotifications));
            OnPropertyChanged(nameof(ThemeMode));
            OnPropertyChanged(nameof(SelectedNetworkAdapter));
            OnPropertyChanged(nameof(EnableEtwMonitoring));
            OnPropertyChanged(nameof(ShowSystemProcesses));
            OnPropertyChanged(nameof(ShowInactiveProcesses));
            OnPropertyChanged(nameof(ChartUpdateInterval));
            OnPropertyChanged(nameof(ChartHistorySeconds));
            OnPropertyChanged(nameof(DatabasePath));
            OnPropertyChanged(nameof(EnableDatabaseLogging));
            OnPropertyChanged(nameof(MaxLogDays));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}