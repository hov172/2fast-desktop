#if TWOFAST_DESKTOP
using Project2FA.Services;
using Project2FA.Services.MacOS;
using Project2FA.UnoApp;
using UNOversal.Ioc;
using UNOversal.Services.Dialogs;
using Windows.Storage.Pickers;
namespace Project2FA.Uno.Views;
public sealed partial class SettingPage
{
    private bool fileActionRunning;
    private async Task FileAction(Func<Task> action)
    {
        if (fileActionRunning) return;
        fileActionRunning = true; foreach (var button in DatafileActions.Children.OfType<Button>()) button.IsEnabled = false; DatafileStatus.Text = "";
        try { await action(); }
        catch (Exception error)
        {
            MacOSScanDiagnostics.Record("data-file action", error);
            DatafileStatus.Text = error is IOException || error is ArgumentException || error is InvalidOperationException
                ? error.Message : "The file operation could not be completed. Check access to the selected folder.";
        }
        finally { foreach (var button in DatafileActions.Children.OfType<Button>()) button.IsEnabled = true; fileActionRunning = false; }
    }
    private async Task RefreshFileDetails()
    {
        var file = await DataService.Instance.CurrentMacOSVault();
        ViewModel.DatafilePartViewModel.DatafileName = file.Name;
        ViewModel.DatafilePartViewModel.DatafilePath = file.Path;
    }
    private async Task<string> PickFolder()
    {
        var picker = new FolderPicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
        picker.FileTypeFilter.Add("*");
        return (await picker.PickSingleFolderAsync())?.Path;
    }
    private async Task<bool> ConfirmLocalMove()
    {
        if (DataService.Instance.ActivatedDatafile != null || !SettingsService.Instance.DataFileWebDAVEnabled) return true;
        var dialog = new ContentDialog { Title = "Use a local vault", XamlRoot = XamlRoot, PrimaryButtonText = "Continue locally", CloseButtonText = "Cancel",
            Content = new TextBlock { Text = "This operation moves or renames the local copy and stops WebDAV syncing. The server copy stays at its current location.", TextWrapping = TextWrapping.Wrap, MaxWidth = 440 } };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }
    private async void RenameVault_Click(object sender, RoutedEventArgs args) => await FileAction(async () =>
    {
        if (!await ConfirmLocalMove()) return;
        var file = await DataService.Instance.CurrentMacOSVault();
        var name = new TextBox { Header = "New filename", Text = file.Name };
        var dialog = new ContentDialog { Title = "Rename data file", Content = name, PrimaryButtonText = "Rename", CloseButtonText = "Cancel", XamlRoot = XamlRoot };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        await DataService.Instance.CopyMacOSVault(Path.GetDirectoryName(file.Path), name.Text, true);
        await RefreshFileDetails(); DatafileStatus.Text = "Data file renamed. Enable biometric unlock again if you use it.";
    });
    private async void MoveVault_Click(object sender, RoutedEventArgs args) => await FileAction(async () =>
    {
        if (!await ConfirmLocalMove()) return;
        string folder = await PickFolder(); if (folder == null) return;
        var file = await DataService.Instance.CurrentMacOSVault();
        await DataService.Instance.CopyMacOSVault(folder, file.Name, true);
        await RefreshFileDetails(); DatafileStatus.Text = "Data file moved. Enable biometric unlock again if you use it.";
    });
    private async void BackupVault_Click(object sender, RoutedEventArgs args) => await FileAction(async () =>
    {
        string folder = await PickFolder(); if (folder == null) return;
        var file = await DataService.Instance.CurrentMacOSVault();
        string name = Path.GetFileNameWithoutExtension(file.Name) + "-backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".2fa";
        string path = await DataService.Instance.CopyMacOSVault(folder, name, false);
        DatafileStatus.Text = "Encrypted backup saved to " + path;
    });
    private async void PasswordVault_Click(object sender, RoutedEventArgs args) => await FileAction(async () =>
    {
        var dialog = new ChangeDatafilePasswordContentDialog { XamlRoot = XamlRoot };
        await App.Current.Container.Resolve<IDialogService>().ShowDialogAsync(dialog, new DialogParameters());
        if (dialog.ViewModel.PasswordChanged) DatafileStatus.Text = "Password changed. Use the new password next time you open this data file.";
    });
    private async void UpgradeVault_Click(object sender, RoutedEventArgs args) => await FileAction(async () =>
    {
        var file = await DataService.Instance.CurrentMacOSVault();
        if (MacOSVaultCodec.IsModern(await File.ReadAllTextAsync(file.Path))) { DatafileStatus.Text = "This vault already uses authenticated encryption."; return; }
        var dialog = new ContentDialog
        {
            Title = "Upgrade vault encryption", XamlRoot = XamlRoot, PrimaryButtonText = "Upgrade", CloseButtonText = "Cancel",
            Content = new TextBlock { Text = "The upgraded vault works with the updated 2fast desktop app on Windows and macOS. Older releases and mobile clients cannot open it. An encrypted legacy copy will be kept beside the file. Your password stays the same; Biometric unlock must be enabled again. For WebDAV, the remote vault is upgraded too.", TextWrapping = TextWrapping.Wrap, MaxWidth = 440 }
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        await DataService.Instance.UpgradeMacOSVault();
        DatafileStatus.Text = "Vault upgraded. The encrypted legacy copy is preserved beside the data file.";
    });
    private async void OpenVault_Click(object sender, RoutedEventArgs args) => await FileAction(async () =>
    {
        if (!await DataService.Instance.WriteLocalDatafile()) return;
        await ViewModel.NavigationService.NavigateAsync(nameof(UseDataFilePage));
    });
    private async void NewVault_Click(object sender, RoutedEventArgs args) => await FileAction(async () =>
    {
        if (!await DataService.Instance.WriteLocalDatafile()) return;
        await ViewModel.NavigationService.NavigateAsync(nameof(NewDataFilePage));
    });
}
#endif
