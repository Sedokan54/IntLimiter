using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NetLimiterClone.Models;

namespace NetLimiterClone.Services
{
    public class BandwidthLimiterService : IDisposable
    {
        private readonly ConcurrentDictionary<int, BandwidthRule> _activeRules = new();
        private readonly ConcurrentDictionary<int, TokenBucket> _downloadBuckets = new();
        private readonly ConcurrentDictionary<int, TokenBucket> _uploadBuckets = new();
        private readonly Timer _enforcementTimer;
        private readonly WFPService _wfpService;
        private bool _disposed = false;

        public event EventHandler<ProcessLimitEventArgs>? ProcessLimitApplied;
        public event EventHandler<ProcessLimitEventArgs>? ProcessLimitRemoved;

        public BandwidthLimiterService()
        {
            try
            {
                _wfpService = new WFPService();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WFP Service initialization failed: {ex.Message}");
            }
            
            _enforcementTimer = new Timer(EnforceLimits, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
        }

        public bool SetProcessLimit(int processId, long downloadLimitBps, long uploadLimitBps, BandwidthPriority priority = BandwidthPriority.Normal)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                var rule = new BandwidthRule(processId, process.ProcessName, process.MainModule?.FileName ?? string.Empty)
                {
                    DownloadLimit = downloadLimitBps,
                    UploadLimit = uploadLimitBps,
                    Priority = priority,
                    IsEnabled = true
                };

                _activeRules.AddOrUpdate(processId, rule, (key, oldRule) => rule);

                // Create token buckets for rate limiting
                if (downloadLimitBps > 0)
                {
                    _downloadBuckets.AddOrUpdate(processId, 
                        new TokenBucket(downloadLimitBps, downloadLimitBps), 
                        (key, oldBucket) => new TokenBucket(downloadLimitBps, downloadLimitBps));
                }
                else
                {
                    _downloadBuckets.TryRemove(processId, out _);
                }

                if (uploadLimitBps > 0)
                {
                    _uploadBuckets.AddOrUpdate(processId, 
                        new TokenBucket(uploadLimitBps, uploadLimitBps), 
                        (key, oldBucket) => new TokenBucket(uploadLimitBps, uploadLimitBps));
                }
                else
                {
                    _uploadBuckets.TryRemove(processId, out _);
                }

                // Apply WFP filters for real traffic control
                if (_wfpService != null)
                {
                    _wfpService.SetProcessBandwidthLimit(processId, downloadLimitBps, uploadLimitBps);
                }

                ProcessLimitApplied?.Invoke(this, new ProcessLimitEventArgs(processId, rule));
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting process limit for PID {processId}: {ex.Message}");
                return false;
            }
        }

        public bool RemoveProcessLimit(int processId)
        {
            try
            {
                if (_activeRules.TryRemove(processId, out var rule))
                {
                    _downloadBuckets.TryRemove(processId, out _);
                    _uploadBuckets.TryRemove(processId, out _);

                    // Remove WFP filters
                    if (_wfpService != null)
                    {
                        _wfpService.RemoveProcessFilters(processId);
                    }

                    ProcessLimitRemoved?.Invoke(this, new ProcessLimitEventArgs(processId, rule));
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error removing process limit for PID {processId}: {ex.Message}");
                return false;
            }
        }

        public BandwidthRule? GetProcessLimit(int processId)
        {
            _activeRules.TryGetValue(processId, out var rule);
            return rule;
        }

        public IEnumerable<BandwidthRule> GetAllLimits()
        {
            return _activeRules.Values;
        }

        public bool IsProcessLimited(int processId)
        {
            return _activeRules.ContainsKey(processId) && _activeRules[processId].IsEnabled;
        }

        public bool BlockProcess(int processId)
        {
            try
            {
                if (_wfpService != null)
                {
                    return _wfpService.BlockProcess(processId);
                }
                return SetProcessLimit(processId, 0, 0, BandwidthPriority.Low);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error blocking process {processId}: {ex.Message}");
                return false;
            }
        }

        public bool UnblockProcess(int processId)
        {
            try
            {
                if (_wfpService != null)
                {
                    return _wfpService.UnblockProcess(processId);
                }
                return RemoveProcessLimit(processId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error unblocking process {processId}: {ex.Message}");
                return false;
            }
        }

        private void EnforceLimits(object? state)
        {
            try
            {
                // This is a simplified enforcement mechanism
                // In a real implementation, this would integrate with WFP or similar
                foreach (var kvp in _activeRules)
                {
                    var processId = kvp.Key;
                    var rule = kvp.Value;

                    if (!rule.IsEnabled)
                        continue;

                    try
                    {
                        // Check if process still exists
                        var process = Process.GetProcessById(processId);
                        if (process.HasExited)
                        {
                            RemoveProcessLimit(processId);
                            continue;
                        }

                        // Simulate traffic control (in real implementation, this would be done at network level)
                        EnforceProcessLimit(processId, rule);
                    }
                    catch (ArgumentException)
                    {
                        // Process no longer exists
                        RemoveProcessLimit(processId);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error enforcing limit for process {processId}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in EnforceLimits: {ex.Message}");
            }
        }

        private void EnforceProcessLimit(int processId, BandwidthRule rule)
        {
            // Token bucket algorithm implementation
            var now = DateTime.UtcNow;

            // Download enforcement
            if (_downloadBuckets.TryGetValue(processId, out var downloadBucket))
            {
                downloadBucket.Refill(now);
                // In real implementation, this would throttle actual network traffic
            }

            // Upload enforcement
            if (_uploadBuckets.TryGetValue(processId, out var uploadBucket))
            {
                uploadBucket.Refill(now);
                // In real implementation, this would throttle actual network traffic
            }
        }

        public void PauseAllLimits()
        {
            foreach (var rule in _activeRules.Values)
            {
                rule.IsEnabled = false;
            }
        }

        public void ResumeAllLimits()
        {
            foreach (var rule in _activeRules.Values)
            {
                rule.IsEnabled = true;
            }
        }

        public void ClearAllLimits()
        {
            var processIds = new List<int>(_activeRules.Keys);
            foreach (var processId in processIds)
            {
                RemoveProcessLimit(processId);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _enforcementTimer?.Dispose();
                _wfpService?.Dispose();
                ClearAllLimits();
                _disposed = true;
            }
        }
    }

    public class TokenBucket
    {
        private readonly long _capacity;
        private readonly long _refillRate;
        private long _tokens;
        private DateTime _lastRefillTime;
        private readonly object _lock = new object();

        public TokenBucket(long capacity, long refillRatePerSecond)
        {
            _capacity = capacity;
            _refillRate = refillRatePerSecond;
            _tokens = capacity;
            _lastRefillTime = DateTime.UtcNow;
        }

        public bool TryConsume(long tokens)
        {
            lock (_lock)
            {
                Refill(DateTime.UtcNow);
                
                if (_tokens >= tokens)
                {
                    _tokens -= tokens;
                    return true;
                }
                return false;
            }
        }

        public void Refill(DateTime now)
        {
            lock (_lock)
            {
                var timePassed = (now - _lastRefillTime).TotalSeconds;
                if (timePassed > 0)
                {
                    var tokensToAdd = (long)(timePassed * _refillRate);
                    _tokens = Math.Min(_capacity, _tokens + tokensToAdd);
                    _lastRefillTime = now;
                }
            }
        }

        public long AvailableTokens
        {
            get
            {
                lock (_lock)
                {
                    return _tokens;
                }
            }
        }
    }

    public class ProcessLimitEventArgs : EventArgs
    {
        public int ProcessId { get; }
        public BandwidthRule Rule { get; }

        public ProcessLimitEventArgs(int processId, BandwidthRule rule)
        {
            ProcessId = processId;
            Rule = rule;
        }
    }
}