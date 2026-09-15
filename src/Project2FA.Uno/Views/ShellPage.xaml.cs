using Project2FA.UnoApp;
using Project2FA.ViewModels;
using UNOversal.Ioc;
using UNOversal.Navigation;
using Windows.UI.Core;
using Frame = Microsoft.UI.Xaml.Controls.Frame;

namespace Project2FA.Uno.Views
{
    public sealed partial class ShellPage : Page
    {
        private SystemNavigationManager _navManager;
        
        public NavigationView ShellViewInternal { get; private set; }

        public ShellPageViewModel ViewModel { get; } = new ShellPageViewModel();

        public Frame MainFrame { get; }

        private string _settingsNavigationStr;

        private object PreviousItem { get; set; }
        public ShellPage()
        {
            this.InitializeComponent();
            _navManager = SystemNavigationManager.GetForCurrentView();
            _settingsNavigationStr = "SettingPage?PivotItem=0";
            ShellViewInternal = ShellView;
            ShellView.Content = MainFrame = new Frame();
#if TWOFAST_DESKTOP
            MainFrame.Navigating += (_, args) =>
            {
                if (!ViewModel.NavigationIsAllowed && !CanNavigateWhileLocked(args.SourcePageType))
                    args.Cancel = true;
            };
#endif
            ViewModel.NavigationService = NavigationFactory.Create(MainFrame);

            SetupGestures();
        }

        internal static bool CanNavigateWhileLocked(Type page) =>
            page == typeof(LoginPage) || page == typeof(TutorialPage) ||
            page == typeof(WelcomePage) || page == typeof(NewDataFilePage) ||
            page == typeof(UseDataFilePage) || page == typeof(FileActivationPage) ||
            page == typeof(BlankPage);

        private void SetupGestures()
        {
            _navManager.BackRequested += NavManager_BackRequested;
#pragma warning disable AsyncFixer03 // Fire-and-forget async-void methods or delegates
            ShellView.BackRequested += async (s, e) => await ViewModel.NavigationService.GoBackAsync();
#pragma warning restore AsyncFixer03 // Fire-and-forget async-void methods or delegates
        }

        private async void NavManager_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (ViewModel.NavigationService.CanGoBack())
            {
                e.Handled = true;
                await ViewModel.NavigationService.GoBackAsync();
#if ANDROID || IOS
                if (App.ShellPageInstance.MainFrame.Content is UIElement uIElement)
                {
                    switch (uIElement)
                    {
                        case AccountCodePage:
                            ViewModel.SelectedIndex = 0;
                            break;
                            //  SelectedIndex =1 is not a real page
                            //case SearchPage:
                            //    ViewModel.SelectedIndex = 1;
                        case SettingPage:
                            ViewModel.SelectedIndex = 2;
                            break;
                        default:
                            break;
                    }
                }
#endif
            }
            else
            {
#if __ANDROID__ || __IOS__
                // if the search is seleted, a back command go to
                // the AccountCodePage index 0
                if (ViewModel.SelectedIndex == 1)
                {
                    e.Handled = true;
                    ViewModel.SelectedIndex = 0;
                }
#endif
            }
        }

        private async void ShellView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (!ViewModel.NavigationIsAllowed) return;
            try
            {
                await SetSelectedItem(args.IsSettingsInvoked ? sender.SettingsItem : args.InvokedItemContainer);
            }
            catch (Exception error)
            {
#if TWOFAST_DESKTOP
                DesktopScanDiagnostics.Record("sidebar navigation", error);
                await DesktopSession.Message(
                    App.Current.Container.Resolve<UNOversal.Services.Dialogs.IDialogService>(),
                    "Unable to open page", "Navigation failed. Please return to Accounts and try again.");
#else
                throw;
#endif
            }
        }

        private async Task SetSelectedItem(object selectedItem, bool withNavigation = true)
        {
            if (selectedItem == null)
            {
                ShellView.SelectedItem = null;
            }
            else if (selectedItem == ShellView.SettingsItem)
            {
                if (withNavigation)
                {
                    if ((await ViewModel.NavigationService.NavigateAsync(_settingsNavigationStr)).Success)
                    {
                        PreviousItem = selectedItem;
                        ShellView.SelectedItem = selectedItem;
                    }
                    else
                    {
                        ShellView.SelectedItem = null;
                    }
                }
                else
                {
                    PreviousItem = selectedItem;
                    ShellView.SelectedItem = selectedItem;
                }
            }
            else if (selectedItem is NavigationViewItem item)
            {
                if (item.Tag is string path)
                {
                    if (!withNavigation)
                    {
                        PreviousItem = item;
                        ShellView.SelectedItem = item;
                    }
                    else if ((await ViewModel.NavigationService.NavigateAsync(path)).Success)
                    {
                        PreviousItem = selectedItem;
                        ShellView.SelectedItem = selectedItem;
                    }
                    else
                    {
                        ShellView.SelectedItem = PreviousItem;
                    }
                }
            }
        }

    }
}
