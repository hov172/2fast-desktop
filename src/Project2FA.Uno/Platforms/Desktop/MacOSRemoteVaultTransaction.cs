#if TWOFAST_DESKTOP
using WebDAVClient;
namespace Project2FA.Services.MacOS;

internal sealed class MacOSRemoteVaultTransaction
{
    private readonly ConditionalVaultClient remote;
    private readonly VaultRevision original;
    private readonly string candidate;
    internal MacOSRemoteVaultTransaction(ConditionalVaultClient remote, VaultRevision original, string candidate)
    { this.remote = remote; this.original = original; this.candidate = candidate; }

    internal async Task Apply()
    {
        try { await remote.ReplaceAsync(original, candidate); }
        catch
        {
            // A lost response does not imply the server rejected the write.
            var actual = await remote.ReadAsync();
            if (actual.Content != candidate) throw;
        }
    }
    internal async Task Restore()
    {
        var actual = await remote.ReadAsync();
        if (actual.Content == original.Content) return;
        if (actual.Content != candidate)
            throw new IOException("Another device changed the remote vault; its changes were preserved. Keep the encrypted recovery copy and resolve the conflict before syncing.");
        await remote.ReplaceAsync(actual, original.Content);
    }
}
#endif
