#!/bin/bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root"
dotnet run --project tests/Desktop/SharedProjectTests/SharedProjectTests.csproj -c Release
dotnet run --project tests/Desktop/ImporterCryptoTests/ImporterCryptoTests.csproj -c Release
dotnet run --project tests/MacOS/ParserTests.csproj -c Release
dotnet run --project tests/MacOS/AccountCommitTests/AccountCommitTests.csproj -c Release
dotnet run --project tests/MacOS/IconTests/IconTests.csproj -c Release
dotnet run --project tests/MacOS/DeviceBindingTests/DeviceBindingTests.csproj -c Release
dotnet run --project tests/MacOS/OcraTests/OcraTests.csproj -c Release
dotnet run --project tests/MacOS/NewDataFileTests/NewDataFileTests.csproj -c Release
dotnet run --project tests/MacOS/RemoteVaultTests/RemoteVaultTests.csproj -c Release
dotnet run --project tests/MacOS/SecretStoreTests/SecretStoreTests.csproj -c Release
test_bundle="$repo_root/tests/MacOS/bin/NativeTests.app"
mkdir -p "$test_bundle/Contents/MacOS"
xcrun clang -fobjc-arc -fblocks -Wall -Wextra -Werror -Wno-unused-parameter \
  -mmacosx-version-min=15.0 -framework AppKit -framework AVFoundation \
  -framework LocalAuthentication -framework Security -framework Vision \
  -framework CoreMedia -framework QuartzCore -framework CoreImage -framework ScreenCaptureKit \
  tests/MacOS/NativeTests.m -o "$test_bundle/Contents/MacOS/NativeTests"
cp src/Project2FA.Uno/Platforms/Desktop/Native/Info.plist "$test_bundle/Contents/Info.plist"
/usr/libexec/PlistBuddy -c 'Add :CFBundleIdentifier string jpweber.it.Project2FA.Uno.NativeTests' "$test_bundle/Contents/Info.plist"
/usr/libexec/PlistBuddy -c 'Add :CFBundleExecutable string NativeTests' "$test_bundle/Contents/Info.plist"
/usr/libexec/PlistBuddy -c 'Add :CFBundlePackageType string APPL' "$test_bundle/Contents/Info.plist"
/usr/libexec/PlistBuddy -c 'Add :CFBundleName string 2fast Native Tests' "$test_bundle/Contents/Info.plist"
xattr -cr "$test_bundle"
cp build/macos-signing/embedded.provisionprofile "$test_bundle/Contents/embedded.provisionprofile"
cp build/macos-signing/Entitlements.plist tests/MacOS/bin/TestEntitlements.plist
/usr/bin/python3 - <<'PY'
import pathlib, plistlib
p = pathlib.Path('tests/MacOS/bin/TestEntitlements.plist')
claims = plistlib.loads(p.read_bytes())
claims['com.apple.application-identifier'] += '.NativeTests'
claims['keychain-access-groups'] = [claims['com.apple.application-identifier']]
p.write_bytes(plistlib.dumps(claims))
PY
identity="$(/usr/bin/python3 -c 'import xml.etree.ElementTree as E; print(E.parse("build/macos-signing/Signing.props").findtext("PropertyGroup/CodesignKey"))')"
codesign --force --sign "$identity" --entitlements tests/MacOS/bin/TestEntitlements.plist "$test_bundle"
"$test_bundle/Contents/MacOS/NativeTests" "${1:-}"
