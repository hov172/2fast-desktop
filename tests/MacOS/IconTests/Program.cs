using Project2FA.Converters;
using Project2FA.Repository.Models;
using Project2FA.Services;
DataService.Instance.FontIconCollection.Add(new() { Name = "microsoftoutlook", UnicodeIndex = 0xe101 });
var glyph = new FontIconNameToGlyphConverter();
var initials = new PersonalPictureInitialsVisibilityConverter();
foreach (var name in new[] { "microsoftoutlook", "Microsoft Outlook", " MICROSOFT-OUTLOOK ", "microsoft_outlook" })
{
    if (!Equals(glyph.Convert(name, null, null, null), "\ue101")) throw new Exception("Known icon not resolved.");
    if (!Equals(initials.Convert(new TwoFACodeModel { Label = "Email", AccountIconName = name }, null, null, null), "")) throw new Exception("Initials overlap icon.");
}
foreach (var name in new[] { "unknown icon", "", " ", null })
{
    if (!Equals(glyph.Convert(name, null, null, null), "")) throw new Exception("Unknown glyph should be empty.");
    if (!Equals(initials.Convert(new TwoFACodeModel { Label = "Email", AccountIconName = name }, null, null, null), "Email")) throw new Exception("Unknown icon hides initials.");
}
Console.WriteLine("Icon regression: 16 checks passed for normalized lookup and initials fallback.");
namespace Microsoft.UI.Xaml.Data { public interface IValueConverter { object Convert(object value, Type type, object parameter, string language); object ConvertBack(object value, Type type, object parameter, string language); } }
namespace Project2FA.Repository.Models { public class FontIdentifikationModel { public string Name { get; set; } public uint UnicodeIndex { get; set; } } public class TwoFACodeModel { public string Label { get; set; } public string AccountIconName { get; set; } } }
namespace Project2FA.Services { public class DataService { public static DataService Instance { get; } = new(); public List<FontIdentifikationModel> FontIconCollection { get; } = new(); } }
