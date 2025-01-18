using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using EBDC.Models;

namespace EBDC.Services
{
    public class UpdateService
    {
        private readonly ILogger<UpdateService> _logger;
        private readonly string _currentVersion;
        private readonly string _updateUrl = "https://api.github.com/repos/SARABANDER/EBDC/releases/latest";
        private readonly HttpClient _httpClient;

        public event Action<string>? UpdateAvailable;
        public event Action<double>? DownloadProgressChanged;
        public event Action<bool, string>? UpdateCompleted;

        public UpdateService(ILogger<UpdateService> logger)
        {
            _logger = logger;
            _currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "EBDC-UpdateCheck");
        }

        public async Task CheckForUpdatesAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(_updateUrl);
                var releaseInfo = JsonSerializer.Deserialize<GithubReleaseInfo>(response);

                if (releaseInfo != null && IsNewerVersion(releaseInfo.TagName))
                {
                    _logger.LogInformation($"Nuovo aggiornamento disponibile: {releaseInfo.TagName}");
                    UpdateAvailable?.Invoke(releaseInfo.TagName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il controllo degli aggiornamenti");
            }
        }

        public async Task DownloadAndInstallUpdateAsync(string version)
        {
            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), "EBDC_Update");
                Directory.CreateDirectory(tempPath);

                var updateZipPath = Path.Combine(tempPath, "update.zip");
                var downloadUrl = $"https://github.com/SARABANDER/EBDC/releases/download/{version}/EBDC.zip";

                using (var response = await _httpClient.GetStreamAsync(downloadUrl))
                using (var fileStream = File.Create(updateZipPath))
                {
                    var buffer = new byte[8192];
                    int bytesRead;
                    long totalBytesRead = 0;
                    var contentLength = response.Length;

                    while ((bytesRead = await response.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;
                        var progress = (double)totalBytesRead / contentLength * 100;
                        DownloadProgressChanged?.Invoke(progress);
                    }
                }

                // Creiamo un batch file per completare l'aggiornamento
                var batchPath = Path.Combine(tempPath, "update.bat");
                var appPath = AppDomain.CurrentDomain.BaseDirectory;
                var updateScript = $@"@echo off
timeout /t 2 /nobreak
taskkill /F /IM EBDC.exe
timeout /t 1 /nobreak
powershell Expand-Archive -Path ""{updateZipPath}"" -DestinationPath ""{appPath}"" -Force
start """" ""{Path.Combine(appPath, "EBDC.exe")}""
del ""{updateZipPath}""
del ""%~f0""
exit";

                await File.WriteAllTextAsync(batchPath, updateScript);

                // Avviamo il processo di aggiornamento
                Process.Start(new ProcessStartInfo
                {
                    FileName = batchPath,
                    UseShellExecute = true,
                    CreateNoWindow = true
                });

                UpdateCompleted?.Invoke(true, "Aggiornamento completato con successo!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'aggiornamento");
                UpdateCompleted?.Invoke(false, $"Errore durante l'aggiornamento: {ex.Message}");
            }
        }

        private bool IsNewerVersion(string newVersion)
        {
            // Rimuoviamo il 'v' iniziale se presente
            newVersion = newVersion.TrimStart('v');
            
            if (Version.TryParse(_currentVersion, out Version? currentVer) && 
                Version.TryParse(newVersion, out Version? newVer))
            {
                return newVer > currentVer;
            }
            
            return false;
        }
    }
}
