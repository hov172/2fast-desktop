#if TWOFAST_DESKTOP
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Project2FA.Services.Desktop;

internal static class DesktopOcra
{
    internal const string Suite = "OCRA-1:HOTP-SHA1-6:QN08-T1M";

    // The two additional profiles are used to verify the core against RFC 6287 appendix C.
    internal static string Compute(byte[] key, string challenge, DateTimeOffset now, string suite = Suite)
    {
        if (key == null || key.Length == 0) throw new ArgumentException("A token seed is required.");
        if (string.IsNullOrEmpty(challenge) || challenge.Length > 8 || challenge.Any(c => c < '0' || c > '9'))
            throw new ArgumentException("Enter a numeric challenge of 1–8 digits.");
        bool timed = suite == Suite || suite == "OCRA-1:HOTP-SHA512-8:QN08-T1M";
        if (!timed && suite != "OCRA-1:HOTP-SHA1-6:QN08") throw new ArgumentException("Unsupported OCRA suite.");
        if (now.ToUnixTimeSeconds() < 0) throw new ArgumentOutOfRangeException(nameof(now));
        byte[] prefix = Encoding.ASCII.GetBytes(suite);
        byte[] message = new byte[prefix.Length + 1 + 128 + (timed ? 8 : 0)];
        prefix.CopyTo(message, 0);
        // Match RFC 6287 reference encoding: decimal -> hexadecimal, right padded to 128 bytes.
        string hex = uint.Parse(challenge, CultureInfo.InvariantCulture).ToString("x", CultureInfo.InvariantCulture).PadRight(256, '0');
        Convert.FromHexString(hex).CopyTo(message, prefix.Length + 1);
        if (timed)
        {
            ulong timestamp = (ulong)(now.ToUnixTimeSeconds() / 60);
            for (int i = 0; i < 8; i++) message[message.Length - 1 - i] = (byte)(timestamp >> (8 * i));
        }
        using HMAC hmac = suite.Contains("SHA512") ? new HMACSHA512(key) : new HMACSHA1(key);
        byte[] digest = hmac.ComputeHash(message);
        try
        {
            int offset = digest[^1] & 15;
            int binary = ((digest[offset] & 127) << 24) | (digest[offset + 1] << 16) |
                         (digest[offset + 2] << 8) | digest[offset + 3];
            int digits = suite.Contains("SHA512") ? 8 : 6;
            return (binary % (digits == 8 ? 100000000 : 1000000)).ToString("D" + digits, CultureInfo.InvariantCulture);
        }
        finally { CryptographicOperations.ZeroMemory(message); CryptographicOperations.ZeroMemory(digest); }
    }
}
#endif
