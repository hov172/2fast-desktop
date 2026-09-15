using System;
#if WINDOWS_UWP
using Windows.UI.Xaml.Data;
#else
using Microsoft.UI.Xaml.Data;
#endif

namespace Project2FA.Converters
{
    public partial class PersonPictureSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return double.TryParse(value.ToString(), out double size) ? (size / 3) * 2 : value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
