# PS2 BIOS and input follow-up — 2026-09-26

## Diagnosis

User video is Marvel vs. Capcom 2's intro (not the earlier Dragon Ball game-details screenshot). Extracted three frames from the 14.97-second recording; one sample is black. Video alone cannot prove the renderer cause or cutscene skip behavior. User reports visual artifacts and no response to controller buttons.

Actual selected backend was Play! 0.72. Its isolated input profile used Qt keyboard provider 1366575993 for pad1, not XInput. UMBRA had no Play! controller configuration integration. Root cause of missing managed Xbox input is confirmed at configuration level. Physical button response still needs testing.

## Changes

Added PlayConfiguration to generate a separate umbra-xinput profile and select it during Play! launch when Xbox/XInput devices are connected. It uses actual connected XInput slots (up to two), native key IDs, analog axis types, triggers/buttons, both sticks, Start/Back, native PS2 face buttons and configured manual mappings. Default keyboard profile is retained. Unrelated config preferences are preserved, modified XML backed up; size/DTD/root/link checks reject unsafe or damaged configuration. Added per-game OpenGL/Vulkan choice; not forced as a purported visual fix. Play! remains optional and separate from PCSX2 saves. Working profile restore now includes Play! renderer selection.

Added Play with PCSX2 — own BIOS directly in game details. Supplied BIOS matches the existing managed copy: 4 MiB, SHA256 F4C948E61A291D4B3F92A141E550CF8357204287A31FF784CACCBEDAEF910C9D. Source: user's D:\ps2 bios usa\SCPH-39001_BIOS_V7_USA_160_(NTSC)\SCPH-39001_BIOS_V7_USA_160.BIN. User-provided proprietary firmware, not downloaded or licensed for redistribution. Managed copy verified unchanged. Changed UseBiosFreePs2 to false, keeping BIOS path and all other preferences; prior full settings saved to D:\Umbra-Diagnostics\settings-before-pcsx-default.json. No native save migration. Play! and PCSX2 saves remain separate.

Current published build D:\Umbra-PS2-Preview\Umbra.exe. Desktop Launch UMBRA points to it; older builds remain recoverable. The earlier running frontend was asked to close normally and exited before settings migration; no forced user game termination.

## Evidence

- PASS: 104 unit/fixture tests. New checks cover Play XInput provider, Start/Cross and both sticks, actual slot2 routing, inactive second pad, keyboard preservation, unrelated XML preferences and unsafe XML refusal. D:\Umbra-Diagnostics\umbra-tests-de6bc933604547f7a8ff550ce976954f.
- PASS: self-contained desktop compilation/publish.
- Actual PCSX2 2.8.2 isolated runtime: accepted BIOS as USA v01.60 (07/02/2002); identified disc SLUS-20486; loaded/executed game ELF CRC 4D228733; opened Xbox/XInput controller via SDL, six axes/eleven buttons. These are runtime BIOS acceptance/game-start/device-detection facts, not physical button or gameplay verification.
- Probe alive after 20 seconds. Requested window close, then forcibly terminated ONLY isolated probe after five seconds without exit; exit -1. No clean exit or native-save roundtrip claim. New test memory cards were in D:\Umbra-Diagnostics\pcsx-bios-runtime, never user's save folder. Missing NVRAM/MEC diagnostics occurred on this fresh isolated config; engine continued through game boot.
- Evidence D:\Umbra-Diagnostics\pcsx-bios-runtime\engines\PS2\PCSX2\logs\emulog.txt. Command EngineProbe --pcsx-boot <PCSX2 exe> <user MVC2 bin> <user BIOS> <isolated data>.
- Play generated XML: configuration/fixture coverage only. Play physical input and Vulkan runtime are IMPLEMENTED — RUNTIME VERIFICATION REQUIRED. PCSX2 video/physical input similarly awaits user result. Existing N64 behavior is not changed.

## References and downloads

Play! source for installed tag 0.72, commit 8de4a71f5215ef38e357ac77a07948eb3424448f: https://github.com/jpd002/Play-/blob/0.72/Source/ui_qt/win32/InputProviderXInput.cpp ; https://github.com/jpd002/Play-/blob/0.72/Source/input/InputBindingManager.cpp ; https://github.com/jpd002/Play-/blob/0.72/Source/ui_qt/settingsdialog.h . Provider 'xinp' = 2020175472; native renderer enum OpenGL0/Vulkan1. No emulator engine/ROM/BIOS download.

Official project compatibility report https://github.com/jpd002/Play-Compatibility/issues/123 describes MVC2 visual glitches/slowdowns in historical tests. It does not prove the cause of this Windows 0.72 recording and is not UMBRA compatibility certification.

Diagnostic-only imageio-ffmpeg 0.6.0 Python wheel installed from PyPI into D:\Umbra-Diagnostics\python to inspect the supplied video (bundled FFmpeg7.1). Not shipped in UMBRA. Downloaded source reference files and diagnostics binary hashes are in D:\Umbra-Diagnostics\download-hashes.json. No external upload of user video or firmware.

## User check

Open Launch UMBRA. Select Marvel vs. Capcom 2, press normal Play (now PCSX2) or Play with PCSX2 — own BIOS. At the title/menu test Start/Menu and A; then test directional input in a menu. An unskippable cutscene alone cannot establish controller failure. Report whether flashing/black frames occur during gameplay as well as the intro. Do not expect Play! native progress to appear in PCSX2's separate memory cards. Development preview; no production readiness or save-roundtrip claim.

Final checks: isolated UI smoke passed startup bounds at 150% DPI, five rendered pages, game-details construction and tuning dialog lifecycle. Evidence D:\Umbra-Diagnostics\ps2-ui-smoke\smoke-results.txt. Recording blackdetect found a 0.1-second black interval at 7.466667–7.566667 seconds; this alone cannot distinguish a normal edit from an emulation artifact. It is not a verified graphics fix.
