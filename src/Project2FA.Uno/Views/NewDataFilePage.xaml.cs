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

namespace Project2FA.Uno.Views
{
    public sealed partial class NewDataFilePage : Page
    {
        public NewDataFilePageViewModel ViewModel => DataContext as NewDataFilePageViewModel;
        public NewDataFilePage()
        {
            this.InitializeComponent();
            FormKeyboard.Attach(this, () => NewVaultSubmitButton);
            // Refresh x:Bind when the DataContext changes.
            DataContextChanged += (s, e) => Bindings.Update();
            Unloaded += (_, _) => ShowPasswords.IsChecked = false;
#if TWOFAST_DESKTOP
            foreach (var box in new[] { PB_LocalPassword, PB_LocalPasswordRepeat })
            {
                // Some Uno targets implement PasswordBox on top of TextBox; newer
                // targets use Control and already disable prediction for passwords.
                if ((object)box is TextBox textBox)
                {
                    textBox.IsTextPredictionEnabled = false;
                    textBox.IsSpellCheckEnabled = false;
                }
            }
            PasswordWhitespaceHint.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("PasswordWhitespaceHint") });
            ValidationMessage.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("ValidationMessage") });
            Loaded += async (s, e) => { if (ViewModel != null) await ViewModel.InitializeDesktopLocation(); };
#endif
        }

        private void ShowPasswords_Changed(object sender, RoutedEventArgs e)
        {
            var mode = ShowPasswords.IsChecked == true ? PasswordRevealMode.Visible : PasswordRevealMode.Peek;
            PB_LocalPassword.PasswordRevealMode = mode;
            PB_LocalPasswordRepeat.PasswordRevealMode = mode;
        }

        private async void HLBTN_PasswordInfo(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = Strings.Resources.NewDatafilePasswordInfoTitle,
                Content = new TextBlock { Text = Strings.Resources.NewDatafilePasswordInfo, TextWrapping = TextWrapping.Wrap },
                CloseButtonText = Strings.Resources.Confirm,
                XamlRoot = XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async void BTN_LocalPath_Click(object sender, RoutedEventArgs e)
        {
            //if (!MainPivot.Items.Contains(FolderPivotItem))
            //{
            //    MainPivot.Items.Add(FolderPivotItem);
            //}
            //ViewModel.SelectWebDAV = false;
            //bool result = await ViewModel.SetLocalPath();
            //if (!result)
            //{
            //    if (MainPivot.Items.Contains(FolderPivotItem))
            //    {
            //        MainPivot.Items.Remove(FolderPivotItem);
            //    }
            //}
        }

        private void BTN_WebDAV_Click(object sender, RoutedEventArgs e)
        {
            //MainPivot.Items.Remove(FolderPivotItem);
            //if (!MainPivot.Items.Contains(WebDAVPivotItem))
            //{
            //    MainPivot.Items.Add(WebDAVPivotItem);
            //}
            //ViewModel.SelectedIndex = 1;
            //ViewModel.SelectWebDAV = true;
            //ViewModel.ChooseWebDAV();
        }

        private void HLBTN_WDPasswordInfo(object sender, RoutedEventArgs e)
        {
            //AutoCloseTeachingTip teachingTip = new AutoCloseTeachingTip
            //{
            //    Target = sender as FrameworkElement,
            //    Subtitle = Strings.Resources.WebDAVAppPasswordInfo,
            //    AutoCloseInterval = 8000,
            //    IsLightDismissEnabled = true,
            //    BorderBrush = new SolidColorBrush((Color)App.Current.Resources["SystemAccentColor"]),
            //    IsOpen = true,
            //};
            //RootGrid.Children.Add(teachingTip);
        }
    }
}
