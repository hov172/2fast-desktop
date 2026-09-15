using System;
using System.Globalization;
#if WINDOWS_UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
#endif

namespace Project2FA.Converters
{
    public partial class DateTimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is DateTime dt ? dt.ToString(CultureInfo.CurrentCulture) : string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value == null || value.ToString().Trim().Length == 0)
            {
                return null;
            }
            var s = value.ToString();

            switch (s)
            {
                case "today":
                    return DateTime.Today;
                case "now":
                    return DateTime.Now;
                case "yesterday":
                    return DateTime.Today.AddDays(-1);
                case "tomorrow":
                    return DateTime.Today.AddDays(1);
            }

            return DateTime.TryParse(s, out var dt) ? dt : DependencyProperty.UnsetValue;
        }
    }
}
