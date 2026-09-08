#if TWOFAST_DESKTOP
namespace Project2FA.Services.MacOS;
internal static class DesktopPlatform
{
    internal static string BiometricName => OperatingSystem.IsWindows() ? "Windows Hello" : "Touch ID";
    internal static string BiometricDescription => OperatingSystem.IsWindows()
        ? "Unlock this vault with Windows Hello. Set up your PIN, face or fingerprint in Settings → Accounts → Sign-in options."
        : "Unlock this vault using a fingerprint enrolled in System Settings → Touch ID & Password.";
    internal static string Name => OperatingSystem.IsWindows() ? "Windows" : "macOS";
}
#endif
