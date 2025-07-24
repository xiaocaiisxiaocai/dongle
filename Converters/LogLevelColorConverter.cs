using System;
using System.Globalization;
using System.Windows.Data;

namespace LicenseProtection.Views
{
    public class LogLevelColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string logEntry && parameter is string targetLevel)
            {
                return logEntry.Contains($"[{targetLevel.PadRight(8)}]");
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}