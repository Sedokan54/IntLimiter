using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using NetLimiterClone.Models;

namespace NetLimiterClone.Services
{
    public class SettingsExportService
    {
        private readonly SettingsService _settingsService;
        private readonly ProfileService _profileService;
        private readonly DatabaseService _databaseService;
        private readonly ThemeService _themeService;

        public SettingsExportService()
        {
            _settingsService = new SettingsService();
            _profileService = new ProfileService(new BandwidthLimiterService());
            _databaseService = new DatabaseService();
            _themeService = new ThemeService();
        }

        public async Task<string> ExportSettingsAsync(string filePath)
        {
            try
            {
                var exportData = new SettingsExportData
                {
                    Version = "1.0",
                    ExportDate = DateTime.Now,
                    ApplicationSettings = _settingsService.CurrentSettings,
                    Profiles = _profileService.GetAllProfiles(),
                    BandwidthRules = await _databaseService.GetAllBandwidthRulesAsync(),
                    CurrentTheme = _themeService.CurrentTheme.ToString(),
                    ProcessGroups = await _databaseService.GetAllProcessGroupsAsync()
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var json = JsonSerializer.Serialize(exportData, options);
                await File.WriteAllTextAsync(filePath, json);

                return $"Settings exported successfully to: {filePath}";
            }
            catch (Exception ex)
            {
                return $"Export failed: {ex.Message}";
            }
        }

        public async Task<string> ImportSettingsAsync(string filePath, bool validateOnly = false)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return "Error: File not found";
                }

                var json = await File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };

                var importData = JsonSerializer.Deserialize<SettingsExportData>(json, options);
                
                if (importData == null)
                {
                    return "Error: Invalid or corrupted settings file";
                }

                // Validate the imported data
                var validationResult = ValidateImportData(importData);
                if (!validationResult.IsValid)
                {
                    return $"Validation failed: {validationResult.ErrorMessage}";
                }

                if (validateOnly)
                {
                    return "Validation successful. The settings file is valid.";
                }

                // Apply the imported settings
                await ApplyImportedSettingsAsync(importData);

                return $"Settings imported successfully from: {filePath}";
            }
            catch (JsonException ex)
            {
                return $"Invalid JSON format: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Import failed: {ex.Message}";
            }
        }

        private ValidationResult ValidateImportData(SettingsExportData importData)
        {
            // Check version compatibility
            if (string.IsNullOrEmpty(importData.Version))
            {
                return new ValidationResult(false, "Missing version information");
            }

            // Validate application settings
            if (importData.ApplicationSettings == null)
            {
                return new ValidationResult(false, "Missing application settings");
            }

            if (importData.ApplicationSettings.UpdateInterval < 100 || importData.ApplicationSettings.UpdateInterval > 10000)
            {
                return new ValidationResult(false, "Invalid update interval (must be between 100-10000ms)");
            }

            // Validate profiles
            if (importData.Profiles != null)
            {
                foreach (var profile in importData.Profiles)
                {
                    if (string.IsNullOrEmpty(profile.Name))
                    {
                        return new ValidationResult(false, "Profile missing name");
                    }

                    if (profile.Rules != null)
                    {
                        foreach (var rule in profile.Rules)
                        {
                            if (rule.DownloadLimit < 0 || rule.UploadLimit < 0)
                            {
                                return new ValidationResult(false, $"Invalid limits in profile '{profile.Name}'");
                            }
                        }
                    }
                }
            }

            // Validate bandwidth rules
            if (importData.BandwidthRules != null)
            {
                foreach (var rule in importData.BandwidthRules)
                {
                    if (rule.ProcessId <= 0)
                    {
                        return new ValidationResult(false, "Invalid process ID in bandwidth rule");
                    }
                    
                    if (rule.DownloadLimit < 0 || rule.UploadLimit < 0)
                    {
                        return new ValidationResult(false, "Invalid bandwidth limits");
                    }
                }
            }

            // Validate theme
            if (!string.IsNullOrEmpty(importData.CurrentTheme))
            {
                if (!Enum.TryParse<ThemeType>(importData.CurrentTheme, true, out _))
                {
                    return new ValidationResult(false, $"Invalid theme: {importData.CurrentTheme}");
                }
            }

            return new ValidationResult(true, "Validation successful");
        }

        private async Task ApplyImportedSettingsAsync(SettingsExportData importData)
        {
            // Apply application settings
            if (importData.ApplicationSettings != null)
            {
                _settingsService.UpdateSettings(settings => 
                {
                    // Copy properties from imported settings
                    // TODO: Implement proper settings copying
                });
            }

            // Apply theme
            if (!string.IsNullOrEmpty(importData.CurrentTheme) && 
                Enum.TryParse<ThemeType>(importData.CurrentTheme, true, out var theme))
            {
                _themeService.SetTheme(theme);
            }

            // Clear existing rules and profiles
            await _databaseService.ClearAllBandwidthRulesAsync();
            _profileService.ClearAllProfiles();

            // Apply profiles
            if (importData.Profiles != null)
            {
                foreach (var profile in importData.Profiles)
                {
                    _profileService.SaveProfile(profile);
                }
            }

            // Apply bandwidth rules
            if (importData.BandwidthRules != null)
            {
                foreach (var rule in importData.BandwidthRules)
                {
                    await _databaseService.SaveBandwidthRuleAsync(rule);
                }
            }

            // Apply process groups
            if (importData.ProcessGroups != null)
            {
                foreach (var group in importData.ProcessGroups)
                {
                    await _databaseService.SaveProcessGroupAsync(group);
                }
            }
        }

        public async Task<string> CreateBackupAsync()
        {
            try
            {
                var backupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                    "IntLimiter", "Backups");
                
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupPath = Path.Combine(backupDir, $"settings_backup_{timestamp}.json");

                return await ExportSettingsAsync(backupPath);
            }
            catch (Exception ex)
            {
                return $"Backup creation failed: {ex.Message}";
            }
        }

        public List<string> GetAvailableBackups()
        {
            try
            {
                var backupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                    "IntLimiter", "Backups");
                
                if (!Directory.Exists(backupDir))
                {
                    return new List<string>();
                }

                return Directory.GetFiles(backupDir, "settings_backup_*.json")
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        public async Task<string> RestoreBackupAsync(string backupPath)
        {
            return await ImportSettingsAsync(backupPath);
        }

        public async Task CleanupOldBackupsAsync(int keepDays = 30)
        {
            try
            {
                var backupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                    "IntLimiter", "Backups");
                
                if (!Directory.Exists(backupDir))
                {
                    return;
                }

                var cutoffDate = DateTime.Now.AddDays(-keepDays);
                var oldBackups = Directory.GetFiles(backupDir, "settings_backup_*.json")
                    .Where(f => File.GetCreationTime(f) < cutoffDate)
                    .ToList();

                foreach (var backup in oldBackups)
                {
                    File.Delete(backup);
                }
            }
            catch
            {
                // Silently ignore cleanup errors
            }
        }
    }

    public class SettingsExportData
    {
        public string Version { get; set; } = "1.0";
        public DateTime ExportDate { get; set; }
        public AppSettings? ApplicationSettings { get; set; }
        public List<BandwidthProfile>? Profiles { get; set; }
        public List<BandwidthRule>? BandwidthRules { get; set; }
        public string? CurrentTheme { get; set; }
        public List<ProcessGroup>? ProcessGroups { get; set; }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }

        public ValidationResult(bool isValid, string errorMessage)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }
    }

    public enum ThemeType
    {
        Light,
        Dark
    }
}