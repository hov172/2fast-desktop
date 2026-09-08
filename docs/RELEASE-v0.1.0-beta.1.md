# 2fast Desktop v0.1.0-beta.1

Uno desktop builds for macOS (universal Apple Silicon/Intel), Windows x64,
and Windows ARM64, with shared account and vault management.

## Included

- Camera and screen QR scanning, QR display, account editing and countdowns.
- Supported OCRA/Deepnet MobileID import workflows.
- Touch ID integration on enrolled Macs and Windows Hello integration.
- V4 authenticated vault encryption and legacy vault upgrade with backups.
- Shared Windows/macOS vault handling and conditional HTTPS WebDAV transactions.

## Install and compatibility

Download the archive for your platform and extract it. On Windows run
Project2FA.Uno.exe from the extracted directory. Windows builds are unsigned
portable packages; the Mac app uses development signing and is not a notarized
production release. See the attached platform guides for prerequisites.

Update both desktop applications before Settings → Data file → Upgrade vault
 encryption. Updated Windows and macOS can share V4 vaults. Older Windows releases
and mobile clients cannot open upgraded files. Device-bound tokens and biometric
credentials remain device-specific. Preserve your backup before upgrading.

## Validation and limitations

Both Windows architectures and the universal Mac app built successfully. Shared
managed regressions and synthetic encrypted vault exchange passed in both
directions. Actual Windows UI, camera/screen capture and Windows Hello need
Windows hardware acceptance. Successful Touch ID unlock was not exercised because
the available Mac has no enrolled fingerprint; physical Intel Mac testing remains
outstanding. This is a beta prerelease, not a claim of completed hardware validation.

SHA256SUMS covers the three build archives. Full source, licenses, documentation,
and verification details are included in the tagged repository.
