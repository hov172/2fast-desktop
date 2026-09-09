using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
internal static class VaultV4Checks
{
    internal static void Run(Assembly app, object account)
    {
        var codec = app.GetType("Project2FA.Services.MacOS.MacOSVaultCodec", true)!;
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        var encrypt = codec.GetMethod("Encrypt", flags)!;
        var decrypt = codec.GetMethod("Decrypt", flags)!;
        var equal = codec.GetMethod("SameAccounts", flags)!;
        var modelType = app.GetType("Project2FA.Repository.Models.DatafileModel", true)!;
        var model = Activator.CreateInstance(modelType)!;
        var collectionProperty = modelType.GetProperty("Collection")!;
        var collection = (IList)Activator.CreateInstance(collectionProperty.PropertyType)!;
        collection.Add(account); collectionProperty.SetValue(model, collection);
        var categories = modelType.GetProperty("GlobalCategories")!;
        categories.SetValue(model, Activator.CreateInstance(categories.PropertyType));
        const string password = "synthetic password with unicode é and trailing space ";
        string Encrypt(object m, string p) => (string)encrypt.Invoke(null, new[] { m, p })!;
        object Decrypt(string text, string p) => decrypt.Invoke(null, new[] { text, p })!;
        void Reject(string text, string p)
        {
            try { Decrypt(text, p); }
            catch (TargetInvocationException ex) when (ex.InnerException is CryptographicException or InvalidDataException or System.Text.Json.JsonException) { return; }
            throw new Exception("Malformed vault or incorrect password was accepted.");
        }
        string content = Encrypt(model, password);
        foreach (var attempt in new[] { "", "wrong" })
        {
            try { codec.GetMethod("VerifyCredential", flags)!.Invoke(null, new object[] { content, attempt, "vault-synthetic" }); throw new Exception("Invalid unlock attempt accepted."); }
            catch (TargetInvocationException error) when (error.InnerException is System.Security.Cryptography.CryptographicException) { }
        }
        codec.GetMethod("VerifyCredential", flags)!.Invoke(null, new object[] { content, password, "vault-synthetic" });
        try { codec.GetMethod("VerifyCredential", flags)!.Invoke(null, new object[] { content, "wrong", "vault-synthetic" }); throw new Exception("Biometric enrollment accepted a wrong V4 password."); }
        catch (TargetInvocationException ex) when (ex.InnerException is CryptographicException) { }
        if (!(bool)equal.Invoke(null, new[] { model, Decrypt(content, password) })!) throw new Exception("V4 lost account metadata.");
        if (content.Contains("Synthetic") || content.Contains(Convert.ToBase64String(Encoding.ASCII.GetBytes("12345678901234567890")))) throw new Exception("V4 exposes account data.");
        var first = JsonNode.Parse(content)!.AsObject(); var second = JsonNode.Parse(Encrypt(model, password))!.AsObject();
        foreach (string field in new[] { "Salt", "Nonce", "Data", "Tag" })
        {
            if (first[field]!.ToJsonString() == second[field]!.ToJsonString()) throw new Exception("Encryption reused " + field);
            var changed = JsonNode.Parse(content)!.AsObject(); byte[] bytes = Convert.FromBase64String(changed[field]!.GetValue<string>());
            bytes[0] ^= 1; changed[field] = Convert.ToBase64String(bytes); Reject(changed.ToJsonString(), password);
        }
        Reject(content, "incorrect"); Reject(content, password.TrimEnd());
        foreach (var field in new[] { "Version", "Iterations" })
        {
            var changed = JsonNode.Parse(content)!.AsObject(); changed[field] = int.MaxValue; Reject(changed.ToJsonString(), password);
        }
        foreach (var field in new[] { "Algorithm", "Kdf" })
        { var changed = JsonNode.Parse(content)!.AsObject(); changed[field] = "unsupported"; Reject(changed.ToJsonString(), password); }
        collection.Clear(); string empty = Encrypt(model, password); Reject(empty, "wrong"); Decrypt(empty, password); collection.Add(account);
        var crypto = app.GetType("Project2FA.Core.Services.Crypto.CryptoService", true)!;
        var serializer = Activator.CreateInstance(app.GetType("Project2FA.Services.SerializationCryptoService", true)!)!;
        foreach (int version in new[] { 0, 1, 2, 3 })
        {
            byte[] iv = RandomNumberGenerator.GetBytes(16), salt = RandomNumberGenerator.GetBytes(16), key;
            var method = crypto.GetMethod("CreateByteArrayKeyV" + (version == 0 ? 1 : version))!;
            key = (byte[])method.Invoke(null, version == 3 ? new object[] { Encoding.UTF8.GetBytes(password), salt, 32, 25000 } : new object[] { Encoding.UTF8.GetBytes(password), 32, version < 2 ? 1000 : 25000 })!;
            modelType.GetProperty("IV")!.SetValue(model, iv); modelType.GetProperty("Salt")!.SetValue(model, salt); modelType.GetProperty("Version")!.SetValue(model, version);
            string legacy = (string)serializer.GetType().GetMethod("SerializeEncrypt")!.Invoke(serializer, new[] { key, iv, model, (object)version })!;
            object restored = Decrypt(legacy, password);
            try { codec.GetMethod("DecryptCurrent", flags)!.Invoke(null, new object[] { legacy, password, "vault-synthetic" }); throw new Exception("Authenticated vault downgrade accepted."); }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidDataException) { }
            if (!(bool)equal.Invoke(null, new[] { model, restored })!) throw new Exception("Legacy migration lost data.");
            string migrated = Encrypt(restored, password);
            if (!(bool)equal.Invoke(null, new[] { model, Decrypt(migrated, password) })!) throw new Exception("Migration failed.");
        }
        string id = (string)codec.GetMethod("NewCredentialId", flags)!.Invoke(null, null)!;
        if (!id.StartsWith("vault-") || id.Length != 70) throw new Exception("Credential ID is not random sized.");
        string fixtureRead = Environment.GetEnvironmentVariable("TWOFAST_FIXTURE_READ");
        if (!string.IsNullOrEmpty(fixtureRead) && !(bool)equal.Invoke(null, new[] { model, Decrypt(File.ReadAllText(fixtureRead), password) })!)
            throw new Exception("Cross-build vault interchange changed account data.");
        string fixtureWrite = Environment.GetEnvironmentVariable("TWOFAST_FIXTURE_WRITE");
        if (!string.IsNullOrEmpty(fixtureWrite))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(fixtureWrite))!);
            File.WriteAllText(fixtureWrite, Encrypt(model, password));
        }
        Console.WriteLine("Authenticated vault: complete metadata/empty vault round trips, tamper and wrong-password rejection, fresh salt/nonce, V0–V3 migrations and random credential identifiers passed.");
    }
}
