#!/bin/bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root"
case "${1:-all}" in
  all) runtimes=(win-x64 win-arm64) ;;
  win-x64|win-arm64) runtimes=("$1") ;;
  *) echo "Usage: $0 [all|win-x64|win-arm64]" >&2; exit 2 ;;
esac
for runtime in "${runtimes[@]}"; do
  arch="${runtime#win-}"
  dotnet publish src/Project2FA.Uno/Project2FA.Uno.csproj -c Release -f net10.0-desktop -r "$runtime" \
    "-p:DirectoryBuildTargetsPath=$repo_root/build/Desktop.targets" -p:EnableWindowsTargeting=true \
    -p:SelfContained=true -p:UseMonoRuntime=false -o "$repo_root/dist/windows-$arch"
done
python3 scripts/package-windows.py
