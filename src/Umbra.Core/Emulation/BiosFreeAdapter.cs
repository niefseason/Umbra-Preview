namespace Umbra.Core.Emulation;

// Play! implements its own HLE firmware. It is a separate, opt-in backend, never a
// PCSX2 BIOS bypass, and its save formats must not be mixed with PCSX2 saves.
public static class BiosFreeAdapter
{
    public static LaunchPlan Prepare(AppPaths paths,BackendSettings backend,Game game,DisplaySettings display,int? startupState=null,ControllerProfile? controller=null,IReadOnlyList<int>? xinputDevices=null,string renderer="opengl")
    {
        Adapters.ValidateBackend(backend);
        JsonStore.ValidateDisplay(display);
        if(display.Stretch)throw new UserError("play-display","Stretch is not integrated for the experimental Play! backend.");
        if(game.Platform!=ConsoleKind.PS2 || !File.Exists(game.Path))throw new UserError("play-game","Select an available PS2 game.");
        ContentDetection.ValidatePlatformFormat(game.Path,game.Platform);
        if(startupState.HasValue)throw new UserError("play-state","Clear the PCSX2 startup state before using Play!. State formats are different.");
        if(display.Profile is GraphicsProfile.Enhanced or GraphicsProfile.Custom)throw new UserError("play-display","The experimental Play! path supports Original and Clean Original only. Enhanced and Custom are not integrated for this engine.");
        var root=Path.Combine(paths.Engine(ConsoleKind.PS2),"Play");Directory.CreateDirectory(root);SaveService.RejectLinks(root,paths.Root);
        var marker=Path.Combine(root,"portable.txt");SaveService.RejectLinks(marker,paths.Root);if(!File.Exists(marker))File.WriteAllText(marker,"");
        PlayConfiguration.Configure(root,controller,xinputDevices,renderer);
        var args=new List<string>{Path.GetExtension(game.Path).Equals(".elf",StringComparison.OrdinalIgnoreCase)?"--elf":"--disc",Path.GetFullPath(game.Path)};
        if(display.Fullscreen)args.Add("--fullscreen");
        return new(ConsoleKind.PS2,backend.Executable,root,args,root);
    }
}
