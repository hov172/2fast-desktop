#if TWOFAST_DESKTOP
using BiometryService;
namespace Project2FA.Services.Desktop;
internal static class DesktopDeviceBinding
{
    private static string? identity;
    private static readonly object gate = new();
    internal static string Identity
    {
        get
        {
            lock (gate)
            {
            if (identity != null) return identity;
            try { identity = DesktopNative.Read("mobileid-device-binding-id"); }
            catch (BiometryException e) when (e.Reason == BiometryExceptionReason.KeyInvalidated) { }
            if (string.IsNullOrEmpty(identity))
            {
                string created = Guid.NewGuid().ToString("N");
                DesktopNative.Write("mobileid-device-binding-id", created);
                identity = created;
            }
            return identity;
            }
        }
    }
    internal static bool Allows(string boundIdentity) => string.IsNullOrEmpty(boundIdentity) || boundIdentity == Identity;
}
#endif
