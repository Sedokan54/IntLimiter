using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NetLimiterClone.ViewModels;

namespace NetLimiterClone.Views
{
    public partial class ConnectionDetailsWindow : Window
    {
        private readonly ConnectionDetailsViewModel _viewModel;

        public ConnectionDetailsWindow(ConnectionDetailsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            
            // Start loading connections
            _viewModel.LoadConnections();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.LoadConnections();
        }

        private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _viewModel.FilterConnections(FilterTextBox.Text);
        }

        private void ProtocolComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProtocolComboBox.SelectedItem is ComboBoxItem item)
            {
                _viewModel.FilterByProtocol(item.Content.ToString());
            }
        }

        private void StateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StateComboBox.SelectedItem is ComboBoxItem item)
            {
                _viewModel.FilterByState(item.Content.ToString());
            }
        }

        private void ConnectionsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ConnectionsGrid.SelectedItem != null)
            {
                _viewModel.ShowConnectionDetails();
            }
        }

        private void CloseConnectionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ConnectionsGrid.SelectedItem != null)
            {
                _viewModel.CloseSelectedConnection();
            }
        }

        private void CopyAddressMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ConnectionsGrid.SelectedItem != null)
            {
                _viewModel.CopySelectedAddress();
            }
        }

        private void WhoisLookupMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ConnectionsGrid.SelectedItem != null)
            {
                _viewModel.PerformWhoisLookup();
            }
        }

        private void ExportMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ExportToCSV();
        }

        private void CloseSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConnectionsGrid.SelectedItem != null)
            {
                _viewModel.CloseSelectedConnection();
            }
        }

        private void ExportAllButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ExportToCSV();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}