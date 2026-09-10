# CLAUDE.md — how work is done in this repo

README.md says *what* 2fast Desktop is. This file says *how* to change it.
It is the foundation layer of the context stack; the other layers are
[docs/architecture.md](docs/architecture.md), [docs/ux-flows.md](docs/ux-flows.md),
[docs/design.md](docs/design.md), [docs/interactions.md](docs/interactions.md),
and [docs/plan.md](docs/plan.md). Read the layer that matches the question
instead of re-deriving it from source.

## The rule that matters most: reuse before you write

This is a fork of an existing, complete app. Almost every capability you are
asked for **already exists somewhere**. Before adding a class, a service, a
converter, or a helper, find the owner in the table below and extend it.

| If you need… | It already lives in | Do not create |
| --- | --- | --- |
| otpauth:// / OCRA / MobileID parsing | `Project2FA.Shared/Services/Parser/` (`IProject2FAParser`) and `src/Project2FA.Uno/Platforms/Desktop/MacOSOtpParser.cs` | a third parser — see the redundancy note below |
| Vault read/write, unlock, password change | `Project2FA.Shared/Services/DataService.cs` (+ its desktop partial `Platforms/Desktop/MacOSDatafileActions.cs`) | a new "vault manager" |
| Encryption / hashing / key derivation | `Project2FA.Shared/Services/Crypto/CryptoService.cs`, `Serialization/SerializationCryptoService.cs`, `Platforms/Desktop/MacOSVaultCodec.cs` (V4) | a fourth crypto entry point |
| JSON (de)serialization | `Project2FA.Shared/Services/Serialization/` — `SerializationService`, `SerializationContext` (source-generated, AOT-safe) | ad-hoc `JsonSerializer` calls with new options |
| Settings / preferences | `Project2FA.Shared/Services/SettingsService/SettingsService.cs` (`SettingsService.Instance`) | a new settings wrapper |
| Secrets, biometric-bound keys | `UNOversal.Services.Secrets.ISecretService`, `Platforms/Desktop/MacOSSecretHelper.cs` | a new keychain/credential helper |
| WebDAV | `Project2FA.Shared/Services/WebDAV/` (`WebDAVDirectoryService.Instance`). The copy in `Project2FA.Core/Services/WebDAV/` is **dead** — unreferenced, `GetClient()` returns `null` | a third WebDAV client |
| Backup import (Aegis, andOTP, 2FAS, 2fast) | `Project2FA.Shared/Services/Importer/` behind `IBackupImporterService` | a new importer service; add a format alongside the existing four |
| Page/dialog view-model state | `Project2FA.Shared/ViewModels/` and `ViewModels/Base/*ViewModelBase.cs` | a parallel view-model for the same page |
| Value conversion for XAML | `Project2FA.Shared/Converters/` (16 converters) | a converter that duplicates an existing one |
| Cross-view-model signalling | `Project2FA.Shared/Messenger/` + `CommunityToolkit.Mvvm.Messaging` | events or static callbacks |
| Reusable controls | `Project2FA.Shared/Controls/`, `src/UnoLibrary.Controls/`, `src/Project2FA.Uno/Controls/` | a copy of an existing control |
| Constants and container names | `Project2FA.Core/Constants.cs` | inline string literals |

If nothing owns the capability, add it to the layer that owns the *concern*
(see [docs/architecture.md](docs/architecture.md)), not to the file you happen
to be editing.

## Known redundancy — do not extend it, and do not add to it

The desktop fork accumulated a parallel set of classes because earlier work did
not locate the shared ones. Two facts to internalise:

1. **`MacOS*` names are not macOS-only.** 19 of the 20 `MacOS*.cs` files under
   `src/Project2FA.Uno/Platforms/Desktop/` are guarded by `#if TWOFAST_DESKTOP`,
   so they compile into the **Windows** build too. Only `MacOSNative.cs` is
   really macOS (`#if TWOFAST_MACOS`). The prefix is a historical misnomer; it
   is the main reason the code gets re-implemented instead of reused. Never name
   a new desktop type `MacOS*`.
2. **Two OTP parsers coexist.** `AddAccountViewModelBase.ParseQRCode` branches on
   `TWOFAST_DESKTOP` between the static `MacOSOtpParser` and the injected
   `IProject2FAParser`. Consolidation is tracked in
   [docs/plan.md](docs/plan.md). Until then, change **both** or neither, and add
   coverage to `tests/MacOS/ParserTests.csproj` (which, despite its path, is the
   shared parser suite and runs on Windows too).

3. **The shared layer calls into the head 42 times.** `Project2FA.Shared` makes
   42 fully-qualified `Project2FA.Services.MacOS.*` calls across 13 files
   (`DataService.cs` alone: 15). That is backwards, and it is why desktop
   capabilities are invisible from the shared project. Do not add a 43rd — if
   shared code needs a desktop capability, give it an interface.
4. **Desktop strings skip localization.** `Platforms/Desktop/` holds 44
   hardcoded UI literals and 32 English exception messages shown to users, and
   zero `Strings.Resources` uses, in an app shipping 15+ languages. New
   user-facing text there gets a resource key.

New duplication is a defect. Consolidating the existing duplication is planned
work with tests attached — not something to do opportunistically in the middle
of an unrelated change.

The counterexample worth copying: `AddAccountPageViewModel` (70 lines) and
`AddAccountContentDialogViewModel` (84 lines) are thin shells over the shared
`AddAccountViewModelBase` (1090 lines). That is what reuse looks like here.

## Conventions

**Layering.** `Project2FA.Core` (platform-free) → `Project2FA.Shared` (shared
project, `.shproj`, compiled into every head) → heads (`src/Project2FA.Uno`,
`Project2FA/Project2FA.UWP`). Code in `Shared` must not reach into a head's
namespaces. `AddAccountViewModelBase` currently violates this by referencing
`Project2FA.Services.MacOS.MacOSOtpParser`; do not add a second violation.

**MVVM.** `CommunityToolkit.Mvvm`. View-models derive from `ObservableObject` /
`ObservableRecipient` and declare properties by hand with `SetProperty`, and
commands as `IAsyncRelayCommand` / `ICommand` fields. The source generators
(`[ObservableProperty]`, `[RelayCommand]`) are used in exactly one place each —
match the surrounding hand-written style rather than introducing generators
piecemeal. Shared behaviour goes in `ViewModels/Base/*ViewModelBase.cs`;
platform additions go in a `partial` of the same view-model.

**Binding.** Prefer `x:Bind` (262 uses) over `{Binding}` (49). Use `{Binding}`
only where `x:Bind` cannot express the target (data templates over untyped
items, style setters).

**DI and navigation.** UNOversal over DryIoc. Every service, page and dialog is
registered in `RegisterTypes` in [src/Project2FA.Uno/App.xaml.cs](src/Project2FA.Uno/App.xaml.cs):
`RegisterSingleton<IFoo, Foo>()`, `RegisterForNavigation<Page, PageViewModel>()`,
`RegisterDialog<Dialog, DialogViewModel>()`. Navigate with
`INavigationService.NavigateAsync(nameof(SomePage))`; a leading `/` resets the
stack. Never `new` a view-model that has a registration.

**Platform code.** Symbols are `TWOFAST_DESKTOP` (Windows **and** macOS),
`TWOFAST_WINDOWS`, `TWOFAST_MACOS`, plus Uno's `__IOS__` / `__ANDROID__` /
`HAS_UNO` / `WINDOWS_UWP`. Pick the narrowest symbol that is actually true.

**Serialization.** Go through `SerializationService` and register new types in
`SerializationContext` — the desktop heads publish trimmed, so reflection-based
JSON fails at runtime, not at build time.

**Adding a file to `Project2FA.Shared`.** It is a shared project (`.shproj`):
both heads compile the list in `Project2FA.Shared.projitems`, not the directory.
A new `.cs` file needs a `<Compile Include="$(MSBuildThisFileDirectory)…" />`
entry with **exactly matching case**, or it compiles in no head and you get a
confusing "type does not exist" error. `tests/Desktop/SharedProjectTests`
enforces this.

**Strings.** Localised resources live in `Project2FA.Shared/Strings/<lang>/`.
`en` is the source of truth; the app ships 15+ languages. New user-facing text
gets a resource key, not a literal.

## Security rules

This app holds TOTP secrets. Treat every change under `Services/Crypto`,
`Services/Serialization`, `MacOSVaultCodec`, `MacOSSecretHelper`,
`MacOSDeviceBinding` or the WebDAV path as security-critical.

- V4 vaults are AES-256-GCM with PBKDF2-SHA256 at 600,000 iterations and a fixed
  associated-data tag. Do not change those parameters without a migration path —
  older Windows builds and mobile clients cannot read upgraded files.
- Zero key and plaintext buffers with `CryptographicOperations.ZeroMemory` in a
  `finally`, as the existing code does.
- Compare secrets with `CryptographicOperations.FixedTimeEquals`.
- QR payloads, WebDAV responses and imported backups are untrusted input:
  validate length and shape before parsing, as `MacOSOtpParser` does.
- WebDAV must stay HTTPS-only (enforced as of 1.4.5).
- Never log secrets, passwords, vault contents, or paths that contain them.
  `scripts/verify-release-privacy.py` gates releases and will fail the build.

## Commands

No project MCP servers are configured, so the callable tool surface is the
scripts. Run them from the repo root.

```
# build
pwsh scripts/build-windows.ps1 -Runtime win-x64        # or win-arm64, add -SingleFile
./scripts/build-macos.sh

# test
pwsh scripts/test-windows.ps1                          # QR frame, shared parser, OCRA, Windows native
./scripts/test-macos.sh

# a single suite
dotnet run --project tests/MacOS/ParserTests.csproj -c Release

# package / release
python scripts/package-windows.py
python scripts/package-macos-universal.py
python scripts/verify-release-privacy.py dist/windows-x64
```

The Uno head targets `net10.0-desktop`; note that `TWOFAST_DESKTOP` /
`TWOFAST_WINDOWS` / `TWOFAST_MACOS` are only defined when a `RuntimeIdentifier`
is passed, so a build without `-r win-x64` (or `osx-…`) excludes all the desktop
platform files and fails with missing-handler errors. The release process is in
[docs/RELEASING.md](docs/RELEASING.md).

**The legacy UWP head is dormant — do not assume it builds.** Both build scripts
publish only `src/Project2FA.Uno/Project2FA.Uno.csproj`; no script or CI touches
`Project2FA/Project2FA.UWP`, and it cannot be built from macOS at all. It does
not compile today (285 errors, no XAML codegen) and has not since it was imported
from upstream. Making it build needs MSBuild 18 *and* the UWP workload — see
[docs/plan.md](docs/plan.md) before spending any time on it. Changes to
`Project2FA.Shared` still compile into it, which is what `SharedProjectTests`
guards.

## Scope discipline

[docs/plan.md](docs/plan.md) states the current phase. Implement the current
phase only. Do not scaffold later phases, and do not fold consolidation work
into an unrelated fix — say it is needed and leave it to its own change.
