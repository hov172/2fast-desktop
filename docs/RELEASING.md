# Desktop releases

The current desktop release is `v1.4.3`, following `v1.4.2`. Its app
version is 1.4.3 and build number is 143. This release includes macOS packages and Windows x64/ARM64 ZIPs and standalone executables.
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

For 1.4.3, the asset set is the notarized Mac DMG and app ZIP, Windows x64/ARM64
portable ZIPs and standalone EXEs, the versioned documentation ZIP, and SHA256SUMS.
Upload the flattened guides alongside the archive. Verify remote asset digests
against the local files before declaring the release complete.
