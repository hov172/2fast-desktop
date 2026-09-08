using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace WebDAVClient;

public sealed class VaultRevision
{
    public string Content { get; }
    public string ETag { get; }
    public VaultRevision(string content, string etag) { Content = content; ETag = etag; }
}

// The caller owns HttpClient. No remote operation changes local credentials.
public sealed class ConditionalVaultClient
{
    private readonly HttpClient client;
    private readonly Uri uri;
    private const int MaxBytes = 64 * 1024 * 1024;
    public ConditionalVaultClient(HttpClient client, Uri uri) { this.client = client; this.uri = uri; }
    public async Task<VaultRevision> ReadAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        var tag = response.Headers.ETag;
        if (tag == null || tag.IsWeak || tag.Tag == "*")
            throw new IOException("This WebDAV server must provide strong ETags to safely change a vault password.");
        if (response.Content.Headers.ContentLength > MaxBytes) throw new IOException("The remote vault exceeds the supported size.");
        using var input = await response.Content.ReadAsStreamAsync();
        using var output = new MemoryStream();
        byte[] buffer = new byte[8192];
        int count;
        while ((count = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            if (output.Length + count > MaxBytes) throw new IOException("The remote vault exceeds the supported size.");
            await output.WriteAsync(buffer, 0, count);
        }
        return new VaultRevision(Encoding.UTF8.GetString(output.ToArray()), tag.ToString());
    }
    public async Task<VaultRevision> CreateAsync(string content)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, uri);
        request.Headers.IfNoneMatch.Add(EntityTagHeaderValue.Any);
        request.Content = new StringContent(content, Encoding.UTF8, "application/octet-stream");
        using var response = await client.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.PreconditionFailed)
            throw new IOException("A WebDAV vault already exists at this location. Open it instead of replacing it.");
        response.EnsureSuccessStatusCode();
        var actual = await ReadAsync();
        if (actual.Content != content) throw new IOException("The newly created WebDAV vault could not be verified.");
        return actual;
    }
    public async Task<VaultRevision> ReplaceAsync(VaultRevision expected, string content)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, uri);
        var etag = EntityTagHeaderValue.Parse(expected.ETag);
        if (etag.IsWeak || etag.Tag == "*") throw new IOException("A strong ETag is required.");
        request.Headers.IfMatch.Add(etag);
        request.Content = new StringContent(content, Encoding.UTF8, "application/octet-stream");
        using var response = await client.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.PreconditionFailed)
            throw new IOException("The WebDAV vault changed on another device. Reload it before trying again.");
        response.EnsureSuccessStatusCode();
        // Read back even when PUT returns an ETag: verify storage and detect intermediaries.
        var actual = await ReadAsync();
        if (actual.Content != content) throw new IOException("The WebDAV vault changed while verifying the upload.");
        return actual;
    }
}
