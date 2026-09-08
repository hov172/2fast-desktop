using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Project2FA.ViewModels;
using Project2FA.Repository.Models;
using Project2FA.UnoApp;
using UNOversal.Ioc;
using UNOversal.Services.Dialogs;

namespace Project2FA.Uno.Views;

public sealed partial class AddAccountPage : Page
{
    public AddAccountPageViewModel ViewModel => DataContext as AddAccountPageViewModel;
    public AddAccountPage()
    {
        this.InitializeComponent();
        FormKeyboard.Attach(this, () => PrimaryButton);
        // Refresh x:Bind when the DataContext changes.
        DataContextChanged += (s, e) => Bindings.Update();
    }

    private async void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            await ViewModel.SearchAccountFonts(sender.Text);
        }
    }

    private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is FontIdentifikationModel selectedItem)
        {
            if (selectedItem.Name != Strings.Resources.AccountCodePageSearchNotFound)
            {
                ViewModel.AccountIconName = selectedItem.Name;
            }
            else
            {
                sender.Text = string.Empty;
            }
        }
    }

    private void AutoSuggestBox_GotFocus(object sender, RoutedEventArgs e)
    {
        ViewModel.SearchAccountFonts(ViewModel.AccountIconName);
    }

    private void BTN_SearchIcon_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            FlyoutBase.ShowAttachedFlyout(btn);
        }
    }

    private void REB_Notes_TextChanged(object sender, RoutedEventArgs e)
    {
        //ViewModel.Model.Notes = Toolbar.Formatter?.Text;
    }

    private void BTN_Expertsettings_Help_Click(object sender, RoutedEventArgs e)
    {
        //AutoCloseTeachingTip teachingTip = new AutoCloseTeachingTip
        //{
        //    Target = sender as FrameworkElement,
        //    Subtitle = Strings.Resources.AddAccountCodeContentDialogExpertSettingsHelp,
        //    AutoCloseInterval = 8000,
        //    IsLightDismissEnabled = true,
        //    BorderBrush = new SolidColorBrush((Color)App.Current.Resources["SystemAccentColor"]),
        //    IsOpen = true,
        //};
        //RootGrid.Children.Add(teachingTip);
    }

    private void HLBTN_CategoryInfo(object sender, RoutedEventArgs e)
    {
        TeachingTip teachingTip = new TeachingTip
        {
            Target = sender as FrameworkElement,
            MaxWidth = 400,
            Subtitle = Strings.Resources.EditAccountContentDialogAccountCategoryInfoText,
            IsLightDismissEnabled = true,
            BorderBrush = new SolidColorBrush((Windows.UI.Color)App.Current.Resources["SystemAccentColor"]),
            IsOpen = true
        };
        RootGrid.Children.Add(teachingTip);
    }

    private async void BTN_ManageCategories_Click(object sender, RoutedEventArgs e)
    {
        var dialogService = App.Current.Container.Resolve<UNOversal.Services.Dialogs.IDialogService>();
        await dialogService.ShowDialogAsync(new ManageCategoriesContentDialog(), new UNOversal.Services.Dialogs.DialogParameters());
        // Refresh the selectable tokens with the latest global categories, keeping current selections.
        var selected = System.Linq.Enumerable.ToHashSet(
            System.Linq.Enumerable.Select(
                System.Linq.Enumerable.Where(ViewModel.GlobalTempCategories, x => x.IsSelected), x => x.Guid));
        ViewModel.GlobalTempCategories.Clear();
        foreach (var category in Project2FA.Services.DataService.Instance.GlobalCategories)
        {
            var clone = (CategoryModel)category.Clone();
            clone.IsSelected = selected.Contains(clone.Guid);
            ViewModel.GlobalTempCategories.Add(clone);
        }
        Bindings.Update();
    }

    /// <summary>
    /// Is triggered when the element is selected
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void TokenView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is CategoryModel model)
        {
            model.IsSelected = !model.IsSelected;
        }
    }

    private void SettingsExpander_Expanded(object sender, System.EventArgs e)
    {
        //SV_AccountInput.ScrollToElement(sender as FrameworkElement);
    }
}
