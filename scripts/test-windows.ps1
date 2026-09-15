param([switch]$Hello)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

function Invoke-Suite($Project, $FailureMessage, $ExtraArgs = @()) {
    if ($ExtraArgs.Count) { dotnet run --project $Project -c Release -- @ExtraArgs }
    else { dotnet run --project $Project -c Release }
    if ($LASTEXITCODE -ne 0) { throw $FailureMessage }
}

Push-Location $root
try {
    Invoke-Suite 'tests/Desktop/SharedProjectTests/SharedProjectTests.csproj' 'Shared project manifest is out of sync.'
    Invoke-Suite 'tests/Desktop/ImporterCryptoTests/ImporterCryptoTests.csproj' 'Backup importer crypto tests failed.'
    Invoke-Suite 'tests/Desktop/QrFrameTests/QrFrameTests.csproj' 'QR frame tests failed.'
    Invoke-Suite 'tests/MacOS/ParserTests.csproj' 'Shared QR parser tests failed.'
    Invoke-Suite 'tests/MacOS/OcraTests/OcraTests.csproj' 'OCRA tests failed.'
    $nativeArgs = @()
    if ($Hello) { $nativeArgs += '--hello' }
    Invoke-Suite 'tests/Desktop/WindowsNativeTests/WindowsNativeTests.csproj' 'Windows native tests failed.' $nativeArgs
} finally { Pop-Location }
