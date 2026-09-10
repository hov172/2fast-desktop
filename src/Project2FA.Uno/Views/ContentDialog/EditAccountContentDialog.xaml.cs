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
using Project2FA.ViewModels;

namespace Project2FA.Uno.Views;


public sealed partial class EditAccountContentDialog : ContentDialog
{
    public EditAccountContentDialogViewModel ViewModel => DataContext as EditAccountContentDialogViewModel;
    public EditAccountContentDialog()
    {
        this.InitializeComponent();
        // Refresh x:Bind when the DataContext changes.
        DataContextChanged += (s, e) => Bindings.Update();
    }
    private async void IconName_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            await ViewModel.SearchAccountFonts(sender.Text);
    }

    private void IconName_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is Project2FA.Repository.Models.FontIdentifikationModel icon && icon.Name != Project2FA.Strings.Resources.AccountCodePageSearchNotFound)
            ViewModel.AccountIconName = icon.Name;
    }

    private async void Save_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        try
        {
            EditError.Text = "";
            if (!ViewModel.IsPrimaryBTNEnable) { args.Cancel = true; return; }
            if (ViewModel.PrimaryButtonCommand is CommunityToolkit.Mvvm.Input.IAsyncRelayCommand command)
                await command.ExecuteAsync(null);
        }
        catch (Exception error)
        {
            args.Cancel = true;
            EditError.Text = "The edits could not be saved. Check file access and try again.";
#if TWOFAST_DESKTOP
            DesktopScanDiagnostics.Record("saving account edits", error);
#endif
        }
        finally { deferral.Complete(); }
    }
}
