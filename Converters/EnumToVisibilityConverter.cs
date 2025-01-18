using System.Windows;
using System.Windows.Data;
using EBDC.Models;
using System.Linq;

namespace EBDC.Converters
{
    public class EnumToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is not DownloadStatus status || parameter is not string expectedStatus)
                return Visibility.Collapsed;

            var statusList = expectedStatus.Split('|');
            return statusList.Any(s => Enum.TryParse<DownloadStatus>(s, out var expected) && expected == status)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
