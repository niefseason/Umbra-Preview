# Development

Requirements: Windows 10/11 x64, .NET SDK 8.0.424 or compatible .NET 8 SDK. Inno Setup 6.7.3 is used for the release installer. No external NuGet libraries are used; self-contained publishing obtains Microsoft runtime packs.

```powershell
dotnet build src/Umbra.Desktop -c Release
dotnet build tests/Umbra.TestBackend -c Release
dotnet run --project tests/Umbra.Tests -c Release -- tests/Umbra.TestBackend/bin/Release/net8.0/Umbra.TestBackend.exe
dotnet run --project src/Umbra.Desktop -- --data .\artifacts\dev-data
```

Use absolute paths when invoking the probe or changing working directories. `scripts/build.ps1` runs tests and self-contained publish, and optionally Inno Setup. Build failures stop the script.

```powershell
.\scripts\build.ps1 -InnoCompiler 'C:\path\to\ISCC.exe'
```

For a render smoke run, invoke the built application with `--smoke --data <empty-absolute-directory>`. It creates five PNGs, exercises game details and exits. This is a screen construction/render check, not a click-through accessibility test.

Branding: edit `config/branding.json` (name, glyph, accent, background, panel, text, muted color and font), then build. Adapter/platform IDs and user data identity are intentionally stable. Replace `assets/umbra.ico` to update the executable and installer icon. The UI uses original vector geometry.

Backend changes require source inspection, regression tests and a real engine boot check. Never introduce arbitrary command templates, scripts next to imported files, game downloads or shared emulator config folders. Do not claim a fixture-process test proves emulator compatibility.

Development artifacts under `artifacts/`, `bin/` and `obj/` are ignored. No real user library or BIOS belongs in the repository. Tests generate clearly synthetic header/BIOS fixtures and retain their temporary output for inspection; these are not valid game or BIOS releases.
