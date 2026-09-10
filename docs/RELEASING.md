# Desktop releases

The current desktop release is `v1.5.1`, following `v1.5.0`. Its app
version is 1.5.1 and build number is 151. This release includes macOS packages and Windows x64/ARM64 ZIPs and standalone executables.
This private distribution does not replace upstream version history.

1. Run the documented platform builds and regression checks. Mac packaging needs
   locally configured signing; never commit private keys or signing profiles.
2. Review the verification report and record hardware checks that were not run.
3. Commit the source and docs, then create an annotated `vX.Y.Z` tag on that commit.
4. Build/package the universal Mac app and Windows x64/ARM64 archives. Upload
   these as release assets, rather than committing binaries to Git.
5. Generate SHA-256 checksums for exactly the attached downloads. Publish the
   installation, compatibility and verification documents alongside the builds.
6. Set release/prerelease status as requested by the maintainer. Always retain
   signing and hardware acceptance limitations in the notes. Verify repository
   privacy and that the tag points to the intended commit.

Use `scripts/build-macos.sh` and `scripts/package-macos-universal.py` following
MACOS.md, and `scripts/build-windows.ps1` or `scripts/build-windows.sh` following
docs/WINDOWS.md. The initial release contains already verified local build outputs.


## Documentation-only maintenance

Keep the released application source tag immutable. Commit expanded guides on
main and identify post-tag documentation updates in the release notes. Refresh
the release's attached documents and documentation archive. If documentation
inside build ZIPs is refreshed, retain the application binaries unchanged,
regenerate checksums, and disclose that archive checksums have changed. Never
replace binaries without a new application release/version.

Before uploading, check relative links in source and in the flattened release
bundle. Include the user guide, both platform guides, compatibility, vault format,
verification, release notes, license, and release process. Leave historical audits
and upstream documentation clearly identified as historical material.

After all platform builds finish, run `python3 scripts/package-release-docs.py` to assemble the current-version guides, refresh the Windows packages with those guides, and checksum all current release downloads.

For 1.5.1, the asset set is the notarized Mac DMG and app ZIP, Windows x64/ARM64
portable ZIPs and standalone EXEs, the versioned documentation ZIP, and SHA256SUMS.
Upload the flattened guides alongside the archive. Verify remote asset digests
against the local files before declaring the release complete.

## Privacy gates

Release targets disable generated debug symbols and map compiler source paths to
`/_/src`. Publish into fresh output directories: deleting PDB files from an older
build does not remove the CodeView paths already stored in assemblies or bundled
EXEs. Run `python3 scripts/verify-release-privacy.py` on final app directories and
standalone EXEs. Documentation packaging invokes the same gate automatically.

Universal Mac packaging requires `APPLE_DEVELOPER_ID` (certificate SHA-1) and
`APPLE_DISTRIBUTION_PROFILE` (local distribution profile path). It signs nested
code with Developer ID, requires Hardened Runtime/timestamps, rejects profiles
with registered devices and scans the signed app before creating its ZIP. Keep
these local signing inputs outside Git. Submit the app and DMG separately for
notarization, staple both, and regenerate the app ZIP from the stapled app.

Use a GitHub noreply identity for new author, committer and tagger metadata.
The 1.4.5 privacy cleanup rewrites prior maintainer identity metadata while
preserving source trees, messages and upstream attribution. This is an explicit
privacy exception to the normal immutable-tag policy. Old application/checksum
assets are withdrawn; source tags and historical documentation remain. Re-clone
after the rewrite instead of merging an old checkout, which could restore the
old history. Existing clones and cached GitHub objects may still retain it.

Developer ID signatures intentionally expose the certificate holder and Apple
team. The maintainer accepts that disclosure; it is needed for this signed Mac
distribution and is distinct from device provisioning lists and private keys.
