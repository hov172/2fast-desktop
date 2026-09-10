#if TWOFAST_DESKTOP
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Project2FA.Core.Services.Crypto;
using Project2FA.Repository.Models;

namespace Project2FA.Services.Desktop;

// V4 protects the complete serialized vault, including empty collections and metadata.
internal static class DesktopVaultCodec
{
    internal const int Iterations = 600_000;
    private const int MaxBytes = 32 * 1024 * 1024;
    private static readonly byte[] AssociatedData = Encoding.UTF8.GetBytes("2fast:v4:AES-256-GCM:PBKDF2-SHA256:600000");
    private static readonly JsonSerializerOptions PlainOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        TypeInfoResolver = new SerializationContext().WithAddedModifier(info =>
        {
            foreach (var property in info.Properties)
                if (property.CustomConverter is EncryptedStringConverter or Serialization.EncryptedBytesConverter)
                    property.CustomConverter = null;
        })
    };
    internal static bool IsModern(string content) => Version(content) == 4;
    private static int Version(string content)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Length > MaxBytes * 2)
            throw new InvalidDataException("The vault is empty or exceeds the supported size.");
        using var doc = JsonDocument.Parse(content);
        foreach (var p in doc.RootElement.EnumerateObject())
            if (p.Name.Equals("Version", StringComparison.OrdinalIgnoreCase)) return p.Value.GetInt32();
        return 0;
    }
    internal static string Encrypt(DatafileModel model, string password)
    {
        if (string.IsNullOrEmpty(password)) throw new ArgumentException("A vault password is required.");
        byte[] plain = JsonSerializer.SerializeToUtf8Bytes(model, (JsonTypeInfo<DatafileModel>)PlainOptions.GetTypeInfo(typeof(DatafileModel)));
        byte[] salt = RandomNumberGenerator.GetBytes(32), nonce = RandomNumberGenerator.GetBytes(12);
        byte[] key = Derive(password, salt), cipher = new byte[plain.Length], tag = new byte[16];
        try
        {
            if (plain.Length > MaxBytes) throw new InvalidDataException("The vault exceeds the supported size.");
            using var aes = new AesGcm(key, 16);
            aes.Encrypt(nonce, plain, cipher, tag, AssociatedData);
            return JsonSerializer.Serialize(new VaultEnvelope { Salt = salt, Nonce = nonce, Tag = tag, Data = cipher }, VaultEnvelopeContext.Default.VaultEnvelope);
        }
        finally { CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(plain); }
    }
    internal static DatafileModel VerifyCredential(string content, string password, string credentialId)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(credentialId)) throw new CryptographicException("A vault password is required.");
        if (!credentialId.StartsWith("vault-", StringComparison.Ordinal) && CryptoService.CreateStringHash(password) != credentialId)
            throw new CryptographicException("The vault password is incorrect.");
        return DecryptCurrent(content, password, credentialId);
    }
    internal static DatafileModel DecryptCurrent(string content, string password, string credentialId)
    {
        if (credentialId?.StartsWith("vault-", StringComparison.Ordinal) == true && !IsModern(content))
            throw new InvalidDataException("An authenticated vault was replaced by a legacy file. Open that file explicitly if this change was intentional.");
        return Decrypt(content, password);
    }
    internal static DatafileModel Decrypt(string content, string password)
    {
        int version = Version(content);
        if (version != 4)
        {
            if (version < 0 || version > 3) throw new InvalidDataException("This vault format is not supported. Update 2fast before opening it.");
            var header = new SerializationService().Deserialize<DatafileModel>(content);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password ?? ""), key = null;
            try
            {
                key = version switch
                {
                    0 or 1 => CryptoService.CreateByteArrayKeyV1(passwordBytes),
                    2 => CryptoService.CreateByteArrayKeyV2(passwordBytes),
                    _ => CryptoService.CreateByteArrayKeyV3(passwordBytes, header.Salt)
                };
                return Validate(new SerializationCryptoService().DeserializeDecrypt<DatafileModel>(key, header.IV, content, version));
            }
            finally { CryptographicOperations.ZeroMemory(passwordBytes); if (key != null) CryptographicOperations.ZeroMemory(key); }
        }
        var envelope = JsonSerializer.Deserialize(content, VaultEnvelopeContext.Default.VaultEnvelope)
            ?? throw new InvalidDataException("Invalid vault envelope.");
        if (envelope.Version != 4 || envelope.Algorithm != "AES-256-GCM" || envelope.Kdf != "PBKDF2-SHA256" || envelope.Iterations != Iterations ||
            envelope.Salt?.Length != 32 || envelope.Nonce?.Length != 12 || envelope.Tag?.Length != 16 || envelope.Data == null || envelope.Data.Length > MaxBytes)
            throw new InvalidDataException("Invalid or unsupported vault encryption parameters.");
        byte[] derived = Derive(password ?? "", envelope.Salt), plainBytes = new byte[envelope.Data.Length];
        try
        {
            using var aes = new AesGcm(derived, 16);
            aes.Decrypt(envelope.Nonce, envelope.Data, envelope.Tag, plainBytes, AssociatedData);
            return Validate(JsonSerializer.Deserialize(plainBytes, (JsonTypeInfo<DatafileModel>)PlainOptions.GetTypeInfo(typeof(DatafileModel))));
        }
        finally { CryptographicOperations.ZeroMemory(derived); CryptographicOperations.ZeroMemory(plainBytes); }
    }
    private static DatafileModel Validate(DatafileModel model)
    {
        if (model?.Collection == null) throw new InvalidDataException("The vault has no account collection.");
        model.GlobalCategories ??= new();
        return model;
    }
    private static byte[] Derive(string password, byte[] salt)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(password);
        try { return Rfc2898DeriveBytes.Pbkdf2(bytes, salt, Iterations, HashAlgorithmName.SHA256, 32); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }
    internal static bool SameAccounts(DatafileModel left, DatafileModel right)
    {
        byte[] Snapshot(DatafileModel model) => JsonSerializer.SerializeToUtf8Bytes(
            new DatafileModel { Collection = model.Collection, GlobalCategories = model.GlobalCategories },
            (JsonTypeInfo<DatafileModel>)PlainOptions.GetTypeInfo(typeof(DatafileModel)));
        byte[] a = Snapshot(left), b = Snapshot(right);
        try { return CryptographicOperations.FixedTimeEquals(SHA256.HashData(a), SHA256.HashData(b)); }
        finally { CryptographicOperations.ZeroMemory(a); CryptographicOperations.ZeroMemory(b); }
    }
    internal static string NewCredentialId() => "vault-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
}
internal sealed class VaultEnvelope
{
    public int Version { get; set; } = 4;
    public string Algorithm { get; set; } = "AES-256-GCM";
    public string Kdf { get; set; } = "PBKDF2-SHA256";
    public int Iterations { get; set; } = DesktopVaultCodec.Iterations;
    public byte[] Salt { get; set; }
    public byte[] Nonce { get; set; }
    public byte[] Tag { get; set; }
    public byte[] Data { get; set; }
}
[JsonSerializable(typeof(VaultEnvelope))]
internal partial class VaultEnvelopeContext : JsonSerializerContext { }
#endif
