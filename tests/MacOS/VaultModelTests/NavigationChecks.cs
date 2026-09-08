#nullable enable
using System.Reflection;
using System.Runtime.CompilerServices;

internal static class NavigationChecks
{
    internal static void Run(Assembly app)
    {
        var appType = app.GetType("Project2FA.UnoApp.App", true)!;
        var register = appType.GetMethod("RegisterTypes")!;
        var registryType = register.GetParameters()[0].ParameterType;
        // Run the actual compiled registration method without starting UI or opening a vault.
        var registry = DispatchProxy.Create(registryType, typeof(RegistrationSink));
        register.Invoke(RuntimeHelpers.GetUninitializedObject(appType), new[] { registry });
        var parametersType = app.GetType("UNOversal.Navigation.NavigationParameters", true)!;
        var parameters = Activator.CreateInstance(parametersType)!;
        var pathType = app.GetType("UNOversal.Navigation.NavigationPath", true)!;
        var route = Activator.CreateInstance(pathType, new object[] { 0, "AddAccountPage", parameters })!;
        if ((Type)pathType.GetProperty("View")!.GetValue(route)! != app.GetType("Project2FA.Uno.Views.AddAccountPage") ||
            (Type)pathType.GetProperty("ViewModel")!.GetValue(route)! != app.GetType("Project2FA.ViewModels.AddAccountPageViewModel"))
            throw new Exception("Account review route does not resolve its page and view model.");
        foreach (string page in new[] { "SettingPage?PivotItem=0", "SettingPage?PivotItem=1", "SettingPage?PivotItem=2", "AccountCodePage" })
            Activator.CreateInstance(pathType, new object[] { 0, page, parameters });
        var decodedRoute = Activator.CreateInstance(pathType, new object[] { 0, "SettingPage?PivotItem=2&name=hello+world&literal=%252F&value=a%3Db", parameters })!;
        var decoded = (System.Collections.IEnumerable)pathType.GetProperty("Parameters")!.GetValue(decodedRoute)!;
        var query = new Dictionary<string, object>();
        foreach (var entry in decoded)
            query.Add((string)entry.GetType().GetProperty("Key")!.GetValue(entry)!, entry.GetType().GetProperty("Value")!.GetValue(entry)!);
        if (!Equals(query["PivotItem"], "2") || !Equals(query["name"], "hello world") || !Equals(query["literal"], "%2F") || !Equals(query["value"], "a=b"))
            throw new Exception("Navigation query decoding failed or decoded twice.");
        Console.WriteLine("Compiled app: account review, accounts, three sidebar routes, and query decoding passed.");
    }
}

public class RegistrationSink : DispatchProxy
{
    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        if (method!.ReturnType == typeof(void)) return null;
        if (method.ReturnType.IsInstanceOfType(this)) return this;
        return method.ReturnType.IsValueType ? Activator.CreateInstance(method.ReturnType) : null;
    }
}
