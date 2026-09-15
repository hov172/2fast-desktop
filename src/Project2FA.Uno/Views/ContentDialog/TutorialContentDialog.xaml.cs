namespace Project2FA.Uno.Views;

public sealed partial class TutorialContentDialog : ContentDialog
{
    public TutorialContentDialog()
    {
        this.InitializeComponent();
        // Refresh x:Bind when the DataContext changes.
        DataContextChanged += (s, e) => Bindings.Update();
    }
}
