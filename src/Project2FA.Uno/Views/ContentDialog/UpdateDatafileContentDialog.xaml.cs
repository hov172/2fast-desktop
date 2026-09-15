using Project2FA.ViewModels;

namespace Project2FA.Uno.Views;

public sealed partial class UpdateDatafileContentDialog : ContentDialog
{
    public UpdateDatafileContentDialogViewModel ViewModel => DataContext as UpdateDatafileContentDialogViewModel;
    public UpdateDatafileContentDialog()
    {
        this.InitializeComponent();
        // Refresh x:Bind when the DataContext changes.
        DataContextChanged += (s, e) => Bindings.Update();
    }
}
