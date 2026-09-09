param([ValidateSet('win-x64','win-arm64')][string]$Runtime = 'win-x64', [switch]$SingleFile)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    $arch = $Runtime.Substring(4)
    if ($SingleFile) {
        # Fully self-contained single .exe: runtime, native libraries and all
        # content assets (icons, JSONs) are embedded and self-extracted.
        dotnet publish src/Project2FA.Uno/Project2FA.Uno.csproj -c Release -f net10.0-desktop -r $Runtime "-p:DirectoryBuildTargetsPath=$root/build/Desktop.targets" -p:SelfContained=true -p:UseMonoRuntime=false -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -o "dist/windows-$arch-singlefile"
        if ($LASTEXITCODE -ne 0) { throw 'Windows single-file build failed.' }
        python scripts/verify-release-privacy.py "dist/windows-$arch-singlefile/Project2FA.Uno.exe"
        if ($LASTEXITCODE -ne 0) { throw 'Windows release privacy verification failed.' }
        Copy-Item "dist/windows-$arch-singlefile/Project2FA.Uno.exe" "dist/2fast-windows-$arch.exe" -Force
        return
    }
    dotnet publish src/Project2FA.Uno/Project2FA.Uno.csproj -c Release -f net10.0-desktop -r $Runtime "-p:DirectoryBuildTargetsPath=$root/build/Desktop.targets" -p:SelfContained=true -p:UseMonoRuntime=false -o "dist/windows-$arch"
    if ($LASTEXITCODE -ne 0) { throw 'Windows build failed.' }
    Get-ChildItem "dist/windows-$arch/opencv_videoio_ffmpeg*.dll" | Remove-Item
    Copy-Item LICENSE, docs/licenses/OpenCvSharp-LICENSE "dist/windows-$arch"
    Copy-Item docs/WINDOWS.md, docs/DESKTOP-COMPATIBILITY.md "dist/windows-$arch"
    python scripts/verify-release-privacy.py "dist/windows-$arch"
    if ($LASTEXITCODE -ne 0) { throw 'Windows release privacy verification failed.' }
    Compress-Archive -Path "dist/windows-$arch/*" -DestinationPath "dist/2fast-windows-$arch.zip" -Force
} finally { Pop-Location }
