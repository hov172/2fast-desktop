#if TWOFAST_DESKTOP
using Project2FA.Core;
using Project2FA.Services.Desktop;
using Project2FA.UnoApp;
using Project2FA.Uno.Views;
using UNOversal.Services.Secrets;
using UNOversal.Ioc;
using Windows.Storage;

namespace Project2FA.Services;
public partial class DataService
{
    public async Task ResetMacOSSetup()
    {
        DesktopSession.CancelOperations();
        await CollectionAccessSemaphore.WaitAsync();
        try
        {
            _initialization = true;
            var secrets = App.Current.Container.Resolve<ISecretService>().Helper;
            string hash = SettingsService.Instance.DataFilePasswordHash;
            if (!string.IsNullOrEmpty(hash)) secrets.RemoveSecret(Constants.ContainerName, hash);
            foreach (string key in new[] { "WDPassword", "WDUsername", "WDServerAddress", Constants.ActivatedDatafileHashName })
                secrets.RemoveSecret(Constants.ContainerName, key);
            SecretHelper.ClearSession();
            Collection.Clear(); GlobalCategories.Clear(); ActivatedDatafile = null;
            ApplicationData.Current.LocalSettings.Values.Clear();
            _errorOccurred = false; IsLoading = false;
        }
        finally { _initialization = false; CollectionAccessSemaphore.Release(); }
        App.ShellPageInstance.ViewModel.NavigationIsAllowed = false;
        await App.ShellPageInstance.ViewModel.NavigationService.NavigateAsync("/" + nameof(WelcomePage));
    }
}
#endif
