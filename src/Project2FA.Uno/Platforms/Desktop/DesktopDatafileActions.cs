#if TWOFAST_DESKTOP
using System.Security.Cryptography;
using System.Text;
using Project2FA.Core;
using Project2FA.Core.Messenger;
using CommunityToolkit.Mvvm.Messaging;
using Project2FA.Core.Services.Crypto;
using Project2FA.Repository.Models;
using Project2FA.Repository.Models.Enums;
using Project2FA.Services.Desktop;
using UNOversal.Services.Secrets;
using Windows.Storage;

namespace Project2FA.Services;
public partial class DataService
{
    internal Task<StorageFile> CurrentDesktopVault() => DesktopVaultLocation.OpenAsync(
        ActivatedDatafile?.Path ?? SettingsService.Instance.DataFilePath,
        ActivatedDatafile?.Name ?? SettingsService.Instance.DataFileName,
        ActivatedDatafile == null && SettingsService.Instance.DataFileWebDAVEnabled);

    private void RevokeMacOSUnlock()
    {
        string key = ActivatedDatafile != null ? Constants.ActivatedDatafileHashName : SettingsService.Instance.DataFilePasswordHash;
        DesktopNative.Delete(SecretHelper.BiometricKey(key));
        SettingsService.Instance.ActivateBiometricLogin = false;
        SettingsService.Instance.PreferBiometricLogin = Enums.BiometricPreferEnum.No;
    }

    public async Task<bool> ChangeMacOSPassword(string current, string replacement, bool adoptExisting = false)
    {
        if (string.IsNullOrEmpty(replacement)) throw new ArgumentException("Enter a new password.");
        if (!adoptExisting)
        {
            string sessionKey = ActivatedDatafile != null ? Constants.ActivatedDatafileHashName : SettingsService.Instance.DataFilePasswordHash;
            string sessionValue = SecretService.Helper.ReadSecret(Constants.ContainerName, sessionKey);
            byte[] expected = ActivatedDatafile != null ? SerializationService.Deserialize<byte[]>(sessionValue) : Encoding.UTF8.GetBytes(sessionValue);
            byte[] entered = Encoding.UTF8.GetBytes(current ?? "");
            try
            {
                if (expected.Length == 0 || !CryptographicOperations.FixedTimeEquals(SHA256.HashData(expected), SHA256.HashData(entered)))
                    throw new InvalidOperationException("The current password is incorrect.");
            }
            finally { CryptographicOperations.ZeroMemory(expected); CryptographicOperations.ZeroMemory(entered); }
        }
        await CollectionAccessSemaphore.WaitAsync();
        try
        {
            var file = await CurrentDesktopVault();
            string original = await System.IO.File.ReadAllTextAsync(file.Path);
            var decoded = await Task.Run(() => DesktopVaultCodec.DecryptCurrent(original, adoptExisting ? replacement : current ?? "", ActivatedDatafile == null ? SettingsService.Instance.DataFilePasswordHash : null));
            if (decoded == null) throw new InvalidOperationException("The data file could not be read.");
            string keyName = ActivatedDatafile != null ? Constants.ActivatedDatafileHashName : SettingsService.Instance.DataFilePasswordHash;
            string previousSecret = SecretService.Helper.ReadSecret(Constants.ContainerName, keyName);
            string previousHash = SettingsService.Instance.DataFilePasswordHash;
            string nextHash = adoptExisting && !DesktopVaultCodec.IsModern(original) ? CryptoService.CreateStringHash(replacement) : DesktopVaultCodec.NewCredentialId();
            string nextKey = ActivatedDatafile != null ? Constants.ActivatedDatafileHashName : nextHash;
            string nextSecret = ActivatedDatafile != null ? SerializationService.Serialize(Encoding.UTF8.GetBytes(replacement)) : replacement;
            RevokeMacOSUnlock();
            string content = original;
            if (!adoptExisting)
            {
                content = await Task.Run(() => DesktopVaultCodec.Encrypt(decoded, replacement));
                var verified = await Task.Run(() => DesktopVaultCodec.Decrypt(content, replacement));
                if (!DesktopVaultCodec.SameAccounts(decoded, verified)) throw new IOException("Vault verification failed.");
            }
            DesktopRemoteVaultTransaction remoteChange = null;
            if (SettingsService.Instance.DataFileWebDAVEnabled && ActivatedDatafile == null && !adoptExisting)
            {
                var remote = WebDAV.WebDAVClientService.Instance.GetClient().GetConditionalVaultClient(SettingsService.Instance.DataFilePath + "/" + file.Name);
                var revision = await remote.ReadAsync();
                var remoteModel = await Task.Run(() => DesktopVaultCodec.Decrypt(revision.Content, current));
                if (!DesktopVaultCodec.SameAccounts(decoded, remoteModel))
                    throw new IOException("The WebDAV vault differs from the local copy. Reload it before changing the password.");
                remoteChange = new DesktopRemoteVaultTransaction(remote, revision, content);
            }
            // Explicit upgrades keep the legacy encrypted original for older clients and recovery.
            if (!DesktopVaultCodec.IsModern(original) && !adoptExisting)
                await DesktopFileTransaction.CopyNew(file.Path, file.Path + ".legacy-" + Guid.NewGuid().ToString("N") + ".2fa");
            await DesktopFileTransaction.Replace(file.Path, content, () =>
            {
                SecretService.Helper.WriteSecret(Constants.ContainerName, nextKey, nextSecret);
                if (ActivatedDatafile == null) SettingsService.Instance.DataFilePasswordHash = nextHash;
            }, () =>
            {
                if (nextKey != keyName) SecretService.Helper.ForgetSessionSecret(Constants.ContainerName, nextKey);
                SettingsService.Instance.DataFilePasswordHash = previousHash;
                SecretService.Helper.WriteSecret(Constants.ContainerName, keyName, previousSecret);
            }, remoteChange == null ? null : async (path, text) =>
            {
                if (text == content) await remoteChange.Apply();
                else await remoteChange.Restore();
                await DesktopVaultLocation.WriteAtomicAsync(Path.GetDirectoryName(path)!, Path.GetFileName(path), text);
            });
            if (nextKey != keyName) SecretService.Helper.ForgetSessionSecret(Constants.ContainerName, keyName);
            return true;
        }
        finally
        {
            CollectionAccessSemaphore.Release();
        }
    }

    internal async Task WriteDesktopRemoteVault(string path, string original, string content, string password)
    {
        var remote = WebDAV.WebDAVClientService.Instance.GetClient().GetConditionalVaultClient(SettingsService.Instance.DataFilePath + "/" + Path.GetFileName(path));
        var revision = await remote.ReadAsync();
        bool same = await Task.Run(() => DesktopVaultCodec.SameAccounts(DesktopVaultCodec.Decrypt(original, password), DesktopVaultCodec.DecryptCurrent(revision.Content, password, SettingsService.Instance.DataFilePasswordHash)));
        if (!same) throw new IOException("The WebDAV vault changed on another device. Reload before saving; your pending changes have not been uploaded.");
        var transaction = new DesktopRemoteVaultTransaction(remote, revision, content);
        await DesktopFileTransaction.Replace(path, content, () => { }, () => { }, async (target, text) =>
        {
            if (text == content) await transaction.Apply(); else await transaction.Restore();
            await DesktopVaultLocation.WriteAtomicAsync(Path.GetDirectoryName(target)!, Path.GetFileName(target), text);
        });
    }

    private async Task<(bool successful, bool outdated)> SyncDesktopRemoteVault()
    {
        await CollectionAccessSemaphore.WaitAsync();
        try
        {
            var file = await CurrentDesktopVault();
            string original = await File.ReadAllTextAsync(file.Path);
            string password = SecretService.Helper.ReadSecret(Constants.ContainerName, SettingsService.Instance.DataFilePasswordHash);
            var remote = WebDAV.WebDAVClientService.Instance.GetClient().GetConditionalVaultClient(SettingsService.Instance.DataFilePath + "/" + file.Name);
            WebDAVClient.VaultRevision revision;
            try { revision = await remote.ReadAsync(); }
            catch (System.Net.Http.HttpRequestException error) when (error.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                await Task.Run(() => DesktopVaultCodec.Decrypt(original, password));
                await remote.CreateAsync(original);
                return (true, false);
            }
            if (revision.Content == original) return (true, false);
            await Task.Run(() => DesktopVaultCodec.DecryptCurrent(revision.Content, password, SettingsService.Instance.DataFilePasswordHash));
            // Remote content is authoritative on reload, never local filesystem timestamps.
            await DesktopFileTransaction.CopyNew(file.Path, file.Path + ".before-sync-" + Guid.NewGuid().ToString("N") + ".2fa");
            string previousId = SettingsService.Instance.DataFilePasswordHash;
            string nextId = DesktopVaultCodec.IsModern(revision.Content) && !previousId.StartsWith("vault-", StringComparison.Ordinal)
                ? DesktopVaultCodec.NewCredentialId() : previousId;
            if (nextId != previousId) RevokeMacOSUnlock();
            await DesktopFileTransaction.Replace(file.Path, revision.Content, () =>
            {
                if (nextId != previousId)
                {
                    SecretService.Helper.WriteSecret(Constants.ContainerName, nextId, password);
                    SettingsService.Instance.DataFilePasswordHash = nextId;
                }
            }, () =>
            {
                SettingsService.Instance.DataFilePasswordHash = previousId;
                if (nextId != previousId) SecretService.Helper.ForgetSessionSecret(Constants.ContainerName, nextId);
            });
            if (nextId != previousId) SecretService.Helper.ForgetSessionSecret(Constants.ContainerName, previousId);
            return (true, true);
        }
        catch (Exception error)
        {
            DesktopScanDiagnostics.Record("WebDAV sync", error);
            Messenger.Send(new WebDAVStatusChangedMessage(WebDAVStatus.Failed));
            return (false, false);
        }
        finally { CollectionAccessSemaphore.Release(); }
    }

    public async Task UpgradeDesktopVault()
    {
        string key = ActivatedDatafile != null ? Constants.ActivatedDatafileHashName : SettingsService.Instance.DataFilePasswordHash;
        string value = SecretService.Helper.ReadSecret(Constants.ContainerName, key);
        string password = ActivatedDatafile != null ? Encoding.UTF8.GetString(SerializationService.Deserialize<byte[]>(value)) : value;
        await ChangeMacOSPassword(password, password);
    }

    public async Task<string> CopyDesktopVault(string folder, string name, bool move)
    {
        if (!await WriteLocalDatafile()) throw new IOException("The current data file could not be saved. Try again before copying it.");
        await CollectionAccessSemaphore.WaitAsync();
        try
        {
            var source = await CurrentDesktopVault();
            string destination = Path.Combine(folder, DesktopFileTransaction.FileName(name));
            if (Path.GetFullPath(source.Path) == Path.GetFullPath(destination)) return source.Path;
            if (move) RevokeMacOSUnlock();
            await DesktopFileTransaction.CopyNew(source.Path, destination);
            if (move)
            {
                string previousPath = SettingsService.Instance.DataFilePath, previousName = SettingsService.Instance.DataFileName;
                var previousActivated = ActivatedDatafile;
                bool previousWebDAV = SettingsService.Instance.DataFileWebDAVEnabled;
                try
                {
                    if (ActivatedDatafile != null) ActivatedDatafile = await StorageFile.GetFileFromPathAsync(destination);
                    else { SettingsService.Instance.DataFilePath = destination; SettingsService.Instance.DataFileName = Path.GetFileName(destination); SettingsService.Instance.DataFileWebDAVEnabled = false; }
                    System.IO.File.Delete(source.Path);
                }
                catch
                {
                    SettingsService.Instance.DataFilePath = previousPath; SettingsService.Instance.DataFileName = previousName;
                    ActivatedDatafile = previousActivated;
                    SettingsService.Instance.DataFileWebDAVEnabled = previousWebDAV;
                    System.IO.File.Delete(destination);
                    throw;
                }
            }
            return destination;
        }
        finally { CollectionAccessSemaphore.Release(); }
    }
}
#endif
