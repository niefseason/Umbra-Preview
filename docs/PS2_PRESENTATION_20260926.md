# PS2 presentation and audio tuning — 2026-09-26

Development preview. User reports the BIOS-backed PS2 session worked well, with fuzzy sound and rough graphics. Latest local native log identifies Dragon Ball Z: Budokai Tenkaichi 3 (SLUS-21678), approximately 1,377 seconds of emulated play time, with shutdown data writes. This is user feedback plus session-log evidence, not an independently verified memory-card round trip or performance benchmark.

## Diagnosis and implementation

- This game's override selected Original Hardware, native 1×, no stretch. PCSX2 was already using Cubeb at 48 kHz, a 50 ms buffer, 20 ms output latency, and enabled time stretching. No measured speed/underrun trace establishes the cause of the reported fuzzy sound.
- Per-game presentation now exposes Custom resolution 1×–6×, stretch, fullscreen and VSync. Custom-only controls are disabled for other profiles. Existing per-game fullscreen/VSync/scale choices survive editing; they no longer silently revert to global values.
- Original/Clean Original retain native scale; Enhanced retains 3×. Higher resolution improves edge detail at increased GPU cost, not game frame rate. Stretch widens geometry rather than patching widescreen support. No cycle stealing, overclock, frame skipping or 60 FPS patches introduced.
- PCSX2-only per-game sound buffer choices: 50/75/100/150 ms, with optional speed/FPS overlay. TimeStretch stays enabled. Additional buffering is a diagnostic candidate for transient interruption, with extra audio latency, not a proven audio fix. Each launch resets owned tuning values to avoid leaking the previous game's options.
- Working profiles capture/restore/undo the new PS2 tuning and validate supported values. Unsupported buffers fail before rewriting the native INI. Play! and other backends do not receive the PCSX2 tuning.

## Verification

- PASS: 107 automated unit/fixture/process tests, zero failures. Includes custom-to-original reset, invalid buffer/config preservation and working-profile restore/undo. Artifacts: `D:\Umbra-Diagnostics\umbra-tests-fa503bdcf52f47b48539d172d11f3f14`. These tests do not emulate games.
- PASS: self-contained Release publish to `D:\Umbra-PS2-Preview`; existing desktop shortcut retained.
- PASS: isolated WPF smoke at `D:\Umbra-Diagnostics\ps2-tuning-ui-smoke`; five pages rendered, N64 controls dialog rendered/closed, PS2 sound/presentation UI constructed; startup bounds contained at 150% DPI.
- PARTIAL real runtime: isolated official PCSX2 2.8.2 launched the user's existing disc and BIOS, executed SLUS_216.78 at ~5.9 seconds and logged Cubeb buffer=100, latency=20, stretching enabled. Generated configuration requests Custom 2× and Stretch. Log at `D:\Umbra-Diagnostics\pcsx-tuning-runtime\engines\PS2\PCSX2\logs\emulog.txt`. Test alive at 20 seconds; graceful close timed out and the isolated probe was terminated (exit -1). No clean-exit, audible quality, frame rate or actual stretch-rendering claim. Missing NVRAM/MEC messages belong to the fresh isolated data folder. Live memory cards were not used.
- Command: `Umbra.EngineProbe --pcsx-boot <installed-pcsx2.exe> <user-disc.iso> <user-bios.bin> D:\Umbra-Diagnostics\pcsx-tuning-runtime --tuned`. Regression harness: `dotnet run --project tests/Umbra.Tests -c Release -- <Umbra.TestBackend.exe>`; UI: `Umbra.exe --smoke --data D:\Umbra-Diagnostics\ps2-tuning-ui-smoke`.

## Applied preview and rollback

The user's Dragon Ball Z override (`7ffcb86ce5e14be6982ad73130dd077f`) is preselected to Custom, Stretch, 2×, 100 ms buffer and speed/FPS display for the next test. Its original working profile was captured first. Original override, INI, log and profile copies: `D:\Umbra-Diagnostics\ps2-before-tuning`. Native saves, controller bindings, other games and global settings were not changed.

1. Open Launch UMBRA, select Dragon Ball Z and press Play (PCSX2 with imported BIOS).
2. Confirm fullscreen fills the display and note any cropping/geometry issues. Stretch intentionally widens a 4:3 image.
3. Compare the same fight for 5 minutes: speed should stay near 100%; record FPS, any crackling and whether it coincides with speed drops. Check controller response remains normal.
4. If speed drops, quit, choose Custom resolution 1× in Presentation, save and repeat the same scene. Resolution affects load; the game's inherent frame rate is unchanged.
5. If sound is delayed or not improved, quit and return the sound buffer to 50 ms. Turn off the performance overlay after testing.
6. To undo all preview tuning: quit the game and select Restore working profile. Keep the original snapshot until the new setup is confirmed.

Audible improvement, sustained speed, full-screen stretch and physical controller regression: **IMPLEMENTED — RUNTIME VERIFICATION REQUIRED**. No production-readiness claim.

## Sources and provenance

No engine, ROM, BIOS or other asset downloaded this turn. Existing official PCSX2 2.8.2 (GPL-3.0+) executable SHA-256: `982C7C62600A999CF15A25C18349426166C785E7867B2FCC5018D733245B71A3`. User-supplied BIOS provenance/hash remains recorded in PS2_INPUT_BIOS_20260926.md. The user's existing disc is not redistributed.

Native setting names verified against pinned official sources, read online rather than added to the distribution:

- [PCSX2 2.8.2 configuration](https://github.com/PCSX2/pcsx2/blob/v2.8.2/pcsx2/Pcsx2Config.cpp): boolean VsyncEnable, scale/aspect, performance overlay, SPU2/Output SyncMode (TimeStretch).
- [PCSX2 2.8.2 audio stream](https://github.com/PCSX2/pcsx2/blob/v2.8.2/pcsx2/Host/AudioStream.cpp): SPU2/Output BufferMS and output latency.
- [PCSX2 2.8.2 SPU2](https://github.com/PCSX2/pcsx2/blob/v2.8.2/pcsx2/SPU2/spu2.cpp): actual stream creation and synchronization.
