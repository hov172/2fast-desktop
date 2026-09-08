# 2fast 1.4.2 — App information and support links

Version **1.4.2**, build **142**. This release publishes the universal macOS app
for Apple Silicon and Intel, requiring macOS 15 or newer.

## Changes

- Expanded **About the app** with version and build values embedded from the
  build configuration, keeping the display in sync with the packaged app.
- Added operating system and app/system architecture details, plus
  **Copy app details** for support reports.
- Added links to the user guide, releases, issues, desktop source, upstream
  project, and GPL-3.0 license. Private project links require repository access.
- Preserved upstream attribution and identified the independent desktop port.

## Installation

Download `2fast-macos-universal.dmg`, open it, and drag **2fast** into
**Applications**. Quit an older running copy before replacing it. The alternative
ZIP contains the same app. Updating the app does not replace your vault.

Both the app and DMG use Developer ID Application signing and Apple notarization
with stapled tickets. `SHA256SUMS-v1.4.2-macos.txt` covers both downloads.

## Verification

The release checks include builds for both architectures, generated version
metadata, universal-launcher dispatch, all executable signing identities and
secure timestamps, Apple notarization acceptance, ticket validation, and
`syspolicy_check distribution` on the packaged app. The GitHub release notes
record the notarization submission IDs and any remaining verification limits.

Windows binaries are not included in this release; use the earlier Windows
release assets. Physical Intel and Touch ID acceptance remain untested for this
update. See [the macOS guide](../MACOS.md) and [user guide](USER-GUIDE.md).
