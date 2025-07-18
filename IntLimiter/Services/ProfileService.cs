using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NetLimiterClone.Models;

namespace NetLimiterClone.Services
{
    public class ProfileService
    {
        private readonly string _profilesFilePath;
        private readonly BandwidthLimiterService _bandwidthLimiterService;
        private readonly List<BandwidthProfile> _profiles = new();
        private BandwidthProfile? _activeProfile;

        public event EventHandler<ProfileChangedEventArgs>? ProfileChanged;
        public event EventHandler<ProfilesUpdatedEventArgs>? ProfilesUpdated;
        public event EventHandler? ProfilesChanged;

        public ProfileService(BandwidthLimiterService bandwidthLimiterService)
        {
            _bandwidthLimiterService = bandwidthLimiterService;
            
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configDir = Path.Combine(appDataPath, "NetLimiterClone");
            Directory.CreateDirectory(configDir);
            _profilesFilePath = Path.Combine(configDir, "profiles.json");
            
            LoadProfiles();
            InitializeDefaultProfiles();
        }

        public IReadOnlyList<BandwidthProfile> Profiles => _profiles.AsReadOnly();
        public BandwidthProfile? ActiveProfile => _activeProfile;

        public async Task<bool> CreateProfileAsync(string name, string description = "")
        {
            try
            {
                if (_profiles.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    return false; // Profile already exists
                }

                var profile = new BandwidthProfile(name, description);
                _profiles.Add(profile);
                
                await SaveProfilesAsync();
                ProfilesUpdated?.Invoke(this, new ProfilesUpdatedEventArgs(_profiles));
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating profile: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteProfileAsync(string name)
        {
            try
            {
                var profile = _profiles.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (profile == null || profile.IsDefault)
                {
                    return false; // Profile not found or is default
                }

                if (profile.IsActive)
                {
                    await DeactivateProfileAsync();
                }

                _profiles.Remove(profile);
                await SaveProfilesAsync();
                ProfilesUpdated?.Invoke(this, new ProfilesUpdatedEventArgs(_profiles));
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting profile: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ActivateProfileAsync(string name)
        {
            try
            {
                var profile = _profiles.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (profile == null)
                {
                    return false;
                }

                // Deactivate current profile
                if (_activeProfile != null)
                {
                    _activeProfile.IsActive = false;
                }

                // Activate new profile
                _activeProfile = profile;
                _activeProfile.IsActive = true;
                _activeProfile.UpdateLastUsed();

                // Apply profile rules
                await ApplyProfileRulesAsync(profile);

                await SaveProfilesAsync();
                ProfileChanged?.Invoke(this, new ProfileChangedEventArgs(profile, null));
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error activating profile: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeactivateProfileAsync()
        {
            try
            {
                if (_activeProfile == null)
                {
                    return false;
                }

                var previousProfile = _activeProfile;
                
                // Clear all bandwidth rules
                _bandwidthLimiterService.ClearAllLimits();
                
                _activeProfile.IsActive = false;
                _activeProfile = null;

                await SaveProfilesAsync();
                ProfileChanged?.Invoke(this, new ProfileChangedEventArgs(null, previousProfile));
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deactivating profile: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateProfileAsync(BandwidthProfile profile)
        {
            try
            {
                var existingProfile = _profiles.FirstOrDefault(p => p.Name.Equals(profile.Name, StringComparison.OrdinalIgnoreCase));
                if (existingProfile == null)
                {
                    return false;
                }

                // Update profile properties
                existingProfile.Description = profile.Description;
                existingProfile.GlobalSettings = profile.GlobalSettings.Clone();
                existingProfile.Rules.Clear();
                existingProfile.Rules.AddRange(profile.Rules);

                // If this is the active profile, reapply rules
                if (existingProfile.IsActive)
                {
                    await ApplyProfileRulesAsync(existingProfile);
                }

                await SaveProfilesAsync();
                ProfilesUpdated?.Invoke(this, new ProfilesUpdatedEventArgs(_profiles));
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating profile: {ex.Message}");
                return false;
            }
        }

        public BandwidthProfile? GetProfile(string name)
        {
            return _profiles.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<bool> SaveCurrentStateAsProfileAsync(string name, string description = "")
        {
            try
            {
                if (_profiles.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    return false; // Profile already exists
                }

                var profile = new BandwidthProfile(name, description);
                
                // Get current bandwidth rules
                var currentRules = _bandwidthLimiterService.GetAllLimits();
                foreach (var rule in currentRules)
                {
                    profile.AddRule(rule);
                }

                _profiles.Add(profile);
                await SaveProfilesAsync();
                ProfilesUpdated?.Invoke(this, new ProfilesUpdatedEventArgs(_profiles));
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving current state as profile: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ExportProfileAsync(string name, string filePath)
        {
            try
            {
                var profile = GetProfile(name);
                if (profile == null)
                {
                    return false;
                }

                var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, json);
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exporting profile: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ImportProfileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return false;
                }

                var json = await File.ReadAllTextAsync(filePath);
                var profile = JsonSerializer.Deserialize<BandwidthProfile>(json);
                
                if (profile == null)
                {
                    return false;
                }

                // Ensure unique name
                var originalName = profile.Name;
                var counter = 1;
                while (_profiles.Any(p => p.Name.Equals(profile.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    profile.Name = $"{originalName} ({counter})";
                    counter++;
                }

                profile.CreatedAt = DateTime.Now;
                profile.IsActive = false;
                profile.IsDefault = false;

                _profiles.Add(profile);
                await SaveProfilesAsync();
                ProfilesUpdated?.Invoke(this, new ProfilesUpdatedEventArgs(_profiles));
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error importing profile: {ex.Message}");
                return false;
            }
        }

        private async Task ApplyProfileRulesAsync(BandwidthProfile profile)
        {
            // Clear existing rules
            _bandwidthLimiterService.ClearAllLimits();

            // Apply profile rules
            foreach (var rule in profile.Rules.Where(r => r.IsEnabled))
            {
                _bandwidthLimiterService.SetProcessLimit(
                    rule.ProcessId,
                    rule.DownloadLimit,
                    rule.UploadLimit,
                    rule.Priority);
            }

            // Apply global settings if enabled
            if (profile.GlobalSettings.EnableGlobalDownloadLimit || profile.GlobalSettings.EnableGlobalUploadLimit)
            {
                // This would be implemented in the bandwidth limiter service
                // For now, we'll just log it
                System.Diagnostics.Debug.WriteLine($"Global limits: DL={profile.GlobalSettings.GlobalDownloadLimit}, UL={profile.GlobalSettings.GlobalUploadLimit}");
            }
        }

        private void InitializeDefaultProfiles()
        {
            if (!_profiles.Any())
            {
                var defaultProfiles = new[]
                {
                    DefaultProfiles.CreateGamingProfile(),
                    DefaultProfiles.CreateWorkProfile(),
                    DefaultProfiles.CreateStreamingProfile(),
                    DefaultProfiles.CreateUnlimitedProfile()
                };

                foreach (var profile in defaultProfiles)
                {
                    profile.IsDefault = true;
                    _profiles.Add(profile);
                }

                SaveProfilesAsync().Wait();
            }
        }

        private async Task LoadProfiles()
        {
            try
            {
                if (File.Exists(_profilesFilePath))
                {
                    var json = await File.ReadAllTextAsync(_profilesFilePath);
                    var profiles = JsonSerializer.Deserialize<List<BandwidthProfile>>(json);
                    
                    if (profiles != null)
                    {
                        _profiles.Clear();
                        _profiles.AddRange(profiles);
                        
                        // Find active profile
                        _activeProfile = _profiles.FirstOrDefault(p => p.IsActive);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading profiles: {ex.Message}");
            }
        }

        private async Task SaveProfilesAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_profiles, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_profilesFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving profiles: {ex.Message}");
            }
        }

        public void Dispose()
        {
            // Clean up if needed
        }
    }

    public class ProfileChangedEventArgs : EventArgs
    {
        public BandwidthProfile? NewProfile { get; }
        public BandwidthProfile? PreviousProfile { get; }

        public ProfileChangedEventArgs(BandwidthProfile? newProfile, BandwidthProfile? previousProfile)
        {
            NewProfile = newProfile;
            PreviousProfile = previousProfile;
        }
    }

    public class ProfilesUpdatedEventArgs : EventArgs
    {
        public IReadOnlyList<BandwidthProfile> Profiles { get; }

        public ProfilesUpdatedEventArgs(IReadOnlyList<BandwidthProfile> profiles)
        {
            Profiles = profiles;
        }
    }
}