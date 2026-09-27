# Emulation backends and license review

Source and release information inspected during this build on 2026-09-07/08. Upstream details can change; pin an engine and revalidate before release.

| Engine | Integration | License considerations | Verification |
| --- | --- | --- | --- |
| PCSX2 2.8.2 | `-nogui -batch -datapath <parent> -fullscreen -- <game>`; private INI/BIOS/cards | PCSX2 project uses GPL-3.0-or-later; components may have other notices. Corresponding source and notices required for redistribution. | Official Windows x64 archive `pcsx2-v2.8.2-windows-x64-Qt.7z`, SHA-256 `7dfc829ca1994cc1045ac49f05e39b6cf968b72e6a374c40e05c2a2b4ac200b4`; isolated `-testconfig` exit 0 only. |
| Dolphin | `-b -u <managed-user-folder> -e <game>`; private GFX/Dolphin/GCPad INIs | GPL-2.0-or-later; preserve attribution, licenses and applicable source obligations. | Source/fixture verified; real runtime pending. |
| Mupen64Plus 2.6.0 | Console UI plus shared core/plugins; explicit config/data/plugin directories | Core LICENSES identifies GPL version 2; UI source permits version 2 or later. Plugins/dependencies require individual review. Do not treat all components as one blanket license. | Official Windows x64 bundle `mupen64plus-bundle-win64-2.6.0.zip`, SHA-256 `8292c68d6ffc3428d4181ac09b1368ef2adb2cc568325545aac78cb6c1e66e21`; GPL demo `m64p_test_rom.v64`, SHA-256 `b5fe9d650a67091c97838386f5102ad94c79232240f9c5bcc72334097d76224c`, rendered two frames and exited normally. |

The installer does not redistribute emulator binaries. The optional N64 installer downloads the unchanged official archive directly from upstream, verifies SHA-256 `8292C68D6FFC3428D4181AC09B1368EF2ADB2CC568325545AAC78CB6C1E66E21`, retains license documents, and excludes game files. An engine bundle is not proof that every DLL is covered by the same license. A future fully bundled release must accompany the exact binaries with the relevant corresponding source, build/installation information and third-party notices; do not substitute a generic homepage link for those obligations.

The official Dolphin site exposes rolling Windows builds rather than GitHub release assets. The build environment's network policy blocked the official `dl.dolphin-emu.org` download during this continuation, so no Dolphin binary or GameCube test title was downloaded or installed here. The verified source license remains GPLv2+; runtime status stays **IMPLEMENTED — RUNTIME VERIFICATION REQUIRED**.

## Primary sources

- [PCSX2 CLI](https://pcsx2.net/docs/advanced/cli/)
- [PCSX2 GPL license](https://github.com/PCSX2/pcsx2/blob/master/COPYING.GPLv3)
- [PCSX2 2.8.2 release](https://github.com/PCSX2/pcsx2/releases/tag/v2.8.2)
- [PCSX2 data path and settings source](https://github.com/PCSX2/pcsx2/blob/v2.8.2/pcsx2/Pcsx2Config.cpp)
- [Dolphin license FAQ](https://dolphin-emu.org/docs/faq/)
- [Dolphin CLI source](https://github.com/dolphin-emu/dolphin/blob/master/Source/Core/UICommon/CommandLineParse.cpp)
- [Dolphin graphics settings](https://github.com/dolphin-emu/dolphin/blob/master/Source/Core/Core/Config/GraphicsSettings.cpp)
- [Mupen64Plus 2.6.0 release and source bundles](https://github.com/mupen64plus/mupen64plus-core/releases/tag/2.6.0)
- [Mupen64Plus core licenses](https://github.com/mupen64plus/mupen64plus-core/blob/master/LICENSES)
- [Mupen64Plus UI arguments/source license](https://github.com/mupen64plus/mupen64plus-ui-console/blob/master/src/main.c)

## Adapter limits

The verified N64 bundle defaults to Rice, not GLideN64. The adapter writes conservative settings for both. Rice native framebuffer mode and no texture enhancements are selected in Original; output remains 640×480. A single resolution policy cannot match every title's original video mode. Enhanced chooses a larger output/native factor. Original PS2/GameCube internal scale is 1; enhancements use 3. Widescreen/texture/cheat patches are disabled. Engine-default compatibility fixes may still operate.

Mupen64Plus auto SDL mapping is intentionally retained because raw joystick indexes differ from standardized game-controller indexes. UMBRA now applies the saved logical N64 manual profile to each device's SDL expressions while preserving a safe default for unknown inputs. PS2 uses SDL-style binding strings; GameCube uses XInput expressions. These mappings require physical-device tests before release.
