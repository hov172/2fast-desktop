# 2fast for Windows — Uno desktop

Extract the complete architecture-specific ZIP and run `Project2FA.Uno.exe`.
Use x64 for Intel/AMD PCs and ARM64 for ARM Windows PCs. The .NET runtime and
matching native camera library are included. Keep the DLLs beside the executable.
This is a locally built, unsigned desktop distribution, not a Windows Store update.
Use a supported Windows 10/11 x64 or Windows 11 ARM64 system. Windows-native
acceptance testing remains outstanding.

Open your existing `.2fa` file using the application's file-selection flow.
See **DESKTOP-COMPATIBILITY.md** before upgrading a vault shared with other clients.
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

Build from source on Windows:

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
