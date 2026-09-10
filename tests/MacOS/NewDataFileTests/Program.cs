using Project2FA.ViewModels;
using Project2FA.Services.Desktop;
using Windows.Storage;

await FileTransactionChecks.Run();
var vm = new NewDataFilePageViewModel();
int passed = 0;
void Check(bool expected, string message)
{
    vm.Validate();
    if (vm.DatafileBTNActive != expected || !vm.ValidationMessage.Contains(message)) throw new Exception("Validation failed: " + message);
    passed++;
}
Check(false, "filename");
vm.DateFileName = "sample";
Check(false, "password");
vm.Password = "test-only";
Check(false, "Repeat");
vm.PasswordRepeat = "different";
Check(false, "do not match");
vm.PasswordRepeat = vm.Password;
Check(false, "folder");
var folder = Path.Combine(Path.GetTempPath(), "2fast-form-test-" + Guid.NewGuid());
Directory.CreateDirectory(folder);
try
{
    vm.LocalStorageFolder = new StorageFolder { Path = folder };
    Check(true, "");
    vm.PasswordRepeat = "";
    Check(false, "Repeat");
    vm.PasswordRepeat = vm.Password;
    File.WriteAllText(Path.Combine(folder, "sample.2fa"), "synthetic vault placeholder");
    Check(false, "already exists");
    vm.DateFileName = "sample.2fa";
    Check(false, "already exists");
    vm.DateFileName = "../escape";
    Check(false, "path separators");
    vm.DateFileName = "new vault";
    Check(true, "");
    vm.Password = vm.PasswordRepeat = new string('x', 10000) + "🔑";
    Check(true, "");
    vm.Password = "example-pass "; vm.PasswordRepeat = "example-pass";
    Check(false, "do not match");
    if (!vm.PasswordWhitespaceHint.Contains("first password") || vm.Password != "example-pass ") throw new Exception("Trailing whitespace was hidden or changed");
    passed++;
    vm.PasswordRepeat = vm.Password;
    Check(true, "");
    if (!vm.PasswordWhitespaceHint.Contains("Both")) throw new Exception("Missing whitespace hint");
    passed++;
    vm.Password = vm.PasswordRepeat = "example-pass";
    Check(true, "");
    if (vm.PasswordWhitespaceHint != "") throw new Exception("Stale whitespace hint");
    passed++;
    var vault = Path.Combine(folder, "sample.2fa");
    await DesktopVaultLocation.WriteAtomicAsync(folder, "sample.2fa", "replacement ciphertext");
    if (File.ReadAllText(vault) != "replacement ciphertext" || Directory.GetFiles(folder, ".2fast-*.tmp").Length != 0) throw new Exception("Atomic replacement failed");
    passed++;
    Directory.CreateDirectory(Path.Combine(folder, "blocked.2fa"));
    try { await DesktopVaultLocation.WriteAtomicAsync(folder, "blocked.2fa", "synthetic"); throw new Exception("Invalid destination accepted"); }
    catch (IOException) { passed++; }
    if (File.ReadAllText(vault) != "replacement ciphertext" || Directory.GetFiles(folder, ".2fast-*.tmp").Length != 0) throw new Exception("Failure changed vault or leaked temporary file");
    passed++;
    try { await DesktopVaultLocation.WriteAtomicAsync(folder, "../escape", "synthetic"); throw new Exception("Path traversal accepted"); }
    catch (ArgumentException) { passed++; }

    foreach (var savedPath in new[] { vault, folder })
    {
        var opened = await DesktopVaultLocation.OpenAsync(savedPath, "sample.2fa", false);
        if (opened.Path != vault) throw new Exception("Wrong vault location");
        passed++;
    }
    ApplicationData.Current.LocalFolder = new StorageFolder { Path = folder };
    var cached = await DesktopVaultLocation.OpenAsync("https://test.invalid/dav", "sample.2fa", true);
    if (cached.Path != vault) throw new Exception("Wrong WebDAV cache location");
    passed++;
    try
    {
        await DesktopVaultLocation.OpenAsync(Path.Combine(folder, "missing.2fa"), "sample.2fa", false);
        throw new Exception("Missing file fell back to a different vault");
    }
    catch (FileNotFoundException) { passed++; }
}
finally { Directory.Delete(folder, true); }
Console.WriteLine($"New data file validation: {passed} checks passed.");

namespace Project2FA.ViewModels
{
    public partial class NewDataFilePageViewModel
    {
        public string DateFileName { get; set; }
        public string Password { get; set; }
        public string PasswordRepeat { get; set; }
        public string FolderPath { get; set; }
        public bool SelectWebDAV { get; set; }
        public bool DatafileBTNActive { get; set; }
        public StorageFolder LocalStorageFolder { get; set; }
        public object LocalStorageFile { get; set; }
        private void SetProperty(ref string field, string value) => field = value;
        public void Validate() => ValidateDesktopInputs();
    }
}
namespace Windows.Storage
{
    public class ApplicationData
    {
        public static ApplicationData Current { get; } = new();
        public StorageFolder LocalFolder { get; set; }
    }
    public class StorageFile
    {
        public string Path { get; set; }
        public static Task<StorageFile> GetFileFromPathAsync(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException();
            return Task.FromResult(new StorageFile { Path = path });
        }
    }
    public class StorageFolder
    {
        public string Path { get; set; }
        public static Task<StorageFolder> GetFolderFromPathAsync(string path) => Task.FromResult(new StorageFolder { Path = path });
    }
}
