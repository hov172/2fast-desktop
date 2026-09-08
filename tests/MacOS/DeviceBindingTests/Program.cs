using Project2FA.Services.MacOS;
if (!MacOSDeviceBinding.Allows("") || MacOSNative.Reads != 0) throw new Exception("Unbound token requires Keychain");
string local = MacOSDeviceBinding.Identity;
if (local.Length != 32 || MacOSNative.Writes != 1) throw new Exception("Missing identity not created once");
if (!MacOSDeviceBinding.Allows(local)) throw new Exception("Local token rejected");
if (MacOSDeviceBinding.Allows("other-device")) throw new Exception("Different Mac accepted");
Parallel.For(0, 20, _ => { if (MacOSDeviceBinding.Identity != local) throw new Exception("Identity changed"); });
if (MacOSNative.Reads != 1 || MacOSNative.Writes != 1) throw new Exception("Concurrent identity replacement");
Console.WriteLine("Device binding: 5 checks passed.");
namespace Project2FA.Services.MacOS
{
    internal static class MacOSNative
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
