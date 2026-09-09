# 2fast 1.4.0 user guide

This guide describes the updated Uno desktop app for Windows and macOS. It does
not describe the older Windows Store/UWP app or mobile clients. Button wording
can vary with language and window size; the Accounts page exposes common actions
and additional actions are available from the **+** and **…** menus.

## 1. Install and open the app

Download from the private repository's [1.4.0 release](https://github.com/hov172/2fast-desktop/releases/tag/v1.4.0).
GitHub access requires an account authorized for this private repository.

| Download | Use on |
| --- | --- |
| `2fast-macos-universal.zip` | Apple Silicon or Intel Mac, macOS 15+ |
| `2fast-windows-x64.zip` | Intel/AMD 64-bit Windows PC |
| `2fast-windows-arm64.zip` | ARM64 Windows PC |

Extract the ZIP before opening the app. Mac users open `2fast.app`; Windows users
open `Project2FA.Uno.exe` and keep the other extracted files alongside it. The
.NET runtime is included. A vault is separate from the application: replacing
application files does not upgrade or replace your `.2fa` file.

The Mac app is development-signed, not Developer ID signed/notarized. Gatekeeper
can reject it, particularly on other Macs. Windows packages are unsigned. Read
the platform guide for installation limits rather than assuming release status
means Apple/Microsoft distribution approval.

## 2. Create a vault or open an existing one

A **data file**, also called a **vault**, is the encrypted `.2fa` file containing
your accounts. Its filename is independent of each account's display name.

To start fresh:

1. Choose the new-data-file option from startup, or **Settings → Data file →
   Create new data file** when a vault is already open.
2. Enter a filename and choose its folder. The default local folder is Documents.
3. Enter the vault password twice. Use the password reveal control to check it.
4. Resolve any validation message, then choose **Create data file**.
5. Keep the password somewhere you can recover it. It is needed when biometric
   unlock is unavailable and when opening the vault on another computer.

Password fields have no app-imposed length limit. The password is used exactly:
uppercase/lowercase, Unicode characters, and leading/trailing spaces all matter.
A trailing-space message does not trim the password for you. If creation is
unavailable, check matching passwords, filename, folder, and existing-file errors.
The app refuses to overwrite an existing destination vault.

To use an existing file, select it through the open-data-file flow and enter its
password. From an unlocked vault use **Settings → Data file → Open another data
file**. If a file was moved outside the app, select its new location. Do not
create a replacement over the original just to resolve a missing-file message.

There is no password recovery service. Touch ID/Hello does not make a forgotten
vault password recoverable on a different device. Creating a new vault starts a
separate collection; it does not recover accounts from an inaccessible vault.

## 3. Add an account by scanning a QR code

Use the authenticator setup QR supplied by the service or your administrator.
A normal webpage URL, login-session QR, or arbitrary QR is not necessarily an
authenticator setup code.

### Scan a phone, printed code, or another display

1. Open Accounts and choose **Scan camera** (also available through **+**).
2. Allow camera access when the operating system asks.
3. Choose the camera if more than one is available.
4. Keep the entire QR visible, in focus, and free of glare. The preview should
   show what is being scanned.
5. When the QR is decoded, review the account details, then save/confirm.
6. Wait for Accounts to return and check that the account appears.

Decoding a QR and saving an account are separate steps. Closing account review
without saving does not import it. Wait for one import to finish before starting
another. Scanning continues through frames without a QR; there is no need to
reopen the scanner for each unsuccessful frame.

### Scan a QR on a webpage on this computer

1. Open the setup webpage and make the full QR visible.
2. In Accounts choose **Scan screen**, or the screen option inside the scanner.
3. On Mac, use Apple's sharing picker to select the browser window or display.
   On Windows, choose a window or display from the scanner's source list.
4. Check the preview. If capturing a display, move the 2fast window so it does
   not cover the QR. If a window capture is black, try its display instead.
5. Keep the QR visible until account review opens, then review and save it.

Mac screen scanning can continue while you interact with the browser. Camera
capture stops when its scanner is closed; Mac camera capture also stops when
switching apps. Screen capture ends when canceled or the QR is accepted; Apple's
Stop Sharing control also ends Mac sharing. Camera/screen frames are processed
locally rather than uploaded as screenshots.

## 4. Add an account manually

Choose **Add manually** for a supported TOTP account when the service provides a
manual setup key instead of a QR. Enter the service/account details and its secret
in the form. Preserve the supplied algorithm, digit count, and period where the
form offers those choices. Review before saving. A setup key is not your website
password and is not a currently displayed six-digit login code.

For a challenge-response token choose **Add OCRA**. Enter the token name, raw
seed, and the encoding supplied by the administrator: Base32, Base64, or Hex.
The supported suite is `OCRA-1:HOTP-SHA1-6:QN08-T1M`. Do not guess a seed encoding
or substitute a TOTP setup key for an OCRA enrollment without confirmation from
the issuing service.

## 5. Read, copy, edit, and organize accounts

The Accounts page shows the issuer/account name and, for a time-based token, its
current code and countdown. A code changes when its configured period ends.
Use **Copy code** to paste into the service's verification field. If a code is
about to expire, wait for the next code before submitting it. Keep the computer's
date/time synchronized with the service.

Use **Edit** to change an account's displayed name/details and save the dialog.
Changing the account name does not rename the vault, change your website login,
or re-enroll the token with its provider. Icon/display changes do not change the
underlying secret. Favorite controls mark accounts for easier access.

Use **View QR** for a supported TOTP account to show its enrollment QR. Another
authenticator that reads this QR receives the account's secret. Display it only
when you intend to transfer the token. OCRA/MobileID QR export is deliberately
blocked because exporting those profiles as TOTP would create an incorrect token.

If you use an account's delete action, read its confirmation carefully. Removing
an account from this vault does not disable two-factor authentication on the
website; keep a working authenticator or the service's recovery method available.

## 6. Deepnet MobileID and OCRA challenges

The scanner accepts supported encrypted Deepnet `mobileid://…/mobileid/install`
envelopes, as well as explicit supported `otpauth://ocra` codes. If the QR omits
an authorization code, enter the code provided with that enrollment when asked.
The vault password and the Deepnet authorization code are different credentials.

Supported MobileID accounts normally show their changing OTP. For a challenge,
open the account's **… → OCRA challenge…** action:

1. Enter the numeric challenge from the login page (1–8 digits).
2. Choose **Generate**.
3. Use **Copy response** or type the response into the login page.
4. Submit before the current minute ends. Generate again if it expires.

Changing the challenge clears the previous response. The dialog also clears a
response when it expires or closes. If the server rejects responses, verify the
challenge, system time, and exact enrollment profile with the administrator.

Only the documented suite/profiles are implemented. HOTP/migration bundles,
legacy MobileID profiles, mutual authentication, and Deepnet push approval
registration are unsupported. A supported offline token may be imported from an
envelope containing push metadata without enrolling push approvals. Device-bound
tokens remain restricted to their enrolled device even if their vault is copied.

## 7. Enable Touch ID or Windows Hello

First configure biometrics/Hello in the operating system. Hardware capability
alone is insufficient: a Mac with Touch ID but no enrolled fingerprint cannot
use Touch ID unlock.

1. Unlock the vault using its password.
2. Open Settings and enable **Use Touch ID** or **Use Windows Hello**.
3. Confirm the vault password and complete the operating system's authentication.
4. At the next locked login, use the biometric unlock button. A startup preference
   can enable automatic prompting where offered.

Canceling a prompt leaves password login available. An explicit logout should
leave the app locked. Re-enable biometric unlock after changing the password,
moving/renaming the vault, changing fingerprints, or invalidating platform keys.
Each computer needs its own enrollment. To stop saving an unlock credential,
disable the biometric option; your vault and account entries remain separate.

Touch ID uses a biometric-protected device-local Keychain item. Windows uses
Hello-protected key wrapping and per-user DPAPI. Windows Hello may offer a PIN,
face recognition, or fingerprint; it is not identical to Mac's Touch ID policy.

## 8. Manage the data file

Open **Settings → Data file**. Details describe the active vault; use the action
buttons to make changes rather than trying to type into a displayed path.

| Action | What it does |
| --- | --- |
| Rename data file | Changes the local filename; does not rename accounts |
| Move to another folder | Relocates the local vault; protects existing destinations |
| Save encrypted backup | Saves an encrypted copy; does not export plaintext secrets |
| Change password | Verifies the current password and writes a V4 vault with the new one |
| Upgrade vault encryption | Converts a legacy vault to V4 while keeping the same password |
| Open another data file | Selects a different existing vault |
| Create new data file | Starts a separate vault |

A backup requires the password that protected it when it was created. Changing
the live vault's password does not change older backups. To restore, preserve the
current file first, select the backup through Open another data file, and unlock
it with its original password. Verify the accounts before replacing any copy.

Upgrade both desktop applications before upgrading a vault shared across Mac and
Windows. The updated apps support V4; older Windows/mobile clients do not.
Encryption upgrade retains an encrypted `.legacy-<id>.2fa` copy beside the vault.
An already upgraded vault reports that it uses authenticated encryption.

If an operation reports a recovery path, preserve that file. Do not delete or
repeatedly overwrite copies while deciding which has the newest accounts. See
[vault format and recovery](MACOS-VAULT-FORMAT.md) for transaction details.

## 9. WebDAV and multiple computers

Use the app's WebDAV setup flow with the server URL, credentials, and remote file
location supplied by your administrator/provider. Use the canonical HTTPS URL:
redirects are rejected. The server must supply strong ETags and support conditional
writes. A server merely supporting file uploads is not sufficient.

WebDAV edits, password changes, and encryption upgrades require connectivity.
Codes from an already loaded vault remain available offline. Avoid simultaneous
edits on multiple computers. On conflict, preserve local/recovery copies and
reload the authenticated remote vault before deciding what to change; do not
force an older copy over newer remote data.

Reload retains a `.before-sync-<id>.2fa` local backup. Renaming/moving a WebDAV
local copy prompts to switch to local use, leaves the server copy at its current
URL, and stops syncing that moved copy. It is not a remote rename operation.

## 10. Troubleshooting

| Symptom | Check or next step |
| --- | --- |
| Create button unavailable | Password match, filename, destination permissions, existing-file message |
| Password rejected | Reveal entry; check spaces/case and that the correct vault/backup is selected |
| Touch ID unavailable on an ARM Mac | Enroll a fingerprint; CPU architecture alone does not enable it |
| Hello/Touch ID canceled or invalidated | Unlock by password and enroll again if needed |
| Camera missing or blank | Platform privacy permissions, selected camera, another app holding the device |
| Screen preview black | Select a display; keep QR visible; some protected windows cannot be captured |
| QR read but import rejected | Unsupported profile, malformed enrollment, missing/wrong Deepnet authorization code |
| Account absent after scanning | Complete account review/save; decoding alone does not save it |
| View QR unavailable for OCRA/MobileID | Those profiles are not exported as TOTP QR codes |
| Code rejected | Time synchronization, correct account/type, expiration, provider enrollment profile |
| Data-file fields cannot be typed into | Use Settings → Data file action buttons |
| Old client cannot open upgraded vault | Update that client to this desktop build or use its preserved legacy copy |
| WebDAV save fails | Connectivity, canonical HTTPS address, ETags, permissions, concurrent updates |
| Mac launch rejected | Current release is development-signed, not notarized; see MACOS.md |

When reporting a problem, include OS/architecture, app version, action taken,
and the error wording. Do not include the QR payload, setup key, vault password,
authorization code, or an unredacted account QR screenshot.

## 11. Appearance, privacy, and starting over

Settings includes theme/design choices and a preference to hide OTPs. Use these
for display/privacy preferences; they do not change the token enrolled with the
provider. A hidden code is different from a missing account or an expired OCRA
response. Settings also contains logging controls and a log-opening action;
review logs before sharing them with a maintainer.

To stop remembering an unlock credential, disable **Use Touch ID/Windows Hello**.
To restart the application's setup, use the **Factory reset** action in Settings
and read its confirmation. In these desktop builds reset clears local app settings,
the current vault's saved unlock credential, WebDAV login settings, and the active
in-memory collection, then returns to Welcome. It does not delete your `.2fa`
files or change their passwords. You can reopen them afterward with their existing
passwords. Save a backup and record the file location before resetting so you
know which file to reopen. Reset is not password recovery and is not secure file
erasure. To build a separate collection after reset, choose Create new data file
with a different filename.


## Desktop workspace in 1.4.0

Accounts is the main workspace. Each card shows the account name, service,
verification code, and remaining seconds. Use **Copy code** to copy the current
code, **Edit** to change the account name/service/notes, and **View QR** to show
its supported provisioning QR. Treat an exported setup QR like the original
secret. Use the star to mark a favorite and **More → Delete account…** to remove
an account after confirmation. Supported OCRA accounts also show the challenge
action; follow the OCRA section for the server challenge and device restrictions.

The toolbar offers **Scan screen**, **Scan camera**, **Add manually**, **Add
OCRA**, and **Sync**. Screen scanning captures the selected window/display
locally and uses the same account-review flow as camera scanning. The search box
matches both account and service names without case sensitivity. Clear it to
restore the full list. The count distinguishes all accounts from search results.
With no accounts, the page explains how to add the first one; an unsuccessful
search displays a different message.

The left navigation shows labels in wide windows and collapses in smaller
windows. Use the menu button to reveal labels. **Lock vault** ends the current
unlocked session. **Settings** contains preferences and Data file management;
the Data file actions are grouped into location, protection, and other vaults,
with a description beneath each action. The separate **Datafile** navigation
entry retains the existing vault navigation workflow. Unlock and creation forms
use narrower panels for easier reading. The desktop palette follows the chosen
light/dark theme; high-contrast resources use system window/text colors.

If saving fails, the message now distinguishes a locked/expired session, a
missing file, denied access, changed authentication, and network or I/O failure.
Unlock again after session expiry. If another client changed a shared file,
reload before trying again and preserve recovery copies. These messages do not
remove the need to verify file access or resolve a WebDAV conflict.

Press **Enter/Return** in a single-line field to submit the unlock, new-vault, open-vault, or add-account form when its submit button is enabled. Multiline notes retain Enter for new lines, and autocomplete fields retain their suggestion behavior. Rename, password-change, MobileID import, and OCRA dialogs use their primary action as the default button.

## Accounts layout in 1.4.3

Use **Add account** in the top toolbar for camera/screen scanning, manual entry,
or OCRA. Search, Lock app, and Reload data file remain in the toolbar.
Account rows show the service, account name, code, copy button, and countdown.
Open the row’s **⋯** menu for Edit, QR code, Favorite, OCRA challenge, or Delete.
The navigation rail starts collapsed. At desktop widths of 900 logical pixels
or more, expanding it moves the content aside. Smaller windows use an overlay;
close it with the menu button or by clicking outside it.
