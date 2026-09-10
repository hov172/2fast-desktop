# UX flows

The journeys the app actually supports, in the order a user meets them. Use this
to decide where a change belongs before touching a view. Screen structure and
tokens are in [design.md](design.md); motion and state behaviour in
[interactions.md](interactions.md).

## Shell

Every page is hosted in `ShellPage`, which owns a `NavigationView`
(`PaneDisplayMode="LeftCompact"`, `CompactPaneLength="56"`, expanding to `Left`
at `MinWindowWidth="900"`) on desktop, and a `TabBar` on mobile
(`ViewModel.IsMobile`). Menu items:

| Item | Target |
| --- | --- |
| Accounts | `/AccountCodePage` |
| Data file | `SettingPage?PivotItem=1` |
| About (footer) | `SettingPage?PivotItem=2` |

`ViewModel.NavigationIsAllowed` gates every item. It is false while the vault is
locked, which is how the lock is enforced at the navigation layer — do not add a
navigation path that bypasses it.

## 1. First run

```
WelcomePage
  ├─ Create a new data file  → NewDataFilePage  → (vault created) → AccountCodePage
  └─ Use an existing file    → UseDataFilePage  → local file or WebDAV → LoginPage
```

`TutorialPage` is shown instead when the app decides the user has not seen the
introduction.

## 2. Unlock

```
app start
  ├─ no vault configured        → WelcomePage
  ├─ vault present              → /LoginPage
  ├─ launched with a .2fa file  → /FileActivationPage → /AccountCodePage
  └─ tutorial pending           → /TutorialPage
```

`LoginPage` accepts the vault password, or biometrics when enrolled — Touch ID on
macOS, Windows Hello on Windows, via `IBiometryService`. On success it resets the
stack to `/AccountCodePage`. Failures clear the password field and show an
inline error; the vault stays locked and `NavigationIsAllowed` stays false.

Locking (idle, explicit, or session cancellation) cancels in-flight operations,
clears the session secret, and drops decrypted accounts from memory.

## 3. Accounts — the main screen

`AccountCodePage` lists the vault entries. Per entry: issuer/label, avatar or
initials, the current code, a countdown to the next period, and copy / edit /
show-QR actions. Search filters by account name or service. Categories and
favourites filter the list; changes are broadcast with `CategoriesChangedMessage`
and `FilteringChangedMessage`.

Codes are hidden by default when *Prefer hidden TOTP* is on, revealed on demand.

## 4. Adding an account

All four entry points converge on the same review step —
`AddAccountPage` on desktop, `AddAccountContentDialog` on Windows/UWP,
a full-screen page on mobile — backed by `AddAccountViewModelBase`:

```
Scan screen   (desktop: pick a window or display, decode in place)  ┐
Camera        (CameraPage / live capture)                           ├→ parse → review → save
Manual entry  (issuer, account, secret, algorithm, digits, period)  │
OCRA / Deepnet MobileID setup                                       ┘
```

Parsing is strict and rejects malformed payloads with a single message:
*"Invalid or unsupported authenticator QR. Expected a TOTP, OCRA, or Deepnet
MobileID setup code."* MobileID additionally prompts for an authorization code.

Scan-screen exists because the QR is usually on the *same* computer; no camera or
second device is required.

## 5. Editing and removing

Edit opens `EditAccountContentDialog` (desktop/UWP) or `EditAccountPage`
(mobile). Show-QR opens `DisplayQRCodeContentDialog`, which renders the entry
back to a QR for transferring to another device. Saves go through `DataService`
and are committed to the vault before the dialog closes.

## 6. Settings

`SettingPage` is pivot-based; the shell deep-links into pivots by index.

| Pivot | Contains |
| --- | --- |
| 0 — General | theme mode, corner radius, custom design, Pride design, prefer hidden TOTP, Windows Hello / biometric preference, factory reset, logging mode |
| 1 — Data file | vault location, WebDAV, change password, upgrade vault encryption, backup |
| 2 — About | version, credits, licences, dependencies |

**Upgrade vault encryption** is the one destructive-adjacent action: it rewrites
the vault to V4. Both desktop apps must be updated first; older Windows releases
and mobile clients cannot read the result. Keep its warning copy intact.

## 7. Import and backup

`ImportBackupPageViewModel` / `ImportBackupContentDialogViewModel` behind
`IBackupImporterService`, with four formats: Aegis, andOTP, 2FAS, and 2fast.
Imported files are untrusted input — validate before merging into the vault.

## 8. WebDAV / shared vaults

`UseDataFilePage` and `WebDAVAuthContentDialog` configure a remote vault
(Nextcloud login flow v2 is supported). Connection state is broadcast with
`WebDAVStatusChangedMessage` and surfaced in the shell. HTTPS is mandatory.
Remote writes go through the remote vault transaction path so an interrupted
sync cannot leave a partial vault.
