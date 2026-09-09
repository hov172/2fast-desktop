# 2fast 1.4.5 — Privacy and transport fixes

Version **1.4.5**, build **145**, for macOS universal and Windows x64/ARM64.

- WebDAV setup, server checks and authenticated clients require HTTPS. Login
  responses must use the configured HTTPS origin; token requests reject redirects.
  Replace an existing HTTP server address with its canonical HTTPS address.
- Diagnostic logging records timestamps, exception types and numeric error codes.
  Raw messages, stack traces and freeform event text are omitted in all modes.
  The app removes its old unredacted log on its next diagnostic write.
- Release builds remove debug symbols and map compiler paths. Packaging scans the
  final assemblies and standalone executables for maintainer workstation paths.
- Mac packaging requires Developer ID distribution profiles without registered
  devices. The certificate holder and team remain visible with maintainer consent.
- Prior maintainer author/committer/tagger metadata uses a GitHub noreply identity.
  Source trees and upstream attribution are preserved. Re-clone after this rewrite.

Application downloads and their checksums from older releases are withdrawn for
privacy cleanup. Historical source tags and documentation remain. Old clones,
previous downloads and cached GitHub objects cannot be recalled by this release.

Mac app and DMG require separate accepted notarization results and stapled tickets
before publication. Windows packages are unsigned; native Windows hardware
execution was not performed on the Mac build host.

See the [user guide](USER-GUIDE.md), [macOS guide](../MACOS.md),
[Windows guide](WINDOWS.md) and [privacy verification](audits/2026-09-08-privacy-remediation.md).
