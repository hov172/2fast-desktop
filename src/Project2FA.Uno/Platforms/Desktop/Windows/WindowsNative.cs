#if TWOFAST_WINDOWS
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using BiometryService;

namespace Project2FA.Services.MacOS;

// Windows implementation of the shared desktop service boundary.
internal static class MacOSNative
{
    internal static string Read(string key, IntPtr context = default) => WindowsCredentialStore.Read(key, context != IntPtr.Zero);
    internal static void Write(string key, string value, bool biometric = false, IntPtr context = default) => WindowsCredentialStore.Write(key, value, biometric);
    internal static void Delete(string key) => WindowsCredentialStore.Delete(key);
    internal static int tf_biometry_status() => WindowsCredentialStore.HelloAvailable() ? 0 : -7;
    internal static Task<T> WithAuthentication<T>(CancellationToken ct, Func<IntPtr, T> action) => Task.Run(() =>
    {
        ct.ThrowIfCancellationRequested();
        T value = action(new IntPtr(1));
        ct.ThrowIfCancellationRequested();
        return value;
    }, ct);
    internal static Task<string?> ScanCamera(CancellationToken ct, bool screen = false) => WindowsQrScanner.Scan(ct, screen);
}
internal sealed class MacOSBiometryService : IBiometryService
{
    public Task<BiometryCapabilities> GetCapabilities(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        bool supported = WindowsCredentialStore.HelloAvailable();
        return Task.FromResult(new BiometryCapabilities(BiometryType.Fingerprint, supported, supported));
    }
    public Task ScanBiometry(CancellationToken ct) => MacOSNative.WithAuthentication(ct, _ =>
    {
        string name = "vault:verification:" + Guid.NewGuid().ToString("N");
        try { WindowsCredentialStore.Write(name, Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), true); return true; }
        finally { WindowsCredentialStore.Delete(name); }
    });
    public Task Encrypt(CancellationToken ct, string keyName, string value) => MacOSNative.WithAuthentication(ct, _ => { WindowsCredentialStore.Write(keyName, value, true); return true; });
    public Task<string> Decrypt(CancellationToken ct, string keyName) => MacOSNative.WithAuthentication(ct, _ => WindowsCredentialStore.Read(keyName, true));
    public void Remove(string keyName) => WindowsCredentialStore.Delete(keyName);
}

internal static class WindowsCredentialStore
{
    private static readonly CngProvider Provider = new("Microsoft Passport Key Storage Provider");
    private static readonly object Gate = new();
    private static string DirectoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "2fast", "Credentials");
    private static string Id(string name) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(name)));
    private static string FilePath(string name) => Path.Combine(DirectoryPath, Id(name) + ".bin");
    private static string KeyName(string name) => WindowsIdentity.GetCurrent().User!.Value + "//2fast/vault/" + Id(name);
    [DllImport("cryptngc.dll", CharSet = CharSet.Unicode)]
    private static extern int NgcGetDefaultDecryptionKeyName(string sid, int reserved1, int reserved2, out IntPtr name);
    internal static bool HelloAvailable()
    {
        IntPtr name = IntPtr.Zero;
        try { return NgcGetDefaultDecryptionKeyName(WindowsIdentity.GetCurrent().User!.Value, 0, 0, out name) == 0 && name != IntPtr.Zero && !string.IsNullOrEmpty(Marshal.PtrToStringUni(name)); }
        catch { return false; }
        finally { if (name != IntPtr.Zero) Marshal.FreeCoTaskMem(name); }
    }
    private static CngKey OpenHello(string name, bool create)
    {
        if (!HelloAvailable()) throw new BiometryException(BiometryExceptionReason.NotEnrolled, "Set up Windows Hello in Settings → Accounts → Sign-in options, or use your vault password.");
        string id = KeyName(name);
        CngKey key;
        if (CngKey.Exists(id, Provider)) key = CngKey.Open(id, Provider);
        else if (create)
        {
            var options = new CngKeyCreationParameters { Provider = Provider, KeyUsage = CngKeyUsages.Decryption | CngKeyUsages.Signing, ExportPolicy = CngExportPolicies.None };
            options.Parameters.Add(new CngProperty("Length", BitConverter.GetBytes(2048), CngPropertyOptions.None));
            options.Parameters.Add(new CngProperty("NgcCacheType", BitConverter.GetBytes(1), CngPropertyOptions.None));
            key = CngKey.Create(CngAlgorithm.Rsa, id, options);
        }
        else throw new BiometryException(BiometryExceptionReason.KeyInvalidated, "The Windows Hello key is missing. Unlock with your password and enable Windows Hello again.");
        try
        {
            // Refuse imported/weakly protected replacement keys; every unwrap must require a gesture.
            if (key.KeySize != 2048 || ((int)key.KeyUsage & 8) != 0 || BitConverter.ToInt32(key.GetProperty("NgcCacheType", CngPropertyOptions.None).GetValue()) != 1)
                throw new CryptographicException("Windows Hello key protection could not be verified.");
            key.SetProperty(new CngProperty("PinCacheIsGestureRequired", BitConverter.GetBytes(1), CngPropertyOptions.None));
            key.SetProperty(new CngProperty("Use Context", Encoding.Unicode.GetBytes("Unlock your 2fast vault\0"), CngPropertyOptions.None));
            IntPtr owner = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            if (owner != IntPtr.Zero) key.SetProperty(new CngProperty("HWND Handle", IntPtr.Size == 8 ? BitConverter.GetBytes(owner.ToInt64()) : BitConverter.GetBytes(owner.ToInt32()), CngPropertyOptions.None));
            return key;
        }
        catch { key.Dispose(); throw; }
    }
    internal static void Write(string name, string secret, bool biometric)
    {
        lock (Gate)
        {
            byte[] plain = Encoding.UTF8.GetBytes(secret), stored = null;
            try
            {
                if (plain.Length == 0) throw new ArgumentException("A credential is required.");
                if (!biometric && name.StartsWith("vault:", StringComparison.Ordinal)) throw new InvalidOperationException("Vault passwords require Windows Hello protection.");
                if (biometric)
                {
                    using var key = OpenHello(name, true);
                    using var rsa = new RSACng(key);
                    byte[] aesKey = RandomNumberGenerator.GetBytes(32), nonce = RandomNumberGenerator.GetBytes(12), tag = new byte[16], cipher = new byte[plain.Length];
                    try
                    {
                        using var publicKey = RSA.Create(); publicKey.ImportParameters(rsa.ExportParameters(false));
                        byte[] wrapped = publicKey.Encrypt(aesKey, RSAEncryptionPadding.OaepSHA256);
                        // Enrollment proves the protected private key is usable before saving a password.
                        byte[] verified = rsa.Decrypt(wrapped, RSAEncryptionPadding.OaepSHA256);
                        try { if (!CryptographicOperations.FixedTimeEquals(verified, aesKey)) throw new CryptographicException(); }
                        finally { CryptographicOperations.ZeroMemory(verified); }
                        if (wrapped.Length != 256) throw new CryptographicException("Invalid Windows Hello key size.");
                        using var aes = new AesGcm(aesKey, 16);
                        aes.Encrypt(nonce, plain, cipher, tag, Encoding.UTF8.GetBytes(name));
                        stored = new byte[1 + 256 + 12 + 16 + cipher.Length]; stored[0] = 1;
                        wrapped.CopyTo(stored, 1); nonce.CopyTo(stored, 257); tag.CopyTo(stored, 269); cipher.CopyTo(stored, 285);
                    }
                    finally { CryptographicOperations.ZeroMemory(aesKey); }
                }
                else { stored = new byte[plain.Length + 1]; plain.CopyTo(stored, 1); }
                byte[] protectedData = ProtectedData.Protect(stored, Encoding.UTF8.GetBytes(name), DataProtectionScope.CurrentUser);
                Directory.CreateDirectory(DirectoryPath);
                string path = FilePath(name), temporary = path + "." + Guid.NewGuid().ToString("N");
                try
                {
                    using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { stream.Write(protectedData); stream.Flush(true); }
                    File.Move(temporary, path, true);
                }
                finally { File.Delete(temporary); }
            }
            catch (CryptographicException error) { throw Translate(error); }
            finally { CryptographicOperations.ZeroMemory(plain); if (stored != null) CryptographicOperations.ZeroMemory(stored); }
        }
    }
    internal static string Read(string name, bool allowHello)
    {
        lock (Gate)
        {
            if (!File.Exists(FilePath(name))) throw new BiometryException(BiometryExceptionReason.KeyInvalidated, "The saved credential is missing. Use your vault password.");
            byte[] data = ProtectedData.Unprotect(File.ReadAllBytes(FilePath(name)), Encoding.UTF8.GetBytes(name), DataProtectionScope.CurrentUser);
            try
            {
                if (data.Length < 2) throw new CryptographicException();
                if (data[0] == 0)
                {
                    if (name.StartsWith("vault:", StringComparison.Ordinal)) throw new CryptographicException("Vault credentials require Windows Hello.");
                    return Encoding.UTF8.GetString(data, 1, data.Length - 1);
                }
                if (!allowHello || data[0] != 1 || data.Length <= 285) throw new CryptographicException("Windows Hello is required.");
                using var key = OpenHello(name, false); using var rsa = new RSACng(key);
                byte[] aesKey = rsa.Decrypt(data.AsSpan(1, 256).ToArray(), RSAEncryptionPadding.OaepSHA256), plain = new byte[data.Length - 285];
                try
                {
                    if (aesKey.Length != 32) throw new CryptographicException("Invalid Windows Hello envelope key.");
                    using var aes = new AesGcm(aesKey, 16);
                    aes.Decrypt(data.AsSpan(257, 12), data.AsSpan(285), data.AsSpan(269, 16), plain, Encoding.UTF8.GetBytes(name));
                    return Encoding.UTF8.GetString(plain);
                }
                finally { CryptographicOperations.ZeroMemory(aesKey); CryptographicOperations.ZeroMemory(plain); }
            }
            catch (CryptographicException error) { throw Translate(error); }
            finally { CryptographicOperations.ZeroMemory(data); }
        }
    }
    private static Exception Translate(CryptographicException error) => error.HResult is unchecked((int)0x80090036) or unchecked((int)0x800704C7)
        ? new OperationCanceledException() : new BiometryException(BiometryExceptionReason.Failed, "Windows could not unlock the protected credential. Use your vault password and enroll Windows Hello again.");
    internal static void Delete(string name)
    {
        lock (Gate)
        {
            if (name.StartsWith("vault:", StringComparison.Ordinal) && CngKey.Exists(KeyName(name), Provider))
            { using var key = CngKey.Open(KeyName(name), Provider); key.Delete(); }
            File.Delete(FilePath(name));
        }
    }
}
#endif
