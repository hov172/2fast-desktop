// Covers Project2FA.Shared/Services/Importer/BackupCryptoHelper.cs, the scaffolding
// shared by the andOTP and 2FAS backup importers. Every check cross-references an
// independent implementation (.NET's own PBKDF2 and AES-GCM) rather than pinning
// BouncyCastle against itself, so a wrong digest or key length cannot pass.
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using OtpNet;
using Project2FA.Services.Importer;

int passed = 0;
void Check(bool condition, string what) { if (!condition) throw new Exception("FAIL: " + what); passed++; }
void CheckBytes(byte[] actual, byte[] expected, string what) => Check(actual.SequenceEqual(expected), what);

// --- constants the importers depend on ---------------------------------------
Check(BackupCryptoHelper.AlgorithmDescription == "AES/GCM/NoPadding", "cipher string is AES/GCM/NoPadding");
Check(BackupCryptoHelper.KeyLength == 32, "derived keys are 256-bit");

// --- DeriveKey matches .NET's PBKDF2 for both digests the importers use -------
var cases = new (string password, string salt, int iterations)[]
{
    ("password", "salt", 1),
    ("password", "salt", 4096),
    ("correct horse battery staple", "0123456789ab", 10000),   // 2FAS iteration count
    ("", "salt", 1000),                                        // empty password
    ("passwordPASSWORDpassword", "saltSALTsaltSALTsalt", 2048),
};
foreach (var (password, salt, iterations) in cases)
{
    byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
    byte[] saltBytes = Encoding.UTF8.GetBytes(salt);

    byte[] sha1 = BackupCryptoHelper.DeriveKey(new Sha1Digest(), passwordBytes, saltBytes, iterations).GetKey();
    byte[] sha1Expected = Rfc2898DeriveBytes.Pbkdf2(passwordBytes, saltBytes, iterations, HashAlgorithmName.SHA1, 32);
    CheckBytes(sha1, sha1Expected, $"SHA1 PBKDF2 matches .NET ({password.Length}/{iterations})");

    byte[] sha256 = BackupCryptoHelper.DeriveKey(new Sha256Digest(), passwordBytes, saltBytes, iterations).GetKey();
    byte[] sha256Expected = Rfc2898DeriveBytes.Pbkdf2(passwordBytes, saltBytes, iterations, HashAlgorithmName.SHA256, 32);
    CheckBytes(sha256, sha256Expected, $"SHA256 PBKDF2 matches .NET ({password.Length}/{iterations})");

    Check(sha1.Length == 32 && sha256.Length == 32, "derived key is 32 bytes");
    Check(!sha1.SequenceEqual(sha256), "digest choice changes the derived key");
}

// The digest is a parameter, not a default: andOTP derives with SHA1, 2FAS with SHA256.
// Swapping them must not silently produce the same key.
byte[] pw = Encoding.UTF8.GetBytes("shared-password"), st = Encoding.UTF8.GetBytes("shared-salt");
Check(!BackupCryptoHelper.DeriveKey(new Sha1Digest(), pw, st, 10000).GetKey()
       .SequenceEqual(BackupCryptoHelper.DeriveKey(new Sha256Digest(), pw, st, 10000).GetKey()),
    "andOTP (SHA1) and 2FAS (SHA256) derivations stay distinct");

// --- the cipher string really is AES-256-GCM ----------------------------------
// Decrypt, with .NET, a payload BouncyCastle produced through the shared constant.
byte[] key = BackupCryptoHelper.DeriveKey(new Sha256Digest(), pw, st, 10000).GetKey();
byte[] iv = Convert.FromHexString("000102030405060708090a0b");
byte[] plaintext = Encoding.UTF8.GetBytes("[{\"secret\":\"JBSWY3DPEHPK3PXP\"}]");

var cipher = CipherUtilities.GetCipher(BackupCryptoHelper.AlgorithmDescription);
cipher.Init(true, new ParametersWithIV(new KeyParameter(key), iv));
byte[] sealedBytes = cipher.DoFinal(plaintext);

byte[] ciphertext = sealedBytes[..^16], tag = sealedBytes[^16..], roundTripped = new byte[plaintext.Length];
using (var aes = new AesGcm(key, 16)) aes.Decrypt(iv, ciphertext, tag, roundTripped);
CheckBytes(roundTripped, plaintext, "BouncyCastle AES/GCM/NoPadding decrypts under .NET AesGcm");

// A tampered payload must fail, not return garbage — this is what tells an importer
// that the password was wrong rather than handing back corrupt account data.
sealedBytes[0] ^= 0xFF;
var tampered = CipherUtilities.GetCipher(BackupCryptoHelper.AlgorithmDescription);
tampered.Init(false, new ParametersWithIV(new KeyParameter(key), iv));
try { tampered.DoFinal(sealedBytes); throw new Exception("FAIL: tampered payload accepted"); }
catch (Org.BouncyCastle.Crypto.InvalidCipherTextException) { passed++; }

// --- ToHashMode ---------------------------------------------------------------
Check(BackupCryptoHelper.ToHashMode("SHA1") == OtpHashMode.Sha1, "SHA1 maps to Sha1");
Check(BackupCryptoHelper.ToHashMode("SHA256") == OtpHashMode.Sha256, "SHA256 maps to Sha256");
Check(BackupCryptoHelper.ToHashMode("SHA512") == OtpHashMode.Sha512, "SHA512 maps to Sha512");
foreach (string unsupported in new[] { "sha1", "MD5", "SHA-256", "", "SHA384" })
{
    try { BackupCryptoHelper.ToHashMode(unsupported); throw new Exception($"FAIL: '{unsupported}' accepted"); }
    catch (ArgumentException) { passed++; }
}
try { BackupCryptoHelper.ToHashMode(null!); throw new Exception("FAIL: null algorithm accepted"); }
catch (ArgumentException) { passed++; }

Console.WriteLine($"Importer crypto: {passed} checks passed (PBKDF2 cross-checked against .NET, AES-GCM round trip, hash mapping).");
