#if TWOFAST_DESKTOP
namespace Project2FA.Services.Desktop;
internal static class DesktopPlatform
{
    internal static string BiometricName => OperatingSystem.IsWindows()
        ? DesktopText.Get("DesktopBiometricName.Windows", "Windows Hello")
        : DesktopText.Get("DesktopBiometricName.Mac", "Touch ID");
    internal static string BiometricDescription => OperatingSystem.IsWindows()
        ? DesktopText.Get("DesktopBiometricDescription.Windows", "Unlock this vault with Windows Hello. Set up your PIN, face or fingerprint in Settings → Accounts → Sign-in options.")
        : DesktopText.Get("DesktopBiometricDescription.Mac", "Unlock this vault using a fingerprint enrolled in System Settings → Touch ID & Password.");
    internal static string Name => OperatingSystem.IsWindows()
        ? DesktopText.Get("DesktopPlatformName.Windows", "Windows")
        : DesktopText.Get("DesktopPlatformName.Mac", "macOS");
}
#endif
