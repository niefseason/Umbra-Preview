# Engine and test-asset download record

Recorded during the continuation release check. Hashes are SHA-256 of the downloaded archive or test asset. Nothing in this record is bundled into UMBRA's release archives.

| Asset | Official source | Version | License | SHA-256 | Result |
| --- | --- | --- | --- | --- | --- |
| Mupen64Plus Windows bundle | `https://github.com/mupen64plus/mupen64plus-core/releases/download/2.6.0/mupen64plus-bundle-win64-2.6.0.zip` | 2.6.0 | GPLv2 core/UI; plugins and dependencies retain individual notices | Archive `8292c68d6ffc3428d4181ac09b1368ef2adb2cc568325545aac78cb6c1e66e21`; `mupen64plus-ui-console.exe` `db0f420ebb65211b6e69223958d9100ba2e1e2fc0180a4ffc0239c573223003a` | Downloaded, extracted to an isolated probe directory, startup/render/exit verified |
| Mupen64Plus demo | Included in the official bundle as `m64p_test_rom.v64` | Official GPL demo | GPL (as identified by the engine) | `b5fe9d650a67091c97838386f5102ad94c79232240f9c5bcc72334097d76224c` | Rendered frames 10 and 60; exit code 0; not shipped |
| PCSX2 Windows Qt archive | `https://github.com/PCSX2/pcsx2/releases/download/v2.8.2/pcsx2-v2.8.2-windows-x64-Qt.7z` | 2.8.2 | GPL-3.0-or-later plus component notices | Archive `7dfc829ca1994cc1045ac49f05e39b6cf968b72e6a374c40e05c2a2b4ac200b4`; `pcsx2-qt.exe` `982c7c62600a999cf15a25c18349426166c785e7867b2fcc5018d733245b71a3` | Downloaded/extracted to an isolated probe directory; adapter-generated `-testconfig` exit code 0; no BIOS/game boot |
| PS2 test content | None | — | — | — | Not downloaded: no BIOS is redistributed and no PS2 homebrew asset was fetched under the current network/approval limit |
| Dolphin Windows x64 build | `https://dl.dolphin-emu.org/releases/2603a/dolphin-2603a-x64.7z` (official rolling build URL) | 2603a | GPLv2+ | — | Download blocked by the current network/approval policy; no binary or GameCube content was installed |
| GameCube test content | None | — | — | — | Not downloaded: legal homebrew boot remains a user/approval-dependent runtime gate |

The current UMBRA downloader installs only the pinned N64 bundle after hash verification. PS2 and Dolphin are selected from user-installed official builds in System Status. A future bundled release requires an exact binary/source/notice audit for every engine component.
