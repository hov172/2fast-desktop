#!/bin/bash
# Build the release DMG from the stapled dist/2fast.app with a drag-to-install
# Applications link, then sign it. Notarize and staple the DMG afterwards.
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root/dist"
: "${APPLE_DEVELOPER_ID:?Set APPLE_DEVELOPER_ID (certificate SHA-1).}"
xcrun stapler validate 2fast.app
stage="$(mktemp -d "$repo_root/build/dmg-XXXXXX")"
trap 'rm -rf "$stage"' EXIT
ditto 2fast.app "$stage/2fast.app"
ln -s /Applications "$stage/Applications"
rm -f 2fast-macos-universal.dmg
hdiutil create -volname 2fast -srcfolder "$stage" -ov -format UDZO 2fast-macos-universal.dmg
codesign --timestamp -s "$APPLE_DEVELOPER_ID" 2fast-macos-universal.dmg
echo "Built: $repo_root/dist/2fast-macos-universal.dmg"
