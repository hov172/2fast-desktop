using System.Reflection;
using System.Xml.Linq;

internal static class WorkflowChecks
{
    internal static void Run(Assembly app, string root)
    {
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var views = Path.Combine(root, "src/Project2FA.Uno/Views");
        var shell = XDocument.Load(Path.Combine(views, "ShellPage.xaml"));
        var nav = shell.Descendants().Single(e => (string)e.Attribute(x + "Name") == "ShellView");
        var handler = (string)nav.Attribute("ItemInvoked");
        if (string.IsNullOrEmpty(handler) || app.GetType("Project2FA.Uno.Views.ShellPage")!.GetMethod(handler, BindingFlags.NonPublic | BindingFlags.Instance) == null)
            throw new Exception("Sidebar has no compiled navigation event handler.");
        var settings = XDocument.Load(Path.Combine(views, "SettingPage.xaml"));
        foreach (string action in new[] { "RenameVault_Click", "MoveVault_Click", "BackupVault_Click", "PasswordVault_Click", "OpenVault_Click", "NewVault_Click", "UpgradeVault_Click" })
            if (!settings.Descendants().Any(e => (string)e.Attribute("Click") == action) || app.GetType("Project2FA.Uno.Views.SettingPage")!.GetMethod(action, BindingFlags.NonPublic | BindingFlags.Instance) == null)
                throw new Exception("Missing data-file action: " + action);
        var passwordDialog = XDocument.Load(Path.Combine(views, "ContentDialog/ChangeDatafilePasswordContentDialog.xaml"));
        if (passwordDialog.Descendants().Count(e => e.Name.LocalName == "PasswordBox") != 3 ||
            (string)passwordDialog.Root!.Attribute("PrimaryButtonClick") != "ChangePassword_Click" ||
            string.IsNullOrEmpty((string)passwordDialog.Root!.Attribute("CloseButtonText")))
            throw new Exception("Password-change dialog lacks its fields or completion controls.");
        Console.WriteLine("Data-file controls: seven compiled actions and complete password form verified.");
        var edit = XDocument.Load(Path.Combine(views, "ContentDialog/EditAccountContentDialog.xaml"));
        foreach (var attr in new[] { "PrimaryButtonText", "CloseButtonText", "PrimaryButtonClick" })
            if (string.IsNullOrEmpty((string)edit.Root!.Attribute(attr))) throw new Exception("Edit dialog cannot save/close: " + attr);
        foreach (var property in new[] { "Label", "Issuer", "Notes" })
        {
            if (!edit.Descendants().Any(e => (string)e.Attribute("Text") == "{x:Bind ViewModel." + property + ", Mode=TwoWay}"))
                throw new Exception("Missing active edit field: " + property);
            if (app.GetType("Project2FA.ViewModels.EditAccountContentDialogViewModel")!.GetProperty(property)?.CanWrite != true)
                throw new Exception("Edit field has no writable view-model property: " + property);
        }
        var accounts = XDocument.Load(Path.Combine(views, "AccountCodePage.xaml"));
        foreach (var template in accounts.Descendants().Where(e => new[] { "TwoFACodeCustomTemplate", "TwoFACodeCustomAccentTemplate" }.Contains((string)e.Attribute(x + "Key"))))
        {
            foreach (var grid in template.Descendants().Where(e => e.Name.LocalName == "Grid"))
            {
                bool contentStarted = false;
                foreach (var child in grid.Elements())
                {
                    bool definition = child.Name.LocalName is "Grid.RowDefinitions" or "Grid.ColumnDefinitions";
                    if (definition && contentStarted) throw new Exception("Grid definitions interrupt card content: Uno can omit subsequent children.");
                    if (!child.Name.LocalName.Contains('.')) contentStarted = true;
                }
            }
            foreach (var binding in new[] { "Label", "Issuer", "TwoFACode", "Seconds" })
                if (!template.Descendants().Any(e => ((string)e.Attribute("Text"))?.StartsWith("{x:Bind " + binding + ",") == true))
                    throw new Exception("Account card lacks visible binding: " + binding);
            var rows = template.Descendants().First(e => e.Name.LocalName == "Grid.RowDefinitions");
            if (rows.Elements().Any(e => !string.Equals((string)e.Attribute("Height"), "Auto", StringComparison.OrdinalIgnoreCase)))
                throw new Exception("Account rows must size to their content.");
        }
        Console.WriteLine("Both account templates retain names, code, countdown, content-sized rows, and uninterrupted grid content.");
        var actions = accounts.Descendants().Where(e => new[] { "MFI_EditAccount_Click", "MFI_ExportAccount_Click", "MFI_DeleteAccount_Click", "BTN_CopyCode_Click", "BTN_SetFavourite_Click", "OcraChallenge_Click" }.Contains((string)e.Attribute("Click"))).ToList();
        if (actions.Count != 20 || actions.Any(e => (string)e.Attribute("Tag") != "{x:Bind}"))
            throw new Exception("Account actions must carry their account explicitly in both templates.");
        var png = (byte[])app.GetType("Project2FA.Services.QrImageRenderer")!.GetMethod("RenderPng", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { "otpauth://totp/Regression:Account?secret=JBSWY3DPEHPK3PXP&issuer=Regression" })!;
        if (!png.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) throw new Exception("QR renderer did not produce a PNG.");
        File.WriteAllBytes(Path.Combine(root, "build/generated-qr-regression.png"), png);
        var qrDialog = XDocument.Load(Path.Combine(views, "ContentDialog/DisplayQRCodeContentDialog.xaml"));
        if (!qrDialog.Descendants().Any(e => e.Name.LocalName == "Image" && ((string)e.Attribute("Source"))?.Contains("ViewModel.QRImage") == true))
            throw new Exception("QR dialog is not connected to the PNG image source.");
        Console.WriteLine("QR PNG rendered by compiled app and dialog image binding verified.");
        Console.WriteLine("Workflow contracts: sidebar handler, editable/closable dialog, and 20 account-action bindings passed.");
    }
}
