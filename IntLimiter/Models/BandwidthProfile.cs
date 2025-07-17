using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NetLimiterClone.Models
{
    public class BandwidthProfile : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _description = string.Empty;
        private bool _isActive = false;
        private bool _isDefault = false;
        private DateTime _createdAt = DateTime.Now;
        private DateTime _lastUsed = DateTime.Now;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public bool IsDefault
        {
            get => _isDefault;
            set => SetProperty(ref _isDefault, value);
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        public DateTime LastUsed
        {
            get => _lastUsed;
            set => SetProperty(ref _lastUsed, value);
        }

        public List<BandwidthRule> Rules { get; set; } = new();
        
        public ProfileSettings GlobalSettings { get; set; } = new();

        // Calculated properties
        public int RuleCount => Rules.Count;
        public string LastUsedFormatted => LastUsed.ToString("yyyy-MM-dd HH:mm");
        public string CreatedAtFormatted => CreatedAt.ToString("yyyy-MM-dd HH:mm");

        public BandwidthProfile()
        {
        }

        public BandwidthProfile(string name, string description = "")
        {
            Name = name;
            Description = description;
        }

        public void AddRule(BandwidthRule rule)
        {
            if (!Rules.Contains(rule))
            {
                Rules.Add(rule);
                OnPropertyChanged(nameof(RuleCount));
            }
        }

        public void RemoveRule(BandwidthRule rule)
        {
            if (Rules.Remove(rule))
            {
                OnPropertyChanged(nameof(RuleCount));
            }
        }

        public void ClearRules()
        {
            Rules.Clear();
            OnPropertyChanged(nameof(RuleCount));
        }

        public BandwidthProfile Clone()
        {
            var clone = new BandwidthProfile
            {
                Name = Name + " (Copy)",
                Description = Description,
                IsActive = false,
                IsDefault = false,
                CreatedAt = DateTime.Now,
                LastUsed = DateTime.Now,
                GlobalSettings = GlobalSettings.Clone()
            };

            foreach (var rule in Rules)
            {
                clone.Rules.Add(new BandwidthRule(rule.ProcessId, rule.ProcessName, rule.ProcessPath)
                {
                    DownloadLimit = rule.DownloadLimit,
                    UploadLimit = rule.UploadLimit,
                    IsEnabled = rule.IsEnabled,
                    Priority = rule.Priority
                });
            }

            return clone;
        }

        public void UpdateLastUsed()
        {
            LastUsed = DateTime.Now;
            OnPropertyChanged(nameof(LastUsedFormatted));
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

    public class ProfileSettings
    {
        public bool EnableGlobalDownloadLimit { get; set; } = false;
        public bool EnableGlobalUploadLimit { get; set; } = false;
        public long GlobalDownloadLimit { get; set; } = 0;
        public long GlobalUploadLimit { get; set; } = 0;
        public bool BlockUnlistedProcesses { get; set; } = false;
        public bool ShowNotifications { get; set; } = true;
        public bool AutoApplyToNewProcesses { get; set; } = false;
        public int Priority { get; set; } = 0; // 0=Normal, 1=High, 2=Critical

        public ProfileSettings Clone()
        {
            return new ProfileSettings
            {
                EnableGlobalDownloadLimit = EnableGlobalDownloadLimit,
                EnableGlobalUploadLimit = EnableGlobalUploadLimit,
                GlobalDownloadLimit = GlobalDownloadLimit,
                GlobalUploadLimit = GlobalUploadLimit,
                BlockUnlistedProcesses = BlockUnlistedProcesses,
                ShowNotifications = ShowNotifications,
                AutoApplyToNewProcesses = AutoApplyToNewProcesses,
                Priority = Priority
            };
        }
    }

    public static class DefaultProfiles
    {
        public static BandwidthProfile CreateGamingProfile()
        {
            var profile = new BandwidthProfile("Gaming", "Optimized for gaming - prioritizes game traffic")
            {
                GlobalSettings = new ProfileSettings
                {
                    EnableGlobalDownloadLimit = true,
                    EnableGlobalUploadLimit = true,
                    GlobalDownloadLimit = 50 * 1024 * 1024, // 50 Mbps
                    GlobalUploadLimit = 10 * 1024 * 1024,   // 10 Mbps
                    ShowNotifications = false,
                    Priority = 1
                }
            };

            // Add some gaming-specific rules
            profile.Rules.Add(new BandwidthRule(0, "steam", "")
            {
                DownloadLimit = 0, // No limit for Steam
                UploadLimit = 0,
                Priority = BandwidthPriority.High,
                IsEnabled = true
            });

            profile.Rules.Add(new BandwidthRule(0, "discord", "")
            {
                DownloadLimit = 5 * 1024 * 1024, // 5 Mbps
                UploadLimit = 1 * 1024 * 1024,   // 1 Mbps
                Priority = BandwidthPriority.High,
                IsEnabled = true
            });

            return profile;
        }

        public static BandwidthProfile CreateWorkProfile()
        {
            var profile = new BandwidthProfile("Work", "Optimized for work - limits entertainment apps")
            {
                GlobalSettings = new ProfileSettings
                {
                    EnableGlobalDownloadLimit = true,
                    EnableGlobalUploadLimit = true,
                    GlobalDownloadLimit = 100 * 1024 * 1024, // 100 Mbps
                    GlobalUploadLimit = 20 * 1024 * 1024,    // 20 Mbps
                    ShowNotifications = true,
                    Priority = 0
                }
            };

            // Limit entertainment apps
            profile.Rules.Add(new BandwidthRule(0, "chrome", "")
            {
                DownloadLimit = 20 * 1024 * 1024, // 20 Mbps
                UploadLimit = 5 * 1024 * 1024,    // 5 Mbps
                Priority = BandwidthPriority.Normal,
                IsEnabled = true
            });

            profile.Rules.Add(new BandwidthRule(0, "spotify", "")
            {
                DownloadLimit = 1 * 1024 * 1024,  // 1 Mbps
                UploadLimit = 512 * 1024,         // 512 Kbps
                Priority = BandwidthPriority.Low,
                IsEnabled = true
            });

            return profile;
        }

        public static BandwidthProfile CreateStreamingProfile()
        {
            var profile = new BandwidthProfile("Streaming", "Optimized for streaming - prioritizes video apps")
            {
                GlobalSettings = new ProfileSettings
                {
                    EnableGlobalDownloadLimit = false,
                    EnableGlobalUploadLimit = true,
                    GlobalUploadLimit = 5 * 1024 * 1024, // 5 Mbps for uploads
                    ShowNotifications = false,
                    Priority = 1
                }
            };

            // Prioritize streaming apps
            profile.Rules.Add(new BandwidthRule(0, "netflix", "")
            {
                DownloadLimit = 0, // No limit
                UploadLimit = 0,
                Priority = BandwidthPriority.High,
                IsEnabled = true
            });

            profile.Rules.Add(new BandwidthRule(0, "youtube", "")
            {
                DownloadLimit = 0, // No limit
                UploadLimit = 0,
                Priority = BandwidthPriority.High,
                IsEnabled = true
            });

            return profile;
        }

        public static BandwidthProfile CreateUnlimitedProfile()
        {
            var profile = new BandwidthProfile("Unlimited", "No bandwidth limits - full speed")
            {
                GlobalSettings = new ProfileSettings
                {
                    EnableGlobalDownloadLimit = false,
                    EnableGlobalUploadLimit = false,
                    ShowNotifications = false,
                    Priority = 0
                }
            };

            return profile;
        }
    }
}