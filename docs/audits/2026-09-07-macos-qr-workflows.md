# macOS QR and workflow audit — 2026-09-07

Scope: Uno macOS camera/screen capture, QR ingestion, Deepnet MobileID/OCRA interoperability, account persistence, device binding, setup/reset/password workflows, local writes, and restored dependency advisories. This was source review and targeted regression testing, not penetration testing or a live Deepnet server certification.

## Confirmed defects fixed

| Finding | Impact | Change / evidence |
|---|---|---|
| Native scanner accepted only `otpauth://totp` | OCRA and MobileID QR codes were discarded before import. | Native scanner now returns decoded text to the managed validator. Native tests exercise OCRA and MobileID payloads through the real frame-processing method. |
| Screen frames shared the camera throttle | The first static screen frame could be dropped immediately after a camera frame; unchanged frames were then ignored. | Complete screen frames bypass camera throttling. Regression forces a recent camera timestamp, supplies exactly one screen frame, and verifies both decoding and nonempty preview contents. |
| Poor screen-selection feedback | Scanner could remain behind selected content or appear blank without explanation. | Bring preview forward, clear stale images, reject invalid capture dimensions, update resolution on source changes, and show an eight-second no-frame hint. |
| Capture lifecycle races | Late camera permission/runtime notifications could interfere with screen capture; repeated stop requests could race cleanup. | Ignore camera callbacks in screen mode; stop cleanup is idempotent. Missing/denied camera retains the screen-scan option. |
| MobileID envelope mistaken for raw key material | Decoding `seed` as a Base64 HMAC key would produce wrong OTPs. | Decode authorization-code-protected envelope using the existing BouncyCastle dependency, verify seed/serial checksums, derive the client key, and preserve embedded profile settings. No vendor app code is shipped. |
| MobileID OTP confused with OCRA response | MobileID has a normal time-based OTP with fixed truncation offset 16, separately from OCRA challenge response with dynamic truncation. | Imported MobileID tokens show normal OTPs; their context menu offers OCRA challenge entry. Supported profile: version 1 time-based, requested SHA1/QN08/T1M OCRA suite. Unsupported legacy/event/mutual-authentication profiles fail explicitly. |
| Device-binding flag missing from ordinary vault model | A generic portable import would lose that flag. | Preserve a binding to an app-specific, nonsynchronizing Keychain identifier. Generation/copy/challenge reject a mismatched Mac. This is application-enforced binding; the seed is still encrypted in the vault, not a nonexportable Secure Enclave key. |
| Push-registration metadata caused wholesale rejection | The supplied QR also contains optional device/push information. | Import offline OTP/OCRA, discard registration URLs/credentials, and clearly label push approvals as not enrolled. The app does not contact those URLs. |
| TOTP URI without issuer produced empty required label | Valid account could not be saved. | Use the account label as the service label fallback. |
| Copy tooltip appeared before successful copy | Canceling an OCRA challenge could still show “copied.” | Show copy confirmation only after successful copying. |
| macOS factory reset did not clear settings | Reset removed credentials but retained setup. | Clear preferences and in-memory session/account state under the collection lock, navigate to Welcome, preserve vault files and device-binding identity. Both reset entry points use this path. |
| Change Password treated a full vault path as a folder | Existing local vault could fail password validation. | Reuse the same full-path/legacy-folder resolver as normal loading. |
| Write-error recovery reacquired its held semaphore | A write failure could hang indefinitely. | Recovery uses the caller's existing lock, without nested acquisition. |
| Temporary file was on app storage volume | Replacement of a vault on another volume was not reliably atomic. | macOS creates a unique private temporary file alongside the destination, flushes, renames, and cleans up by the original temporary path. Failure tests verify the other vault remains unchanged and no temp file remains. |
| Filename extension checked with substring matching | Names containing `.2fa` in the middle were handled inconsistently. | Use case-insensitive suffix matching. |

## Verification

- 110 automated assertions: 32 QR parser, 5 device identity, 25 OCRA, 26 form/location/write, 9 secret-store, 13 native Keychain/Vision/frame-path checks.
- 12 additional assertions against the compiled app's actual encrypted serializer and model clone, preserving MobileID type, suite, timing, checksum flag, device binding, and seed.
- The user-supplied screenshot decoded with Apple Vision and passed MobileID envelope/profile validation. Its payload was passed directly in memory to the validator; no raw payload, seed, authorization code, account name, or registration credentials were printed or placed in fixtures. No account was added by this diagnostic.
- Independent synthetic Python HMAC/envelope fixtures check the requested OCRA profile and MobileID key/OTP behavior; RFC 6287 vectors check the OCRA implementation.
- Release build and signed bundle verification are required before distribution; logs are included alongside the delivered build.
- NuGet advisory query for restored desktop direct/transitive packages reported no vulnerable packages from the configured nuget.org and CommunityToolkit feeds.
- Native frame-path tests verify preview delivery, but a fresh end-to-end system-picker interaction and live camera session were not completed during this audit. Earlier builds had interactive capture tests; those do not certify this build.
- No live Deepnet server verification, push registration, mutual authentication, or physical Touch ID test was performed. Touch ID is not enrolled on this machine.

## Unresolved findings

1. **Resolved for V4; retained for explicit legacy compatibility — vault encryption.** New macOS vaults use AES-256-GCM with a fresh salt/nonce and PBKDF2-SHA256/600,000. Modern credential identities are random, and login authenticates the file. Explicit migration preserves and verifies the complete model and keeps a legacy encrypted copy. Older clients cannot read V4. Unmigrated files and old copies retain their legacy weaknesses; the application does not silently migrate shared files. See [format and recovery](../MACOS-VAULT-FORMAT.md).
2. **Implemented — local and WebDAV password transactions.** Local commits retain recovery copies and roll back credentials/file writes. HTTPS WebDAV uses strong ETags, conditional PUT, read-back verification, lost-response reconciliation, and conditional compensation. Another device's conflicting content is preserved; uncertain recovery retains the original encrypted copy. Server/local commits cannot be globally atomic. Fault-injection checks pass; a live WebDAV server acceptance pass remains external.
3. **Isolated — legacy crypto context.** Legacy serialization is synchronous under a shared lock, with thread-local key/IV copies cleared afterward. V4 uses operation-owned .NET AES-GCM instances and bypasses the legacy converters. Concurrent round trips verify isolation and buffer ownership.
4. **Open diagnosis — reported automatic password whitespace.** No automatic insertion was reproduced. The form preserves exact input, reports trailing whitespace, and removes whitespace-only PasswordBox markup. Do not silently trim existing passwords.
5. **Coverage limitation — account workflows.** Reset and password-path fixes were compiled/source-reviewed; real interactive reset/change-password failure recovery was not exercised against the user's vault. No destructive tests were performed on user files.

## Interoperability sources

- RFC 6287: https://www.rfc-editor.org/rfc/rfc6287.html
- Deepnet QR provisioning workflow: https://wiki.deepnetsecurity.com/display/DualShield6/Install+MobileID+token+by+QR+code
- Deepnet OTP workflow: https://wiki.deepnetsecurity.com/display/DualShield6/Use+MobileID+in+OTP+Verification
- Public MobileID reference distribution (6.0.17 APK), inspected locally for format interoperability without running the app: https://support.deepnetsecurity.com/download/mobileid/mobileid-6.0.17.apk

Legacy RC4/MD5 is confined to decoding the vendor envelope; it is not used for new vault encryption. Public reference-package analysis is ignored under `build/interoperability/` and is not bundled with 2fast.

## Follow-up: account review navigation

The user's diagnostic identified `NavigationPath` failing before account review: `AddAccountPage` was registered only on iOS/Android. macOS QR import, manual OCRA setup, and manual account entry all require that route. The macOS startup now registers the review page and its view model.

A compiled-app regression invokes the actual startup registration method and resolves the review route. It reproduced the missing-key exception in the prior build. This exercises registration without opening or modifying a user vault; it does not certify the full interactive account-save workflow.

## Follow-up: controls after account import

Confirmed source defects and applied fixes:

- Sidebar navigation had no `ItemInvoked` handler. Connected Accounts, Settings, Data file, and About; removed stale selected-item suppression that could prevent returning to a destination.
- Sidebar query routes used Uno's unimplemented `WwwFormUrlDecoder.GetEnumerator`. A compiled-app test reproduced the exception. macOS now decodes query pairs with .NET, splitting at the first equals sign and decoding each component once.
- The desktop edit dialog contained only commented-out form controls and had no Save/Cancel buttons. Restored service, account, icon, and notes fields, attached the dialog to the shell XAML root, and used a button deferral to await its save command. Editing metadata preserves category assignments.
- Account menu actions depended on inherited popup DataContext. All 12 actions across both account templates now carry the model explicitly; OCRA action visibility also uses a typed binding.
- Settings Back had no command, and data-file/about route selections had no corresponding content. Wired Back and added data-file information and About content.
- Account timer startup ran twice on macOS. Removed constructor startup, stop timers on unload, invalidate pending starts, and prevent overlapping ticks.
- OTP refresh depended on hitting a narrow rollover window. macOS recomputes from the clock each tick and immediately before copying; delayed ticks no longer leave the previous period's code displayed. OCRA accounts remain challenge-driven.
- Closed copy-confirmation teaching tips accumulated in the page. Remove them on close.
- A manual-entry helper used the wrong navigation parameter name. Aligned it with the account-review initializer.

Validation: Apple Silicon build passed. Compiled-app tests preserve the 12 encrypted-model checks and now resolve account review, Accounts, three sidebar routes, and query decoding (including plus, escaped equals, and single decoding). UI contract checks verify the actual compiled sidebar handler, active editable/closable dialog fields, and all 12 explicit account-action bindings. These checks do not simulate a live mouse click or certify the complete UI workflow. No live app process was available when checked; no user vault was changed by testing.

Remaining limits: MobileID/OCRA QR export deliberately shows an explanation because an ordinary TOTP QR cannot preserve those settings. The later V4 update addresses vault encryption for new and explicitly migrated files; legacy compatibility is preserved. This follow-up repairs identified UI defects; it is not a claim that every upstream feature or every failure path is fully implemented/tested.

Final architecture verification: both arm64 and x64 builds passed; expanded compiled-app and UI-contract checks passed on arm64 and on x64 under Rosetta.

## Follow-up: repeated additions, QR image, and discoverability

- Confirmed that adding a model fired an async-void collection-save handler, then immediately navigated to Accounts where startup reloaded the file. Added an awaited, serialized account-commit path. The write method now returns success/failure, duplicate submissions are rejected, and a failed/throwing save removes the unsaved candidate so retry remains possible. This fixes the add/navigation ordering; the later data-file update also addresses local password-change transactions.
- Confirmed ZXing.Net.Uno's barcode display control has no Skia/macOS image-attachment branch. Replaced it with a PNG rendered using ZXing pixel output and Skia, bound to a standard Uno Image on a white background with quiet zone. An actual PNG produced by the compiled app decoded to the exact synthetic account URI with Apple Vision.
- Added visible camera/screen/manual/OCRA/settings toolbar buttons and Copy/Edit/View QR/OCRA actions on each card. The original menus remain available. Account names stack vertically for a stable narrow layout; the add menu uses Uno's popup instead of a native popup.
- Regression coverage now includes delayed/repeated account commits, duplicate clicks, save failure/exception rollback and retry; compiled renderer output; dialog image binding; and 20 explicit account-action bindings. These are automated component/source-contract checks, not a live interactive acceptance test of every control.

## Account-card layout regression correction

The new action row was placed before Grid.ColumnDefinitions. Uno generated the action controls but omitted subsequent account detail controls. Moved all grid definitions before content and changed card rows to Auto sizing. Added a compiled-IL regression requiring both generated card templates to construct their text and button controls, plus XAML checks for label/account/code/countdown bindings and definition order. The prior shipped universal build fails the generated-card check; the corrected build passes. ARM and Intel packages were rebuilt. This is generated-control verification, not a live screenshot comparison.

## Icon-name fallback

Icon lookup required an exact name while the initials converter suppressed initials for every nonempty name. Changed both converters to share normalized lookup (case, spaces, hyphens, underscores) and keep initials when no matching icon exists. Added edit-field suggestions using the existing icon collection; the “not found” suggestion cannot replace the selected name. Sixteen linked-converter checks cover known variants, missing/empty names, glyph resolution, and initials fallback.

## Data-file management completion

The macOS Data file page now exposes Rename, Move, Backup, Change password, Open, and New controls. Rename/move update the selected path only after creating the destination; existing destinations are never overwritten. Backups and recovery copies use owner-only file permissions. Touch ID is revoked for password/path changes and can be enrolled again after success. Password reveal and matching validation are present in the restored dialog. Account edit and favorite state now roll back after failed saves.

Temporary-file tests cover password commit, failed writes, credential rollback, retained recovery copies, filename validation, and backup collisions. Compiled UI contracts check all six data-file handlers and the password dialog. These are automated checks, not a completed interactive acceptance pass. No user vault was modified by these checks. The later WebDAV update supports conditional password transactions; moving a remote vault’s local copy explicitly switches to local use. Cleanup failure can leave an encrypted recovery copy after a successful change.

## Remaining-items implementation

Added authenticated V4 encryption, explicit migration, random modern vault credential identities, authenticated login and backup import, downgrade rejection, and complete password-change integration. Replaced timestamp-based remote overwrites with conditional writes and verified downloads. WebDAV changes require HTTPS, connectivity and strong ETag support. Migration is an explicit compatibility decision in the UI. See the format document for exact guarantees and retained legacy copies.

Automated checks cover complete V4 metadata/empty-vault round trips, V0–V3 migration, tamper/wrong-password/whitespace handling, downgrade rejection, conditional create/update, HTTP conflicts, lost responses, credential failure rollback and recovery-copy retention. New native checks confirm this Mac reports -7 (Touch ID not enrolled), refuses biometric enrollment and leaves no password stored. No enrolled fingerprint or physical Intel machine is available; Rosetta is not physical Intel certification. The supplied user vault remains untouched by testing.

Final verification: both packaged runtimes passed the compiled workflow, model, crypto and V4 regression suites; x64 ran under Rosetta. The external test runners require the bundle's `Contents/MacOS` directory in `DYLD_LIBRARY_PATH` to find ICU; the application host already resides there. Both universal launcher slices passed routing checks, and the final bundle passed deep/strict signature verification. Full native/file/parser/OCRA/WebDAV suites passed. Successful fingerprint unlock, physical Intel acceptance, and a live WebDAV-server acceptance pass remain unverified.
