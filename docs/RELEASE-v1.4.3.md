# 2fast 1.4.3 — Classic accounts layout

Version **1.4.3**, build **143**, for macOS 15+ (Apple Silicon and Intel) and
Windows x64/ARM64.

## Changes

- Restored wide, flat account rows with cyan accents, large codes, copy controls,
  and countdown rings. Service and account names remain visible together.
- Added a compact navigation rail and a top toolbar with Add account, Search,
  Lock app, and Reload data file. The Add account menu retains all import methods.
- Kept edit, QR display, favorites, OCRA challenge, and delete in each row's ⋯ menu.
- Fixed expanded navigation covering and dimming account rows at desktop widths:
  at 900 logical pixels and above the pane pushes content aside. Narrow windows
  retain a dismissible overlay. The pane starts collapsed.
- Preserved the expanded About page, version/build information, support links,
  vault operations, and existing authentication functions.

## Downloads and installation

On macOS, open `2fast-macos-universal.dmg` and drag **2fast** into Applications.
The ZIP contains the same universal app. Quit the old copy before replacing it.
The app and DMG are Developer ID signed, Apple notarized, and stapled.

On Windows, choose the x64 or ARM64 ZIP or standalone EXE matching your system.
Extract the ZIP completely before opening `Project2FA.Uno.exe`. Windows downloads
are unsigned. No separate .NET installation is required. Updating the application
does not replace your vault.

`SHA256SUMS` covers all six application downloads and the documentation ZIP.
See the [user guide](USER-GUIDE.md), [macOS guide](../MACOS.md), and
[Windows guide](WINDOWS.md). The release notes on GitHub include notarization IDs.

## Verification scope

Release validation covers platform builds, embedded versions, account/vault
workflow checks, universal launcher dispatch, Windows native architecture checks,
Developer ID signatures, Apple notarization acceptance, stapled tickets, and macOS
pre-distribution policy checks. The sidebar is also checked with synthetic accounts.
Native Windows launch, camera/screen capture, Windows Hello, physical Intel Mac,
and Touch ID acceptance require target hardware and were not run for this release.
