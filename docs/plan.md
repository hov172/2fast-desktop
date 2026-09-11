# Plan

Current release: **1.5.1**. Last reviewed: **2026-09-10**.

**Done:** Phase 0 (context stack), 1b (dead code), 1c (duplicate collapse),
1d (coverage), desktop namespace correction, and parser seam consolidation.
Remaining structural work is tracked in Phases 2, 4, 5 and 6.

This file states what phase the project is in. Implement the current phase only.
Do not scaffold later phases; do not fold consolidation work into an unrelated
fix. If a change you were asked for requires a later phase, say so and stop.

Rationale for the sequence: the duplication catalogued in
[architecture.md](architecture.md) is not a correctness bug — the shipped app
works and the suites pass. It is a *legibility* bug that keeps producing new
duplication, because the names hide what already exists. So the order is
**make the structure honest first, then merge implementations**. Renames are
reversible and diff-reviewable; merging two crypto or parser implementations is
neither, and goes last with tests written first.

---

## Phase 0 — Context stack ✅ done

Give the repo the six-layer context every contributor and agent reads before
touching code.

- [x] `CLAUDE.md` — conventions, reuse map, security rules, commands
- [x] `docs/architecture.md` — layers, data flow, structural debt
- [x] `docs/ux-flows.md` — the journeys
- [x] `docs/design.md` — tokens, controls, accessibility
- [x] `docs/interactions.md` — motion, state, feedback
- [x] `docs/plan.md` — this file
- [ ] `.mcp.json` — **not created.** No project MCP servers exist; the callable
      surface is `scripts/`, documented in `CLAUDE.md`. Add this file only when
      there is a real server to wire.

**Exit criterion:** a contributor can answer "where does this belong?" from the
docs without reading `App.xaml.cs`.

**Maintenance:** these files are load-bearing. Stale context is worse than none.
Any change that moves a service, adds a platform symbol, changes the vault
format, or alters the shell breakpoint updates the matching layer *in the same
commit*.

---

## Phase 1 — Make the names honest ✅

Pure rename and namespace correction. No behaviour change, no logic moved.

- Renamed the desktop implementations to `Desktop*`. `MacOSNative.cs`
  (`#if TWOFAST_MACOS`) keeps its native wrapper filename.
- Namespace correction completed: `Project2FA.Services.MacOS` is now
  `Project2FA.Services.Desktop`.
  This includes `WindowsCameras`, `WindowsQrScanner`, `WindowsScreenCapture` and
  `WindowsCredentialStore`, which currently sit in a `…MacOS` namespace.
- Rename `tests/MacOS/ParserTests.csproj` → `tests/Desktop/ParserTests.csproj`,
  and move the other cross-platform suites out of `tests/MacOS/`.
- Update `<Compile Include="../../../src/…" />` paths in every affected test
  `.csproj` — the suites include source files directly and will break silently
  in the IDE otherwise.
- Update `scripts/test-windows.ps1` and `scripts/test-macos.sh` paths.

**Verify:** `pwsh scripts/test-windows.ps1` and `./scripts/test-macos.sh` pass;
`git diff` contains no changed statements, only identifiers and paths.

**Non-goal:** merging anything.

### Phase 1b — delete dead code ✅ done

Deleted `Project2FA.Core/Services/WebDAV/` (4 files, ~640 lines): an unreferenced
fork of the live `Shared/…/WebDAVDirectoryService`, a `WebDAVClientService` whose
`GetClient()` was commented out and returned `null`, and an error-handler
interface serving only that copy. `Project2FA.Core.csproj` had already excluded
the folder from compilation, so nothing was built from it; the stale
`Compile`/`EmbeddedResource`/`None` `Remove` items went too.

### Phase 1c — collapse the copy-pasted small stuff ✅ done

- Added `Converters/BoolToValueConverter.cs`. `FavouriteToIconConverter`,
  `ShowCodeToIconConverter`, `FavouriteTooltipConverter` and
  `TOTPVisibilityTooltipConverter` now derive from it and declare only their
  value pair — 4 copies of `Convert`/`ConvertBack` down to 1. Class names and
  `x:Key` names are unchanged, so no XAML moved.
- Added `Services/Importer/BackupCryptoHelper.cs` with the `AES/GCM/NoPadding`
  constants, a digest-parameterized `DeriveKey`, and `ToHashMode`. The andOTP and
  2FAS importers use it; each keeps its own digest, iteration count and payload
  layout. Aegis was left alone — its slot-based derivation shares nothing.

**Verified:** desktop head builds `0 Error(s)` with the warning count unchanged
from baseline (964); `scripts/test-windows.ps1` green — 32 parser checks, 25 OCRA
checks including RFC 6287 vectors, Windows DPAPI round trip.

**Release path verified too.** `scripts/build-windows.ps1 -Runtime win-x64
-SingleFile` completes with exit 0, producing a 347 MB self-contained
`dist/2fast-windows-x64.exe`, and `scripts/verify-release-privacy.py` passes —
no workstation paths, debug symbols or provisioning data in the artifact. This
was re-run after clearing the NuGet caches, so the restore was cold.

### Phase 1d — close the two coverage gaps ✅ done

Both suites were mutation-tested: each was confirmed to fail on a deliberately
broken input before being accepted.

- **`tests/Desktop/ImporterCryptoTests`** — 34 checks over `BackupCryptoHelper`,
  the code Phase 1c moved. `DeriveKey` is cross-checked against .NET's own
  `Rfc2898DeriveBytes.Pbkdf2` for both SHA1 (andOTP) and SHA256 (2FAS) across
  five password/salt/iteration cases, so a wrong digest or key length cannot
  pass; the `AES/GCM/NoPadding` string is proven to mean AES-256-GCM by
  decrypting a BouncyCastle payload with .NET's `AesGcm`; a tampered payload must
  raise `InvalidCipherTextException` (this is what tells an importer the password
  was wrong rather than returning corrupt accounts); `ToHashMode` is pinned
  including its rejections. Verified to fail when `KeyLength` is changed to 16
  and when `"SHA256"` is mismapped.

- **`tests/Desktop/SharedProjectTests`** — the manifest check. `Project2FA.Shared`
  is a `.shproj`: both heads compile the file list in
  `Project2FA.Shared.projitems`, not the directory. A file added to disk but not
  the manifest compiles in *no* head and fails silently — the trap Phase 1c hit.
  The check fails on unregistered files, manifest entries missing on disk,
  duplicates, and case-only mismatches. Verified to fail on all of those.

  **This found a real latent bug.** The manifest declared
  `Services\Importer\AndOTPBackupImportService.cs` while the file on disk was
  `AndOtpBackupImportService.cs`. Harmless on Windows and on a default
  case-insensitive macOS volume; a build failure on a case-sensitive filesystem
  or Linux CI. The file was renamed to match its class, interface and manifest.

Both are wired into `scripts/test-windows.ps1` and `scripts/test-macos.sh`.

---

## The legacy UWP head

Not a phase. `Project2FA/Project2FA.UWP` is inherited Windows Store code that
nothing here builds or ships. It compiles, and it launches to a black window.
Keeping it compiling is worthwhile only because `Project2FA.Shared` compiles into
it, giving the shared layer a second compiler.

Everything about building, running and diagnosing it — the three toolchain
requirements, the 285-error symptom that means the UWP workload is missing, the
signing behaviour and the render failure — is in
**[uwp-head.md](uwp-head.md)**. Read that before spending any time on it.

---

## Phase 2 — Shared desktop boundary ✅

This phase centralizes the desktop boundary. Shared code uses a conditional
global using and the V4 codec is owned by shared serialization, so capability
references are no longer repeated as head-qualified names throughout the shared
layer.

- The shared boundary is declared by `GlobalUsings.Desktop.cs` and the shared
  serialization manifest entry; the reference count for fully-qualified desktop
  names is zero.

**Verify:** `tests/…/WindowsNativeTests`, `DeviceBindingTests`, `SecretStoreTests`
pass on both platforms; no `#if` remains at a call site that only needed to pick
an implementation.

---

## Phase 3 — One OTP parser ✅

The security-sensitive merge. Tests first.

`StrictProject2FAParser` is the strict desktop implementation:
bounded input length, rejects non-default ports / user-info / fragments,
round-trip-validates the Base32 secret, constrains algorithm, digits and period,
and handles OCRA and MobileID. `Project2FAParser` is regex-based and looser.

- `tests/MacOS/ParserTests.csproj` pins every strict-parser rejection and the
  OCRA/MobileID paths; it passed with 32 checks after the move.
- The strict implementation now lives in `Project2FA.Shared/Services/Parser/`
  behind `IProject2FAParser`, and desktop registration selects it.
- The `#if TWOFAST_DESKTOP` parser branch was removed from
  `Project2FA.Shared/ViewModels/Base/AddAccountViewModelBase.cs`.
- Mobile continues to register `Project2FAParser`; the strict desktop behavior
  is selected only under `TWOFAST_DESKTOP`.

**Verify:** parser and OCRA suites pass; a manual scan on each platform imports a
real TOTP, an OCRA and a MobileID code.

**Do not** relax any validation to make the merge easier. Those checks are the
security control.

---

## Phase 4 — Localize the desktop strings ✅

Desktop user-facing dialog, status, scanner, OCRA, biometric, and data-file
text now resolves through `DesktopText` and the shared English resource file.
Native exception fallbacks remain intentionally local because they are also
diagnostic messages used by platform failure reporting.

**Verify:** resource XML has 523 unique keys; focused desktop suites pass; no
desktop dialog flow bypasses the resource seam.

**Non-goal:** translating into the other 14 languages — `en` is the source of
truth and the rest follow the project's normal translation route.

---

## Phase 5 — Rehome view-model behaviour ✅

- Pure OCRA seed decoding, validation and metadata construction now live in the
  shared `DesktopOcraTokenFactory`; desktop view-model code only owns controls
  and navigation.
- Genuine camera, biometric and native credential behavior remains in desktop
  partials, while shell access is supplied through `IDesktopShellContext` and
  `DesktopSession` no longer reaches directly into `App.ShellPageInstance`.

**Verify:** focused shared, parser, OCRA, QR, file-transaction and device
binding suites pass. Full platform UI and hardware acceptance still require
the matching Windows/macOS hosts.

---

## Phase 6 — Vault codec ownership ✅

`DesktopVaultCodec` now lives in shared serialization and owns V4. The existing
crypto helpers remain only for explicit V0–V3 compatibility, preserving the
documented migration path and existing vault formats.

---

## Standing non-goals

- No new `MacOS*`-prefixed types.
- No third implementation of anything in the reuse table in `CLAUDE.md`.
- No change to V4 crypto parameters without a migration path.
- No new reference from `Project2FA.Core` or `Project2FA.Shared` into a head.
