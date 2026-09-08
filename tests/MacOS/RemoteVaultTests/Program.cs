using System.Net;
using System.Net.Http.Headers;
using Project2FA.Services.MacOS;
using WebDAVClient;

static async Task Reject(Func<Task> action)
{
    try { await action(); }
    catch (IOException) { return; }
    throw new Exception("Unsafe remote operation succeeded.");
}
var server = new Server(); using var http = new HttpClient(server);
var remote = new ConditionalVaultClient(http, new Uri("https://synthetic.invalid/vault.2fa"));
await Reject(async () => await remote.CreateAsync("must not replace"));
server.Missing = true;
await remote.CreateAsync("original ciphertext");
var original = await remote.ReadAsync();
var change = new MacOSRemoteVaultTransaction(remote, original, "new ciphertext");
await change.Apply(); if (server.Content != "new ciphertext") throw new Exception("Upload missing");
await change.Restore(); if (server.Content != "original ciphertext") throw new Exception("Rollback missing");
original = await remote.ReadAsync(); server.Change("other device");
await Reject(() => remote.ReplaceAsync(original, "overwrite"));
if (server.Content != "other device") throw new Exception("Conflict overwritten");
server.Change("original ciphertext"); original = await remote.ReadAsync();
server.LoseResponse = true; change = new(remote, original, "new ciphertext");
await change.Apply(); if (server.Content != "new ciphertext") throw new Exception("Lost response not recovered");
server.Change("third device"); await Reject(change.Restore);
if (server.Content != "third device") throw new Exception("Third-device update lost");
server.WeakETag = true; await Reject(async () => await remote.ReadAsync()); server.WeakETag = false;
server.NoETag = true; await Reject(async () => await remote.ReadAsync()); server.NoETag = false;
server.Change("original ciphertext"); original = await remote.ReadAsync();
change = new(remote, original, "new ciphertext");
string directory = Path.Combine(Path.GetTempPath(), "2fast-remote-tests-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
try
{
    string path = Path.Combine(directory, "test.2fa"); await File.WriteAllTextAsync(path, "original ciphertext");
    async Task Writer(string p, string text)
    {
        if (text == "new ciphertext") await change.Apply(); else await change.Restore();
        await File.WriteAllTextAsync(p, text);
    }
    bool rolledBack = false;
    await Reject(() => MacOSFileTransaction.Replace(path, "new ciphertext", () => throw new IOException("credential store failure"), () => rolledBack = true, Writer));
    if (!rolledBack || server.Content != "original ciphertext" || await File.ReadAllTextAsync(path) != "original ciphertext") throw new Exception("Distributed rollback failed");
    original = await remote.ReadAsync(); change = new(remote, original, "new ciphertext");
    await Reject(() => MacOSFileTransaction.Replace(path, "new ciphertext", () => { server.Change("concurrent ciphertext"); throw new IOException("commit failure"); }, () => { }, Writer));
    if (server.Content != "concurrent ciphertext" || !Directory.GetFiles(directory, "*.recovery-*").Any(p => File.ReadAllText(p) == "original ciphertext")) throw new Exception("Conflict recovery copy not retained");
}
finally { Directory.Delete(directory, true); }
Console.WriteLine("WebDAV transactions: conditional upload, read-back, stale ETag rejection, lost-response recovery, strong ETag enforcement, credential rollback and concurrent-write recovery passed.");

sealed class Server : HttpMessageHandler
{
    public string Content = "original ciphertext"; int generation = 1;
    public bool LoseResponse, WeakETag, NoETag, Missing;
    public void Change(string text) { Content = text; generation++; }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        string tag = "\"" + generation + "\"";
        if (request.Method == HttpMethod.Put)
        {
            if (request.Headers.IfNoneMatch.Any())
            {
                if (!Missing || request.Headers.IfNoneMatch.Single().Tag != "*") return new(HttpStatusCode.PreconditionFailed);
            }
            else if (Missing || request.Headers.IfMatch.SingleOrDefault()?.ToString() != tag) return new(HttpStatusCode.PreconditionFailed);
            Missing = false;
            Change(await request.Content!.ReadAsStringAsync(token));
            if (LoseResponse) { LoseResponse = false; throw new HttpRequestException("Synthetic lost response"); }
            return new(HttpStatusCode.NoContent);
        }
        if (Missing) return new(HttpStatusCode.NotFound);
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Content) };
        if (!NoETag) response.Headers.ETag = new EntityTagHeaderValue(tag, WeakETag);
        return response;
    }
}
namespace Project2FA.Services.MacOS
{
    internal static class MacOSVaultLocation
    {
        internal static Task WriteAtomicAsync(string folder, string name, string text) => File.WriteAllTextAsync(Path.Combine(folder, name), text);
    }
}
