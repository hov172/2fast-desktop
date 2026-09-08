using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Project2FA.Repository.Models;
using Project2FA.UnoApp;
using Project2FA.ViewModels;
using UNOversal.Ioc;
using UNOversal.Services.Dialogs;
using System;
using System.Collections.ObjectModel;
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
                ViewModel.SearchAccountFonts(sender.Text);
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

        private void ReplaceNoteFontColor(bool isLightTheme)
        {
            if (isLightTheme)
            {
                ViewModel.Notes = ViewModel.Notes.Replace(@"\red255\green255\blue255", @"\red0\green0\blue0");
            }
            else
            {
                ViewModel.Notes = ViewModel.Notes.Replace(@"\red0\green0\blue0", @"\red255\green255\blue255");
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

        private void SettingsExpander_Expanded(object sender, EventArgs e)
        {

        }
    }
}
