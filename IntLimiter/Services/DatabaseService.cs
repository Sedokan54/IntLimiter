using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NetLimiterClone.Models;

namespace NetLimiterClone.Services
{
    public class DatabaseService : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Timer _cleanupTimer;
        private readonly object _lockObject = new object();
        private bool _disposed = false;

        public DatabaseService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dbDirectory = Path.Combine(appDataPath, "NetLimiterClone");
            Directory.CreateDirectory(dbDirectory);
            
            var databasePath = Path.Combine(dbDirectory, "netlimiter.db");
            var connectionString = $"Data Source={databasePath}";
            
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connectionString)
                .Options;

            _context = new AppDbContext(options);
            _context.Database.EnsureCreated();

            // Setup cleanup timer to run daily
            _cleanupTimer = new Timer(CleanupOldRecords, null, TimeSpan.Zero, TimeSpan.FromDays(1));
        }

        public async Task SaveNetworkStatsAsync(NetworkStats stats)
        {
            try
            {
                lock (_lockObject)
                {
                    var entity = new NetworkStatsEntity
                    {
                        ProcessId = stats.ProcessId,
                        ProcessName = stats.ProcessName,
                        BytesSent = stats.BytesSent,
                        BytesReceived = stats.BytesReceived,
                        Timestamp = stats.Timestamp
                    };

                    _context.NetworkStats.Add(entity);
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving network stats: {ex.Message}");
            }
        }

        public async Task SaveBandwidthRuleAsync(BandwidthRule rule)
        {
            try
            {
                lock (_lockObject)
                {
                    var existingRule = _context.BandwidthRules
                        .FirstOrDefault(r => r.ProcessId == rule.ProcessId);

                    if (existingRule != null)
                    {
                        existingRule.DownloadLimit = rule.DownloadLimit;
                        existingRule.UploadLimit = rule.UploadLimit;
                        existingRule.IsEnabled = rule.IsEnabled;
                        existingRule.Priority = rule.Priority.ToString();
                        existingRule.LastModified = rule.LastModified;
                    }
                    else
                    {
                        var entity = new BandwidthRuleEntity
                        {
                            ProcessId = rule.ProcessId,
                            ProcessName = rule.ProcessName,
                            ProcessPath = rule.ProcessPath,
                            DownloadLimit = rule.DownloadLimit,
                            UploadLimit = rule.UploadLimit,
                            IsEnabled = rule.IsEnabled,
                            Priority = rule.Priority.ToString(),
                            CreatedAt = rule.CreatedAt,
                            LastModified = rule.LastModified
                        };

                        _context.BandwidthRules.Add(entity);
                    }

                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving bandwidth rule: {ex.Message}");
            }
        }

        public async Task<List<BandwidthRule>> LoadBandwidthRulesAsync()
        {
            try
            {
                lock (_lockObject)
                {
                    var entities = _context.BandwidthRules.ToList();
                    var rules = new List<BandwidthRule>();

                    foreach (var entity in entities)
                    {
                        var rule = new BandwidthRule(entity.ProcessId, entity.ProcessName, entity.ProcessPath)
                        {
                            DownloadLimit = entity.DownloadLimit,
                            UploadLimit = entity.UploadLimit,
                            IsEnabled = entity.IsEnabled,
                            CreatedAt = entity.CreatedAt,
                            LastModified = entity.LastModified
                        };

                        if (Enum.TryParse<BandwidthPriority>(entity.Priority, out var priority))
                        {
                            rule.Priority = priority;
                        }

                        rules.Add(rule);
                    }

                    return rules;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading bandwidth rules: {ex.Message}");
                return new List<BandwidthRule>();
            }
        }

        public async Task DeleteBandwidthRuleAsync(int processId)
        {
            try
            {
                lock (_lockObject)
                {
                    var rule = _context.BandwidthRules
                        .FirstOrDefault(r => r.ProcessId == processId);

                    if (rule != null)
                    {
                        _context.BandwidthRules.Remove(rule);
                        _context.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting bandwidth rule: {ex.Message}");
            }
        }

        public async Task<List<NetworkStats>> GetNetworkStatsAsync(DateTime from, DateTime to, int? processId = null)
        {
            try
            {
                lock (_lockObject)
                {
                    var query = _context.NetworkStats
                        .Where(s => s.Timestamp >= from && s.Timestamp <= to);

                    if (processId.HasValue)
                    {
                        query = query.Where(s => s.ProcessId == processId.Value);
                    }

                    var entities = query.OrderBy(s => s.Timestamp).ToList();
                    var stats = new List<NetworkStats>();

                    foreach (var entity in entities)
                    {
                        stats.Add(new NetworkStats
                        {
                            ProcessId = entity.ProcessId,
                            ProcessName = entity.ProcessName,
                            BytesSent = entity.BytesSent,
                            BytesReceived = entity.BytesReceived,
                            Timestamp = entity.Timestamp
                        });
                    }

                    return stats;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting network stats: {ex.Message}");
                return new List<NetworkStats>();
            }
        }

        public async Task<Dictionary<string, long>> GetDailyUsageAsync(DateTime date)
        {
            try
            {
                lock (_lockObject)
                {
                    var startOfDay = date.Date;
                    var endOfDay = startOfDay.AddDays(1);

                    var stats = _context.NetworkStats
                        .Where(s => s.Timestamp >= startOfDay && s.Timestamp < endOfDay)
                        .GroupBy(s => s.ProcessName)
                        .Select(g => new
                        {
                            ProcessName = g.Key,
                            TotalBytes = g.Sum(s => s.BytesSent + s.BytesReceived)
                        })
                        .ToDictionary(x => x.ProcessName, x => x.TotalBytes);

                    return stats;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting daily usage: {ex.Message}");
                return new Dictionary<string, long>();
            }
        }

        private void CleanupOldRecords(object? state)
        {
            try
            {
                lock (_lockObject)
                {
                    var cutoffDate = DateTime.Now.AddDays(-30); // Keep 30 days by default

                    var oldStats = _context.NetworkStats
                        .Where(s => s.Timestamp < cutoffDate);

                    _context.NetworkStats.RemoveRange(oldStats);
                    _context.SaveChanges();

                    System.Diagnostics.Debug.WriteLine($"Cleaned up old network stats before {cutoffDate}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }

        public async Task<long> GetDatabaseSizeAsync()
        {
            try
            {
                var dbPath = _context.Database.GetConnectionString()?.Replace("Data Source=", "");
                if (!string.IsNullOrEmpty(dbPath) && File.Exists(dbPath))
                {
                    var fileInfo = new FileInfo(dbPath);
                    return fileInfo.Length;
                }
                return 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task OptimizeDatabaseAsync()
        {
            try
            {
                lock (_lockObject)
                {
                    _context.Database.ExecuteSqlRaw("VACUUM");
                    _context.Database.ExecuteSqlRaw("REINDEX");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error optimizing database: {ex.Message}");
            }
        }

        public void SaveBandwidthRule(BandwidthRule rule)
        {
            // TODO: Implement saving bandwidth rule to database
        }

        public void RemoveBandwidthRule(int processId)
        {
            // TODO: Implement removing bandwidth rule from database
        }

        public async Task SaveProcessGroupAsync(ProcessGroup group)
        {
            // TODO: Implement saving process group to database
            await Task.CompletedTask;
        }

        public async Task<List<BandwidthRule>> GetAllBandwidthRulesAsync()
        {
            // TODO: Implement getting all bandwidth rules from database
            await Task.CompletedTask;
            return new List<BandwidthRule>();
        }

        public async Task<List<ProcessGroup>> GetAllProcessGroupsAsync()
        {
            // TODO: Implement getting all process groups from database
            await Task.CompletedTask;
            return new List<ProcessGroup>();
        }

        public async Task ClearAllBandwidthRulesAsync()
        {
            // TODO: Implement clearing all bandwidth rules from database
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cleanupTimer?.Dispose();
                _context?.Dispose();
                _disposed = true;
            }
        }
    }

    public class AppDbContext : DbContext
    {
        public DbSet<NetworkStatsEntity> NetworkStats { get; set; } = null!;
        public DbSet<BandwidthRuleEntity> BandwidthRules { get; set; } = null!;

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NetworkStatsEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ProcessId, e.Timestamp });
                entity.HasIndex(e => e.Timestamp);
            });

            modelBuilder.Entity<BandwidthRuleEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ProcessId).IsUnique();
            });
        }
    }

    public class NetworkStatsEntity
    {
        public int Id { get; set; }
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public long BytesSent { get; set; }
        public long BytesReceived { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class BandwidthRuleEntity
    {
        public int Id { get; set; }
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public string ProcessPath { get; set; } = string.Empty;
        public long DownloadLimit { get; set; }
        public long UploadLimit { get; set; }
        public bool IsEnabled { get; set; }
        public string Priority { get; set; } = "Normal";
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }
    }
}