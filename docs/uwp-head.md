# The legacy UWP head

`Project2FA/Project2FA.UWP` — inherited Windows Store code, **compile-only**.

It compiles. It launches to a black window. Nothing in this repository builds or
ships it. Read this before spending time on it; almost none of what follows is
discoverable from the project file, and all of it is expensive to rediscover.

## Status

- Neither `scripts/build-windows.ps1` nor `scripts/build-macos.sh` touches it —
  both publish only `src/Project2FA.Uno/Project2FA.Uno.csproj`. There is no CI
  (`.github/` has no `workflows/`).
- It cannot be built on macOS at all: it needs the Windows SDK, WinRT projections
  and CsWinRT.
- It has been touched once in this fork, by `568bb6a`, the commit that imported
  it from upstream.

**Keep it compiling anyway.** `Project2FA.Shared` compiles into it, so it is a
second compiler over the shared layer — which is how the Phase 1c converter and
importer changes were verified beyond inference. On machines without the UWP
toolchain, `tests/Desktop/SharedProjectTests` is the fallback guard.

## Building it

Three things are required. Any one missing produces errors that look like broken
source code but are not.

1. **Windows SDK 10.0.26100** — `winget install --id Microsoft.WindowsSDK.10.0.26100`.
   Without it CsWinRT fails on the missing
   `Platforms\UAP\10.0.26100.0\Platform.xml`. The project cannot be retargeted to
   an older SDK: `net10.0-windows10.0.22000.0` fails restore with `NU1202`,
   because `CommunityToolkit.Uwp.Lottie 8.2.250604` supports only
   `net9.0-windows10.0.26100` or `uap10.0.16299`.

2. **MSBuild 18**, i.e. the Visual Studio 2026 generation. .NET SDK 10.0.303
   refuses to load under MSBuild 17, so Visual Studio 2022 and the 2019/2022
   Build Tools cannot build this project at all:
   *"Version 10.0.303 of the .NET SDK requires at least version 18.0.0 of MSBuild."*

3. **The Universal Windows Platform workload** (~6 GB), which supplies the XAML
   compiler that `<UseUwp>true</UseUwp>` needs:

   ```
   setup.exe modify --installPath "C:\Program Files\Microsoft Visual Studio\18\Community" \
     --add Microsoft.VisualStudio.Workload.Universal --includeRecommended --quiet --norestart
   ```

   Quote the install path — unquoted, it truncates at the first space and the
   installer exits 1 with "an installed product … cannot be found".

Then build with the 2026 MSBuild, not `dotnet build`:

```
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" \
  Project2FA\Project2FA.UWP\Project2FA.UWPNet.csproj -t:Build -restore \
  -p:Configuration=Debug -p:Platform=x64
```

### The one diagnostic worth memorising

With the workload missing, the build emits **~285 errors** that all read as
broken code — `InitializeComponent`, `LV_AccountCollection`, `ShellHeaderTemplate`,
`MainPivot` "does not exist". They are not source errors. No XAML `*.g.cs` is
generated at all, so every code-behind file loses its generated half. Installing
the workload took the generated file count from 1 to 58 and the errors from 285
to 0.

**If you see those errors, check the workload before reading any C#.**

### Signing

The project pins a Store certificate (`PackageCertificateThumbprint 7DA25049…`)
that exists only on the release machine. `AppxPackageSigningEnabled` is now
`True` for `Release` only, so `Debug` compiles without it; previously it was
unconditional and a successful compile still ended in
`SigningCertificateThumbprintNotInStore` at the MSIX packaging step. Override
with `-p:AppxPackageSigningEnabled=True|False`.

## Running it

Registration needs the **Debug** VCLibs framework package, or it fails with
`0x80073CF3`:

```
Add-AppxPackage "C:\Program Files (x86)\Microsoft SDKs\Windows Kits\10\ExtensionSDKs\Microsoft.VCLibs\14.0\Appx\Debug\x64\Microsoft.VCLibs.x64.Debug.14.00.appx"
Add-AppxPackage -Register "<bin>\net10.0-windows10.0.26100.0\AppxManifest.xml"
```

Remove it again with `Get-AppxPackage *2fastBeta* | Remove-AppxPackage`. Note the
registration points at your `bin` folder, so it breaks on the next rebuild.

## It renders nothing, and the cause is not in this code

The app launches to a live, responsive window with correct chrome and title
(`2fast - two factor authenticator`) and an entirely black client area. This is
pre-existing: the same build from unmodified `2b6d3eb` in a separate worktree
behaves identically.

The startup logic is *correct*. Instrumenting `OnStartAsync` showed the whole
sequence completing on a fresh install:

```
CreateShell called → ShellPage resolved → OnStartAsync entered (Content == null)
→ fresh-install branch → NavigateAsync("/TutorialPage") success=True
→ Window.Current.Content = ShellPage (MainFrame content: TutorialPage)
→ Activate() returned
```

No exception, nothing in the Application event log, nothing in
`App_UnhandledException`, `LocalState` empty. The visual tree is fully built and
the window activated — the content simply never paints. That is rendering and
composition in UWP-on-.NET-10, not anything reading this repository's C# will
find.

If you do chase it, the honest first step is checking whether a minimal
UWP-on-.NET-10 app renders at all on the machine. If it does not, the problem is
the platform, not 2fast.

**Two traps when comparing against a baseline:**

- `Add-AppxPackage -Register` silently no-ops when the same identity and version
  is already registered. Remove the package first, then register, and confirm
  `InstallLocation` actually moved — otherwise the "baseline" run is the build
  you were trying to compare against.
- UWP windows are hosted in `ApplicationFrameWindow`, so `Process.MainWindowHandle`
  is always 0. Find the window by class and title instead.

## Recommendation

Treat it as compile-only. Chasing the render failure means debugging a very new
platform combination for an app this project does not distribute.
