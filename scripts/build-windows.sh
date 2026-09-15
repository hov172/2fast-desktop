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
  # Restore with the runtime explicitly selected so RID-specific native assets
  # (OpenCvSharp and the Uno desktop host) are present in project.assets.json.
  # Restore in Release: the Uno SDK adds its Hot Design / MCP dev tooling to a
  # Debug (non-Optimize) graph, and a --no-restore Release publish would ship it.
  dotnet restore src/Project2FA.Uno/Project2FA.Uno.csproj -p:Configuration=Release \
    "-p:RuntimeIdentifier=$runtime" "-p:DirectoryBuildTargetsPath=$repo_root/build/Desktop.targets" \
    -p:EnableWindowsTargeting=true
  dotnet publish src/Project2FA.Uno/Project2FA.Uno.csproj -c Release -f net10.0-desktop -r "$runtime" --no-restore \
    "-p:RuntimeIdentifier=$runtime" "-p:PathMap=$repo_root=/_/src" -p:UnoGenerateHotReloadInfo=false \
    "-p:DirectoryBuildTargetsPath=$repo_root/build/Desktop.targets" -p:EnableWindowsTargeting=true \
    -p:SelfContained=true -p:UseMonoRuntime=false -o "$repo_root/dist/windows-$arch"
  # Standalone single .exe, matching build-windows.ps1 -SingleFile: runtime,
  # native libraries and content assets are embedded and self-extracted.
  dotnet publish src/Project2FA.Uno/Project2FA.Uno.csproj -c Release -f net10.0-desktop -r "$runtime" --no-restore \
    "-p:RuntimeIdentifier=$runtime" "-p:PathMap=$repo_root=/_/src" -p:UnoGenerateHotReloadInfo=false \
    "-p:DirectoryBuildTargetsPath=$repo_root/build/Desktop.targets" -p:EnableWindowsTargeting=true \
    -p:SelfContained=true -p:UseMonoRuntime=false -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true \
    -o "$repo_root/dist/windows-$arch-singlefile"
done
/usr/bin/python3 scripts/package-windows.py
