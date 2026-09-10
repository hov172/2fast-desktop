# 2fast 1.5.0 for macOS

The app is named **2fast**, version **1.5.0**, build **150**. Download
`2fast-macos-universal.dmg` from the private repository's
[release page](https://github.com/hov172/2fast-desktop/releases/tag/v1.5.0).

## Requirements and installation

- macOS 15 or newer, Apple Silicon or Intel.
- No separate .NET runtime installation or architecture selection is needed.
- Camera scanning needs an accessible camera and camera permission.
- Touch ID needs compatible hardware and an enrolled fingerprint.

Open the DMG and drag `2fast.app` into Applications. A ZIP containing the same
app is also available. Quit an older running copy before replacing
it. Keep your `.2fa` vault and backup files separate from the application. Replacing
the app does not replace the vault or automatically upgrade its encryption.

**Release signing:** the published app and DMG are Developer ID Application
signed, notarized by Apple, and stapled with their notarization tickets. Local
source builds using the setup below initially use Apple Development signing;
release packaging requires Developer ID signing and notarization before upload.

## About the app

Open **About the app** in the navigation menu for the version, build number, OS
and architecture details. **Copy app details** copies those fields for a support
report. The page also links to the user guide, releases, issues, source code and
GPL-3.0 license, and credits the original 2fast project. Private project links
require repository access.

## First use and daily operation

Follow the [complete user guide](docs/USER-GUIDE.md) for creating/opening a vault,
adding accounts, copying codes, renaming accounts, backups, WebDAV and recovery.

Quick path:

1. Create a new data file with a name/password, or open an existing `.2fa` file.
2. Use **Scan camera**, **Scan screen**, **Add manually**, or **Add OCRA** in Accounts.
3. Review the imported account and save it.
4. Use **Copy code** for a current OTP; the countdown indicates its remaining time.
5. Use **Edit** for account names/details and **Settings → Data file** for the vault.

The password is used exactly, including spaces. Reveal controls help inspect the
entry. A Mac with no fingerprint enrolled uses password unlock even if Touch ID
hardware is present. There is no vault password recovery service.

## Touch ID setup

1. Enroll a fingerprint in **System Settings → Touch ID & Password**.
2. Unlock the vault by password, then enable **Use Touch ID** in 2fast Settings.
3. Confirm the vault password and complete the native Touch ID prompt.
4. Use **Unlock with Touch ID** on the login screen. **Use Touch ID on startup**
   controls automatic prompting; explicit logout leaves the app locked.

Canceling/locking out Touch ID leaves password unlock available. Re-enable it
after a password change, vault move/rename, or fingerprint changes. Disable the
option to revoke the saved unlock credential. Enrollment is local to this Mac.

The password is stored with a device-only Data Protection Keychain
`biometryCurrentSet` constraint. Keychain enforces biometric access itself.
Passwords otherwise remain in the unlocked app session; WebDAV credentials use
Keychain. Migration from the historical desktop secret store discards saved vault
passwords, moves WebDAV credentials to Keychain, and preserves the old store if
migration fails. It does not fall back to weaker persistence on failure.

## Camera and screen scanning

**Camera:** choose **Scan camera**, allow access, choose a camera if needed, and
keep the entire setup QR in focus. Camera capture stops on closing the scanner or
switching to another app. If access was denied, review 2fast's camera permission
in System Settings → Privacy & Security → Camera.

**Webpage:** choose **Scan screen** or the screen option inside the scanner. Select
a browser window or display in Apple's sharing picker. Keep the QR visible and
check the preview; zoom the webpage if necessary. Display capture includes other
visible windows, so move the scanner away from the QR. Screen capture can continue
while you interact with the browser. Cancel, closing the scanner, successful
decoding, navigation/logout, or Apple's Stop Sharing ends capture.

Camera uses AVFoundation, screen sharing uses ScreenCaptureKit, and QR recognition
uses Vision. Frames are processed locally; the scanner does not record video,
capture audio, upload images, or save screenshots. Both sources lead to the same
account review and save workflow.

See the [user guide](docs/USER-GUIDE.md) for supported TOTP, OCRA and MobileID
profiles. OCRA/MobileID QR export, Deepnet push registration, mutual authentication,
and unsupported enrollment profiles are not enabled by screen scanning.

## Vault management and Windows compatibility

**Settings → Data file** offers rename, move, encrypted backup, change password,
encryption upgrade, open, and create actions. Use these buttons to change the file;
its displayed details are not editable text inputs.

Updated Windows and macOS desktop builds share V4 vaults. Update both apps before
**Upgrade vault encryption**. Older Windows/UWP and mobile clients cannot open
V4. The upgrade preserves a legacy encrypted backup; password changes do not
update passwords on old backups. See [compatibility](docs/DESKTOP-COMPATIBILITY.md)
and [vault format/recovery](docs/MACOS-VAULT-FORMAT.md).

## Build from source

Install Xcode Command Line Tools, XcodeGen, and the .NET SDK specified by
`global.json` (10.0.300 with patch roll-forward). The project pins Uno.Sdk 6.6.42.
Dependency source is vendored in this repository, including local modifications;
there are no submodules to initialize.

Protected Keychain access needs Apple-authorized signing entitlements and a
provisioning profile. Sign into Xcode with the appropriate Apple developer account:

```sh
./scripts/setup-macos-signing.sh YOUR_APPLE_TEAM_ID
export APPLE_DEVELOPER_ID=YOUR_DEVELOPER_ID_CERTIFICATE_SHA1
export APPLE_DISTRIBUTION_PROFILE=/path/to/distribution.provisionprofile
bash scripts/build-macos.sh universal
```

Setup provisions `jpweber.it.Project2FA.Uno` and writes local signing inputs under
`build/macos-signing/`. These files and profiles are ignored by Git; private keys
stay in Keychain. Renew expired profiles/certificates through the same setup.

The default build produces `dist/2fast.app` and `dist/2fast-macos-universal.zip`.
To build an intermediate single-architecture app, use `osx-arm64` or `osx-x64`.
Each goes under `src/Project2FA.Uno/bin/Release/net10.0-desktop/<runtime>/publish/`.
Single-architecture commands do not refresh the universal archive.

The universal launcher contains arm64/x86_64 slices and selects the matching
self-contained signed app in `Contents/Helpers/arm64` or `Contents/Helpers/x64`.
It preserves that app's libraries, resources, profile, and bundle identity.
The native Objective-C bridge is compiled with warnings treated as errors.

## Verification

```sh
./scripts/test-macos.sh
/usr/bin/python3 scripts/test-macos-universal.py
codesign --verify --deep --strict dist/2fast.app
```

Additional compiled-app contracts after building:

```sh
dotnet run --project tests/MacOS/VaultModelTests/VaultModelTests.csproj -c Release -- src/Project2FA.Uno/bin/Release/net10.0-desktop/osx-arm64 .
```

With a fingerprint enrolled and a person available:

```sh
./scripts/test-macos.sh --biometric
open -n tests/MacOS/bin/NativeTests.app --args --camera
open -n tests/MacOS/bin/NativeTests.app --args --screen
```

These native checks use a separate synthetic test app/credential. Its profile must
authorize its bundle/access group. Do not use production enrollment secrets as
fixtures. The available Mac has no fingerprint enrolled: negative enrollment was
checked, successful Touch ID unlock remains untested. Intel compiled checks run
under Rosetta; physical Intel acceptance remains outstanding. Live capture and
Deepnet server interoperability need platform acceptance beyond managed tests.
See the [verification report](docs/audits/2026-09-08-desktop-parity.md) and
[release notes](docs/RELEASE-v1.5.0.md).

## Licensing and provenance

This private distribution retains upstream GPL-3.0 licensing; see [LICENSE](LICENSE).
It is not an official upstream 2fast release. Vendored dependencies were initially
restored from BiometryService `c8e9aa11807e6df54a31109ead1a625fca25ab41`, Otp.NET
`06f39f3781dc8f4b8c59d1c30a86a9c767af0ccf`, UNOversalTemplate
`d09e40ab67f60dbf2cb1f0829d9c47f7c96424bd`, and ZXing.Net.Uno
`cb9baedd1034a22248aca75ec74be975acf2cbfb`, then modified locally. Preserve each
dependency's license when redistributing.

## Release privacy in 1.4.5

The release removes registered-device provisioning lists, debug-symbol files and
maintainer workstation paths. Mac signatures still identify the accepted
Developer ID certificate holder and team. Older application downloads have been
withdrawn as part of this cleanup; use the current release.

For universal release packaging, export `APPLE_DEVELOPER_ID` with your Developer
ID certificate SHA-1 and `APPLE_DISTRIBUTION_PROFILE` with your local all-device
profile path. The packager rejects development profiles and signs nested apps.
The resulting ZIP still needs Apple notarization and stapling before publication;
see [release procedure](docs/RELEASING.md).
