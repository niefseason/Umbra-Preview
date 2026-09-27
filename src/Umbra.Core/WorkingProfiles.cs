using System.Text.Json;
namespace Umbra.Core;

public sealed class WorkingProfile
{
    public int Version {get;set;}=1;
    public string GameId {get;set;}="";
    public ConsoleKind Platform {get;set;}
    public DateTimeOffset Created {get;set;}=DateTimeOffset.UtcNow;
    public GameOverride Options {get;set;}=new();
}

// Settings only. Never restores native saves, engine binaries or firmware.
public sealed class WorkingProfiles(AppPaths paths)
{
    string FileFor(Game game,string slot)
    {
        _=paths.Override(game.Id); // validates the ID before building any path
        var file=Path.Combine(paths.Root,"profiles",game.Id,slot+".json");
        SaveService.RejectLinks(file,paths.Root);SaveService.RejectLinks(file+".bak",paths.Root);
        return file;
    }
    static T Copy<T>(T value)=>JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value,JsonStore.Options),JsonStore.Options)!;
    public bool Exists(Game game)=>File.Exists(FileFor(game,"working"));
    public bool CanUndo(Game game)=>File.Exists(FileFor(game,"before-restore"));
    public void Save(Game game,Settings settings,GameOverride options,DisplaySettings effectiveDisplay)
    {
        var frozen=Copy(options);
        frozen.Display=Copy(effectiveDisplay);
        frozen.SavedController=Copy(options.SavedController??settings.Controller);
        var snapshot=new WorkingProfile{GameId=game.Id,Platform=game.Platform,Options=frozen};
        Validate(snapshot,game);
        JsonStore.Write(FileFor(game,"working"),snapshot);
    }
    WorkingProfile Read(Game game,string slot)
    {
        var file=FileFor(game,slot);
        if(!File.Exists(file))throw new UserError("profile-missing","No saved profile is available yet.");
        if(new FileInfo(file).Length>1024*1024)throw new UserError("profile-invalid","This profile is too large. Your current settings were retained.");
        try
        {
            var profile=JsonSerializer.Deserialize<WorkingProfile>(File.ReadAllText(file),JsonStore.Options)
                ??throw new JsonException();
            Validate(profile,game);return profile;
        }
        catch(JsonException){throw new UserError("profile-invalid","This profile is damaged. Your current settings were retained.");}
    }
    static void Validate(WorkingProfile profile,Game game)
    {
        if(profile.Version!=1 || profile.GameId!=game.Id || profile.Platform!=game.Platform || profile.Options is null)
            throw new UserError("profile-invalid","The saved profile is incompatible with this game. Your current settings were retained.");
        var o=profile.Options;
        o.PcsxTuning?.Validate();
        if(o.PlayRenderer is not ("opengl" or "vulkan"))throw new UserError("profile-invalid","Unsupported Play! renderer.");
        o.StickTuning?.Validate();
        if(o.Display is not null)JsonStore.ValidateDisplay(o.Display);
        if(o.ControllerPreset is not ("" or "classic" or "goldeneye-solitaire" or "modern-12" or "auto-modern" or "modern-shooter" or "no-c") || o.N64VideoPlugin is not ("" or "rice" or "glide64mk2"))
            throw new UserError("profile-invalid","The saved profile has unsupported settings.");
        if(o.SavedController is { } c)
        {
            (c.ModernTuning??new()).Validate();
            if(c.Buttons is null || c.SdlDevices is null || c.N64AnalogDeadzone is <0 or >32768)
                throw new UserError("profile-invalid","The saved controller profile is invalid.");
            _=Controllers.Translate(game.Platform,c);
            if(c.SdlDevices.Any(p=>p.Key is <0 or >3 || p.Value is null || p.Value.Inputs is null))
                throw new UserError("profile-invalid","The saved device mapping is invalid.");
        }
    }
    public GameOverride Restore(Game game,GameOverride current)
    {
        var saved=Read(game,"working");
        // Capture the exact previous override before replacing it. Globals remain untouched.
        JsonStore.Write(FileFor(game,"before-restore"),new WorkingProfile{GameId=game.Id,Platform=game.Platform,Options=Copy(current)});
        return Apply(game,current,saved.Options);
    }
    public GameOverride Undo(Game game,GameOverride current)=>Apply(game,current,Read(game,"before-restore").Options);
    GameOverride Apply(Game game,GameOverride current,GameOverride saved)
    {
        var result=Copy(current);
        result.Display=Copy(saved.Display);result.SavedController=Copy(saved.SavedController);
        result.StickTuning=Copy(saved.StickTuning);
        result.ControllerPreset=saved.ControllerPreset;result.N64VideoPlugin=saved.N64VideoPlugin;
        result.PlayRenderer=saved.PlayRenderer;
        result.PcsxTuning=Copy(saved.PcsxTuning);
        result.XeniaAudioBackend=Copy(saved.XeniaAudioBackend);
        result.InvertLook=saved.InvertLook;result.ReloadOnWest=saved.ReloadOnWest;result.WeaponOnLeft=saved.WeaponOnLeft;
        var destination=paths.Override(game.Id);SaveService.RejectLinks(destination,paths.Root);
        SaveService.RejectLinks(destination+".bak",paths.Root);
        JsonStore.Write(destination,result);
        return result;
    }
}
