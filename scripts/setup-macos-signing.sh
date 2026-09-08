#!/bin/bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root"
team="${1:?Usage: scripts/setup-macos-signing.sh APPLE_TEAM_ID}"
[[ "$team" =~ ^[A-Z0-9]{10}$ ]] || { echo 'Invalid Apple team ID.' >&2; exit 2; }
xcodegen generate --spec build/signing/project.yml
xcodebuild -project build/signing/MacOSSigning.xcodeproj -scheme MacOSSigning \
  -configuration Release -destination 'platform=macOS' -derivedDataPath build/signing-derived \
  -allowProvisioningUpdates "DEVELOPMENT_TEAM=$team" build
/usr/bin/python3 scripts/prepare-macos-signing.py
