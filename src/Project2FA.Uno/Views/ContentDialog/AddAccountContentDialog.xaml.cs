using Project2FA.ViewModels;

namespace Project2FA.Uno.Views;

public sealed partial class AddAccountContentDialog : ContentDialog
{
    public AddAccountContentDialogViewModel ViewModel => DataContext as AddAccountContentDialogViewModel;
    public AddAccountContentDialog()
    {
        this.InitializeComponent();
        // Refresh x:Bind when the DataContext changes.
        DataContextChanged += (s, e) => Bindings.Update();
    }
}
