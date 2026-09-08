#if TWOFAST_DESKTOP
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Project2FA.Services.MacOS;

internal static class MacOSVaultLocation
{
    internal static async Task WriteAtomicAsync(string folder, string name, string content)
    {
        if (string.IsNullOrEmpty(name) || Path.GetFileName(name) != name || name is "." or "..")
            throw new System.ArgumentException("Invalid vault filename.");
        string destination = Path.Combine(folder, name);
        string temporary = Path.Combine(folder, ".2fast-" + System.Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            await using (var stream = new FileStream(temporary, new FileStreamOptions
            {
                Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None,
                Options = FileOptions.Asynchronous, UnixCreateMode = OperatingSystem.IsWindows() ? null : UnixFileMode.UserRead | UnixFileMode.UserWrite
            }))
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);
                await stream.WriteAsync(bytes);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporary, destination, overwrite: true);
        }
        finally
        {
            // Use the original temporary path: a moved StorageFile object can point at the live vault.
            try { File.Delete(temporary); } catch (IOException) { } catch (System.UnauthorizedAccessException) { }
        }
    }

    internal static async Task<StorageFile> OpenAsync(string savedPath, string name, bool webDAV)
    {
        // Current macOS settings store the complete filename. Accept legacy folder paths too.
        var path = webDAV ? Path.Combine(ApplicationData.Current.LocalFolder.Path, name)
            : Directory.Exists(savedPath) ? Path.Combine(savedPath, name) : savedPath;
        return await StorageFile.GetFileFromPathAsync(path);
    }
}
#endif
