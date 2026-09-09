# 2fast 1.4.4 for Windows — Uno desktop

The app title is **2fast**, version **1.4.4**, build **144**.
See the [complete user guide](USER-GUIDE.md) for step-by-step vault setup, account
editing, OCRA/Deepnet, backup/restore, WebDAV and troubleshooting.

Extract the complete architecture-specific ZIP and run `Project2FA.Uno.exe`.
Alternatively, download the matching standalone `2fast-windows-x64.exe` or
`2fast-windows-arm64.exe`; these embed the runtime, libraries, and app assets
and extract them automatically when launched.
Use x64 for Intel/AMD PCs and ARM64 for ARM Windows PCs. The .NET runtime and
matching native camera library are included. Keep the DLLs beside the executable.
This is a locally built, unsigned desktop distribution, not a Windows Store update.
Use a supported Windows 10/11 x64 or Windows 11 ARM64 system. Windows-native
acceptance testing remains outstanding.

Open your existing `.2fa` file using the application's file-selection flow.
See [desktop compatibility](DESKTOP-COMPATIBILITY.md) before upgrading a vault shared with other clients.
The updated Windows and macOS desktop apps read and write the same V4 format.

- **Scan camera** opens continuous scanning with a preview and device selection.
- **Scan screen** lets you select a window or display and continues until a QR is found.
- Both routes use the same TOTP/OCRA/Deepnet parser and account review as macOS.
- **Settings → Data file** contains rename/move, encrypted backup, password change,
  encryption upgrade, open, and create actions.
- **Settings → Use Windows Hello** protects saved unlock credentials with a Windows
  Passport key. Configure Windows Hello in Windows Settings first. Password unlock
  remains available if Hello is unavailable, canceled or invalidated.

Camera privacy controls: Windows Settings → Privacy & security → Camera → allow
camera access for desktop apps. A missing camera does not disable screen scanning.
If a window preview is black, select its display and keep the webpage QR visible.
Keep the scanner out of the way of the QR when capturing a whole display.

The camera package uses OpenCV's DirectShow backend. If Windows reports missing
native runtime components, install Microsoft's supported Visual C++ runtime for
this architecture from the official page below. The unused FFmpeg video-file
backend is omitted from these archives.

## Install an update

Quit the old app. Extract the new archive into a fresh folder and launch the new
Project2FA.Uno.exe. Do not mix DLLs from different releases or architectures. Keep
your vault and backups outside the application folder. App replacement does not
automatically upgrade vault encryption. This release does not update the old
Microsoft Store application; open your vault using the new Uno desktop executable.

## Windows Hello

Configure Windows Hello under Windows Settings → Accounts → Sign-in options.
Unlock the vault by password before enabling **Use Windows Hello** in 2fast. Enter
the vault password when asked and complete the Windows prompt. A PIN, fingerprint,
or face may be offered by Windows. Canceling leaves password login available.
Re-enroll after changing the vault password/path or invalidating platform keys.
Disabling the option revokes its protected saved credential without deleting accounts.

The implementation wraps an AES key using a nonexportable Windows Passport RSA
key requiring authentication; per-user DPAPI also protects the stored credential
envelope. Neither copying the vault nor copying an app folder transfers Hello
registration. Native Windows Hello acceptance has not been run on a Windows PC.

## Scanning tips

The source list includes available cameras, windows and displays. **Refresh sources**
updates it after device/window changes. Check the live preview before positioning
a QR. Blank frames continue scanning; a decoded supported QR opens account review.
Save that review to commit the account. Cancel closes the scanner. If a camera
driver stalls, the dialog can close while the worker finishes releasing the driver;
try a display source or close other camera applications before trying again.

Camera access for desktop applications must be permitted in Windows privacy
settings. Windows uses its own source list, not Apple's sharing picker. Window
capture can fail for protected/GPU content; selecting the display is the fallback.
Camera and screen frames are processed locally. No camera is required for screen
scanning. See the user guide for the distinction between successful QR decoding
and successful account import.

## About the app in 1.4.4

**About the app** now shows the version, build number, OS and app/system
architectures. Use **Copy app details** for support reports. The page also links
to the user guide, releases, issues, source code, and GPL-3.0 license, with credit
to the upstream 2fast project.

## Reliability fixes retained from 1.4.1 (2026-09-08)

- **Vault saves failed on Windows** with "The changes could not be saved":
  the atomic file writer set a Unix-only file mode, which .NET rejects on
  Windows. The mode is now applied only on Unix-like systems; saving vaults
  and creating new data files works on Windows.
- **New-vault retry loop**: a failed first attempt left an empty `.2fa`
  placeholder that made every retry fail. Empty leftovers are reused or
  cleaned up automatically; non-empty vaults are never overwritten.
- **Scanner preview showed nothing**: the preview relied on a WinRT
  `DataWriter` API not implemented in the Uno desktop target. Frames are now
  written to the preview stream directly, so camera/window/screen previews
  display and QR detection proceeds, matching macOS behavior.

## Build and test

Use the .NET SDK pinned in the root global.json. Dependencies are restored through
NuGet and vendored source; no Git submodule setup is required. Run from the
repository root on Windows:

```powershell
./scripts/build-windows.ps1 -Runtime win-x64
./scripts/build-windows.ps1 -Runtime win-arm64
./scripts/test-windows.ps1
# With Windows Hello enrolled and a person available for its prompt:
./scripts/test-windows.ps1 -Hello
```

On macOS/Linux the equivalent cross-build command is `./scripts/build-windows.sh`.
The Windows native tests cannot run on those systems. The old UWP project remains
in the source archive for reference; this distribution uses the shared Uno desktop
project and its Win32 host.

References:
- [Uno desktop publishing](https://platform.uno/docs/articles/uno-publishing-desktop.html)
- [Microsoft Visual C++ runtime](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist)
- [OpenCvSharp source](https://github.com/shimat/opencvsharp/tree/b161e7e012f5101f6d5dc68a835c59db6cc88b18)
