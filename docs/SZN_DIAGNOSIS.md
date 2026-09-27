# UMBRA by SZN — diagnosis and development preview, 2026-09-08

This is the existing UMBRA project, confirmed by its owner as UMBRA by SZN. No rebranding or replacement of the working N64 engine was performed.

## Diagnosis

- The actual saved settings used Custom + Stretch=true. GameCube and PS2 executable fields were empty. PCSX2 2.8.2 was extracted only in the workspace; Dolphin was absent.
- The pinned Mupen64Plus bundle runs Rice. GLideN64's AspectRatio key does not control Rice. A 640x480 exclusive-fullscreen signal alone cannot guarantee pillarboxing by the display/driver.
- The PS2 and GameCube adapters are real process/configuration adapters, not merely frontend placeholders. Missing registered executables prevented launching.

## Changes and evidence

| Area | Result | Evidence and limits |
|---|---|---|
| Regression harness | PASS, automated only | 64 passed, 0 failed. First publish failed because a test app held DLLs open; closing that owned test instance resolved it. Final publish succeeded. |
| App screens | PASS, smoke only | Home, Library, Controllers, Settings, System Status rendered; details constructed. File-picker clicks were not verified: computer-use capture/input geometry was unreliable. |
| N64 launch/render/exit | PASS, real GPL demo only | Rebuilt adapter booted the official GPL demo and generated two screenshots, exit 0. A separate full UMBRA import/launch/session probe also exited 0 without forcing. |
| N64 4:3 fullscreen geometry | PASS, narrow runtime test | Actual native window measurements: 1920x1080 monitor, 1440x1080 viewport, offset 240,0. Rice's own screenshot is 1440x1080 and shows the demo. Black backdrop is implemented. This does not verify hardware-accurate rendering, all games, F11 key interaction, mixed-DPI or other monitors. |
| Profiles | PARTIAL | Shared scale/geometry policy; existing Original, Clean Original, Enhanced, Custom remain. Original/Clean use native-oriented backend settings, no forced widescreen patches or speed changes. Clean is currently the same rendering as Original. Rice fullscreen uses a borderless 4:3 render window plus black backdrop, retaining its input/audio plugins. F11 toggle implementation still requires keyboard runtime verification. |
| Dolphin installation | PASS, filesystem/engine startup | Official 2603a downloaded, extracted, hashed, copied into UMBRA data and registered. --version exited 0 without printed text. Actual launch window identified Dolphin 2603a, JIT64, Direct3D 11, HLE. This is a pinned release, not a claim that it is the newest available build. |
| Dolphin Swiss boot | FAIL | UMBRA launched Swiss r2092; Dolphin reported unknown instruction 00000000 at PC 00417068, last_PC 00000000, LR 0027b504. The bounded probe force-closed it after 25 seconds. A separate full-MMU/single-CPU-thread test also timed out; no success inferred. Test-only settings were not applied to the user's configuration. No GameCube gameplay or save claim. |
| PCSX2 registration | PASS, filesystem/config only | Existing official 2.8.2 copied into UMBRA data and registered. Earlier -testconfig evidence remains configuration-only. BIOS/game execution is blocked on user-provided firmware/content. |
| BIOS import | PARTIAL | Import PS2 BIOS copies a screened user file to a hash-named managed location, retains original, guards destination links and changed copies. Current screening is size + ROM markers, not full firmware authentication. PCSX2 remains authoritative. No real BIOS was available; PS2 READY is not asserted. |
| BIOS-free PS2 alternative | PARTIAL | Official Play! 0.72 installed and --version returned 0 with Play! Version: 0.72. System Status offers explicit PCSX2 / Play! selection. Play! uses its own HLE firmware, no Sony BIOS. Its working directory uses a portable marker and a separate Play save subtree. Launch argument/isolation/state rejection tests passed, but no PS2 game boot occurred. Play! native controls/presentation and saves require verification. Enhanced/Custom and PCSX2 startup-state import are rejected for this route. |
| System Status | PARTIAL | Engine file existence and selected hash are checked, with BIOS screening, controller count, writable save storage, graphics and audio limitations. Audio is explicitly runtime-verification-required. Presence never establishes gameplay readiness. |

The command-line --runtime-probe requires --data and a test file. It uses the real library import and game-launch methods, adds N64 engine screenshots at frames 300/600, and requests exit after 20 seconds, force-stopping only that probe after another 5 seconds. --gamecube provides the explicit platform needed for a generic DOL file. A completed probe can still contain a failed/forced backend exit: always inspect the log.

## Reproduction commands

From the workspace root (PowerShell):

```powershell
$env:DOTNET_CLI_HOME="$PWD/work/toolchain"
$env:APPDATA="$PWD/work/toolchain/appdata"
$env:NUGET_PACKAGES="$PWD/work/toolchain/packages"
& work/toolchain/dotnet/dotnet.exe run --project outputs/Umbra/tests/Umbra.EngineProbe -c Release -- "$PWD/work/engine-checks/mupen/Release/mupen64plus-ui-console.exe" "$PWD/work/engine-checks/mupen/Release/m64p_test_rom.v64" "$PWD/work/szn-n64-probe"
# These data folders contain isolated engine settings and demo-only library entries:
& outputs/Umbra-SZN-Preview/Umbra.exe --data "$PWD/work/szn-fullscreen-probe" --runtime-probe "$PWD/work/engine-checks/mupen/Release/m64p_test_rom.v64"
& outputs/Umbra-SZN-Preview/Umbra.exe --data "$PWD/work/szn-dolphin-probe" --gamecube --runtime-probe "$PWD/work/engine-checks/swiss-tar/swiss_r2092/DOL/swiss_r2092.dol"
```

The N64 demo has a conservative windowed compatibility entry. The fullscreen probe has an explicit per-game fullscreen override; do not mistake its earlier windowed run for fullscreen evidence.

## Sources, versions and notices

Archive URL/version/license/SHA-256 records: `../../SZN-evidence/downloads.json`. No engines or test games were added to the frontend package. Full upstream engine trees/notices remain in the managed engine folders. Redistributing a bundled-engine package still requires a component/source/notice audit, including Qt and Dolphin's per-file licenses.

- Dolphin 2603a: https://dl.dolphin-emu.org/releases/2603a/dolphin-2603a-x64.7z ; source https://github.com/dolphin-emu/dolphin/tree/2603a . Upstream COPYING describes mostly GPLv2+ and an aggregate compatible with GPLv3.
  - Archive SHA256: 4CC6D975FE9646ED7326271EC9A2D84B93301DE7CDB525C6B20ED97C0A715BF1
  - Dolphin.exe SHA256: 5E076628B977603DCDE9DDEF5EEDF10B5A9CE0B6208BD4E6DFA9039CDEBF3433
- Swiss v0.6r2092: https://github.com/emukidid/swiss-gc/releases/tag/v0.6r2092 . GPL-2.0 license and bundled Licenses directory retained.
  - Used tar.xz SHA256: 14A6ED9C3F6DD5109CC8CBA5608BB022BA03926B021C242988AC4AAD3234C5D2
  - swiss_r2092.dol SHA256: B844ECE3877621F6256A1EB8D196AF9D0A3B58C3E17BDE7E94D239B5BB668FFF
  - Earlier 7z SHA256: 895AA9D2DD3A7994CAD22ACFF84027E5C7D6348B3369FEE6826B919A08111241. Windows tar reported a CRC error on its ELF entry; that extraction was not used. DOL hashes from the two archives match.
- Play! 0.72, release source 8de4a71f: https://www.purei.org/downloads/play/stable/0.72/Play-x86-64.exe ; https://github.com/jpd002/Play-/tree/8de4a71f . Upstream BSD-style License.txt retained; Qt has separate licensing requirements.
  - Installer SHA256: 466E3BEBC4839726682C95C63004847B43244CBE74FB0AFCBBBA413561FB0F3A
  - Play.exe SHA256: 794C545CC03B79273B9D831586F849F03CC2231F02363B9EBCCCA9055E573F36
  - First silent-install attempt used a mixed-separator destination and created no expected folder. Retrying with a normalized Windows path installed successfully. --version, run from an isolated working folder, returned 0.
- Existing PCSX2 2.8.2, Mupen64Plus 2.6.0 and its GPL demo provenance/hashes remain in ENGINE_DOWNLOAD_RECORD.md. No Sony BIOS or commercial game was downloaded.
- PCSX2's BIOS requirement: https://pcsx2.net/docs/setup/bios/ . Play!'s BIOS-free design and CLI: https://github.com/jpd002/Play-/blob/master/README.md . Rice config reference: https://github.com/mupen64plus/mupen64plus-video-rice/blob/master/src/Config.cpp .

## User validation still needed

1. Open the new UMBRA-SZN-Preview build. In System Status, confirm all engine paths are detected. Original is the saved default; check game-specific overrides if a title still stretches.
2. N64: Add Game, select a lawful game, Play. On 16:9 expect centered 4:3 with equal side borders. Exercise F11 twice, Alt-Tab, normal quit, Original/Clean/Enhanced and audio. Verify unchanged controller mapping. Repeat on your other display/DPI setup if used.
3. GameCube: provide a lawful compatible game/homebrew file; Add Game, select GameCube when prompted, Play. Swiss r2092 is a recorded failing case, not a suitable passing baseline. Verify sound, geometry, controls and native memory-card save/relaunch.
4. PS2: either import your own BIOS and use PCSX2, or choose Use Play! without BIOS. Provide a lawful PS2 game/ELF. For Play!, use Original/Clean Original and clear any PCSX2 startup state. Verify real boot and native save/relaunch; game compatibility is not guaranteed.
5. Physical Xbox/PlayStation/Nintendo/generic SDL, multiple players, N64 manual mapping, gamepad-only navigation, audible sound and accessibility remain IMPLEMENTED — RUNTIME VERIFICATION REQUIRED where implementation exists. The current launcher detects XInput only; SDL enumeration and gamepad-only navigation are not fully implemented, so record failures rather than assume readiness. See CONTROLLER_TEST_PLAN.md for the evidence form.
6. Signing, clean VM, upgrade/uninstall retention and production release gates remain open. No restart, signing, purchase or publication was performed.

**This remains a development preview.**
