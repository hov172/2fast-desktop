using CommunityToolkit.WinUI;
using Project2FA.Controls;
using Project2FA.UnoApp;
using Project2FA.ViewModels;
using Symptum.UI.Markdown;
using Windows.System;
using Windows.UI;

namespace Project2FA.Uno.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TutorialPage : Page
	{
        public TutorialPageViewModel ViewModel => DataContext as TutorialPageViewModel;
        public TutorialPage()
        {
            this.InitializeComponent();
            this.Loaded += TutorialPage_Loaded;
        }

        private void TutorialPage_Loaded(object sender, RoutedEventArgs e)
        {
#if !__MOBILE__
            App.ShellPageInstance.ShellViewInternal.Header = string.Empty;
            //App.ShellPageInstance.ShellViewInternal.HeaderTemplate = ShellHeaderTemplate;
#endif
        }

        /// <summary>
        /// Create the TeachingTip for the specific framework element
        /// </summary>
        /// <param name="element"></param>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        private async Task CreateTeachingTip(FrameworkElement element, string title, string content)
        {
            TextBlock txt = new TextBlock { Text = content, TextWrapping = TextWrapping.WrapWholeWords };
            var control = MainGrid.FindDescendant(nameof(TeachingTip));
            if (control != null)
            {
                var tooltip = (control as TeachingTip);
                if (tooltip.IsOpen)
                {
                    tooltip.IsOpen = false;
                    await Task.Delay(500);
                }
                tooltip.Title = title;
                tooltip.Content = txt;
                tooltip.Target = element;
                tooltip.IsOpen = true;
            }
            else
            {
                TeachingTip teachingTip = new TeachingTip
                {
                    Target = element,
                    Name = nameof(TeachingTip),
                    Content = txt,
                    IconSource = new Microsoft.UI.Xaml.Controls.SymbolIconSource { Symbol = Symbol.Help },
                    BorderBrush = new SolidColorBrush((Color)App.Current.Resources["SystemAccentColor"]),
                    IsOpen = true,
                };
                MainGrid.Children.Add(teachingTip);
            }
        }

        /// <summary>
        /// Shows the TeachingTip of a tutorial item for the element that raised the event
        /// </summary>
        private void ShowItemTeachingTip(object sender, string title, string content)
        {
            if (sender is FrameworkElement element)
            {
                CreateTeachingTip(element, title, content).ConfigureAwait(false);
            }
        }

        private void BTN_SetFavourite_Click(object sender, RoutedEventArgs e)
            => ShowItemTeachingTip(sender, Strings.Resources.TutorialPageItemFavouriteBTNTitle, Strings.Resources.TutorialPageItemFavouriteBTNTDesc);

        private void BTN_CopyCode_Click(object sender, RoutedEventArgs e)
            => ShowItemTeachingTip(sender, Strings.Resources.TutorialPageItemCopyCodeBTNTitle, Strings.Resources.TutorialPageItemCopyCodeBTNDesc);

        private void BTN_ShowCode_Click(object sender, RoutedEventArgs e)
            => ShowItemTeachingTip(sender, Strings.Resources.TutorialPageItemShowCodeBTNTitle, Strings.Resources.TutorialPageItemShowCodeBTNDesc);

        private void TutorialPageItemMoreBTN_Click(object sender, RoutedEventArgs e)
            => ShowItemTeachingTip(sender, Strings.Resources.TutorialPageItemMoreBTNTitle, Strings.Resources.TutorialPageItemMoreBTNDesc);

        private void FV_Tutorials_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel?.SelectedIndex != 4)
            {
                return;
            }
            if (MainGrid.FindDescendant(nameof(TeachingTip)) is TeachingTip tooltip && tooltip.IsOpen)
            {
                tooltip.IsOpen = false;
            }
        }

        private async void MarkdownTextBlock_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            if (Uri.TryCreate(e.Uri.ToString(), UriKind.Absolute, out Uri link))
            {
                await Launcher.LaunchUriAsync(link);
            }

        }

        private void HLBTN_PasswordInfo(object sender, RoutedEventArgs e)
        {
            var markdownText = new MarkdownTextBlock
            {
                Margin = new Thickness(8, 8, 8, 8),
                Text = Strings.Resources.TutorialPagePasswordInfo
            };
            markdownText.OnLinkClicked += MarkdownTextBlock_LinkClicked;
            AutoCloseTeachingTip teachingTip = new AutoCloseTeachingTip
            {
                Target = sender as FrameworkElement,
                HeroContent = markdownText,
                AutoCloseInterval = 8000,
                IsLightDismissEnabled = true,
                BorderBrush = new SolidColorBrush((Color)App.Current.Resources["SystemAccentColor"]),
                IsOpen = true,
            };
            MainGrid.Children.Add(teachingTip);
        }
    }
}
