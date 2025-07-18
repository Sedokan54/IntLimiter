using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using NetLimiterClone.Models;

namespace NetLimiterClone.Services
{
    public class NetworkMonitorService : IDisposable
    {
        private readonly Dictionary<int, ProcessInfo> _processCache = new();
        private readonly Dictionary<int, NetworkStats> _previousStats = new();
        private readonly Timer _updateTimer;
        private readonly object _lockObject = new();
        private bool _disposed = false;

        public event EventHandler<ProcessInfo[]>? ProcessesUpdated;
        public event EventHandler<NetworkStats>? NetworkStatsUpdated;

        public ProcessInfo[] ActiveProcesses => _processCache.Values.ToArray();

        public NetworkMonitorService()
        {
            _updateTimer = new Timer(UpdateNetworkStats, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        private void UpdateNetworkStats(object? state)
        {
            try
            {
                var processes = GetRunningProcesses();
                var processInfos = new List<ProcessInfo>();

                foreach (var process in processes)
                {
                    try
                    {
                        var processInfo = GetOrCreateProcessInfo(process);
                        if (processInfo != null)
                        {
                            UpdateProcessNetworkStats(processInfo);
                            processInfos.Add(processInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error updating process {process.ProcessName}: {ex.Message}");
                    }
                }

                ProcessesUpdated?.Invoke(this, processInfos.ToArray());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateNetworkStats: {ex.Message}");
            }
        }

        private Process[] GetRunningProcesses()
        {
            try
            {
                return Process.GetProcesses()
                    .Where(p => !p.HasExited)
                    .ToArray();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting running processes: {ex.Message}");
                return Array.Empty<Process>();
            }
        }

        private ProcessInfo? GetOrCreateProcessInfo(Process process)
        {
            lock (_lockObject)
            {
                if (_processCache.TryGetValue(process.Id, out var existingInfo))
                {
                    return existingInfo;
                }

                try
                {
                    var processInfo = new ProcessInfo
                    {
                        ProcessId = process.Id,
                        ProcessName = process.ProcessName,
                        ProcessPath = Helpers.ProcessHelper.GetProcessPath(process),
                        IsSystemProcess = Helpers.ProcessHelper.IsSystemProcess(process),
                        Icon = Helpers.ProcessHelper.GetProcessIcon(process)
                    };

                    _processCache[process.Id] = processInfo;
                    return processInfo;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error creating ProcessInfo for {process.ProcessName}: {ex.Message}");
                    return null;
                }
            }
        }


        private void UpdateProcessNetworkStats(ProcessInfo processInfo)
        {
            try
            {
                // Get network stats using performance counters
                var currentStats = GetNetworkStatsFromPerformanceCounters(processInfo.ProcessId);
                
                if (_previousStats.TryGetValue(processInfo.ProcessId, out var previousStats))
                {
                    // Calculate speed as difference between current and previous stats
                    var timeDiff = (currentStats.Timestamp - previousStats.Timestamp).TotalSeconds;
                    if (timeDiff > 0)
                    {
                        processInfo.DownloadSpeed = (long)((currentStats.BytesReceived - previousStats.BytesReceived) / timeDiff);
                        processInfo.UploadSpeed = (long)((currentStats.BytesSent - previousStats.BytesSent) / timeDiff);
                    }
                }

                processInfo.TotalDownload = currentStats.BytesReceived;
                processInfo.TotalUpload = currentStats.BytesSent;
                processInfo.ConnectionCount = GetConnectionCount(processInfo.ProcessId);

                _previousStats[processInfo.ProcessId] = currentStats;
                
                NetworkStatsUpdated?.Invoke(this, currentStats);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating network stats for process {processInfo.ProcessName}: {ex.Message}");
            }
        }

        private NetworkStats GetNetworkStatsFromPerformanceCounters(int processId)
        {
            try
            {
                // This is a simplified version - in a real implementation you would use:
                // 1. ETW (Event Tracing for Windows) for accurate per-process network monitoring
                // 2. WMI queries for network adapter statistics
                // 3. Performance counters for system-wide network statistics
                
                // For now, return mock data with some randomization to simulate network activity
                var random = new Random();
                var baseValue = processId * 1000; // Some base value based on process ID
                
                return new NetworkStats
                {
                    ProcessId = processId,
                    BytesReceived = baseValue + random.Next(0, 10000),
                    BytesSent = baseValue + random.Next(0, 5000),
                    Timestamp = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting network stats for process {processId}: {ex.Message}");
                return new NetworkStats { ProcessId = processId };
            }
        }

        private int GetConnectionCount(int processId)
        {
            try
            {
                // In a real implementation, you would use netstat or similar to get actual connection count
                // For now, return a simulated count
                return new Random().Next(0, 10);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public void CleanupDeadProcesses()
        {
            lock (_lockObject)
            {
                var runningProcessIds = Process.GetProcesses()
                    .Where(p => !p.HasExited)
                    .Select(p => p.Id)
                    .ToHashSet();

                var deadProcessIds = _processCache.Keys
                    .Where(id => !runningProcessIds.Contains(id))
                    .ToList();

                foreach (var deadProcessId in deadProcessIds)
                {
                    _processCache.Remove(deadProcessId);
                    _previousStats.Remove(deadProcessId);
                }
            }
        }

        public ProcessInfo[] GetAllProcesses()
        {
            lock (_lockObject)
            {
                return _processCache.Values.ToArray();
            }
        }

        public ProcessInfo? GetProcessById(int processId)
        {
            lock (_lockObject)
            {
                _processCache.TryGetValue(processId, out var processInfo);
                return processInfo;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _updateTimer?.Dispose();
                _processCache.Clear();
                _previousStats.Clear();
                _disposed = true;
            }
        }
    }
}