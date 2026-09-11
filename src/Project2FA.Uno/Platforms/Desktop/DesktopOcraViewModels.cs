#if TWOFAST_DESKTOP
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Project2FA.Repository.Models;
using Project2FA.Services;
using Project2FA.Services.Desktop;
using Project2FA.Uno.Views;
using Project2FA.UnoApp;
using UNOversal.Navigation;
using Windows.ApplicationModel.DataTransfer;

namespace Project2FA.ViewModels;

public partial class AccountCodePageViewModel
{
    public async Task AddOcraToken()
    {
        var name = new TextBox { Header = DesktopText.Get("OcraTokenName", "Token name") };
        var seed = new PasswordBox { Header = DesktopText.Get("OcraTokenSeed", "Token seed"), MaxLength = 0, PasswordRevealMode = PasswordRevealMode.Peek };
        var encoding = new ComboBox { Header = DesktopText.Get("OcraSeedEncoding", "Seed encoding"), ItemsSource = new[] { DesktopText.Get("Base32", "Base32"), DesktopText.Get("Base64", "Base64"), DesktopText.Get("Hex", "Hex") }, SelectedIndex = 0 };
        var error = new TextBlock { TextWrapping = TextWrapping.Wrap };
        var panel = new StackPanel { Spacing = 10 };
        panel.Children.Add(new TextBlock { Text = DesktopText.Get("OcraInstructions", "{0}\nUse the raw seed and encoding supplied by your administrator. This token requires a login challenge.").Replace("{0}", DesktopOcra.Suite), TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(name); panel.Children.Add(seed); panel.Children.Add(encoding); panel.Children.Add(error);
        var dialog = new ContentDialog { DefaultButton = ContentDialogButton.Primary, Title = DesktopText.Get("AddOcraToken", "Add OCRA token"), Content = panel, PrimaryButtonText = DesktopText.Get("Continue", "Continue"), CloseButtonText = DesktopText.Get("Cancel", "Cancel"), XamlRoot = DesktopSession.Shell.XamlRoot };
        List<KeyValuePair<string, string>>? values = null;
        dialog.PrimaryButtonClick += (_, e) =>
        {
            if (!DesktopOcraTokenFactory.TryCreate(name.Text, seed.Password, encoding.SelectedIndex, out values, out var validationError))
            {
                e.Cancel = true;
                error.Text = DesktopText.Get("OcraInvalidSeed", validationError);
            }
        };
        var token = DesktopSession.Token;
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
        if (!DesktopDeviceBinding.Allows(model.MobileIdDeviceId))
        { await DesktopSession.Message(DialogService, DesktopText.Get("OcraWrongDeviceTitle", "Token belongs to another desktop"), DesktopText.Get("OcraWrongDevice", "Re-enroll this device-bound MobileID token on this desktop.")); return false; }
        if (model.OcraSuite != DesktopOcra.Suite)
        { await DesktopSession.Message(DialogService, DesktopText.Get("OcraUnsupportedTitle", "Unsupported OCRA suite"), DesktopText.Get("OcraUnsupported", "This token's OCRA suite is not supported.")); return false; }
        var challenge = new TextBox { Header = DesktopText.Get("OcraChallenge", "Login challenge (1–8 digits)"), MaxLength = 0 };
        var response = new TextBlock { FontSize = 30 };
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap };
        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(challenge); panel.Children.Add(response); panel.Children.Add(status);
        var dialog = new ContentDialog { DefaultButton = ContentDialogButton.Primary, Title = model.Issuer + " — OCRA", Content = panel,
            PrimaryButtonText = DesktopText.Get("Generate", "Generate"), SecondaryButtonText = DesktopText.Get("CopyResponse", "Copy response"), CloseButtonText = DesktopText.Get("Close", "Close"), XamlRoot = DesktopSession.Shell.XamlRoot };
        long responseMinute = -1;
        bool Generate()
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                response.Text = DesktopOcra.Compute(model.SecretByteArray, challenge.Text, now);
                responseMinute = now.ToUnixTimeSeconds() / 60;
                status.Text = DesktopText.Get("OcraResponseExpiry", "Use this response before the current minute ends.");
                return true;
            }
            catch (ArgumentException) { response.Text = ""; status.Text = DesktopText.Get("OcraInvalidChallenge", "Enter the numeric challenge from your login page (1–8 digits)."); return false; }
        }
        dialog.PrimaryButtonClick += (_, e) => { e.Cancel = true; Generate(); };
        dialog.SecondaryButtonClick += (_, e) =>
        {
            e.Cancel = true;
            if (!Generate()) return;
            try { var data = new DataPackage(); data.SetText(response.Text); Clipboard.SetContent(data); status.Text = DesktopText.Get("OcraCopied", "Response copied."); }
            catch (Exception) { status.Text = DesktopText.Get("OcraCopyFailed", "Unable to copy. Read the response above."); }
        };
        challenge.TextChanged += (_, _) => { response.Text = ""; status.Text = ""; };
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) =>
        {
            if (response.Text.Length > 0 && responseMinute != DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60)
            { response.Text = ""; status.Text = DesktopText.Get("OcraExpired", "Response expired. Generate a new response."); }
        };
        using var cancel = DesktopSession.Token.Register(() => dialog.DispatcherQueue.TryEnqueue(() => dialog.Hide()));
        timer.Start();
        try { await dialog.ShowAsync(); }
        finally { timer.Stop(); challenge.Text = ""; response.Text = ""; }
        return false; // This dialog handles copying; don't copy the list's Challenge label.
    }
}
#endif
