# Authenticated Windows and macOS desktop vaults — 1.3.6

New vaults use format V4. The complete account/category document is encrypted with
.NET AES-256-GCM, including metadata and empty collections. Each write generates
a fresh 32-byte salt and 12-byte nonce. Password derivation uses
PBKDF2-HMAC-SHA256 with 600,000 iterations. The authentication tag is 16 bytes.
The fixed format/algorithm/KDF parameters are authenticated as associated data;
unsupported parameters and excessive envelope sizes are rejected before key
computation. Password bytes are used exactly, including Unicode and whitespace.
Temporary key/plaintext byte buffers are cleared after use. Decrypted account
objects necessarily remain in memory while the vault is unlocked.

The JSON envelope contains `Version`, `Algorithm`, `Kdf`, `Iterations`, `Salt`,
`Nonce`, `Tag`, and `Data`. Binary fields use Base64. The encrypted document uses
the existing model's JSON representation without the legacy field converters.
Its inner `Version` is historical model metadata; the outer V4 envelope controls
cryptography. Readers reject future versions rather than guessing a legacy KDF.

Modern vault settings store a random credential identifier, not a password hash.
Login authenticates the actual vault. A selected modern vault cannot be silently
downgraded to a legacy file. Opening a different legacy file explicitly is still
supported. Touch ID items use the existing device-only biometric Keychain ACL.

## Existing files and compatibility

V0/V1/V2/V3 files remain readable. Ordinary saves of legacy files preserve V2
compatibility. Use **Settings → Data file → Upgrade vault encryption** to migrate
with the same password, or **Change password** to migrate with a new password.
Both actions explain that the updated Windows and macOS Uno desktop builds can
read V4. Older releases (including the old Windows UWP app) and mobile clients
remain incompatible. See [desktop compatibility](DESKTOP-COMPATIBILITY.md).
Do not upgrade a shared vault until every client that must read it supports V4.

Migration verifies the staged document and retains an encrypted
`.legacy-<id>.2fa` copy beside the original. It preserves account types, OCRA and
MobileID settings, categories, seeds and metadata. File/credential commits use an
encrypted recovery copy and rollback on failure. Touch ID/Windows Hello must be enrolled again.
Legacy files, old backups, and legacy password verifiers remain weak until they
are replaced; creating a new encrypted file cannot strengthen existing copies.
After verifying migration and any required compatibility, manage old copies using
your normal backup retention process. The app does not delete them automatically.

## WebDAV

WebDAV password changes and upgrades operate online over HTTPS. The server must
provide strong ETags and honor conditional HTTP requests. The app checks the
remote account document against the local document, uploads with `If-Match`,
reads back and verifies the upload, then commits local credentials. A lost PUT
response is reconciled by reading the remote content. A concurrent update is
never deliberately overwritten during rollback; if recovery is uncertain, the
original encrypted recovery file is kept and its path is reported.

Ordinary WebDAV edits also require connectivity and strong ETags. They fail
without dropping the local vault when offline or in conflict. OTP generation
from the already loaded local vault remains available offline. Reload downloads
authenticated remote content and retains a `.before-sync-<id>.2fa` local copy.
Filesystem timestamps no longer authorize overwriting the remote vault. Creating
a missing remote vault uses `If-None-Match: *`. HTTP redirects are rejected;
configure the canonical HTTPS server address.

Rename/move of a WebDAV local copy asks whether to switch to local use. The remote
copy remains at its existing URL; syncing stops for the relocated local file.
This is not a remote folder rename. Generic WebDAV cannot guarantee an atomic
transaction spanning its server and the local filesystem: recovery copies and
conditional compensation cover failures without claiming distributed atomicity.
Tests use a synthetic HTTP handler; no live user's WebDAV server was changed.

## Verification and references

The compiled-app tests cover V4 round trips, empty vault password authentication,
metadata and seed preservation, tampered headers/ciphertext, fresh salt/nonce,
wrong passwords, whitespace preservation, downgrade rejection, and V0–V3 migration.
File/HTTP fault-injection tests cover commit, rollback, conflicts, lost responses,
ETag requirements, create collisions, and retained recovery copies.

- [.NET AES-GCM](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm.encrypt)
- [OWASP password derivation guidance](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html)
- [OWASP cryptographic storage guidance](https://cheatsheetseries.owasp.org/cheatsheets/Cryptographic_Storage_Cheat_Sheet.html)

## User-facing recovery procedure

1. Preserve the original vault and any recovery path reported by the app.
2. Use Settings → Data file → Open another data file to inspect a backup copy.
3. Supply the password in effect when that copy was written, not necessarily the
   current vault password. Confirm the expected accounts before replacing files.
4. For WebDAV, stop making concurrent changes and resolve remote/local versions
   before resuming writes. Moving to local use does not delete the server copy.
5. Re-enable biometric unlock for the selected file after recovery if needed.

No password reset service or decryption bypass is provided. Backup filenames do
not prove which copy has the newest account data. User instructions are in the
[user guide](USER-GUIDE.md); this document describes format/transaction behavior.
