using System;
using System.Globalization;
using System.Windows.Data;
using EBDC.Models;

namespace EBDC.Converters
{
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DownloadStatus status && parameter is string parameterString)
            {
                if (Enum.TryParse<DownloadStatus>(parameterString, out var targetStatus))
                {
                    return status == targetStatus;
                }
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
