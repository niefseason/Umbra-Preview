using System.IO.Compression;
using System.Text.Json;
using Umbra.Core.Emulation;
namespace Umbra.Core;
public static class Diagnostics
{
    public static List<Issue> Check(AppPaths paths,Settings settings,int controllers)
    {
        var result=new List<Issue>();
        foreach(var platform in Enum.GetValues<ConsoleKind>())
        {
            var backend=settings.Backends[platform];
            try { Adapters.ValidateBackend(backend); result.Add(new(platform.ToString(),"PARTIAL",$"{Adapters.Name(platform)} {backend.Version}: executable exists and matches its recorded hash. RUNTIME VERIFICATION REQUIRED for game compatibility.")); }
            catch(UserError e) { result.Add(new(platform.ToString(),e.Code=="backend-missing"?"MISSING":"FAIL",e.Message)); }
        }
        try { ContentDetection.ValidateBios(settings.BiosPath); result.Add(new("PS2 BIOS","NEEDS ATTENTION","Structure accepted. PCSX2 must verify this dump when a game boots.")); }
        catch(UserError e) { result.Add(new("PS2 BIOS",e.Code=="bios-missing"?"MISSING":"ERROR",e.Message)); }
        try{Adapters.ValidateBackend(settings.BiosFreePs2);result.Add(new("PS2 BIOS-free option","PARTIAL",$"Play! executable hash accepted; {(settings.UseBiosFreePs2?"selected":"not selected")}. No Sony BIOS needed. Gameplay, controls, graphics and saves require runtime verification."));}
        catch(UserError e){result.Add(new("PS2 BIOS-free option",settings.UseBiosFreePs2?"FAIL":"MISSING",e.Message));}
        result.Add(new("Controllers",controllers>0?"DETECTED":"MISSING",controllers>0?$"{controllers} XInput controller(s) connected. In-game bindings and drift require runtime verification.":"Connect an XInput controller. Other SDL devices may work in the engine but are not detected by this build."));
        foreach(var folder in settings.LibraryFolders) result.Add(new("Library folder",Directory.Exists(folder)?"READY":"MISSING",Directory.Exists(folder)?"Available.":"Reconnect the drive or add its new location."));
        try
        {
            var saves=new SaveService(paths,new EventLog(paths)); foreach(var platform in Enum.GetValues<ConsoleKind>()) saves.Ensure(platform);
            var drive=new DriveInfo(Path.GetPathRoot(paths.Root)!);
            result.Add(new("Save storage",drive.AvailableFreeSpace>512L*1024*1024?"READY":"NEEDS ATTENTION",$"{drive.AvailableFreeSpace/(1024.0*1024*1024):0.0} GB free. Save directories are writable."));
        }
        catch(Exception e) when(e is IOException or UnauthorizedAccessException or UserError) { result.Add(new("Save storage","ERROR","Save storage is unavailable. Check disk space and folder permissions.")); }
        result.Add(new("Graphics","NEEDS ATTENTION","Rendering support and full-speed emulation must be tested in-game. WPF rendering availability is not an emulator performance test."));
        result.Add(new("Audio","RUNTIME VERIFICATION REQUIRED","The engine controls audio output. No end-to-end audio playback test has been reported; device presence alone does not verify sound."));
        return result;
    }
    public static string Report(AppPaths paths,Settings settings,int controllers) => JsonSerializer.Serialize(new {
        app="Umbra",version="0.1.0",os=Environment.OSVersion.ToString(),architecture=System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
        profile=settings.Display.Profile.ToString(),checks=Check(paths,settings,controllers),
        backends=settings.Backends.ToDictionary(x=>x.Key.ToString(),x=>new {x.Value.Version,x.Value.Sha256})
    },JsonStore.Options);
    public static void Export(string destination,AppPaths paths,Settings settings,int controllers)
    {
        using var zip=ZipFile.Open(destination,ZipArchiveMode.Create);
        using(var writer=new StreamWriter(zip.CreateEntry("diagnostics.json").Open())) writer.Write(Report(paths,settings,controllers));
        foreach(var name in new[]{"umbra.jsonl","umbra.jsonl.1"}) { var file=Path.Combine(paths.Logs,name); if(File.Exists(file)) zip.CreateEntryFromFile(file,name); }
    }
}
