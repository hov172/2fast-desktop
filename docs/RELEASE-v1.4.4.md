# 2fast 1.4.4 — Unlock and navigation fix

Version **1.4.4**, build **144**, for macOS 15+ (Apple Silicon and Intel) and
Windows x64/ARM64. This update fixes navigation before vault authentication and
an incorrect password-recovery dialog triggered by an empty unlock session.

## Changes

- Start with protected navigation disabled; enable it after credential verification.
- Block protected desktop page routes while locked, including direct navigation.
- Clear session credentials and displayed accounts on lock and cancel pending work.
- Prevent vault loading without an unlocked session and prevent canceled loads
  from repopulating account rows.
- Return to password entry on missing or invalid session credentials instead of
  the misleading “saved password invalid / change password” loop.
- Preserve the classic accounts layout and all existing account/vault features.

The incorrect dialog did not establish that the user's actual password was wrong.
This fix does not reset passwords or rewrite vault contents.

## Downloads and installation

Quit the running app before replacing it. On macOS, open
`2fast-macos-universal.dmg` and drag **2fast** into Applications. The app ZIP
contains the same universal app. Both app and DMG are Developer ID Application
signed, Apple notarized, and stapled.

On Windows, choose the x64 or ARM64 ZIP or standalone EXE matching your machine.
Extract the ZIP completely before opening `Project2FA.Uno.exe`. Windows packages
are unsigned and include the .NET runtime.

After installation, enter the current data-file password. Keep the existing vault;
no password change is required merely because the old dialog appeared.
`SHA256SUMS` covers all six application downloads and the documentation ZIP.
See the [user guide](USER-GUIDE.md), [macOS guide](../MACOS.md), and
[Windows guide](WINDOWS.md).

## Verification and limits

Synthetic UI checks confirmed that protected pages could not open before unlock,
unlocked navigation worked, and relocking cleared accounts and blocked access.
Empty and incorrect credentials were rejected, followed by successful decryption
with the correct credential. Production account/vault, generated-control, crypto,
tamper rejection, wrong-password, and legacy-migration checks passed.

All platform versions and Windows native architectures were checked. The Mac app
and DMG passed signature and ticket validation; the extracted app ZIP passed
macOS pre-distribution policy checks. Notarization IDs appear in the GitHub notes.

The user's real vault was not opened or changed during testing; its successful
unlock still requires confirmation after installation. Native Windows execution,
physical Intel Mac, camera hardware, Windows Hello, and Touch ID were not tested.
