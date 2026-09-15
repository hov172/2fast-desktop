using System;
#if WINDOWS_UWP
using Windows.UI.Xaml.Data;
#else
using Microsoft.UI.Xaml.Data;
#endif

namespace Project2FA.Converters
{
    public partial class FontIconUnicodeIndexToGlyphConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return int.TryParse(value.ToString(), out var unicodeIndex)
                ? ((char)unicodeIndex).ToString()
                : string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
