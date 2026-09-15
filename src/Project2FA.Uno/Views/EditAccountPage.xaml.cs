using Project2FA.Repository.Models;
using Project2FA.UnoApp;
using Project2FA.ViewModels;
using UNOversal.Ioc;
using UNOversal.Services.Dialogs;
using Windows.UI;

namespace Project2FA.Uno.Views
{
    public sealed partial class EditAccountPage : Page
    {
        public EditAccountPageViewModel ViewModel => DataContext as EditAccountPageViewModel;
        public EditAccountPage()
        {
            this.InitializeComponent();
            // Refresh x:Bind when the DataContext changes.
            DataContextChanged += (s, e) => Bindings.Update();
        }

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                _ = ViewModel.SearchAccountFonts(sender.Text); // event handler cannot await
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
                    ViewModel.AccountIconName = string.Empty;
                }
            }
        }

        /// <summary>
        /// Automatic search when the control has the focus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void AutoSuggestBox_GotFocus(object sender, RoutedEventArgs e)
        {
            await ViewModel.SearchAccountFonts(ViewModel.AccountIconName);
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

        private void REB_Notes_TextChanged(object sender, RoutedEventArgs e)
        {
            //ViewModel.Notes = Toolbar.Formatter?.Text;
        }

        private void HLBTN_CategoryInfo(object sender, RoutedEventArgs e)
        {
            TeachingTip teachingTip = new TeachingTip
            {
                Target = sender as FrameworkElement,
                MaxWidth = 250,
                Subtitle = Strings.Resources.EditAccountContentDialogAccountCategoryInfoText,
                IsLightDismissEnabled = false,
                BorderBrush = new SolidColorBrush((Color)App.Current.Resources["SystemAccentColor"]),
                IsOpen = true
            };
            RootGrid.Children.Add(teachingTip);
        }

        private async void BTN_ManageCategories_Click(object sender, RoutedEventArgs e)
        {
            var dialogService = App.Current.Container.Resolve<IDialogService>();
            await dialogService.ShowDialogAsync(new ManageCategoriesContentDialog(), new DialogParameters());
            // Refresh the selectable tokens with the latest global categories, keeping current selections.
            var selected = ViewModel.GlobalTempCategories.Where(x => x.IsSelected).Select(x => x.Guid).ToHashSet();
            ViewModel.GlobalTempCategories.Clear();
            foreach (var category in Project2FA.Services.DataService.Instance.GlobalCategories)
            {
                var clone = (CategoryModel)category.Clone();
                clone.IsSelected = selected.Contains(clone.Guid);
                ViewModel.GlobalTempCategories.Add(clone);
            }
            Bindings.Update();
        }

        private void SettingsExpander_Expanded(object sender, EventArgs e)
        {

        }
    }
}
