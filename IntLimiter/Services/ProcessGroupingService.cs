using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using NetLimiterClone.Models;

namespace NetLimiterClone.Services
{
    public class ProcessGroupingService
    {
        private readonly string _configFilePath;
        private readonly Dictionary<string, ProcessGroup> _groups = new();
        private readonly Dictionary<string, string> _applicationGroups = new();
        private ProcessGroupType _currentGroupType = ProcessGroupType.Application;

        public event EventHandler<ProcessGroupsChangedEventArgs>? ProcessGroupsChanged;

        public ProcessGroupingService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configDir = Path.Combine(appDataPath, "NetLimiterClone");
            Directory.CreateDirectory(configDir);
            _configFilePath = Path.Combine(configDir, "process_groups.json");
            
            LoadGroupConfiguration();
            InitializeDefaultGroups();
        }

        public ProcessGroupType CurrentGroupType
        {
            get => _currentGroupType;
            set
            {
                _currentGroupType = value;
                RebuildGroups();
            }
        }

        public ObservableCollection<ProcessGroup> GroupProcesses(IEnumerable<ProcessInfo> processes)
        {
            var processGroups = new ObservableCollection<ProcessGroup>();
            
            switch (_currentGroupType)
            {
                case ProcessGroupType.Application:
                    processGroups = GroupByApplication(processes);
                    break;
                case ProcessGroupType.Service:
                    processGroups = GroupByService(processes);
                    break;
                case ProcessGroupType.User:
                    processGroups = GroupByUser(processes);
                    break;
                case ProcessGroupType.Custom:
                    processGroups = GroupByCustomRules(processes);
                    break;
            }

            ProcessGroupsChanged?.Invoke(this, new ProcessGroupsChangedEventArgs(processGroups));
            return processGroups;
        }

        private ObservableCollection<ProcessGroup> GroupByApplication(IEnumerable<ProcessInfo> processes)
        {
            var groups = new Dictionary<string, ProcessGroup>();
            
            foreach (var process in processes)
            {
                var groupName = GetApplicationGroupName(process.ProcessName);
                
                if (!groups.ContainsKey(groupName))
                {
                    groups[groupName] = new ProcessGroup
                    {
                        GroupName = groupName,
                        GroupType = ProcessGroupType.Application,
                        GroupIcon = process.Icon // Use the first process icon as group icon
                    };
                }
                
                groups[groupName].AddProcess(process);
            }

            return new ObservableCollection<ProcessGroup>(groups.Values.OrderBy(g => g.GroupName));
        }

        private ObservableCollection<ProcessGroup> GroupByService(IEnumerable<ProcessInfo> processes)
        {
            var groups = new Dictionary<string, ProcessGroup>();
            var serviceProcesses = new[] { "svchost", "services", "lsass", "winlogon", "csrss" };
            
            foreach (var process in processes)
            {
                var groupName = process.IsSystemProcess ? "System Services" : 
                               serviceProcesses.Any(sp => process.ProcessName.Contains(sp, StringComparison.OrdinalIgnoreCase)) ? 
                               "Windows Services" : "User Applications";
                
                if (!groups.ContainsKey(groupName))
                {
                    groups[groupName] = new ProcessGroup
                    {
                        GroupName = groupName,
                        GroupType = ProcessGroupType.Service,
                        GroupIcon = process.Icon
                    };
                }
                
                groups[groupName].AddProcess(process);
            }

            return new ObservableCollection<ProcessGroup>(groups.Values.OrderBy(g => g.GroupName));
        }

        private ObservableCollection<ProcessGroup> GroupByUser(IEnumerable<ProcessInfo> processes)
        {
            var groups = new Dictionary<string, ProcessGroup>();
            
            foreach (var process in processes)
            {
                var groupName = GetProcessUser(process);
                
                if (!groups.ContainsKey(groupName))
                {
                    groups[groupName] = new ProcessGroup
                    {
                        GroupName = groupName,
                        GroupType = ProcessGroupType.User,
                        GroupIcon = process.Icon
                    };
                }
                
                groups[groupName].AddProcess(process);
            }

            return new ObservableCollection<ProcessGroup>(groups.Values.OrderBy(g => g.GroupName));
        }

        private ObservableCollection<ProcessGroup> GroupByCustomRules(IEnumerable<ProcessInfo> processes)
        {
            var groups = new Dictionary<string, ProcessGroup>();
            
            foreach (var process in processes)
            {
                var groupName = GetCustomGroupName(process.ProcessName);
                
                if (!groups.ContainsKey(groupName))
                {
                    groups[groupName] = new ProcessGroup
                    {
                        GroupName = groupName,
                        GroupType = ProcessGroupType.Custom,
                        GroupIcon = process.Icon
                    };
                }
                
                groups[groupName].AddProcess(process);
            }

            return new ObservableCollection<ProcessGroup>(groups.Values.OrderBy(g => g.GroupName));
        }

        private string GetApplicationGroupName(string processName)
        {
            var lowerName = processName.ToLower();
            
            // Check for known application groups
            if (_applicationGroups.ContainsKey(lowerName))
            {
                return _applicationGroups[lowerName];
            }
            
            // Common browser processes
            if (lowerName.Contains("chrome") || lowerName.Contains("chromium"))
                return "Google Chrome";
            if (lowerName.Contains("firefox"))
                return "Mozilla Firefox";
            if (lowerName.Contains("edge") || lowerName.Contains("msedge"))
                return "Microsoft Edge";
            if (lowerName.Contains("opera"))
                return "Opera";
            if (lowerName.Contains("brave"))
                return "Brave";

            // Office applications
            if (lowerName.Contains("winword") || lowerName.Contains("excel") || 
                lowerName.Contains("powerpoint") || lowerName.Contains("outlook"))
                return "Microsoft Office";

            // Development tools
            if (lowerName.Contains("devenv") || lowerName.Contains("code"))
                return "Development Tools";

            // Media players
            if (lowerName.Contains("vlc") || lowerName.Contains("wmplayer") || 
                lowerName.Contains("spotify") || lowerName.Contains("itunes"))
                return "Media Players";

            // Games
            if (lowerName.Contains("steam") || lowerName.Contains("game") || 
                lowerName.Contains("launcher"))
                return "Games";

            // Default: use process name as group name
            return processName;
        }

        private string GetProcessUser(ProcessInfo process)
        {
            // This is a simplified implementation
            // In a real application, you'd use WMI or Win32 API to get the actual user
            return process.IsSystemProcess ? "SYSTEM" : Environment.UserName;
        }

        private string GetCustomGroupName(string processName)
        {
            // Check custom grouping rules
            var lowerName = processName.ToLower();
            
            foreach (var kvp in _applicationGroups)
            {
                if (lowerName.Contains(kvp.Key))
                {
                    return kvp.Value;
                }
            }
            
            return "Other";
        }

        public void CreateCustomGroup(string groupName, string[] processNames)
        {
            foreach (var processName in processNames)
            {
                _applicationGroups[processName.ToLower()] = groupName;
            }
            
            SaveGroupConfiguration();
            RebuildGroups();
        }

        public void DeleteCustomGroup(string groupName)
        {
            var keysToRemove = _applicationGroups.Where(kvp => kvp.Value == groupName).Select(kvp => kvp.Key).ToList();
            foreach (var key in keysToRemove)
            {
                _applicationGroups.Remove(key);
            }
            
            SaveGroupConfiguration();
            RebuildGroups();
        }

        public void AddProcessToGroup(string processName, string groupName)
        {
            _applicationGroups[processName.ToLower()] = groupName;
            SaveGroupConfiguration();
            RebuildGroups();
        }

        public void RemoveProcessFromGroup(string processName)
        {
            _applicationGroups.Remove(processName.ToLower());
            SaveGroupConfiguration();
            RebuildGroups();
        }

        public Dictionary<string, string[]> GetCustomGroups()
        {
            return _applicationGroups.GroupBy(kvp => kvp.Value)
                .ToDictionary(g => g.Key, g => g.Select(kvp => kvp.Key).ToArray());
        }

        private void InitializeDefaultGroups()
        {
            if (!_applicationGroups.Any())
            {
                // Add some default groupings
                _applicationGroups["chrome"] = "Google Chrome";
                _applicationGroups["chromium"] = "Google Chrome";
                _applicationGroups["firefox"] = "Mozilla Firefox";
                _applicationGroups["msedge"] = "Microsoft Edge";
                _applicationGroups["opera"] = "Opera";
                _applicationGroups["brave"] = "Brave";
                _applicationGroups["winword"] = "Microsoft Office";
                _applicationGroups["excel"] = "Microsoft Office";
                _applicationGroups["powerpoint"] = "Microsoft Office";
                _applicationGroups["outlook"] = "Microsoft Office";
                _applicationGroups["steam"] = "Steam";
                _applicationGroups["discord"] = "Discord";
                _applicationGroups["spotify"] = "Spotify";
                _applicationGroups["vlc"] = "VLC Media Player";
                
                SaveGroupConfiguration();
            }
        }

        private void LoadGroupConfiguration()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var json = File.ReadAllText(_configFilePath);
                    var config = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (config != null)
                    {
                        _applicationGroups.Clear();
                        foreach (var kvp in config)
                        {
                            _applicationGroups[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading group configuration: {ex.Message}");
            }
        }

        private void SaveGroupConfiguration()
        {
            try
            {
                var json = JsonSerializer.Serialize(_applicationGroups, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving group configuration: {ex.Message}");
            }
        }

        private void RebuildGroups()
        {
            // This would be called when grouping type changes
            // The actual rebuild would be handled by the calling code
        }
    }

    public class ProcessGroupsChangedEventArgs : EventArgs
    {
        public ObservableCollection<ProcessGroup> Groups { get; }

        public ProcessGroupsChangedEventArgs(ObservableCollection<ProcessGroup> groups)
        {
            Groups = groups;
        }
    }
}