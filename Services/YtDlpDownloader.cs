using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EBDC.Models;
using EBDC.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace EBDC.Services
{
    public class YtDlpDownloader : IDownloader
    {
        private readonly ILogger<YtDlpDownloader> _logger;
        private readonly string _ytDlpPath;
        private string _downloadPath = string.Empty;
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _downloadTasks;
        private static readonly Regex ProgressRegex = new(@"\[download\]\s+(\d+\.?\d*)%", RegexOptions.Compiled);
        private static readonly Regex TitleRegex = new(@"(?:\[download\]|)\s*(?:Destination:|)(.+?)\s*$", RegexOptions.Compiled);
        private static readonly Regex DurationRegex = new(@"\[info\]\s+Duration:\s+(\d+:\d+)", RegexOptions.Compiled);

        public event Action<VideoInfo>? DownloadProgressChanged;
        public event Action<VideoInfo>? DownloadCompleted;
        public event Action<VideoInfo, Exception>? DownloadFailed;

        public YtDlpDownloader(ILogger<YtDlpDownloader> logger)
        {
            _logger = logger;
            _ytDlpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "yt-dlp.exe");
            _downloadTasks = new ConcurrentDictionary<string, CancellationTokenSource>();
        }

        public void SetDownloadPath(string path)
        {
            _downloadPath = path;
        }

        public async Task<VideoInfo> GetVideoInfoAsync(string url)
        {
            var videoInfo = new VideoInfo { Url = url };

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = _ytDlpPath,
                    Arguments = $"--no-playlist --get-title --get-duration \"{url}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length >= 1)
                    {
                        videoInfo.Title = lines[0].Trim();
                        if (lines.Length >= 2)
                        {
                            videoInfo.Duration = lines[1].Trim();
                        }
                    }
                }
                else
                {
                    throw new Exception("Impossibile ottenere le informazioni del video");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'estrazione delle informazioni del video");
                videoInfo.Status = DownloadStatus.Error;
            }

            return videoInfo;
        }

        public async Task StartDownloadAsync(VideoInfo video)
        {
            if (string.IsNullOrEmpty(_downloadPath))
                throw new InvalidOperationException("Il percorso di download non è stato impostato");

            var cts = new CancellationTokenSource();
            if (!_downloadTasks.TryAdd(video.Url, cts))
            {
                throw new InvalidOperationException("Download già in corso per questo URL");
            }

            try
            {
                var outputTemplate = Path.Combine(_downloadPath, "%(title)s.%(ext)s");
                var format = GetFormatArgument(video.Format, video.Quality);

                var startInfo = new ProcessStartInfo
                {
                    FileName = _ytDlpPath,
                    Arguments = $"{format} --no-playlist -o \"{outputTemplate}\" \"{video.Url}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                var progressBuffer = new System.Text.StringBuilder();
                
                process.OutputDataReceived += (sender, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;

                    var match = ProgressRegex.Match(e.Data);
                    if (match.Success && double.TryParse(match.Groups[1].Value, out double progress))
                    {
                        video.Progress = progress;
                        DownloadProgressChanged?.Invoke(video);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        _logger.LogWarning($"yt-dlp error output: {e.Data}");
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync(cts.Token);

                if (process.ExitCode == 0)
                {
                    video.Progress = 100;
                    video.Status = DownloadStatus.Completed;
                    DownloadCompleted?.Invoke(video);
                }
                else
                {
                    throw new Exception($"yt-dlp è terminato con codice di errore {process.ExitCode}");
                }
            }
            catch (OperationCanceledException)
            {
                video.Status = DownloadStatus.Cancelled;
                _logger.LogInformation($"Download cancellato per {video.Url}");
            }
            catch (Exception ex)
            {
                video.Status = DownloadStatus.Error;
                _logger.LogError(ex, $"Errore durante il download di {video.Url}");
                DownloadFailed?.Invoke(video, ex);
            }
            finally
            {
                _downloadTasks.TryRemove(video.Url, out _);
                cts.Dispose();
            }
        }

        public void CancelDownload(string url)
        {
            if (_downloadTasks.TryGetValue(url, out var cts))
            {
                cts.Cancel();
            }
        }

        private string GetFormatArgument(string format, string quality)
        {
            return format.ToLower() switch
            {
                "mp4 (video)" => GetVideoFormatString(quality),
                "mp3 (audio)" => "-x --audio-format mp3 --audio-quality 0",
                "wav (audio)" => "-x --audio-format wav --audio-quality 0 --postprocessor-args \"-acodec pcm_s16le\"",
                _ => throw new ArgumentException($"Formato non supportato: {format}")
            };
        }

        private string GetVideoFormatString(string quality)
        {
            return quality switch
            {
                "1080p" => "-f \"bestvideo[height<=1080]+bestaudio/best[height<=1080]\"",
                "720p" => "-f \"bestvideo[height<=720]+bestaudio/best[height<=720]\"",
                "480p" => "-f \"bestvideo[height<=480]+bestaudio/best[height<=480]\"",
                "360p" => "-f \"bestvideo[height<=360]+bestaudio/best[height<=360]\"",
                _ => "-f \"bestvideo+bestaudio/best\""
            };
        }
    }
}
