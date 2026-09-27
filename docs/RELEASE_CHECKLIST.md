# Release checklist

This build is a development preview. Do not relabel it production-ready by changing a version string.

## Xbox 360 addition

- [x] Official Xenia master installed separately on D:, source/license/hash recorded; no BIOS/game downloads. [Evidence](XBOX360_20260927.md).
- [x] Xbox 360 library, XEX/ISO format checks, direct native launch, isolated save-content backups and capability-specific UI; 112 automated tests pass.
- [x] Actual Xenia D3D12 initialization on RX 5600M recorded; no game boot or clean-exit claim.
- [ ] Xbox 360 lawful game boot, physical Xbox controls, fullscreen, audio, native saves/relaunch and recovery — IMPLEMENTED — RUNTIME VERIFICATION REQUIRED; waiting for a game dump.
- [ ] Xbox 360 STFS/GOD package import, remapping, display enhancements, save-state and screenshot automation — unavailable in this integration.

## Existing platforms

- [x] Fix confirmed PCSX2 face-button binding rejection; official native SDL names, two-port remap/reset regression tests; 109 automated tests pass. [Evidence](PS2_BINDINGS_20260927.md).
- [x] Actual isolated MVC2 executable boot after binding correction with zero Invalid binding warnings; no physical-input claim.
- [ ] Xbox face-button acceptance after PCSX2 binding correction — IMPLEMENTED — RUNTIME VERIFICATION REQUIRED.

- [x] DBZ full-image SHA-1/size match Redump-derived metadata; user confirms stretch and sharper characters. Scope/evidence: [DBZ_IMAGE_CHECK_20260926.md](DBZ_IMAGE_CHECK_20260926.md).
- [ ] Diagnose persistent DBZ audio fuzziness and faded maps/menus despite reported ~100% speed — IMPLEMENTED — RUNTIME VERIFICATION REQUIRED.

- [x] PS2 per-game presentation/audio tuning and working-profile rollback; 107 automated tests and isolated UI smoke pass. See [PS2_PRESENTATION_20260926.md](PS2_PRESENTATION_20260926.md).
- [x] Isolated real PCSX2 DBZ executable startup and 100 ms audio-buffer acceptance logged (probe terminated; no clean-exit claim).
- [ ] Validate 2×/stretch rendering, sustained speed, audible quality and unchanged physical input in DBZ — IMPLEMENTED — RUNTIME VERIFICATION REQUIRED.

Latest source review: [REVIEW_20260926.md](REVIEW_20260926.md).
- [x] Fix Dolphin action-mode analog shoulder overwrite; classic/action switching regression coverage.
- [x] Recover null SDL entries/input maps and retain custom deadzones in Controllers.
- [x] Replace connected-controller READY with DETECTED; correct title-action and PCSX2 preset claims.
- [x] 2026-09-26: 78 unit/fixture tests pass; separate preview publishes and isolated UI smoke passes.
- [ ] Validate corrected Dolphin trigger behavior on the physical Xbox controller — IMPLEMENTED — RUNTIME VERIFICATION REQUIRED.

Historical checked mapping items below describe code/configuration coverage only. They do not establish universal camera/aim/shoot behavior or title-specific shooter compatibility.

- [x] Modular frontend and core; replaceable branding.
- [x] Build desktop and regression harness with warnings treated as errors.
- [x] Scan, migration, recovery, mapping, metadata and process tests.
- [x] Actual N64 GPL demo boot/render/exit.
- [x] PCSX2 isolated configuration startup check.
- [x] Official N64 and PS2 engine download/extraction records with source, version, license and SHA-256.
- [x] Official Dolphin 2603a Windows build downloaded, extracted, hashed, installed and registered; Swiss r2092 actual boot failed. See SZN_DIAGNOSIS.md.
- [x] Render main screens and inspect dark-theme layout.
- [ ] PS2 legal game boot, BIOS validation and native memory-card round-trip.
- [ ] Dolphin legal homebrew/game boot and memory-card round-trip.
- [x] Device-aware SDL profile schema, hot-plug polling and native N64 mapping generation (runtime device verification still required).
- [x] Xbox SDL defaults corrected for analog Z trigger, Start button, and non-cascading logical remaps; repeated N64 fullscreen resizing suppressed (69-test regression run). Physical Xbox and runtime gameplay verification remains required.
- [x] Xbox N64 raw button/axis layout aligned with the official Mupen64Plus SDL profile, including 24000 C-stick thresholds and analog deadzone. Runtime stick calibration remains required.
- [x] Explicit Xbox action layout: left movement/right camera on GameCube/PS2; N64 right-stick C/look only when the title-specific preset is enabled; A primary action, LT aim, RT shoot, LB/RB shoulder actions; X/Y remain game-defined. Runtime gameplay verification remains required.
- [x] N64 right-stick C-button mapping is now opt-in per profile because C-buttons are game-defined actions (57 core tests pass); GameCube retains native C-stick support.
- [x] Official upstream SDL axis order restored (Xbox left stick axes 0/1, right C-stick axes 2/3); Controllers page now exposes a neutral XInput snapshot for drift diagnosis. N64 second-camera behavior remains title-specific.
- [x] N64 adapter now forces Mupen64Plus mode 0/manual bindings; actual GPL-demo launch retained the generated native mapping after engine startup (exit 0, two screenshots). Physical controller/title verification remains required.
- [x] N64 per-game control presets now override the global action mode: Classic, Modern Shooter (right-stick C/look) and Disable C actions. The latest regression run is 74 passed, 0 failed; real title/controller behavior remains runtime verification required.
- [x] N64 analog deadzone restored to official 4,096 default and made selectable per profile (4,096–20,000) to diagnose worn-stick drift without silently reducing the normal range. Runtime stick calibration remains required.
- [x] Published modern-control build passed isolated WPF smoke (`--smoke --data`): Home, Library, Controllers, Settings, System Status and Game Details rendered; exit code 0.
- [ ] Physical Xbox, PlayStation, Nintendo and generic SDL device matrix.
- [ ] Multiple-player tests; native N64 manual mapping.
- [ ] Sustained gameplay/audio/performance/profile comparison per engine.
- [x] Capability catalog, per-game startup-state slot selection, native state/screenshot folders and backup autosave protection.
- [ ] Real state save/load round trips, engine hotkeys, screenshot capture and automatic state restore.
- [ ] Remaining display modes/effects and advanced per-game controls.
- [x] Keyboard focusable navigation, accessible button names, F11 launcher fullscreen toggle and controller hot-plug status polling.
- [ ] Full keyboard/gamepad-only navigation and accessibility audit on Windows assistive technology.
- [ ] Full clean-machine install, Windows restart and genuine prior-version upgrade matrix.
- [ ] Populate known-good compatibility catalog with evidence-backed real game entries.
- [x] Opt-in CC0 Wikidata title lookup with attribution and offline local metadata fallback.
- [ ] Code signing, version support policy and update migration/rollback.
- [x] Signing/update preparation: `scripts/sign.ps1` requires an explicit certificate thumbprint; `config/update-manifest.example.json` defines a version/hash contract without publishing anything.
- [x] Current preview license/source/notice audit completed; no emulator binaries are bundled. Any future engine bundle still requires release-specific source and third-party notices.
- [ ] Independent security review of import and restore boundaries.

Packaging and same-host clean-directory validation results are recorded in STATUS.md after the final build. Uninstall must leave `%LOCALAPPDATA%\Umbra` intact. Never add wildcard uninstall deletion of data, ROMs or BIOS directories.

## SZN continuation, 2026-09-08

- [x] Diagnose actual engine paths and saved Custom/Stretch setting; back up settings and register Dolphin/PCSX2 without replacing N64.
- [x] Shared display policy and N64 centered 4:3 borderless presentation; narrow real demo geometry/screenshot/normal-exit evidence recorded.
- [x] 64 unit/fixture tests; final publish and WPF smoke passed.
- [x] Optional Play! 0.72 BIOS-free path, explicit UI selection, separate data and incompatible-state rejection; engine version check passed.
- [ ] Play! actual PS2 boot, native control/display behavior, save isolation and save round trip — IMPLEMENTED — RUNTIME VERIFICATION REQUIRED.
- [ ] GameCube compatible legal-content boot; Swiss r2092 is FAIL, not a passing catalog entry.
- [ ] F11 key interactions, all display profiles, mixed-DPI/multiple monitors, audible sound and physical controllers — IMPLEMENTED — RUNTIME VERIFICATION REQUIRED where implemented.
- [ ] Full BIOS validation with a real user dump; current import screening does not authenticate firmware.

Sources/hashes/notices, exact commands and remaining limitations: SZN_DIAGNOSIS.md. This update does not close any signing, clean-machine, installer-retention or production release gate.


- [x] Reject CHD/GameCube and CHD/N64 mismatches, including renamed containers; preserve import errors; expose console correction and BIOS-free PS2 launch actions. 67 automated tests pass.
- [x] Narrow PS2 BIOS-free runtime evidence: Play! loaded SLUS-20486 and rendered its intro; UMBRA launched its executable. This does not close normal-exit/gameplay/controller/save gates.


## GoldenEye preset, 2026-09-26
- [x] Dedicated 1.2 Solitaire preset and per-game controls; runtime SDL mapping detection; reject unsupported layouts rather than guess.
- [x] 85 unit/fixture tests; real Xbox mapping discovery; Mupen GPL-demo boot/config retention (not GoldenEye gameplay).
- [x] Separate self-contained preview built on D: after C: filled; incomplete C: output moved intact to D:.
- [x] Document native reload/interact and one-binding-per-type limitations, precise setup and acceptance template in GOLDENEYE_CONTROLS.md.
- [ ] Physical GoldenEye Xbox neutral/sticks/triggers/actions/menus/relaunch — IMPLEMENTED — RUNTIME VERIFICATION REQUIRED.
- [ ] PlayStation/generic/multiplayer/hot-plug behavior — IMPLEMENTED — RUNTIME VERIFICATION REQUIRED.
- [ ] Restore sufficient C: free space before user gameplay/save backups. No production readiness claimed.

- [x] GoldenEye preview isolated UI smoke: five rendered pages plus game-details construction, exit 0. External USB D: used per user request. Existing user data retained in place.

## Perfect Dark follow-up
- [x] Record user-reported GoldenEye Xbox gameplay success, scoped to that combination.
- [x] Automatic modern controls for recognized GoldenEye/Perfect Dark headers, unknown-game rejection and shared 1.2 instructions.
- [x] Per-game renderer selection, availability validation and default-renderer isolation; preserve GoldenEye.
- [x] 91 automated unit/fixture tests, self-contained external-drive preview, isolated UI smoke.
- [x] Actual Perfect Dark/Glide64mk2 intro boot, screenshots and exit 0 (not affected-menu verification).
- [ ] Verify Perfect Dark repeated-menu correction and modern 1.2 physical controls — IMPLEMENTED — RUNTIME VERIFICATION REQUIRED.
- [ ] Broaden game-specific profiles only with source/control research and physical evidence; no arbitrary-game universal camera claim.
Evidence, sources and rollback: PERFECT_DARK_20260926.md. Development preview remains in effect.

- [x] Fix Glide64mk2 fullscreen viewport initialization: bypass unsupported post-start resize, native fullscreen at physical screen resolution; 93 automated tests pass. Actual Perfect Dark 1920x1080 intro capture/exit passes. See PERFECT_DARK_20260926.md.
- [ ] Verify menu/mission at full size, 4:3 versus Custom stretch, native fullscreen switching and multiple monitors — IMPLEMENTED — RUNTIME VERIFICATION REQUIRED.

- [x] Fit startup window to monitor work area and DPI, cap minimum size, scroll sidebar on short windows; 95 automated tests pass.
- [ ] User confirms first-open clipping corrected on their display; different physical scaling/multi-monitor checks — IMPLEMENTED — RUNTIME VERIFICATION REQUIRED.

- [x] Actual startup UI smoke at 150% DPI: window contained in work area, header visually inspected, five pages rendered and game details constructed. Evidence startup-layout-smoke-v3. No gameplay regression test needed for this launcher-only change; engine/controller code unchanged.

## Working setup protection
- [x] User reports startup clipping corrected on their current display setup (does not cover every monitor/DPI combination).
- [x] Single desktop Launch UMBRA shortcut targets the current external preview and is read-back verified.
- [x] Save/restore/undo working controller/display profiles per game; preserve native saves and current startup-state selection.
- [x] Corrupt/wrong-game/schema/path validation; 99 unit/fixture tests pass.
- [x] Capture current GoldenEye/Perfect Dark profiles, duplicate to external drive, keep live overrides unchanged.
- [ ] User gameplay after profile restore; other-controller/multiple-monitor runtime checks — IMPLEMENTED — RUNTIME VERIFICATION REQUIRED.
Evidence: WORKING_PROFILES_20260926.md. No production readiness claim.

- [x] Working-profile preview isolated startup/UI smoke passed at 150% DPI; five rendered pages and game-details panel construction.

## Modern stick tuning
- [x] Per-game horizontal/vertical camera response, independent movement threshold/deadzone, validation and reset; default response unchanged.
- [x] Working profiles capture/restore optional tuning, including older snapshots.
- [x] Live XInput diagnostics and five-second neutral observation; no automatic calibration or SDL gameplay claim.
- [x] 102 unit/fixture tests; official engine + existing GPL demo startup/configuration persistence, exit 0.
- [ ] Physical controller tuning, drift/release behavior and game feel — IMPLEMENTED — RUNTIME VERIFICATION REQUIRED.
Evidence and exact scope: STICK_TUNING_20260926.md. Development preview remains in effect.

- [x] Tuning UI smoke: startup bounds, five pages, game details, tuning dialog render and close; inspected screenshot. Five-second physical observation still requires user testing.

## PS2 user BIOS/input follow-up
- [x] Diagnose Play keyboard-only bindings; implement native XInput profile generation with preservation/security tests.
- [x] Add direct PCSX2 play action and Play renderer selection; 104 automated tests pass, desktop build published.
- [x] Verify user-provided BIOS managed-copy hash; select PCSX2 by default, preserve existing settings backup and separate saves.
- [x] Actual isolated PCSX2 BIOS acceptance (USA v01.60), MVC2 ELF execution, SDL Xbox device detection.
- [ ] Actual physical PS2 buttons/sticks, graphics/cutscene behavior, clean exit and native memory-card roundtrip — IMPLEMENTED — RUNTIME VERIFICATION REQUIRED.
- [ ] Play physical controller/Vulkan tests — IMPLEMENTED — RUNTIME VERIFICATION REQUIRED.
Details/scope: PS2_INPUT_BIOS_20260926.md. No production readiness claim.

- [x] PS2 preview isolated startup/UI smoke passed; physical PS2 gameplay gates remain open.
