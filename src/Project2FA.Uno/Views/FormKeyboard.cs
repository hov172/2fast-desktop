using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace Project2FA.Uno.Views;

/// <summary>Submit a form from a single-line field, including keys consumed by Uno text controls.</summary>
internal static class FormKeyboard
{
    internal static void Attach(Page page, Func<Button> submit)
    {
        page.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler((_, e) =>
        {
            if (e.Key != VirtualKey.Enter || e.KeyStatus.WasKeyDown) return;
            if (TrySubmit(page, e.OriginalSource as DependencyObject, submit())) e.Handled = true;
        }), handledEventsToo: true);
    }

    internal static bool TrySubmit(Page page, DependencyObject source, Button button)
    {
        bool editable = false;
        for (var node = source; node != null && node != page;
             node = VisualTreeHelper.GetParent(node))
        {
            if (node is RichEditBox or AutoSuggestBox) return false;
            if (node is TextBox text)
            {
                if (text.AcceptsReturn || text.IsReadOnly) return false;
                editable = true;
            }
            if (node is PasswordBox) editable = true;
        }
        if (!editable) return false;
        var command = button?.Command;
        if (button?.IsEnabled != true || button.Visibility != Visibility.Visible ||
            command?.CanExecute(button.CommandParameter) != true) return false;
        command.Execute(button.CommandParameter);
        return true;
    }
}
