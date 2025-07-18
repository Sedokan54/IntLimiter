using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using NetLimiterClone.Models;

namespace NetLimiterClone.Services
{
    public class ETWNetworkMonitorService : IDisposable
    {
        private const string SESSION_NAME = "NetLimiterETWSession";
        private TraceEventSession? _session;
        private Task? _monitoringTask;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly ConcurrentDictionary<int, ProcessInfo> _processCache = new();
        private readonly ConcurrentDictionary<int, NetworkCounters> _networkCounters = new();
        private readonly object _lockObject = new();
        private bool _disposed = false;

        public event EventHandler<List<ProcessInfo>>? ProcessesUpdated;
        public event EventHandler<NetworkStats>? NetworkStatsUpdated;

        public ETWNetworkMonitorService()
        {
            // Check for administrator privileges
            if (!IsRunningAsAdmin())
            {
                throw new UnauthorizedAccessException("ETW monitoring requires administrator privileges");
            }
        }

        public void StartMonitoring()
        {
            if (_session != null)
                return;

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                
                // Clean up any existing session
                TraceEventSession.GetActiveSessionNames().ToList().ForEach(name =>
                {
                    if (name.Contains(SESSION_NAME))
                    {
                        try
                        {
                            var oldSession = new TraceEventSession(name);
                            oldSession.Stop();
                            oldSession.Dispose();
                        }
                        catch { }
                    }
                });

                _session = new TraceEventSession(SESSION_NAME);
                _session.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);

                _monitoringTask = Task.Run(() => MonitorNetworkEvents(_cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start ETW monitoring: {ex.Message}");
                throw;
            }
        }

        public void StopMonitoring()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _session?.Stop();
                _monitoringTask?.Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping ETW monitoring: {ex.Message}");
            }
        }

        private void MonitorNetworkEvents(CancellationToken cancellationToken)
        {
            try
            {
                if (_session == null)
                    return;

                var source = new ETWTraceEventSource(SESSION_NAME);
                
                // Subscribe to TCP/IP events
                // TODO: Update ETW event handlers for TraceEvent 3.0.7
                // source.Kernel.TcpIpSend += OnTcpIpSend;
                // source.Kernel.TcpIpReceive += OnTcpIpReceive;
                // source.Kernel.UdpIpSend += OnUdpIpSend;
                // source.Kernel.UdpIpReceive += OnUdpIpReceive;

                // Process events
                var processTimer = new Timer(ProcessNetworkCounters, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

                // Start processing
                source.Process();

                processTimer?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ETW monitoring error: {ex.Message}");
            }
        }

        private void OnTcpIpSend(Microsoft.Diagnostics.Tracing.Parsers.Kernel.TcpIpSendTraceData data)
        {
            UpdateNetworkCounters(data.ProcessID, data.size, 0);
        }

        private void OnTcpIpReceive(object data)
        {
            // TODO: Update to work with TraceEvent 3.0.7 API changes
            // UpdateNetworkCounters(data.ProcessID, 0, data.size);
        }

        private void OnUdpIpSend(object data)
        {
            // TODO: Update to work with TraceEvent 3.0.7 API changes
            // UpdateNetworkCounters(data.ProcessID, data.size, 0);
        }

        private void OnUdpIpReceive(object data)
        {
            // TODO: Update to work with TraceEvent 3.0.7 API changes
            // UpdateNetworkCounters(data.ProcessID, 0, data.size);
        }

        private void UpdateNetworkCounters(int processId, int bytesSent, int bytesReceived)
        {
            if (processId <= 0)
                return;

            _networkCounters.AddOrUpdate(processId, 
                new NetworkCounters 
                { 
                    BytesSent = bytesSent, 
                    BytesReceived = bytesReceived,
                    LastUpdate = DateTime.Now
                },
                (key, existing) => 
                {
                    existing.BytesSent += bytesSent;
                    existing.BytesReceived += bytesReceived;
                    existing.LastUpdate = DateTime.Now;
                    return existing;
                });
        }

        private void ProcessNetworkCounters(object? state)
        {
            try
            {
                var currentTime = DateTime.Now;
                var processList = new List<ProcessInfo>();

                // Get all running processes
                var processes = Process.GetProcesses();
                
                foreach (var process in processes)
                {
                    try
                    {
                        if (process.Id <= 0 || process.HasExited)
                            continue;

                        var processInfo = _processCache.GetOrAdd(process.Id, pid => CreateProcessInfo(process));
                        
                        // Update with current network stats
                        if (_networkCounters.TryGetValue(process.Id, out var counters))
                        {
                            // Calculate speeds (bytes per second)
                            var timeDiff = (currentTime - counters.LastSpeedUpdate).TotalSeconds;
                            if (timeDiff >= 1.0)
                            {
                                processInfo.DownloadSpeed = (long)((counters.BytesReceived - counters.LastBytesReceived) / timeDiff);
                                processInfo.UploadSpeed = (long)((counters.BytesSent - counters.LastBytesSent) / timeDiff);
                                
                                processInfo.TotalDownload = counters.BytesReceived;
                                processInfo.TotalUpload = counters.BytesSent;

                                counters.LastBytesReceived = counters.BytesReceived;
                                counters.LastBytesSent = counters.BytesSent;
                                counters.LastSpeedUpdate = currentTime;

                                // Fire network stats event
                                NetworkStatsUpdated?.Invoke(this, new NetworkStats
                                {
                                    ProcessId = process.Id,
                                    ProcessName = processInfo.ProcessName,
                                    BytesSent = processInfo.TotalUpload,
                                    BytesReceived = processInfo.TotalDownload,
                                    Timestamp = currentTime
                                });
                            }
                        }
                        else
                        {
                            // Reset speeds if no network activity
                            processInfo.DownloadSpeed = 0;
                            processInfo.UploadSpeed = 0;
                        }

                        processList.Add(processInfo);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing process {process.Id}: {ex.Message}");
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }

                // Fire processes updated event
                ProcessesUpdated?.Invoke(this, processList);

                // Clean up old counters
                CleanupOldCounters(currentTime);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ProcessNetworkCounters: {ex.Message}");
            }
        }

        private ProcessInfo CreateProcessInfo(Process process)
        {
            return new ProcessInfo
            {
                ProcessId = process.Id,
                ProcessName = process.ProcessName,
                ProcessPath = Helpers.ProcessHelper.GetProcessPath(process),
                IsSystemProcess = Helpers.ProcessHelper.IsSystemProcess(process),
                Icon = Helpers.ProcessHelper.GetProcessIcon(process),
                DownloadSpeed = 0,
                UploadSpeed = 0,
                TotalDownload = 0,
                TotalUpload = 0
            };
        }

        private void CleanupOldCounters(DateTime currentTime)
        {
            var cutoff = currentTime.AddMinutes(-5);
            var keysToRemove = _networkCounters
                .Where(kvp => kvp.Value.LastUpdate < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _networkCounters.TryRemove(key, out _);
                _processCache.TryRemove(key, out _);
            }
        }

        public void CleanupDeadProcesses()
        {
            var runningProcessIds = Process.GetProcesses().Select(p => p.Id).ToHashSet();
            
            var deadProcesses = _processCache.Keys
                .Where(pid => !runningProcessIds.Contains(pid))
                .ToList();

            foreach (var pid in deadProcesses)
            {
                _processCache.TryRemove(pid, out _);
                _networkCounters.TryRemove(pid, out _);
            }
        }

        private static bool IsRunningAsAdmin()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                StopMonitoring();
                _session?.Dispose();
                _cancellationTokenSource?.Dispose();
                _disposed = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing ETW monitor: {ex.Message}");
            }
        }

        private class NetworkCounters
        {
            public long BytesSent { get; set; }
            public long BytesReceived { get; set; }
            public long LastBytesSent { get; set; }
            public long LastBytesReceived { get; set; }
            public DateTime LastUpdate { get; set; }
            public DateTime LastSpeedUpdate { get; set; }
        }
    }
}