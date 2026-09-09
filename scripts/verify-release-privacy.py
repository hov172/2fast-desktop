#!/usr/bin/env python3
"""Fail release packaging if symbols, workstation paths or device profiles leak."""
from pathlib import Path
import mmap
import plistlib
import subprocess
import sys

root = Path(__file__).resolve().parent.parent
paths = [Path(p) for p in sys.argv[1:]] or [root / 'dist/2fast.app', root / 'dist/windows-x64', root / 'dist/windows-arm64', root / 'dist/2fast-windows-x64.exe', root / 'dist/2fast-windows-arm64.exe']
markers = {str(Path.home()) + '/', str(root), str(root).replace('/', '\\')}
needles = [s.encode(encoding) for s in markers for encoding in ('utf-8', 'utf-16le')]
failures = []
count = 0
for target in paths:
    if not target.exists():
        failures.append(f'Missing artifact: {target.name}')
        continue
    files = [target] if target.is_file() else target.rglob('*')
    for p in files:
        if not p.is_file() or p.is_symlink():
            continue
        count += 1
        label = str(p.relative_to(root)) if p.is_relative_to(root) else p.name
        if p.suffix.lower() in ('.pdb', '.mdb', '.pfx', '.p12', '.2fa'):
            failures.append(f'Forbidden release file: {label}')
        if p.stat().st_size:
            with p.open('rb') as f, mmap.mmap(f.fileno(), 0, access=mmap.ACCESS_READ) as data:
                if any(data.find(needle) >= 0 for needle in needles):
                    failures.append(f'Workstation path in {label}')
        if p.suffix == '.provisionprofile':
            result = subprocess.run(['security', 'cms', '-D', '-i', str(p)], capture_output=True, check=True)
            profile = plistlib.loads(result.stdout)
            if profile.get('ProvisionedDevices') or not profile.get('ProvisionsAllDevices'):
                failures.append(f'Development/device profile in {label}')
if failures:
    print('\n'.join(failures), file=sys.stderr)
    raise SystemExit(1)
print(f'PASS: {count} release files contain no detected workstation paths, debug-symbol files, or device provisioning lists.')
