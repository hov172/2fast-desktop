#if TWOFAST_DESKTOP
using System.Security.Cryptography;

namespace Project2FA.Services.Desktop;

/// <summary>
/// Builds the account values for an OCRA token.  Keeping seed decoding and
/// account metadata construction here leaves the desktop dialog responsible
/// only for collecting input and displaying validation feedback.
/// </summary>
internal static class DesktopOcraTokenFactory
{
    internal static bool TryCreate(
        string name,
        string seed,
        int encodingIndex,
        out List<KeyValuePair<string, string>>? values,
        out string error)
    {
        values = null;
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            error = "Enter a token name and valid seed in the selected encoding (at least 10 bytes).";
            return false;
        }

        byte[]? key = null;
        try
        {
            key = encodingIndex switch
            {
                0 => OtpNet.Base32Encoding.ToBytes(seed.Trim().ToUpperInvariant()),
                1 => Convert.FromBase64String(seed.Trim()),
                2 => Convert.FromHexString(seed.Trim()),
                _ => throw new FormatException()
            };
            if (key.Length < 10)
            {
                error = "Enter a token name and valid seed in the selected encoding (at least 10 bytes).";
                return false;
            }

            values = new()
            {
                new("label", "Deepnet"),
                new("issuer", name.Trim()),
                new("secret", OtpNet.Base32Encoding.ToString(key)),
                new("period", "60"),
                new("digits", "6"),
                new("algorithm", "SHA1"),
                new("ocrasuite", DesktopOcra.Suite)
            };
            return true;
        }
        catch (ArgumentException)
        {
            error = "Enter a token name and valid seed in the selected encoding (at least 10 bytes).";
            return false;
        }
        catch (FormatException)
        {
            error = "Enter a token name and valid seed in the selected encoding (at least 10 bytes).";
            return false;
        }
        finally
        {
            if (key != null) CryptographicOperations.ZeroMemory(key);
        }
    }
}
#endif
