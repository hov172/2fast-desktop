#!/usr/bin/python3
"""Generate local signing inputs from Xcode's authorized signing helper build."""
import hashlib
import datetime
import pathlib
import plistlib
import shutil
import subprocess
import xml.etree.ElementTree as ET

root = pathlib.Path(__file__).resolve().parent.parent
derived = root / 'build/signing-derived'
profile = derived / 'Build/Products/Release/MacOSSigning.app/Contents/embedded.provisionprofile'
claims = derived / 'Build/Intermediates.noindex/MacOSSigning.build/Release/MacOSSigning.build/MacOSSigning.app.xcent'
if not profile.exists() or not claims.exists():
    raise SystemExit('Run scripts/setup-macos-signing.sh YOUR_TEAM_ID first.')
decoded = plistlib.loads(subprocess.check_output(['security', 'cms', '-D', '-i', str(profile)]))
if decoded['ExpirationDate'] <= datetime.datetime.utcnow():
    raise SystemExit('The signing profile has expired. Run scripts/setup-macos-signing.sh YOUR_TEAM_ID again.')
identities = subprocess.check_output(['security', 'find-identity', '-v', '-p', 'codesigning'], text=True)
identity = next((hashlib.sha1(cert).hexdigest().upper() for cert in decoded['DeveloperCertificates']
                 if hashlib.sha1(cert).hexdigest().upper() in identities), None)
if identity is None:
    raise SystemExit('No local signing identity matches the Xcode provisioning profile.')
common = root / 'src/Project2FA.Uno/Platforms/Desktop/Native/Entitlements.plist'
entitlements = plistlib.loads(common.read_bytes())
authorized = plistlib.loads(claims.read_bytes())
for name in ['com.apple.application-identifier', 'com.apple.developer.team-identifier', 'keychain-access-groups']:
    entitlements[name] = authorized[name]
output = root / 'build/macos-signing'
output.mkdir(exist_ok=True)
(output / 'Entitlements.plist').write_bytes(plistlib.dumps(entitlements))
shutil.copyfile(profile, output / 'embedded.provisionprofile')
project = ET.Element('Project')
group = ET.SubElement(project, 'PropertyGroup')
for name, value in {
    'CodesignKey': identity,
    'UnoMacOSEntitlements': '$(MSBuildThisFileDirectory)Entitlements.plist',
    'MacOSProvisioningProfile': '$(MSBuildThisFileDirectory)embedded.provisionprofile',
    'UnoMacOSHardenedRuntime': 'true',
}.items():
    ET.SubElement(group, name).text = value
ET.ElementTree(project).write(output / 'Signing.props', encoding='utf-8', xml_declaration=True)
print('Prepared authorized macOS signing inputs in build/macos-signing.')
