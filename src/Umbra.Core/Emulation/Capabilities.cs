namespace Umbra.Core.Emulation;

public sealed record BackendCapabilities(
    bool SaveStates,
    bool StartupState,
    bool Screenshots,
    bool AutosaveOnExit,
    bool HotPlugControllers,
    string Notes);

public static class CapabilityCatalog
{
    public static BackendCapabilities For(ConsoleKind platform) => platform switch
    {
        ConsoleKind.Xbox360 => new(false,false,false,false,false,
            "Experimental Xenia support. Native Xbox/XInput controls; UMBRA remapping is unavailable. Native content saves are backed up; keep game packages outside the save folder. Save states, screenshot automation, stretch, enhancements and hot-plug verification are not integrated. No BIOS required. IMPLEMENTED — RUNTIME VERIFICATION REQUIRED."),
        ConsoleKind.N64 => new(true, true, true, false, true,
            "Mupen64Plus supports startup state loading and frame screenshots. State hotkeys and automatic state capture remain engine-controlled."),
        ConsoleKind.GameCube => new(true, true, true, false, true,
            "Dolphin supports state files and screenshots, but this adapter does not synthesize hotkeys or force a state save."),
        _ => new(true, true, true, false, true,
            "PCSX2 supports state slots and screenshots, but state hotkeys and automatic state capture remain engine-controlled."),
    };
}

public sealed class StateSlotService(AppPaths paths, EventLog log)
{
    // Keep state slots inside each engine's native state directory so the normal
    // save backup covers them without mixing formats between backends.
    public string Folder(ConsoleKind platform) { var name=platform switch { ConsoleKind.PS2=>"sstates", ConsoleKind.GameCube=>"StateSaves", _=>"states" }; var folder=Path.Combine(paths.Engine(platform),name); Directory.CreateDirectory(folder); return folder; }
    public string SlotPath(ConsoleKind platform, int slot)
    {
        if (slot is < 0 or > 9) throw new UserError("state-slot", "Choose a save-state slot from 0 to 9.");
        return Path.Combine(Folder(platform), $"slot-{slot}.state");
    }
    public string ScreenshotFolder(ConsoleKind platform) { var folder=Path.Combine(paths.Engine(platform),"screenshots"); Directory.CreateDirectory(folder); return folder; }
    public IReadOnlyList<string> Existing(ConsoleKind platform) => Directory.EnumerateFiles(Folder(platform), "slot-*.*", SearchOption.TopDirectoryOnly).OrderBy(x=>x).ToArray();
    public LaunchPlan LoadOnStart(LaunchPlan plan, int slot)
    {
        var capability=CapabilityCatalog.For(plan.Platform); if(!capability.StartupState) throw new UserError("state-unsupported", "This engine cannot load a save state at startup.");
        var state=SlotPath(plan.Platform,slot); if(!File.Exists(state)) throw new UserError("state-missing", $"Save-state slot {slot} is empty.");
        var args=new List<string>(plan.Arguments);
        if(plan.Platform==ConsoleKind.N64) args.InsertRange(Math.Max(0,args.Count-1),["--savestate",state]);
        else if(plan.Platform==ConsoleKind.PS2) args.InsertRange(0,["-statefile",state]);
        else args.InsertRange(0,["-s",state]);
        log.Write("state.load",$"{plan.Platform};slot={slot}"); return plan with { Arguments=args };
    }
    public string NewScreenshotPath(ConsoleKind platform) => Path.Combine(ScreenshotFolder(platform), $"umbra-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.png");
    public void RegisterStateFile(ConsoleKind platform,int slot,string source)
    {
        if(!CapabilityCatalog.For(platform).SaveStates)throw new UserError("state-unsupported","State files are not supported by this integration.");
        var destination=SlotPath(platform,slot); if(!File.Exists(source)) throw new UserError("state-missing","The engine did not create a save-state file.");
        if((File.GetAttributes(source)&FileAttributes.ReparsePoint)!=0) throw new UserError("state-link","Choose a regular state file, not a redirected link.");
        if(new FileInfo(source).Length>2L*1024*1024*1024) throw new UserError("state-size","The state file exceeds the 2 GB safety limit.");
        if(Path.GetFullPath(source).Equals(Path.GetFullPath(destination),StringComparison.OrdinalIgnoreCase)) return;
        File.Copy(source,destination,true); log.Write("state.register",$"{platform};slot={slot}");
    }
}
