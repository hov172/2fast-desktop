#!/usr/bin/env python3
"""Sign all app code with a configured Developer ID distribution identity."""
from pathlib import Path
import hashlib
import os
import plistlib
import shutil
import subprocess
import sys

root = Path(__file__).resolve().parent.parent
app = Path(sys.argv[1]).resolve()
identity = os.environ['APPLE_DEVELOPER_ID']
profile = Path(os.environ['APPLE_DISTRIBUTION_PROFILE']).resolve()
entitlements = root / 'build/macos-signing/Entitlements.plist'
claims = plistlib.loads(subprocess.check_output(['security', 'cms', '-D', '-i', str(profile)]))
if claims.get('ProvisionedDevices') or not claims.get('ProvisionsAllDevices'):
    raise SystemExit('Release signing requires an all-device distribution profile, without registered devices.')
if identity.upper() not in {hashlib.sha1(c).hexdigest().upper() for c in claims['DeveloperCertificates']}:
    raise SystemExit('The configured certificate fingerprint is not authorized by the distribution profile.')
bundles = sorted([app, *app.rglob('*.app')], key=lambda p: len(p.parts), reverse=True)
for bundle in bundles:
    shutil.copy2(profile, bundle / 'Contents/embedded.provisionprofile')
magic = {bytes.fromhex(s) for s in ['feedface', 'cefaedfe', 'feedfacf', 'cffaedfe', 'cafebabe', 'bebafeca', 'cafebabf', 'bfbafeca']}
count = 0
for path in app.rglob('*'):
    if not path.is_file() or path.is_symlink():
        continue
    with path.open('rb') as stream:
        if stream.read(4) not in magic:
            continue
    cmd = ['codesign', '--force', '--sign', identity, '--options', 'runtime', '--timestamp']
    if path.parent.name == 'MacOS' and path.suffix != '.dylib':
        cmd += ['--entitlements', str(entitlements)]
    subprocess.run([*cmd, str(path)], check=True)
    count += 1
for bundle in bundles:
    subprocess.run(['codesign', '--force', '--sign', identity, '--options', 'runtime', '--timestamp', '--entitlements', str(entitlements), str(bundle)], check=True)
    details = subprocess.run(['codesign', '-d', '--verbose=4', str(bundle)], capture_output=True, text=True, check=True).stderr
    if 'Authority=Developer ID Application:' not in details or 'runtime' not in details or 'Timestamp=' not in details:
        raise SystemExit('Release app must have Developer ID signing, Hardened Runtime and a secure timestamp.')
subprocess.run(['codesign', '--verify', '--deep', '--strict', str(app)], check=True)
subprocess.run([sys.executable, str(root / 'scripts/verify-release-privacy.py'), str(app)], check=True)
print(f'Developer ID signed {count} Mach-O files and {len(bundles)} app bundles.')
