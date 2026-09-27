# Troubleshooting

**Missing engine:** open System Status. Download N64 support inside UMBRA or select the correct engine `.exe` from its official portable/extracted package. Keep the engine's DLLs and resources together. Do not select a script or shortcut.

**Engine changed:** select it again after an intentional update. UMBRA checks the selected executable hash before launch. Changing a backend does not delete settings or saves. Incompatible future engine versions may require an adapter update.

**PCSX2 portable mode:** an engine with `portable.ini` or `portable.txt` would ignore the isolated data path. Select a separate standard extracted build without those markers. UMBRA does not alter another installation's portable configuration.

**Missing/invalid BIOS:** Settings → Select BIOS. Choose a complete dump from your own PS2, not an archive. Structural screening checks size and ROM-directory markers; PCSX2 performs the authoritative validation. UMBRA privately copies the selected BIOS and never overwrites its source.

**Game missing after moving a drive/folder:** open game details → Locate moved file. Matching size/header fingerprint retains favorite/playtime/ID. A different file must be imported separately. Old entries stay visible when a drive is disconnected.

**Scan skips a file:** use Add game and choose its console. Generic ISO/CHD/compressed disc containers cannot always be inferred safely. Archives must be extracted outside UMBRA. Wii is not a supported target.

**No controller:** connect an XInput-compatible controller. Other SDL devices may be recognized by an engine even though this frontend reports no XInput controller. No physical controller input has been verified on the test host. N64 uses upstream auto configuration; manual remaps in UMBRA are currently applied only to PS2/GameCube.

**Game remains open:** quit from the game or restore UMBRA from the taskbar and choose Quit active game. The engine may ask for confirmation. Force stop is a last resort and can lose unsaved progress. The pre-launch backup remains available.

**Save backup failed:** gameplay is blocked if the pre-launch backup fails. Check free space and write permissions. Individual snapshot content is limited to 2 GB. Move older backups to another drive if needed; no automatic pruning occurs. Data folders redirected through filesystem links are rejected by save protection.

**Restore interrupted:** a pre-restore recovery ZIP was created before replacing matching live files. Retry from that archive once disk/permission issues are fixed. Restore retains files absent from the archive. Backups are best made when every emulator using those folders is closed.

**Stuttering or bad graphics:** Original uses native-oriented settings but cannot guarantee full speed. Keep normal game speed, avoid enhancement profiles, and verify the particular game against upstream compatibility resources. UMBRA does not measure frame times or GPU load and will not claim your machine passes performance requirements from a simple device check.

**Corrupt settings:** the loader keeps a `.corrupt-*` copy and tries `.bak`; future-version settings are refused instead of downgraded. Do not delete the data folder to troubleshoot saved games.

**Diagnostics:** System Status → Copy report / Export bundle. Launcher logs record controlled event names, platform IDs and error types, not your original paths or titles. Full engine logs may contain private paths and are intentionally excluded. Developer details exposes local paths on screen for your own use.
