namespace Umbra.Core.Emulation;

public static class XeniaAdapter
{
    public static LaunchPlan Prepare(AppPaths paths,Settings settings,Game game,DisplaySettings? display=null,int? startupState=null,BackendSettings? audioBackend=null)
    {
        if(!File.Exists(game.Path))throw new UserError("game-missing","Reconnect the drive containing this Xbox 360 dump.");
        ContentDetection.ValidatePlatformFormat(game.Path,ConsoleKind.Xbox360);
        var backend=audioBackend??settings.Backends[ConsoleKind.Xbox360];Adapters.ValidateBackend(backend);
        if(startupState.HasValue)throw new UserError("state-unsupported","Xbox 360 save-state loading is not integrated. Use in-game saves.");
        display??=new(){Fullscreen=settings.Display.Fullscreen};JsonStore.ValidateDisplay(display);
        if(display.Stretch || display.Profile is GraphicsProfile.Enhanced or GraphicsProfile.Custom || display.Scale!=1)
            throw new UserError("xenia-display","Xbox 360 currently supports native presentation and fullscreen only. Stretch and enhanced profiles are not integrated.");
        var baseRoot=paths.Engine(ConsoleKind.Xbox360);
        var root=audioBackend is null?baseRoot:Path.Combine(baseRoot,"Canary");SaveService.RejectLinks(root,paths.Root);
        var content=Path.Combine(baseRoot,audioBackend is null?"content":"content-canary");
        SaveService.RejectLinks(content,paths.Root);Directory.CreateDirectory(content);
        Directory.CreateDirectory(root);
        foreach(var folder in new[]{"content","cache"}){SaveService.RejectLinks(Path.Combine(root,folder),paths.Root);Directory.CreateDirectory(Path.Combine(root,folder));}
        // Official Xenia CLI options override config; game paths remain individual arguments.
        // Keep native Xbox controls and game speed. Never load a guest XEX as a host process.
        var arguments=new List<string>{ $"--storage_root={root}",$"--content_root={content}",$"--cache_root={Path.Combine(root,"cache")}",
             "--hid=xinput","--discord=false",$"--fullscreen={display.Fullscreen.ToString().ToLowerInvariant()}",$"--target={Path.GetFullPath(game.Path)}"};
        if(audioBackend is not null)
        {
            // Canary's config dump did not reflect command-line-only XMA values.
            // Supply a small explicit config so the tested decoder choice is visible.
            var config=Path.Combine(root,"umbra-audio.toml");
            foreach(var file in new[]{config,config+".tmp",config+".bak"})SaveService.RejectLinks(file,paths.Root);
            File.WriteAllText(config+".tmp","[APU]\nxma_decoder = \"new\"\nuse_dedicated_xma_thread = false\n");
            if(File.Exists(config))File.Copy(config,config+".bak",true);
            File.Move(config+".tmp",config,true);arguments.Add("--config="+config);
        }
        return new(ConsoleKind.Xbox360,backend.Executable,root,arguments,root);
    }
}
