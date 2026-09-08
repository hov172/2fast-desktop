# 2fast Desktop — Windows and macOS

Private development distribution of [2fast](https://github.com/2fast-team/2fast),
built with Uno Platform. Upstream licensing and attribution are retained.

## Downloads

Current release: **1.3.6** (app title **2fast**).

Get the builds from [Releases](https://github.com/hov172/2fast-desktop/releases).

| Package | Platform |
| --- | --- |
| `2fast-macos-universal.zip` | macOS, Apple Silicon and Intel |
| `2fast-windows-x64.zip` | Windows, Intel/AMD x64 |
| `2fast-windows-arm64.zip` | Windows ARM64 |

Extract the Windows archive before launching `Project2FA.Uno.exe`. Windows
packages are unsigned portable builds. Mac builds use development signing.

## Features

- Shared encrypted vaults, account editing, QR display, and countdowns.
- Camera and screen QR scanning, TOTP, supported OCRA and Deepnet MobileID imports.
- Touch ID on enrolled Macs and Windows Hello integration on Windows.
- Authenticated V4 vault encryption, legacy import/upgrade, backups and WebDAV.

Update both desktop apps before selecting **Settings → Data file → Upgrade vault
 encryption**. Updated Windows and Mac builds share V4 files. Older Windows
releases and mobile clients cannot read upgraded files. Device-bound tokens and
biometric credentials remain device-specific.

## Documentation

Start with the [complete user guide](docs/USER-GUIDE.md): installation, first vault,
camera/screen scanning, manual accounts, editing, OCRA/Deepnet, biometrics,
backup/restore, WebDAV and troubleshooting.

- [macOS setup, building and use](MACOS.md)
- [Windows setup, building and use](docs/WINDOWS.md)
- [Desktop compatibility](docs/DESKTOP-COMPATIBILITY.md)
- [Vault format and recovery](docs/MACOS-VAULT-FORMAT.md)
- [Verification and outstanding hardware acceptance](docs/audits/2026-09-08-desktop-parity.md)
- [Release process](docs/RELEASING.md)
- [Original upstream README](docs/UPSTREAM-README.md)

## Source and validation

Use the .NET SDK pinned in `global.json`. Desktop build and test scripts are in
`scripts/`; follow each platform guide for prerequisites and signing setup.
Vendored dependency source is included in this repository, including local
platform changes; no submodule initialization is needed.

Windows x64/ARM64 builds and managed regression tests pass. Synthetic V4 vault
exchange passes in both directions. Actual Windows UI, camera/screen capture,
and Hello acceptance still require a Windows PC. Successful Touch ID unlock
requires enrollment and has not been exercised on the available Mac. Physical
Intel Mac acceptance remains outstanding.
