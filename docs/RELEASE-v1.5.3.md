# 2fast 1.5.3 — Code simplification and small defect fixes

Version **1.5.3**, build **153**, for macOS universal and Windows x64/ARM64.

Vaults, accounts, scanning, biometrics, WebDAV and the V4 format are unchanged.
Upgrading from 1.5.2 requires no vault migration.

## What changed

A behaviour-preserving simplification pass over the first-party code
(`Project2FA.Core`, `Project2FA.Shared`, the Uno desktop head and the test
suites) removed about 500 net lines: nested conditionals became early returns,
redundant boolean comparisons and LINQ chains were collapsed, dead handlers and
commented-out code were deleted, and unused usings were pruned. The vendored
libraries and the security-critical vault, crypto, serialization, secret-store
and WebDAV code were not touched.

Defects fixed along the way:

- The file-size converter could index past its unit table for sizes beyond
  yottabytes; it now clamps to the last unit.
- The multi-select list helper subscribed a new `SelectionChanged` handler on
  every binding change and dereferenced an unguarded cast; it now subscribes
  once and guards both casts.
- The data-file update dialog resolved the secret service from the container
  instead of using the injected instance.
- Backup import read the file and encoded the password once per switch arm;
  it now does each once.
- English UI text in the account list summary, the settings section title and
  the MobileID device-binding notice moved to resource keys, read through the
  desktop resource seam with English fallbacks. Translations for the new keys
  fall back to English until they are added.
- The vault model test suite looked up two types by their pre-rename names and
  threw before asserting anything; the names are now qualified and the suite
  runs green.

## Verification

- macOS: Uno desktop head builds clean in Release; `scripts/test-macos.sh`
  passes (parser, importer crypto, OCRA, device binding, file and WebDAV
  transactions, secret store, native Touch ID and Vision checks); the vault
  model suite passes against the built app.
- Universal bundle signed with Developer ID, Hardened Runtime and timestamps;
  notarized and stapled; Gatekeeper reports Notarized Developer ID.
- Windows: PE architecture checks for the app host, CLR and camera native
  library; privacy gate passed.
- Hardware acceptance limits from 1.5.1 still apply: Touch ID unlock with an
  enrolled finger and physical Intel Mac runs remain untested.

See the [user guide](USER-GUIDE.md), [macOS guide](../MACOS.md) and
[Windows guide](WINDOWS.md).
