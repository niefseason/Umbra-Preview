# Modern stick tuning — 2026-09-26

IMPLEMENTED — RUNTIME VERIFICATION REQUIRED for physical gameplay feel and drift measurement. Development preview.

Open Launch UMBRA, choose GoldenEye or Perfect Dark, then Camera sensitivity & drift check. A modern 1.2 preset must be selected before saving. Settings apply at next launch. The in-game style must still be 1.2.

- Separate horizontal and vertical look response: 50–200%, default 100%. Below 100% lowers maximum turning speed; above 100% reaches maximum sooner. These are native N64 stick-response changes, not mouse look or a new camera implementation.
- Movement activation threshold: default 8000/32768, selectable 2000–24000. Left stick remains digital eight-way movement. Trigger bindings and camera axes are not reassigned.
- Camera deadzone: defaults to existing base controller deadzone; per-game override 0–24000. Right stick only in modern 1.2 profiles.
- Existing vertical inversion remains in the game's control options. Save working profile after successful tuning if you want to keep the new feel. Restore working profile brings back saved tuning, including old snapshots with no tuning override. Reset tuning returns to base-controller response.
- Live Xbox/XInput panel polls at 100 ms while the dialog is open, showing per-controller stick magnitude and trigger levels. A five-second neutral sample records per-axis maximum absolute values for each connected controller, flags disconnect/index-set changes, and never writes calibration automatically. Do not touch sticks while sampling. If resting values reach the selected threshold, try raising it slightly and repeat.
- XInput diagnostics do not identify SDL joystick indices or prove SDL output/gameplay behavior. PlayStation/generic SDL live diagnostics are explicitly unavailable. Swapping devices within the same XInput slot may not be detected; repeat after changing devices. Timer stops when the dialog closes.

## Implementation and evidence

Uses the official input plugin's formula: AnalogPeak = deadzone + round((32768 - deadzone) * 100 / sensitivityPercent). Horizontal/vertical peaks are independent. Plugin accepts integer peak values above 32768, so slower response can reduce full-stick output. Output is clamped by the plugin. Defaults preserve AnalogPeak 32768,32768 and movement threshold 8000. No user settings were changed during this implementation.

Primary sources inspected: https://github.com/mupen64plus/mupen64plus-input-sdl/blob/master/src/plugin.c (GetKeys response/deadzone/range math and clamping), https://github.com/mupen64plus/mupen64plus-input-sdl/blob/master/src/config.c (integer AnalogPeak parsing). No downloads or new game assets.

PASS: 102 unit/fixture tests, including default response equivalence, separate response axes, invalid ranges, working-profile restore/undo of tuning, and movement-threshold changes leaving all non-C bindings unchanged. Artifacts D:\Umbra-PerfectDark-Work\umbra-tests-0fcbe6b91d8b4b6ca0eb13df56be8f18.

PASS actual engine startup/configuration only: existing official Mupen 2.6.0 and bundled GPL demo ran with --goldeneye --tuned via EngineProbe; exit 0, two screenshots. Post-engine config retains AnalogDeadzone 6000,6000; AnalogPeak 27414,41691 (horizontal 125%, vertical 75%); C movement threshold 10000. Evidence D:\Umbra-PerfectDark-Work\tuning-runtime. These are deliberately nondefault isolated settings, not applied to the user's games. This proves parsing/persistence/startup, not stick feel or physical neutrality.

Self-contained build updated in D:\Umbra-Fullscreen-Preview; existing Launch UMBRA shortcut still points there. Native saves, working profile backups, live overrides and global settings unchanged.

## Acceptance

1. With the controller untouched, run the five-second check; note each stick's maximum and connection type. Repeat twice.
2. Launch at unchanged 100% first; verify no movement at rest and no look/fire cross-binding.
3. Change horizontal response only, relaunch, check turning compared with vertical look. Then restore or choose your preference.
4. Test a camera deadzone adjustment independently of movement threshold. Check release-to-stop and diagonals.
5. Close/reopen the dialog, disconnect/reconnect, and verify live values resume; check profile save/restore after a satisfactory game session.
6. Record game, 1.2 control style, Xbox model, sensitivity/deadzone/threshold values, idle observations, and PASS/FAIL. Non-XInput diagnostics and other engines are outside this milestone.

PASS isolated UI smoke at 150% DPI: startup contained, five pages rendered, game details constructed, tuning dialog rendered/visually inspected and closed. Screenshot D:\Umbra-PerfectDark-Work\tuning-ui-smoke\smoke-stick-tuning.png. The smoke closed before collecting a five-second sample; this is UI lifecycle coverage, not physical drift-check acceptance.
