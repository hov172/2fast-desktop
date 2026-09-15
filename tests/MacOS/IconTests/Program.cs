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
var bytes = new BytesToHumanReadableConverter();
if (!Equals(bytes.Convert(512, null, null, null), "512 B")) throw new Exception("Bytes below 1 KB unscaled.");
if (!Equals(bytes.Convert(1536, null, null, null), "1.5 KB")) throw new Exception("KB scaling.");
if (!Equals(bytes.Convert(decimal.MaxValue, null, null, null), "65536.000 YB")) throw new Exception("Sizes beyond YB must clamp to the last unit, not overflow.");
Console.WriteLine("Icon regression: 19 checks passed for normalized lookup, initials fallback and byte formatting.");
namespace Microsoft.UI.Xaml.Data { public interface IValueConverter { object Convert(object value, Type type, object parameter, string language); object ConvertBack(object value, Type type, object parameter, string language); } }
namespace Project2FA.Repository.Models { public class FontIdentifikationModel { public string Name { get; set; } public uint UnicodeIndex { get; set; } } public class TwoFACodeModel { public string Label { get; set; } public string AccountIconName { get; set; } } }
namespace WebDAVClient.Types { public class ResourceInfoModel { } }
namespace Project2FA.Services { public class DataService { public static DataService Instance { get; } = new(); public List<FontIdentifikationModel> FontIconCollection { get; } = new(); } }
