using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace EBDC.Services
{
    public class PersistentSettingsService
    {
        private const string SettingsFileName = "settings.json";
        private string SettingsFilePath => Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            SettingsFileName);

        public class Settings
        {
            public string? DownloadPath { get; set; }
            public string LastSelectedFormat { get; set; } = "MP4 (Video)";
            public string LastSelectedQuality { get; set; } = "Migliore";
        }

        private Settings _currentSettings;

        public Settings CurrentSettings => _currentSettings;

        public PersistentSettingsService()
        {
            try
            {
                _currentSettings = LoadSettings();
            }
            catch
            {
                _currentSettings = new Settings();
            }
        }

        public void SaveSettings()
        {
            try
            {
                var json = JsonSerializer.Serialize(_currentSettings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Errore nel salvataggio delle impostazioni: {ex.Message}",
                    "Errore",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private Settings LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<Settings>(json);
                    if (settings != null)
                    {
                        settings.LastSelectedFormat ??= "MP4 (Video)";
                        settings.LastSelectedQuality ??= "Migliore";
                        return settings;
                    }
                }
            }
            catch
            {
                // In caso di errore, ritorna nuove impostazioni predefinite
            }
            return new Settings();
        }

        public void UpdateDownloadPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Il percorso di download non può essere vuoto.", nameof(path));

            _currentSettings.DownloadPath = path;
            SaveSettings();
        }

        public void UpdateLastSelectedFormat(string format)
        {
            if (string.IsNullOrEmpty(format))
                throw new ArgumentException("Il formato non può essere vuoto.", nameof(format));

            _currentSettings.LastSelectedFormat = format;
            SaveSettings();
        }

        public void UpdateLastSelectedQuality(string quality)
        {
            if (string.IsNullOrEmpty(quality))
                throw new ArgumentException("La qualità non può essere vuota.", nameof(quality));

            _currentSettings.LastSelectedQuality = quality;
            SaveSettings();
        }
    }
}
