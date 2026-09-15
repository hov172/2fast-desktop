using System;
#if WINDOWS_UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
#endif

namespace Project2FA.Converters
{
    public partial class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var invert = parameter != null;
            // A string counts as "empty" when null or blank; any other value only when null.
            bool isEmpty = value is string str ? string.IsNullOrEmpty(str) : value == null;
            if (isEmpty)
            {
                return invert ? Visibility.Visible : Visibility.Collapsed;
            }
            return invert ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
