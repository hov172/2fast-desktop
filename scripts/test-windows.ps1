param([switch]$Hello)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    dotnet run --project tests/Desktop/SharedProjectTests/SharedProjectTests.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Shared project manifest is out of sync.' }
    dotnet run --project tests/Desktop/ImporterCryptoTests/ImporterCryptoTests.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Backup importer crypto tests failed.' }
    dotnet run --project tests/Desktop/QrFrameTests/QrFrameTests.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'QR frame tests failed.' }
    dotnet run --project tests/MacOS/ParserTests.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Shared QR parser tests failed.' }
    dotnet run --project tests/MacOS/OcraTests/OcraTests.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'OCRA tests failed.' }
    $nativeArgs = @()
    if ($Hello) { $nativeArgs += '--hello' }
    dotnet run --project tests/Desktop/WindowsNativeTests/WindowsNativeTests.csproj -c Release -- @nativeArgs
    if ($LASTEXITCODE -ne 0) { throw 'Windows native tests failed.' }
} finally { Pop-Location }
