# Testing and evidence

The final deterministic suite completed **59 passed, 0 failed**, including device-aware SDL translation, applied N64 manual mappings, native state-folder registration, additional disc extensions, save-path hardening and presentation validation. Final rerun evidence is in `test-results-continuation.txt` in the delivery folder. The runner has no external test framework dependency and returns a nonzero exit code on any failure.

Tests cover N64 byte orders, GameCube disc magic, PS2 BOOT2 screening, ambiguous images, script rejection, mismatched consoles, scan cancellation, missing folders, deduplication, persistence, JSON quarantine/recovery, schema migration, future schema refusal, controller mappings, save directories, backup/restore, wrong-console rejection, traversal/tamper rejection, metadata normalization, compatibility enhancement rejection, diagnostic redaction, missing/changed engines, adapter argument arrays, original/enhanced profile settings, all three fixture processes and crash cleanup.

## Real engine tests

Mupen64Plus 2.6.0 Windows x64: official bundled `m64p_test_rom.v64`, identified by the engine as “Mupen64Plus Demo by Marshallh (GPL)”. The actual adapter generated configuration and arguments; the probe added `--testshots 10,60`. It rendered on AMD Radeon RX 5600M / OpenGL 4.6, captured both frames, closed the ROM and returned exit code 0. This proves a demo boots/renders/exits, not game compatibility, input correctness or sustained audio/frame pacing. The test ROM is not shipped in UMBRA deliverables.

PCSX2 2.8.2 Windows x64: `-testconfig` accepted the full adapter-generated version-1 configuration and returned exit code 0 after correcting the appended `PCSX2` data directory. It did not boot a game or BIOS. Synthetic BIOS markers used to exercise configuration generation were not executed as firmware.

Dolphin: official source-level flag/settings inspection and synthetic-process integration only. The official `dl.dolphin-emu.org` Windows download was blocked by the current network/approval policy, so no binary or GameCube title was downloaded. **IMPLEMENTED — RUNTIME VERIFICATION REQUIRED**.

## User-driven game validation

Use legal homebrew or your own backups. For each engine/profile: boot, play, inspect controls, save natively, quit normally, relaunch and confirm native save persistence. Compare Original and Enhanced screenshots, test fullscreen/windowed, disconnect/reconnect controllers, test every player port, deliberately interrupt a disposable session and restore a backup. Never use your only save copy for destructive testing.

## Scope of packaging checks

A clean install into a separate workspace directory on this host can prove extraction, executable startup, reinstallation and uninstall behavior. It cannot prove a truly clean Windows machine, Windows reboot, GPU-driver diversity or antivirus/signature reputation. Those remain release gates.

The same-host clean-directory install, installed executable render smoke, actual Start-menu shortcut target, same-version reinstall and uninstall all completed successfully. A sentinel save in the isolated external data root survived uninstall. The final package refresh adds the custom icon, license notices and documentation; no install/uninstall path rules changed.

See [STATUS.md](STATUS.md) for the complete QA matrix and honest states.
