using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Umbra.Core;
using Umbra.Core.Emulation;
namespace Umbra.Desktop;
public sealed partial class MainWindow
{
    void ShowGame(Game game)
    {
        var panel=UI.Column(UI.Button("←  Back to collection",()=>Navigate(page)),UI.Text(game.PlatformLabel,11,"Accent"),UI.Heading(game.Title,38),UI.Text($"{game.Playtime} played   ·   Last played: {(game.LastPlayed?.ToLocalTime().ToString("g")??"Never")}",13,"Muted"));
        panel.Children.Add(UI.Row(UI.Button("▶  Play",()=>Play(game),true),UI.Button(game.Favorite?"★  Favorited":"☆  Favorite",()=>Safe(()=>{game.Favorite=!game.Favorite;library.Save(games);ShowGame(game);})),UI.Button("Show file location",()=>OpenFolder(Path.GetDirectoryName(game.Path)!))));
        if(session.Active) panel.Children.Add(UI.Row(UI.Button("Ask game to quit",()=> {session.RequestStop();Notice("A quit request was sent. Return to the game to finish saving and confirm if requested.");}),UI.Button("Force stop…",()=> {if(MessageBox.Show(this,"Force stop can lose unsaved progress. Your pre-launch backup is retained. Stop the engine now?","Force stop",MessageBoxButton.YesNo)==MessageBoxResult.Yes)session.ForceStop();})));
        if(game.Platform==ConsoleKind.PS2)panel.Children.Add(UI.Panel(UI.Column(UI.Heading("PlayStation 2 engine",21),UI.Text(settings.UseBiosFreePs2?"Play! BIOS-free is selected. Compatibility varies by game.":"PCSX2 is selected. It requires a BIOS imported from your own console.",13,"Muted"),UI.Row(UI.Button("Play with Play! — no BIOS",()=>SafeAsync(()=>RunGame(game,true))),UI.Button("Play with PCSX2 — own BIOS",()=>SafeAsync(()=>RunGame(game,false))),UI.Button("Import PS2 BIOS",SelectBios)))));
        var title=new TextBox{Text=game.Title};
        var region=new TextBox{Text=game.Metadata.Region};var genre=new TextBox{Text=game.Metadata.Genre};
        panel.Children.Add(UI.Panel(UI.Column(UI.Heading("Game information",21),UI.Text("Title",12,"Muted"),title,UI.Text("Genre",12,"Muted"),genre,UI.Text("Region",12,"Muted"),region,UI.Row(UI.Button("Save details",()=>Safe(()=>{if(string.IsNullOrWhiteSpace(title.Text))throw new UserError("title","Enter a game title.");game.Title=title.Text.Trim()[..Math.Min(title.Text.Trim().Length,256)];game.Metadata.Genre=genre.Text;game.Metadata.Region=region.Text;library.Save(games);ShowGame(game);})),UI.Button("Import metadata JSON",()=>ImportMetadata(game)),UI.Button("Look up Wikidata",()=>LookupMetadata(game)),UI.Button("Choose cover image",()=>ImportCover(game))))));
        var over=JsonStore.Read(paths.Override(game.Id),()=>new GameOverride());
        if(game.Platform==ConsoleKind.PS2){
            var audioBuffers=new[]{50,75,100,150};
            var audio=UI.Choice(["50 ms — standard","75 ms — extra buffer","100 ms — crackle test","150 ms — larger buffer / more delay"],Math.Max(0,Array.IndexOf(audioBuffers,over.PcsxTuning?.AudioBufferMs??50)));
            var performance=new CheckBox{Content="Show game speed and FPS while playing",IsChecked=over.PcsxTuning?.ShowPerformance??false};
            panel.Children.Add(UI.Panel(UI.Column(UI.Heading("PCSX2 sound and performance",21),UI.Text("A larger audio buffer can absorb short interruptions, but adds sound delay and cannot fix sustained slowdown. Time stretching stays enabled. If speed falls below 100%, try 1× resolution below before raising image quality. These settings affect PCSX2 only.",13,"Muted"),audio,performance,UI.Button("Save PS2 sound settings",()=>Safe(()=>{if(session.Active)throw new UserError("game-busy","Quit the game first.");over.PcsxTuning=new(){AudioBufferMs=audioBuffers[audio.SelectedIndex],ShowPerformance=performance.IsChecked==true};JsonStore.Write(paths.Override(game.Id),over);Notice("PS2 sound and performance settings saved for the next launch.");})))));
            var playGraphics=UI.Choice(["Play! OpenGL","Play! Vulkan (requires compatible GPU)"],over.PlayRenderer=="vulkan"?1:0);
            panel.Children.Add(UI.Panel(UI.Column(UI.Heading("Play! graphics and input",21),UI.Text("Play! receives a managed Xbox/XInput profile at launch when a controller is connected. Other controllers still need Play!'s own configuration. This graphics setting affects Play! only. Changing renderer is a compatibility test, not a guaranteed fix.",13,"Muted"),playGraphics,UI.Button("Save Play! graphics",()=>Safe(()=>{over.PlayRenderer=playGraphics.SelectedIndex==1?"vulkan":"opengl";JsonStore.Write(paths.Override(game.Id),over);Notice("Play! renderer saved for the next launch.");})))));
        }
        var workingProfiles=new WorkingProfiles(paths);
        var restoreProfile=UI.Button("Restore working profile",()=>Safe(()=>{
            if(session.Active || busy)throw new UserError("profile-busy","Quit the game and wait for the current operation first.");
            workingProfiles.Restore(game,JsonStore.Read(paths.Override(game.Id),()=>new GameOverride()));ShowGame(game);Notice("Working controls and display restored for this game. You can undo this restore.");
        }));restoreProfile.IsEnabled=workingProfiles.Exists(game);
        var undoProfile=UI.Button("Undo profile restore",()=>Safe(()=>{
            if(session.Active || busy)throw new UserError("profile-busy","Quit the game and wait for the current operation first.");
            workingProfiles.Undo(game,JsonStore.Read(paths.Override(game.Id),()=>new GameOverride()));ShowGame(game);Notice("The controls and display from before the restore are back.");
        }));undoProfile.IsEnabled=workingProfiles.CanUndo(game);
        panel.Children.Add(UI.Panel(UI.Column(UI.Heading("Working profile",21),UI.Text("After testing a game, save its current controls and display here. Restore affects only this game; game progress is not part of this profile. Save changes in the controls/display sections before capturing a profile.",13,"Muted"),
            UI.Row(UI.Button("Save working profile",()=>Safe(()=>{
                if(session.Active || busy)throw new UserError("profile-busy","Quit the game and wait for the current operation first.");
                var current=JsonStore.Read(paths.Override(game.Id),()=>new GameOverride());
                var catalog=JsonStore.Read(Path.Combine(AppContext.BaseDirectory,"config","compatibility.json"),()=>new CompatibilityDatabase());
                workingProfiles.Save(game,settings,current,current.Display??(game.Platform==ConsoleKind.Xbox360?new DisplaySettings{Fullscreen=settings.Display.Fullscreen}:catalog.Resolve(game,settings.Backends[game.Platform].Sha256)??settings.Display));
                ShowGame(game);Notice("Working profile saved. Its controller and display settings can now be restored independently of global settings.");
            })),restoreProfile,undoProfile),
            UI.Text(over.SavedController is null?"Controls currently use your global profile plus this game's preset.":"This game uses restored controller settings. Global controller edits will not change this saved copy.",12,"Muted"),
            UI.Button("Use global controller settings again",()=>Safe(()=>{if(session.Active || busy)throw new UserError("profile-busy","Quit the game first.");var current=JsonStore.Read(paths.Override(game.Id),()=>new GameOverride());current.SavedController=null;JsonStore.Write(paths.Override(game.Id),current);ShowGame(game);Notice("Global controller settings reconnected; the game-specific preset remains selected.");})))));
        if(game.Platform==ConsoleKind.N64)
        {
            panel.Children.Add(UI.Button("Camera sensitivity & drift check",()=>ShowStickTuning(game)));
            var controlPreset=UI.Choice(["Use global controller profile","Classic N64 controls","Modern shooter (right stick = C/look)","Disable right-stick C actions","GoldenEye 007 — modern (requires 1.2 Solitaire)","Modern 1.2 — GoldenEye / Perfect Dark","Automatic modern — supported games"],over.ControllerPreset switch {"classic"=>1,"modern-shooter"=>2,"no-c"=>3,"goldeneye-solitaire"=>4,"modern-12"=>5,"auto-modern"=>6,_=>0});
            var renderer=UI.Choice(["Engine default graphics","Rice","Glide64mk2 — Perfect Dark candidate"],over.N64VideoPlugin switch {"rice"=>1,"glide64mk2"=>2,_=>0});
            panel.Children.Add(UI.Panel(UI.Column(UI.Heading("N64 graphics renderer",21),UI.Text("Glide64mk2 includes Perfect Dark framebuffer handling. Select it to test repeated menu imagery. This changes only this game's launch; graphics verification is still required.",13,"Muted"),renderer,UI.Button("Save game renderer",()=>Safe(()=>{over.N64VideoPlugin=renderer.SelectedIndex switch {1=>"rice",2=>"glide64mk2",_=>""};JsonStore.Write(paths.Override(game.Id),over);Notice("Renderer saved for the next launch.");})))));
            var invertLook=new CheckBox{Content="Modern 1.2: invert vertical look",IsChecked=over.InvertLook};
            var reloadButton=UI.Choice(["Interact/reload: A / Cross","Interact/reload: X / Square"],over.ReloadOnWest?1:0);
            var weaponButton=UI.Choice(["Weapon/menu confirm: RB / R1","Weapon/menu confirm: LB / L1"],over.WeaponOnLeft?1:0);
            panel.Children.Add(UI.Panel(UI.Column(UI.Heading("Modern shooter setup",21),
                UI.Text("Automatic modern recognizes GoldenEye and Perfect Dark from the ROM header. Set Control Style 1.2 in the game (Solitaire in GoldenEye), look to Upright and Aim Control to Hold. Other games need their own profile; UMBRA does not change saved game options.",13,"Muted"),
                UI.Text("Left stick: move/strafe • Right stick: look • LT/L2: aim • RT/R2: fire • Start/Options: pause/watch. The chosen action button reloads or interacts with nearby objects. The chosen bumper changes weapons and confirms menus; hold it and tap RT/R2 to cycle backwards.",13,"Muted"),
                UI.Text("The current input plugin accepts one face button per native action: choose A/Cross or X/Square below. Movement is eight-way because 1.2 uses digital C-buttons. No jump exists. Aiming retains the game's leaning/crouching behavior. SDL-mapped dual-stick pads are detected at launch; unsupported layouts are blocked. RUNTIME VERIFICATION REQUIRED.",13,"Muted"),reloadButton,weaponButton,invertLook)));
            panel.Children.Add(UI.Panel(UI.Column(UI.Heading("Controls for this game",21),UI.Text("GoldenEye and Perfect Dark share the modern 1.2 preset. Other presets retain their previous native mappings; camera and action meanings depend on each game.",13,"Muted"),controlPreset,UI.Button("Save game controls",()=>Safe(()=>{over.ControllerPreset=controlPreset.SelectedIndex switch {1=>"classic",2=>"modern-shooter",3=>"no-c",4=>"goldeneye-solitaire",5=>"modern-12",6=>"auto-modern",_=>""};over.InvertLook=invertLook.IsChecked==true;over.ReloadOnWest=reloadButton.SelectedIndex==1;over.WeaponOnLeft=weaponButton.SelectedIndex==1;JsonStore.Write(paths.Override(game.Id),over);Notice("Game control preset saved for the next launch. Modern profiles require 1.2 in the game's options.");})) )));
        }
        var consoleChoice=UI.Choice(["PlayStation 2","GameCube","Nintendo 64","Xbox 360"],(int)game.Platform);
        panel.Children.Add(UI.Panel(UI.Column(UI.Heading("Console assignment",21),UI.Text("Choose the console this game was made for. Changing this does not convert the file. Existing saves are retained in their original console folder.",13,"Muted"),consoleChoice,UI.Button("Correct console",()=>Safe(()=>
        {
            if(session.Active)throw new UserError("game-busy","Quit the game before changing its console.");
            var selected=(ConsoleKind)consoleChoice.SelectedIndex;
            var checkedGame=library.Import(game.Path,selected);
            if(game.Platform!=selected){over.StartupStateSlot=null;JsonStore.Write(paths.Override(game.Id),over);game.Platform=checkedGame.Platform;library.Save(games);}
            ShowGame(game);Notice("Console assignment saved. The original game file and existing saves were retained.");
        })))));
        if(game.Platform==ConsoleKind.Xbox360)
        {
            panel.Children.Add(UI.Panel(UI.Column(UI.Heading("Music compatibility test",21),UI.Text(over.XeniaAudioBackend is null?"Default Xenia engine. Canary can be tested per game for missing music.":"Canary audio test selected: new decoder, synchronous decoding. This is experimental and may affect performance. It uses separate saves; switching back restores access to the original saves.",13,"Muted"),UI.Row(UI.Button("Select Canary audio test engine…",()=>Safe(()=>{if(session.Active)throw new UserError("game-busy","Quit the game first.");var picker=new OpenFileDialog{Title="Select official Xenia Canary executable",Filter="Xenia Canary|xenia_canary.exe"};if(picker.ShowDialog(this)!=true)return;over.XeniaAudioBackend=new(){Executable=picker.FileName,Sha256=Adapters.HashExecutable(picker.FileName),Version="Canary audio test (user selected)"};JsonStore.Write(paths.Override(game.Id),over);ShowGame(game);})),UI.Button("Use default Xenia again",()=>Safe(()=>{if(session.Active)throw new UserError("game-busy","Quit the game first.");over.XeniaAudioBackend=null;JsonStore.Write(paths.Override(game.Id),over);ShowGame(game);Notice("Default Xenia selected; both sets of saves retained.");}))))));
            var xboxFullscreen=new CheckBox{Content="Launch fullscreen",IsChecked=over.Display?.Fullscreen??settings.Display.Fullscreen};
            panel.Children.Add(UI.Panel(UI.Column(UI.Heading("Xbox 360 — experimental",21),UI.Text("Xenia launches default.xex or Xbox 360 ISO files directly. Keep extracted game files together. No BIOS needed. Native Xbox/XInput controls and native presentation are used. Stretch, enhanced profiles, UMBRA remapping and save states are not integrated. Gameplay, audio and saves require runtime verification.",13,"Muted"),xboxFullscreen,UI.Button("Save Xbox 360 presentation",()=>Safe(()=>{if(session.Active)throw new UserError("game-busy","Quit the game first.");over.Display=new(){Fullscreen=xboxFullscreen.IsChecked==true};JsonStore.Write(paths.Override(game.Id),over);Notice("Xbox 360 fullscreen preference saved.");})))));
        }
        else
        {
        var choices=UI.Choice(["Use global profile","Original Hardware","Clean Original","Enhanced","Custom"],over.Display is null?0:(int)over.Display.Profile+1);
        var stretchToFill=new CheckBox { Content="Stretch to fill the screen (Custom only)", IsChecked=over.Display?.Stretch??settings.Display.Stretch, Margin=new Thickness(0,8,0,8) };
        var resolution=UI.Choice(["1× — native / lowest load","2× — sharper / moderate load","3× — higher load","4× — high load","5× — very high load","6× — highest load"],Math.Clamp(over.Display?.Scale??settings.Display.Scale,1,6)-1);
        var fullScreen=new CheckBox{Content="Fullscreen",IsChecked=over.Display?.Fullscreen??settings.Display.Fullscreen};
        var vsync=new CheckBox{Content="VSync — reduce screen tearing",IsChecked=over.Display?.VSync??settings.Display.VSync};
        void RefreshCustom(){resolution.IsEnabled=stretchToFill.IsEnabled=choices.SelectedIndex==4;fullScreen.IsEnabled=vsync.IsEnabled=choices.SelectedIndex!=0;}
        choices.SelectionChanged+=(_,_)=>RefreshCustom();RefreshCustom();
        panel.Children.Add(UI.Panel(UI.Column(UI.Heading("Presentation for this game",21),UI.Text("Original/Clean Original use native resolution and authentic geometry. Enhanced uses 3× resolution. Custom enables stretch and the resolution choice below. Stretch fills the screen by widening the picture; it is not a widescreen patch. Higher resolution sharpens edges but does not raise the game's frame rate. Play! does not support Custom in UMBRA.",13,"Muted"),choices,stretchToFill,UI.Text("Custom internal resolution",12,"Muted"),resolution,fullScreen,vsync,UI.Button("Save game profile",()=>Safe(()=> {if(session.Active)throw new UserError("game-busy","Quit the game first.");over.Display=choices.SelectedIndex==0?null:new DisplaySettings{Profile=(GraphicsProfile)(choices.SelectedIndex-1),Fullscreen=fullScreen.IsChecked==true,VSync=vsync.IsChecked==true,Scale=resolution.SelectedIndex+1,Stretch=choices.SelectedIndex==4 && stretchToFill.IsChecked==true};JsonStore.Write(paths.Override(game.Id),over);Notice("Game profile saved for the next launch.");})))));
        }
        panel.Children.Add(UI.Panel(UI.Column(UI.Heading("Native saves & recovery",21),UI.Text("Save inside the game for permanent progress. Emulator save states are separate, engine-version-specific snapshots. This build does not automate save-state hotkeys or autosave states.",14,"Muted"),UI.Row(UI.Button("Back up saves now",()=>Backup(game.Platform)),UI.Button("Open save data",()=>OpenFolder(paths.Engine(game.Platform))),UI.Button("Restore backup…",()=>Restore(game.Platform))))));
        var caps=CapabilityCatalog.For(game.Platform); var stateService=new StateSlotService(paths,log);
        var statePanel=UI.Column(UI.Heading("Engine capabilities",21),UI.Text(caps.Notes+" UMBRA's automatic pre-launch/post-exit backup is the supported autosave protection in this preview; it is distinct from emulator save states.",13,"Muted"));
        if(caps.SaveStates){
        var slots=stateService.Existing(game.Platform); statePanel.Children.Add(UI.Text($"Save-state slots present: {slots.Count}  ·  Screenshots: {Directory.EnumerateFiles(stateService.ScreenshotFolder(game.Platform),"*.png").Count()}",12,"Accent"));
        var slotChoice=UI.Choice(Enumerable.Range(0,10).Select(x=>$"Slot {x}"));
        if(caps.SaveStates)statePanel.Children.Add(UI.Row(UI.Button("Load state on next launch",()=>Safe(()=>{var over=JsonStore.Read(paths.Override(game.Id),()=>new GameOverride());_ = Adapters.Prepare(paths,settings,game,over.Display??null,slotChoice.SelectedIndex);over.StartupStateSlot=slotChoice.SelectedIndex;JsonStore.Write(paths.Override(game.Id),over);Notice("State slot validated and saved for the next launch.");})),UI.Button("Register existing state…",()=>Safe(()=>{if(session.Active)throw new UserError("state-busy","Quit the engine before registering a state file.");var picker=new OpenFileDialog{Title="Register engine state in Slot "+slotChoice.SelectedIndex,Filter="State files|*.*"};if(picker.ShowDialog(this)!=true)return;stateService.RegisterStateFile(game.Platform,slotChoice.SelectedIndex,picker.FileName);ShowGame(game);Notice("State file registered. Load that slot on the next launch to test restore.");})),UI.Button("Clear startup state",()=>Safe(()=>{var over=JsonStore.Read(paths.Override(game.Id),()=>new GameOverride());over.StartupStateSlot=null;JsonStore.Write(paths.Override(game.Id),over);Notice("Startup state cleared.");})),UI.Button("Open state files",()=>OpenFolder(stateService.Folder(game.Platform))),UI.Button("Open screenshots",()=>OpenFolder(stateService.ScreenshotFolder(game.Platform)))));
        statePanel.Children.Add(UI.Text("State capture hotkeys remain controlled by each engine and are not remapped by UMBRA. No claim of state round-trip compatibility is made until a real game test passes.",12,"Muted")); } panel.Children.Add(UI.Panel(statePanel));
        panel.Children.Add(UI.Panel(UI.Column(UI.Heading("File",21),UI.Text(game.Path,12,"Muted"),UI.Row(UI.Button("Locate moved file",()=>Relink(game)),UI.Button("Remove from library",()=>Safe(()=> {games.Remove(game);library.Save(games);Navigate(page);Notice("Removed from the library. Game files, saves and backups were retained.");}))))));content.Content=panel;
    }
    void Play(Game game)=>SafeAsync(()=>RunGame(game));
    async Task RunGame(Game game,bool? biosFreeOverride=null)
    {
        if(session.Active)throw new UserError("already-playing","A game is already running. Quit it before starting another.");
        Notice("Checking engine and protecting saves…");
        var over=JsonStore.Read(paths.Override(game.Id),()=>new GameOverride());
        var compatibility=JsonStore.Read(Path.Combine(AppContext.BaseDirectory,"config","compatibility.json"),()=>new CompatibilityDatabase());
        var auto=compatibility.Resolve(game,settings.Backends[game.Platform].Sha256);
        var selectedDisplay=over.Display??(game.Platform==ConsoleKind.Xbox360?new DisplaySettings{Fullscreen=settings.Display.Fullscreen}:auto??settings.Display);
        var managedFullscreen=DisplayManager.UsesManagedFullscreen(game.Platform,selectedDisplay.Fullscreen,over.N64VideoPlugin);
        (int Width,int Height)? outputSize=game.Platform==ConsoleKind.N64 && selectedDisplay.Fullscreen && !managedFullscreen
            ? Umbra.Desktop.Platform.AspectFullscreen.MonitorSize(this) : null;
        var launchDisplay=new DisplaySettings{Profile=selectedDisplay.Profile,Fullscreen=managedFullscreen?false:selectedDisplay.Fullscreen,Stretch=selectedDisplay.Stretch,VSync=selectedDisplay.VSync,Scale=selectedDisplay.Scale};
        var controllerSettings=over.SavedController??settings.Controller;
        if(game.Platform==ConsoleKind.N64 && !string.IsNullOrWhiteSpace(over.ControllerPreset))
        {
            controllerSettings=Controllers.ForN64Preset(controllerSettings,N64GameProfiles.Resolve(over.ControllerPreset,game.Path));
            controllerSettings.GoldenEyeInvertLook=over.InvertLook;
            controllerSettings.GoldenEyeReloadOnWest=over.ReloadOnWest;
            controllerSettings.GoldenEyeWeaponOnLeft=over.WeaponOnLeft;
            if(over.StickTuning is not null)controllerSettings.ModernTuning=over.StickTuning;
        }
        var launchSettings=new Settings { Controller=controllerSettings, Display=settings.Display, Backends=settings.Backends, BiosPath=settings.BiosPath, UseBiosFreePs2=settings.UseBiosFreePs2, BiosFreePs2=settings.BiosFreePs2, LibraryFolders=settings.LibraryFolders, SchemaVersion=settings.SchemaVersion, SetupComplete=settings.SetupComplete };
        var playDevices=Umbra.Desktop.Platform.ControllerMonitor.Connected().Select(p=>p.Index).Take(Math.Min(2,controllerSettings.Players)).ToArray();
        var plan=await Task.Run(()=>game.Platform==ConsoleKind.PS2 && (biosFreeOverride??settings.UseBiosFreePs2)
            ? BiosFreeAdapter.Prepare(paths,settings.BiosFreePs2,game,launchDisplay,over.StartupStateSlot,controllerSettings,playDevices,over.PlayRenderer)
            : Adapters.Prepare(paths,launchSettings,game,launchDisplay,over.StartupStateSlot,over.N64VideoPlugin,outputSize,over.PcsxTuning,over.XeniaAudioBackend));
        if(game.Platform==ConsoleKind.N64 && Environment.GetCommandLineArgs().Contains("--runtime-probe"))plan.Arguments.InsertRange(plan.Arguments.Count-1,["--testshots","300,600"]);
        await Task.Run(()=>saves.Backup(game.Platform));
        var started=DateTimeOffset.UtcNow;
        SessionResult result;
        Umbra.Desktop.Platform.AspectFullscreen? presentation=null;
        void OnStarted(){if(managedFullscreen)presentation=new(session,detail=>log.Write("display.viewport",detail),selectedDisplay.Stretch);}
        session.Started+=OnStarted;
        try{result=await session.Run(plan,game);}
        finally{session.Started-=OnStarted;presentation?.Dispose();WindowState=WindowState.Normal;Activate();sessionStatus.Text="";sessionControls.Visibility=Visibility.Collapsed;}
        game.LastPlayed=started;game.PlaySeconds+=result.Seconds;library.Save(games);
        WindowState=WindowState.Normal;Activate();sessionStatus.Text="";
        if(!result.Forced && result.ExitCode==0)await Task.Run(()=>saves.Backup(game.Platform));
        ShowGame(game);
        Notice(result.Forced?"Game stopped. Unsaved progress may be lost; the pre-launch backup was retained.":result.ExitCode==0?"Session finished. Native saves were backed up.":"The game engine stopped unexpectedly. Saves and the pre-launch backup were retained. Check System Status and try again.");
    }
    void Backup(ConsoleKind platform)=>SafeAsync(async()=> {if(session.Active)throw new UserError("save-busy","Quit the game before backing up saves.");Notice("Backing up native saves and states…");await Task.Run(()=>saves.Backup(platform));Notice("Backup complete. Open the backup folder in Settings to keep an additional copy elsewhere.");});
    void Restore(ConsoleKind platform)=>Safe(()=>
    {
        if(session.Active || busy)throw new UserError("save-busy","Wait for the current operation and quit the game before restoring saves.");
        var picker=new OpenFileDialog{Title="Restore "+platform+" save backup",Filter="UMBRA backups|*.zip",InitialDirectory=paths.Backups};if(picker.ShowDialog(this)!=true)return;
        if(MessageBox.Show(this,"Restore this backup over matching save files? A new backup of your current saves will be made first.","Restore saves",MessageBoxButton.YesNo)!=MessageBoxResult.Yes)return;
        SafeAsync(async()=>{await Task.Run(()=>saves.Restore(picker.FileName,platform));Notice("Saves restored. The previous save files are preserved in a new backup.");});
    });
    void Relink(Game game)=>Safe(()=>
    {
        var picker=new OpenFileDialog{Title="Locate this game's new file"};if(picker.ShowDialog(this)!=true)return;
        var imported=library.Import(picker.FileName,game.Platform);
        if(imported.ContentId!=game.ContentId)throw new UserError("different-game","This file does not match the original file's size and header fingerprint. Add it as a separate game instead.");
        if(games.Any(g=>g.Id!=game.Id && g.Path.Equals(imported.Path,StringComparison.OrdinalIgnoreCase)))throw new UserError("duplicate","This file is already in the library.");
        game.Path=imported.Path;library.Save(games);ShowGame(game);
    });
    void ImportMetadata(Game game)=>Safe(()=>
    {
        var picker=new OpenFileDialog{Title="Import optional metadata",Filter="Metadata|*.json"};if(picker.ShowDialog(this)!=true)return;
        var (title,metadata)=MetadataService.Read(picker.FileName);metadata.CoverPath=game.Metadata.CoverPath;game.Metadata=metadata;if(title is not null)game.Title=title;library.Save(games);ShowGame(game);
    });
    void ImportCover(Game game)=>Safe(()=>
    {
        var picker=new OpenFileDialog{Title="Choose cover art you may use",Filter="Images|*.png;*.jpg;*.jpeg"};if(picker.ShowDialog(this)!=true)return;
        if(new FileInfo(picker.FileName).Length>10_000_000)throw new UserError("cover-size","Choose an image smaller than 10 MB.");
        var destination=Path.Combine(paths.Root,"artwork",game.Id+Path.GetExtension(picker.FileName).ToLowerInvariant());File.Copy(picker.FileName,destination,true);game.Metadata.CoverPath=destination;library.Save(games);ShowGame(game);
    });
    void LookupMetadata(Game game)=>SafeAsync(async()=>
    {
        if(MessageBox.Show(this,"This sends the game title and console name to Wikidata, which publishes CC0 data. No game files, paths or BIOS data are sent. Continue?","Optional metadata lookup",MessageBoxButton.YesNo)!=MessageBoxResult.Yes)return;
        Notice("Looking up optional metadata…"); var provider=new WikidataProvider(); var result=await provider.LookupAsync(game.Title,game.Platform);
        if(result.Title is not null) game.Title=result.Title; game.Metadata.Developer=result.Metadata.Developer; game.Metadata.Publisher=result.Metadata.Publisher; game.Metadata.Genre=result.Metadata.Genre; game.Metadata.Region=result.Metadata.Region; library.Save(games); ShowGame(game); Notice(result.Attribution+". Offline local metadata remains available.");
    });
}
