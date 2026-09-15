# 2fast 1.5.2 — Windows icon and release-pipeline hardening

Version **1.5.2**, build **152**, for macOS universal and Windows x64/ARM64.

Vaults, accounts, scanning, biometrics, WebDAV and the V4 format are unchanged.
This is a packaging release: it ships the four fixes made after the `v1.5.1`
source tag as a properly versioned build, so every attached binary maps to the
tagged commit. Upgrading from 1.5.1 requires no vault migration.

## Windows

- The Windows executables now carry an embedded application icon. Uno's
  Resizetizer only injects an `.ico` into WindowsAppSDK targets, not the
  `net10.0-desktop` Skia head, so `Platforms/Desktop/icon.ico` is composed from
  the generated PNGs and set as `ApplicationIcon`. Explorer, the desktop and
  the installer-free ZIP now show the 2fast icon.
- The running window sets its Win32 icon at startup, so the taskbar button and
  Alt-Tab switcher show the app icon instead of the generic .NET one.
- Standalone single-file `2fast-windows-x64.exe` and `2fast-windows-arm64.exe`
  are built by `scripts/build-windows.sh` alongside the ZIPs. They embed the
  runtime, native libraries and content assets and self-extract on launch.

## Build and release pipeline

- `dotnet restore` runs with `Configuration=Release` on both platforms. A Debug
  restore graph pulls the Uno SDK Hot Design / MCP development tooling into a
  `--no-restore` Release publish; the 1.5.1 assets were rebuilt to remove it and
  the scripts now prevent it.
- macOS release signing fails fast unless `APPLE_DISTRIBUTION_PROFILE` points at
  a Developer ID (Direct) provisioning profile whose entitlements cover the
  bundle. Signing with the wrong certificate produced an app that launchd
  refused to spawn (`RBSRequestErrorDomain` code 5).
- `scripts/package-windows.py` and `scripts/package-release-docs.py` checksum
  the standalone EXEs together with the ZIPs, DMG and documentation archive.

## Verification

- Windows: PE architecture checks for the app host, CLR and camera native
  library; `ICON`/`GROUP_ICON` resources present; privacy gate passed.
- macOS: universal bundle signed with Developer ID, Hardened Runtime and
  timestamps; notarized and stapled; Gatekeeper reports Notarized Developer ID.
- Hardware acceptance limits from 1.5.1 still apply: Touch ID unlock with an
  enrolled finger and physical Intel Mac runs remain untested.

See the [user guide](USER-GUIDE.md), [macOS guide](../MACOS.md) and
[Windows guide](WINDOWS.md).
