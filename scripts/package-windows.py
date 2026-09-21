from pathlib import Path
import hashlib
import os
import re
import struct
import zipfile
import subprocess
import sys

def find_iscc():
    """Inno Setup compiler, or None. The installer is optional: the portable zip
    and the single-file exe are the primary artifacts and build without it."""
    override = os.environ.get('ISCC')
    if override:
        return Path(override) if Path(override).exists() else None
    for base in [os.environ.get('ProgramFiles(x86)', r'C:\Program Files (x86)'),
                 os.environ.get('ProgramFiles', r'C:\Program Files')]:
        for version in ['6', '7']:
            candidate = Path(base) / ('Inno Setup ' + version) / 'ISCC.exe'
            if candidate.exists():
                return candidate
    return None

def app_version(root):
    """Single source of truth: the Uno head's ApplicationDisplayVersion."""
    text = (root / 'src/Project2FA.Uno/Project2FA.Uno.csproj').read_text(encoding='utf-8')
    match = re.search(r'<ApplicationDisplayVersion>([^<]+)</ApplicationDisplayVersion>', text)
    assert match, 'ApplicationDisplayVersion not found in Project2FA.Uno.csproj'
    return match.group(1).strip()

def check_pe_architecture(path, machine, label):
    binary = path.read_bytes()
    pe = struct.unpack_from('<I', binary, 0x3c)[0]
    assert binary[pe:pe+4] == b'PE\0\0' and struct.unpack_from('<H', binary, pe+4)[0] == machine, f'Wrong architecture: {label}'
    return binary

root = Path(__file__).resolve().parent.parent
dist = root / 'dist'
iscc = find_iscc()
version = app_version(root)
installers = []
for arch, machine in [('x64', 0x8664), ('arm64', 0xaa64)]:
    folder = dist / ('windows-' + arch)
    # An empty or partial folder is a leftover from an earlier release, not
    # something to package. Key off the app host, not the directory existing.
    if not (folder / 'Project2FA.Uno.exe').exists():
        if folder.exists():
            print('Skipping ' + arch + ': ' + str(folder) + ' has no Project2FA.Uno.exe (stale or unbuilt).')
        continue
    for name in ['Project2FA.Uno.exe', 'OpenCvSharpExtern.dll', 'coreclr.dll']:
        check_pe_architecture(folder / name, machine, name)
    # The app only opens cameras through DirectShow, never video files/FFmpeg.
    for unused in folder.glob('opencv_videoio_ffmpeg*.dll'):
        unused.unlink()
    (folder / 'LICENSE').write_bytes((root / 'LICENSE').read_bytes())
    (folder / 'THIRD-PARTY-NOTICES.md').write_text("OpenCvSharp 4.13.0.20260627, Copyright 2008-2026 shimat, Apache-2.0.\nSource and license: https://github.com/shimat/opencvsharp/tree/b161e7e012f5101f6d5dc68a835c59db6cc88b18\nOpenCV 4.13, Apache-2.0: https://github.com/opencv/opencv/tree/4.13.0\nSee the application's About/dependencies section for other dependencies.\n")
    license_file = root / 'docs/licenses/OpenCvSharp-LICENSE'
    if license_file.exists(): (folder / 'OpenCvSharp-LICENSE').write_bytes(license_file.read_bytes())
    for name in ['WINDOWS.md', 'DESKTOP-COMPATIBILITY.md']:
        source = root / 'docs' / name
        if source.exists():
            (folder / name).write_bytes(source.read_bytes())
    subprocess.run([sys.executable, str(root / 'scripts/verify-release-privacy.py'), str(folder)], check=True)
    with zipfile.ZipFile(dist / ('2fast-windows-' + arch + '.zip'), 'w', zipfile.ZIP_DEFLATED) as archive:
        for path in sorted(folder.rglob('*')):
            if path.is_file():
                archive.write(path, Path('2fast') / path.relative_to(folder))
    single = dist / ('windows-' + arch + '-singlefile') / 'Project2FA.Uno.exe'
    if single.exists():
        binary = check_pe_architecture(single, machine, 'single-file exe')
        subprocess.run([sys.executable, str(root / 'scripts/verify-release-privacy.py'), str(single)], check=True)
        (dist / ('2fast-windows-' + arch + '.exe')).write_bytes(binary)
    # Installer last: it packages `folder`, which has just been given its
    # LICENSE, notices and guides and been privacy-verified uncompressed. The
    # payload inside the installer is LZMA2-compressed, so scanning the setup
    # exe for workstation paths would pass vacuously - the meaningful check is
    # the one already run on the folder above.
    if iscc:
        icon = root / 'src/Project2FA.Uno/Platforms/Desktop/icon.ico'
        command = [str(iscc), '/Qp',
                   '/DAppVersion=' + version,
                   '/DArch=' + arch,
                   '/DSourceDir=' + str(folder),
                   '/DOutputDir=' + str(dist)]
        if icon.exists():
            command.append('/DIconFile=' + str(icon))
        command.append(str(root / 'scripts/windows-installer.iss'))
        subprocess.run(command, check=True)
        installer = dist / ('2fast-windows-' + arch + '-setup.exe')
        check_pe_architecture(installer, 0x14c, 'installer')  # Inno stubs are 32-bit by design
        installers.append(installer.name)
lines = []
for path in sorted([*dist.glob('2fast-*.zip'), *dist.glob('2fast-windows-*.exe')]):
    lines.append(hashlib.sha256(path.read_bytes()).hexdigest() + '  ' + path.name)
(dist / 'SHA256SUMS').write_text('\n'.join(lines) + '\n')
print('Windows archives created; app host, CLR and camera native architectures verified.')
if installers:
    print('Installers created: ' + ', '.join(installers))
elif iscc is None:
    print('Skipping installers: Inno Setup not found. Install it with '
          '`winget install JRSoftware.InnoSetup`, or set ISCC to its ISCC.exe.')
