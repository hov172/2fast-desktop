#if TWOFAST_DESKTOP
using BiometryService;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Project2FA.Core;
using Project2FA.Core.Services.Crypto;
using Project2FA.Services;
using Project2FA.Services.Enums;
using Project2FA.Services.MacOS;
using Project2FA.Uno.Views;
using Project2FA.UnoApp;
using UNOversal.Navigation;
using UNOversal.Services.Dialogs;
using UNOversal.Services.Secrets;

namespace Project2FA.Services.MacOS
{
    internal static class MacOSSession
    {
        private static CancellationTokenSource lifetime = new();
        internal static CancellationToken Token => lifetime.Token;
        internal static void CancelOperations()
        {
            lifetime.Cancel();
            lifetime.Dispose();
            lifetime = new();
        }
        internal static void Lock()
        {
            CancelOperations();
            SecretHelper.ClearSession();
        }
        internal static Task Message(IDialogService service, string title, string text) => service.ShowDialogAsync(new ContentDialog
        {
            Title = title, Content = new TextBlock { Text = text, TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap },
            CloseButtonText = "OK", XamlRoot = App.ShellPageInstance.XamlRoot
        }, new DialogParameters());
    }
}

namespace Project2FA.ViewModels
{
    public partial class LoginPageViewModel
    {
        private readonly MacOSBiometryService macBiometry = new();
        private async Task MacOSPasswordLogin()
        {
            if (IsLoading || string.IsNullOrEmpty(Password)) return;
            try
            {
                IsLoading = true;
                if (!await CheckNavigationRequest(Password))
                {
                    Password = string.Empty;
                    await ShowLoginError();
                }
            }
            catch (Exception)
            {
                await MacOSSession.Message(DialogService, "Unable to unlock", "The secure credential store could not be opened. Check that this is a correctly signed build, then try again.");
            }
            finally { IsLoading = false; }
        }
        public async Task RefreshMacOSBiometry()
        {
            var capabilities = await macBiometry.GetCapabilities(CancellationToken.None);
            BiometricIsUsable = SettingsService.Instance.ActivateBiometricLogin && capabilities.IsSupported;
            if (BiometricIsUsable && !IsLogout && SettingsService.Instance.PreferBiometricLogin == BiometricPreferEnum.Prefer)
                await ((IAsyncRelayCommand)BiometricoLoginCommand).ExecuteAsync(null);
        }
        private async Task MacOSLoginTask()
        {
            if (IsLoading || !SettingsService.Instance.ActivateBiometricLogin) return;
            var token = MacOSSession.Token;
            string hash = SettingsService.Instance.DataFilePasswordHash;
            try
            {
                IsLoading = true;
                // SecItemCopyMatching enforces the biometric ACL before returning any secret.
                string password = await macBiometry.Decrypt(token, SecretHelper.BiometricKey(hash));
                token.ThrowIfCancellationRequested();
                if (hash != SettingsService.Instance.DataFilePasswordHash || !await CheckNavigationRequest(password))
                    await ShowLoginError();
            }
            catch (OperationCanceledException) { /* Keep password login available. */ }
            catch (BiometryException e)
            {
                if (e.Reason == BiometryExceptionReason.KeyInvalidated)
                {
                    SettingsService.Instance.ActivateBiometricLogin = false;
                    BiometricIsUsable = false;
                }
                await MacOSSession.Message(DialogService, DesktopPlatform.BiometricName, e.Message);
            }
            catch (Exception) { await MacOSSession.Message(DialogService, DesktopPlatform.BiometricName, "Unable to unlock with " + DesktopPlatform.BiometricName + ". Use your data-file password."); }
            finally { IsLoading = false; }
        }
    }

    public partial class SettingsPartViewModel
    {
        private bool changingMacBiometry;
        private CancellationTokenSource? macSettingsOperation;
        public void CancelMacOSBiometry() => macSettingsOperation?.Cancel();
        public Task RefreshMacOSBiometrySettings() => CheckBiometricLoginIsSupported();
        private async Task SetMacOSBiometry(bool enabled)
        {
            if (changingMacBiometry || enabled == _settings.ActivateBiometricLogin) return;
            changingMacBiometry = true;
            IsBiometricLoginSupported = false;
            string hash = _settings.DataFilePasswordHash;
            string key = SecretHelper.BiometricKey(hash);
            using var operation = CancellationTokenSource.CreateLinkedTokenSource(MacOSSession.Token);
            macSettingsOperation = operation;
            var token = operation.Token;
            var service = new MacOSBiometryService();
            bool enrolling = false;
            try
            {
                if (!enabled)
                {
                    service.Remove(key);
                    _settings.ActivateBiometricLogin = false;
                    _settings.PreferBiometricLogin = BiometricPreferEnum.No;
                }
                else
                {
                    var passwordBox = new PasswordBox { Header = "Data-file password", MaxLength = 0, PasswordRevealMode = PasswordRevealMode.Peek };
                    var dialog = new ContentDialog
                    {
                        Title = "Enable " + DesktopPlatform.BiometricName, Content = passwordBox,
                        PrimaryButtonText = "Continue", CloseButtonText = "Cancel",
                        XamlRoot = App.ShellPageInstance.XamlRoot
                    };
                    if (await DialogService.ShowDialogAsync(dialog, new DialogParameters()) != ContentDialogResult.Primary) return;
                    token.ThrowIfCancellationRequested();
                    string password = passwordBox.Password;
                    passwordBox.Password = string.Empty;
                    try
                    {
                        var file = await DataService.Instance.CurrentMacOSVault();
                        string content = await File.ReadAllTextAsync(file.Path);
                        await Task.Run(() => MacOSVaultCodec.VerifyCredential(content, password, hash), token);
                    }
                    catch (System.Security.Cryptography.CryptographicException)
                    {
                        await MacOSSession.Message(DialogService, DesktopPlatform.BiometricName, "The data-file password is incorrect.");
                        return;
                    }
                    enrolling = true;
                    await service.Encrypt(token, key, password);
                    token.ThrowIfCancellationRequested();
                    if (_settings.DataFilePasswordHash != hash) throw new OperationCanceledException();
                    _settings.ActivateBiometricLogin = true;
                    _settings.PreferBiometricLogin = BiometricPreferEnum.No;
                }
            }
            catch (OperationCanceledException)
            {
                if (enrolling) { try { service.Remove(key); } catch { } _settings.ActivateBiometricLogin = false; }
            }
            catch (Exception e)
            {
                if (enrolling) { try { service.Remove(key); } catch { } _settings.ActivateBiometricLogin = false; }
                await MacOSSession.Message(DialogService, DesktopPlatform.BiometricName, e is BiometryException ? e.Message : "Unable to change " + DesktopPlatform.BiometricName + " settings. Please try again.");
            }
            finally
            {
                changingMacBiometry = false;
                macSettingsOperation = null;
                await CheckBiometricLoginIsSupported();
                OnPropertyChanged(nameof(ActivateBiometricLogin));
                OnPropertyChanged(nameof(PreferBiometricLogin));
            }
        }
    }

    public partial class AccountCodePageViewModel
    {
        private bool macScanning;
        private Task CameraCommandTask() => ScanMacOSCode(false);
        public Task ScanMacOSScreen() => ScanMacOSCode(true);
        private async Task ScanMacOSCode(bool screen)
        {
            if (macScanning) return;
            macScanning = true;
            var token = MacOSSession.Token;
            string stage = "opening scanner";
            try
            {
                while (!token.IsCancellationRequested)
                {
                    stage = "capturing QR";
                    string? payload = await MacOSNative.ScanCamera(token, screen);
                    if (payload == null) return;
                    stage = "reading token format";
                    bool parsed = MacOSOtpParser.TryParse(payload, out var values, out var parseError);
                    if (!parsed && parseError == "Authorization code required")
                    {
                        var password = new PasswordBox { Header = "Deepnet authorization code", MaxLength = 0, PasswordRevealMode = PasswordRevealMode.Peek };
                        var prompt = new ContentDialog { Title = "Unlock MobileID token", Content = password, PrimaryButtonText = "Import", CloseButtonText = "Cancel", XamlRoot = App.ShellPageInstance.XamlRoot };
                        using var cancelPrompt = token.Register(() => prompt.DispatcherQueue.TryEnqueue(() => prompt.Hide()));
                        try
                        {
                            if (await prompt.ShowAsync() != ContentDialogResult.Primary || token.IsCancellationRequested) return;
                            parsed = MacOSOtpParser.TryParse(payload, out values, out parseError, password.Password);
                        }
                        finally { password.Password = string.Empty; }
                    }
                    if (!parsed)
                    {
                        var retry = new ContentDialog { Title = "Unable to import this QR", Content = new TextBlock { Text = parseError, TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap }, PrimaryButtonText = "Scan another QR", CloseButtonText = "Cancel", XamlRoot = App.ShellPageInstance.XamlRoot };
                        using var cancelRetry = token.Register(() => retry.DispatcherQueue.TryEnqueue(() => retry.Hide()));
                        if (await retry.ShowAsync() == ContentDialogResult.Primary) continue;
                        return;
                    }
                    token.ThrowIfCancellationRequested();
                    stage = "opening account review";
                    var parameters = new NavigationParameters();
                    parameters.Add("AccountValuePair", values);
                    var result = await NavigationService.NavigateAsync(nameof(AddAccountPage), parameters);
                    if (!result.Success) throw result.Exception ?? new InvalidOperationException("The account review could not be opened. Return to your account list and try again.");
                    return;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                MacOSScanDiagnostics.Record(stage, e);
                Exception cause = e;
                while (cause.InnerException != null) cause = cause.InnerException;
                string detail = cause is BiometryException ? cause.Message
                    : stage == "capturing QR" && cause is InvalidOperationException ? cause.Message
                    : "The operation failed while " + stage + ". Error type: " + cause.GetType().Name + ".";
                await MacOSSession.Message(DialogService, stage == "opening account review" ? "QR read — account import failed" : "QR scanner", detail);
            }
            finally { macScanning = false; }
        }
    }
}
#endif
