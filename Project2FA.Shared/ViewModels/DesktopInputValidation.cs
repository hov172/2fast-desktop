#if TWOFAST_DESKTOP
using System;

namespace Project2FA.ViewModels;

internal static class DesktopInputValidation
{
    internal static (string WhitespaceHint, string Message, bool IsValid) Validate(
        string name, string password, string repeat, bool selectWebDav, bool hasFolder, bool fileExists)
    {
        bool first = !string.IsNullOrEmpty(password) && char.IsWhiteSpace(password[^1]);
        bool second = !string.IsNullOrEmpty(repeat) && char.IsWhiteSpace(repeat[^1]);
        string hint = first && second ? "Both password fields end with whitespace. Remove it unless it is part of your intended password."
            : first ? "The first password ends with whitespace. Use Show passwords to check it."
            : second ? "The repeated password ends with whitespace. Use Show passwords to check it." : string.Empty;
        string message;
        if (string.IsNullOrWhiteSpace(name)) message = "Enter a filename.";
        else if (name.IndexOfAny(new[] { '/', '\\', '\0' }) >= 0 || name is "." or "..") message = "Enter a filename without path separators.";
        else if (string.IsNullOrEmpty(password)) message = "Enter a password for this data file.";
        else if (string.IsNullOrEmpty(repeat)) message = "Repeat your password.";
        else if (!string.Equals(password, repeat, StringComparison.Ordinal)) message = "The passwords do not match.";
        else if (!selectWebDav && !hasFolder) message = "Choose a folder using Change path.";
        else if (!selectWebDav && fileExists) message = "A data file with this name already exists. Choose another filename.";
        else message = string.Empty;
        return (hint, message, message.Length == 0);
    }
}
#endif
