using Project2FA.Services.MacOS;
using BiometryService;
if (!OperatingSystem.IsWindows()) { Console.WriteLine("SKIP: Windows native tests require Windows."); return; }
string name = "2fast-native-test-" + Guid.NewGuid().ToString("N");
const string value = "synthetic test credential é with trailing space ";
try
{
    WindowsCredentialStore.Write(name, value, false);
    if (WindowsCredentialStore.Read(name, false) != value) throw new Exception("Credential round trip changed bytes");
    WindowsCredentialStore.Delete(name); WindowsCredentialStore.Delete(name);
    try { WindowsCredentialStore.Read(name, false); throw new Exception("Deleted credential returned"); }
    catch (BiometryException error) when (error.Reason == BiometryExceptionReason.KeyInvalidated) { }
    Console.WriteLine("PASS: Windows DPAPI credential round trip, deletion and missing-item rejection.");
    using var canceled = new CancellationTokenSource(); canceled.Cancel();
    try { await MacOSNative.WithAuthentication<bool>(canceled.Token, _ => throw new Exception("Canceled operation ran")); }
    catch (OperationCanceledException) { Console.WriteLine("PASS: Canceled secure operation did not run."); }
    bool available = WindowsCredentialStore.HelloAvailable();
    Console.WriteLine("Windows Hello enrolled: " + available);
    if (args.Contains("--hello"))
    {
        if (!available) throw new InvalidOperationException("Enroll Windows Hello before the interactive test.");
        name = "vault:" + name;
        WindowsCredentialStore.Write(name, value, true);
        try { WindowsCredentialStore.Read(name, false); throw new Exception("Hello protection bypassed"); }
        catch (BiometryException) { }
        if (WindowsCredentialStore.Read(name, true) != value) throw new Exception("Hello credential changed");
        Console.WriteLine("PASS: Windows Hello enrollment, protected unlock and noninteractive-read rejection.");
    }
}
finally { WindowsCredentialStore.Delete(name); }
namespace Project2FA.Services.MacOS
{
    internal static class WindowsQrScanner
    {
        internal static Task<string?> Scan(CancellationToken token, bool screen) => throw new NotSupportedException("Scanner is outside this credential test harness.");
    }
}
