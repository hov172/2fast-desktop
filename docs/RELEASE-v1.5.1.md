# 2fast 1.5.1 — Desktop boundary and localization cleanup

Version **1.5.1**, build **151**, for macOS universal and Windows x64/ARM64.

Vaults, accounts, scanning, biometrics, WebDAV and the V4 format remain
compatible. This release completes the desktop/shared boundary cleanup,
consolidates parser and codec ownership, and routes desktop dialogs and status
messages through the resource pipeline. Upgrading from 1.5.0 requires no vault
migration.

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
- Removed all fully-qualified shared-to-desktop calls. Shared code now uses an
  explicit conditional seam and `DesktopVaultCodec` is owned by shared
  serialization.
- Replaced the desktop parser fork with the injected `StrictProject2FAParser`
  registration and moved pure desktop input validation into shared code.
- Renamed implementation types from `MacOS*` to `Desktop*` so Windows and
  macOS use the same desktop implementation names.
- Localized data-file, OCRA, biometric, and QR-import UI through `DesktopText`
  resource keys with safe English fallbacks.

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

The focused suites run from `scripts/test-windows.ps1` and
`scripts/test-macos.sh`. The complete Uno app build requires the platform
workloads and is validated on matching Windows/macOS build hosts.

## Build

- The legacy UWP project now compiles. Its MSIX signing certificate is required
  for `Release` only, so `Debug` no longer fails packaging on machines without
  it. This project is not shipped and is not part of either platform's build; it
  serves as a second compiler over the shared layer. See
  [uwp-head.md](uwp-head.md).
- The Windows and macOS applications build from
  `src/Project2FA.Uno/Project2FA.Uno.csproj` with application version 1.5.1
  and build number 151.

## Contributor documentation

`CLAUDE.md`, `docs/architecture.md`, `docs/interactions.md`, `docs/plan.md`,
and the platform guides record the updated boundaries, resource seam, build
requirements, and release verification status.

See the [user guide](USER-GUIDE.md), [macOS guide](../MACOS.md) and
[Windows guide](WINDOWS.md).
