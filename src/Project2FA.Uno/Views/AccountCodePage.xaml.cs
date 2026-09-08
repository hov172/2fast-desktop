using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
using Project2FA.Controls;
using Project2FA.Repository.Models;
using Project2FA.UnoApp;
using Project2FA.ViewModels;
using UNOversal.Ioc;
using UNOversal.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;

namespace Project2FA.Uno.Views;

public sealed partial class AccountCodePage : Page
{
    public AccountCodePageViewModel ViewModel => DataContext as AccountCodePageViewModel;
    public AccountCodePage()
    {
        this.InitializeComponent();
#if TWOFAST_DESKTOP
        ScreenScanMenuItem.Visibility = Visibility.Visible;
        OcraMenuItem.Visibility = Visibility.Visible;
        Unloaded += (_, _) =>
        {
            ViewModel?.StopAccountTimers();
            Project2FA.Services.MacOS.MacOSSession.CancelOperations();
        };
#endif
        Loaded += (_, _) => ObserveAccounts();
        Unloaded += (_, _) => StopObservingAccounts();
        DataContextChanged += (_, _) => { Bindings.Update(); ObserveAccounts(); };
        // Evcnt for the native back behavior which currently skips the framework extension
#if __ANDROID__ || __IOS__
        App.ShellPageInstance.MainFrame.Navigated -= MainFrame_Navigated;
        App.ShellPageInstance.MainFrame.Navigated += MainFrame_Navigated;
        PropertyChangedCallback callback = new PropertyChangedCallback(SelectedTabBarIndexChanged);
        //register an event for the changed selected index property of the TabBar
        MobileAutoSuggestBox.RegisterDisposablePropertyChangedCallback(VisibilityProperty, SelectedTabBarIndexChanged);
        //App.ShellPageInstance.ViewModel.TabBarIsVisible = true;
#endif
    }

    private CommunityToolkit.WinUI.Collections.AdvancedCollectionView observedAccounts;

    private void StopObservingAccounts()
    {
        if (observedAccounts != null) observedAccounts.VectorChanged -= AccountsChanged;
        observedAccounts = null;
    }

    private void ObserveAccounts()
    {
        StopObservingAccounts();
        observedAccounts = ViewModel?.TwoFADataService?.ACVCollection;
        // These page controls can load before the navigation framework assigns
        // a view model. Attach the current instance explicitly on each transition.
        LV_AccountCollection.ItemsSource = observedAccounts;
        ABB_Logout.Command = ViewModel?.LogoutCommand;
        ABB_Refresh.Command = ViewModel?.RefreshCommand;
        CameraAction.Command = ViewModel?.CameraCommand;
        ManualAction.Command = ViewModel?.AddAccountCommand;
        MobileAutoSuggestBox.ItemsSource = ViewModel?.SearchAccountCollection;
        if (observedAccounts != null) observedAccounts.VectorChanged += AccountsChanged;
        UpdateCollectionSummary();
    }

    private void AccountsChanged(Windows.Foundation.Collections.IObservableVector<object> sender, Windows.Foundation.Collections.IVectorChangedEventArgs e)
        => UpdateCollectionSummary();

    private void UpdateCollectionSummary()
    {
        if (ViewModel?.TwoFADataService == null) return;
        int total = ViewModel.TwoFADataService.Collection.Count;
        int shown = ViewModel.TwoFADataService.ACVCollection.Count;
        CollectionSummary.Text = shown == total ? $"{total} account{(total == 1 ? "" : "s")}" : $"{shown} of {total} accounts";
        EmptyState.Visibility = shown == 0 ? Visibility.Visible : Visibility.Collapsed;
        EmptyTitle.Text = total == 0 ? "Add your first account" : "No matching accounts";
        EmptyDescription.Text = total == 0
            ? "Scan a setup QR from your screen or camera, or enter a setup key manually."
            : "Try another account name or clear the search field.";
    }

    private static TwoFACodeModel AccountFromSender(object sender) =>
        sender is FrameworkElement element ? element.Tag as TwoFACodeModel ?? element.DataContext as TwoFACodeModel : null;

    private async void OcraChallenge_Click(object sender, RoutedEventArgs e)
    {
#if TWOFAST_DESKTOP
        if (AccountFromSender(sender) is TwoFACodeModel model)
            await ViewModel.ShowOcraResponse(model);
#endif
    }

    private async void OcraMenuItem_Click(object sender, RoutedEventArgs e)
    {
#if TWOFAST_DESKTOP
        await ViewModel.AddOcraToken();
#endif
    }

    private async void ScreenScanMenuItem_Click(object sender, RoutedEventArgs e)
    {
#if TWOFAST_DESKTOP
        await ViewModel.ScanMacOSScreen();
#endif
    }

    private void SelectedTabBarIndexChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var visibilty = (Visibility)args.NewValue;
        switch (visibilty)
        {
            case Visibility.Collapsed:
                var fadeOutStoryboard = new Storyboard();
                var fadeAnimation = new DoubleAnimation { From = 1.0, To = 0.0, Duration = new Duration(TimeSpan.FromSeconds(1.0)) };
                Storyboard.SetTarget(fadeAnimation, MobileAutoSuggestBox);
                Storyboard.SetTargetProperty(fadeAnimation, "Opacity");
                fadeOutStoryboard.Children.Add(fadeAnimation);
                fadeOutStoryboard.Begin();
                break;
            case Visibility.Visible:
                var fadeInStoryboard = new Storyboard();
                var fadeInAnimation = new DoubleAnimation { From = 0.0, To = 1.0, Duration = new Duration(TimeSpan.FromSeconds(1.0)) };
                Storyboard.SetTarget(fadeInAnimation, MobileAutoSuggestBox);
                Storyboard.SetTargetProperty(fadeInAnimation, "Opacity");
                fadeInStoryboard.Children.Add(fadeInAnimation);
                fadeInStoryboard.Begin();
                MobileAutoSuggestBox.Focus(FocusState.Programmatic);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Detects the native back behavior which currently skips the framework extension
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MainFrame_Navigated(object sender, NavigationEventArgs e)
    {
        App.ShellPageInstance.ViewModel.TabBarIsVisible = true;
        if (e.NavigationMode == Microsoft.UI.Xaml.Navigation.NavigationMode.Back)
        {
            ViewModel.Initialize(new NavigationParameters());

            //set current index for TabBar on smartphones
#if __ANDROID__ || __IOS__
            if (App.ShellPageInstance.MainFrame.Content is UIElement uIElement)
            {
                switch (uIElement)
                {
                    case AccountCodePage:
                        App.ShellPageInstance.ViewModel.SelectedIndex = 0;
                        break;
                    case SettingPage:
                        App.ShellPageInstance.ViewModel.SelectedIndex = 2;
                        break;
                    default:
                        break;
                }
            }
#endif
        }
    }

    private void CreateTeachingTip(FrameworkElement element)
    {
        TeachingTip teachingTip = new TeachingTip
        {
            Target = element,
            Content = Strings.Resources.AccountCodePageCopyCodeTeachingTip,
            IsLightDismissEnabled = true,
            BorderBrush = new SolidColorBrush((Color)App.Current.Resources["SystemAccentColor"]),
            IsOpen = true,
        };
        teachingTip.Closed += (_, _) => MainGrid.Children.Remove(teachingTip);
        MainGrid.Children.Add(teachingTip);
    }

    /// <summary>
    /// Copies the current generated TOTP of the entry into the clipboard
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void BTN_CopyCode_Click(object sender, RoutedEventArgs e)
    {
        if (AccountFromSender(sender) is TwoFACodeModel model)
        {
            if (await ViewModel.CopyCodeToClipboardCommandTask(model))
            {
                // via xaml flyout
                CreateTeachingTip(sender as FrameworkElement);
            }
        }
    }

    private async void BTN_EditItem_Click(object sender, RoutedEventArgs e)
    {
        if (AccountFromSender(sender) is TwoFACodeModel model)
        {
            await ViewModel.EditAccountCommandTask(model);
        }
    }

    private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ViewModel?.SetSuggestionList(sender.Text,true);
            UpdateCollectionSummary();
        }
    }

    private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is TwoFACodeModel item)
        {
            if (item.Label != Strings.Resources.AccountCodePageSearchNotFound)
            {
                ViewModel.TwoFADataService.ACVCollection.Filter = x => ((TwoFACodeModel)x) == item;
                ViewModel.SearchedAccountLabel = item.Label;
            }
            else
            {
                ViewModel.SearchedAccountLabel = string.Empty;
                ViewModel.TwoFADataService.ACVCollection.Filter = null;
            }
        }
        else
        {
            ViewModel.SearchedAccountLabel = string.Empty;
            ViewModel.TwoFADataService.ACVCollection.Filter = null;
        }
    }

    private async void BTN_AddAccountManual_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.AddAccountManual();
    }

    private async void BTN_AddAccountCamera_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.AddAccountWithCamera();
    }

    private async void BTN_ShareItem_Click(object sender, RoutedEventArgs e)
    {
        if (AccountFromSender(sender) is TwoFACodeModel model)
        {
            await ViewModel.ExportQRCodeCommandTask(model);
        }
    }
    private void ABB_ShowActions_Click(object sender, RoutedEventArgs e)
    {
        if (sender is AppBarButton abbtn)
        {
            FlyoutBase.ShowAttachedFlyout(abbtn);
            //ViewModel.SendCategoryFilterUpdatate();
        }
    }

    private async void BTN_DeleteItem_Click(object sender, RoutedEventArgs e)
    {
        if (AccountFromSender(sender) is TwoFACodeModel model)
        {
            await ViewModel.DeleteAccountCommandTask(model);   
        }
    }

    private async void BTN_SetFavourite_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button mfi && mfi.DataContext is TwoFACodeModel model)
        {
            await ViewModel.SetFavouriteCommandTask(model);
        }
    }

    private void AutoSuggestBox_GotFocus(object sender, RoutedEventArgs e)
    {
        ViewModel.SetSuggestionList(ViewModel.SearchedAccountLabel, false);
    }

    private void BTN_ShowCode_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is TwoFACodeModel model)
        {
            ViewModel.HideOrShowTOTPCodeCommandTask(model);
        }
    }

    private async void MFI_DeleteAccount_Click(object sender, RoutedEventArgs e)
    {
        if (AccountFromSender(sender) is TwoFACodeModel model)
        {
            await ViewModel.DeleteAccountCommandTask(model);
        }
    }

    private async void MFI_EditAccount_Click(object sender, RoutedEventArgs e)
    {
        if (AccountFromSender(sender) is TwoFACodeModel model)
        {
            await ViewModel.EditAccountCommandTask(model);
        }
    }

    private async void MFI_ExportAccount_Click(object sender, RoutedEventArgs e)
    {
        if (AccountFromSender(sender) is TwoFACodeModel model)
        {
            await ViewModel.ExportQRCodeCommandTask(model);
        }
    }
}
