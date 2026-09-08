# 2fast 1.4.0

Desktop layout and account-list reliability update for macOS Universal (Apple
Silicon and Intel), Windows x64, and Windows ARM64. Display/assembly version
1.4.0, package manifest 1.4.0.0, build 140. App identifiers remain unchanged.

## Changes

- Adaptive labeled navigation, light/dark palettes, readable account cards,
  visible copy/edit/QR commands, and name/service search.
- Direct screen/camera scanning commands and clearer empty/search states.
- Grouped Data file actions and more focused unlock/account/vault forms.
- Fixed the Uno selection synchronization exception that left a populated
  account list blank. Reattach page commands and source with the view model.
- More specific, sanitized save-error messages and an expired-session guard.

Quit 2fast before replacing the app. Extract Windows builds to a fresh folder.
Back up the vault before updating. V4 remains compatible between updated Mac
and Windows clients; older Windows/mobile clients cannot read upgraded files.
Encryption upgrade remains optional. See [the user guide](USER-GUIDE.md) for all workflows and
[the UI review](audits/2026-09-08-desktop-ui.md) for verification limits.

## Distribution limits

The Mac app uses Apple Development signing, not Developer ID signing or
notarization. Gatekeeper may reject it on another Mac. Windows packages are
unsigned. Native Windows acceptance, physical Intel Mac acceptance, and
successful Touch ID with an enrolled fingerprint remain outstanding. A stable
release label does not remove these limits.

Press **Enter/Return** in a single-line field to submit the unlock, new-vault, open-vault, or add-account form when its submit button is enabled. Multiline notes retain Enter for new lines, and autocomplete fields retain their suggestion behavior. Rename, password-change, MobileID import, and OCRA dialogs use their primary action as the default button.
