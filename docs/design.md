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
- `TranslucentBackground{High,Medium,Low}Brush` exist in light and dark at 0.9 /
  0.75 / 0.55 opacity for overlay surfaces; high contrast defines only High and
  Medium.

## Typography and iconography

- Text styles come from WinUI defaults; `ShellHeaderTitleTextBlockStyle` is the
  shell header.
- Icon fonts ship in `Project2FA.Shared/Assets/Fonts/`:
  `FluentSystemIcons-Regular.ttf`, `FluentSystemIcons-Filled.ttf`,
  `SimpleIcons.ttf` — referenced via the `SimpleIcons`, `SegoeFluentIconsFilled`
  and `SegoeFullFluentIcons` font-family resources in `Styles.xaml`. The
  `SegoeFluentIcons` resource points at `Segoe Fluent Icons.ttf`, which is not
  shipped. Use `FullFluentFontIconStyle` for `FontIcon`.
- Service logos for accounts resolve through
  `FontIconNameToGlyphConverter` / `FontIconUnicodeIndexToGlyphConverter` against
  the identification data in `Assets/JSONs`. Add a service there, not in a view.

## Layout

- Shell breakpoint: `MinWindowWidth="900"` switches the `NavigationView` from
  `LeftCompact` (`CompactPaneLength="56"`) to `Left`. Below that the pane is
  compact; on mobile the shell swaps to a `TabBar`.
- Round corners is a user toggle (`UseRoundCorner`, Settings → General → Round
  corners), so bind radius rather than fixing it.
- `CardActionControlMinWidth` (248) sizes the action controls on the Settings
  and Add-account pages. Account rows keep copy and show/hide visible; edit, QR,
  favourite, OCRA challenge and delete live in the row's ⋯ menu — a deliberate
  1.4.3 change. Do not hide the visible actions behind a hover-only affordance.

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
