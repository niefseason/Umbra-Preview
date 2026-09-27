# UMBRA controller validation plan

This plan is intentionally manual. Do not mark controller rows PASS from configuration files or the synthetic process fixture. Record one evidence bundle per device and per console.

## Setup

1. Install the final UMBRA build in a disposable Windows user profile or VM. Keep `%LOCALAPPDATA%\Umbra` outside the application folder.
2. Connect one controller at a time, then connect two or more controllers. Record the Windows device name, vendor/product ID, SDL GUID if available, connection order, and whether XInput or SDL is used.
3. Import a legal homebrew/test title for each console. Use the same title and save directory for relaunch checks. Do not use the only copy of a personal save.
4. In Controllers, confirm the device count, choose player count, set one deliberate remap, save it, restart UMBRA, and confirm it persists.

## Per-device mapping checks

For each Xbox, PlayStation, Nintendo-style and generic SDL device, verify South/East/West/North, Start/Select, shoulders/triggers, both sticks, D-pad, L3/R3 and analog range. Confirm the UI label matches the physical button. For N64, record the SDL GUID separately for identity, plus the engine-reported numeric device index. Do not put a GUID into the native device-index field. Inspect the native Start, button, axis and hat settings and verify each remap in-game. The current launcher has no complete SDL enumeration/manual mapping validation; mark those cases PARTIAL or FAIL if defaults replace a requested mapping.

## Per-console checks

- PS2: Cross/Circle/Square/Triangle, Select/Start, L1/R1/L2/R2, both sticks, L3/R3, analog pressure where the title supports it.
- GameCube: A/B/X/Y, Start, Z, L/R digital and analog, C-stick, main stick and D-pad.
- N64: A/B, Start, Z, L/R, C-buttons, analog stick, D-pad and controller pak behavior.

For each mapping, test in a disposable session, save natively, exit normally, relaunch and confirm the mapping and save both survive. Test a controller disconnected before launch, disconnected during play, and reconnected after launch. Verify that UMBRA returns to the library after normal exit and after a backend crash.

## Multiple players and gamepad-only navigation

With two to four controllers, assign each player a unique device, launch a title that supports that many players, and verify no player controls another. Navigate Home → Library → game details → Play → quit using only a controller. Confirm every focused control has a visible focus indicator, a spoken/accessible name, and a non-destructive default. Repeat with Windows Narrator or another assistive technology and with high contrast enabled.

## Xbox modern layout check

The Xbox action preset affects N64 and GameCube only. On GameCube, LT sends Z, RT sends B, and LB/RB send L/R including their analog values. Test that LT/RT do not additionally activate L/R. Toggle action mode off, launch, then on and relaunch to check that old shoulder bindings do not leak across modes. PCSX2 keeps its manual face-button mapping, LT/RΤ to L2/R2, and independent left/right sticks; Play! controls are managed separately by that engine. A second native stick does not guarantee a game's camera uses it.

For N64, the preset sends left stick to the N64 stick, LT to Z, RT or B to native B, A to native A, and LB/RB to L/R. X/Y are unassigned in action mode. Select `Modern shooter (right stick = C/look)` only after confirming the actual title's control scheme. There is no verified title-specific modern shooter profile in this preview. Do not assume GoldenEye or Perfect Dark work with this generic mapping. Record the exact title, control scheme, and native button responsible for each action before judging the mapping.

Before launching, open Controllers and record the neutral snapshot. With the controller untouched, all four stick values should remain near zero and both trigger values should be released. If any value moves, disconnect/reconnect the controller and repeat; persistent non-zero values indicate Windows/XInput calibration drift and should be recorded as a hardware calibration issue rather than solved by swapping game buttons. During play, test each direction one at a time, then hold the stick still for 30 seconds to confirm no movement or action occurs at rest.

## Evidence template

Copy this block for each run and attach screenshots/logs without including game paths or BIOS contents:

```text
Run ID:
Date / UMBRA version:
Windows build / GPU:
Controller make/model:
Connection: XInput / SDL / other
Windows device name:
SDL GUID:
Player count:
Console / engine / version:
Legal test title and source/license:
Mapping file hash before/after:
Controls checked:
Hot-plug result:
Native save result:
Save-state result (if supported):
Screenshot result:
Gamepad-only navigation result:
Accessibility result (tool/settings):
Observed issue / exact reproduction:
Attached sanitized log names:
Tester:
```

Until these records exist for each device class and player count, keep the status **IMPLEMENTED — RUNTIME VERIFICATION REQUIRED**.

