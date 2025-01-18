using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace EBDC.Models
{
    public enum DownloadStatus
    {
        Ready,
        Queued,
        Downloading,
        Paused,
        Completed,
        Failed,
        Cancelled,
        Error
    }

    public class VideoInfo : INotifyPropertyChanged
    {
        private string _url = string.Empty;
        private string _title = string.Empty;
        private string _duration = string.Empty;
        private string _thumbnailUrl = string.Empty;
        private DownloadStatus _status = DownloadStatus.Ready;
        private double _progress;
        private bool _isSelected;
        private bool _isDownloading;
        private string _format = string.Empty;
        private string _quality = string.Empty;

        public string Url
        {
            get => _url;
            set => SetProperty(ref _url, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Duration
        {
            get => _duration;
            set => SetProperty(ref _duration, value);
        }

        public string ThumbnailUrl
        {
            get => _thumbnailUrl;
            set => SetProperty(ref _thumbnailUrl, value);
        }

        public DownloadStatus Status
        {
            get => _status;
            set
            {
                if (SetProperty(ref _status, value))
                {
                    IsDownloading = value == DownloadStatus.Downloading;
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        public string StatusText
        {
            get
            {
                return Status switch
                {
                    DownloadStatus.Ready => "Pronto",
                    DownloadStatus.Downloading => $"Download in corso ({Progress:F1}%)",
                    DownloadStatus.Completed => "Completato",
                    DownloadStatus.Error => "Errore",
                    DownloadStatus.Cancelled => "Annullato",
                    DownloadStatus.Paused => "In pausa",
                    _ => string.Empty
                };
            }
        }

        public double Progress
        {
            get => _progress;
            set
            {
                if (SetProperty(ref _progress, value))
                {
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public bool IsDownloading
        {
            get => _isDownloading;
            set => SetProperty(ref _isDownloading, value);
        }

        public string Format
        {
            get => _format;
            set => SetProperty(ref _format, value);
        }

        public string Quality
        {
            get => _quality;
            set => SetProperty(ref _quality, value);
        }

        public ICommand? DownloadCommand { get; set; }
        public ICommand? CancelCommand { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
