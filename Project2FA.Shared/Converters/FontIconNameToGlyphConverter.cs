using Project2FA.Services;
using System;
using System.Linq;

#if WINDOWS_UWP
using Windows.UI.Xaml.Data;
#else
using Microsoft.UI.Xaml.Data;
#endif

namespace Project2FA.Converters
{
    internal static class IconNameLookup
    {
        internal static string Normalize(string name) => new string((name ?? string.Empty)
            .Where(c => !char.IsWhiteSpace(c) && c != '-' && c != '_').ToArray()).ToLowerInvariant();

        internal static Project2FA.Repository.Models.FontIdentifikationModel Find(string name)
        {
            string key = Normalize(name);
            if (key.Length == 0) return null;
            return DataService.Instance.FontIconCollection.FirstOrDefault(icon => Normalize(icon.Name) == key);
        }
    }

    public partial class FontIconNameToGlyphConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string name)
            {
                var model = IconNameLookup.Find(name);
                if (model != null)
                {
                    return ((char)model.UnicodeIndex).ToString();
                }
                return string.Empty;
            }
            else
            {
                return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
