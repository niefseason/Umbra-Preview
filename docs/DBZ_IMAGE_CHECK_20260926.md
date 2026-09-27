# Dragon Ball Z disc-image inspection — 2026-09-26

## Result

PASS — full-image SHA-1 and byte length match the USA En/Ja SLUS-21678 entry in Libretro's Redump-derived PS2 metadata (DAT version 2026.08.01). Direct Redump access was unavailable, so this is explicitly a comparison against the maintained mirror, not a fresh direct Redump query. No image data was downloaded, uploaded, modified or redistributed.

Local image: `D:\Dragon Ball Z - Budokai Tenkaichi 3 (USA) (EnJa)\Dragon Ball Z - Budokai Tenkaichi 3 (USA) (En,Ja).iso`.

- Length: 3,020,816,384 bytes, matching reference.
- SHA-1, measured over the entire image with PowerShell Get-FileHash: `A1C4D21C154DA8B1D4B15055907EDB0ABB055E63`, matching reference.
- MD5, independently measured over the entire image: `350404A81DE912347C8B262542FED970`, also matching reference.
- ISO primary volume descriptor: CD001; volume SLUS_21678; 2,048-byte sectors; 1,475,008 sectors; declared volume bytes exactly equal file size. Root directory sector 261.
- Existing PCSX2 runtime identifies version 1.00, serial SLUS-21678 and executable CRC 428113C2. That executable CRC is not a whole-disc integrity check.

Reference: [Libretro PS2 Redump metadata](https://github.com/libretro/libretro-database/blob/master/metadat/redump/Sony%20-%20PlayStation%202.dat), USA En/Ja entry. Reference CRC32 is `10466681` (not independently measured).

## User runtime evidence and diagnosis

User confirms stretch succeeds, the performance overlay is unobtrusive, characters look sharper at 2×, backgrounds/menus remain faded, and 100 ms buffering produced little audible improvement. User reports speed stays near 100% and output is through laptop speakers. This supports the specific stretch result and perceived character improvement; it does not establish universal graphics accuracy or objectively clean audio.

The latest engine log records 2×/Stretch in the INI, Cubeb 48 kHz, buffer=100 ms, output latency=20 ms, and time stretching enabled. It also confirms all three title-specific GS fixes were applied: halfPixelOffset=5, nativeScaling=4 and beforeDraw=1. These match the [official PCSX2 2.8.2 game database](https://github.com/PCSX2/pcsx2/blob/v2.8.2/bin/resources/GameIndex.yaml) entry for SLUS-21678, including post-effect alignment and lighting fixes. No evidence justifies applying old forum skipdraw/renderer hacks blindly.

Inference: sharper rendered geometry does not establish that source textures/menu images or the game's post-processing have improved. Increasing resolution is not a texture replacement or a color correction. The exact cause of the faded appearance remains unverified without an image comparison. Near-100% speed makes sustained emulation slowdown less likely as the audio cause, but does not rule out brief interruptions, original audio characteristics, speaker distortion or output processing.

Other observed log warnings: invalid SDL face-button strings SDL-0/Y, B, A and X, and a COP2 old-value diagnostic. These are separate issues; neither establishes an audio/graphics cause or a bad disc. Physical controller behavior remains separately tracked; no controller mapping was changed during this inspection.

Reviewed runtime log retained at `D:\Umbra-Diagnostics\dbz-image-check\emulog-reviewed.txt`. No game, save, controller, display or system-audio settings changed this turn. No code changes or repeat unit test claims.

## Next targeted comparisons

1. Compare a repeatable scene at moderate laptop volume, then through wired headphones if available. Record whether fuzziness affects music, voices, effects or all audio.
2. Compare ordinary music/video through the same speakers and volume. If it also distorts, investigate the speaker/output path before engine changes.
3. Capture an example of the faded map/menu. Compare the same scene in native 1× and 2×, retaining the working profile and game fixes. Color fading and texture softness require different remedies.
4. Since additional buffering did not help, 50 ms remains an available lower-latency comparison; no automatic rollback was performed.

For obtaining dependable game files, the recommended provenance is a dump of the user's own disc using the [official PCSX2 disc-dumping guide](https://pcsx2.net/docs/setup/discs/) and verification against preservation checksums. Redump labels in filenames do not themselves prove integrity; the full-image checksum does. This finding supplies no reason to replace the current image.

Audio quality and remaining graphics behavior: **IMPLEMENTED — RUNTIME VERIFICATION REQUIRED**. UMBRA remains a development preview.
