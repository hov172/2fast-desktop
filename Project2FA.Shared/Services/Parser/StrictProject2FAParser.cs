#if TWOFAST_DESKTOP
using System.Globalization;
using System.Text.RegularExpressions;
using Project2FA.Services.Desktop;

namespace Project2FA.Services.Parser;

// Treat camera payloads as untrusted input. Decode each component once, without
// unescaping the entire URI (which would turn escaped '&' into parameter separators).
internal sealed class StrictProject2FAParser : IProject2FAParser
{
    public List<KeyValuePair<string, string>> ParseQRCodeStr(string qrCodeStr)
        => TryParse(qrCodeStr, out var values) ? values : new List<KeyValuePair<string, string>>();

    public List<KeyValuePair<string, string>> ParseCmdStr(string cmdStr)
        => ParseQRCodeStr(cmdStr);

    internal static bool TryParse(string text, out List<KeyValuePair<string, string>> values)
        => TryParse(text, out values, out _);

    internal static bool TryParse(string text, out List<KeyValuePair<string, string>> values, out string error, string? authorizationCode = null)
    {
        error = "Invalid or unsupported authenticator QR. Expected a TOTP, OCRA, or Deepnet MobileID setup code.";
        if (text?.StartsWith("mobileid://", StringComparison.OrdinalIgnoreCase) == true)
            return DesktopMobileId.TryParse(text, authorizationCode, out values, out error);
        values = new();
        if (string.IsNullOrEmpty(text) || text.Length > 16384 ||
            !Uri.TryCreate(text, UriKind.Absolute, out var uri) ||
            !uri.Scheme.Equals("otpauth", StringComparison.OrdinalIgnoreCase) ||
            !(uri.Host.Equals("totp", StringComparison.OrdinalIgnoreCase) || uri.Host.Equals("ocra", StringComparison.OrdinalIgnoreCase)) ||
            uri.UserInfo.Length != 0 || !uri.IsDefaultPort || uri.Fragment.Length != 0) return false;
        try
        {
            var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string part in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var pair = part.Split('=', 2);
                if (pair.Length != 2) return false;
                string key = Uri.UnescapeDataString(pair[0]);
                string value = Uri.UnescapeDataString(pair[1]);
                if (!query.TryAdd(key, value)) return false;
            }
            bool ocra = uri.Host.Equals("ocra", StringComparison.OrdinalIgnoreCase);
            if (ocra && query.GetValueOrDefault("suite") != "OCRA-1:HOTP-SHA1-6:QN08-T1M") return false;
            if (!query.TryGetValue("secret", out var secret)) return false;
            secret = secret.ToUpperInvariant();
            if (!Regex.IsMatch(secret, "^[A-Z2-7]{16,1024}={0,6}$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100))) return false;
            // Validate the actual decoder too, including malformed padding/length.
            var decoded = OtpNet.Base32Encoding.ToBytes(secret);
            try
            {
                if (decoded.Length < 10 || OtpNet.Base32Encoding.ToString(decoded).TrimEnd('=') != secret.TrimEnd('=')) return false;
                if (secret.Contains('=') && secret.Length % 8 != 0) return false;
            }
            finally { System.Security.Cryptography.CryptographicOperations.ZeroMemory(decoded); }
            string algorithm = query.GetValueOrDefault("algorithm", "SHA1").ToUpperInvariant();
            if (algorithm is not ("SHA1" or "SHA256" or "SHA512")) return false;
            if (!int.TryParse(query.GetValueOrDefault("digits", "6"), NumberStyles.None, CultureInfo.InvariantCulture, out int digits) || digits is not (6 or 8)) return false;
            if (!int.TryParse(query.GetValueOrDefault("period", ocra ? "60" : "30"), NumberStyles.None, CultureInfo.InvariantCulture, out int period) || period < 1 || period > 86400) return false;
            if (ocra && (algorithm != "SHA1" || digits != 6 || period != 60)) return false;
            string label = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
            if (label.Length == 0 || label.Length > 512 || label.Any(char.IsControl)) return false;
            var labelParts = label.Split(':', 2);
            string issuer = query.GetValueOrDefault("issuer", labelParts.Length == 2 ? labelParts[0] : string.Empty);
            if (issuer.Length > 512 || issuer.Any(char.IsControl)) return false;
            if (labelParts.Length == 2 && issuer.Length != 0 && issuer != labelParts[0]) return false;
            string account = labelParts.Length == 2 ? labelParts[1] : label;
            if (string.IsNullOrWhiteSpace(account)) return false;
            // Existing 2fast model calls the service name 'label' and account name 'issuer'.
            values = new()
            {
                new("auth", ocra ? "ocra" : "totp"), new("label", string.IsNullOrWhiteSpace(issuer) ? account : issuer), new("issuer", account),
                new("secret", secret), new("algorithm", algorithm),
                new("digits", digits.ToString(CultureInfo.InvariantCulture)),
                new("period", period.ToString(CultureInfo.InvariantCulture))
            };
            if (ocra) values.Add(new("ocrasuite", "OCRA-1:HOTP-SHA1-6:QN08-T1M"));
            return true;
        }
        catch (Exception e) when (e is UriFormatException or FormatException or ArgumentException or RegexMatchTimeoutException) { return false; }
    }
}
#endif
