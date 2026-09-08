from pathlib import Path
import hashlib
import struct
import zipfile
root = Path(__file__).resolve().parent.parent
dist = root / 'dist'
for arch, machine in [('x64', 0x8664), ('arm64', 0xaa64)]:
    folder = dist / ('windows-' + arch)
    if not folder.exists():
        continue
    for name in ['Project2FA.Uno.exe', 'OpenCvSharpExtern.dll', 'coreclr.dll']:
        binary = (folder / name).read_bytes()
        pe = struct.unpack_from('<I', binary, 0x3c)[0]
        assert binary[pe:pe+4] == b'PE\0\0' and struct.unpack_from('<H', binary, pe+4)[0] == machine, f'Wrong architecture: {name}'
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
    with zipfile.ZipFile(dist / ('2fast-windows-' + arch + '.zip'), 'w', zipfile.ZIP_DEFLATED) as archive:
        for path in sorted(folder.rglob('*')):
            if path.is_file():
                archive.write(path, Path('2fast') / path.relative_to(folder))
lines = []
for path in sorted(dist.glob('2fast-*.zip')):
    lines.append(hashlib.sha256(path.read_bytes()).hexdigest() + '  ' + path.name)
(dist / 'SHA256SUMS').write_text('\n'.join(lines) + '\n')
print('Windows archives created; app host, CLR and camera native architectures verified.')
