#if TWOFAST_DESKTOP
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Project2FA.Repository.Models;
using Project2FA.Services.MacOS;
using Project2FA.Uno.Views;
using Project2FA.UnoApp;
using UNOversal.Navigation;
using System.Security.Cryptography;
using Windows.ApplicationModel.DataTransfer;

namespace Project2FA.ViewModels;

public partial class AccountCodePageViewModel
{
    public async Task AddOcraToken()
    {
        var name = new TextBox { Header = "Token name" };
        var seed = new PasswordBox { Header = "Token seed", MaxLength = 0, PasswordRevealMode = PasswordRevealMode.Peek };
        var encoding = new ComboBox { Header = "Seed encoding", ItemsSource = new[] { "Base32", "Base64", "Hex" }, SelectedIndex = 0 };
        var error = new TextBlock { TextWrapping = TextWrapping.Wrap };
        var panel = new StackPanel { Spacing = 10 };
        panel.Children.Add(new TextBlock { Text = MacOSOcra.Suite + "\nUse the raw seed and encoding supplied by your administrator. This token requires a login challenge.", TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(name); panel.Children.Add(seed); panel.Children.Add(encoding); panel.Children.Add(error);
        var dialog = new ContentDialog { Title = "Add OCRA token", Content = panel, PrimaryButtonText = "Continue", CloseButtonText = "Cancel", XamlRoot = App.ShellPageInstance.XamlRoot };
        List<KeyValuePair<string, string>>? values = null;
        dialog.PrimaryButtonClick += (_, e) =>
        {
            byte[]? key = null;
            try
            {
                if (string.IsNullOrWhiteSpace(name.Text)) throw new ArgumentException();
                key = encoding.SelectedIndex switch
                {
                    0 => OtpNet.Base32Encoding.ToBytes(seed.Password.Trim().ToUpperInvariant()),
                    1 => Convert.FromBase64String(seed.Password.Trim()),
                    2 => Convert.FromHexString(seed.Password.Trim()),
                    _ => throw new ArgumentException()
                };
                if (key.Length < 10) throw new ArgumentException();
                values = new() { new("label", "Deepnet"), new("issuer", name.Text.Trim()),
                    new("secret", OtpNet.Base32Encoding.ToString(key)), new("period", "60"),
                    new("digits", "6"), new("algorithm", "SHA1"), new("ocrasuite", MacOSOcra.Suite) };
            }
            catch (Exception e2) when (e2 is ArgumentException or FormatException)
            { e.Cancel = true; error.Text = "Enter a token name and valid seed in the selected encoding (at least 10 bytes)."; }
            finally { if (key != null) CryptographicOperations.ZeroMemory(key); }
        };
        var token = MacOSSession.Token;
        using var cancel = token.Register(() => dialog.DispatcherQueue.TryEnqueue(() => dialog.Hide()));
        try
        {
            if (await dialog.ShowAsync() != ContentDialogResult.Primary || values == null || token.IsCancellationRequested) return;
            var parameters = new NavigationParameters(); parameters.Add("AccountValuePair", values);
            await NavigationService.NavigateAsync(nameof(AddAccountPage), parameters);
        }
        finally { seed.Password = string.Empty; values?.Clear(); }
    }

    public async Task<bool> ShowOcraResponse(TwoFACodeModel model)
    {
        if (!MacOSDeviceBinding.Allows(model.MobileIdDeviceId))
        { await MacOSSession.Message(DialogService, "Token belongs to another Mac", "Re-enroll this device-bound MobileID token on this Mac."); return false; }
        if (model.OcraSuite != MacOSOcra.Suite)
        { await MacOSSession.Message(DialogService, "Unsupported OCRA suite", "This token's OCRA suite is not supported."); return false; }
        var challenge = new TextBox { Header = "Login challenge (1–8 digits)", MaxLength = 0 };
        var response = new TextBlock { FontSize = 30 };
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap };
        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(challenge); panel.Children.Add(response); panel.Children.Add(status);
        var dialog = new ContentDialog { Title = model.Issuer + " — OCRA", Content = panel,
            PrimaryButtonText = "Generate", SecondaryButtonText = "Copy response", CloseButtonText = "Close", XamlRoot = App.ShellPageInstance.XamlRoot };
        long responseMinute = -1;
        bool Generate()
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                response.Text = MacOSOcra.Compute(model.SecretByteArray, challenge.Text, now);
                responseMinute = now.ToUnixTimeSeconds() / 60;
                status.Text = "Use this response before the current minute ends.";
                return true;
            }
            catch (ArgumentException) { response.Text = ""; status.Text = "Enter the numeric challenge from your login page (1–8 digits)."; return false; }
        }
        dialog.PrimaryButtonClick += (_, e) => { e.Cancel = true; Generate(); };
        dialog.SecondaryButtonClick += (_, e) =>
        {
            e.Cancel = true;
            if (!Generate()) return;
            try { var data = new DataPackage(); data.SetText(response.Text); Clipboard.SetContent(data); status.Text = "Response copied."; }
            catch (Exception) { status.Text = "Unable to copy. Read the response above."; }
        };
        challenge.TextChanged += (_, _) => { response.Text = ""; status.Text = ""; };
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) =>
        {
            if (response.Text.Length > 0 && responseMinute != DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60)
            { response.Text = ""; status.Text = "Response expired. Generate a new response."; }
        };
        using var cancel = MacOSSession.Token.Register(() => dialog.DispatcherQueue.TryEnqueue(() => dialog.Hide()));
        timer.Start();
        try { await dialog.ShowAsync(); }
        finally { timer.Stop(); challenge.Text = ""; response.Text = ""; }
        return false; // This dialog handles copying; don't copy the list's Challenge label.
    }
}
#endif
