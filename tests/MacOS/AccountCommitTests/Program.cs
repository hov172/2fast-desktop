using Project2FA.Services;
var queue = new AccountCommitQueue<string>();
var accounts = new List<string>();
var disk = new List<string>();
var release = new TaskCompletionSource<bool>();
int saves = 0;
async Task<bool> Save()
{
    saves++;
    await release.Task;
    disk = accounts.ToList();
    return true;
}
var first = queue.Commit("one", accounts.Contains, accounts.Add, x => accounts.Remove(x), Save);
var second = queue.Commit("two", accounts.Contains, accounts.Add, x => accounts.Remove(x), Save);
if (first.IsCompleted || second.IsCompleted || saves != 1 || accounts.Count != 1) throw new Exception("Adds bypassed pending persistence.");
release.SetResult(true);
if (!await first || !await second || !disk.SequenceEqual(new[] { "one", "two" })) throw new Exception("Repeated adds were lost.");
if (await queue.Commit("two", accounts.Contains, accounts.Add, x => accounts.Remove(x), Save) || saves != 2) throw new Exception("Duplicate submission saved twice.");
if (await queue.Commit("failed", accounts.Contains, accounts.Add, x => accounts.Remove(x), () => Task.FromResult(false)) || accounts.Contains("failed")) throw new Exception("Failed save left unsaved account.");
try { await queue.Commit("exception", accounts.Contains, accounts.Add, x => accounts.Remove(x), () => throw new IOException("synthetic write failure")); }
catch (IOException) { }
if (accounts.Contains("exception")) throw new Exception("Exception did not roll back.");
if (!await queue.Commit("retry", accounts.Contains, accounts.Add, x => accounts.Remove(x), Save)) throw new Exception("Failure blocked later imports.");
Console.WriteLine("Account commit: delayed saves, repeated imports, duplicate clicks, rollback, and retry passed.");
