using Project2FA.ViewModels;
using System.Reflection;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.DataTransfer;

namespace Project2FA.Uno.Views
{
    public sealed partial class SettingPage : Page
    {
        public SettingPageViewModel ViewModel => DataContext as SettingPageViewModel;
        public SettingPage()
        {
            this.InitializeComponent();
            InitializeAboutDetails();
#if TWOFAST_DESKTOP
            PlatformTitle.Text = "2fast for " + DesktopPlatform.Name;
            BiometricSettingsExpander.Header = "Use " + DesktopPlatform.BiometricName;
            BiometricStartupSettingsCard.Header = "Use " + DesktopPlatform.BiometricName + " on startup";
            BiometricSettingsExpander.Description = DesktopPlatform.BiometricDescription;
            Unloaded += (_, _) => ViewModel?.SettingsPartViewModel.CancelDesktopBiometry();
            Loaded += async (_, _) =>
            {
                int section = ViewModel.SelectedItem;
                GeneralSettingsSection.Visibility = section == 0 ? Visibility.Visible : Visibility.Collapsed;
                DatafileSection.Visibility = section == 1 ? Visibility.Visible : Visibility.Collapsed;
                AboutSection.Visibility = section == 2 ? Visibility.Visible : Visibility.Collapsed;
                SettingsTitle.Text = section == 1 ? "Data file" : section == 2 ? "About" : "Settings";
                if (section == 0) await ViewModel.SettingsPartViewModel.RefreshDesktopBiometrySettings();
            };
#endif
            // Refresh x:Bind when the DataContext changes.
            DataContextChanged += (s, e) => Bindings.Update();
        }

        private void InitializeAboutDetails()
        {
            // Read the values embedded by MSBuild, so this stays in sync with app packaging.
            var assembly = typeof(SettingPage).Assembly;
            var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>().ToArray();
            var version = metadata.FirstOrDefault(item => item.Key == "ApplicationDisplayVersion")?.Value
                ?? assembly.GetName().Version?.ToString(3) ?? "Unavailable";
            var build = metadata.FirstOrDefault(item => item.Key == "ApplicationBuildNumber")?.Value
                ?? "Unavailable";

            AboutVersionText.Text = version;
            AboutBuildText.Text = "Build " + build;
            AboutSystemText.Text = RuntimeInformation.OSDescription;
            AboutArchitectureText.Text = $"App architecture: {RuntimeInformation.ProcessArchitecture} · System architecture: {RuntimeInformation.OSArchitecture}";
        }

        private void CopyAppDetails_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var details = new DataPackage();
                details.SetText($"{PlatformTitle.Text}\nVersion {AboutVersionText.Text}\n{AboutBuildText.Text}\n{AboutSystemText.Text}\n{AboutArchitectureText.Text}");
                Clipboard.SetContent(details);
                AboutCopyStatus.Text = "App details copied.";
            }
            catch (Exception)
            {
                AboutCopyStatus.Text = "Could not copy app details. You can select and copy the information above.";
            }
            AboutCopyStatus.Visibility = Visibility.Visible;
        }
    }
}
