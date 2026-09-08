#if TWOFAST_DESKTOP
using System.Security.Cryptography;
namespace Project2FA.Services.MacOS;
internal sealed class VaultSessionExpiredException : InvalidOperationException { }
internal static class VaultSaveErrors
{
    internal static string Describe(Exception error) => error switch
    {
        VaultSessionExpiredException => "Your vault is locked or the unlock session has ended. Return to Accounts, unlock the vault with its password, then try again.",
        FileNotFoundException or DirectoryNotFoundException => "The vault or its folder could not be found. Open the correct data file from Settings before saving again.",
        UnauthorizedAccessException => "2fast does not have permission to write to the vault folder. Check the folder permissions and the operating system's file-access settings, then retry.",
        CryptographicException => "The current vault could not be authenticated. Its password or contents may have changed outside this app. Reopen the correct file with its current password before retrying.",
        System.Net.Http.HttpRequestException => "The WebDAV server could not be reached. Check your connection and server settings, then retry. Keep any local recovery copy.",
        OperationCanceledException => "Saving was canceled. Unlock the vault again if the session ended, then retry.",
        IOException => "The vault could not be written, or a newer file conflicted with this save. Check file access and available space. For WebDAV, check the connection and reload before retrying. Preserve any recovery copy.",
        _ => "The vault could not be saved. Reopen it and try again. A diagnostic containing the error type and code location was recorded without account secrets."
    };
}
#endif
