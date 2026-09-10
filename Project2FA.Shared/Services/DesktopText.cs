#if TWOFAST_DESKTOP
using Windows.ApplicationModel.Resources;

namespace Project2FA.Services;

internal static class DesktopText
{
    private static readonly ResourceLoader Loader = ResourceLoader.GetForViewIndependentUse("Resources");

    internal static string Get(string key, string fallback)
    {
        var value = Loader.GetString(key);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
#endif
