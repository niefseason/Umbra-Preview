# Pipeline: PCSX2 face-button binding correction — 2026-09-27

Development preview. Dragon Ball Z audio/graphics investigation is deferred at the user's request. This scoped pipeline step addresses a confirmed engine-binding defect found during that investigation.

## Diagnosis and fix

PCSX2 2.8.2 rejected generated SDL-0/A, B, X and Y strings in the previous actual engine log. UMBRA translated logical PS2 actions into Xbox labels but did not convert face labels to PCSX2's native SDL setting names. The adapter now emits FaceSouth, FaceEast, FaceWest and FaceNorth, respectively. Unknown inputs fail explicitly. Existing manual remaps still apply before native translation; both PS2 ports use the correction. Stick, trigger, N64, Dolphin and Play! mappings are unchanged.

Official version-pinned reference: [SDLInputSource.cpp](https://github.com/PCSX2/pcsx2/blob/v2.8.2/pcsx2/Input/SDLInputSource.cpp), s_sdl_button_setting_names. Read online; no new binaries, ROMs, BIOS or assets downloaded.

## Evidence

- PASS automated: 109 unit/fixture/process tests, zero failures. Added native-name/unknown-input coverage and two-port remap/reset checks; existing N64/GameCube configuration/process regressions pass. Artifacts: D:\Umbra-Diagnostics\umbra-tests-4d3f4b1c6cb24fc8888aeb17b5d4886b.
- PASS build: self-contained desktop published to D:\Umbra-PS2-Preview (same desktop shortcut target).
- PASS isolated UI smoke: D:\Umbra-Diagnostics\input-ui-20260927\smoke-results.txt; five pages rendered, tuning dialog rendered/closed, PS2 controls constructed, startup bounds contained at 150% DPI.
- PARTIAL actual runtime: official installed PCSX2 2.8.2, user's existing BIOS and MVC2 CHD, isolated data at D:\Umbra-Diagnostics\pcsx-input-20260927. Engine executed SLUS_204.86 at ~9.1 seconds. Log contains zero Invalid binding warnings. Alive at 20 seconds; close timed out and only the isolated probe was terminated (exit -1). No clean-exit or save round-trip claim. No physical input injected or verified.
- Commands: `dotnet run --project tests/Umbra.Tests -c Release -- <Umbra.TestBackend.exe>`; `Umbra.EngineProbe --pcsx-boot <installed engine> <existing user MVC2 CHD> <existing user BIOS> D:\Umbra-Diagnostics\pcsx-input-20260927`; `dotnet publish src/Umbra.Desktop -c Release -r win-x64 --self-contained true -o D:\Umbra-PS2-Preview --no-restore`.

No live overrides, settings, game files or native saves changed. Native bindings will be regenerated using the correction on the next PCSX2 launch; original INI is automatically backed up by the existing adapter writer.

## Manual acceptance / next gate

Connect the Xbox controller before launching a PS2 title. Test each A/B/X/Y independently in a menu or gameplay with known actions, plus Start, both sticks and both triggers; confirm there is no cross-triggering. Record game, engine version, controller connection type, expected/actual action per button, unplug/reconnect behavior and whether relaunch retains it. Multiple physical pads and non-Xbox SDL controllers remain unverified.

Physical input: **IMPLEMENTED — RUNTIME VERIFICATION REQUIRED**. Next release-gate work is native save/relaunch and real save-state round trips, using isolated data where feasible. No production readiness claim.
