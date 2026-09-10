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
    private string validationMessage = string.Empty;
    public string ValidationMessage
    {
        get => validationMessage;
        private set => SetProperty(ref validationMessage, value);
    }

    public async Task InitializeDesktopLocation()
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
        ValidateDesktopInputs();
    }

    private void ValidateDesktopInputs()
    {
        var name = DateFileName ?? string.Empty;
        var result = DesktopInputValidation.Validate(name, Password, PasswordRepeat, SelectWebDAV,
            LocalStorageFolder != null || LocalStorageFile != null,
            LocalStorageFolder != null && File.Exists(Path.Combine(LocalStorageFolder.Path, name.EndsWith(".2fa", StringComparison.OrdinalIgnoreCase) ? name : name + ".2fa")));
        PasswordWhitespaceHint = result.WhitespaceHint;
        ValidationMessage = result.Message;
        DatafileBTNActive = result.IsValid;
    }
}
#endif
