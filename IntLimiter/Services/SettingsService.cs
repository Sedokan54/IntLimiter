using System;
using System.IO;
using System.Text.Json;
using NetLimiterClone.Models;

namespace NetLimiterClone.Services
{
    public class SettingsService
    {
        private readonly string _settingsFilePath;
        private AppSettings _currentSettings;

        public AppSettings CurrentSettings => _currentSettings;

        public event EventHandler<AppSettings>? SettingsChanged;

        public SettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "NetLimiterClone");
            
            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);

            _settingsFilePath = Path.Combine(appFolder, "settings.json");
            _currentSettings = LoadSettings();
        }

        private AppSettings LoadSettings()
        {
            try
            {
                if (!File.Exists(_settingsFilePath))
                {
                    var defaultSettings = new AppSettings();
                    SaveSettings(defaultSettings);
                    return defaultSettings;
                }

                var json = File.ReadAllText(_settingsFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };

                var settings = JsonSerializer.Deserialize<AppSettings>(json, options);
                return settings ?? new AppSettings();
            }
            catch (Exception)
            {
                // If loading fails, return default settings
                return new AppSettings();
            }
        }

        public void SaveSettings(AppSettings settings)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(_settingsFilePath, json);

                _currentSettings = settings.Clone();
                SettingsChanged?.Invoke(this, _currentSettings);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save settings: {ex.Message}", ex);
            }
        }

        public void UpdateSettings(Action<AppSettings> updateAction)
        {
            var settingsCopy = _currentSettings.Clone();
            updateAction(settingsCopy);
            SaveSettings(settingsCopy);
        }

        public void ResetToDefaults()
        {
            SaveSettings(new AppSettings());
        }

        public void ExportSettings(string filePath)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(_currentSettings, options);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to export settings: {ex.Message}", ex);
            }
        }

        public void ImportSettings(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("Settings file not found.");

                var json = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };

                var settings = JsonSerializer.Deserialize<AppSettings>(json, options);
                if (settings != null)
                {
                    SaveSettings(settings);
                }
                else
                {
                    throw new InvalidOperationException("Invalid settings file format.");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to import settings: {ex.Message}", ex);
            }
        }

        public string GetSettingsFilePath()
        {
            return _settingsFilePath;
        }

        public bool BackupSettings()
        {
            try
            {
                var backupPath = _settingsFilePath + ".backup";
                File.Copy(_settingsFilePath, backupPath, true);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool RestoreFromBackup()
        {
            try
            {
                var backupPath = _settingsFilePath + ".backup";
                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, _settingsFilePath, true);
                    _currentSettings = LoadSettings();
                    SettingsChanged?.Invoke(this, _currentSettings);
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}