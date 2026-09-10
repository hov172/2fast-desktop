# Design system

Where the visual vocabulary is defined and which constraints hold. Never
hard-code a colour, font family or icon glyph in a view — reference the resource.

## Resource files

| File | Role |
| --- | --- |
| [src/Project2FA.Uno/Themes/AppColors.xaml](../src/Project2FA.Uno/Themes/AppColors.xaml) | theme dictionary root: `Light`, `Default` (dark), high contrast; sets the accent |
| `Themes/ColorsLight.xaml`, `ColorsDark.xaml`, `ColorsHighContrast.xaml` | per-theme brushes |
| `Styles/ColorPaletteOverride.json` → `ColorPaletteOverride.xaml` | Material-style token set (`PrimaryColor`, `OnPrimaryColor`, `…ContainerColor`, `ErrorColor`, …). **Generated — edit the JSON, not the XAML.** |
| `Styles/Styles.xaml` | shared styles and font-family resources |
| `Styles/Generic/Button.xaml`, `PersonPicture.xaml`, `SettingsCard.xaml` | control-level styles |
| `Styles/Desktop.xaml` | desktop-only overrides |
| `Themes/Generic.xaml` | default templates for custom controls |

## Colour

- Accent is `#FF009BC1` (`ColorPaletteResources Accent`), with explicit
  `SystemAccentColorLight1-3` / `Dark1-3` ramps overridden per theme rather than
  left to system generation.
- Three themes must stay working: light, dark, high contrast. A colour defined in
  only one theme dictionary is a bug — add it to all three.
- Semantic tokens from `ColorPaletteOverride` (`PrimaryColor`,
  `PrimaryContainerColor`, `ErrorColor`, and their `On…` foregrounds) are the
  preferred reference. Fall back to WinUI system brushes
  (`ControlFillColorTertiaryBrush`, `SystemChromeAltHighColor`, …) for control
  chrome.
- `TranslucentBackground{High,Medium,Low}Brush` exist per theme at 0.9 / 0.75 /
  0.55 opacity for overlay surfaces.

## Typography and iconography

- Text styles come from WinUI defaults; `ShellHeaderTitleTextBlockStyle` is the
  shell header.
- Icon fonts ship in `Project2FA.Shared/Assets/Fonts/`:
  `FluentSystemIcons-Regular.ttf`, `FluentSystemIcons-Filled.ttf`,
  `SimpleIcons.ttf` — referenced via the `SimpleIcons`, `SegoeFluentIcons`,
  `SegoeFluentIconsFilled` and `SegoeFullFluentIcons` font-family resources in
  `Styles.xaml`. Use `FullFluentFontIconStyle` for `FontIcon`.
- Service logos for accounts resolve through
  `FontIconNameToGlyphConverter` / `FontIconUnicodeIndexToGlyphConverter` against
  the identification data in `Assets/JSONs`. Add a service there, not in a view.

## Layout

- Shell breakpoint: `MinWindowWidth="900"` switches the `NavigationView` from
  `LeftCompact` (`CompactPaneLength="56"`) to `Left`. Below that the pane is
  compact; on mobile the shell swaps to a `TabBar`.
- Corner radius is user-configurable (Settings → General → corner radius), so
  bind radius rather than fixing it.
- `CardActionControlMinWidth` keeps account-card actions (copy, edit, QR) at a
  usable size. Those actions must remain visible on the card, not hidden behind a
  hover-only affordance — this was a deliberate 1.4.3 change.

## Controls to reuse

Before writing a control, check these:

`Project2FA.Shared/Controls/` — `AutoCloseTeachingTip`, `ImageEx` (async image
with placeholder), `ProgressRing` + `RingShape`, `RadialProgressBar`,
`SettingsGroup` (with automation peer), `Shimmer` (skeleton).
`src/Project2FA.Uno/Controls/` — desktop `RadialProgressBar`, `Shimmer`.
`src/UnoLibrary.Controls/` — `MarkdownTextBlock` and its renderers.

## Accessibility

- `SettingsGroup` ships an automation peer; keep automation names on new
  composite controls.
- High contrast is a first-class theme, not an afterthought.
- Settings rows are declared with `x:Uid` and localized headers
  (`~SettingsAppThemeMode.Header` style). Every user-visible string is a resource
  key in `Project2FA.Shared/Strings/<lang>/`; `en` is the source of truth.
