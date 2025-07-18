using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace NetLimiterClone.Services
{
    public class ThemeService
    {
        private const string THEME_CONFIG_FILE = "theme.json";
        private readonly string _configPath;
        private ThemeMode _currentTheme = ThemeMode.Dark;

        public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

        public ThemeService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configDir = Path.Combine(appDataPath, "NetLimiterClone");
            Directory.CreateDirectory(configDir);
            _configPath = Path.Combine(configDir, THEME_CONFIG_FILE);
            
            LoadThemePreference();
        }

        public ThemeMode CurrentTheme
        {
            get => _currentTheme;
            set
            {
                if (_currentTheme != value)
                {
                    var previousTheme = _currentTheme;
                    _currentTheme = value;
                    ApplyTheme(value);
                    SaveThemePreference();
                    ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(value, previousTheme));
                }
            }
        }

        public void ToggleTheme()
        {
            CurrentTheme = CurrentTheme == ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark;
        }

        public void ApplyTheme(ThemeMode theme)
        {
            try
            {
                var app = Application.Current;
                if (app == null) return;

                // Clear existing theme resources
                ClearThemeResources();

                // Apply new theme
                var themeUri = theme switch
                {
                    ThemeMode.Light => new Uri("pack://application:,,,/Resources/LightTheme.xaml"),
                    ThemeMode.Dark => new Uri("pack://application:,,,/Resources/DarkTheme.xaml"),
                    _ => new Uri("pack://application:,,,/Resources/DarkTheme.xaml")
                };

                var themeDict = new ResourceDictionary { Source = themeUri };
                app.Resources.MergedDictionaries.Add(themeDict);

                System.Diagnostics.Debug.WriteLine($"Applied {theme} theme");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying theme: {ex.Message}");
            }
        }

        private void ClearThemeResources()
        {
            try
            {
                var app = Application.Current;
                if (app == null) return;

                // Remove existing theme dictionaries
                var toRemove = new List<ResourceDictionary>();
                foreach (var dict in app.Resources.MergedDictionaries)
                {
                    if (dict.Source != null && 
                        (dict.Source.ToString().Contains("DarkTheme.xaml") || 
                         dict.Source.ToString().Contains("LightTheme.xaml")))
                    {
                        toRemove.Add(dict);
                    }
                }

                foreach (var dict in toRemove)
                {
                    app.Resources.MergedDictionaries.Remove(dict);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing theme resources: {ex.Message}");
            }
        }

        private void LoadThemePreference()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    var config = JsonSerializer.Deserialize<ThemeConfig>(json);
                    if (config != null && Enum.IsDefined(typeof(ThemeMode), config.Theme))
                    {
                        _currentTheme = config.Theme;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading theme preference: {ex.Message}");
            }

            // Apply the loaded theme
            ApplyTheme(_currentTheme);
        }

        private void SaveThemePreference()
        {
            try
            {
                var config = new ThemeConfig { Theme = _currentTheme };
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving theme preference: {ex.Message}");
            }
        }

        public string GetThemeDisplayName(ThemeMode theme)
        {
            return theme switch
            {
                ThemeMode.Light => "Light",
                ThemeMode.Dark => "Dark",
                _ => "Unknown"
            };
        }

        public ThemeMode[] GetAvailableThemes()
        {
            return Enum.GetValues<ThemeMode>();
        }

        public void SetTheme(NetLimiterClone.Models.ThemeMode themeMode)
        {
            var newTheme = themeMode switch
            {
                NetLimiterClone.Models.ThemeMode.Dark => ThemeMode.Dark,
                NetLimiterClone.Models.ThemeMode.Light => ThemeMode.Light,
                _ => ThemeMode.Light
            };
            CurrentTheme = newTheme;
        }
    }

    public enum ThemeMode
    {
        Light,
        Dark
    }

    public class ThemeChangedEventArgs : EventArgs
    {
        public ThemeMode NewTheme { get; }
        public ThemeMode PreviousTheme { get; }

        public ThemeChangedEventArgs(ThemeMode newTheme, ThemeMode previousTheme)
        {
            NewTheme = newTheme;
            PreviousTheme = previousTheme;
        }
    }

    internal class ThemeConfig
    {
        public ThemeMode Theme { get; set; } = ThemeMode.Dark;
    }
}