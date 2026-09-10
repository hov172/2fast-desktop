using Project2FA.ViewModels;
using Project2FA.Services;


namespace Project2FA.Uno.Views
{
    public sealed partial class LoginPage : Page
    {
        public LoginPageViewModel ViewModel => DataContext as LoginPageViewModel;
        public LoginPage()
        {
            this.InitializeComponent();
            FormKeyboard.Attach(this, () => LoginSubmitButton);
            // Refresh x:Bind when the DataContext changes.
            DataContextChanged += (s, e) => Bindings.Update();
            this.Loaded += LoginPage_Loaded;
            Unloaded += (_, _) => ShowPassword.IsChecked = false;
#if TWOFAST_DESKTOP
            Unloaded += (_, _) => DesktopSession.CancelOperations();
#endif
        }

        private void ShowPassword_Changed(object sender, RoutedEventArgs e)
        {
            LoginPassword.PasswordRevealMode = ShowPassword.IsChecked == true
                ? Microsoft.UI.Xaml.Controls.PasswordRevealMode.Visible
                : Microsoft.UI.Xaml.Controls.PasswordRevealMode.Peek;
        }

        private async void LoginPage_Loaded(object sender, RoutedEventArgs e)
        {
#if TWOFAST_DESKTOP
            TouchIDLoginButton.Content = "Unlock with " + DesktopPlatform.BiometricName;
            TouchIDLoginButton.SetBinding(VisibilityProperty, new Microsoft.UI.Xaml.Data.Binding
            {
                Source = ViewModel, Path = new PropertyPath(nameof(ViewModel.BiometricIsUsable))
            });
            await ViewModel.RefreshDesktopBiometry();
#endif
            if (System.Diagnostics.Debugger.IsAttached || SettingsService.Instance.PrideMonthDesign)
            {
                PageStaticBackgroundBorder.Visibility = Visibility.Visible;
                PageImageBackgroundBorder.Visibility = Visibility.Collapsed;
            }
        }
    }
}
