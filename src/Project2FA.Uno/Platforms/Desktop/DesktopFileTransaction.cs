#if TWOFAST_DESKTOP
namespace Project2FA.Services.Desktop;
internal static class DesktopFileTransaction
{
    internal static string FileName(string name)
    {
        name = name?.Trim() ?? "";
        if (name.Length == 0 || name is "." or ".." || Path.GetFileName(name) != name || name.Contains(':') || name.Contains('\\'))
            throw new ArgumentException("Enter a filename without a folder path.");
        return name.EndsWith(".2fa", StringComparison.OrdinalIgnoreCase) ? name : name + ".2fa";
    }
    internal static async Task CopyNew(string source, string destination)
    {
        string temporary = Path.Combine(Path.GetDirectoryName(destination)!, ".2fast-copy-" + Guid.NewGuid().ToString("N"));
        try
        {
            await using (var input = File.OpenRead(source))
            await using (var output = new FileStream(temporary, new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Options = FileOptions.Asynchronous, UnixCreateMode = OperatingSystem.IsWindows() ? null : UnixFileMode.UserRead | UnixFileMode.UserWrite }))
            {
                await input.CopyToAsync(output);
                output.Flush(true);
            }
            File.Move(temporary, destination, false);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    internal static async Task Replace(string path, string content, Action commit, Action rollback, Func<string, string, Task> write = null)
    {
        write ??= (target, text) => DesktopVaultLocation.WriteAtomicAsync(Path.GetDirectoryName(target)!, Path.GetFileName(target), text);
        string backup = path + ".recovery-" + Guid.NewGuid().ToString("N");
        await CopyNew(path, backup);
        bool recoveryComplete = false;
        try
        {
            await write(path, content);
            commit();
            recoveryComplete = true;
        }
        catch
        {
            try
            {
                await write(path, await File.ReadAllTextAsync(backup));
                rollback();
                recoveryComplete = true;
            }
            catch (Exception recovery)
            {
                throw new IOException("The change could not be completed. The original encrypted vault is preserved at " + backup, recovery);
            }
            throw;
        }
        finally
        {
            if (recoveryComplete)
            {
                // Cleanup failure must not report a committed password change as failed.
                try { File.Delete(backup); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }
}
#endif
