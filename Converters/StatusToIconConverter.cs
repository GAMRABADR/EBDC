using System;
using System.Globalization;
using System.Windows.Data;
using EBDC.Models;
using MaterialDesignThemes.Wpf;

namespace EBDC.Converters
{
    public class StatusToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DownloadStatus status)
            {
                return status switch
                {
                    DownloadStatus.Downloading => PackIconKind.Pause,
                    DownloadStatus.Paused => PackIconKind.Play,
                    DownloadStatus.Failed => PackIconKind.AlertCircle,
                    DownloadStatus.Completed => PackIconKind.Check,
                    _ => PackIconKind.Download
                };
            }
            return PackIconKind.Download;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
