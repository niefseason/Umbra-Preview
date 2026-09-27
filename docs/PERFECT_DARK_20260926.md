# Perfect Dark / shared modern controls — 2026-09-26

Development preview. IMPLEMENTED — RUNTIME VERIFICATION REQUIRED for the reported menu glitch and Perfect Dark physical gameplay.

## Findings and changes

- The user's screenshot shows repeated/scrolling menu imagery. Active native config selects Rice 2.6.0. This is a renderer/framebuffer compatibility hypothesis, not a proven root cause. The shipped Glide64mk2.ini has a Perfect Dark section enabling fb_smart/fb_hires and disabling optimize_texrect, providing a concrete alternate renderer to test.
- Added a per-game renderer selector (default Rice, Rice, Glide64mk2). Launch uses explicit --gfx and validates requested plugins. Default explicitly remains Rice so another game's renderer choice cannot leak into GoldenEye.
- Added shared Modern 1.2 and Automatic modern presets. Automatic mode reads the N64 ROM header in z64/v64/n64 byte orders, recognizes GOLDENEYE or PERFECT DARK, and uses the same tested mapping implementation. Unknown titles are blocked with an explanation instead of guessing. No claim of universal camera controls for arbitrary N64 games.
- Perfect Dark already had goldeneye-solitaire selected. The game's own control style must also be set to 1.2; the frontend does not modify ROMs or native saved options. The game's decompiled movement code explicitly handles C-button/D-pad movement and analog look for 1.2. Native aim-mode movement and context actions remain game-defined.
- Perfect Dark's existing override was backed up to D:\Umbra-PerfectDark-Work\perfect-dark-override-before.json, then set to auto-modern / glide64mk2. All other fields retained. GoldenEye override, native saves, ROMs, and installed app unchanged.
- User reported GoldenEye played successfully on their Xbox controller. Record this as user-reported gameplay success for that combination, not verification of other games/controllers or every release gate.

## Use

Close the old UMBRA window and open D:\Umbra-Modern-Preview\Umbra.exe. Perfect Dark is preselected for automatic modern controls and Glide64mk2. In Perfect Dark set Control Style 1.2, normal/upright look and hold-to-aim. Left stick moves/strafe; right stick looks; LT aims; RT fires; A is context interact/reload; RB is native A/weapon/menu confirm. The action button and bumper are selectable in game details. Previous weapon uses hold bumper plus RT; native secondary-function/aim/inventory behavior remains game-defined. See GOLDENEYE_CONTROLS.md for shared limitations and neutral-input test steps.

Reopen the pictured Game Files menu and observe it for 30 seconds. Confirm whether repeated layers disappear; check ordinary pause/options/inventory menus too. Then test idle, both sticks separately, LT and RT, interaction/reload, and exit/relaunch. Record exact game scheme, renderer and observed result. If regression occurs select Rice in game details; previous override backup is retained. Do not use the old preview for this new automatic preset.

## Verification

- PASS: 91 unit/fixture tests, 0 failed. Includes header recognition for both games and all byte orders, unknown/truncated-header rejection, shared preset preservation, invalid/missing renderer rejection, and per-launch renderer isolation. A new test initially failed due to comparing slash styles in test paths; normalizing both paths fixed the assertion. Result: D:\Umbra-PerfectDark-Work\umbra-tests-c5af95dc9c0e463e8616d2134077d154.
- PASS: self-contained desktop build at D:\Umbra-Modern-Preview; isolated UI smoke rendered Home, Library, Controllers, Settings, System Status and constructed game details. Evidence D:\Umbra-PerfectDark-Work\ui-smoke\smoke-results.txt.
- PASS, limited actual runtime: user's existing Perfect Dark USA Rev 1 file booted with official Mupen64Plus/Glide64mk2 2.6.0, one SDL Xbox controller, screenshot frames 300/600, exit 0. Inspected screenshots show intro logos. Evidence D:\Umbra-PerfectDark-Work\glide-runtime\engine-probe.log and engines\N64\screenshots. No native saves copied into this isolated run. The affected interactive menu was not reached; its correction is unverified. No physical control behavior or sound verification from this run.
- GoldenEye: prior user report of successful Xbox play; no new GoldenEye gameplay regression session performed this turn.

Commands (bundled dotnet, source root outputs/Umbra):
- run --project tests/Umbra.Tests -c Release -- <absolute Umbra.TestBackend.exe>
- run --project tests/Umbra.EngineProbe -c Release -- <official mupen console> <user's existing Perfect Dark ROM> D:\Umbra-PerfectDark-Work\glide-runtime --glide --long --goldeneye
- publish src/Umbra.Desktop -c Release -r win-x64 --self-contained true -o D:\Umbra-Modern-Preview --no-restore
- D:\Umbra-Modern-Preview\Umbra.exe --smoke --data D:\Umbra-PerfectDark-Work\ui-smoke

## Sources/assets

No engine, BIOS, game, or asset downloaded. Reused the previously installed official Mupen64Plus 2.6.0 distribution and its GPL-licensed Glide64mk2 plugin; existing release provenance/notices remain applicable. Plugin SHA-256: B798E74063777BE2D8D180C5CB209BC89D7FDB6D7CAAA6994CCB90C1E985E25C.

Technical references: https://github.com/n64decomp/perfect_dark/blob/master/src/game/bondmove.c (CONTROLMODE_12 branch); https://github.com/mupen64plus/mupen64plus-video-rice ; installed official Glide64mk2.ini [Perfect Dark] section. SDL translation/parser references remain in GOLDENEYE_CONTROLS.md. Header recognition is routing logic, not compatibility evidence.

## Fullscreen regression correction

User screenshot exposed a 640x480 image at the top-left of a 1920x1080 window. Root cause: UMBRA applied its Rice resize wrapper to Glide64mk2. Upstream Glide64mk2 ResizeVideoOutput is intentionally empty and states resizing is unsupported: https://github.com/mupen64plus/mupen64plus-video-glide64mk2/blob/master/src/Glide64/Main.cpp . Enlarging the HWND did not reinitialize the renderer viewport.

Fixed: renderer-aware fullscreen policy retains the existing wrapper for Rice, uses native fullscreen for Glide64mk2, obtains physical monitor dimensions before launch and passes that output resolution to the engine. Glide aspect is explicit: 0 for 4:3, 2 for Custom stretch. Controllers, game files and saves are untouched. Glide uses engine-native fullscreen switching; UMBRA's wrapper F11 binding applies only to Rice. Live resolution changes and multiple monitors remain runtime verification required.

PASS: 93 unit/fixture tests, including fullscreen policy, output resolution, and aspect reset. Artifacts D:\Umbra-PerfectDark-Work\umbra-tests-96d8af14c11f48818c29f769dcb0a5cb. PASS actual runtime: Perfect Dark / Glide64mk2 initialized 1920x1080 fullscreen, captured frames 300/600, exited 0. Inspected second screenshot has centered, scaled intro content; not a reproduction of the interactive menu or mission. Evidence D:\Umbra-PerfectDark-Work\fullscreen-fix. Probe command adds --fullhd to prior --glide --long --goldeneye invocation. No downloads.

New self-contained build: D:\Umbra-Fullscreen-Preview\Umbra.exe. Close the older UMBRA window before opening it. Recheck the original menu and mission at full size. Menu corruption and physical gameplay remain IMPLEMENTED — RUNTIME VERIFICATION REQUIRED.
