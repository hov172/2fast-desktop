using Project2FA.Services.Desktop;
if (!DesktopDeviceBinding.Allows("") || DesktopNative.Reads != 0) throw new Exception("Unbound token requires Keychain");
string local = DesktopDeviceBinding.Identity;
if (local.Length != 32 || DesktopNative.Writes != 1) throw new Exception("Missing identity not created once");
if (!DesktopDeviceBinding.Allows(local)) throw new Exception("Local token rejected");
if (DesktopDeviceBinding.Allows("other-device")) throw new Exception("Different Mac accepted");
Parallel.For(0, 20, _ => { if (DesktopDeviceBinding.Identity != local) throw new Exception("Identity changed"); });
if (DesktopNative.Reads != 1 || DesktopNative.Writes != 1) throw new Exception("Concurrent identity replacement");
Console.WriteLine("Device binding: 5 checks passed.");
namespace Project2FA.Services.Desktop
{
    internal static class DesktopNative
    {
        internal static int Reads, Writes;
        internal static string Read(string key) { Reads++; throw new BiometryService.BiometryException(BiometryService.BiometryExceptionReason.KeyInvalidated); }
        internal static void Write(string key, string value) { Writes++; }
    }
}
namespace BiometryService
{
    public enum BiometryExceptionReason { KeyInvalidated }
    public class BiometryException : Exception { public BiometryExceptionReason Reason { get; } public BiometryException(BiometryExceptionReason reason) { Reason = reason; } }
}
