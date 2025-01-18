using System;

namespace EBDC.Services.Events
{
    public class DownloadProgressEventArgs : EventArgs
    {
        public string Url { get; }
        public double Progress { get; }

        public DownloadProgressEventArgs(string url, double progress)
        {
            Url = url;
            Progress = progress;
        }
    }

    public class DownloadCompletedEventArgs : EventArgs
    {
        public string Url { get; }

        public DownloadCompletedEventArgs(string url)
        {
            Url = url;
        }
    }

    public class DownloadFailedEventArgs : EventArgs
    {
        public string Url { get; }
        public string Error { get; }

        public DownloadFailedEventArgs(string url, string error)
        {
            Url = url;
            Error = error;
        }
    }

    public class DownloadPausedEventArgs : EventArgs
    {
        public string Url { get; }

        public DownloadPausedEventArgs(string url)
        {
            Url = url;
        }
    }

    public class DownloadResumedEventArgs : EventArgs
    {
        public string Url { get; }

        public DownloadResumedEventArgs(string url)
        {
            Url = url;
        }
    }

    public class ResourcesUpdatedEventArgs : EventArgs
    {
        public double CpuUsage { get; }
        public double MemoryUsageMB { get; }
        public double NetworkSpeedMbps { get; }

        public ResourcesUpdatedEventArgs(double cpuUsage, double memoryUsageMB, double networkSpeedMbps)
        {
            CpuUsage = cpuUsage;
            MemoryUsageMB = memoryUsageMB;
            NetworkSpeedMbps = networkSpeedMbps;
        }
    }
}
