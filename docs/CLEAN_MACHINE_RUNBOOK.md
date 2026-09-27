# Clean-machine and update validation runbook

This runbook prepares the release gates without restarting the current computer, signing binaries or publishing an update.

## Disposable Windows VM

1. Create a Windows 10/11 x64 VM snapshot with no UMBRA installation and no `%LOCALAPPDATA%\Umbra` directory. Record the Windows build, GPU/driver and architecture.
2. Copy `Umbra-0.1.0-Windows-Setup.exe` and `SHA256SUMS.txt` into the VM. Verify the setup hash before execution.
3. Run the installer as a standard user. Confirm the install path is under `%LOCALAPPDATA%\Programs\Umbra`, the Start-menu target points to `Umbra.exe`, and the first-run screens are keyboard accessible.
4. Run `Umbra.exe --smoke --data <new-absolute-folder>` and retain `smoke-results.txt` and the rendered PNGs. Import only legal test content and user-owned BIOS files for engine runtime checks.
5. Create a sentinel file under `%LOCALAPPDATA%\Umbra\engines\N64\save`, uninstall from Windows Apps, and verify the application files are removed while the entire `%LOCALAPPDATA%\Umbra` tree and sentinel remain unchanged.

## Upgrade, rollback and signing gates

1. Install the prior approved build in a separate snapshot, create settings, library and save fixtures, then install 0.1.0 over it. Verify schema migration, library identity, saves and backups before and after launch.
2. Test an interrupted update and rollback using a copied VM snapshot. The old build must remain launchable and user data must remain intact.
3. Build the update manifest from the final installer hash. Run `scripts/sign.ps1` only with the release certificate thumbprint and approved timestamp service, then verify Authenticode on the clean VM.
4. Exercise the real update channel, downgrade refusal and rollback policy only after the update endpoint, credentials and approval are available. Record installer, manifest, signature, rollback and data-retention evidence together.

The current same-host checks passed, but a clean VM, Windows restart, prior released build, signing credential, update endpoint and rollback rehearsal remain release gates.
