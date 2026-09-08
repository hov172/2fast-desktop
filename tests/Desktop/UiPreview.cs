#if TWOFAST_UI_PREVIEW
using Microsoft.UI.Xaml;
using Project2FA.Repository.Models;
using Project2FA.Services;
using Project2FA.Uno.Views;
using Project2FA.UnoApp;
using Project2FA.ViewModels;

internal static class DesktopUiPreview
{
    // Compiled only by an explicit developer preview build. Never opens a vault,
    // calls account initialization, or enables account mutations.
    internal static void Show(Window window)
    {
        if (Environment.GetEnvironmentVariable("TWOFAST_UI_KEYBOARD") == "1")
        {
            ShowKeyboardCheck(window);
            return;
        }
        var data = DataService.Instance;
        data.DisableUiPreviewAutosave();
        foreach (var entry in new[] { ("Personal email", "Example Mail", "123456"), ("Work account", "Example Workspace", "654321"), ("Developer account", "Example Code", "246810") })
            data.Collection.Add(new TwoFACodeModel { Label = entry.Item1, Issuer = entry.Item2, TwoFACode = entry.Item3, Seconds = 24, Period = 30 });
        var page = new AccountCodePage { DataContext = new AccountCodePageViewModel(null, App.ShellPageInstance.ViewModel.NavigationService, null) };
        App.ShellPageInstance.MainFrame.Content = page;
        App.ShellPageInstance.ViewModel.NavigationIsAllowed = true;
        App.ShellPageInstance.IsHitTestVisible = false;
        window.Content = App.ShellPageInstance;
        window.Title = "2fast — READ-ONLY DESIGN PREVIEW (sample accounts)";
        window.Activate();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) => {
            timer.Stop();
            var list = (Microsoft.UI.Xaml.Controls.ListView)page.FindName("LV_AccountCollection");
            System.IO.File.AppendAllText("/private/tmp/twofast-ui-preview-tree.log", $"Items={list.Items.Count} Source={list.ItemsSource?.GetType().Name} Template={list.ItemTemplateSelector?.SelectTemplate(data.Collection[0], list)?.GetType().Name}\n");
            Dump(page, 0);
        };
        timer.Start();
    }
    private static void ShowKeyboardCheck(Window window)
    {
        var input = new Microsoft.UI.Xaml.Controls.PasswordBox { Header = "Synthetic keyboard test", MaxLength = 0 };
        var submit = new Microsoft.UI.Xaml.Controls.Button { Content = "Submit sample" };
        int count = 0;
        submit.Command = new CommunityToolkit.Mvvm.Input.RelayCommand(() =>
        {
            count++;
            System.IO.File.WriteAllText("/private/tmp/twofast-keyboard-result.txt", $"Submissions={count}");
        });
        var panel = new Microsoft.UI.Xaml.Controls.StackPanel { Margin = new Thickness(40), Spacing = 20 };
        panel.Children.Add(input); panel.Children.Add(submit);
        var page = new Microsoft.UI.Xaml.Controls.Page { Content = panel };
        FormKeyboard.Attach(page, () => submit);
        page.Loaded += (_, _) => input.Focus(FocusState.Programmatic);
        window.Content = page;
        window.Title = "2fast — SYNTHETIC KEYBOARD CHECK";
        window.Activate();
        var multiline = new Microsoft.UI.Xaml.Controls.TextBox { AcceptsReturn = true };
        var single = new Microsoft.UI.Xaml.Controls.TextBox();
        var suggestions = new Microsoft.UI.Xaml.Controls.AutoSuggestBox();
        if (!FormKeyboard.TrySubmit(page, input, submit) || count != 1) throw new Exception("Password Enter submission failed");
        if (!FormKeyboard.TrySubmit(page, single, submit) || count != 2) throw new Exception("Text Enter submission failed");
        if (FormKeyboard.TrySubmit(page, multiline, submit) || FormKeyboard.TrySubmit(page, suggestions, submit) || count != 2) throw new Exception("Enter stole multiline/autocomplete behavior");
        submit.IsEnabled = false;
        if (FormKeyboard.TrySubmit(page, input, submit) || count != 2) throw new Exception("Disabled form submitted");
        submit.IsEnabled = true;
        submit.Command = new CommunityToolkit.Mvvm.Input.RelayCommand(() => count++, () => false);
        if (FormKeyboard.TrySubmit(page, input, submit) || count != 2) throw new Exception("Command CanExecute bypassed");
        System.IO.File.WriteAllText("/private/tmp/twofast-keyboard-result.txt", "PASS: password and single-line submit; multiline, suggestions, disabled button and unavailable command do not submit.");
    }
    private static void Dump(DependencyObject node, int depth)
    {
        if (node is FrameworkElement f) System.IO.File.AppendAllText("/private/tmp/twofast-ui-preview-tree.log", new string(' ', depth) + node.GetType().Name + " " + f.Name + $" {f.ActualWidth}x{f.ActualHeight}\n");
        for (int i=0;i<Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(node);i++) Dump(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(node,i), depth+1);
    }
}
namespace Project2FA.Services
{
    public partial class DataService
    {
        internal void DisableUiPreviewAutosave()
        {
            Collection.CollectionChanged -= Accounts_CollectionChanged;
            Collection.Clear();
        }
    }
}
#endif
