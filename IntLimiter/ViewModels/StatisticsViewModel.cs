using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using NetLimiterClone.Models;
using NetLimiterClone.Services;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace NetLimiterClone.ViewModels
{
    public class StatisticsViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private DateTime _startDate = DateTime.Today.AddDays(-7);
        private DateTime _endDate = DateTime.Today.AddDays(1);
        private string _timelineGranularity = "Hourly";
        private bool _showUploadInTimeline = true;

        public ObservableCollection<ProcessStatistics> ProcessStatistics { get; }
        public ObservableCollection<ProcessStatistics> FilteredProcessStatistics { get; }

        // Summary Properties
        public string TotalDownloaded => FormatBytes(_totalDownloaded);
        public string TotalUploaded => FormatBytes(_totalUploaded);
        public string ActiveProcesses => _activeProcesses.ToString();
        public string PeakSpeed => FormatSpeed(_peakSpeed);
        public string StatusText => _statusText;
        public string LastUpdateText => $"Last updated: {_lastUpdate:HH:mm:ss}";

        // Chart Properties
        public PlotModel UsageChart { get; set; }
        public PlotModel TimelineChart { get; set; }

        // Backing fields
        private long _totalDownloaded;
        private long _totalUploaded;
        private int _activeProcesses;
        private long _peakSpeed;
        private string _statusText = "Ready";
        private DateTime _lastUpdate = DateTime.Now;

        public bool ShowUploadInTimeline
        {
            get => _showUploadInTimeline;
            set
            {
                _showUploadInTimeline = value;
                OnPropertyChanged();
                UpdateTimelineChart();
            }
        }

        public StatisticsViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            ProcessStatistics = new ObservableCollection<ProcessStatistics>();
            FilteredProcessStatistics = new ObservableCollection<ProcessStatistics>();
            
            UsageChart = CreateUsageChart();
            TimelineChart = CreateTimelineChart();
        }

        public void SetDateRange(DateTime start, DateTime end)
        {
            _startDate = start;
            _endDate = end;
            LoadStatistics();
        }

        public void SetTimelineGranularity(string granularity)
        {
            _timelineGranularity = granularity;
            UpdateTimelineChart();
        }

        public async void LoadStatistics()
        {
            try
            {
                _statusText = "Loading statistics...";
                OnPropertyChanged(nameof(StatusText));

                // Get network stats from database
                var networkStats = await _databaseService.GetNetworkStatsAsync(_startDate, _endDate);
                
                // Process the data
                ProcessNetworkStats(networkStats);
                
                // Update charts
                UpdateUsageChart();
                UpdateTimelineChart();
                
                _lastUpdate = DateTime.Now;
                _statusText = "Statistics loaded successfully";
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(LastUpdateText));
            }
            catch (Exception ex)
            {
                _statusText = $"Error loading statistics: {ex.Message}";
                OnPropertyChanged(nameof(StatusText));
            }
        }

        private void ProcessNetworkStats(List<NetworkStats> networkStats)
        {
            ProcessStatistics.Clear();
            
            if (!networkStats.Any())
            {
                ResetSummaryValues();
                return;
            }

            // Group by process name
            var processGroups = networkStats.GroupBy(s => s.ProcessName).ToList();
            
            _totalDownloaded = 0;
            _totalUploaded = 0;
            _activeProcesses = processGroups.Count;
            _peakSpeed = 0;

            foreach (var group in processGroups)
            {
                var processName = group.Key;
                var stats = group.OrderBy(s => s.Timestamp).ToList();
                
                var totalDownload = stats.Sum(s => s.BytesReceived);
                var totalUpload = stats.Sum(s => s.BytesSent);
                var sessionCount = stats.Select(s => s.ProcessId).Distinct().Count();
                
                // Calculate active time (time between first and last activity)
                var activeTime = TimeSpan.Zero;
                if (stats.Count > 1)
                {
                    activeTime = stats.Last().Timestamp - stats.First().Timestamp;
                }
                
                // Calculate average speed
                var averageSpeed = activeTime.TotalSeconds > 0 
                    ? (totalDownload + totalUpload) / activeTime.TotalSeconds 
                    : 0;

                // Update peak speed
                if (averageSpeed > _peakSpeed)
                {
                    _peakSpeed = (long)averageSpeed;
                }

                ProcessStatistics.Add(new ProcessStatistics
                {
                    ProcessName = processName,
                    Downloaded = totalDownload,
                    Uploaded = totalUpload,
                    SessionCount = sessionCount,
                    ActiveTime = activeTime,
                    AverageSpeed = averageSpeed
                });

                _totalDownloaded += totalDownload;
                _totalUploaded += totalUpload;
            }

            // Sort by total usage
            var sortedStats = ProcessStatistics.OrderByDescending(p => p.Total).ToList();
            ProcessStatistics.Clear();
            foreach (var stat in sortedStats)
            {
                ProcessStatistics.Add(stat);
            }

            // Update filtered list
            FilterProcesses(string.Empty);
            
            // Update summary properties
            OnPropertyChanged(nameof(TotalDownloaded));
            OnPropertyChanged(nameof(TotalUploaded));
            OnPropertyChanged(nameof(ActiveProcesses));
            OnPropertyChanged(nameof(PeakSpeed));
        }

        private void ResetSummaryValues()
        {
            _totalDownloaded = 0;
            _totalUploaded = 0;
            _activeProcesses = 0;
            _peakSpeed = 0;
            
            OnPropertyChanged(nameof(TotalDownloaded));
            OnPropertyChanged(nameof(TotalUploaded));
            OnPropertyChanged(nameof(ActiveProcesses));
            OnPropertyChanged(nameof(PeakSpeed));
        }

        public void FilterProcesses(string filter)
        {
            FilteredProcessStatistics.Clear();
            
            var filtered = string.IsNullOrEmpty(filter) 
                ? ProcessStatistics 
                : ProcessStatistics.Where(p => p.ProcessName.Contains(filter, StringComparison.OrdinalIgnoreCase));
            
            foreach (var stat in filtered)
            {
                FilteredProcessStatistics.Add(stat);
            }
        }

        public void SortProcesses(string sortBy)
        {
            var sorted = sortBy switch
            {
                "Sort by Download" => FilteredProcessStatistics.OrderByDescending(p => p.Downloaded),
                "Sort by Upload" => FilteredProcessStatistics.OrderByDescending(p => p.Uploaded),
                "Sort by Name" => FilteredProcessStatistics.OrderBy(p => p.ProcessName),
                _ => FilteredProcessStatistics.OrderByDescending(p => p.Total)
            };

            FilteredProcessStatistics.Clear();
            foreach (var stat in sorted)
            {
                FilteredProcessStatistics.Add(stat);
            }
        }

        public async void ExportToCSV()
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    DefaultExt = "csv",
                    FileName = $"network_stats_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var csv = new StringBuilder();
                    csv.AppendLine("Process Name,Downloaded (bytes),Uploaded (bytes),Total (bytes),Sessions,Active Time,Average Speed (bps)");

                    foreach (var stat in ProcessStatistics)
                    {
                        csv.AppendLine($"{stat.ProcessName},{stat.Downloaded},{stat.Uploaded},{stat.Total},{stat.SessionCount},{stat.ActiveTime.TotalSeconds},{stat.AverageSpeed}");
                    }

                    await File.WriteAllTextAsync(saveDialog.FileName, csv.ToString());
                    
                    _statusText = $"Statistics exported to {Path.GetFileName(saveDialog.FileName)}";
                    OnPropertyChanged(nameof(StatusText));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting statistics: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private PlotModel CreateUsageChart()
        {
            var model = new PlotModel
            {
                Title = "Top 10 Processes by Usage",
                Background = OxyColors.Transparent,
                TextColor = OxyColors.White,
                PlotAreaBorderColor = OxyColors.Gray
            };

            var categoryAxis = new CategoryAxis
            {
                Position = AxisPosition.Left,
                TextColor = OxyColors.White
            };
            
            var valueAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                TextColor = OxyColors.White,
                StringFormat = "0.##"
            };

            model.Axes.Add(categoryAxis);
            model.Axes.Add(valueAxis);

            return model;
        }

        private PlotModel CreateTimelineChart()
        {
            var model = new PlotModel
            {
                Title = "Network Usage Timeline",
                Background = OxyColors.Transparent,
                TextColor = OxyColors.White,
                PlotAreaBorderColor = OxyColors.Gray
            };

            var dateAxis = new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                TextColor = OxyColors.White,
                StringFormat = "HH:mm"
            };
            
            var valueAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                TextColor = OxyColors.White,
                StringFormat = "0.##"
            };

            model.Axes.Add(dateAxis);
            model.Axes.Add(valueAxis);

            return model;
        }

        private void UpdateUsageChart()
        {
            UsageChart.Series.Clear();
            UsageChart.Axes[0].Reset();

            var top10 = ProcessStatistics.Take(10).ToList();
            if (!top10.Any()) return;

            var downloadSeries = new BarSeries
            {
                Title = "Downloaded",
                FillColor = OxyColors.Green,
                ItemsSource = top10.Select((p, i) => new BarItem { Value = p.Downloaded / (1024.0 * 1024.0), CategoryIndex = i }).ToList()
            };

            var uploadSeries = new BarSeries
            {
                Title = "Uploaded",
                FillColor = OxyColors.Red,
                ItemsSource = top10.Select((p, i) => new BarItem { Value = p.Uploaded / (1024.0 * 1024.0), CategoryIndex = i }).ToList()
            };

            ((CategoryAxis)UsageChart.Axes[0]).ItemsSource = top10.Select(p => p.ProcessName).ToList();
            
            UsageChart.Series.Add(downloadSeries);
            UsageChart.Series.Add(uploadSeries);
            UsageChart.InvalidatePlot(true);
        }

        private async void UpdateTimelineChart()
        {
            TimelineChart.Series.Clear();
            
            try
            {
                var networkStats = await _databaseService.GetNetworkStatsAsync(_startDate, _endDate);
                if (!networkStats.Any()) return;

                var timelineData = GroupStatsByTime(networkStats);
                
                var downloadSeries = new LineSeries
                {
                    Title = "Downloaded",
                    Color = OxyColors.Green,
                    StrokeThickness = 2
                };

                var uploadSeries = new LineSeries
                {
                    Title = "Uploaded",
                    Color = OxyColors.Red,
                    StrokeThickness = 2
                };

                foreach (var data in timelineData)
                {
                    var timestamp = DateTimeAxis.ToDouble(data.Key);
                    downloadSeries.Points.Add(new DataPoint(timestamp, data.Value.downloaded / (1024.0 * 1024.0)));
                    
                    if (_showUploadInTimeline)
                    {
                        uploadSeries.Points.Add(new DataPoint(timestamp, data.Value.uploaded / (1024.0 * 1024.0)));
                    }
                }

                TimelineChart.Series.Add(downloadSeries);
                if (_showUploadInTimeline)
                {
                    TimelineChart.Series.Add(uploadSeries);
                }
                
                TimelineChart.InvalidatePlot(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating timeline chart: {ex.Message}");
            }
        }

        private Dictionary<DateTime, (double downloaded, double uploaded)> GroupStatsByTime(List<NetworkStats> stats)
        {
            var grouped = new Dictionary<DateTime, (double downloaded, double uploaded)>();
            
            foreach (var stat in stats)
            {
                var key = _timelineGranularity switch
                {
                    "Hourly" => new DateTime(stat.Timestamp.Year, stat.Timestamp.Month, stat.Timestamp.Day, stat.Timestamp.Hour, 0, 0),
                    "Daily" => stat.Timestamp.Date,
                    "Weekly" => stat.Timestamp.Date.AddDays(-(int)stat.Timestamp.DayOfWeek),
                    _ => stat.Timestamp.Date
                };

                if (!grouped.ContainsKey(key))
                {
                    grouped[key] = (0, 0);
                }

                grouped[key] = (
                    grouped[key].downloaded + stat.BytesReceived,
                    grouped[key].uploaded + stat.BytesSent
                );
            }

            return grouped.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
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
            return FormatBytes(bps) + "ps";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ProcessStatistics
    {
        public string ProcessName { get; set; } = string.Empty;
        public long Downloaded { get; set; }
        public long Uploaded { get; set; }
        public long Total => Downloaded + Uploaded;
        public int SessionCount { get; set; }
        public TimeSpan ActiveTime { get; set; }
        public double AverageSpeed { get; set; }

        public string DownloadedFormatted => FormatBytes(Downloaded);
        public string UploadedFormatted => FormatBytes(Uploaded);
        public string TotalFormatted => FormatBytes(Total);
        public string ActiveTimeFormatted => ActiveTime.ToString(@"hh\:mm\:ss");
        public string AverageSpeedFormatted => FormatSpeed((long)AverageSpeed);

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
            return FormatBytes(bps) + "ps";
        }
    }
}