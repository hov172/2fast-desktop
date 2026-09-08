# 2fast 1.3.6

Release build for macOS Universal (Apple Silicon and Intel), Windows x64 and
Windows ARM64. The app title is now **2fast**, without the Beta label. Display
and assembly version are 1.3.6, package manifest version 1.3.6.0, and build 136.
Application identifiers remain unchanged to preserve existing app data access.

## Upgrade

Quit the running app, extract the new archive, and replace your previous app.
Windows users should extract into a fresh folder and run Project2FA.Uno.exe.
Keep a backup of your vault. Existing V0–V3 files remain readable; V4 encryption
upgrade is optional through Settings → Data file → Upgrade vault encryption.
Updated Windows and macOS desktop builds share V4 files. Older Windows releases
and mobile clients cannot open V4. Device-bound credentials do not transfer.

## Signing and verification limits

Release status removes the beta designation; it does not change signing trust.
The Mac app is Apple Development signed and is not Developer ID signed or
notarized. Gatekeeper can reject it on other Macs. Windows archives are unsigned.
Windows native UI/camera/screen capture/Hello acceptance and physical Intel Mac
acceptance remain outstanding. Successful Touch ID unlock needs an enrolled
fingerprint and was not exercised on the available Mac.

All three packages are rebuilt from this tag's application source. See the
attached platform guides, compatibility and verification documents. SHA256SUMS
covers exactly the three attached build archives.
