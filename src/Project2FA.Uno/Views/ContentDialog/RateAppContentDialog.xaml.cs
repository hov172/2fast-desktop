using Project2FA.Services;

namespace Project2FA.Uno.Views;

public sealed partial class RateAppContentDialog : ContentDialog
{
    public RateAppContentDialog()
    {
        this.InitializeComponent();
        // Refresh x:Bind when the DataContext changes.
        DataContextChanged += (s, e) => Bindings.Update();
    }

    private async void BTN_RateAppYes_Click(object sender, RoutedEventArgs e)
    {
        // TODO for Android and iOS release
#if __ANDROID__ || __IOS__
        //await Windows.Services.Store.StoreContext.GetDefault().RequestRateAndReviewAppAsync();
#endif
        Hide();
    }

    private void BTN_RateAppNo_Click(object sender, RoutedEventArgs e)
    {
        var setting = SettingsService.Instance;
        setting.AppRated = true;
        Hide();
    }

    private void BTN_RateAppLater_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}
