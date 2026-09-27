# GoldenEye 007 modern controller preset — 2026-09-26

IMPLEMENTED — RUNTIME VERIFICATION REQUIRED. This is a development preview.

## Start here

1. The preview and test data are on D:, confirmed as the USB External HD. C: filled during packaging; a later check showed about 1.7 GiB available. Keep enough free space for saves/backups, which still use C: by default.
2. Open `D:\Umbra-GoldenEye-Preview\Umbra.exe`. This separate build uses your existing library. The installed app was not replaced; close other UMBRA instances first.
3. Open GoldenEye's game details. Under Controls for this game, choose **GoldenEye 007 — modern (requires 1.2 Solitaire)**. Choose the action button and weapon bumper, then Save game controls.
4. In GoldenEye's watch options choose **1.2 Solitaire**. Use Upright look for the non-inverted preset; use the UMBRA invert option if needed. Choose hold-to-aim if available. Game options must be changed in the game; UMBRA does not patch the ROM or save.
5. Use the chosen weapon bumper for native A/menu confirmation, chosen action button for native B/back, and the right stick for native analog menu navigation. Start/Options opens the watch.

| Action | Xbox default | PlayStation equivalent |
| --- | --- | --- |
| Forward/back/strafe | Left stick (also D-pad if mapped) | Left stick |
| Look | Right stick | Right stick |
| Aim | LT | L2 |
| Fire | RT | R2 |
| Interact AND reload | A, optionally X | Cross, optionally Square |
| Next weapon / menu confirm | RB, optionally LB | R1, optionally L1 |
| Previous weapon | Hold selected weapon bumper, tap RT | Hold selected bumper, tap R2 |
| Watch | Start/Menu | Options/Start |

GoldenEye has no jump. Reload/interact is context-sensitive native B. Movement in scheme 1.2 is eight-way digital C-button movement, not modern variable-speed analog movement. Aiming retains native lean/crouch behavior; camera behavior is game-defined while aiming. B/Y and the unused bumper are deliberately unassigned. Separate reload/interact or one-button previous weapon is not implemented.

The current Mupen SDL plugin parses only the first binding of each type (button, axis, hat). Two face buttons assigned to native B would silently ignore one. This preset exposes a single action-button choice instead. Adding simultaneous A/X or a one-button A+Z chord requires an input-plugin enhancement.

## Device handling

The preset asks the selected N64 engine's SDL2.dll for connected-device indices and standardized mappings at launch. No guessed Xbox raw button numbers, global remapping, virtual driver, or synthetic input is used. Unsupported or missing mappings fail with an explanation. Xbox detection was exercised on actual attached hardware. PlayStation/generic translation has fixture coverage only. Connect controllers before launch; changes during a session and multiplayer remain unverified. Other platform/preset mappings are unchanged.

Movement activation threshold is 8000/32767 (about 24%); trigger threshold is 16000. Camera uses the existing configurable N64 analog deadzone. A very large previously saved deadzone can make looking sluggish; choose a lower value in Controllers only if the neutral-input check shows no drift.

## Evidence

- PASS: 85 automated unit/fixture tests, including Xbox/PlayStation raw layouts, axis inversion, split/unknown mapping rejection, native parser limits, and preservation of the global profile. Command: bundled dotnet `run --project tests/Umbra.Tests -c Release -- tests/Umbra.TestBackend/bin/Release/net8.0/Umbra.TestBackend.exe` (absolute fixture path used). Result directory: `%TEMP%\umbra-tests-bf05140d7ade44d0b83689c2501e7115`.
- PASS: actual SDL discovery identified Controller (Xbox One For Windows), index 0; left axes 0/1, right axes 2/3, trigger axes 4/5, face A=0/X=2, bumpers 4/5.
- PASS, startup/configuration only: official Mupen64Plus 2.6.0 ran existing GPL homebrew demo with `Umbra.EngineProbe <engine> <demo> <isolated-data> --goldeneye`, exit 0, two screenshots. Native config retained mode 0, right-stick axes 2/3, left-stick C directions 0/1, R aim axis 4+, Z fire axis 5+, B button 0, A button 5. Evidence: `work/goldeneye-mapping-demo-20260926` relative to task root. This does not verify GoldenEye gameplay, drift, physical button response, audio, or save round trips.
- PASS: desktop self-contained compilation/package in `D:\Umbra-GoldenEye-Preview`. C: packaging failed for lack of space; incomplete package retained at `D:\Umbra-Incomplete-Package-20260926`. No user data deleted.
- PASS: isolated desktop smoke on external D: rendered Home, Library, Controllers, Settings and System Status, and constructed game details; process exit 0. Evidence: D:\Umbra-GoldenEye-Smoke-20260926\smoke-results.txt. This is interface coverage only.
- GoldenEye gameplay: IMPLEMENTED — RUNTIME VERIFICATION REQUIRED. No game controls pressed or gameplay behavior verified by automation.

No engine or game asset was downloaded this turn. Existing Mupen distribution/source/license records remain applicable. SHA-256:

- Mupen console: `DB0F420EBB65211B6E69223958D9100BA2E1E2FC0180A4FFC0239C573223003A`
- m64p_test_rom.v64: `B5FE9D650A67091C97838386F5102AD94C79232240F9C5BCC72334097D76224C`
- Preview Umbra.Core.dll: `C387388050EE3E4603DDF3B0813ADDD199C85A686F1FF2CB8412873F3AD73AB0`
- Preview Umbra.dll: `FC97535F38C75EFFFC68A1CCC8AA465C7E67FF594AB197209EF68FF5CCEB8DD0`

Primary implementation references: [SDL controller mapping API](https://github.com/libsdl-org/SDL/blob/SDL2/include/SDL_gamecontroller.h), [Mupen SDL config parser](https://github.com/mupen64plus/mupen64plus-input-sdl/blob/master/src/config.c). Original control reference: [GoldenEye manual scan](https://www.scribd.com/document/708179832/Manual-GoldenEye-007-N64). Scheme 1.2 guidance was also cross-checked against community control guides; physical gameplay is the acceptance gate.

## Manual acceptance / evidence template

Record build hashes above, controller model and wired/Bluetooth connection, number of connected controllers, game region, control style, look option, deadzone, action/bumper choices, and date. For each check record PASS/FAIL, observed result, and clip/screenshot if useful:

- Start a mission; leave all controls untouched for 30 seconds. No movement, looking, aiming, or firing.
- Left stick forward/back/left/right and diagonals: movement/strafe only; release stops motion.
- Right stick four directions: look only; no firing, reload, weapon change, or menu action. Release stops looking.
- LT holds aim; RT fires; release resets both. Repeat with neither stick touched.
- Fire once and use the chosen action button to reload; approach a door and use it to interact.
- Chosen bumper cycles weapon; hold it and tap RT to cycle backwards. Test menu confirm/back/watch.
- Repeat after exiting and relaunching; verify profile persists. Test invert option separately.
- Later repeat with PlayStation/generic pads and multiple players; do not extrapolate Xbox results.

Report failures with the exact step and whether GoldenEye displayed 1.2 Solitaire. All physical checks remain open until reported.
