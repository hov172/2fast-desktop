#if TWOFAST_DESKTOP
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Project2FA.Core;
using Project2FA.Services;
using Project2FA.Services.MacOS;
using UNOversal.Services.Serialization;

namespace UNOversal.Services.Secrets;

// Replaces UNOversalTemplate's desktop file store only in the macOS build.
public class SecretHelper
{
    private static readonly Dictionary<string, string> Session = new();
    private static readonly object Gate = new();
    private static bool migrated;
    internal ISerializationService? Serializer { get; set; }
    private static bool IsWebDAV(string key) => key is "WDPassword" or "WDUsername" or "WDServerAddress";
    private static string Account(string container, string key) => "credential:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(container + "\0" + key)));
    public static string BiometricKey(string hash) => "vault:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(SettingsService.Instance.DataFilePath + "\0" + hash)));
    public static void ClearSession() { lock (Gate) Session.Clear(); }

    public string ReadSecret(string key) => ReadSecret(GetType().ToString(), key);
    public string ReadSecret(string container, string key)
    {
        lock (Gate)
        {
            MigrateLegacyStore();
            string account = Account(container, key);
            if (!IsWebDAV(key)) return Session.GetValueOrDefault(account, string.Empty);
            try { return MacOSNative.Read(account); }
            catch (BiometryService.BiometryException e) when (e.Reason == BiometryService.BiometryExceptionReason.KeyInvalidated) { return string.Empty; }
        }
    }
    public void WriteSecret(string key, string secret) => WriteSecret(GetType().ToString(), key, secret);
    public void WriteSecret(string container, string key, string secret)
    {
        lock (Gate)
        {
            MigrateLegacyStore();
            string account = Account(container, key);
            if (IsWebDAV(key))
            {
                if (string.IsNullOrEmpty(secret)) MacOSNative.Delete(account);
                else MacOSNative.Write(account, secret);
            }
            else Session[account] = secret;
        }
    }
    public void RemoveSecret(string key) => RemoveSecret(GetType().ToString(), key);
    public void RemoveSecret(string container, string key)
    {
        lock (Gate)
        {
            MigrateLegacyStore();
            if (IsWebDAV(key)) MacOSNative.Delete(Account(container, key));
            else
            {
                // Password changes, reset, and switching files revoke the old biometric credential.
                MacOSNative.Delete(BiometricKey(key));
                Session.Remove(Account(container, key));
                if (key == SettingsService.Instance.DataFilePasswordHash)
                {
                    SettingsService.Instance.ActivateBiometricLogin = false;
                    SettingsService.Instance.PreferBiometricLogin = Project2FA.Services.Enums.BiometricPreferEnum.No;
                }
            }
        }
    }
    internal void ForgetSessionSecret(string container, string key)
    {
        lock (Gate) Session.Remove(Account(container, key));
    }
    public bool IsSecretExistsForKey(string key) => IsSecretExistsForKey(GetType().ToString(), key);
    public bool IsSecretExistsForKey(string container, string key) => ReadSecret(container, key).Length != 0;

    private static void MigrateLegacyStore()
    {
        if (migrated) return;
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Project2FA", "secrets.dat");
        MigrateLegacyStore(path);
    }
    internal static void MigrateLegacyStore(string path)
    {
        if (!System.IO.File.Exists(path)) { migrated = true; return; }
        if (new FileInfo(path).Length > 2 * 1024 * 1024) throw new InvalidOperationException("The legacy credential store is too large to migrate safely.");
        byte[] raw = System.IO.File.ReadAllBytes(path);
        if (raw.Length < 28) throw new InvalidOperationException("The legacy credential store is damaged.");
        var fingerprint = Encoding.UTF8.GetBytes(Environment.MachineName + "|" + Environment.UserName + "|Project2FA-SecretStore");
        var key = Rfc2898DeriveBytes.Pbkdf2(fingerprint, SHA256.HashData(fingerprint), 100_000, HashAlgorithmName.SHA256, 32);
        var plain = new byte[raw.Length - 28];
        try
        {
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(raw.AsSpan(0, 12), raw.AsSpan(28), raw.AsSpan(12, 16), plain);
            var store = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(plain) ?? throw new InvalidOperationException("The legacy credential store is invalid.");
            // Migrate only WebDAV credentials. Vault secrets must be supplied by password or Touch ID.
            foreach (var bucket in store)
                foreach (var item in bucket.Value)
                    if (IsWebDAV(item.Key) && !string.IsNullOrEmpty(item.Value)) MacOSNative.Write(Account(bucket.Key, item.Key), item.Value);
            // Delete only after all persistent credentials have reached Keychain successfully.
            System.IO.File.Delete(path);
            if (System.IO.File.Exists(path + ".tmp")) System.IO.File.Delete(path + ".tmp");
            SettingsService.Instance.ActivateBiometricLogin = false;
            migrated = true;
        }
        finally { CryptographicOperations.ZeroMemory(plain); CryptographicOperations.ZeroMemory(key); }
    }
}
#endif
