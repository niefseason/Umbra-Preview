# Security and recovery review

Review scope: import, path handling, external engine launch, save backup/restore, metadata lookup and installer behavior in the 0.1 preview.

## Findings addressed

- Imported content is treated as data. Supported extensions are allowlisted; ZIP and script files are rejected; files are never executed by scanning.
- Launch arguments use `ProcessStartInfo.ArgumentList`, `UseShellExecute=false`, explicit working directories and a Windows Job Object. No shell command string is assembled from a game path.
- Backend executables must be absolute `.exe` files and are SHA-256 checked at launch. PCSX2 portable markers are rejected because they override the managed data path.
- Save and restore paths reject reparse points, absolute/traversal components, duplicate manifest paths and invalid filenames. Restore fully stages and hashes every entry before a recovery backup and promotion.
- JSON writes are atomic and preserve a `.bak`; malformed files are quarantined. Future schema versions are refused without overwrite.
- Diagnostic reports exclude game titles, imported paths and BIOS contents. Event logs use controlled event codes and platform IDs.
- Metadata lookup is explicit, sends only a title and platform to Wikidata, applies a user agent, and retains CC0 attribution. Offline local metadata remains available.
- Installer is per-user, requests `asInvoker`, and has no uninstall deletion entries for `%LOCALAPPDATA%\Umbra`.

## Residual risks and required review

- A power loss during restore promotion can leave a partial matching set; the pre-restore ZIP is the recovery point. Test recovery on a copy before release.
- The current BIOS structural screen is not cryptographic/authenticity validation; PCSX2 remains authoritative.
- External engine logs can contain paths and are excluded from exported diagnostics. A user may explicitly open them locally.
- Physical SDL device strings and hotkeys require fuzzing against real devices. Continue to reject quotes/control characters and keep the device allowlist narrow.
- Code signing, antivirus reputation, clean VM testing, update transport, rollback and a third-party security review are release gates.

No commercial content, BIOS, keys or firmware are present in the repository or delivery archives. The only downloaded test content used in runtime evidence is the Mupen64Plus GPL demo documented in `EMULATION_BACKENDS.md`.
