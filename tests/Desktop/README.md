# Isolated macOS desktop preview

Build with `TwoFastUiPreview=true scripts/build-macos.sh osx-arm64`, then open
`src/Project2FA.Uno/bin/Release/net10.0-desktop/osx-arm64/publish/2fast.app`.
The title identifies the read-only design preview. It contains three synthetic
accounts with fixed display codes and no provisioning seeds. It does not open a
vault. Autosave is detached, writes are compile-time disabled, and interactions
are disabled. Never distribute this preview as the production application.

Pass `--env TWOFAST_UI_THEME=light` to `open -n <app>` for light appearance.
Pass `--env TWOFAST_UI_KEYBOARD=1` instead to show the isolated keyboard fixture.
It checks form submission in process with a sample command, including multiline,
autocomplete, disabled-button and CanExecute exclusions. Its result is written
to `/private/tmp/twofast-keyboard-result.txt`. Account preview geometry is written
to `/private/tmp/twofast-ui-preview-tree.log`; these local files are not release
assets. Native keyboard automation additionally requires macOS Accessibility
permission and is a separate acceptance check.

After previewing, rebuild normally with `scripts/build-macos.sh universal`.
The production compiled workflow checks require the preview type to be absent.
