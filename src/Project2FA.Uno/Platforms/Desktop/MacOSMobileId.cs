#if TWOFAST_DESKTOP
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Project2FA.Services.MacOS;

// Interoperability decoder for Deepnet MobileID 6 offline install URIs.
// Legacy RC4/MD5 is used only to unwrap this vendor's provisioning envelope.
// Imported accounts use the app's normal encrypted vault, not this envelope.
internal static class MacOSMobileId
{
    internal static bool TryParse(string text, string? authorizationCode, out List<KeyValuePair<string, string>> values, out string error)
    {
        values = new(); error = "Invalid Deepnet MobileID setup QR.";
        byte[]? cipher = null, wrappingKey = null, plain = null, key = null;
        try
        {
            if (text.Length > 16384 || !Uri.TryCreate(text, UriKind.Absolute, out var uri) ||
                uri.Scheme != "mobileid" || uri.AbsolutePath != "/mobileid/install" || uri.UserInfo.Length != 0 ||
                !uri.IsDefaultPort || uri.Fragment.Length != 0 || uri.Host.Length == 0) return false;
            var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var part in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var pair = part.Split('=', 2);
                if (pair.Length != 2 || !query.TryAdd(Uri.UnescapeDataString(pair[0]), Uri.UnescapeDataString(pair[1]))) return false;
            }
            if (query.GetValueOrDefault("suite") != MacOSOcra.Suite)
            { error = "This MobileID OCRA suite is not supported."; return false; }
            bool hasPushRegistration = query.ContainsKey("sid") || query.ContainsKey("pn") || query.ContainsKey("regurl");
            // Registration is a separate vendor step. Import only the offline token;
            // never fetch QR-supplied URLs or retain device-registration credentials.
            string serial = query.GetValueOrDefault("sn", "");
            if (serial.Length is < 1 or > 10 || serial.Any(c => c < '0' || c > '9') || !int.TryParse(serial, out _)) return false;
            string ac = authorizationCode ?? query.GetValueOrDefault("ac", "");
            if (ac.Length == 0) { error = "Authorization code required"; return false; }
            if (ac.Length > 1024) return false;
            cipher = Convert.FromBase64String(query.GetValueOrDefault("seed", ""));
            if (cipher.Length != 18) return false;
            wrappingKey = MD5.HashData(Encoding.UTF8.GetBytes(ac));
            plain = Unwrap(cipher, wrappingKey);
            if (plain.Any(b => b < '0' || b > '9')) { error = "The authorization code or token seed is incorrect."; return false; }
            string seed = Encoding.ASCII.GetString(plain);
            if (seed[1] - '0' != Checksum(serial) || seed[17] - '0' != Checksum(seed[..17]))
            { error = "The authorization code, serial number, or token checksum is incorrect."; return false; }
            int profile = int.Parse(seed.Substring(2, 5), CultureInfo.InvariantCulture);
            // Version 1, time-based OCRA-capable token. Device-locked and mutual-auth profiles need MobileID.
            if (seed[0] != '1') { error = "This is a legacy MobileID token profile."; return false; }
            if ((profile & 3) != 1) { error = "This MobileID token is not time-based."; return false; }
            bool deviceBound = (profile & 16) != 0;
            if ((profile & 8) != 0) { error = "This MobileID token enables mutual server authentication."; return false; }
            int digits = ((profile >> 5) & 7) + 5;
            if (digits > 8) return false;
            int period = (((profile >> 10) & 3) + 1) * 60;
            string name = query.GetValueOrDefault("tn", serial);
            if (string.IsNullOrWhiteSpace(name)) name = serial;
            if (name.Length > 512 || name.Any(char.IsControl)) return false;
            key = SHA1.HashData(plain.AsSpan(7, 10));
            values = new() { new("label", "Deepnet MobileID"), new("issuer", name),
                new("secret", OtpNet.Base32Encoding.ToString(key)), new("algorithm", "SHA1"),
                new("digits", digits.ToString(CultureInfo.InvariantCulture)), new("period", period.ToString(CultureInfo.InvariantCulture)),
                new("ocrasuite", MacOSOcra.Suite), new("mobileid", (profile & 4) != 0 ? "checksum" : "plain") };
            if (deviceBound) values.Add(new("mobileidbinding", "this-mac"));
            if (hasPushRegistration) values.Add(new("importnotice", "Offline OTP and OCRA imported. Push approvals are not enrolled; use Deepnet MobileID for push requests."));
            error = ""; return true;
        }
        catch (Exception e) when (e is ArgumentException or FormatException or OverflowException) { return false; }
        finally
        {
            foreach (var bytes in new[] { cipher, wrappingKey, plain, key }) if (bytes != null) CryptographicOperations.ZeroMemory(bytes);
        }
    }
    private static byte[] Unwrap(byte[] encrypted, byte[] key)
    {
        var cipher = new Org.BouncyCastle.Crypto.Engines.RC4Engine();
        cipher.Init(false, new Org.BouncyCastle.Crypto.Parameters.KeyParameter(key));
        var plain = new byte[encrypted.Length];
        cipher.ProcessBytes(encrypted, 0, encrypted.Length, plain, 0);
        return plain;
    }
    internal static int Checksum(string digits)
    {
        int sum = 0; bool twice = true;
        for (int i = digits.Length - 1; i >= 0; i--)
        {
            int value = digits[i] - '0'; if (twice) { value *= 2; if (value > 9) value -= 9; }
            sum += value; twice = !twice;
        }
        return (10 - sum % 10) % 10;
    }
    internal static string ComputeOtp(byte[] key, int period, int digits, bool checksum, DateTimeOffset now)
    {
        if (period <= 0 || digits < 5 || digits > 8 || now.ToUnixTimeSeconds() < 0) throw new ArgumentException("Invalid MobileID profile.");
        byte[] counter = new byte[8]; ulong step = (ulong)(now.ToUnixTimeSeconds() / period);
        for (int i = 0; i < 8; i++) counter[7 - i] = (byte)(step >> (8 * i));
        byte[] hash = HMACSHA1.HashData(key, counter);
        try
        {
            // MobileID uses fixed offset 16 for OTP; OCRA uses dynamic truncation.
            uint number = ((uint)(hash[16] & 127) << 24) | ((uint)hash[17] << 16) | ((uint)hash[18] << 8) | hash[19];
            string otp = (number % (uint)Math.Pow(10, digits)).ToString("D" + digits, CultureInfo.InvariantCulture);
            return checksum ? otp + Checksum(otp).ToString(CultureInfo.InvariantCulture) : otp;
        }
        finally { CryptographicOperations.ZeroMemory(hash); }
    }
}
#endif
