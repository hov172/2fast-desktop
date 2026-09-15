# 2fast 1.5.4 — Second simplification sweep

Version **1.5.4**, build **154**, for macOS universal and Windows x64/ARM64.

Vaults, accounts, scanning, biometrics, WebDAV and the V4 format are unchanged.
Upgrading from 1.5.3 requires no vault migration.

## What changed

A second behaviour-preserving simplification pass over the first-party code
removed about 380 net lines:

- The Uno head's `App.xaml.cs` lost a commented-out iOS file-open path, the
  loader only it called, its unused field and several stale TODO blocks. An
  empty protocol-activation branch became a single negated check.
- `DataService` lost three commented-out fields, a disabled file-change
  monitoring block and an unreferenced commented-out handler. The icon lookup
  for a label uses early returns and direct LINQ predicates.
- The vendored Markdown control lost a 90-line commented-out duplicate image
  loader and two commented-out alternate implementations. The import resolver
  no longer splits a resource id it never used, which also removes a latent
  crash on a malformed id.
- Nine content dialogs dropped `using` directives that duplicate the generated
  global usings or were unused.
- `scripts/test-windows.ps1` runs each suite through one helper instead of six
  copied blocks; `scripts/package-windows.py` checks PE architecture through
  one helper instead of two inlined copies.
- `DesktopFileTransaction` assigns the Unix create mode inside an explicit
  platform guard so the CA1416 analyzer warning no longer fires.
- `scripts/test-macos.sh` now runs the vault model suite against the built
  desktop head; previously that suite was never wired in.

The security-critical vault, crypto, serialization, secret-store, device
binding and WebDAV code was not touched, nor was the known parser and WebDAV
redundancy that CLAUDE.md reserves for its own change.

## Verification

- macOS: Uno desktop head builds clean in Release; `scripts/test-macos.sh`
  passes (parser, importer crypto, OCRA, device binding, file and WebDAV
  transactions, secret store, vault model suite against the built app, native
  Touch ID and Vision checks) with no analyzer warnings.
- Universal bundle signed with Developer ID, Hardened Runtime and timestamps;
  notarized and stapled; Gatekeeper reports Notarized Developer ID.
- Windows: PE architecture checks for the app host, CLR and camera native
  library; privacy gate passed.
- Hardware acceptance limits from 1.5.1 still apply: Touch ID unlock with an
  enrolled finger and physical Intel Mac runs remain untested.

See the [user guide](USER-GUIDE.md), [macOS guide](../MACOS.md) and
[Windows guide](WINDOWS.md).

## Post-tag documentation refresh (2026-09-15)

After tagging, a documentation audit corrected the guides against the
released code: the user guide had still described release 1.4.0, and the UX
flows, design, interactions, architecture, plan, compatibility and release
process documents carried stale counts and claims. The corrected guides were
committed on `main` after the tag, and the documentation ZIP and both Windows
portable ZIPs attached to this release were regenerated with them. The
application binaries inside the Windows ZIPs, the standalone EXEs and the macOS
DMG and ZIP are unchanged. `SHA256SUMS` and the documentation checksum file were
regenerated, so the archive checksums for the three refreshed ZIPs differ from
the originals published earlier the same day.
