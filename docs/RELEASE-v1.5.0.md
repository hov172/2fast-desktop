# 2fast 1.5.0 — Internal cleanup, coverage and contributor context

Version **1.5.0**, build **150**, for macOS universal and Windows x64/ARM64.

**No user-facing behaviour changes.** Vaults, accounts, scanning, biometrics,
WebDAV and the V4 format are untouched. This release consolidates duplicated
code, adds test coverage and documents the project's structure. Upgrading from
1.4.5 requires no action and changes nothing you can see in the app.

## Removed and consolidated

- Deleted `Project2FA.Core/Services/WebDAV/` — an unreferenced fork of the live
  `Project2FA.Shared/Services/WebDAV/` whose `WebDAVClientService.GetClient()`
  returned `null` unconditionally. The project file had already excluded the
  folder from compilation, so nothing shipped from it. 627 lines.
- `FavouriteToIconConverter`, `ShowCodeToIconConverter`,
  `FavouriteTooltipConverter` and `TOTPVisibilityTooltipConverter` had identical
  bodies differing only in their constant pair. They now derive from a single
  `BoolToValueConverter`. Class and resource key names are unchanged.
- The andOTP and 2FAS importers each carried a copy of the AES/GCM constants, the
  PBKDF2 call shape and the hash-algorithm mapping. Those move to
  `BackupCryptoHelper`; each importer keeps its own digest, iteration count and
  payload layout. The Aegis importer is unchanged.
- Renamed `AndOtpBackupImportService.cs` to `AndOTPBackupImportService.cs`. The
  shared project manifest already declared the `AndOTP` spelling, so the file
  resolved on Windows and on a case-insensitive macOS volume but would have
  failed to compile on a case-sensitive filesystem.

## Added coverage

- `tests/Desktop/ImporterCryptoTests` — 34 checks over the shared backup-import
  cryptography. Key derivation is cross-checked against .NET's own PBKDF2 for
  both digests, the cipher configuration is proven to be AES-256-GCM against
  .NET's `AesGcm`, and a tampered payload must be rejected rather than returned
  as corrupt account data.
- `tests/Desktop/SharedProjectTests` — verifies that every source file in the
  shared project is declared in its manifest exactly once with matching case. A
  file present on disk but missing from the manifest compiles into no
  application and fails silently.

Both suites run from `scripts/test-windows.ps1` and `scripts/test-macos.sh`.

## Build

- The legacy UWP project now compiles. Its MSIX signing certificate is required
  for `Release` only, so `Debug` no longer fails packaging on machines without
  it. This project is not shipped and is not part of either platform's build; it
  serves as a second compiler over the shared layer. See
  [uwp-head.md](uwp-head.md).
- The Windows and macOS applications continue to build from
  `src/Project2FA.Uno/Project2FA.Uno.csproj` exactly as before.

## Contributor documentation

`CLAUDE.md` and `docs/architecture.md`, `ux-flows.md`, `design.md`,
`interactions.md`, `plan.md` and `uwp-head.md` record the project's conventions,
structure, user journeys, design tokens and remaining work — including a measured
inventory of the duplication that has not yet been retired.

See the [user guide](USER-GUIDE.md), [macOS guide](../MACOS.md) and
[Windows guide](WINDOWS.md).
