# Working profiles and launcher — 2026-09-26

Completed the first next-pipeline milestone: preserve user-confirmed working setup and remove launch-path ambiguity.

- Desktop Launch UMBRA.lnk targets D:\Umbra-Fullscreen-Preview\Umbra.exe, verified by reading the saved shortcut. Old preview folders remain as recovery copies; use this shortcut for ongoing work. External drive must remain connected as D:. Disconnected-drive recovery/migration is a later milestone.
- Game details now include Save working profile, Restore working profile, Undo profile restore, and Use global controller settings again. Capture after saving the control/display sections and testing the game. Capturing a profile is a user designation, never automated gameplay certification.
- Captures freeze effective display and base controller settings plus per-game preset, renderer and inversion/action options. Restore freezes controller settings for this game, insulating it from later global edits; UI explains this and provides an explicit return to global settings. Automatic SDL discovery for modern presets still happens at launch.
- Restore preserves the current startup-state slot and notes. Native saves, ROMs, firmware, engine files/selection, and global settings are not restored or modified. Renderer binaries and engine compatibility must still be checked by normal launch validation. In-game control style 1.2 is still configured inside each game, not by this snapshot.
- Before restore, the prior per-game override is saved for undo. Repeated saves preserve the previous working JSON as .bak. Paths validate game ID and reject redirected links including backup destinations; malformed/oversized, wrong-version or wrong-game snapshots are rejected before writing the active override.
- Seeded GoldenEye and Perfect Dark working snapshots from existing current settings following the user's successful-play feedback. Stored under %LOCALAPPDATA%\Umbra\profiles, copied to D:\Umbra-Working-Profiles-20260926\profiles. No restore was applied to the user's live setup. No game progress changed.

## Evidence

99 unit/fixture tests pass, including detached capture versus later global changes, settings restore/undo, preserved state slot/notes, corrupted JSON rejection without changing active settings, wrong identity/schema rejection, invalid mapping and traversal rejection. Artifacts: D:\Umbra-PerfectDark-Work\umbra-tests-ba0dd7e813a84f949f3a7376fe514763. Standard bundled dotnet test harness invocation as in earlier evidence. Self-contained publish succeeded in existing external preview folder; no UMBRA process was running during replacement. Snapshot capture used EngineProbe --save-working <data> <game-id>, which calls the same service as the UI. No downloads.

The user confirmed the prior startup sizing fix works. Record GoldenEye controls, Perfect Dark follow-up and startup sizing as user-reported success on their setup, not blanket controller/game/release certification. Snapshot unit tests are not gameplay/save-roundtrip evidence. This remains a development preview.

Next pipeline priorities: camera sensitivity/deadzone controls with drift diagnostics; external-drive data migration/disconnection handling; expanded game-aware profiles and controller-accessible menus. Each needs scoped validation before claiming support.

PASS isolated UI smoke after publish: startup bounds contained at 150% DPI; Home, Library, Controllers, Settings, System Status rendered; game details with profile panel constructed. Evidence D:\Umbra-PerfectDark-Work\working-profile-ui-smoke\smoke-results.txt. This checks interface construction, not manual button interaction or gameplay.
