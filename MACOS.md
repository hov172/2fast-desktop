# 2fast for macOS

The default build is now one universal app: `dist/2fast.app`, distributed as
`dist/2fast-macos-universal.zip`. It supports Apple Silicon and Intel Macs running
macOS 15 or newer. No architecture selection or .NET installation is required.

The universal Mach-O launcher selects an independently signed, self-contained Uno
app from `Contents/Helpers/arm64` or `Contents/Helpers/x64`. Each runtime retains
its own managed assemblies, native libraries, resources, provisioning profile,
and original bundle identifier. Architecture-specific ZIPs remain available.

## Using the features

**Touch ID:** Enroll a fingerprint in macOS System Settings → Touch ID & Password.
Unlock your data file with its password, open 2fast Settings, and enable **Use
Touch ID**. Confirm the data-file password and the native Touch ID prompt. The
login screen then offers **Unlock with Touch ID**. **Use Touch ID on startup**
controls automatic prompting; an explicit logout does not automatically unlock.
Cancellation and lockout leave password login available. If fingerprints change,
sign in with the password and enroll the vault again.

**Camera:** In the account screen's **+** menu, choose the camera option. Allow
camera access when macOS asks, select a camera if necessary, and point it at an
authenticator setup QR. Review the imported account before saving. Closing the
scanner or switching to another app stops camera capture.

**Webpage/screen:** Choose **+ → Scan QR from screen…**, or **Scan from screen…**
inside the camera scanner. Use the macOS picker to select a browser window or a
display. Keep the setup QR visible; zoom the webpage if needed. A preview shows
the shared content. Capture ends on a decoded QR, Cancel, window close,
logout/navigation, or macOS Stop Sharing. Unlike a camera scan, screen scanning
continues while you interact with the selected browser window.

Both inputs decode QR codes locally using Apple's Vision framework. Camera uses
AVFoundation; screen sharing uses ScreenCaptureKit's system picker. Neither
records frames, captures audio, uploads images, nor writes screenshots to disk.
The scanner accepts `otpauth://totp` setup codes with SHA1/SHA256/SHA512, 6 or 8
digits, and valid positive periods; explicit `otpauth://ocra` QR codes carrying
suite `OCRA-1:HOTP-SHA1-6:QN08-T1M`; and supported encrypted Deepnet
`mobileid://.../mobileid/install` envelopes. It prompts for a Deepnet authorization
code if one is not embedded. Review the account before saving.

Imported MobileID tokens display the vendor's normal changing OTP. Use the token's
**… → OCRA challenge…** action for a login challenge. Device-bound imports are
bound to this Mac's app-specific Keychain identifier. Offline tokens can be imported
from QR codes that also contain push metadata, but **push approvals are not enrolled**.
HOTP/migration bundles, legacy MobileID profiles, and mutual authentication remain
unsupported. Plain raw-seed OCRA setup is available through **+ → Add OCRA token…**.

## Security and credential migration

Touch ID secrets use the macOS Data Protection Keychain with a
`biometryCurrentSet` access-control constraint. Keychain enforces biometric
access when retrieving the password; the app does not retrieve a normally
accessible password after a separate yes/no biometric check. Credentials are
bound to the selected vault identity, stay on this device, and are
revoked when disabling Touch ID, changing the password, or switching files.

The macOS build replaces the upstream desktop `SecretHelper`. Vault passwords
are cached only for the current in-memory session; optional persistent copies
require Touch ID. WebDAV credentials use the Data Protection Keychain. On first
use, the old machine/user-derived encrypted `Project2FA/secrets.dat` store is
migrated: WebDAV credentials go to Keychain, old vault passwords are discarded,
and the legacy file and its temporary copy are removed only after migration
succeeds. Migration failure preserves the old file and does not silently fall
back to insecure storage. Existing users must log in with their vault password
and explicitly enable Touch ID.

## Building

Requirements: macOS 15+, Xcode Command Line Tools, .NET SDK 10.0.300 (or a newer
patch in that feature band), Uno.Sdk 6.6.42, XcodeGen, and the four submodules
listed in `.gitmodules`. In a Git checkout, use:

```sh
git submodule update --init --recursive
```

The protected Keychain requires an Apple-authorized signing entitlement and
provisioning profile. An ad-hoc signature cannot provide this access. Sign into
Xcode with your Apple Developer account, then perform the one-time setup:

```sh
./scripts/setup-macos-signing.sh YOUR_APPLE_TEAM_ID
```

This uses Xcode automatic provisioning for `jpweber.it.Project2FA.Uno`, then
prepares local signing inputs under `build/macos-signing/`. Provisioning profiles
and generated signing inputs are excluded from version control; private keys
remain in macOS Keychain. Repeat setup when the profile or certificate expires.
The helper project is for signing only; the shipped app is built by Uno.

```sh
./scripts/build-macos.sh
./scripts/test-macos.sh
```

The default universal build output is `dist/2fast.app`. Architecture-specific
intermediate apps remain under `src/Project2FA.Uno/bin/Release/net10.0-desktop/`.

The default `./scripts/build-macos.sh` builds both architectures, refreshes the
universal app and ZIP, and updates checksums. Explicit architecture builds only
refresh their intermediate publish output. An Intel
build can be produced with `./scripts/build-macos.sh osx-x64`. The Intel build
and signature verification passed; all 17 bundled executable/native binaries
contain x86_64. Under Rosetta, 12 encrypted-model assertions and the actual startup
account-review route regression passed. Physical Intel hardware, camera/screen
capture, and Touch ID have not been tested for this architecture. `build/MacOS.targets` excludes unrelated mobile/Windows workloads
while preserving normal project build targets. Native Objective-C code is
compiled with warnings treated as errors and bundled as `libTwoFastMac.dylib`.

## Verification

Current audit verification on Apple Silicon:

- 110 automated parser, OCRA, device-binding, form/write, secret-store, and native checks.
- 12 assertions against the compiled app's encrypted model serializer and cloning.
- Supplied QR screenshot decoded and validated locally without recording its secrets.
- Release build and signature verification; no known vulnerable packages in the restored desktop advisory query.
- Native regression checks verify a single static screen frame survives the camera throttle boundary and reaches both preview and decoding.

Earlier builds had live camera and system-picker tests. The current revision still
needs fresh interactive capture and live Deepnet server verification. See the
[audit report](docs/audits/2026-09-07-macos-qr-workflows.md) for fixed defects and
current verification coverage and compatibility limits.

Run the additional compiled-model check after building:

```sh
dotnet run --project tests/MacOS/VaultModelTests/VaultModelTests.csproj -c Release -- src/Project2FA.Uno/bin/Release/net10.0-desktop/osx-arm64
```

This Mac reported `LAErrorBiometryNotEnrolled` during validation. A successful
physical Touch ID enrollment/unlock still needs a fingerprint to be enrolled.
To run the interactive checks afterward:

```sh
./scripts/test-macos.sh --biometric
open -n tests/MacOS/bin/NativeTests.app --args --camera
open -n tests/MacOS/bin/NativeTests.app --args --screen
```

The interactive biometric test uses only a disposable synthetic credential. The
screen test provides a synthetic QR window to select. The test app has a
separate bundle identifier and Keychain access group; its profile must authorize
that identifier (the automatically generated wildcard development profile does).

The delivered app is signed for local development, not notarized or an official
2fast release. Public distribution requires an appropriate distribution profile,
Developer ID signing, and notarization. Existing upstream Uno API/nullability
warnings remain. Full vault/import/WebDAV workflows and older macOS versions
have not been exhaustively tested.

## Sources and licensing

- [Uno macOS packaging](https://platform.uno/docs/articles/uno-publishing-desktop-macos.html)
- [Apple Keychain access groups](https://developer.apple.com/documentation/security/sharing-access-to-keychain-items-among-a-collection-of-apps)
- [Apple biometric Keychain access control](https://developer.apple.com/documentation/security/secaccesscontrolcreateflags/biometrycurrentset)
- [Apple screen-sharing picker](https://developer.apple.com/documentation/screencapturekit/sccontentsharingpicker)

The source archive's missing submodules were restored at the 2fast-pinned
revisions: BiometryService `c8e9aa11807e6df54a31109ead1a625fca25ab41`, Otp.NET
`06f39f3781dc8f4b8c59d1c30a86a9c767af0ccf`, UNOversalTemplate
`d09e40ab67f60dbf2cb1f0829d9c47f7c96424bd`, and ZXing.Net.Uno
`cb9baedd1034a22248aca75ec74be975acf2cbfb`.

The application retains its upstream GPL-3.0 license. See `LICENSE` and the
individual dependency licenses.

Setup uses Documents by default, allows changing the folder, explains disabled creation, and refuses to overwrite an existing vault. Missing-file recovery avoids Windows-only capability APIs. Password entry has no app-imposed length limit and includes reveal controls. Trailing whitespace is reported without changing the entered password; automatic whitespace insertion has not been reproduced.

Manual OCRA setup accepts an administrator-provided raw seed in Base32, Base64, or Hex for `OCRA-1:HOTP-SHA1-6:QN08-T1M`. Responses accept 1–8 numeric challenge digits and clear at minute boundaries or when the dialog closes. QR import also supports the profiles described above. OCRA/MobileID QR export is blocked to avoid exporting a misleading TOTP token. Older 2fast builds do not support these account types.

Runtime platform reference: [.NET 10 supported operating systems](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md) includes macOS x64. Our native bridge raises the app minimum to macOS 15.

Workflow follow-up: sidebar navigation and query decoding, account menu targets,
edit dialog, Settings Back, data-file/About content, and timer refresh were repaired.
Run the compiled-app workflow checks from the repository root:

```sh
dotnet run --project tests/MacOS/VaultModelTests/VaultModelTests.csproj -c Release -- src/Project2FA.Uno/bin/Release/net10.0-desktop/osx-arm64 .
```

These check compiled routes and UI contracts; live click-through verification is
still required. See the audit report for exact coverage and open issues.

Universal packaging verification: the launcher contains arm64 and x86_64 slices.
Both slices select their matching runtime; executable fixtures verify argument
forwarding and child exit status, including an app path with spaces. The final
bundle and its embedded apps pass deep, strict signature verification.

```sh
/usr/bin/python3 scripts/test-macos-universal.py
```

Intel launcher checks on this development machine run under Rosetta; physical
Intel hardware testing remains outstanding.

Account workflow update: new accounts finish saving before returning to Accounts.
The QR dialog renders a PNG through a standard Image control on macOS. Labeled
scan/add/settings controls and per-account Copy/Edit/View QR buttons are visible
without opening a menu. The account commit regression is included in
`scripts/test-macos.sh`; compiled workflow checks also generate a synthetic QR PNG.


Data-file management: open Settings → Data file to rename or move the active local
vault, save an encrypted backup, change its password, open another vault, or create
a new one. Existing destination files are protected against overwrite. Password
changes retain an encrypted recovery copy until the new file and credentials are
committed. Re-enable Touch ID after changing a password or moving the vault.
WebDAV password changes now use conditional, verified HTTPS uploads with rollback.
The server must provide strong ETags; WebDAV edits require connectivity. Moving a
WebDAV local copy switches to local use and leaves the server copy in place.
New vaults use authenticated V4 encryption. Existing vaults can be upgraded from
Settings → Data file → Upgrade vault encryption. This keeps a legacy backup and
works with the updated Windows and macOS Uno desktop builds. Older releases
and mobile clients still cannot read V4. See [desktop compatibility](docs/DESKTOP-COMPATIBILITY.md).
See [vault format and recovery](docs/MACOS-VAULT-FORMAT.md).


Hardware acceptance: this Apple Silicon Mac reports Touch ID not enrolled
(`LAErrorBiometryNotEnrolled`, -7). Tests confirm protected enrollment fails and
stores no credential; password-based encryption/unlock tests pass. Successful
fingerprint enrollment/unlock still needs a fingerprint enrolled by the owner.
After enrollment, run `./scripts/test-macos.sh --biometric` for an isolated
synthetic Keychain test. Intel compiled tests run under Rosetta; physical Intel
acceptance still requires an Intel Mac running macOS 15 or later.
