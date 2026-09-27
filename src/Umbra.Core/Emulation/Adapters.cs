using System.Diagnostics;
using System.Security.Cryptography;
namespace Umbra.Core.Emulation;

public sealed record LaunchPlan(ConsoleKind Platform, string Executable, string WorkingDirectory, List<string> Arguments, string DataDirectory)
{
    public ProcessStartInfo StartInfo()
    {
        var info=new ProcessStartInfo(Executable) { WorkingDirectory=WorkingDirectory, UseShellExecute=false, CreateNoWindow=true };
        foreach(var arg in Arguments) info.ArgumentList.Add(arg);
        return info;
    }
}
public static class Adapters
{
    public static string Name(ConsoleKind p) => p switch {ConsoleKind.PS2=>"PCSX2",ConsoleKind.GameCube=>"Dolphin",ConsoleKind.Xbox360=>"Xenia",_=>"Mupen64Plus"};
    public static string Website(ConsoleKind p) => p switch {ConsoleKind.PS2=>"https://pcsx2.net/downloads/",ConsoleKind.GameCube=>"https://dolphin-emu.org/download/",ConsoleKind.Xbox360=>"https://xenia.jp/download/",_=>"https://github.com/mupen64plus/mupen64plus-core/releases"};
    public static string HashExecutable(string path)
    { using var stream=File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(stream)); }
    public static void ValidateBackend(BackendSettings backend)
    {
        if(string.IsNullOrWhiteSpace(backend.Executable) || !File.Exists(backend.Executable)) throw new UserError("backend-missing", "This console engine is missing. Select its executable in System Status.");
        if(!Path.IsPathFullyQualified(backend.Executable) || (OperatingSystem.IsWindows() && !backend.Executable.EndsWith(".exe",StringComparison.OrdinalIgnoreCase))) throw new UserError("backend-invalid","Select the engine's .exe file, not a script or shortcut.");
        if(string.IsNullOrEmpty(backend.Sha256) || !string.Equals(HashExecutable(backend.Executable),backend.Sha256,StringComparison.OrdinalIgnoreCase)) throw new UserError("backend-changed","The engine has changed since you selected it. Select it again in System Status to approve this version.");
    }
    public static LaunchPlan Prepare(AppPaths paths, Settings settings, Game game, DisplaySettings? gameDisplay = null, int? startupState = null, string n64VideoPlugin = "", (int Width,int Height)? outputSize = null, PcsxTuning? pcsxTuning = null, BackendSettings? xeniaAudioBackend = null)
    {
        if(game.Platform==ConsoleKind.Xbox360)return XeniaAdapter.Prepare(paths,settings,game,gameDisplay,startupState,xeniaAudioBackend);
        if(!File.Exists(game.Path)) throw new UserError("game-missing","The game file cannot be found. Locate it again from the game details.");
        ContentDetection.ValidatePlatformFormat(game.Path,game.Platform);
        var backend=settings.Backends[game.Platform]; ValidateBackend(backend);
        if(game.Platform==ConsoleKind.PS2) ContentDetection.ValidateBios(settings.BiosPath);
        var display=gameDisplay??settings.Display; JsonStore.ValidateDisplay(display);
        if(display.Profile != GraphicsProfile.Custom && display.Stretch) throw new UserError("aspect", "Stretch is a custom presentation option. Select Custom or turn off stretch.");
        var root=paths.Engine(game.Platform); Directory.CreateDirectory(root);
        var args=new List<string>();
        var scale=DisplayManager.InternalScale(display);
        switch(game.Platform)
        {
            case ConsoleKind.N64:
                if(n64VideoPlugin is not ("" or "rice" or "glide64mk2"))
                    throw new UserError("video-plugin","Unknown N64 renderer. Choose a renderer in game details.");
                if(n64VideoPlugin.Length>0)
                {
                    var plugin=Path.Combine(Path.GetDirectoryName(backend.Executable)!,"mupen64plus-video-"+n64VideoPlugin+".dll");
                    if(!File.Exists(plugin))throw new UserError("video-plugin-missing","This engine does not include the selected graphics renderer. Choose Engine default or an engine distribution that includes it.");
                    args.AddRange(["--gfx",plugin]);
                }
                else args.AddRange(["--gfx","mupen64plus-video-rice.dll"]); // Preserve the existing default after another game's renderer selection.
                if(settings.Controller.GoldenEyeSolitaire)
                {
                    var detected=SdlControllerDiscovery.Read(Path.Combine(Path.GetDirectoryName(backend.Executable)!,"SDL2.dll"));
                    var selected=detected.Take(settings.Controller.Players).ToArray();
                    if(selected.Length<settings.Controller.Players)
                        throw new UserError("controller-missing","Connect the selected number of controllers before starting the modern 1.2 profile.");
                    for(int i=0;i<selected.Length;i++)
                    {
                        // Validate every player before changing any native configuration.
                        _=Controllers.TranslateN64Sdl(selected[i],settings.Controller);
                        settings.Controller.SdlDevices[i]=selected[i];
                    }
                }
                ConfigureN64(root,settings.Controller,display,scale);
                var size=outputSize??(640*scale,480*scale);
                if(size.Item1<1 || size.Item2<1 || size.Item1>16384 || size.Item2>16384)
                    throw new UserError("display-size","The selected screen size is unsupported.");
                args.AddRange(["--configdir",root,"--datadir",Path.GetDirectoryName(backend.Executable)!,"--plugindir",Path.GetDirectoryName(backend.Executable)!,"--noosd",display.Fullscreen?"--fullscreen":"--windowed","--resolution",$"{size.Item1}x{size.Item2}",Path.GetFullPath(game.Path)]);
                break;
            case ConsoleKind.GameCube:
                ConfigureDolphin(root,settings.Controller,display,scale);
                args.AddRange(["-b","-u",root,"-e",Path.GetFullPath(game.Path)]);
                break;
            case ConsoleKind.PS2:
                if(new[]{"portable.ini","portable.txt"}.Any(name=>File.Exists(Path.Combine(Path.GetDirectoryName(backend.Executable)!,name))))throw new UserError("pcsx2-portable","This PCSX2 copy forces portable mode, which overrides save isolation. Select a standard extracted PCSX2 engine without portable.ini or portable.txt.");
                ConfigurePcsx(root,settings,display,scale,pcsxTuning??new());
                args.AddRange(["-nogui","-batch","-datapath",Path.GetDirectoryName(root)!,display.Fullscreen?"-fullscreen":"-nofullscreen","--",Path.GetFullPath(game.Path)]);
                break;
        }
        var plan=new LaunchPlan(game.Platform,backend.Executable,Path.GetDirectoryName(backend.Executable)!,args,root);
        return startupState.HasValue ? new StateSlotService(paths, new EventLog(paths)).LoadOnStart(plan,startupState.Value) : plan;
    }
    static void ConfigureDolphin(string root, ControllerProfile controllers, DisplaySettings display,int scale)
    {
        var config=Path.Combine(root,"Config"); Directory.CreateDirectory(config);
        var dolphin=IniFile.Load(Path.Combine(config,"Dolphin.ini"));
        dolphin.Set("Interface","ConfirmStop",false); dolphin.Set("Interface","UsePanicHandlers",true);
        dolphin.Set("Display","Fullscreen",display.Fullscreen); dolphin.Set("Display","RenderToMain",false);
        dolphin.Set("Core","EnableCheats",false); dolphin.Set("Core","EmulationSpeed",1.0);
        for(int i=0;i<4;i++) dolphin.Set("Core",$"SIDevice{i}",i<controllers.Players?6:0);
        dolphin.Save(Path.Combine(config,"Dolphin.ini"));
        var gfx=IniFile.Load(Path.Combine(config,"GFX.ini"));
        gfx.Set("Settings","InternalResolution",scale); gfx.Set("Settings","AspectRatio",display.Stretch?3:0);
        gfx.Set("Hardware","VSync",display.VSync); gfx.Set("Settings","MSAA",1); gfx.Set("Settings","SSAA",false);
        gfx.Set("Settings","HiresTextures",false); gfx.Set("Enhancements","ForceTextureFiltering",0);
        gfx.Set("Enhancements","MaxAnisotropy",0);gfx.Set("Enhancements","DisableCopyFilter",false);gfx.Set("Enhancements","HDROutput",false);
        gfx.Set("Enhancements","PostProcessingShader",""); gfx.Set("Enhancements","ForceTrueColor",false);
        gfx.Set("Settings","wideScreenHack",false); gfx.Save(Path.Combine(config,"GFX.ini"));
        var pads=IniFile.Load(Path.Combine(config,"GCPadNew.ini"));
            var map=Controllers.Translate(ConsoleKind.GameCube,controllers);
        for(int player=0;player<4;player++)
        {
            var section=$"GCPad{player+1}";
            pads.Set(section,"Device",$"XInput/{player}/Gamepad");
            if(controllers.XboxActionLayout)
            {
                pads.Set(section,"Buttons/A","`Button A`"); pads.Set(section,"Buttons/B","`Trigger R`");
                pads.Set(section,"Buttons/X","`Button X`"); pads.Set(section,"Buttons/Y","`Button Y`");
                pads.Set(section,"Buttons/Start","`Button Start`"); pads.Set(section,"Buttons/Z","`Trigger L`");
                pads.Set(section,"Triggers/L","`Shoulder L`"); pads.Set(section,"Triggers/R","`Shoulder R`");
                pads.Set(section,"Triggers/L-Analog","`Shoulder L`"); pads.Set(section,"Triggers/R-Analog","`Shoulder R`");
            }
            else foreach(var pair in map) pads.Set(section,(pair.Key is "L" or "R"?"Triggers/":"Buttons/")+pair.Key,"`"+DolphinButton(pair.Value)+"`");
            pads.Set(section,"Main Stick/Dead Zone","20.00"); pads.Set(section,"C-Stick/Dead Zone","20.00");
            pads.Set(section,"Main Stick/Up","`Left Y+`"); pads.Set(section,"Main Stick/Down","`Left Y-`");
            pads.Set(section,"Main Stick/Left","`Left X-`"); pads.Set(section,"Main Stick/Right","`Left X+`");
            pads.Set(section,"C-Stick/Up","`Right Y+`"); pads.Set(section,"C-Stick/Down","`Right Y-`");
            pads.Set(section,"C-Stick/Left","`Right X-`"); pads.Set(section,"C-Stick/Right","`Right X+`");
            // Action mode already owns analog shoulders. Reapplying physical
            // triggers here would make LT/RT send two different native inputs.
            if(!controllers.XboxActionLayout)
            {
                pads.Set(section,"Triggers/L-Analog","`Trigger L`"); pads.Set(section,"Triggers/R-Analog","`Trigger R`");
            }
            foreach(var dir in new[]{"Up","Down","Left","Right"}) pads.Set(section,"D-Pad/"+dir,"`Pad "+(dir=="Up"?"N":dir=="Down"?"S":dir=="Left"?"W":"E")+"`");
        }
        pads.Save(Path.Combine(config,"GCPadNew.ini"));
    }
    static string DolphinButton(string b)=>b switch {"LeftShoulder"=>"Shoulder L","RightShoulder"=>"Shoulder R",_=>"Button "+b};
    static void ConfigurePcsx(string root, Settings settings, DisplaySettings display,int scale,PcsxTuning tuning)
    {
        tuning.Validate();
        var path=Path.Combine(root,"inis","PCSX2.ini"); var ini=IniFile.Load(path);
        ini.Set("UI","SettingsVersion",1); ini.Set("UI","SetupWizardIncomplete",false);
        var managedBios=Path.Combine(root,"bios");Directory.CreateDirectory(managedBios);
        var biosName=HashExecutable(settings.BiosPath)+".bin";var biosCopy=Path.Combine(managedBios,biosName);
        if(!File.Exists(biosCopy))File.Copy(settings.BiosPath,biosCopy);
        ini.Set("Folders","Bios",managedBios); ini.Set("Filenames","BIOS",biosName);
        ini.Set("EmuCore","EnableCheats",false); ini.Set("EmuCore","EnableWideScreenPatches",false); ini.Set("EmuCore","EnableNoInterlacingPatches",false);
        ini.Set("EmuCore/GS","upscale_multiplier",scale); ini.Set("EmuCore/GS","AspectRatio",display.Stretch?"Stretch":"Auto 4:3/3:2");
        ini.Set("EmuCore/GS","VsyncEnable",display.VSync); ini.Set("EmuCore/GS","fxaa",false);
        ini.Set("EmuCore/GS","LoadTextureReplacements",false); ini.Set("EmuCore/GS","ShadeBoost",false);
        ini.Set("EmuCore/GS","FrameLimitEnable",true); ini.Set("EmuCore/Framerate","NominalScalar",1.0);
        // PCSX2 2.8.2 native settings. Reset on each launch to prevent a game's
        // diagnostic/audio choice leaking into another game's session.
        ini.Set("EmuCore/GS","OsdShowSpeed",tuning.ShowPerformance);
        ini.Set("EmuCore/GS","OsdShowFPS",tuning.ShowPerformance);
        ini.Set("SPU2/Output","BufferMS",tuning.AudioBufferMs);
        ini.Set("SPU2/Output","SyncMode","TimeStretch");
        ini.Set("InputSources","SDL",true); ini.Set("InputSources","XInput",false);
        var map=Controllers.Translate(ConsoleKind.PS2,settings.Controller);
        // PS2 multitap is deliberately not enabled implicitly; two native ports are supported.
        for(int player=0;player<2;player++)
        {
            var section=$"Pad{player+1}"; var device=$"SDL-{player}/";
            ini.Set(section,"Type",player<settings.Controller.Players?"DualShock2":"None");
            foreach(var pair in map) ini.Set(section,pair.Key,device+Controllers.PcsxButton(pair.Value));
            foreach(var dir in new[]{"Up","Down","Left","Right"}) ini.Set(section,dir,device+"DPad"+dir);
            ini.Set(section,"LUp",device+"-LeftY"); ini.Set(section,"LDown",device+"+LeftY"); ini.Set(section,"LLeft",device+"-LeftX"); ini.Set(section,"LRight",device+"+LeftX");
            ini.Set(section,"RUp",device+"-RightY"); ini.Set(section,"RDown",device+"+RightY"); ini.Set(section,"RLeft",device+"-RightX"); ini.Set(section,"RRight",device+"+RightX");
            ini.Set(section,"L2",device+"+LeftTrigger"); ini.Set(section,"R2",device+"+RightTrigger");
            ini.Set(section,"L3",device+"LeftStick"); ini.Set(section,"R3",device+"RightStick");
        }
        ini.Save(path);
    }
    static void ConfigureN64(string root,ControllerProfile controllers,DisplaySettings display,int scale)
    {
        var path=Path.Combine(root,"mupen64plus.cfg"); var ini=IniFile.Load(path);
        foreach(var folder in new[]{"save","states","screenshots"}) Directory.CreateDirectory(Path.Combine(root,folder));
        ini.Set("Core","Version","1.01"); ini.Set("Core","SaveSRAMPath",Quote(Path.Combine(root,"save")));
        ini.Set("Core","SaveStatePath",Quote(Path.Combine(root,"states"))); ini.Set("Core","ScreenshotPath",Quote(Path.Combine(root,"screenshots")));
        ini.Set("Video-General","VerticalSync",display.VSync); ini.Set("Video-General","Fullscreen",display.Fullscreen);
        ini.Set("Video-Glide64mk2","aspect",display.Stretch?2:0);
        ini.Set("Video-GLideN64","UseNativeResolutionFactor",scale); ini.Set("Video-GLideN64","MultiSampling",0);
        ini.Set("Video-GLideN64","txHiresEnable",false); ini.Set("Video-GLideN64","txFilterMode",0); ini.Set("Video-GLideN64","txEnhancementMode",0);
        ini.Set("Video-GLideN64","AspectRatio",display.Stretch?0:1);
        ini.Set("Video-Rice","Version",1);ini.Set("Video-Rice","InN64Resolution",scale==1);
        ini.Set("Video-Rice","LoadHiResTextures",false);ini.Set("Video-Rice","TextureEnhancement",0);
        ini.Set("Video-Rice","TextureEnhancementControl",0);ini.Set("Video-Rice","ForceTextureFilter",0);
        ini.Set("Video-Rice","SkipFrame",false);ini.Set("Video-Rice","MultiSampling",0);
        var map=Controllers.Translate(ConsoleKind.N64,controllers);
        // Device-aware SDL mappings are written when a GUID profile is known; generic defaults remain safe otherwise.
        for(int player=0;player<4;player++)
        {
            var section=$"Input-SDL-Control{player+1}";
            var plugged=player<controllers.Players;
            // Mupen mode 2 is fully automatic and overwrites every hand-authored
            // binding at startup. UMBRA owns these mappings, so use mode 0.
            ini.Set(section,"version",2); ini.Set(section,"mode",0); ini.Set(section,"plugged",plugged);
            ini.Set(section,"plugin",2); ini.Set(section,"mouse",false);
            var device=controllers.SdlDevices.TryGetValue(player,out var selected)?selected:Controllers.DefaultSdlDevice(player);
            ini.Set(section,"device",plugged?(controllers.GoldenEyeSolitaire?device.DeviceIndex??player:player):-1);
            var deadzone=Math.Clamp(controllers.N64AnalogDeadzone,0,32768);
            if(controllers.GoldenEyeSolitaire)
            {
                controllers.ModernTuning.Validate();
                deadzone=controllers.ModernTuning.CameraDeadzone??deadzone;
            }
            ini.Set(section,"AnalogDeadzone",$"{deadzone},{deadzone}");
            ini.Set(section,"AnalogPeak",controllers.GoldenEyeSolitaire?$"{controllers.ModernTuning.Peak(deadzone,false)},{controllers.ModernTuning.Peak(deadzone,true)}":"32768,32768");
            var bindings=controllers.GoldenEyeSolitaire && !plugged
                ? GoldenEyeControls.EmptyBindings() : Controllers.TranslateN64Sdl(device,controllers);
            foreach(var pair in bindings) ini.Set(section,pair.Key,pair.Value);
            // Remove legacy names from older UMBRA builds so Mupen cannot keep
            // stale Start/C-axis actions after a profile change.
            ini.Set(section,"Start Button",""); ini.Set(section,"C Button X Axis",""); ini.Set(section,"C Button Y Axis","");
        }
        ini.Save(path);
        JsonStore.Write(Path.Combine(root,"umbra-controller-intent.json"),new { Logical=map, Devices=controllers.SdlDevices });
    }
    static string Quote(string value)=>"\""+value.Replace('\\','/')+"\"";
}
