## 2026-09-27 Xbox 360 integration

Added experimental Xbox 360 library/import/Xenia adapter, native XInput/fullscreen and isolated content backup support. Official master v1.0.2844 installed on D:, hashed and registered. 112 automated tests pass; desktop publishes. Actual Xenia initializes D3D12 on RX 5600M, but no game is available; all gameplay/input/audio/save behavior remains **IMPLEMENTED — RUNTIME VERIFICATION REQUIRED**. BIOS-named BIN found on D: and left untouched; Xenia needs no BIOS. See [XBOX360_20260927.md](XBOX360_20260927.md) for sources, hashes, limits and validation. Development preview only.

## 2026-09-27 PS2 engine binding correction

Fixed rejected SDL face-button strings using official PCSX2 2.8.2 native names. 109 automated tests pass; desktop published on D:. Actual isolated MVC2 executes with zero Invalid binding warnings (probe terminated after close timeout; no physical-input or clean-exit claim). Dragon Ball Z tuning deferred and unchanged. Physical response remains **IMPLEMENTED — RUNTIME VERIFICATION REQUIRED**. See [PS2_BINDINGS_20260927.md](PS2_BINDINGS_20260927.md).

## 2026-09-26 Dragon Ball Z image verification

Full local disc-image SHA-1 and size match Libretro's Redump-derived USA SLUS-21678 record. ISO volume length agrees. User confirms stretch succeeds and characters are sharper; menu/background fading and fuzzy laptop-speaker audio persist at approximately 100% speed. PCSX2 logs confirm built-in title graphics fixes applied. No replacement image or further settings changes justified by this inspection. See [DBZ_IMAGE_CHECK_20260926.md](DBZ_IMAGE_CHECK_20260926.md). Audio/remaining graphics diagnosis: **IMPLEMENTED — RUNTIME VERIFICATION REQUIRED**.

## 2026-09-26 PS2 presentation and audio tuning

User reports BIOS-backed PS2 worked well, with audio/visual quality concerns. Added explicit per-game Custom resolution/stretch/fullscreen/VSync and PCSX2 sound buffer/speed overlay controls with working-profile rollback. 107 automated tests pass; UI smoke passes. Actual isolated PCSX2 executes DBZ SLUS-21678 and logs the new 100 ms buffer; probe required termination. DBZ working setup backed up before selecting 2×/Stretch/100 ms for testing. Audible improvement, sustained performance and stretch rendering remain **IMPLEMENTED — RUNTIME VERIFICATION REQUIRED**. See [PS2_PRESENTATION_20260926.md](PS2_PRESENTATION_20260926.md). Development preview.

## 2026-09-26 PS2 input and BIOS follow-up

[PS2_INPUT_BIOS_20260926.md](PS2_INPUT_BIOS_20260926.md): Play! keyboard-only input diagnosed; managed Xbox mapping and renderer selection added. User's existing BIOS copy verified; default switched to PCSX2. Actual isolated PCSX2 accepted USA v01.60, executed MVC2 game ELF and detected Xbox via SDL. Physical input/graphics/save roundtrip remain **IMPLEMENTED — RUNTIME VERIFICATION REQUIRED**. 104 automated tests pass. Launch UMBRA now targets D:\Umbra-PS2-Preview. Native saves untouched; development preview.

## 2026-09-26 modern stick tuning

Added per-game horizontal/vertical response, separate movement threshold and camera deadzone, reset, working-profile capture/restore support, and live XInput five-second drift check. Existing values and native saves unchanged. 102 automated tests pass; GPL-demo real engine startup retains tuned native values, exit 0. Physical feel/drift remains **IMPLEMENTED — RUNTIME VERIFICATION REQUIRED**. See [STICK_TUNING_20260926.md](STICK_TUNING_20260926.md).

## 2026-09-26 working setup protection

User confirms startup sizing works. Added desktop Launch UMBRA shortcut to existing external build, per-game working-profile capture/restore/undo, and explicit return to global controller settings. GoldenEye/Perfect Dark current profiles captured and copied to external drive; native saves unchanged. 99 automated unit/fixture tests pass; self-contained publish succeeded. See [WORKING_PROFILES_20260926.md](WORKING_PROFILES_20260926.md) for scope and evidence. Development preview remains in effect.

## 2026-09-26 Perfect Dark and shared modern profiles

[PERFECT_DARK_20260926.md](PERFECT_DARK_20260926.md): user confirms GoldenEye Xbox gameplay worked. Added automatic/shared modern 1.2 controls for GoldenEye/Perfect Dark and per-game renderer selection. 91 unit/fixture tests pass; UI smoke passes; Perfect Dark boot with Glide64mk2 reaches intro logos and exits 0. Repeated-menu correction and Perfect Dark physical controls remain **IMPLEMENTED — RUNTIME VERIFICATION REQUIRED**. New external-drive preview D:\Umbra-Modern-Preview. Perfect Dark override backed up and preselected; GoldenEye and native saves unchanged.

## 2026-09-26 GoldenEye dedicated preset

See [GOLDENEYE_CONTROLS.md](GOLDENEYE_CONTROLS.md) for setup, constraints and evidence. Added SDL device-aware 1.2 Solitaire movement/look separation, LT aim/RT fire, selectable native action/weapon buttons and vertical inversion. 85 unit/fixture tests pass; connected Xbox SDL discovery and real GPL-demo startup/config persistence pass. GoldenEye gameplay remains **IMPLEMENTED — RUNTIME VERIFICATION REQUIRED**. Separate preview: D:\Umbra-GoldenEye-Preview. C: is critically low on space; free space before gameplay/save backups. No installed app, user saves or game overrides changed.

## 2026-09-26 source review

Latest review: [REVIEW_20260926.md](REVIEW_20260926.md). Fixed Dolphin action-trigger overlap, null SDL profile recovery, custom deadzone retention, and misleading control/readiness labels. Four new regressions failed before the fixes; all **78 unit/fixture tests pass** afterward. Separate self-contained preview: `../Umbra-Review-20260926`; isolated UI smoke passed with malformed device entries and a custom deadzone. No new gameplay evidence, downloads, or user-data changes. Physical input remains **IMPLEMENTED — RUNTIME VERIFICATION REQUIRED**.

Correction to earlier entries: the Xbox action preset affects N64/GameCube only; PCSX2 uses its manual/native mappings and Play! manages controls separately. Two sticks do not establish camera support in a title. N64 X/Y are unassigned in action mode. Earlier GoldenEye/Perfect Dark suggestions and statements promising aim/shoot/jump are not verified compatibility evidence.

## 2026-09-08 SZN continuation

## 2026-09-08 Xbox/display follow-up

Fixed two causes of the reported Xbox/N64 instability: the default SDL profile now uses the Xbox joystick Start button and analog trigger axis for Z (instead of treating the trigger as a digital button), and logical remapping resolves from an immutable device snapshot so an A/B swap cannot cascade into other controls. The N64 4:3 fullscreen wrapper now changes the native window only when its geometry or style actually differs; repeated timer ticks no longer resize the SDL window. Per-game Custom profiles retain the global stretch preference, so Custom can use the same full-screen stretch setting. Build and regression harness completed successfully (69 tests passed). These are configuration and fixture results; physical Xbox behavior and real gameplay remain **IMPLEMENTED — RUNTIME VERIFICATION REQUIRED**.

Follow-up: added 20% Dolphin main/C-stick deadzones, a 25% N64 SDL analog deadzone, and thresholded N64 C-stick digital directions to suppress resting-stick drift and phantom action presses. Custom fullscreen N64 now uses a centered monitor viewport when stretch is enabled; Original remains centered 4:3. Publish rebuilt successfully; the currently running installed process must be closed before replacing its locked binaries.

Xbox mapping follow-up: aligned the default N64 profile with the official Mupen64Plus SDL Xbox One/360 entries (A=button 0, B=button 2, X/Y C-button fallbacks, Start=button 7, Z=axis 4+, shoulders=buttons 4/5, C-stick axes 2/3 with 24000 thresholds). This addresses the observed wrong-direction and phantom-button symptoms at configuration level. Physical stick calibration and gameplay remain **IMPLEMENTED — RUNTIME VERIFICATION REQUIRED**.

2026-09-08: An intermediate axis swap was reverted after upstream comparison. The official Mupen64Plus SDL Xbox profile uses left-stick movement on axes 0/1 and right-stick C-buttons on axes 2/3; the profile now follows that layout again. The earlier swap was the reason a right stick could drive movement. No physical runtime result is claimed until this corrected build is tested.
Upstream research also confirms that Mupen64Plus intentionally maps N64 C-buttons to a modern controller's right stick; those four inputs remain game-defined actions. UMBRA therefore cannot promise a universal second camera for N64, while Dolphin/GameCube has a real second analog stick. The Controllers page now shows a neutral XInput snapshot (raw left/right stick and trigger values) to identify drift instead of guessing. Corrected publish: `artifacts/app-xbox-official`.

Engine-binding verification: Mupen64Plus documents mode 0 as fully manual and mode 2 as fully automatic. UMBRA previously emitted mode 2, which caused the engine to replace its mappings at startup. The adapter now emits mode 0, device 0/-1, native `Start`, `A Button`, `B Button`, `X Axis`, `Y Axis`, and cleared C directions when requested. An actual Mupen64Plus 2.6.0 GPL-demo launch completed with exit 0 and two screenshots; the post-launch config retained mode 0, `X Axis = axis(0-,0+)`, `B Button = axis(5+,24000) button(2)`, and blank C directions. This verifies startup/config persistence only; physical input and title behavior remain runtime verification required.

2026-09-08: Added the explicit Xbox action layout requested by the user. N64 maps left stick to movement, right stick to C/camera directions, A to jump, LT to Z/aim, RT to B/shoot, and LB/RB to L/R shoulder actions (weapon cycling where the game exposes those actions). Dolphin maps A jump, RT to B/shoot, LT to Z/aim, and LB/RB to L/R shoulder actions; X/Y remain available for game-defined interact/menu actions. The setting is visible on the Controllers page and defaults on for new profiles. 56 core tests pass; this remains configuration evidence until physical gameplay is retested.

2026-09-08: User runtime feedback showed the N64 title assigns C-buttons to fire/back/sidestep, so mapping the right stick to C-buttons cannot act as a universal camera and causes those actions. Added a per-profile “N64: map right stick to C-buttons” switch, default off; GameCube keeps its native second-stick mapping. Neutral N64 analog deadzone raised to 20000 to suppress the reported startup drift. 57 automated tests pass. This is an N64 hardware/game control limitation, not a production gameplay verification.
The adapter now also clears all stale C-direction axis keys when the option is off, so an earlier generated config cannot continue firing actions after the profile is changed. This is ready for a fresh-app runtime check; the installed process may still be running an older binary.

See [SZN_DIAGNOSIS.md](SZN_DIAGNOSIS.md) for the latest scoped evidence: 64 unit/fixture tests pass; N64 demo fullscreen viewport measured 1440x1080 centered on 1920x1080; Dolphin and PCSX2 installed/registered; Swiss boot failed; optional Play! BIOS-free path implemented and version-startup checked only. Firmware/game/controller/audio/graphics limitations remain explicit. Earlier PASS labels below retain only their stated historical test scope.

# Milestone status — UMBRA 0.1.0

States are scoped to the test performed. **PARTIAL / IMPLEMENTED — RUNTIME VERIFICATION REQUIRED** means code exists but the stated runtime requirement has not passed. This is not a production-ready release candidate.

| Component | State | Test performed / result | Known issues | Next action |
| --- | --- | --- | --- | --- |
| Architecture/source | PASS | Modular .NET core and WPF desktop compile | Windows UI only | Keep stable core boundary |
| Frontend | PASS | Home/library/controllers/settings/status rendered; details constructed | Full interaction/accessibility audit outstanding | Keyboard/controller navigation QA |
| Library | PASS | Header tests, scan, cancellation, dedupe, persistence, missing paths, and `.chd/.cso/.gcz/.ciso` extension fixtures | Real container parsing still needs legal sample media | Add legal container samples and exercise relink picker |
| Config/recovery | PASS | Atomic JSON, .bak recovery, quarantine, migration/future-schema tests | Semantic damage may require manual repair | Expand schema validation |
| N64 launch | PASS | Official Mupen64Plus 2.6.0 GPL demo rendered frames 10/60; exit 0 | Narrow demo only; no gameplay/input/performance claim | Legal game/native save/controller QA |
| N64 graphics | PARTIAL | Rice settings generated and demo rendered | Per-title video modes and Enhanced comparison unverified | Presentation comparison matrix |
| N64 engine downloader | PASS | Live official download, SHA-256 validation, extraction and no-game-content check passed | Network needed for installation only | Revalidate any future release pin |
| PS2 configuration | PASS | Official 2.8.2 Windows x64 archive hash recorded; full adapter-generated -testconfig exited 0; appended data subfolder accounted for | Does not validate a real BIOS or render a game; install record documents isolated extraction | Real user BIOS + legal test game |
| PS2 gameplay | PARTIAL | Fixture launch passed | IMPLEMENTED — RUNTIME VERIFICATION REQUIRED | Boot/native save/relaunch |
| GameCube gameplay | PARTIAL | Source settings and fixture launch checked | IMPLEMENTED — RUNTIME VERIFICATION REQUIRED; Dolphin 2603a installed; Swiss r2092 boot failed (unknown instruction); see SZN_DIAGNOSIS.md | Download official build and boot legal test content |
| Controllers | PARTIAL | Logical translation, hot-plug polling, per-player device profiles, applied N64 manual SDL remaps, validated expressions and manual test plan prepared | No physical controller matrix; rumble and SDL enumeration still unverified | Follow CONTROLLER_TEST_PLAN.md and attach evidence |
| Native saves/backups | PASS | Directory/write checks, snapshot, restore, integrity/traversal/wrong-console tests | Real engine save formats require round-trip checks | Native save/relaunch each console |
| Save states/screenshots | PARTIAL | Capability catalog, per-game startup slot persistence, native state and screenshot folders implemented | No unified capture hotkey or real state round-trip; autosave is backup-on-launch/exit | Real game state save/load per engine |
| Presentation | PARTIAL | Original/Enhanced INI assertions; VSync/fullscreen/aspect flags; per-game overrides; N64 demo rendered | Clean=Original today; hardware accuracy and fullscreen switch unverified | Actual per-console image comparison |
| Extra display effects/modes | NOT STARTED | Explicitly labeled unsupported in Settings | CRT/integer/overscan/scanlines/borderless missing | Implement only with verified backend support |
| Metadata | PARTIAL | Offline JSON/local cover plus explicit CC0 Wikidata provider with confirmation and attribution | Network lookup and richer fields unverified | User-run lookup and licensed field expansion |
| Compatibility database | PARTIAL | Versioned schema, exact-match conservative resolver, no silent enhancements test; one evidence-backed Mupen64Plus GPL demo entry added | No PS2/GameCube entries; no commercial title claims | Add entries only after real game evidence |
| Diagnostics/recovery | PASS | Controlled crash exit, journal cleanup, path-redacted report | No emulator frame/audio telemetry | In-game instrumentation |
| Installer | PASS | Per-user same-host install, shortcut target, startup, reinstall and uninstall passed; external test save retained | Not a clean VM; unsigned preview | Clean-machine/signing release gates |
| Signing/clean VM/updates | PARTIAL | Signing script, update-manifest contract and clean-machine/update runbook prepared; no signing performed | No credentials, updater, clean VM or release channel | Run signed pipeline with credentials and VM |

## QA matrix

| Scenario | State | Evidence / limitation |
| --- | --- | --- |
| Fresh install | PASS | Same-host clean-directory install/startup exited 0; not a clean VM |
| No games | PASS | Empty home/library rendered with import actions |
| Missing BIOS | PASS | Actionable exception unit test |
| Invalid BIOS | PASS | Empty/random BIOS rejected; valid-looking fixture is screening only |
| Controller disconnected | PASS | No-controller status rendered |
| Controller connected after launch | PARTIAL | Polling implemented; no device available |
| Multiple controllers | PARTIAL | Per-player profiles and port configuration tested; no physical multi-device evidence |
| Game launch | PASS | Actual N64 GPL demo and all three fixture processes |
| Normal exit | PASS | Actual N64 test exit and fixture sessions |
| Backend crash | PASS | Fixture exit 17; journal cleared; session released |
| Corrupt configuration | PASS | Backup recovery/quarantine and schema tests |
| Library directory moved | PASS | Missing-directory scan warning; entries retained |
| Game file moved | PARTIAL | Fingerprint relink implemented; UI click path pending |
| Save directory unavailable | PASS | Occupied save-directory fault blocks writes; filesystem checks implemented; broader permission matrix pending |
| Fullscreen switching | PARTIAL | Engine flags and WPF F11 launcher toggle implemented; real fullscreen runtime test not performed |
| Original graphics | PARTIAL | Native-oriented INI assertions and N64 demo, no original-hardware comparison |
| Enhanced graphics | PARTIAL | 3× configuration assertion only |
| Application restart | PASS | Persistence tests, repeated smoke runs and installed application startup |
| Windows restart | NOT STARTED | Do not restart user's machine for validation |
| Update from previous version | PARTIAL | v1→v2 settings tested; installer reinstall passed; no prior product release or updater exists |
| Uninstall retains saves | PASS | Actual uninstall removed application and retained external test save data |

Remaining untested emulation features must retain the label **IMPLEMENTED — RUNTIME VERIFICATION REQUIRED**. Consult release checklist before distributing beyond preview testing.


Installed app updated with rollback copy work/installed-app-before-SZN-20260908010108. Post-update engine paths all exist; OriginalHardware/Stretch=false; PCSX2 remains selected. New preview folder: outputs/Umbra-SZN-Preview. Older ZIP/installer artifacts are historical and were not repackaged during this task.

## Launch correction follow-up

2026-09-08: confirmed the user's CHD contains PS2 serial SLUS-20486, corrected its mistaken GameCube assignment, and selected Play!. Added CHD routing guard, console correction UI and direct BIOS-free PS2 action. 67 automated tests pass. Play! actual intro observed without BIOS; UMBRA session loaded the correct executable, then the timed test force-stopped it. Gameplay, controls, sound and save round trips remain RUNTIME VERIFICATION REQUIRED. See ../../LAUNCH-FIX-RESULTS.md. Dolphin source ZIP contains no executable; existing official backend retained.

## 2026-09-08 Xbox modern control follow-up

The N64 per-game control preset now has explicit precedence over the global profile. `Classic N64 controls` forces the logical/manual mapping, `Modern shooter (right stick = C/look)` forces Xbox action mode plus right-stick C directions, and `Disable right-stick C actions` forces Xbox action mode with all C directions cleared. The global profile is cloned without mutating saved settings. This prevents a global checkbox from silently overriding a game-specific choice. Regression coverage checks all three presets and a generated modern config; the full harness now reports **74 passed, 0 failed**.

The current publish is [artifacts/app-xbox-modern](../artifacts/app-xbox-modern) and has been copied to [Umbra-SZN-Preview](../../Umbra-SZN-Preview). Build and publish completed with .NET 8 Release, win-x64 self-contained output, 0 warnings and 0 errors. `Umbra.exe` SHA-256: `898AC77FAFCDFF351F74244E73D41DB8D387C2358EEB3490CB8B98FF4C5F13B0`. An actual Mupen64Plus 2.6.0 GPL demo was launched again after this change: exit code 0, two screenshots, and the post-start configuration retained `mode = 0`, `device = 0`, `AnalogDeadzone = "4096,4096"`, `Start = "button(7)"`, `X Axis = "axis(0-,0+)"`, and blank C directions. This is engine startup/configuration evidence only; Xbox physical input, title-specific camera behavior, audio and save round trips remain **IMPLEMENTED — RUNTIME VERIFICATION REQUIRED**.

For a recent Xbox-style feel, use `Modern shooter` only on an N64 title whose own control menu assigns C-buttons to looking (for example, a compatible GoldenEye/Perfect Dark control scheme). N64 C-buttons otherwise remain title-defined actions such as fire, back or sidestep, so UMBRA intentionally keeps the global right-stick C mapping off. GameCube and PS2 retain separate native left/right stick paths; their button meanings still come from each game.

The N64 analog deadzone now defaults to the official Mupen64Plus SDL value `4096,4096` instead of the earlier 20,000 value that could make the stick feel excessively long. Controllers exposes 4,096/8,192/12,000/16,000/20,000 choices for worn hardware. The right-stick C-direction threshold remains 24,000 when the per-game Modern Shooter preset is selected. The neutral snapshot and runtime plan remain the source of truth for distinguishing hardware drift from mapping errors.

The published WPF executable also passed an isolated smoke run (`--smoke --data work/smoke-modern-data`): Home, Library, Controllers, Settings, System Status and Game Details all constructed and rendered; process exit code 0. This confirms UI construction only and does not promote any physical controller or gameplay row to PASS.

GoldenEye follow-up: user requested external storage; D: confirmed USB External HD. Preview/test evidence stored there. Isolated desktop smoke passed all six views, exit 0. Latest C: check about 1.7 GiB free; existing library/saves remain unchanged. No data migration performed.

## Fullscreen regression correction

User screenshot exposed a 640x480 image at the top-left of a 1920x1080 window. Root cause: UMBRA applied its Rice resize wrapper to Glide64mk2. Upstream Glide64mk2 ResizeVideoOutput is intentionally empty and states resizing is unsupported: https://github.com/mupen64plus/mupen64plus-video-glide64mk2/blob/master/src/Glide64/Main.cpp . Enlarging the HWND did not reinitialize the renderer viewport.

Fixed: renderer-aware fullscreen policy retains the existing wrapper for Rice, uses native fullscreen for Glide64mk2, obtains physical monitor dimensions before launch and passes that output resolution to the engine. Glide aspect is explicit: 0 for 4:3, 2 for Custom stretch. Controllers, game files and saves are untouched. Glide uses engine-native fullscreen switching; UMBRA's wrapper F11 binding applies only to Rice. Live resolution changes and multiple monitors remain runtime verification required.

PASS: 93 unit/fixture tests, including fullscreen policy, output resolution, and aspect reset. Artifacts D:\Umbra-PerfectDark-Work\umbra-tests-96d8af14c11f48818c29f769dcb0a5cb. PASS actual runtime: Perfect Dark / Glide64mk2 initialized 1920x1080 fullscreen, captured frames 300/600, exited 0. Inspected second screenshot has centered, scaled intro content; not a reproduction of the interactive menu or mission. Evidence D:\Umbra-PerfectDark-Work\fullscreen-fix. Probe command adds --fullhd to prior --glide --long --goldeneye invocation. No downloads.

New self-contained build: D:\Umbra-Fullscreen-Preview\Umbra.exe. Close the older UMBRA window before opening it. Recheck the original menu and mission at full size. Menu corruption and physical gameplay remain IMPLEMENTED — RUNTIME VERIFICATION REQUIRED.

## Startup window fit — 2026-09-26

User reports startup clipping relieved by manually resizing. Source used a fixed 1320x880 WPF-DIP window, minimum 980x680, centered without considering usable monitor space. This can exceed the physical work area under display scaling. Added one-time SourceInitialized placement using the current monitor work rectangle (excludes taskbar) and GetDpiForWindow. Window dimensions and minimums are capped to available space; placement is centered with margins. Sidebar navigation now scrolls on short windows. No controller, engine, save or user configuration changes.

95 unit/fixture tests pass, including startup fit at 100/125/150/200% and short work areas. Results: D:\Umbra-PerfectDark-Work\umbra-tests-47a1a990f35e46e0b0dbaf2cca2dc934. Updated existing D:\Umbra-Fullscreen-Preview in place while no UMBRA process was running. No new preview path to choose. Different physical monitors/scales still require manual checks; numerical coverage is not physical display verification.

Startup verification follow-up: first two checks failed because WPF's startup placement/DPI initialization overrode early HWND positioning. Final implementation uses manual initial placement, fits at SourceInitialized and reconciles once at Loaded after DPI initialization (no ongoing forced resizing). Actual isolated UI smoke at DPI 144 / 150% PASSED: window (24,24) size 1872x1032 contained within work area (0,0) 1920x1080. All five pages rendered and game details constructed. Inspected Home screenshot: top branding, heading and action buttons visible without manual resizing. Evidence: D:\Umbra-PerfectDark-Work\startup-layout-smoke-v3\smoke-results.txt and smoke-Home.png. Current build remains D:\Umbra-Fullscreen-Preview\Umbra.exe. User confirmation on normal startup remains requested; other physical display setups remain unverified.
