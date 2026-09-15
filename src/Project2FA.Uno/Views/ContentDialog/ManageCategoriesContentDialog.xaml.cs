using Microsoft.UI.Xaml.Controls.Primitives;
using Project2FA.Repository.Models;
using Project2FA.ViewModels;

namespace Project2FA.Uno.Views;

public sealed partial class ManageCategoriesContentDialog : ContentDialog
{
    public ManageCategoriesContentDialogViewModel ViewModel => DataContext as ManageCategoriesContentDialogViewModel;

    private Flyout _openedIconFlyout;

    public ManageCategoriesContentDialog()
    {
        this.InitializeComponent();
        // Refresh x:Bind when the DataContext changes.
        DataContextChanged += (s, e) => Bindings.Update();
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (ViewModel != null) ViewModel.DataChanged = true;
    }

    private void CB_CategoryModel_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ComboBox cb && cb.DataContext is CategoryModel model && ViewModel != null)
        {
            ViewModel.SelectedComboBoxItem = ViewModel.IconSourceCollection
                .Where(x => x.UnicodeIndex == Convert.ToUInt32(model.UnicodeIndex)).FirstOrDefault();
        }
    }

    private void BTN_ShowIcons_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            _openedIconFlyout = FlyoutBase.GetAttachedFlyout(btn) as Flyout;
            FlyoutBase.ShowAttachedFlyout(btn);
        }
    }

    private void AcceptIcon_Click(object sender, RoutedEventArgs e)
    {
        _openedIconFlyout?.Hide();
        if (ViewModel?.SelectedComboBoxItem != null && sender is Button btn && btn.DataContext is CategoryModel model)
        {
            model.UnicodeIndex = ViewModel.SelectedComboBoxItem.UnicodeIndex.ToString();
            model.UnicodeString = ViewModel.SelectedComboBoxItem.UnicodeString;
            ViewModel.DataChanged = true;
        }
    }
}
