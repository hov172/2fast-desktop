# Desktop privacy remediation — 2026-09-08

Scope: the five findings from the private-repository review, addressed for
2fast 1.4.5 (build 145). No real vault contents or credentials were used in tests.

| Finding | Remediation and verification |
| --- | --- |
| Registered device identifiers in old Mac profiles | Universal packaging now requires an all-device distribution profile and rejects registered-device lists. The final app scan checks all nested profiles. A development-signed intermediate app was correctly rejected by this gate. |
| Workstation paths in binaries | Release builds disable symbols, clear stale adjacent PDBs, exclude published PDBs and map source paths to `/_/src`. Final package scans include managed assemblies and both bundled Windows EXEs, not just separate symbol files. |
| Personal Git identity | Maintainer author, committer and tagger metadata is rewritten to the GitHub noreply identity. Original source trees, commit messages, dates, topology and upstream attribution are preserved. New commits use the same noreply identity. |
| HTTP credential transmission | Client construction and server-status requests reject HTTP before connecting. Login endpoints require the configured HTTPS origin; token POSTs do not follow redirects. Focused tests exercise both the helper and the compiled production client against loopback with dummy data. |
| Private data in diagnostics | The shared logger permits only UTC timestamps, exception type names and numeric HResults. Freeform messages, exception messages, stack traces and exception Data/Source are omitted. Nested-exception tests verify synthetic secrets and paths do not appear. Settings opens the new redacted log. |

The app removes only its own legacy diagnostic file when it next writes a log.
Previously exported log copies and already downloaded releases cannot be recalled.

## Distribution checks

The release process requires the final privacy scan, deep Developer ID signature
verification, separate accepted Apple notarization submissions for app and DMG,
stapled tickets, and a distribution check on the app extracted from the final ZIP.
The native universal launcher is tested in both CPU modes for runtime selection,
argument forwarding and exit status. Vault regressions cover authenticated
round trips, wrong passwords, tampering, legacy migrations and unlock/navigation
contracts. Windows bundle checks inspect architecture, embedded dependencies and
application/About version metadata; Windows hardware execution is not performed
on this Mac host.

Older application and checksum assets are withdrawn from their releases. Their
source tags and historical documents remain available. Release asset hashes are
compared with the final local files after upload.

## Accepted disclosure and limits

The maintainer explicitly accepts the certificate holder's name and Apple team
identifier in Developer ID signatures. Valid signed and notarized Mac downloads
retain that identity. Distribution profiles contain no registered-device list.

Rewriting reachable Git history does not erase other people's clones, existing
downloads or cached/unreachable GitHub objects. Re-clone after the rewrite to
avoid restoring the previous history. The private local rollback bundle is
ignored and is not uploaded. Repository visibility is unchanged by this cleanup.
The scans target the identified privacy findings; they are not a guarantee that
all possible private information or security vulnerabilities have been found.

## 1.4.5 validation results

- Focused HTTPS/origin/redirect and redaction tests: passed. Compiled production
  constructor and server-status checks rejected HTTP before connecting.
- Mac vault, unlock/navigation, generated XAML and crypto regression checks: passed.
- Universal launcher arm64/x86_64 dispatch, arguments and exit status: passed.
- Mac signature checks: all 37 Mach-O files use Developer ID, secure timestamps
  and Hardened Runtime; all three app bundles pass deep signature verification.
- Apple app submission `018cb0c4-089e-4c1b-9a3c-1a63d3c6f922`: Accepted.
- Apple DMG submission `abea780a-2fee-45dd-b3a0-c291d3259539`: Accepted.
- App/DMG tickets stapled and validated. The final ZIP's extracted app passed
  `syspolicy_check distribution`.
- Windows x64/ARM64 portable and standalone publishes: passed. Bundle inspection
  confirmed each architecture and its embedded app/runtime/camera components.
  All four application assemblies report version 1.4.5 and About build 145.
- Final binary privacy scan: no detected maintainer workstation paths, PDB/MDB
  files, private-key/vault files, or registered-device provisioning lists.

These are automated checks on this build host; the hardware limitations above
remain applicable.
