#!/bin/bash
set -euo pipefail

repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root"
runtime="${1:-universal}"
case "$runtime" in
  universal)
    "$0" osx-arm64
    "$0" osx-x64
    /usr/bin/python3 "$repo_root/scripts/package-macos-universal.py"
    exit 0 ;;
  osx-arm64|osx-x64) ;;
  *) echo "Usage: $0 [universal|osx-arm64|osx-x64]" >&2; exit 2 ;;
esac

for dependency in BiometryService Otp.NET UNOversalTemplate ZXing.Net.Uno; do
  if [[ -z "$(find "$dependency" \( -name '*.csproj' -o -name '*.projitems' \) -print -quit)" ]]; then
    echo "Missing $dependency. Restore the pinned submodules with git submodule update --init --recursive." >&2
    exit 1
  fi
done

dotnet publish src/Project2FA.Uno/Project2FA.Uno.csproj \
  -c Release -f net10.0-desktop -r "$runtime" \
  "-p:DirectoryBuildTargetsPath=$repo_root/build/MacOS.targets" \
  -p:SelfContained=true -p:UseMonoRuntime=false -p:PackageFormat=app

echo "Built: $repo_root/src/Project2FA.Uno/bin/Release/net10.0-desktop/$runtime/publish/2fast.app"
