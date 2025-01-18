using System.Text.Json;
using System.IO;
using Serilog;

namespace EBDC.Services
{
    public class SettingsService
    {
        private readonly string _settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EBDC",
            "settings.json"
        );
        private readonly ILogger _logger;

        public Settings CurrentSettings { get; private set; }

        public SettingsService(ILogger logger)
        {
            _logger = logger;
            CurrentSettings = LoadSettings();
        }

        private Settings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    var settings = JsonSerializer.Deserialize<Settings>(json);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Errore durante il caricamento delle impostazioni");
            }

            return new Settings
            {
                DownloadPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                    "EBDC Downloads"
                )
            };
        }

        public void SaveSettings()
        {
            try
            {
                var directory = Path.GetDirectoryName(_settingsPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(CurrentSettings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(_settingsPath, json);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Errore durante il salvataggio delle impostazioni");
            }
        }

        public void UpdateDownloadPath(string path)
        {
            CurrentSettings.DownloadPath = path;
            SaveSettings();
        }
    }

    public class Settings
    {
        public string DownloadPath { get; set; } = string.Empty;
    }
}
