using WebDAVClient;
using UNOversal.Services.Logging;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Loader;

var origin = WebDavTransport.RequireHttps("https://example.test/nextcloud");
foreach (var bad in new[] { "http://example.test", "file:///tmp/vault", "https://user:password@example.test", "https://example.test/#secret", "invalid" })
{
    try { WebDavTransport.RequireHttps(bad); throw new Exception("Unsafe transport accepted."); }
    catch (ArgumentException) { }
}
foreach (var bad in new[] { "http://example.test/poll", "https://elsewhere.test/poll", "https://example.test:444/poll" })
{
    try { WebDavTransport.RequireSameOrigin(bad, origin); throw new Exception("Unsafe login endpoint accepted."); }
    catch (ArgumentException) { }
}
WebDavTransport.RequireSameOrigin("https://example.test:443/poll", origin);
var error = new InvalidOperationException("synthetic-password", new IOException("/Users/private/vault.2fa"));
error.Data["token"] = "synthetic-token"; error.Source = "synthetic-source";
var record = SafeLogRecord.Format(error);
foreach (var value in new[] { "synthetic-password", "synthetic-token", "synthetic-source", "private", "vault.2fa" })
    if (record.Contains(value)) throw new Exception("Sensitive data entered diagnostics.");
if (!record.Contains("IOException") || !record.Contains("HResult=")) throw new Exception("Diagnostic identity lost.");
// The login HTTP client must return redirects rather than replaying token POSTs.
var reserve = new TcpListener(IPAddress.Loopback, 0); reserve.Start();
int port = ((IPEndPoint)reserve.LocalEndpoint).Port; reserve.Stop();
using var listener = new HttpListener(); listener.Prefixes.Add($"http://127.0.0.1:{port}/"); listener.Start();
var serve = Task.Run(async () => { var request = await listener.GetContextAsync(); request.Response.StatusCode = 307; request.Response.RedirectLocation = $"http://127.0.0.1:{port}/unsafe"; request.Response.Close(); });
using var client = WebDavTransport.CreateLoginClient();
using var response = await client.PostAsync($"http://127.0.0.1:{port}/", new StringContent("synthetic-token")).WaitAsync(TimeSpan.FromSeconds(5));
await serve;
if (response.StatusCode != HttpStatusCode.TemporaryRedirect) throw new Exception("Login redirect followed.");
Console.WriteLine("PASS: HTTPS/origin validation, token redirect blocking, and exception redaction.");
if (args.Length > 0)
{
    // Exercise the production assembly, including both credential entry points.
    var folder = Path.GetFullPath(args[0]);
    AssemblyLoadContext.Default.Resolving += (_, name) =>
    {
        var path = Path.Combine(folder, name.Name + ".dll");
        return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
    };
    var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(folder, "WebDAVClientPortable.dll"));
    var type = assembly.GetType("WebDAVClient.Client")!;
    var ctor = type.GetConstructor(new[] { typeof(string), typeof(NetworkCredential), typeof(bool) })!;
    var status = type.GetMethod("GetServerStatus")!;
    var watch = new TcpListener(IPAddress.Loopback, 0); watch.Start();
    try
    {
        var address = $"http://127.0.0.1:{((IPEndPoint)watch.LocalEndpoint).Port}";
        var credentials = new NetworkCredential("synthetic-user", "synthetic-password");
        try { ctor.Invoke(new object[] { address, credentials, false }); throw new Exception("Compiled constructor accepted HTTP."); }
        catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException) { }
        try
        {
            await ((Task)status.Invoke(null, new object[] { address, false, credentials })!).WaitAsync(TimeSpan.FromSeconds(5));
            throw new Exception("Compiled server check accepted HTTP.");
        }
        catch (ArgumentException) { }
        if (watch.Pending()) throw new Exception("HTTP rejection occurred after opening a connection.");
        Console.WriteLine("PASS: production WebDAV constructor and status check reject HTTP before connecting.");
    }
    finally { watch.Stop(); }
}
