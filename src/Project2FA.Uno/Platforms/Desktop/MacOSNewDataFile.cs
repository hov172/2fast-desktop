#if TWOFAST_DESKTOP
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Project2FA.ViewModels;

public partial class NewDataFilePageViewModel
{
    private string passwordWhitespaceHint = string.Empty;
    public string PasswordWhitespaceHint
    {
        get => passwordWhitespaceHint;
        private set => SetProperty(ref passwordWhitespaceHint, value);
    }
    private static bool HasTrailingWhitespace(string value) => !string.IsNullOrEmpty(value) && char.IsWhiteSpace(value[value.Length - 1]);

    private string validationMessage = string.Empty;
    public string ValidationMessage
    {
        get => validationMessage;
        private set => SetProperty(ref validationMessage, value);
    }

    public async Task InitializeMacOSLocation()
    {
        if (LocalStorageFolder != null || LocalStorageFile != null) return;
        try
        {
            LocalStorageFolder = await StorageFolder.GetFolderFromPathAsync(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            FolderPath = LocalStorageFolder.Path;
        }
        catch (Exception)
        {
            // The user can grant access to another location with the folder picker.
        }
        ValidateMacOSInputs();
    }

    private void ValidateMacOSInputs()
    {
        bool first = HasTrailingWhitespace(Password);
        bool repeat = HasTrailingWhitespace(PasswordRepeat);
        PasswordWhitespaceHint = first && repeat ? "Both password fields end with whitespace. Remove it unless it is part of your intended password."
            : first ? "The first password ends with whitespace. Use Show passwords to check it."
            : repeat ? "The repeated password ends with whitespace. Use Show passwords to check it." : string.Empty;
        var name = DateFileName ?? string.Empty;
        string message;
        if (string.IsNullOrWhiteSpace(name)) message = "Enter a filename.";
        else if (name.IndexOfAny(new[] { '/', '\\', '\0' }) >= 0 || name == "." || name == "..")
            message = "Enter a filename without path separators.";
        else if (string.IsNullOrEmpty(Password)) message = "Enter a password for this data file.";
        else if (string.IsNullOrEmpty(PasswordRepeat)) message = "Repeat your password.";
        else if (!string.Equals(Password, PasswordRepeat, StringComparison.Ordinal)) message = "The passwords do not match.";
        else if (!SelectWebDAV && LocalStorageFolder == null && LocalStorageFile == null)
            message = "Choose a folder using Change path.";
        else if (!SelectWebDAV && LocalStorageFolder != null &&
                 File.Exists(Path.Combine(LocalStorageFolder.Path, name.EndsWith(".2fa", StringComparison.OrdinalIgnoreCase) ? name : name + ".2fa")))
            message = "A data file with this name already exists. Choose another filename.";
        else message = string.Empty;
        ValidationMessage = message;
        DatafileBTNActive = message.Length == 0;
    }
}
#endif
