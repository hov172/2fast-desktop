using Microsoft.UI.Xaml.Data;
using Project2FA.ViewModels;

namespace Project2FA.Uno.Views
{
    public sealed partial class NewDataFilePage : Page
    {
        public NewDataFilePageViewModel ViewModel => DataContext as NewDataFilePageViewModel;
        public NewDataFilePage()
        {
            this.InitializeComponent();
            FormKeyboard.Attach(this, () => NewVaultSubmitButton);
            // Refresh x:Bind when the DataContext changes.
            DataContextChanged += (s, e) => Bindings.Update();
            Unloaded += (_, _) => ShowPasswords.IsChecked = false;
#if TWOFAST_DESKTOP
            foreach (var box in new[] { PB_LocalPassword, PB_LocalPasswordRepeat })
            {
                // Some Uno targets implement PasswordBox on top of TextBox; newer
                // targets use Control and already disable prediction for passwords.
                if ((object)box is TextBox textBox)
                {
                    textBox.IsTextPredictionEnabled = false;
                    textBox.IsSpellCheckEnabled = false;
                }
            }
            PasswordWhitespaceHint.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("PasswordWhitespaceHint") });
            ValidationMessage.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("ValidationMessage") });
            Loaded += async (s, e) => { if (ViewModel != null) await ViewModel.InitializeDesktopLocation(); };
#endif
        }

        private void ShowPasswords_Changed(object sender, RoutedEventArgs e)
        {
            var mode = ShowPasswords.IsChecked == true ? PasswordRevealMode.Visible : PasswordRevealMode.Peek;
            PB_LocalPassword.PasswordRevealMode = mode;
            PB_LocalPasswordRepeat.PasswordRevealMode = mode;
        }

        private async void HLBTN_PasswordInfo(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = Strings.Resources.NewDatafilePasswordInfoTitle,
                Content = new TextBlock { Text = Strings.Resources.NewDatafilePasswordInfo, TextWrapping = TextWrapping.Wrap },
                CloseButtonText = Strings.Resources.Confirm,
                XamlRoot = XamlRoot
            };
            await dialog.ShowAsync();
        }

    }
}
