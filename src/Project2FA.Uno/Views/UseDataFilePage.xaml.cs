using Project2FA.ViewModels;

namespace Project2FA.Uno.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class UseDataFilePage : Page
    {
        public UseDataFilePageViewModel ViewModel => DataContext as UseDataFilePageViewModel;
        public UseDataFilePage()
        {
            this.InitializeComponent();
            FormKeyboard.Attach(this, () => OpenVaultSubmitButton);
            // Refresh x:Bind when the DataContext changes.
            DataContextChanged += (s, e) => Bindings.Update();
        }

        private async void BTN_LocalFile_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.UseExistDatafile();
            PB_LocalPassword.Focus(FocusState.Programmatic);
        }
    }
}
