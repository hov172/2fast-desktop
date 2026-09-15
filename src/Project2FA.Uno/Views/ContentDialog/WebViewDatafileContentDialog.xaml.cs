using Project2FA.ViewModels;

namespace Project2FA.Uno.Views;

public sealed partial class WebViewDatafileContentDialog : ContentDialog
{
    public WebViewDatafileContentDialogViewModel ViewModel => DataContext as WebViewDatafileContentDialogViewModel;
    public WebViewDatafileContentDialog()
    {
        this.InitializeComponent();
        // Refresh x:Bind when the DataContext changes.
        DataContextChanged += (s, e) => Bindings.Update();
    }
}
