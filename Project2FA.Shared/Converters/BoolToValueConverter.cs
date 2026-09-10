using System;
#if WINDOWS_UWP
using Windows.UI.Xaml.Data;
#else
using Microsoft.UI.Xaml.Data;
#endif

namespace Project2FA.Converters
{
    /// <summary>
    /// Shared implementation for the converters that pick one of two constants
    /// from a boolean. Derive and supply the pair; do not re-implement Convert.
    /// </summary>
    public abstract partial class BoolToValueConverter : IValueConverter
    {
        protected abstract object TrueValue { get; }
        protected abstract object FalseValue { get; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is bool flag && flag ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
