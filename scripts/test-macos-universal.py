#!/usr/bin/python3
"""Verify universal dispatch without opening the user's vault or launching app UI."""
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parent.parent
launcher = ROOT / 'dist/2fast.app/Contents/MacOS/2fast-launcher'
slices = subprocess.check_output(['lipo', '-archs', str(launcher)], text=True).split()
assert set(slices) == {'arm64', 'x86_64'}, slices
for cpu, runtime in [('arm64', 'arm64'), ('x86_64', 'x64')]:
    actual = subprocess.check_output(['arch', '-' + cpu, str(launcher), '--print-runtime'], text=True).strip()
    assert actual == runtime, (cpu, actual)
with tempfile.TemporaryDirectory(prefix='universal test ', dir=ROOT / 'build') as temp:
    contents = Path(temp) / '2fast.app/Contents'
    (contents / 'MacOS').mkdir(parents=True)
    test_launcher = contents / 'MacOS/2fast-launcher'
    subprocess.run(['xcrun', 'clang', '-Wall', '-Wextra', '-Werror', '-arch', 'arm64', '-arch', 'x86_64', '-mmacosx-version-min=15.0', str(ROOT / 'scripts/native/macos-universal-launcher.c'), '-o', str(test_launcher)], check=True)
    for runtime in ['arm64', 'x64']:
        child = contents / f'Helpers/{runtime}/2fast.app/Contents/MacOS/Project2FA.Uno'
        child.parent.mkdir(parents=True)
        child.write_text('#!/bin/sh\nprintf "%s\\n" "' + runtime + '" "$@"\nexit 17\n')
        child.chmod(0o755)
    for cpu, runtime in [('arm64', 'arm64'), ('x86_64', 'x64')]:
        result = subprocess.run(['arch', '-' + cpu, str(test_launcher), 'argument with spaces', 'two'], capture_output=True, text=True)
        assert result.returncode == 17, (cpu, result.returncode, result.stderr)
        assert result.stdout.splitlines() == [runtime, 'argument with spaces', 'two'], result.stdout
print('Universal launcher: both slices select and execute the matching runtime; arguments and exit status preserved.')
