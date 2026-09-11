namespace Project2FA.Services;

/// <summary>
/// Narrow shell seam supplied by the application head at startup.
/// </summary>
internal interface IShellContext
{
    dynamic Shell { get; }
    Project2FA.ViewModels.ShellPageViewModel ViewModel { get; }
    dynamic XamlRoot { get; }
    dynamic MainFrame { get; }
    Task RunAsync(Func<Task> action);
    void SetTitleBarAsDraggable();
    void SetupBackButton();
}

internal static class ShellContext
{
    private static IShellContext? current;
    internal static IShellContext Current => current
        ?? throw new InvalidOperationException("The application shell has not been initialized.");
    internal static void Configure(IShellContext context) => current = context
        ?? throw new ArgumentNullException(nameof(context));
}
