#!/usr/bin/python3
"""Package the two signed Uno apps with a native universal launcher."""
from pathlib import Path
import datetime
import hashlib
import os
import plistlib
import shutil
import subprocess
import tempfile
import sys

ROOT = Path(__file__).resolve().parent.parent
DIST = ROOT / 'dist'
DIST.mkdir(exist_ok=True)
if not os.environ.get('APPLE_DEVELOPER_ID'):
    raise SystemExit('Set APPLE_DEVELOPER_ID (certificate SHA-1) for release packaging.')

def run(*args):
    subprocess.run([str(arg) for arg in args], check=True)

with tempfile.TemporaryDirectory(prefix='universal-', dir=ROOT / 'build') as stage:
    app = Path(stage) / '2fast.app'
    contents = app / 'Contents'
    (contents / 'MacOS').mkdir(parents=True)
    (contents / 'Resources').mkdir()
    for arch, cpu in [('arm64', 'arm64'), ('x64', 'x86_64')]:
        source = ROOT / f'src/Project2FA.Uno/bin/Release/net10.0-desktop/osx-{arch}/publish/2fast.app'
        for binary in (source / 'Contents/MacOS').iterdir():
            if binary.suffix == '.dylib' or binary.name == 'Project2FA.Uno':
                slices = subprocess.check_output(['lipo', '-archs', str(binary)], text=True).split()
                if cpu not in slices:
                    raise RuntimeError(f'{binary.name} is missing {cpu}')
        run('codesign', '--verify', '--deep', '--strict', source)
        run('ditto', source, contents / f'Helpers/{arch}/2fast.app')
        if arch == 'arm64':
            info = plistlib.loads((source / 'Contents/Info.plist').read_bytes())
            for icon in (source / 'Contents/Resources').glob('*.icns'):
                shutil.copy2(icon, contents / 'Resources' / icon.name)
    info['CFBundleExecutable'] = '2fast-launcher'
    info['LSArchitecturePriority'] = ['arm64', 'x86_64']
    info['LSMinimumSystemVersion'] = '15.0'
    (contents / 'Info.plist').write_bytes(plistlib.dumps(info))
    run('xcrun', 'clang', '-Wall', '-Wextra', '-Werror', '-arch', 'arm64', '-arch', 'x86_64',
        '-mmacosx-version-min=15.0', ROOT / 'scripts/native/macos-universal-launcher.c',
        '-o', contents / 'MacOS/2fast-launcher')
    run(sys.executable, ROOT / 'scripts/sign-macos-release.py', app)
    run('codesign', '--verify', '--deep', '--strict', app)
    destination = DIST / '2fast.app'
    if destination.exists():
        destination.rename(DIST / ('2fast-before-universal-' + datetime.datetime.now().strftime('%Y%m%d-%H%M%S-%f') + '.app'))
    run('ditto', app, destination)
    archive = DIST / '2fast-macos-universal.zip'
    run('ditto', '-c', '-k', '--sequesterRsrc', '--keepParent', destination, archive)
    sums = []
    for file in sorted(DIST.glob('2fast-macos-*.zip')):
        sums.append(hashlib.sha256(file.read_bytes()).hexdigest() + '  ' + file.name)
    (DIST / 'SHA256SUMS').write_text('\n'.join(sums) + '\n')
print('Built universal app: ' + str(DIST / '2fast.app'))
