using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Project2FA.Services;
using Project2FA.Services.Desktop;
using UNOversal.Services.Secrets;

void Check(bool result, string name)
{
    if (!result) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name);
}
var folder = Path.Combine(Path.GetTempPath(), "2fast-secret-tests-" + Guid.NewGuid());
Directory.CreateDirectory(folder);
try
{
    var path = Path.Combine(folder, "secrets.dat");
    var data = JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, Dictionary<string, string>>
    {
        ["container"] = new() { ["test-hash"] = "synthetic-vault-password", ["WDPassword"] = "synthetic-webdav-password" }
    });
    var fingerprint = Encoding.UTF8.GetBytes(Environment.MachineName + "|" + Environment.UserName + "|Project2FA-SecretStore");
    var key = Rfc2898DeriveBytes.Pbkdf2(fingerprint, SHA256.HashData(fingerprint), 100_000, HashAlgorithmName.SHA256, 32);
    var nonce = RandomNumberGenerator.GetBytes(12);
    var tag = new byte[16];
    var encrypted = new byte[data.Length];
    using (var aes = new AesGcm(key, 16)) aes.Encrypt(nonce, data, encrypted, tag);
    File.WriteAllBytes(path, nonce.Concat(tag).Concat(encrypted).ToArray());
    File.Copy(path, path + ".tmp");
    DesktopNative.FailWrites = true;
    bool failed = false;
    try { SecretHelper.MigrateLegacyStore(path); } catch (InvalidOperationException) { failed = true; }
    Check(failed && File.Exists(path), "failed Keychain migration preserves the original file");
    DesktopNative.FailWrites = false;
    SecretHelper.MigrateLegacyStore(path);
    Check(!File.Exists(path) && !File.Exists(path + ".tmp"), "successful migration removes weak legacy copies");
    Check(DesktopNative.Items.Count == 1 && !DesktopNative.Items.Values.Contains("synthetic-vault-password"), "migration never persists the vault password");
    var helper = new SecretHelper();
    Check(helper.ReadSecret("container", "test-hash") == "", "migration does not automatically unlock a vault");
    helper.WriteSecret("container", "test-hash", "synthetic-vault-password");
    Check(helper.ReadSecret("container", "test-hash") == "synthetic-vault-password" && DesktopNative.Items.Count == 1, "password login caches the secret only in memory");
    SecretHelper.ClearSession();
    Check(helper.ReadSecret("container", "test-hash") == "", "logout clears the session password");
    Check(helper.ReadSecret("container", "WDPassword") == "synthetic-webdav-password", "WebDAV credentials survive session locking in Keychain");
    SettingsService.Instance.ActivateBiometricLogin = true;
    string biometricKey = SecretHelper.BiometricKey("test-hash");
    helper.RemoveSecret("container", "test-hash");
    Check(DesktopNative.Deleted.Contains(biometricKey) && !SettingsService.Instance.ActivateBiometricLogin, "password removal revokes Touch ID and disables its setting");
    SettingsService.Instance.DataFilePath = "/synthetic/another.2fa";
    Check(SecretHelper.BiometricKey("test-hash") != biometricKey, "Touch ID credentials are bound to the selected vault");
}
finally { Directory.Delete(folder, true); }
