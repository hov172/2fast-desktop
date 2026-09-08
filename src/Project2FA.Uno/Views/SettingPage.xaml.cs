using Project2FA.ViewModels;

namespace Project2FA.Uno.Views
{
    public sealed partial class SettingPage : Page
    {
        public SettingPageViewModel ViewModel => DataContext as SettingPageViewModel;
        public SettingPage()
        {
            this.InitializeComponent();
#if TWOFAST_DESKTOP
            PlatformTitle.Text = "2fast for " + Project2FA.Services.MacOS.DesktopPlatform.Name;
            BiometricSettingsExpander.Header = "Use " + Project2FA.Services.MacOS.DesktopPlatform.BiometricName;
            BiometricStartupSettingsCard.Header = "Use " + Project2FA.Services.MacOS.DesktopPlatform.BiometricName + " on startup";
            BiometricSettingsExpander.Description = Project2FA.Services.MacOS.DesktopPlatform.BiometricDescription;
            Unloaded += (_, _) => ViewModel?.SettingsPartViewModel.CancelMacOSBiometry();
            Loaded += async (_, _) =>
            {
                int section = ViewModel.SelectedItem;
                GeneralSettingsSection.Visibility = section == 0 ? Visibility.Visible : Visibility.Collapsed;
                DatafileSection.Visibility = section == 1 ? Visibility.Visible : Visibility.Collapsed;
                AboutSection.Visibility = section == 2 ? Visibility.Visible : Visibility.Collapsed;
                SettingsTitle.Text = section == 1 ? "Data file" : section == 2 ? "About" : "Settings";
                if (section == 0) await ViewModel.SettingsPartViewModel.RefreshMacOSBiometrySettings();
            };
#endif
            // Refresh x:Bind when the DataContext changes.
            DataContextChanged += (s, e) => Bindings.Update();
        }
    }
}
