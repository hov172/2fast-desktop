# Classic accounts layout verification — 1.4.3

The shared Uno desktop UI restores the compact rail, top account toolbar, flat
account rows, cyan codes, copy controls, and countdown rings. Existing commands
remain connected through the toolbar, row flyouts, and navigation.

## Sidebar regression

The reported failure was an expanded navigation overlay obscuring account rows
and dimming the desktop page. At window widths of at least 900 logical pixels,
NavigationView now uses Left mode. Smaller widths use LeftCompact. The initial
IsPaneOpen value is false; visual states no longer override the toggle state.

A developer-only preview uses synthetic accounts and does not initialize or
save a real vault. With TWOFAST_UI_WIDTH=1952 and TWOFAST_UI_PANE_OPEN=1 on the
2x-scale Mac display, the runtime check reported:

- Pane open: true; display mode: Expanded.
- Open pane width: 208 logical pixels.
- Account page offset: X=209 logical pixels.

This confirms the expanded pane reserves its width instead of covering content.
The preview is excluded from production builds. Earlier layout checks covered
wide dark and narrow light layouts, account templates, and command bindings.

## Release checks and limits

Build and packaging logs are retained locally. Release acceptance requires both
Mac builds, Windows x64/ARM64 portable and standalone builds, embedded version
1.4.3/build 143, account/vault contract checks, matching native architectures,
and universal launcher dispatch. macOS additionally requires Developer ID signing,
Apple acceptance for both app and DMG, stapled tickets, and distribution checks.

Windows native execution, physical Intel Mac, camera hardware, Windows Hello,
and Touch ID acceptance are not established by this build-host verification.
