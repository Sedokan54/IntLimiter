using System;
using System.Windows;
using System.Windows.Controls;
using NetLimiterClone.ViewModels;

namespace NetLimiterClone.Views
{
    public partial class StatisticsWindow : Window
    {
        private readonly StatisticsViewModel _viewModel;

        public StatisticsWindow(StatisticsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            
            // Set initial date range
            StartDatePicker.SelectedDate = DateTime.Today.AddDays(-7);
            EndDatePicker.SelectedDate = DateTime.Today;
            
            // Load initial data
            _viewModel.LoadStatistics();
        }

        private void PeriodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PeriodComboBox.SelectedItem is ComboBoxItem item)
            {
                var period = item.Content.ToString();
                var isCustom = period == "Custom Range";
                
                StartDatePicker.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
                EndDatePicker.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
                DateRangeText.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
                
                if (!isCustom)
                {
                    var (start, end) = GetDateRangeForPeriod(period);
                    _viewModel.SetDateRange(start, end);
                }
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (PeriodComboBox.SelectedItem is ComboBoxItem item && item.Content.ToString() == "Custom Range")
            {
                if (StartDatePicker.SelectedDate.HasValue && EndDatePicker.SelectedDate.HasValue)
                {
                    _viewModel.SetDateRange(StartDatePicker.SelectedDate.Value, EndDatePicker.SelectedDate.Value);
                }
            }
            
            _viewModel.LoadStatistics();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ExportToCSV();
        }

        private void ProcessFilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _viewModel.FilterProcesses(ProcessFilterTextBox.Text);
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SortComboBox.SelectedItem is ComboBoxItem item)
            {
                _viewModel.SortProcesses(item.Content.ToString());
            }
        }

        private void GranularityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GranularityComboBox.SelectedItem is ComboBoxItem item)
            {
                _viewModel.SetTimelineGranularity(item.Content.ToString());
            }
        }

        private void ShowUploadCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowUploadInTimeline = ShowUploadCheckBox.IsChecked == true;
        }

        private (DateTime start, DateTime end) GetDateRangeForPeriod(string period)
        {
            var now = DateTime.Now;
            return period switch
            {
                "Today" => (now.Date, now.Date.AddDays(1)),
                "Yesterday" => (now.Date.AddDays(-1), now.Date),
                "Last 7 Days" => (now.Date.AddDays(-7), now.Date.AddDays(1)),
                "Last 30 Days" => (now.Date.AddDays(-30), now.Date.AddDays(1)),
                "This Month" => (new DateTime(now.Year, now.Month, 1), new DateTime(now.Year, now.Month, 1).AddMonths(1)),
                _ => (now.Date.AddDays(-7), now.Date.AddDays(1))
            };
        }
    }
}