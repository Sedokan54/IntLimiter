using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using NetLimiterClone.Models;

namespace NetLimiterClone.Services
{
    public class CLIService
    {
        private readonly NetworkMonitorService _networkMonitorService;
        private readonly BandwidthLimiterService _bandwidthLimiterService;
        private readonly DatabaseService _databaseService;
        private readonly ProfileService _profileService;

        public CLIService()
        {
            _networkMonitorService = new NetworkMonitorService();
            _bandwidthLimiterService = new BandwidthLimiterService();
            _databaseService = new DatabaseService();
            _profileService = new ProfileService(_bandwidthLimiterService);
        }

        public string ProcessArguments(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                return ShowHelp();
            }

            var command = args[0].ToLower();
            
            return command switch
            {
                "--help" or "-h" => ShowHelp(),
                "--list-processes" => ListProcesses(args),
                "--set-limit" => SetLimit(args),
                "--remove-limit" => RemoveLimit(args),
                "--get-stats" => GetStats(args),
                "--list-profiles" => ListProfiles(args),
                "--apply-profile" => ApplyProfile(args),
                "--version" => GetVersion(),
                _ => $"Unknown command: {command}\nUse --help for available commands."
            };
        }

        private string ShowHelp()
        {
            return """
                NetLimiter Clone - Command Line Interface
                
                Usage: NetLimiter.exe [command] [options]
                
                Commands:
                  --help, -h              Show this help message
                  --list-processes        List all processes with network activity
                  --set-limit <pid> <dl> <ul>  Set bandwidth limit for process
                                          pid: Process ID
                                          dl:  Download limit in KB/s (0 = unlimited)
                                          ul:  Upload limit in KB/s (0 = unlimited)
                  --remove-limit <pid>    Remove bandwidth limit for process
                  --get-stats [pid]       Get network statistics (all or specific process)
                  --list-profiles         List all available profiles
                  --apply-profile <name>  Apply a specific profile
                  --version               Show version information
                
                Options:
                  --output json           Output results in JSON format
                  --start-minimized       Start application minimized to system tray
                
                Examples:
                  NetLimiter.exe --list-processes --output json
                  NetLimiter.exe --set-limit 1234 1000 500
                  NetLimiter.exe --remove-limit 1234
                  NetLimiter.exe --apply-profile Gaming
                """;
        }

        private string ListProcesses(string[] args)
        {
            var outputJson = args.Contains("--output") && args.Contains("json");
            var processes = _networkMonitorService.ActiveProcesses;
            
            if (outputJson)
            {
                var processData = processes.Select(p => new
                {
                    ProcessId = p.ProcessId,
                    ProcessName = p.ProcessName,
                    ProcessPath = p.ProcessPath,
                    DownloadSpeed = p.DownloadSpeed,
                    UploadSpeed = p.UploadSpeed,
                    TotalDownload = p.TotalDownload,
                    TotalUpload = p.TotalUpload,
                    ConnectionCount = p.ConnectionCount,
                    IsLimited = p.IsLimited,
                    DownloadLimit = p.DownloadLimit,
                    UploadLimit = p.UploadLimit
                });
                
                return JsonSerializer.Serialize(processData, new JsonSerializerOptions { WriteIndented = true });
            }
            else
            {
                var result = "Active Processes:\n";
                result += "PID\tName\t\t\tDownload\tUpload\t\tConnections\tLimited\n";
                result += new string('-', 80) + "\n";
                
                foreach (var process in processes)
                {
                    result += $"{process.ProcessId}\t{process.ProcessName.PadRight(20)}\t";
                    result += $"{FormatBytes(process.DownloadSpeed)}/s\t{FormatBytes(process.UploadSpeed)}/s\t";
                    result += $"{process.ConnectionCount}\t\t{(process.IsLimited ? "Yes" : "No")}\n";
                }
                
                return result;
            }
        }

        private string SetLimit(string[] args)
        {
            if (args.Length < 4)
            {
                return "Error: set-limit requires 3 parameters: <pid> <download_limit> <upload_limit>";
            }

            if (!int.TryParse(args[1], out int pid))
            {
                return $"Error: Invalid process ID: {args[1]}";
            }

            if (!long.TryParse(args[2], out long downloadLimit))
            {
                return $"Error: Invalid download limit: {args[2]}";
            }

            if (!long.TryParse(args[3], out long uploadLimit))
            {
                return $"Error: Invalid upload limit: {args[3]}";
            }

            try
            {
                // Convert KB/s to bytes/s
                downloadLimit *= 1024;
                uploadLimit *= 1024;

                var rule = new BandwidthRule
                {
                    ProcessId = pid,
                    DownloadLimit = downloadLimit,
                    UploadLimit = uploadLimit,
                    IsEnabled = true,
                    Priority = BandwidthPriority.Normal
                };

                _bandwidthLimiterService.SetProcessLimit(pid, downloadLimit, uploadLimit, rule.Priority);
                _databaseService.SaveBandwidthRule(rule);

                return $"Successfully set limits for process {pid}: " +
                       $"Download: {FormatBytes(downloadLimit)}/s, Upload: {FormatBytes(uploadLimit)}/s";
            }
            catch (Exception ex)
            {
                return $"Error setting limits: {ex.Message}";
            }
        }

        private string RemoveLimit(string[] args)
        {
            if (args.Length < 2)
            {
                return "Error: remove-limit requires 1 parameter: <pid>";
            }

            if (!int.TryParse(args[1], out int pid))
            {
                return $"Error: Invalid process ID: {args[1]}";
            }

            try
            {
                _bandwidthLimiterService.RemoveProcessLimit(pid);
                _databaseService.RemoveBandwidthRule(pid);
                return $"Successfully removed limits for process {pid}";
            }
            catch (Exception ex)
            {
                return $"Error removing limits: {ex.Message}";
            }
        }

        private string GetStats(string[] args)
        {
            var outputJson = args.Contains("--output") && args.Contains("json");
            
            if (args.Length > 1 && int.TryParse(args[1], out int pid))
            {
                // Get stats for specific process
                var process = _networkMonitorService.ActiveProcesses.FirstOrDefault(p => p.ProcessId == pid);
                if (process == null)
                {
                    return $"Process with PID {pid} not found";
                }

                if (outputJson)
                {
                    var stats = new
                    {
                        ProcessId = process.ProcessId,
                        ProcessName = process.ProcessName,
                        TotalDownload = process.TotalDownload,
                        TotalUpload = process.TotalUpload,
                        CurrentDownloadSpeed = process.DownloadSpeed,
                        CurrentUploadSpeed = process.UploadSpeed,
                        ConnectionCount = process.ConnectionCount
                    };
                    return JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });
                }
                else
                {
                    return $"Statistics for {process.ProcessName} (PID: {process.ProcessId}):\n" +
                           $"Total Downloaded: {FormatBytes(process.TotalDownload)}\n" +
                           $"Total Uploaded: {FormatBytes(process.TotalUpload)}\n" +
                           $"Current Download Speed: {FormatBytes(process.DownloadSpeed)}/s\n" +
                           $"Current Upload Speed: {FormatBytes(process.UploadSpeed)}/s\n" +
                           $"Active Connections: {process.ConnectionCount}";
                }
            }
            else
            {
                // Get overall stats
                var processes = _networkMonitorService.ActiveProcesses;
                var totalDownload = processes.Sum(p => p.TotalDownload);
                var totalUpload = processes.Sum(p => p.TotalUpload);
                var totalDownloadSpeed = processes.Sum(p => p.DownloadSpeed);
                var totalUploadSpeed = processes.Sum(p => p.UploadSpeed);
                var activeProcesses = processes.Count(p => p.DownloadSpeed > 0 || p.UploadSpeed > 0);

                if (outputJson)
                {
                    var stats = new
                    {
                        TotalDownload = totalDownload,
                        TotalUpload = totalUpload,
                        CurrentDownloadSpeed = totalDownloadSpeed,
                        CurrentUploadSpeed = totalUploadSpeed,
                        ActiveProcessCount = activeProcesses,
                        TotalProcessCount = processes.Count
                    };
                    return JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });
                }
                else
                {
                    return $"Overall Network Statistics:\n" +
                           $"Total Downloaded: {FormatBytes(totalDownload)}\n" +
                           $"Total Uploaded: {FormatBytes(totalUpload)}\n" +
                           $"Current Download Speed: {FormatBytes(totalDownloadSpeed)}/s\n" +
                           $"Current Upload Speed: {FormatBytes(totalUploadSpeed)}/s\n" +
                           $"Active Processes: {activeProcesses}\n" +
                           $"Total Processes: {processes.Count}";
                }
            }
        }

        private string ListProfiles(string[] args)
        {
            var outputJson = args.Contains("--output") && args.Contains("json");
            var profiles = _profileService.GetAllProfiles();

            if (outputJson)
            {
                return JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
            }
            else
            {
                var result = "Available Profiles:\n";
                result += "Name\t\tDescription\n";
                result += new string('-', 40) + "\n";
                
                foreach (var profile in profiles)
                {
                    result += $"{profile.Name.PadRight(15)}\t{profile.Description}\n";
                }
                
                return result;
            }
        }

        private string ApplyProfile(string[] args)
        {
            if (args.Length < 2)
            {
                return "Error: apply-profile requires 1 parameter: <profile_name>";
            }

            var profileName = args[1];
            
            try
            {
                _profileService.ApplyProfile(profileName);
                return $"Successfully applied profile: {profileName}";
            }
            catch (Exception ex)
            {
                return $"Error applying profile: {ex.Message}";
            }
        }

        private string GetVersion()
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            return $"NetLimiter Clone v{version}\n" +
                   $"Built on .NET 7.0\n" +
                   $"Copyright © 2025";
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
    }
}