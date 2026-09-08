# 2fast 1.4.1

Windows reliability update for the Uno desktop apps: vault saving, QR
scanner preview, and category management. Display/assembly version 1.4.1,
package manifest 1.4.1.0, build 141. App identifiers remain unchanged.
macOS Universal, Windows x64, and Windows ARM64 build from the same tag;
the fixed code paths are shared, and macOS behavior is unchanged.

## Changes

- Fixed vault saves failing on Windows with "The changes could not be
  saved": the atomic file writer set a Unix-only file mode whose setter
  throws on Windows even when assigned null. The mode is now applied only
  on Unix-like systems.
- Fixed the new-vault retry loop: a failed first attempt left an empty
  `.2fa` placeholder that made every retry fail with a file-exists error.
  Empty leftovers are reused or cleaned up; non-empty vaults are never
  overwritten.
- Fixed the QR scanner preview showing nothing on Windows: replaced the
  unimplemented `DataWriter.DetachStream` WinRT call with a direct stream
  write. Camera, window and screen previews now display, matching macOS.
- Implemented the Manage Categories dialog on the Uno desktop apps. It was
  an empty, unregistered stub that opened blank. Create (name + icon),
  rename, change icon, delete, and save now work.
- Added a "Manage categories" entry point on the Add/Edit account pages.
  No desktop UI previously opened the categories manager, so the category
  token list could never be populated. The token list refreshes after the
  dialog closes, preserving selections.
- Build tooling: `scripts/build-windows.ps1 -SingleFile` produces a fully
  self-contained single `2fast-windows-<arch>.exe` with all content assets
  embedded (larger file; the standard folder ZIP remains the default).

Quit 2fast before replacing the app. Extract Windows builds to a fresh folder.
Back up the vault before updating. V4 remains compatible between updated Mac
and Windows clients; older Windows/mobile clients cannot read upgraded files.
Encryption upgrade remains optional. See [the user guide](USER-GUIDE.md) for
all workflows.

## Distribution limits

The Mac app uses Apple Development signing, not Developer ID signing or
notarization. Gatekeeper may reject it on another Mac. Windows packages are
unsigned. The 1.4.1 Windows x64 fixes were exercised manually on Windows
(vault create/save, screen-scan preview, category management); Windows ARM64,
physical Intel Mac acceptance, and Touch ID acceptance remain outstanding.
