using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using NetLimiterClone.Models;
using NetLimiterClone.Services;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace NetLimiterClone.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly NetworkMonitorService _networkMonitorService;
        private readonly ETWNetworkMonitorService _etwNetworkMonitorService;
        private readonly BandwidthLimiterService _bandwidthLimiterService;
        private readonly SystemTrayService _systemTrayService;
        private readonly SettingsService _settingsService;
        private readonly DatabaseService _databaseService;
        private readonly ProcessGroupingService _processGroupingService;
        private readonly ProfileService _profileService;
        private readonly NotificationService _notificationService;
        private readonly ThemeService _themeService;
        private readonly DispatcherTimer _uiUpdateTimer;
        private readonly ObservableCollection<ProcessInfo> _allProcesses;
        private ObservableCollection<ProcessInfo> _processList;
        private ObservableCollection<ProcessGroup> _processGroups;
        private ProcessInfo? _selectedProcess;
        private string _searchText = string.Empty;
        private bool _showSystemProcesses = false;
        private bool _showInactiveProcesses = true;
        private bool _groupingEnabled = false;
        private ProcessGroupType _currentGroupType = ProcessGroupType.Application;
        private string _statusText = "Ready";
        private string _activeProfileName = "None";
        private DateTime _lastUpdateTime;
        private PlotModel _bandwidthChart;

        public ObservableCollection<ProcessInfo> ProcessList
        {
            get => _processList;
            set => SetProperty(ref _processList, value);
        }

        public ObservableCollection<ProcessGroup> ProcessGroups
        {
            get => _processGroups;
            set => SetProperty(ref _processGroups, value);
        }

        public bool GroupingEnabled
        {
            get => _groupingEnabled;
            set
            {
                if (SetProperty(ref _groupingEnabled, value))
                {
                    FilterProcesses();
                }
            }
        }

        public ProcessGroupType CurrentGroupType
        {
            get => _currentGroupType;
            set
            {
                if (SetProperty(ref _currentGroupType, value))
                {
                    _processGroupingService.CurrentGroupType = value;
                    if (GroupingEnabled)
                    {
                        FilterProcesses();
                    }
                }
            }
        }

        public ProcessInfo? SelectedProcess
        {
            get => _selectedProcess;
            set => SetProperty(ref _selectedProcess, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterProcesses();
                }
            }
        }

        public bool ShowSystemProcesses
        {
            get => _showSystemProcesses;
            set
            {
                if (SetProperty(ref _showSystemProcesses, value))
                {
                    FilterProcesses();
                }
            }
        }

        public bool ShowInactiveProcesses
        {
            get => _showInactiveProcesses;
            set
            {
                if (SetProperty(ref _showInactiveProcesses, value))
                {
                    FilterProcesses();
                }
            }
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public string ActiveProfileName
        {
            get => _activeProfileName;
            set => SetProperty(ref _activeProfileName, value);
        }

        public DateTime LastUpdateTime
        {
            get => _lastUpdateTime;
            set => SetProperty(ref _lastUpdateTime, value);
        }

        public PlotModel BandwidthChart
        {
            get => _bandwidthChart;
            set => SetProperty(ref _bandwidthChart, value);
        }

        public string TotalDownloadSpeed => FormatBytes(ProcessList.Sum(p => p.DownloadSpeed)) + "/s";
        public string TotalUploadSpeed => FormatBytes(ProcessList.Sum(p => p.UploadSpeed)) + "/s";
        public int ActiveProcessCount => ProcessList.Count(p => p.DownloadSpeed > 0 || p.UploadSpeed > 0);
        public int LimitedProcessCount => ProcessList.Count(p => p.IsLimited);

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand SetLimitCommand { get; }
        public ICommand RemoveLimitCommand { get; }
        public ICommand BlockProcessCommand { get; }
        public ICommand UnblockProcessCommand { get; }
        public ICommand SetDownloadLimitCommand { get; }
        public ICommand SetUploadLimitCommand { get; }
        public ICommand ViewDetailsCommand { get; }
        public ICommand OpenFileLocationCommand { get; }
        public ICommand NewProfileCommand { get; }
        public ICommand LoadProfileCommand { get; }
        public ICommand SaveProfileCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand PreferencesCommand { get; }
        public ICommand StatisticsCommand { get; }
        public ICommand AboutCommand { get; }
        public ICommand ProfilesCommand { get; }
        public ICommand ToggleThemeCommand { get; }

        public MainViewModel()
        {
            _allProcesses = new ObservableCollection<ProcessInfo>();
            _processList = new ObservableCollection<ProcessInfo>();
            _processGroups = new ObservableCollection<ProcessGroup>();
            _networkMonitorService = new NetworkMonitorService();
            _bandwidthLimiterService = new BandwidthLimiterService();
            
            // Try to use ETW monitoring if running as admin
            try
            {
                _etwNetworkMonitorService = new ETWNetworkMonitorService();
                StatusText = "ETW monitoring enabled (Administrator mode)";
            }
            catch (UnauthorizedAccessException)
            {
                StatusText = "Using basic monitoring (Run as Administrator for ETW monitoring)";
            }
            _systemTrayService = new SystemTrayService();
            _settingsService = new SettingsService();
            _databaseService = new DatabaseService();
            _processGroupingService = new ProcessGroupingService();
            _profileService = new ProfileService(_bandwidthLimiterService);
            _notificationService = new NotificationService();
            _themeService = new ThemeService();
            _bandwidthChart = CreateBandwidthChart();

            // Initialize commands
            RefreshCommand = new RelayCommand(Refresh);
            SetLimitCommand = new RelayCommand(SetLimit, CanExecuteProcessCommand);
            RemoveLimitCommand = new RelayCommand(RemoveLimit, CanExecuteProcessCommand);
            BlockProcessCommand = new RelayCommand(BlockProcess, CanExecuteProcessCommand);
            UnblockProcessCommand = new RelayCommand(UnblockProcess, CanExecuteProcessCommand);
            SetDownloadLimitCommand = new RelayCommand(SetDownloadLimit, CanExecuteProcessCommand);
            SetUploadLimitCommand = new RelayCommand(SetUploadLimit, CanExecuteProcessCommand);
            ViewDetailsCommand = new RelayCommand(ViewDetails, CanExecuteProcessCommand);
            OpenFileLocationCommand = new RelayCommand(OpenFileLocation, CanExecuteProcessCommand);
            NewProfileCommand = new RelayCommand(NewProfile);
            LoadProfileCommand = new RelayCommand(LoadProfile);
            SaveProfileCommand = new RelayCommand(SaveProfile);
            ExitCommand = new RelayCommand(Exit);
            PreferencesCommand = new RelayCommand(Preferences);
            StatisticsCommand = new RelayCommand(Statistics);
            AboutCommand = new RelayCommand(About);
            ProfilesCommand = new RelayCommand(Profiles);
            ToggleThemeCommand = new RelayCommand(ToggleTheme);

            // Setup event handlers
            if (_etwNetworkMonitorService != null)
            {
                _etwNetworkMonitorService.ProcessesUpdated += OnProcessesUpdated;
                _etwNetworkMonitorService.NetworkStatsUpdated += OnNetworkStatsUpdated;
                _etwNetworkMonitorService.StartMonitoring();
            }
            else
            {
                _networkMonitorService.ProcessesUpdated += OnProcessesUpdated;
                _networkMonitorService.NetworkStatsUpdated += OnNetworkStatsUpdated;
            }
            _systemTrayService.ShowMainWindowRequested += OnShowMainWindowRequested;
            _systemTrayService.HideMainWindowRequested += OnHideMainWindowRequested;
            _systemTrayService.ExitApplicationRequested += OnExitApplicationRequested;
            
            // Setup profile service events
            _profileService.ProfileChanged += OnProfileChanged;
            
            // Setup bandwidth limiter events for notifications
            _bandwidthLimiterService.ProcessLimitApplied += OnProcessLimitApplied;
            _bandwidthLimiterService.ProcessLimitRemoved += OnProcessLimitRemoved;
            
            // Load saved bandwidth rules
            LoadBandwidthRules();
            
            // Update active profile display
            UpdateActiveProfile();

            // Setup UI update timer
            _uiUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(_settingsService.CurrentSettings.UpdateInterval)
            };
            _uiUpdateTimer.Tick += OnUiUpdateTimer;
            _uiUpdateTimer.Start();

            // Apply initial settings
            ApplySettings(_settingsService.CurrentSettings);
            _settingsService.SettingsChanged += OnSettingsChanged;

            StatusText = "Monitoring network activity...";
        }

        private void OnProcessesUpdated(object? sender, ProcessInfo[] processes)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _allProcesses.Clear();
                foreach (var process in processes)
                {
                    _allProcesses.Add(process);
                }
                
                FilterProcesses();
                LastUpdateTime = DateTime.Now;
            });
        }

        private void OnNetworkStatsUpdated(object? sender, NetworkStats stats)
        {
            // Update chart with new data
            App.Current.Dispatcher.Invoke(() =>
            {
                UpdateBandwidthChart(stats);
            });
        }

        private void OnUiUpdateTimer(object? sender, EventArgs e)
        {
            // Update calculated properties
            OnPropertyChanged(nameof(TotalDownloadSpeed));
            OnPropertyChanged(nameof(TotalUploadSpeed));
            OnPropertyChanged(nameof(ActiveProcessCount));
            OnPropertyChanged(nameof(LimitedProcessCount));

            // Update system tray tooltip
            _systemTrayService.UpdateTooltip(
                TotalDownloadSpeed,
                TotalUploadSpeed,
                ActiveProcessCount,
                LimitedProcessCount);

            // Update tray icon based on activity
            var hasActivity = ActiveProcessCount > 0;
            _systemTrayService.UpdateIconForActivity(hasActivity);

            // Clean up dead processes
            if (_etwNetworkMonitorService != null)
            {
                _etwNetworkMonitorService.CleanupDeadProcesses();
            }
            else
            {
                _networkMonitorService.CleanupDeadProcesses();
            }
            
            // Save network stats to database
            SaveNetworkStatsToDatabase();
        }

        private void FilterProcesses()
        {
            var filteredProcesses = _allProcesses.AsEnumerable();

            // Text search filter
            if (!string.IsNullOrEmpty(SearchText))
            {
                filteredProcesses = filteredProcesses.Where(p => 
                    p.ProcessName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    p.ProcessPath.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    p.ProcessId.ToString().Contains(SearchText));
            }

            // System processes filter
            if (!ShowSystemProcesses)
            {
                filteredProcesses = filteredProcesses.Where(p => !p.IsSystemProcess);
            }

            // Inactive processes filter
            if (!ShowInactiveProcesses)
            {
                filteredProcesses = filteredProcesses.Where(p => p.DownloadSpeed > 0 || p.UploadSpeed > 0);
            }

            if (GroupingEnabled)
            {
                // Group processes
                var groups = _processGroupingService.GroupProcesses(filteredProcesses);
                ProcessGroups.Clear();
                foreach (var group in groups)
                {
                    ProcessGroups.Add(group);
                }
                
                // Clear individual process list when grouping is enabled
                ProcessList.Clear();
            }
            else
            {
                // Show individual processes
                var sortedProcesses = filteredProcesses
                    .OrderByDescending(p => p.DownloadSpeed + p.UploadSpeed)
                    .ThenByDescending(p => p.TotalDownload + p.TotalUpload)
                    .ThenBy(p => p.ProcessName);

                ProcessList.Clear();
                foreach (var process in sortedProcesses)
                {
                    ProcessList.Add(process);
                }
                
                // Clear groups when not grouping
                ProcessGroups.Clear();
            }
        }

        private PlotModel CreateBandwidthChart()
        {
            var plot = new PlotModel
            {
                Title = "Network Bandwidth",
                Background = OxyColors.Transparent,
                TextColor = OxyColors.White,
                PlotAreaBorderColor = OxyColors.Gray
            };

            var timeAxis = new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Time",
                StringFormat = "HH:mm:ss",
                TextColor = OxyColors.White,
                AxislineColor = OxyColors.Gray,
                TicklineColor = OxyColors.Gray
            };

            var valueAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Bandwidth (KB/s)",
                TextColor = OxyColors.White,
                AxislineColor = OxyColors.Gray,
                TicklineColor = OxyColors.Gray
            };

            plot.Axes.Add(timeAxis);
            plot.Axes.Add(valueAxis);

            var downloadSeries = new LineSeries
            {
                Title = "Download",
                Color = OxyColors.Red,
                StrokeThickness = 2
            };

            var uploadSeries = new LineSeries
            {
                Title = "Upload",
                Color = OxyColors.Green,
                StrokeThickness = 2
            };

            plot.Series.Add(downloadSeries);
            plot.Series.Add(uploadSeries);

            return plot;
        }

        private void UpdateBandwidthChart(NetworkStats stats)
        {
            if (BandwidthChart.Series.Count >= 2)
            {
                var downloadSeries = (LineSeries)BandwidthChart.Series[0];
                var uploadSeries = (LineSeries)BandwidthChart.Series[1];

                var time = DateTimeAxis.ToDouble(stats.Timestamp);
                var totalDownload = ProcessList.Sum(p => p.DownloadSpeed) / 1024.0; // Convert to KB/s
                var totalUpload = ProcessList.Sum(p => p.UploadSpeed) / 1024.0; // Convert to KB/s

                downloadSeries.Points.Add(new DataPoint(time, totalDownload));
                uploadSeries.Points.Add(new DataPoint(time, totalUpload));

                // Keep only last 60 seconds of data
                var cutoffTime = DateTimeAxis.ToDouble(DateTime.Now.AddSeconds(-60));
                while (downloadSeries.Points.Count > 0 && downloadSeries.Points[0].X < cutoffTime)
                {
                    downloadSeries.Points.RemoveAt(0);
                }
                while (uploadSeries.Points.Count > 0 && uploadSeries.Points[0].X < cutoffTime)
                {
                    uploadSeries.Points.RemoveAt(0);
                }

                BandwidthChart.InvalidatePlot(true);
            }
        }

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

        private bool CanExecuteProcessCommand(object? parameter)
        {
            return SelectedProcess != null;
        }

        private void Refresh(object? parameter)
        {
            StatusText = "Refreshing...";
            // The NetworkMonitorService continuously updates, so we just need to clean up dead processes
            _networkMonitorService.CleanupDeadProcesses();
            StatusText = "Monitoring network activity...";
        }

        private void SetLimit(object? parameter)
        {
            if (SelectedProcess == null) return;
            
            var viewModel = new BandwidthLimitViewModel(SelectedProcess);
            var dialog = new Views.BandwidthLimitDialog(viewModel);
            
            if (dialog.ShowDialog() == true)
            {
                var result = _bandwidthLimiterService.SetProcessLimit(
                    SelectedProcess.ProcessId,
                    viewModel.DownloadLimitInBytes,
                    viewModel.UploadLimitInBytes,
                    viewModel.PriorityEnum);
                
                if (result)
                {
                    SelectedProcess.DownloadLimit = viewModel.DownloadLimitInBytes;
                    SelectedProcess.UploadLimit = viewModel.UploadLimitInBytes;
                    SelectedProcess.IsLimited = viewModel.DownloadLimitInBytes > 0 || viewModel.UploadLimitInBytes > 0;
                    StatusText = $"Applied bandwidth limit to {SelectedProcess.ProcessName}";
                }
                else
                {
                    StatusText = $"Failed to apply bandwidth limit to {SelectedProcess.ProcessName}";
                }
            }
        }

        private void RemoveLimit(object? parameter)
        {
            if (SelectedProcess == null) return;
            
            var result = _bandwidthLimiterService.RemoveProcessLimit(SelectedProcess.ProcessId);
            if (result)
            {
                SelectedProcess.DownloadLimit = 0;
                SelectedProcess.UploadLimit = 0;
                SelectedProcess.IsLimited = false;
                StatusText = $"Removed bandwidth limit for {SelectedProcess.ProcessName}";
            }
            else
            {
                StatusText = $"Failed to remove bandwidth limit for {SelectedProcess.ProcessName}";
            }
        }

        private void BlockProcess(object? parameter)
        {
            if (SelectedProcess == null) return;
            
            var result = _bandwidthLimiterService.BlockProcess(SelectedProcess.ProcessId);
            if (result)
            {
                SelectedProcess.IsLimited = true;
                StatusText = $"Blocked network access for {SelectedProcess.ProcessName}";
            }
            else
            {
                StatusText = $"Failed to block network access for {SelectedProcess.ProcessName}";
            }
        }

        private void UnblockProcess(object? parameter)
        {
            if (SelectedProcess == null) return;
            
            var result = _bandwidthLimiterService.UnblockProcess(SelectedProcess.ProcessId);
            if (result)
            {
                SelectedProcess.IsLimited = false;
                StatusText = $"Unblocked network access for {SelectedProcess.ProcessName}";
            }
            else
            {
                StatusText = $"Failed to unblock network access for {SelectedProcess.ProcessName}";
            }
        }

        private void SetDownloadLimit(object? parameter)
        {
            if (SelectedProcess == null) return;
            // TODO: Implement download limit dialog
            StatusText = $"Setting download limit for {SelectedProcess.ProcessName}...";
        }

        private void SetUploadLimit(object? parameter)
        {
            if (SelectedProcess == null) return;
            // TODO: Implement upload limit dialog
            StatusText = $"Setting upload limit for {SelectedProcess.ProcessName}...";
        }

        private void ViewDetails(object? parameter)
        {
            if (SelectedProcess == null) return;
            // TODO: Implement process details window
            StatusText = $"Viewing details for {SelectedProcess.ProcessName}...";
        }

        private void OpenFileLocation(object? parameter)
        {
            if (SelectedProcess == null) return;
            // TODO: Implement file location opening
            StatusText = $"Opening file location for {SelectedProcess.ProcessName}...";
        }

        private void NewProfile(object? parameter)
        {
            // TODO: Implement new profile functionality
            StatusText = "Creating new profile...";
        }

        private void LoadProfile(object? parameter)
        {
            // TODO: Implement load profile functionality
            StatusText = "Loading profile...";
        }

        private void SaveProfile(object? parameter)
        {
            // TODO: Implement save profile functionality
            StatusText = "Saving profile...";
        }

        private void Exit(object? parameter)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void Preferences(object? parameter)
        {
            var viewModel = new SettingsViewModel(_settingsService);
            var dialog = new Views.SettingsWindow(viewModel);
            
            if (dialog.ShowDialog() == true)
            {
                StatusText = "Settings saved successfully";
            }
        }

        private void Statistics(object? parameter)
        {
            try
            {
                var viewModel = new StatisticsViewModel(_databaseService);
                var window = new Views.StatisticsWindow(viewModel);
                window.Show();
                StatusText = "Statistics window opened";
            }
            catch (Exception ex)
            {
                StatusText = $"Error opening statistics: {ex.Message}";
            }
        }

        private void About(object? parameter)
        {
            // TODO: Implement about dialog
            StatusText = "About NetLimiter Clone";
        }

        private void Profiles(object? parameter)
        {
            try
            {
                var viewModel = new ProfilesViewModel(_profileService);
                var window = new Views.ProfilesWindow(viewModel);
                window.Show();
                StatusText = "Profiles window opened";
            }
            catch (Exception ex)
            {
                StatusText = $"Error opening profiles: {ex.Message}";
            }
        }

        private void OnProfileChanged(object? sender, ProfileChangedEventArgs e)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                UpdateActiveProfile();
                if (e.NewProfile != null)
                {
                    StatusText = $"Profile '{e.NewProfile.Name}' activated";
                    _notificationService.ShowProfileActivatedNotification(e.NewProfile.Name);
                }
                else
                {
                    StatusText = "Profile deactivated";
                }
            });
        }

        private void UpdateActiveProfile()
        {
            ActiveProfileName = _profileService.ActiveProfile?.Name ?? "None";
        }

        private void ToggleTheme(object? parameter)
        {
            try
            {
                _themeService.ToggleTheme();
                var themeName = _themeService.GetThemeDisplayName(_themeService.CurrentTheme);
                StatusText = $"Switched to {themeName} theme";
                
                if (_settingsService.CurrentSettings.ShowNotifications)
                {
                    _notificationService.ShowNotification(
                        "Theme Changed",
                        $"Application theme changed to {themeName}",
                        NotificationType.Info);
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Error changing theme: {ex.Message}";
            }
        }

        private void OnProcessLimitApplied(object? sender, ProcessLimitEventArgs e)
        {
            if (_settingsService.CurrentSettings.ShowNotifications)
            {
                _notificationService.ShowBandwidthLimitNotification(
                    e.Rule.ProcessName, 
                    e.Rule.DownloadLimit, 
                    e.Rule.UploadLimit);
            }
        }

        private void OnProcessLimitRemoved(object? sender, ProcessLimitEventArgs e)
        {
            if (_settingsService.CurrentSettings.ShowNotifications)
            {
                _notificationService.ShowNotification(
                    "Bandwidth Limit Removed",
                    $"Bandwidth limit removed from {e.Rule.ProcessName}",
                    NotificationType.Info);
            }
        }

        private void OnShowMainWindowRequested(object? sender, EventArgs e)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = App.Current.MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.Show();
                    mainWindow.WindowState = WindowState.Normal;
                    mainWindow.Activate();
                }
            });
        }

        private void OnHideMainWindowRequested(object? sender, EventArgs e)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = App.Current.MainWindow;
                mainWindow?.Hide();
            });
        }

        private void OnExitApplicationRequested(object? sender, EventArgs e)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                App.Current.Shutdown();
            });
        }

        public void MinimizeToTray()
        {
            OnHideMainWindowRequested(this, EventArgs.Empty);
            if (_settingsService.CurrentSettings.ShowNotifications)
            {
                _notificationService.ShowNotification(
                    "NetLimiter Clone", 
                    "Application minimized to system tray",
                    NotificationType.Info);
            }
        }

        private void OnSettingsChanged(object? sender, AppSettings newSettings)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                ApplySettings(newSettings);
            });
        }

        private void ApplySettings(AppSettings settings)
        {
            // Update UI update timer interval
            _uiUpdateTimer.Interval = TimeSpan.FromMilliseconds(settings.UpdateInterval);

            // Update filter settings
            ShowSystemProcesses = settings.ShowSystemProcesses;
            ShowInactiveProcesses = settings.ShowInactiveProcesses;

            // Update notification settings
            _notificationService.NotificationsEnabled = settings.ShowNotifications;

            // Apply other settings as needed
            FilterProcesses();
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

        private async void LoadBandwidthRules()
        {
            try
            {
                var rules = await _databaseService.LoadBandwidthRulesAsync();
                foreach (var rule in rules)
                {
                    _bandwidthLimiterService.SetProcessLimit(rule.ProcessId, rule.DownloadLimit, rule.UploadLimit, rule.Priority);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading bandwidth rules: {ex.Message}");
            }
        }

        private async void SaveNetworkStatsToDatabase()
        {
            try
            {
                foreach (var process in _allProcesses)
                {
                    if (process.DownloadSpeed > 0 || process.UploadSpeed > 0)
                    {
                        var stats = new NetworkStats
                        {
                            ProcessId = process.ProcessId,
                            ProcessName = process.ProcessName,
                            BytesSent = process.TotalUpload,
                            BytesReceived = process.TotalDownload,
                            Timestamp = DateTime.Now
                        };
                        await _databaseService.SaveNetworkStatsAsync(stats);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving network stats: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _uiUpdateTimer?.Stop();
            _networkMonitorService?.Dispose();
            _etwNetworkMonitorService?.Dispose();
            _bandwidthLimiterService?.Dispose();
            _systemTrayService?.Dispose();
            _databaseService?.Dispose();
            _notificationService?.Dispose();
        }
    }
}