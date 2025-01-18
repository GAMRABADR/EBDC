using System;
using System.Threading.Tasks;
using EBDC.Models;

namespace EBDC.Services.Interfaces
{
    public interface IDownloader
    {
        event Action<VideoInfo> DownloadProgressChanged;
        event Action<VideoInfo> DownloadCompleted;
        event Action<VideoInfo, Exception> DownloadFailed;

        Task<VideoInfo> GetVideoInfoAsync(string url);
        Task StartDownloadAsync(VideoInfo video);
        void CancelDownload(string url);
        void SetDownloadPath(string path);
    }
}
