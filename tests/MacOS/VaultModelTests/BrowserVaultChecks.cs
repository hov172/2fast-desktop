using System.Collections;
using System.Reflection;
// Reverse-direction interop: a V4 vault written by the 2fast Browser extension's WebCrypto codec
// (github.com/hov172/2fast-browser, src/vault.js) must open with the desktop codec.
// Fixture: fixtures/browser-written.2fa — synthetic accounts, password below.
internal static class BrowserVaultChecks
{
    internal static void Run(Assembly app)
    {
        var codec = app.GetType("Project2FA.Services.Desktop.DesktopVaultCodec", true)!;
        var decrypt = codec.GetMethod("Decrypt", BindingFlags.Static | BindingFlags.NonPublic)!;
        var datafileType = app.GetType("Project2FA.Repository.Models.DatafileModel", true)!;
        var accountType = app.GetType("Project2FA.Repository.Models.TwoFACodeModel", true)!;
        string content = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", "browser-written.2fa"));
        const string password = "Browser pass";
        int assertions = 0;
        void Check(bool ok, string what) { if (!ok) throw new Exception("Browser vault: " + what); assertions++; }

        try { decrypt.Invoke(null, new object[] { content, "wrong" }); throw new Exception("Browser vault: wrong password accepted."); }
        catch (TargetInvocationException) { assertions++; }

        var model = decrypt.Invoke(null, new object[] { content, password })!;
        var accounts = (IList)datafileType.GetProperty("Collection")!.GetValue(model)!;
        var categories = (IList)datafileType.GetProperty("GlobalCategories")!.GetValue(model)!;
        Check(accounts.Count == 3, "expected 3 accounts, got " + accounts.Count);
        Check(categories.Count == 1, "expected 1 category, got " + categories.Count);

        object Get(object o, string p) => o.GetType().GetProperty(p)!.GetValue(o)!;
        var github = accounts[0]!; var aws = accounts[1]!; var browser = accounts[2]!;
        Check((string)Get(github, "Issuer") == "GitHub" && (int)Get(github, "TotpSize") == 6 && Get(github, "HashMode").ToString() == "Sha1", "GitHub account fields");
        Check(((byte[])Get(github, "SecretByteArray")).SequenceEqual(new byte[] { 72, 101, 108, 108, 111, 33, 0xde, 0xad, 0xbe, 0xef }), "GitHub seed bytes");
        Check((string)Get(aws, "Issuer") == "Amazon Web Services" && (int)Get(aws, "TotpSize") == 8 && Get(aws, "HashMode").ToString() == "Sha256", "AWS 8-digit SHA-256 fields");
        Check((string)Get(browser, "Issuer") == "Browser" && (string)Get(browser, "OTPType") == "totp", "account created in the browser");

        var category = categories[0]!;
        Check((string)Get(category, "Name") == "Work" && (string)Get(category, "UnicodeString") == "💼", "category name and icon");
        var linked = (IList)Get(github, "SelectedCategories");
        Check(linked.Count == 1 && (Guid)Get(linked[0]!, "Guid") == (Guid)Get(category, "Guid"), "account linked to category by Guid");
        Check(((IList)Get(browser, "SelectedCategories")).Count == 0, "untagged account has no categories");
        Console.WriteLine($"Compiled app: browser-written V4 vault opened with desktop codec ({assertions} assertions).");
    }
}
