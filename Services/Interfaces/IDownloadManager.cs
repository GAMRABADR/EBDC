using System;
using System.Threading.Tasks;
using EBDC.Models;

namespace EBDC.Services.Interfaces
{
    public interface IDownloadManager
    {
        event Action<VideoInfo> DownloadProgressChanged;
        event Action<VideoInfo> DownloadCompleted;
        event Action<VideoInfo, Exception> DownloadFailed;

        void SetDownloadPath(string path);
        Task<VideoInfo> GetVideoInfoAsync(string url);
        Task StartDownloadAsync(VideoInfo video);
        void CancelDownload(string url);
    }
}
