# Desktop UI review — 8 September 2026

## Assessment and implemented changes

The earlier desktop screen needed a substantial layout revision. Large sparse
rows, duplicated commands, icon-only navigation, hidden desktop search, and
undifferentiated vault actions made routine work difficult to discover. The
1.4.0 revision introduces shared theme resources, bounded content widths,
consistent spacing and button sizes, adaptive navigation, readable account
cards, search by name or issuer, distinct empty/search states, and grouped vault
actions. It retains the Uno shared desktop implementation.

The navigation uses the existing NavigationView and its supported adaptive
modes; see Microsoft's [NavigationView guidance](https://learn.microsoft.com/en-us/windows/apps/design/controls/navigationview).
This is a desktop usability improvement, not a claim of completed accessibility
certification or native Windows hardware acceptance.

## Runtime failure found and fixed

The collection contained three accounts while the list was empty. Assigning the
source explicitly exposed an ArgumentException from Uno Selector: SelectedIndex
could not be set to an invalid value with items present. Automatic current-item
synchronization with AdvancedCollectionView caused the assignment to fail; the
binding engine had hidden that failure. The account ListView now sets
IsSynchronizedWithCurrentItem=False and attaches its source and top-level
commands when navigation assigns the view model. Collection change observation
updates the count and empty state and is detached on unload.

Container styling retains its base control template. Both account templates
retain model bindings for code, label, issuer, countdown, and actions. A source
regression check requires synchronization to stay disabled; compiled checks
cover generated card templates and workflow bindings.

## Save diagnostics and preview isolation

Save failures now distinguish expired sessions, missing paths, access errors,
authentication changes, network errors, and I/O conflicts. Desktop diagnostics
record sanitized exception types rather than raw exception text. Existing atomic
write and recovery behavior remains in place.

An early synthetic preview accidentally invoked autosave without an unlocked
session and showed an Unable to save dialog. Preview setup now detaches autosave,
and preview builds have a compile-time write guard. They contain sample accounts
without provisioning secrets and disable interaction. The preview code is
compiled only with TwoFastUiPreview=true; production tests require it to be
absent. No before/after hash of the user's vault was captured, so this review
does not claim independent proof that their file was unchanged.

## Visual verification

A running macOS synthetic preview rendered all three items. Window captures
confirmed names, codes, countdown indicators, and actions in dark and light
modes and a 720-point-wide window with collapsed navigation. Synthetic countdown
values are fixed; this check verifies rendering, not live OTP timing. The
preview deliberately disables account interaction and must not be distributed
as the normal app. Screenshots and diagnostic traces remain local build output.

## Remaining acceptance limits

Physical Intel hardware, native Windows UI/camera/screen capture/Hello, enrolled
Touch ID success, screen-reader behavior, and high-contrast runtime appearance
require further hardware/manual acceptance. Managed checks and architecture
packaging checks do not replace those checks. There is no claim that every
possible workflow or external WebDAV server has been exhaustively tested.

## Keyboard review

Unlock and account/new-vault forms lacked Enter submission. The open-vault
handler also bypassed the button command and did not mark the event handled.
A shared form handler now observes handled key events, excludes multiline and
autocomplete controls, respects enabled state and CanExecute, ignores repeated
key-down events, and executes the same command as the form button. Relevant
rename/password/OCRA/import dialogs have a primary default action.

In-process checks in an isolated running macOS window passed for password and
single-line submission, multiline/autocomplete exclusion, disabled buttons, and
unavailable commands. macOS denied assistive keyboard automation, so physical
Return-key delivery was not verified by automation. The preview fixture and its
sample command are excluded from release binaries.

## Release validation

The production universal app opened to its normal password screen. macOS ARM64
and Intel compiled workflow/encryption checks passed (Intel execution through
Rosetta), as did managed checks against both Windows builds with Mac native
libraries supplied to the test host. Checks include account routes, seven vault
actions, editable dialogs, code/countdown bindings, generated card templates,
QR PNG output, concurrent crypto operations, and authenticated vault migrations.
Universal launcher dispatch, arguments, exit status, and strict code-signature
verification passed. Native tests covered Keychain operations, unenrolled
biometry refusal, synthetic QR decoding, and screen-preview delivery. These
checks do not exercise native Windows capture or prove successful enrolled
biometric unlock. Build warnings are retained in local build logs.
