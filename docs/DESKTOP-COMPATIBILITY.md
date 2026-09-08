# Windows and macOS vault compatibility

The updated **Uno desktop builds for Windows and macOS share one V4 vault codec**.
They can open, edit, save, back up, change the password of, and upgrade the same
`.2fa` vault format. New vaults use authenticated encryption. Existing V0–V3 files
remain readable. Select **Settings → Data file → Upgrade vault encryption** to
upgrade with the current password, or **Change password** to upgrade and change it.

Update the applications on both computers before upgrading the shared vault.
This Windows update is `Project2FA.Uno.exe`, not the old Windows Store/UWP build.
Older installed Windows releases and mobile clients have not acquired V4 support
and still cannot open V4 files. The updated UI now distinguishes those older
releases from the compatible Windows/macOS desktop builds.

An encrypted legacy backup is kept beside a vault when it is upgraded. Old copies
retain their original encryption. Touch ID/Windows Hello must be enrolled again
after a password, vault identity, or path change. Device-local biometric keys do
not transfer between computers. A MobileID token marked device-bound remains
restricted to its enrolled device; matching file formats do not remove that rule.

Both builds share TOTP, the supported OCRA suite
`OCRA-1:HOTP-SHA1-6:QN08-T1M`, supported offline Deepnet MobileID parsing,
account review, serialized saves, edit/favorite rollback, QR image rendering,
countdown logic, and data-file management. Both use conditional HTTPS WebDAV
transactions; the server must support strong ETags. WebDAV edits need connectivity.

Windows continuously scans a chosen camera, window or display and shows a live
preview. It decodes subsequent frames when the first frame has no QR code, and
passes the exact decoded URI into the shared parser. Camera device enumeration,
source switching, cancellation, duplicate scanner protection, and no-camera
screen fallback are included. Some GPU/protected windows refuse window capture;
select their display and keep the QR visible in that case. On macOS, source
selection uses Apple's sharing picker; Windows uses a list of windows/displays.

Windows Hello wraps a random AES key with a nonexportable Windows Passport RSA key
requiring a fresh gesture for unwrapping. The credential envelope is additionally
protected with per-user DPAPI. An ordinary yes/no Windows Hello prompt followed by
an unprotected saved password is not used. Windows Hello can offer PIN, face or
fingerprint; this differs from macOS's Touch ID biometric policy.

## Verification limits

Windows x64 and ARM64 packages were cross-built on a Mac. Their managed vault/UI
contracts can be tested using the Mac test host; that is not Windows OS execution.
Synthetic V4 fixture exchange checks verify that the compiled Windows build writes
files the compiled Mac build reads and vice versa, preserving account data.
QR frame tests cover repeated/inverted TOTP, OCRA and MobileID text, blank-frame
progression, and URI preservation. Native Windows credential tests are provided
in `scripts/test-windows.ps1` (`-Hello` requires a person and enrolled Windows Hello).
Actual Windows camera/window capture, Hello and full UI acceptance still require
execution on a Windows PC. No user's vault was modified by these checks.
