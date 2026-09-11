"""Package current release documentation, fixing links for a flat offline bundle."""
from pathlib import Path
import hashlib
import re
import zipfile
import subprocess
import sys
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parent.parent
dist = root / 'dist'
subprocess.run([sys.executable, str(root / 'scripts/verify-release-privacy.py')], check=True)
version = ET.parse(root / 'src/Project2FA.Uno/Project2FA.Uno.csproj').findtext('PropertyGroup/ApplicationDisplayVersion')
assert version
files = {
    'README.md': 'README.md', 'MACOS.md': 'MACOS.md', 'LICENSE': 'LICENSE',
    'docs/USER-GUIDE.md': 'USER-GUIDE.md', 'docs/WINDOWS.md': 'WINDOWS.md',
    'docs/DESKTOP-COMPATIBILITY.md': 'DESKTOP-COMPATIBILITY.md',
    'docs/MACOS-VAULT-FORMAT.md': 'MACOS-VAULT-FORMAT.md',
    'docs/RELEASING.md': 'RELEASING.md',
    'docs/audits/2026-09-08-desktop-parity.md': 'DESKTOP-VERIFICATION.md',
    'docs/audits/2026-09-08-desktop-ui.md': 'DESKTOP-UI-REVIEW.md',
    'docs/audits/2026-09-08-classic-layout.md': 'CLASSIC-LAYOUT-VERIFICATION.md',
    'docs/audits/2026-09-08-unlock-navigation.md': 'UNLOCK-VERIFICATION.md',
    'docs/audits/2026-09-08-privacy-remediation.md': 'PRIVACY-VERIFICATION.md',
    f'docs/RELEASE-v{version}.md': f'RELEASE-v{version}.md',
}
resolved = {(root / source).resolve(): name for source, name in files.items()}
bundle = dist / f'documentation-{version}'
bundle.mkdir(exist_ok=True)
for source, name in files.items():
    path = root / source
    def link(match):
        target = match.group(1)
        if ':' in target or target.startswith('#'):
            return match.group(0)
        relative, sep, fragment = target.partition('#')
        destination = (path.parent / relative).resolve()
        replacement = resolved.get(destination)
        if replacement is None:
            replacement = f'https://github.com/hov172/2fast-desktop/blob/v{version}/' + destination.relative_to(root).as_posix()
        return '](' + replacement + (sep + fragment if sep else '') + ')'
    text = re.sub(r'\]\(([^)]+)\)', link, path.read_text())
    (bundle / name).write_text(text)
with zipfile.ZipFile(dist / f'2fast-{version}-documentation.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
    for name in files.values():
        archive.write(bundle / name, name)
# Include the same guides in both portable Windows packages.
for arch in ('x64', 'arm64'):
    folder = dist / f'windows-{arch}'
    if not folder.exists():
        raise SystemExit(f'Missing Windows build: {folder}')
    for name in files.values():
        (folder / name).write_bytes((bundle / name).read_bytes())
    with zipfile.ZipFile(dist / f'2fast-windows-{arch}.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
        for path in sorted(folder.rglob('*')):
            if path.is_file():
                archive.write(path, Path('2fast') / path.relative_to(folder))
assets = ['2fast-macos-universal.zip', '2fast-macos-universal.dmg', '2fast-windows-x64.zip', '2fast-windows-arm64.zip', f'2fast-{version}-documentation.zip']
(dist / 'SHA256SUMS').write_text(''.join(hashlib.sha256((dist / name).read_bytes()).hexdigest() + '  ' + name + '\n' for name in assets))
print(f'Packaged {version} documentation and checksums for {len(assets)} archives.')
