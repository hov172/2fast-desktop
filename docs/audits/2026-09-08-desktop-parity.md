# Windows/macOS desktop parity — September 8, 2026

The updated Windows x64 and ARM64 Uno desktop packages now compile the same
account workflows, vault management, OCRA/MobileID parser, QR rendering, and V4
vault codec as the universal macOS app. These packages replace neither an
installed Windows Store/UWP release nor any mobile client automatically.

Existing V0–V3 vaults remain readable. Upgrade through Settings → Data file →
Upgrade vault encryption after updating both desktop applications. Updated
Windows and macOS can exchange V4 files; older releases and mobile clients cannot.
Upgrade retains an encrypted legacy backup. Device-bound tokens and biometric
credentials remain device-specific.

## Changes and defects corrected

- Promoted the desktop account, navigation, QR review, save, and vault settings
  implementations into a shared desktop compilation path.
- Added Windows live camera, window, and display scanning with preview, source
  switching, repeated frame decoding, cancellation, and scanner serialization.
  Camera capture uses DirectShow; screen/window capture uses Windows GDI. Some
  GPU/protected windows require selecting the display instead.
- Added Windows Hello protected credential storage using Passport RSA key
  wrapping, AES-GCM, and per-user DPAPI. Windows enrollment is required; ordinary
  password unlock remains available. Hello may offer PIN, face, or fingerprint.
- Fixed biometric enrollment incorrectly comparing a password hash against a
  modern random vault credential identifier. Enrollment and navigation now
  verify the password against authenticated vault contents.
- Corrected platform labels and encryption upgrade compatibility messages.
- Added Windows build/package scripts, native credential acceptance tests, and
  camera decoder regression tests. Packages include architecture-matched native
  dependencies and omit unused FFmpeg binaries.

## Completed verification

- Release publish of Windows x64 and ARM64, including PE architecture checks for
  the app host, CLR, and camera library.
- Universal macOS release build and strict bundle signature verification.
- Compiled Windows managed contracts on the Mac test host for both architectures:
  account cards, navigation/actions, QR PNG rendering, concurrent encryption,
  authenticated V4 vaults, and legacy migrations.
- Synthetic vault exchange in both directions between compiled Windows and Mac
  codecs, preserving account data. No user vaults were used or changed.
- QR frame regressions covering blank frames followed by QR, repeated and
  inverted codes, TOTP/OCRA/MobileID, exact URI preservation, and frame limits.
- Shared parser, OCRA, file transactions/fault injection, WebDAV, device binding,
  icon, secret, and Mac native regression suites.
- NuGet audit of the final Windows dependency graph reported no known vulnerable
  packages from the configured sources at the time of the check.

## Remaining hardware acceptance

Windows packages were cross-built on macOS. Running their managed assemblies on
the Mac test host does not validate Windows UI, camera drivers, GDI capture, or
Windows Hello. Those require a Windows PC; use scripts/test-windows.ps1 and the
manual workflow in docs/WINDOWS.md. Windows packages are unsigned portable builds.
The available ARM Mac has no enrolled Touch ID, so successful biometric unlock
was not exercised. Physical Intel Mac acceptance is also outstanding.
