# 2fast Desktop — Windows and macOS

This Windows and macOS desktop version is built on top of the original
[2fast project by the 2fast team](https://github.com/2fast-team/2fast), using Uno
Platform.

## Credit to the original project

The original **2fast team and all of its contributors** deserve credit for
creating 2fast and the excellent work that made this version possible. Their
code, design, and ongoing contributions provide the foundation for this desktop
version. Thank you for building and sharing such an awesome open-source project.

This repository extends their work with desktop adaptations and additional
features. It is an independent development distribution, not an official release
from the original 2fast team. Original attribution and the [GPL-3.0 license](LICENSE)
are retained.

Please visit the [original repository](https://github.com/2fast-team/2fast) to
learn more about their work and support the project. The
[original upstream README](docs/UPSTREAM-README.md) is also preserved here.

## Downloads

Current desktop release: **1.4.5** (app title **2fast**).

Get the builds from [Releases](https://github.com/hov172/2fast-desktop/releases).

| Package | Platform |
| --- | --- |
| `2fast-macos-universal.dmg` / `.zip` | macOS, Apple Silicon and Intel |
| `2fast-windows-x64.zip` / `.exe` | Windows, Intel/AMD x64 |
| `2fast-windows-arm64.zip` / `.exe` | Windows ARM64 |

Extract the Windows archive before launching `Project2FA.Uno.exe`. Windows
packages are unsigned portable builds; standalone executables are also available. Open the
Mac DMG and drag 2fast into Applications. The published Mac app and DMG use
Developer ID signing with Apple notarization and stapled tickets.

## Features

- Adaptive desktop navigation, light/dark themes, readable account cards and
  visible copy/edit/QR actions. Search by account name or service.

- Shared encrypted vaults, account editing, QR display, and countdowns.
- **Scan QR codes directly from your desktop through screen sharing**: select a
  browser window or display and import an authenticator QR without a camera.
- Live camera QR scanning, TOTP, supported OCRA and Deepnet MobileID imports.
- Touch ID on enrolled Macs and Windows Hello integration on Windows.
- Authenticated V4 vault encryption, legacy import/upgrade, backups and WebDAV.

Update both desktop apps before selecting **Settings → Data file → Upgrade vault
 encryption**. Updated Windows and Mac builds share V4 files. Older Windows
releases and mobile clients cannot read upgraded files. Device-bound tokens and
biometric credentials remain device-specific.

## Scan a QR code from a webpage or your desktop

You can scan an authenticator setup QR displayed on the **same computer** using
**Scan screen**. A camera or a second device is not required.

1. Open the webpage containing the authenticator setup QR and keep the full code
   visible.
2. Open **Accounts → Scan screen** in 2fast. You can also choose the screen option
   inside the camera scanner.
3. Choose what to share/capture:
   - **macOS:** use Apple's screen-sharing picker to select the browser window or
     an entire display.
   - **Windows:** select a window or display from the scanner's source list.
4. Check the live preview. When capturing an entire display, move 2fast out of
   the way so it does not cover the QR. If a window preview is black, try selecting
   its display instead.
5. The scanner reads successive frames until it finds a QR. Review the imported
   account, then save it to your vault.

Screen frames are processed locally; they are not uploaded to a screen-sharing
service. Both scan sources use the same supported TOTP, OCRA and Deepnet MobileID
import workflow. Unsupported QR profiles cannot be imported simply by sharing
the screen. Use **Cancel** to stop scanning, or Apple's **Stop Sharing** on Mac.

Windows native screen-capture acceptance still requires testing on a Windows PC;
see [verification limits](docs/audits/2026-09-08-desktop-parity.md). More scanning
and troubleshooting instructions are in the [user guide](docs/USER-GUIDE.md).

## Documentation

Start with the [complete user guide](docs/USER-GUIDE.md): installation, first vault,
camera/screen scanning, manual accounts, editing, OCRA/Deepnet, biometrics,
backup/restore, WebDAV and troubleshooting.

- [macOS setup, building and use](MACOS.md)
- [Windows setup, building and use](docs/WINDOWS.md)
- [Desktop compatibility](docs/DESKTOP-COMPATIBILITY.md)
- [Vault format and recovery](docs/MACOS-VAULT-FORMAT.md)
- [Verification and outstanding hardware acceptance](docs/audits/2026-09-08-desktop-parity.md)
- [Desktop UI review and verification](docs/audits/2026-09-08-desktop-ui.md)
- [Release process](docs/RELEASING.md)
- [Original upstream README](docs/UPSTREAM-README.md)

### For contributors

This README covers *what* 2fast Desktop is. How it is built and changed lives in
a companion set of documents — read the one that matches your question:

- [CLAUDE.md](CLAUDE.md) — conventions, the reuse map (what already exists, so
  you do not write it again), security rules, and build/test commands
- [docs/architecture.md](docs/architecture.md) — projects, data flow, and known
  structural debt
- [docs/ux-flows.md](docs/ux-flows.md) — the user journeys the app supports
- [docs/design.md](docs/design.md) — colour, typography, layout and controls
- [docs/interactions.md](docs/interactions.md) — motion, state and feedback
- [docs/plan.md](docs/plan.md) — the current phase and what is out of scope
- [docs/uwp-head.md](docs/uwp-head.md) — the legacy UWP project: how to build it,
  and why it is compile-only

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
