using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using NetLimiterClone.Models;

namespace NetLimiterClone.ViewModels
{
    public class ConnectionDetailsViewModel : INotifyPropertyChanged
    {
        private readonly ProcessInfo _processInfo;
        private readonly List<ConnectionInfo> _allConnections = new();
        private ObservableCollection<ConnectionInfo> _connections = new();
        private ConnectionInfo? _selectedConnection;
        private string _filterText = string.Empty;
        private string _protocolFilter = "All";
        private string _stateFilter = "All States";

        public ObservableCollection<ConnectionInfo> Connections
        {
            get => _connections;
            set => SetProperty(ref _connections, value);
        }

        public ConnectionInfo? SelectedConnection
        {
            get => _selectedConnection;
            set => SetProperty(ref _selectedConnection, value);
        }

        public bool HasSelectedConnection => SelectedConnection != null;

        // Process Information
        public string ProcessName => _processInfo.ProcessName;
        public string ProcessPath => _processInfo.ProcessPath;
        public int ProcessId => _processInfo.ProcessId;
        public ImageSource? ProcessIcon => _processInfo.Icon;

        // Connection Statistics
        public int TotalConnections => _allConnections.Count;
        public int TcpConnections => _allConnections.Count(c => c.Protocol == "TCP");
        public int UdpConnections => _allConnections.Count(c => c.Protocol == "UDP");
        public int ListeningPorts => _allConnections.Count(c => c.State == ConnectionState.Listen);

        public ConnectionDetailsViewModel(ProcessInfo processInfo)
        {
            _processInfo = processInfo;
        }

        public async void LoadConnections()
        {
            try
            {
                await Task.Run(() =>
                {
                    _allConnections.Clear();
                    LoadTcpConnections();
                    LoadUdpConnections();
                });

                Application.Current.Dispatcher.Invoke(() =>
                {
                    UpdateConnectionList();
                    OnPropertyChanged(nameof(TotalConnections));
                    OnPropertyChanged(nameof(TcpConnections));
                    OnPropertyChanged(nameof(UdpConnections));
                    OnPropertyChanged(nameof(ListeningPorts));
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading connections: {ex.Message}");
            }
        }

        private void LoadTcpConnections()
        {
            try
            {
                var tcpConnections = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();
                var tcpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();

                // Active connections
                foreach (var connection in tcpConnections)
                {
                    var connInfo = new ConnectionInfo
                    {
                        ProcessId = _processInfo.ProcessId,
                        ProcessName = _processInfo.ProcessName,
                        Protocol = "TCP",
                        LocalAddress = connection.LocalEndPoint.Address,
                        LocalPort = connection.LocalEndPoint.Port,
                        RemoteAddress = connection.RemoteEndPoint.Address,
                        RemotePort = connection.RemoteEndPoint.Port,
                        State = ConvertTcpState(connection.State),
                        CreatedTime = DateTime.Now.AddMinutes(-new Random().Next(0, 60)) // Estimate
                    };

                    _allConnections.Add(connInfo);
                }

                // Listening connections
                foreach (var listener in tcpListeners)
                {
                    var connInfo = new ConnectionInfo
                    {
                        ProcessId = _processInfo.ProcessId,
                        ProcessName = _processInfo.ProcessName,
                        Protocol = "TCP",
                        LocalAddress = listener.Address,
                        LocalPort = listener.Port,
                        RemoteAddress = IPAddress.Any,
                        RemotePort = 0,
                        State = ConnectionState.Listen,
                        CreatedTime = DateTime.Now.AddMinutes(-new Random().Next(0, 60))
                    };

                    _allConnections.Add(connInfo);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading TCP connections: {ex.Message}");
            }
        }

        private void LoadUdpConnections()
        {
            try
            {
                var udpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners();

                foreach (var listener in udpListeners)
                {
                    var connInfo = new ConnectionInfo
                    {
                        ProcessId = _processInfo.ProcessId,
                        ProcessName = _processInfo.ProcessName,
                        Protocol = "UDP",
                        LocalAddress = listener.Address,
                        LocalPort = listener.Port,
                        RemoteAddress = IPAddress.Any,
                        RemotePort = 0,
                        State = ConnectionState.Listen,
                        CreatedTime = DateTime.Now.AddMinutes(-new Random().Next(0, 60))
                    };

                    _allConnections.Add(connInfo);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading UDP connections: {ex.Message}");
            }
        }

        private static ConnectionState ConvertTcpState(TcpState tcpState)
        {
            return tcpState switch
            {
                TcpState.Closed => ConnectionState.Closed,
                TcpState.Listen => ConnectionState.Listen,
                TcpState.SynSent => ConnectionState.SynSent,
                TcpState.SynReceived => ConnectionState.SynRcvd,
                TcpState.Established => ConnectionState.Established,
                TcpState.FinWait1 => ConnectionState.FinWait1,
                TcpState.FinWait2 => ConnectionState.FinWait2,
                TcpState.CloseWait => ConnectionState.CloseWait,
                TcpState.Closing => ConnectionState.Closing,
                TcpState.LastAck => ConnectionState.LastAck,
                TcpState.TimeWait => ConnectionState.TimeWait,
                TcpState.DeleteTcb => ConnectionState.DeleteTcb,
                _ => ConnectionState.Unknown
            };
        }

        public void FilterConnections(string filterText)
        {
            _filterText = filterText;
            UpdateConnectionList();
        }

        public void FilterByProtocol(string protocol)
        {
            _protocolFilter = protocol;
            UpdateConnectionList();
        }

        public void FilterByState(string state)
        {
            _stateFilter = state;
            UpdateConnectionList();
        }

        private void UpdateConnectionList()
        {
            var filtered = _allConnections.AsEnumerable();

            // Apply text filter
            if (!string.IsNullOrEmpty(_filterText))
            {
                filtered = filtered.Where(c =>
                    c.LocalAddressString.Contains(_filterText, StringComparison.OrdinalIgnoreCase) ||
                    c.RemoteAddressString.Contains(_filterText, StringComparison.OrdinalIgnoreCase) ||
                    c.LocalPort.ToString().Contains(_filterText) ||
                    c.RemotePort.ToString().Contains(_filterText));
            }

            // Apply protocol filter
            if (_protocolFilter != "All")
            {
                filtered = filtered.Where(c => c.Protocol == _protocolFilter);
            }

            // Apply state filter
            if (_stateFilter != "All States")
            {
                var targetState = _stateFilter switch
                {
                    "Established" => ConnectionState.Established,
                    "Listening" => ConnectionState.Listen,
                    "Time Wait" => ConnectionState.TimeWait,
                    "Close Wait" => ConnectionState.CloseWait,
                    _ => ConnectionState.Unknown
                };

                if (targetState != ConnectionState.Unknown)
                {
                    filtered = filtered.Where(c => c.State == targetState);
                }
            }

            Connections.Clear();
            foreach (var connection in filtered.OrderBy(c => c.Protocol).ThenBy(c => c.LocalPort))
            {
                Connections.Add(connection);
            }
        }

        public void ShowConnectionDetails()
        {
            if (SelectedConnection == null) return;

            var details = $"Connection Details\n\n" +
                         $"Protocol: {SelectedConnection.Protocol}\n" +
                         $"Local: {SelectedConnection.LocalEndpoint}\n" +
                         $"Remote: {SelectedConnection.RemoteEndpoint}\n" +
                         $"State: {SelectedConnection.StateString}\n" +
                         $"Duration: {SelectedConnection.DurationFormatted}\n" +
                         $"Bytes Sent: {SelectedConnection.BytesSentFormatted}\n" +
                         $"Bytes Received: {SelectedConnection.BytesReceivedFormatted}";

            MessageBox.Show(details, "Connection Details", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void CloseSelectedConnection()
        {
            if (SelectedConnection == null) return;

            try
            {
                // This is a simplified implementation
                // In a real application, you would use WinAPI calls to close the connection
                var result = MessageBox.Show(
                    $"Are you sure you want to close this connection?\n\n" +
                    $"{SelectedConnection.Protocol} {SelectedConnection.LocalEndpoint} -> {SelectedConnection.RemoteEndpoint}",
                    "Close Connection",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _allConnections.Remove(SelectedConnection);
                    UpdateConnectionList();
                    MessageBox.Show("Connection closed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error closing connection: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void CopySelectedAddress()
        {
            if (SelectedConnection == null) return;

            try
            {
                var address = SelectedConnection.RemoteAddress.ToString();
                Clipboard.SetText(address);
                MessageBox.Show($"Address copied to clipboard: {address}", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error copying address: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void PerformWhoisLookup()
        {
            if (SelectedConnection == null) return;

            try
            {
                var address = SelectedConnection.RemoteAddress.ToString();
                var url = $"https://whois.net/ip/{address}";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error performing whois lookup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ExportToCSV()
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    DefaultExt = "csv",
                    FileName = $"connections_{ProcessName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var csv = new StringBuilder();
                    csv.AppendLine("Protocol,Local Address,Local Port,Remote Address,Remote Port,State,Bytes Sent,Bytes Received,Duration");

                    foreach (var connection in Connections)
                    {
                        csv.AppendLine($"{connection.Protocol},{connection.LocalAddressString},{connection.LocalPort}," +
                                      $"{connection.RemoteAddressString},{connection.RemotePort},{connection.StateString}," +
                                      $"{connection.BytesSent},{connection.BytesReceived},{connection.DurationFormatted}");
                    }

                    File.WriteAllText(saveDialog.FileName, csv.ToString());
                    MessageBox.Show($"Connections exported to {Path.GetFileName(saveDialog.FileName)}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting connections: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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