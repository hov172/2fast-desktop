#if TWOFAST_MACOS
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using BiometryService;

namespace Project2FA.Services.MacOS;

internal static class MacOSNative
{
    private const string Library = "libTwoFastMac.dylib";
    [DllImport(Library)] internal static extern int tf_biometry_status();
    [DllImport(Library)] private static extern IntPtr tf_auth_create();
    [DllImport(Library)] private static extern void tf_auth_cancel(IntPtr context);
    [DllImport(Library)] private static extern void tf_auth_release(IntPtr context);
    [DllImport(Library)] private static extern int tf_auth_scan(IntPtr context);
    [DllImport(Library)] private static extern int tf_keychain_write(IntPtr context, [MarshalAs(UnmanagedType.LPUTF8Str)] string key, byte[] data, int length, int biometric);
    [DllImport(Library)] private static extern int tf_keychain_read(IntPtr context, [MarshalAs(UnmanagedType.LPUTF8Str)] string key, out IntPtr data, out int length);
    [DllImport(Library)] private static extern int tf_keychain_delete([MarshalAs(UnmanagedType.LPUTF8Str)] string key);
    [DllImport(Library)] private static extern void tf_secret_free(IntPtr data, int length);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ScanCallback(int status, IntPtr payload, IntPtr state);
    [DllImport(Library)] private static extern void tf_camera_start(ulong id, int screen, ScanCallback callback, IntPtr state);
    [DllImport(Library)] private static extern void tf_camera_cancel(ulong id);
    private static readonly ScanCallback ScanCompleted = CompleteScan;
    private static long nextScan;

    internal static void Check(int status)
    {
        if (status == 0) return;
        if (status is -2 or -4 or -9 or -128) throw new OperationCanceledException();
        var reason = status switch
        {
            -6 => BiometryExceptionReason.Unavailable,
            -7 => BiometryExceptionReason.NotEnrolled,
            -8 => BiometryExceptionReason.Locked,
            -25300 => BiometryExceptionReason.KeyInvalidated,
            _ => BiometryExceptionReason.Failed
        };
        var message = status switch
        {
            -6 => "Touch ID is not available on this Mac. Use your data-file password.",
            -7 => "Set up a fingerprint in System Settings → Touch ID & Password first.",
            -8 => "Touch ID is locked. Unlock your Mac with its password, or use your data-file password here.",
            -25300 => "The saved Touch ID credential is missing or invalidated. Sign in with your password and enable Touch ID again.",
            -34018 => "Keychain access was denied to this build. Rebuild with the macOS signing entitlements.",
            _ => "macOS could not complete the secure operation. Use your data-file password and try again."
        };
        throw new BiometryException(reason, message);
    }

    internal static string Read(string key, IntPtr context = default)
    {
        int status = tf_keychain_read(context, key, out var pointer, out var length);
        try
        {
            Check(status);
            if (length < 1 || pointer == IntPtr.Zero) throw new InvalidOperationException("Invalid Keychain response.");
            var data = new byte[length];
            try { Marshal.Copy(pointer, data, 0, length); return Encoding.UTF8.GetString(data); }
            finally { CryptographicOperations.ZeroMemory(data); }
        }
        finally { tf_secret_free(pointer, length); }
    }

    internal static void Write(string key, string secret, bool biometric = false, IntPtr context = default)
    {
        var bytes = Encoding.UTF8.GetBytes(secret);
        try { Check(tf_keychain_write(context, key, bytes, bytes.Length, biometric ? 1 : 0)); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }
    internal static void Delete(string key) => Check(tf_keychain_delete(key));

    internal static Task<T> WithAuthentication<T>(CancellationToken ct, Func<IntPtr, T> operation) => Task.Run(() =>
    {
        ct.ThrowIfCancellationRequested();
        var context = tf_auth_create();
        if (context == IntPtr.Zero) throw new InvalidOperationException("Unable to create a Touch ID context.");
        try
        {
            using var registration = ct.Register(() => tf_auth_cancel(context));
            var result = operation(context);
            ct.ThrowIfCancellationRequested();
            return result;
        }
        finally { tf_auth_cancel(context); tf_auth_release(context); }
    }, ct);

    internal static Task ScanBiometry(CancellationToken ct) => WithAuthentication(ct, context => { Check(tf_auth_scan(context)); return true; });

    internal static async Task<string?> ScanCamera(CancellationToken ct, bool screen = false)
    {
        ct.ThrowIfCancellationRequested();
        var completion = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var state = GCHandle.Alloc(completion);
        ulong id = (ulong)Interlocked.Increment(ref nextScan);
        // Native completion owns the GCHandle, including cancellation and error paths.
        try { tf_camera_start(id, screen ? 1 : 0, ScanCompleted, GCHandle.ToIntPtr(state)); }
        catch { state.Free(); throw; }
        using var registration = ct.Register(() => tf_camera_cancel(id));
        var result = await completion.Task;
        ct.ThrowIfCancellationRequested();
        return result;
    }

    private static void CompleteScan(int status, IntPtr payload, IntPtr state)
    {
        var handle = GCHandle.FromIntPtr(state);
        var completion = (TaskCompletionSource<string?>)handle.Target!;
        try
        {
            if (status == 0) completion.TrySetResult(Marshal.PtrToStringUTF8(payload));
            else if (status == 1) completion.TrySetResult(null);
            else completion.TrySetException(new InvalidOperationException(status switch
            {
                2 => "Camera access is disabled. Allow 2fast in System Settings → Privacy & Security → Camera, then try again.",
                3 => "No camera was found. Connect a camera and try again.",
                4 => "The camera is unavailable or was disconnected. Close other camera apps, reconnect it, and try again.",
                6 => "A camera scan is already open.",
                7 => "Unable to share that window or screen. Try selecting it again in the macOS picker.",
                _ => "The camera stopped unexpectedly. Close other camera apps and try again."
            }));
        }
        catch (Exception) { completion.TrySetException(new InvalidOperationException("Unable to read the camera result.")); }
        finally { handle.Free(); }
    }
}

internal sealed class MacOSBiometryService : IBiometryService
{
    public Task<BiometryCapabilities> GetCapabilities(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        int status = MacOSNative.tf_biometry_status();
        return Task.FromResult(new BiometryCapabilities(status == -6 ? BiometryType.None : BiometryType.Fingerprint, status == 0, status != -5));
    }
    public Task ScanBiometry(CancellationToken ct) => MacOSNative.ScanBiometry(ct);
    public Task Encrypt(CancellationToken ct, string keyName, string keyValue) => MacOSNative.WithAuthentication(ct, context =>
    {
        MacOSNative.Write(keyName, keyValue, true, context);
        return true;
    });
    public Task<string> Decrypt(CancellationToken ct, string keyName) => MacOSNative.WithAuthentication(ct, context => MacOSNative.Read(keyName, context));
    public void Remove(string keyName) => MacOSNative.Delete(keyName);
}
#endif
