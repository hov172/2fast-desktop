using Project2FA.ViewModels;

namespace Project2FA.Uno.Views;

public sealed partial class DisplayQRCodeContentDialog : ContentDialog
{
    public DisplayQRCodeContentDialogViewModel ViewModel => DataContext as DisplayQRCodeContentDialogViewModel;
    public DisplayQRCodeContentDialog()
    {
        this.InitializeComponent();
        // Refresh x:Bind when the DataContext changes.
        DataContextChanged += (s, e) => Bindings.Update();
    }
}
