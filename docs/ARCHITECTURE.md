# Architecture

## Decision: native Windows first

.NET 8 WPF supplies a real desktop executable, native file pickers, accessibility/keyboard basics and a low-overhead retained UI. `Umbra.Core` has no WPF dependency; filesystem, library, configuration and adapter logic can be reused on other systems. The process job implementation and WPF/XInput frontend are Windows-specific. No web server, browser runtime or privileged service is needed.

## Modules

- `src/Umbra.Core`: domain models, JSON persistence, scanning/detection, metadata, compatibility, saves, diagnostics.
- `src/Umbra.Core/Emulation`: process launch plans, INI writer, per-engine configuration, Windows job ownership and the pinned N64 downloader.
- `src/Umbra.Desktop`: composition and command handling.
- `src/Umbra.Desktop/UI`: independent page builders, wizard, reusable geometry and smoke renderer.
- `src/Umbra.Desktop/Platform`: XInput discovery.
- `config`: replaceable visual identity and empty versioned compatibility catalog.
- `tests`: deterministic regression harness, synthetic executable fixture, opt-in actual N64 probe.
- `scripts`: repeatable build and Inno Setup specification.

## Engine boundary

Mature engines run out of process with argument arrays and `UseShellExecute=false`. Each receives managed settings and save paths. The launcher minimizes after starting the engine, owns its process tree with a Windows Job, awaits exit without polling, updates playtime and restores the library. Native graphics stay in each engine's rendering window; no fragile window embedding is used. An error exit preserves the pre-launch backup and exposes a readable message. Forced shutdown is explicit because it can lose unsaved progress.

`PCSX2 -datapath` appends `PCSX2`; its adapter passes the parent, while storage uses the resulting child. PCSX2 engines forcing portable mode are rejected because portable markers override this boundary. Dolphin receives `-u`. Mupen64Plus receives explicit config, plugin and data directories.

## Configuration precedence

Global settings → conservative exact-content/exact-engine compatibility entry → explicit game presentation override. Compatibility entries match a size + first-64-KiB fingerprint and executable SHA-256; this is an identification aid, not a full-ROM authenticity hash. The catalog contains one evidence-backed official Mupen64Plus GPL demo entry and no commercial-game settings. Automatic catalog entries may not enable Custom/Enhanced or stretch.

Application settings and library JSON are atomically replaced with a preceding `.bak` copy. Malformed JSON is quarantined. Supported older settings migrate forward; future schemas are refused without overwrite. Adapter INIs preserve unknown key/value pairs but normalize formatting/comments. Separate per-game JSON stores explicit overrides.

## Saves

PS2: `engines/PS2/PCSX2/{memcards,sstates}`. GameCube: `engines/GameCube/{GC,StateSaves}`. N64: `engines/N64/{save,states}`. The backend owns the native format; UMBRA never creates synthetic memory cards. Backups occur while the engine is stopped, before launch and after normal exit. Every file is hashed into a manifest. Restore validates and stages all entries before making a recovery backup and promoting files. A mid-promotion I/O failure can leave a partially restored set; the pre-restore ZIP is the recovery point. Extra current files are retained.

## Deliberate tradeoffs

External engines allow mature emulation now but do not expose standardized telemetry or save-state control. Unsupported controls are identified explicitly rather than mapped to unverified hotkeys. XInput supports common devices on Windows; complete SDL and controller-only navigation require further integration. License-safe engine redistribution needs release-specific source/notice packages, so this installer ships only the frontend/runtime; N64 can be downloaded directly from upstream inside the application.
