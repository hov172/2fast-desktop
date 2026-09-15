using Project2FA.ViewModels;

namespace Project2FA.Uno.Views;

public sealed partial class UseDatafileContentDialog : ContentDialog
{
    public UseDatafileContentDialogViewModel ViewModel => DataContext as UseDatafileContentDialogViewModel;
    public UseDatafileContentDialog()
    {
        this.InitializeComponent();
        // Refresh x:Bind when the DataContext changes.
        DataContextChanged += (s, e) => Bindings.Update();
    }
}
