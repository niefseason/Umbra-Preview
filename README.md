# UMBRA

A native Windows library for PlayStation 2, GameCube, Nintendo 64 and experimental Xbox 360 support. Dark graphite, warm silver, original presentation first.

Xbox 360 uses the separately installed official Xenia engine; no BIOS required. The current integration accepts extracted XEX games and explicitly selected Xbox 360 XDVDFS ISOs. Gameplay is not yet verified. See [Xbox 360 setup and evidence](docs/XBOX360_20260927.md).

**0.1.0 is a functional development preview, not a production-ready release.** The Windows application and installer are built. The N64 adapter has booted the official Mupen64Plus GPL demo and captured rendered frames. PCSX2 has passed an isolated configuration startup check. GameCube and PS2 gameplay, physical controllers, full-speed performance and save compatibility across real games still require validation. See [STATUS.md](docs/STATUS.md).

## Start

Run `Umbra-0.1.0-Windows-Setup.exe` from the delivery folder. It installs for the current user without administrator privileges and creates one Start-menu shortcut. Alternatively, extract the portable ZIP and run `Umbra.exe`. The .NET desktop runtime is included; no development tools are required.

1. Complete or skip the seven-step setup.
2. In **System Status**, choose **Download N64 engine**, or select an existing engine executable. For PS2 select PCSX2 2.8.2; for GameCube select a current Dolphin executable.
3. Import your game files with **Add game**, or choose folders and scan. Generic disc/container formats ask for a console when their headers cannot identify it.
4. For PS2, select your own complete BIOS dump in Settings.
5. Connect an XInput controller. Keyboard defaults are available through the N64 engine if none is connected.
6. Open a game card and press **Play**. Quit in the game to return to UMBRA. Restore UMBRA from the taskbar for quit/force-stop controls if necessary.

The N64 download is fetched directly from the official pinned upstream release, checked against a SHA-256 digest, and installed into UMBRA data. It retains upstream license notices and excludes the upstream demo ROM. No emulators, BIOS, games, keys, or firmware are included in the installer itself.

## Implemented

- Native WPF home, unified library, console filters, favorites, recents, playtime, last played, search and sorting.
- Game/folder import, rescanning, missing-file indicators, relinking moved files, removal without deleting content.
- Original, Clean Original, Enhanced and Custom profile selection; per-game presentation overrides.
- Offline metadata JSON and local cover import; optional Wikidata title lookup uses CC0 data and sends only the title and console name after confirmation.
- Engine launch adapters, executable-change detection, Windows process-tree ownership, crash return and sanitized logs.
- XInput connection checks and configurable global button intent translated for PS2/GameCube; N64 uses automatic SDL mappings.
- Device-aware SDL profile storage (GUID/name/input expressions), validated N64 native mapping generation, hot-plug polling and keyboard focus paths. Physical-device verification remains a release gate.
- Capability-aware startup state slot selection and native screenshot/state folders. UMBRA's pre/post-launch save backup is the supported autosave protection; engine hotkeys and real state round-trips remain runtime work.
- Explicit opt-in Wikidata (CC0) title lookup with attribution and offline JSON fallback.
- Native save/state directory isolation, pre-launch and normal-exit ZIP backups, integrity-checked restoration and recovery snapshots.
- First-run wizard, system diagnostics, report export, developer details, human-readable versioned configuration.
- Self-contained Windows packaging, source and automated regression tests.

## Current limits

- Original presentation is approximate. N64 Rice/GLideN64 configuration support cannot reproduce every game's original video behavior. Clean Original currently shares Original's native settings.
- Physical SDL device enumeration, controller-only launcher navigation and complete device remapping still require runtime validation. N64 manual mappings are translated into the selected device's native SDL expressions when a profile is present.
- No integrated state hotkeys, autosave/restore states, screenshot button, CRT/scanline/overscan effects, integer scaling, separate borderless mode, renderer picker or telemetry-based performance warnings.
- Per-game overrides currently cover presentation only. CPU/audio/memory-card/compatibility patch controls are future work.
- No automatic engine upgrades, remote metadata, cover scraping or populated compatibility database.
- No macOS/Linux UI, code signing, clean-machine VM validation or Windows-restart test yet.

## Data safety

User data lives in `%LOCALAPPDATA%\Umbra`, separate from the application install. `--data <directory>` selects an isolated data root for testing. Uninstallation retains user data. Imported games are referenced, never copied or modified. BIOS is read from the selected source and copied into private engine data so engine sidecar writes cannot affect the original. Removing games does not remove saves or backup history.

Keep an additional copy of your save backups on another drive. Backups are local, capped at 2 GB per snapshot, and are not automatically pruned in this preview. See [Troubleshooting](docs/TROUBLESHOOTING.md).

## Development

Use the .NET 8 SDK and Windows 10/11 x64. Run `scripts/build.ps1`; supply `-InnoCompiler <path-to-ISCC.exe>` to create an installer. See [Development](docs/DEVELOPMENT.md), [Architecture](docs/ARCHITECTURE.md) and [Testing](docs/TESTING.md).

Branding is in `config/branding.json`. Business logic uses stable platform IDs independent of the visible name. Original UMBRA source is MIT licensed. Engines and .NET keep their own licenses; see [Emulation backends](docs/EMULATION_BACKENDS.md).
