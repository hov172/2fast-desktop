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


public sealed partial class ChangeDatafilePasswordContentDialog : ContentDialog
{
    public ChangeDatafilePasswordContentDialogViewModel ViewModel => DataContext as ChangeDatafilePasswordContentDialogViewModel;
    public ChangeDatafilePasswordContentDialog()
    {
        this.InitializeComponent();
        // Refresh x:Bind when the DataContext changes.
        DataContextChanged += (s, e) => Bindings.Update();
        Closed += (_, _) =>
        {
            ViewModel.CurrentPassword = ""; ViewModel.NewPassword = ""; ViewModel.NewPasswordRepeat = "";
            CurrentPasswordBox.PasswordRevealMode = NewPasswordBox.PasswordRevealMode = RepeatPasswordBox.PasswordRevealMode = PasswordRevealMode.Hidden;
        };
    }
    private void Reveal_Changed(object sender, RoutedEventArgs args)
    {
        var mode = (sender as CheckBox)?.IsChecked == true ? PasswordRevealMode.Visible : PasswordRevealMode.Hidden;
        CurrentPasswordBox.PasswordRevealMode = mode; NewPasswordBox.PasswordRevealMode = mode; RepeatPasswordBox.PasswordRevealMode = mode;
    }
    private async void ChangePassword_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        args.Cancel = true;
        try
        {
            ChangeError.Text = "";
            if (!ViewModel.IsPrimaryBTNEnable) return;
            if (!await ViewModel.TestPassword()) { ChangeError.Text = "The current password could not unlock this data file."; return; }
            await ViewModel.ChangePasswordInFileAndDB();
            args.Cancel = !ViewModel.PasswordChanged;
            if (args.Cancel) ChangeError.Text = "The password could not be changed. Your existing password is still in use.";
        }
        catch (Exception error)
        {
#if TWOFAST_DESKTOP
            Project2FA.Services.MacOS.MacOSScanDiagnostics.Record("changing data-file password", error);
#endif
            ChangeError.Text = error is System.IO.IOException || error is InvalidOperationException ? error.Message : "The change could not be completed. Check your password and file access.";
        }
        finally { deferral.Complete(); }
    }
}
