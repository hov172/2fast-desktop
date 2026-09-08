using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
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
