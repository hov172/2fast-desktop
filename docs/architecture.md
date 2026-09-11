# Architecture

How data flows, which layer owns which concern, and where the current structure
misrepresents itself. Conventions live in [CLAUDE.md](../CLAUDE.md).

## Projects

| Project | Kind | Owns |
| --- | --- | --- |
| `Project2FA.Core` | class library, platform-free | `Constants`, WebDAV client + directory service, network-time service, collection utilities, OTP migration proto models |
| `Project2FA.Shared` | **shared project** (`.shproj`) compiled into every head | models, view-models, services (crypto, serialization, parser, importers, settings, WebDAV), converters, messenger, controls, localized strings |
| `src/Project2FA.Uno` | Uno head — `net10.0-desktop`, iOS, Android | **the shipping app**; XAML views, dialogs, DI registration, platform code under `Platforms/` |
| `Project2FA/Project2FA.UWP` | legacy UWP head, **compile-only** | inherited Windows Store build. No script or CI builds it, it cannot build on macOS, and it launches to a black window (pre-existing, reproduces at `HEAD`). It does compile on Windows with the 10.0.26100 SDK, MSBuild 18 and the UWP workload, and it compiles `Project2FA.Shared` — which is its value: a second compiler over the shared layer. See [uwp-head.md](uwp-head.md) |
| `src/UnoLibrary.Controls` | class library | Markdown renderer and supporting controls |
| vendored: `BiometryService`, `Otp.NET`, `ZXing.Net.Uno`, `WebDAVClientPortable`, `UNOversalTemplate` | source deps | biometrics, TOTP/HOTP, QR decode, WebDAV transport, MVVM/DI/navigation framework |

Dependency direction is one-way: `Core` → `Shared` → head. Nothing in `Core` or
`Shared` may reference a head.

## Composition

UNOversal over DryIoc. `App.RegisterTypes` in
[src/Project2FA.Uno/App.xaml.cs](../src/Project2FA.Uno/App.xaml.cs) is the single
composition root:

- `RegisterSingleton<IFoo, Foo>()` — services
- `RegisterForNavigation<Page, PageViewModel>()` — pages
- `RegisterDialog<Dialog, DialogViewModel>()` — content dialogs

Form factor is decided here, not in the view-models: mobile registers
`EditAccountPage` / `AddAccountPage` / `CameraPage` as full-screen pages, desktop
and UWP register the equivalent content dialogs. Desktop additionally registers
`AddAccountPage` because native QR capture, OCRA setup and manual entry all land
on that review page.

## Data flow

```
View (XAML, x:Bind)
  → ViewModel (CommunityToolkit.Mvvm, ObservableObject/ObservableRecipient)
      → DataService (single vault authority, CollectionAccessSemaphore)
          → SerializationService / SerializationCryptoService  (JSON, AOT-safe)
          → CryptoService / VaultCodec                         (AES-256-GCM, PBKDF2)
          → ISecretService + platform native                   (keychain / DPAPI+Hello)
          → WebDAVClientService                                (HTTPS only)
      ↔ IMessenger (CommunityToolkit.Mvvm.Messaging)           (cross-view-model signals)
```

Rules that follow from this:

- Views never touch storage, crypto or HTTP. They bind to a view-model.
- `DataService` is the only writer of the vault. It serializes concurrent access
  through `CollectionAccessSemaphore`; new write paths go through it, not around it.
- Cross-view-model notification uses the messages in
  `Project2FA.Shared/Messenger/` (`CategoriesChangedMessage`,
  `DatafileWriteStatusChangedMessage`, `FilteringChangedMessage`,
  `PasswordStatusChangedMessage`, `WebDAVStatusChangedMessage`), never events or
  static callbacks.
- Anything serialized must be declared in `SerializationContext`. The desktop
  heads publish trimmed; reflection-based JSON fails at runtime.

## Vault format

V4 is the current format: AES-256-GCM over the complete serialized vault
(including empty collections and metadata), PBKDF2-SHA256 at 600,000 iterations,
random 32-byte salt and 12-byte nonce, with a fixed associated-data tag
`2fast:v4:AES-256-GCM:PBKDF2-SHA256:600000`. The envelope is
`{ Salt, Nonce, Tag, Data }`. Legacy vaults are detected by the absence of
`Version == 4` and upgraded through Settings → Data file → Upgrade vault
encryption. Windows and macOS builds share V4 files; older Windows releases and
mobile clients cannot read them. See [MACOS-VAULT-FORMAT.md](MACOS-VAULT-FORMAT.md).

## Platform strategy

| Symbol | Applies to |
| --- | --- |
| `TWOFAST_DESKTOP` | Windows **and** macOS desktop heads |
| `TWOFAST_WINDOWS` | Windows only |
| `TWOFAST_MACOS` | macOS only |
| `__IOS__`, `__ANDROID__`, `HAS_UNO`, `WINDOWS_UWP` | Uno / UWP standard symbols |

Desktop-specific code lives in `src/Project2FA.Uno/Platforms/Desktop/`, with the
Windows-only implementations under `Platforms/Desktop/Windows/`.

## Structural debt

The desktop fork grew a parallel implementation set. It works, and the tests
cover it, but the *shape* misleads readers — human and agent alike — into writing
new code instead of reusing what exists. Recording it here is what keeps that
from repeating; the retirement sequence is in [plan.md](plan.md).

### 1. Desktop naming reflects scope

Desktop implementations now use `Desktop*` names and compile for both Windows
and macOS under `TWOFAST_DESKTOP`. Only `MacOSNative.cs` is genuinely macOS-only
(`#if TWOFAST_MACOS`). The parser suite remains under `tests/MacOS/` for
historical compatibility and is executed by `scripts/test-windows.ps1`.

### 2. Platform abstraction expressed as duplicate type names

`Platforms/Desktop/MacOSNative.cs` and `Platforms/Desktop/Windows/WindowsNative.cs`
provide the same `DesktopNative` seam under mutually exclusive compile symbols.
The Windows file also holds `WindowsCredentialStore`; `WindowsCameras`,
`WindowsQrScanner` and `WindowsScreenCapture` use the shared
`…Services.Desktop` namespace.

The real structure is one desktop capability with two platform implementations;
the shared name makes that seam explicit to readers and tooling.

### 3. Duplicated capabilities

| Capability | Implementations |
| --- | --- |
| otpauth parsing | One `IProject2FAParser` seam. Desktop uses `Services/Parser/StrictProject2FAParser` (strict validation, OCRA + MobileID); mobile uses the legacy-compatible parser. |
| vault encrypt/decrypt | Shared `Services/Serialization/DesktopVaultCodec` owns V4 and delegates explicit V0–V3 compatibility to `CryptoService` / `SerializationCryptoService`. |
| ~~WebDAV client + directory~~ | resolved — the dead `Project2FA.Core/Services/WebDAV/` fork was deleted; `Project2FA.Shared/Services/WebDAV/` is the only implementation |
| ~~bool → constant converters~~ | resolved — `FavouriteToIconConverter`, `ShowCodeToIconConverter`, `FavouriteTooltipConverter` and `TOTPVisibilityTooltipConverter` now derive from `Converters/BoolToValueConverter.cs` and only declare their value pair |
| ~~PBKDF2 `DeriveKey` + `OtpHashMode` switch~~ | resolved — lifted into `Services/Importer/BackupCryptoHelper.cs`, used by the andOTP and 2FAS importers |

`StrictProject2FAParser` is the desktop parser — it bounds input length,
rejects non-default ports, user-info and fragments, validates the Base32 secret
against a round-trip decode, and constrains algorithm, digits and period. Those
checks are covered by the parser suite and are a security control, not a style
difference.

The importer duplication was scaffolding, not format logic. `BackupCryptoHelper`
now owns the `AES/GCM/NoPadding` constants, the `Pkcs5S2ParametersGenerator` call
shape and the `"SHA1"/"SHA256"/"SHA512" => OtpHashMode` switch; each importer
still supplies its own digest, iteration count and payload layout, and keeps its
own upstream attribution header. Aegis was left alone — its slot-based key
derivation shares nothing with the other two.

**Not duplication, despite appearances.** `DesktopFileTransaction` (local
backup → write → rollback), `DesktopRemoteVaultTransaction` (conditional remote
replace, tolerant of a lost response) and `AccountCommitQueue<T>` (in-memory
add/save/revert gate) are three different concerns and should stay separate.
Likewise the thin `AddAccountPageViewModel` / `AddAccountContentDialogViewModel`
over the shared `AddAccountViewModelBase` is the pattern to copy, not a defect.

### 3a. Dead code — removed

`Project2FA.Core/Services/WebDAV/` was a fork of the live shared implementation:
`DirectoryService` (536 lines) differed from `WebDAVDirectoryService` (557) in
~109 lines, mostly a renamed type and a few added state flags, and its
`WebDAVClientService.GetClient()` had its body commented out and returned `null`
unconditionally. Nothing referenced it — and `Project2FA.Core.csproj` had
excluded the whole folder from compilation with `<Compile Remove="Services\WebDAV\**" />`,
so it was not even being built.

Deleted, along with the now-pointless `Compile`/`EmbeddedResource`/`None`
`Remove` items (which also referenced a `Messenger\` folder that no longer
exists). `Project2FA.Shared/Services/WebDAV/` is the single implementation.

### 4. Shared desktop boundary

Shared code uses the conditional `GlobalUsings.Desktop.cs` seam for desktop
capabilities. There are no fully-qualified `Project2FA.Services.Desktop.*`
references scattered through shared view-models and services. The V4 codec is
owned by the shared serialization layer; the desktop project supplies native
and filesystem implementations.

| Shared file | Refs |
| --- | --- |
| `Project2FA.Shared/**/*.cs` | 0 fully-qualified references |

The shared project refers to desktop capabilities by their short names through
the conditional global seam. The implementation inventory remains explicit in
the desktop namespace and is covered by the platform suites.

`DataService.WriteAtomicAsync` (line 1004) is the clearest example: the method
body is an `#if TWOFAST_DESKTOP` early-return delegating to
`DesktopVaultLocation.WriteAtomicAsync`, followed by the UWP `StorageFile`
implementation. Two atomic-write implementations, one file, selected by
preprocessor — the shape an interface is for.

### 4a. Desktop resource seam

Desktop data-file, OCRA, biometric, and QR-import dialogs and status messages
resolve through `DesktopText`, backed by `Project2FA.Shared/Strings/en/` and a
safe English fallback. Native exception diagnostics may retain fallback text
because those messages also serve platform failure reporting and logs.

### 5. View-model extension by platform partial

`DesktopViewModels.cs`, `DesktopOcraViewModels.cs`, `DesktopNewDataFile.cs`,
`DesktopDatafileActions.cs`, `DesktopAccountSave.cs` and `DesktopReset.cs` add
`partial` members to `LoginPageViewModel`, `AccountCodePageViewModel`,
`SettingsPartViewModel`, `NewDataFilePageViewModel` and `DataService` from the
platform folder. `partial` for platform specialization is fine and matches
`SettingPageViewModel.Desktop.cs` in the shared project. The remaining partial
classes contain desktop-only camera, biometric, native credential, and
navigation integration. Pure input validation has moved to shared code;
platform workflows remain partial because mobile has different native services
and lifecycle requirements. OCRA seed decoding and token metadata construction
are shared in `DesktopOcraTokenFactory`. Desktop shell navigation and dialog
roots are provided through `IDesktopShellContext`; desktop platform code does
not reach directly into `App.ShellPageInstance`.

### 6. Shared workflows use an injected shell seam

The boundary cleanup removed both head-coupling routes. Fully-qualified
`Project2FA.Services.Desktop.*` calls and direct `App.ShellPageInstance` calls
from `Project2FA.Shared` are now **0**. Shared workflows use the narrow
`IShellContext`/`ShellContext` seam for navigation, dialog roots, dispatching,
and shell state. Each application head supplies its adapter during startup;
shared code does not reach into a head-owned static.

The desktop adapter also backs `IDesktopShellContext`, so desktop-specific
workflows and shared workflows use the same shell instance. The seam keeps
platform UI handles at the edge while exposing the shared shell view-model as a
typed contract.

## Testing

Console test programs, not a test framework, run through the platform scripts:

- `tests/Desktop/` — `SharedProjectTests`, `ImporterCryptoTests`, `QrFrameTests`,
  `WindowsNativeTests`, `PrivacyTests`
- `tests/MacOS/` — `ParserTests` (shared, cross-platform), `OcraTests`,
  `AccountCommitTests`, `DeviceBindingTests`, `NewDataFileTests`,
  `RemoteVaultTests`, `SecretStoreTests`, `VaultModelTests`, `IconTests`,
  `NativeTests.m`

Suites include source files directly via `<Compile Include="../../../src/…" />`,
so moving or renaming a platform file breaks its test project — update the
`.csproj` in the same change. Including the source this way also means `internal`
types are visible to the suite.

`SharedProjectTests` guards the `.shproj` manifest: `Project2FA.Shared` is a
shared project, so both heads compile the file list in
`Project2FA.Shared.projitems`, **not** the directory. Adding a `.cs` file without
adding a `<Compile Include>` entry means it compiles in no head, silently. The
check also catches case-only mismatches, which work on Windows and on a default
macOS volume but fail on a case-sensitive filesystem.

Release privacy is enforced by `scripts/verify-release-privacy.py`, which runs
inside both build scripts and fails the build on violations.
