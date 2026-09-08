using Project2FA.Services.MacOS;
internal static class FileTransactionChecks
{
    internal static async Task Run()
    {
        string folder = Path.Combine(Path.GetTempPath(), "2fast-file-transaction-" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        try
        {
            string path = Path.Combine(folder, "vault.2fa");
            await File.WriteAllTextAsync(path, "old ciphertext");
            string credential = "old";
            await MacOSFileTransaction.Replace(path, "new ciphertext", () => credential = "new", () => credential = "old");
            if (File.ReadAllText(path) != "new ciphertext" || credential != "new") throw new Exception("Commit failed");
            try { await MacOSFileTransaction.Replace(path, "bad candidate", () => { credential = "bad"; throw new IOException("synthetic settings failure"); }, () => credential = "new"); }
            catch (IOException) { }
            if (File.ReadAllText(path) != "new ciphertext" || credential != "new") throw new Exception("Settings failure did not roll back file/credential");
            int writes = 0;
            try
            {
                await MacOSFileTransaction.Replace(path, "candidate", () => credential = "bad", () => credential = "new", async (target, text) =>
                {
                    if (++writes == 1) throw new IOException("synthetic disk failure");
                    await File.WriteAllTextAsync(target, text);
                });
            }
            catch (IOException) { }
            if (credential != "new" || File.ReadAllText(path) != "new ciphertext") throw new Exception("Write failure changed credentials");
            try { await MacOSFileTransaction.Replace(path, "candidate", () => { }, () => { }, (_, _) => throw new IOException("unavailable disk")); }
            catch (IOException) { }
            var recovery = Directory.GetFiles(folder, "*.recovery-*");
            if (recovery.Length != 1 || File.ReadAllText(recovery[0]) != "new ciphertext") throw new Exception("Recovery copy was lost");
            string backup = Path.Combine(folder, "backup.2fa");
            await MacOSFileTransaction.CopyNew(path, backup);
            await File.WriteAllTextAsync(path, "later ciphertext");
            try { await MacOSFileTransaction.CopyNew(path, backup); throw new Exception("Overwrite allowed"); } catch (IOException) { }
            if (File.ReadAllText(backup) != "new ciphertext" || Directory.GetFiles(folder, ".2fast-copy-*").Length != 0) throw new Exception("Backup overwritten or temporary file leaked");
            if (MacOSFileTransaction.FileName(" name ") != "name.2fa") throw new Exception("Filename normalization failed");
            foreach (var invalid in new[] { "../outside", "", "a/b", "a\\b" })
                try { MacOSFileTransaction.FileName(invalid); throw new Exception("Invalid filename accepted"); } catch (ArgumentException) { }
            Console.WriteLine("File transactions: commit, credential rollback, write failure, recovery copy, backup collision, and filenames passed.");
        }
        finally { Directory.Delete(folder, true); }
    }
}
