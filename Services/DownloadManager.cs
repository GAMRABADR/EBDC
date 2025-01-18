using System;
using System.Threading.Tasks;
using EBDC.Models;
using EBDC.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace EBDC.Services
{
    public class DownloadManager : IDownloadManager
    {
        private readonly IDownloader _downloader;
        private readonly ILogger<DownloadManager> _logger;

        public event Action<VideoInfo>? DownloadProgressChanged;
        public event Action<VideoInfo>? DownloadCompleted;
        public event Action<VideoInfo, Exception>? DownloadFailed;

        public DownloadManager(IDownloader downloader, ILogger<DownloadManager> logger)
        {
            _downloader = downloader;
            _logger = logger;

            _downloader.DownloadProgressChanged += video => DownloadProgressChanged?.Invoke(video);
            _downloader.DownloadCompleted += video => DownloadCompleted?.Invoke(video);
            _downloader.DownloadFailed += (video, ex) => DownloadFailed?.Invoke(video, ex);
        }

        public void SetDownloadPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Il percorso di download non può essere vuoto");

            _downloader.SetDownloadPath(path);
        }

        public async Task<VideoInfo> GetVideoInfoAsync(string url)
        {
            try
            {
                return await _downloader.GetVideoInfoAsync(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'ottenimento delle informazioni del video");
                throw;
            }
        }

        public async Task StartDownloadAsync(VideoInfo video)
        {
            try
            {
                await _downloader.StartDownloadAsync(video);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'avvio del download");
                throw;
            }
        }

        public void CancelDownload(string url)
        {
            try
            {
                _downloader.CancelDownload(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la cancellazione del download");
                throw;
            }
        }
    }
}
