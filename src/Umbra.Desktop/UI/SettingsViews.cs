using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Umbra.Core;
using Umbra.Core.Emulation;
using Umbra.Desktop.Platform;
namespace Umbra.Desktop;
public sealed partial class MainWindow
{
    void RenderSettings()
    {
        var panel=UI.Column(UI.Text("MAKE IT YOURS",11,"Muted"),UI.Heading("Settings",34));
        var profile=UI.Choice(["Original Hardware","Clean Original","Enhanced","Custom"],(int)settings.Display.Profile);
        var fullscreen=new CheckBox{Content="Launch games in fullscreen",IsChecked=settings.Display.Fullscreen};
        var vsync=new CheckBox{Content="VSync",IsChecked=settings.Display.VSync};var stretch=new CheckBox{Content="Stretch to screen (Custom only)",IsChecked=settings.Display.Stretch};
        var scale=UI.Choice(["1× native","2×","3×","4×","5×","6×"],settings.Display.Scale-1);
        panel.Children.Add(UI.Panel(UI.Column(UI.Heading("Original by default.",25),UI.Text("Original Hardware keeps native internal resolution, original speed and aspect ratio, and disables texture replacement and enhancement patches. It approximates the original presentation; it is not a cycle-accuracy guarantee.",14,"Muted"),profile,UI.Text("Clean Original keeps the same native rendering in this build. Enhanced uses 3× rendering. Custom enables the scale below.",12,"Muted"),scale,fullscreen,vsync,stretch,UI.Button("Save presentation",()=>Safe(()=>{var value=new DisplaySettings{Profile=(GraphicsProfile)profile.SelectedIndex,Scale=scale.SelectedIndex+1,Fullscreen=fullscreen.IsChecked==true,VSync=vsync.IsChecked==true,Stretch=stretch.IsChecked==true};if(value.Stretch && value.Profile!=GraphicsProfile.Custom)throw new UserError("stretch","Choose Custom to stretch the picture.");JsonStore.ValidateDisplay(value);settings.Display=value;SaveSettings();Notice("Presentation saved. Changes apply on the next launch.");}),true),UI.Text("CRT simulation, scanlines, overscan, integer scaling, manual renderers and borderless controls are not implemented in this preview. No visual effects are silently added.",12,"Muted"))));
        var folders=UI.Column(UI.Heading("Library locations",21));
        foreach(var folder in settings.LibraryFolders.ToArray())folders.Children.Add(UI.Row(UI.Text(folder,12,"Muted"),UI.Button("Forget location",()=>Safe(()=>{settings.LibraryFolders.Remove(folder);SaveSettings();RenderSettings();Notice("Scan location removed. Existing library entries and game files are retained.");}))));
        folders.Children.Add(UI.Row(UI.Button("Add folder",AddFolder),UI.Button("Scan library",()=>SafeAsync(Rescan))));panel.Children.Add(UI.Panel(folders));
        panel.Children.Add(UI.Panel(UI.Column(UI.Heading("PlayStation 2 BIOS",21),UI.Text(string.IsNullOrEmpty(settings.BiosPath)?"Select a BIOS dump from your own console.":Path.GetFileName(settings.BiosPath),13,"Muted"),UI.Row(UI.Button("Import PS2 BIOS",SelectBios),UI.Button("Official dumping guide",()=>OpenWebsite("https://pcsx2.net/docs/setup/bios/"))))));
        var backupPanel=UI.Column(UI.Heading("Save recovery",21),UI.Text("Native saves and emulator save states remain separate within each engine's data folder. Backups are made before launch and after a normal exit. Backups are never automatically pruned in this preview.",13,"Muted"),UI.Button("Open backup folder",()=>OpenFolder(paths.Backups)));
        foreach(var platform in Enum.GetValues<ConsoleKind>())backupPanel.Children.Add(UI.Row(UI.Button("Back up "+platform,()=>Backup(platform)),UI.Button("Restore "+platform,()=>Restore(platform))));panel.Children.Add(UI.Panel(backupPanel));
        panel.Children.Add(UI.Row(UI.Button("Revisit first-run setup",ShowWizard),UI.Button("Open application data",()=>OpenFolder(paths.Root)),UI.Button("Developer details",ShowDeveloper)));content.Content=panel;
    }
    void RenderStatus()
    {
        var panel=UI.Column(UI.Text("EVERYTHING IN ITS PLACE",11,"Muted"),UI.Heading("System status",34),UI.Text("Selected engines remain “Needs attention” until real game compatibility has been verified. No ROMs or BIOS files are bundled.",13,"Muted"));
        panel.Children.Add(UI.Panel(UI.Column(UI.Heading("PS2 engine choice",21),UI.Text(settings.UseBiosFreePs2?"Play! BIOS-free (experimental) is selected.":"PCSX2 is selected; import your own console BIOS.",13,"Muted"),UI.Text("Play! uses its own high-level firmware and separate saves. Its native controls and graphics are not yet mapped by UMBRA. Enhanced/Custom and PCSX2 startup states are unavailable on this route.",13,"Muted"),UI.Row(UI.Button("Use PCSX2",()=>Safe(()=>{if(session.Active)throw new UserError("engine-busy","Quit the game first.");settings.UseBiosFreePs2=false;SaveSettings();RenderStatus();})),UI.Button("Use Play! without BIOS",()=>Safe(()=>{if(session.Active)throw new UserError("engine-busy","Quit the game first.");Adapters.ValidateBackend(settings.BiosFreePs2);settings.UseBiosFreePs2=true;SaveSettings();RenderStatus();})),UI.Button("Select Play! engine",()=>Safe(()=>{if(session.Active)throw new UserError("engine-busy","Quit the game first.");var picker=new OpenFileDialog{Title="Select official Play.exe",Filter="Play! engine|Play.exe"};if(picker.ShowDialog(this)!=true)return;settings.BiosFreePs2=new(){Executable=picker.FileName,Sha256=Adapters.HashExecutable(picker.FileName),Version="Selected; runtime verification required"};SaveSettings();RenderStatus();}))))));
        var checks=Diagnostics.Check(paths,settings,ControllerMonitor.Connected().Count);
        foreach(var platform in Enum.GetValues<ConsoleKind>())
        {
            var backend=settings.Backends[platform];
            var detected=checks.First(i=>i.Component==platform.ToString());
            panel.Children.Add(UI.Panel(UI.Column(UI.Heading(ConsoleNames.Display(platform),21),UI.Text(detected.State+" · "+detected.Detail,12,"Accent"),UI.Row(UI.Button("Select engine",()=>SelectBackend(platform)),UI.Button("Official downloads",()=>OpenWebsite(Adapters.Website(platform)))))));
        }
        panel.Children.Add(UI.Panel(UI.Column(UI.Heading("Install Nintendo 64 support",21),UI.Text("Download the verified Mupen64Plus 2.6.0 Windows bundle directly from its official GitHub release. GPL license notices are retained. No games are installed. Internet access is only needed for this download.",13,"Muted"),UI.Button("Download N64 engine",()=>SafeAsync(async()=>{if(session.Active)throw new UserError("engine-busy","Quit the game before installing an engine.");Notice("Downloading and checking the N64 engine…");settings.Backends[ConsoleKind.N64]=await EngineInstaller.InstallN64(paths);SaveSettings();RenderStatus();Notice("Nintendo 64 engine installed. Import your game and press Play.");}),true))));
        foreach(var issue in checks.Where(i=>!Enum.TryParse<ConsoleKind>(i.Component,out _)))panel.Children.Add(UI.Panel(UI.Column(UI.Text(issue.State+"   /   "+issue.Component,12,"Accent"),UI.Text(issue.Detail,13,"Muted"))));
        panel.Children.Add(UI.Row(UI.Button("Refresh",RenderStatus),UI.Button("Open logs",()=>OpenFolder(paths.Logs)),UI.Button("Copy diagnostic report",()=>Safe(()=>{Clipboard.SetText(Diagnostics.Report(paths,settings,ControllerMonitor.Connected().Count));Notice("Diagnostic report copied. It excludes game paths, titles and BIOS content.");})),UI.Button("Export diagnostic bundle",ExportDiagnostics)));content.Content=panel;
    }
    void ExportDiagnostics()=>Safe(()=>
    {
        var picker=new SaveFileDialog{Title="Export diagnostics",Filter="ZIP archive|*.zip",FileName="Umbra-diagnostics-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".zip"};if(picker.ShowDialog(this)!=true)return;
        if(File.Exists(picker.FileName))throw new UserError("export-exists","Choose a new filename for the diagnostic bundle.");
        Diagnostics.Export(picker.FileName,paths,settings,ControllerMonitor.Connected().Count);Notice("Diagnostic bundle exported. It contains sanitized launcher logs, not engine logs, games, BIOS or saves.");
    });
    void ShowDeveloper()
    {
        content.Content=UI.Column(UI.Button("←  Settings",()=>Navigate("Settings")),UI.Heading("Developer details",29),UI.Text($"Frontend: 0.1.0\nOS: {Environment.OSVersion}\nWPF render tier: {System.Windows.Media.RenderCapability.Tier >> 16}\nGPU name / emulator renderer: not queried\nActive game ID: {session.ActiveGameId??"None"}\nProfile: {settings.Display.Profile}\nConfig: {paths.Config}\nSave data: {Path.Combine(paths.Root,"engines")}\nLogs: {paths.Logs}\nXInput controllers: {ControllerMonitor.Connected().Count}",14,"Muted"),UI.Text("Engine versions are shown in System Status. The frontend does not currently receive GPU timing or audio synchronization telemetry from external engines.",13,"Muted"));
    }
    void RenderControllers()
    {
        var connected=ControllerMonitor.Connected();
        var panel=UI.Column(UI.Text("ONE PROFILE, THREE SYSTEMS",11,"Muted"),UI.Heading("Controllers",34),UI.Row(UI.Text(connected.Count==0?"Connect a controller to get started.":$"{connected.Count} XInput controller(s) connected.",18),UI.Button("Refresh",RenderControllers)));
        foreach(var (index,state) in connected)panel.Children.Add(UI.Panel(UI.Column(UI.Text($"●  PLAYER {index+1}",12,"Accent"),UI.Heading("XInput gamepad",22),UI.Text("Connected. Start a game to verify inputs end to end.",13,"Muted"),UI.Text($"Neutral snapshot: left ({state.Pad.LX}, {state.Pad.LY}) · right ({state.Pad.RX}, {state.Pad.RY}) · triggers ({state.Pad.LeftTrigger}, {state.Pad.RightTrigger})",12,"Muted"))));
        panel.Children.Add(UI.Text("Xbox-compatible defaults are translated to DualShock 2 and GameCube layouts. PS2 uses two native ports; GameCube supports four. N64 writes the selected per-player SDL device profile and manual mapping into the engine configuration on the next launch.",14,"Muted"));
        panel.Children.Add(UI.Text("PlayStation, Nintendo and generic SDL controllers need engine/device validation. Native detection in this preview is XInput only.",13,"Muted"));
        var players=UI.Choice(["1 player","2 players","3 players","4 players"],settings.Controller.Players-1);panel.Children.Add(players);
        var xboxActions=new CheckBox { Content="Use Xbox action layout for N64 and GameCube", IsChecked=settings.Controller.XboxActionLayout, Margin=new Thickness(0,8,0,8) }; panel.Children.Add(xboxActions);
        panel.Children.Add(UI.Text("This preset changes native button bindings, not game actions. Camera, aim, shoot and jump depend on the game's own controls. PCSX2 uses the manual mapping below; Play! applies an Xbox/XInput profile when a compatible controller is connected.",13,"Muted"));
        var n64CButtons=new CheckBox { Content="N64 only: map right stick to C-buttons for titles whose in-game scheme uses C as camera", IsChecked=settings.Controller.N64RightStickCButtons, Margin=new Thickness(0,4,0,8) }; panel.Children.Add(n64CButtons);
        var deadzoneOptions=new[]{4096,8192,12000,16000,20000,settings.Controller.N64AnalogDeadzone}.Distinct().Order().ToArray();
        var deadzone=UI.Choice(deadzoneOptions.Select(x=>$"N64 stick deadzone: {x:N0} / 32,768"),Array.IndexOf(deadzoneOptions,settings.Controller.N64AnalogDeadzone));
        panel.Children.Add(deadzone);
        var mappings=new Dictionary<string,ComboBox>();
        panel.Children.Add(UI.Text("Manual face-button mapping applies to PCSX2, and to N64/GameCube when Xbox action layout is off. A per-game N64 preset can override the global choice.",13,"Muted"));
        foreach(var logical in new[]{"South","East","West","North","Start","Select","L","R"})
        {var choice=UI.Choice(Controllers.Allowed,Array.IndexOf(Controllers.Allowed,settings.Controller.Buttons[logical]));mappings[logical]=choice;panel.Children.Add(UI.Row(new TextBlock{Text=logical,Width=140,VerticalAlignment=VerticalAlignment.Center},choice));}
        panel.Children.Add(UI.Row(UI.Button("Save global mapping",()=>Safe(()=>{settings.Controller.Players=players.SelectedIndex+1;settings.Controller.XboxActionLayout=xboxActions.IsChecked==true;settings.Controller.N64RightStickCButtons=n64CButtons.IsChecked==true;settings.Controller.N64AnalogDeadzone=deadzoneOptions[Math.Max(0,deadzone.SelectedIndex)];foreach(var pair in mappings)settings.Controller.Buttons[pair.Key]=(string)pair.Value.SelectedItem;SaveSettings();Notice("Mapping saved for the next launch. Verify controls in each engine before relying on the profile.");}),true),UI.Button("Reset defaults",()=>Safe(()=>{settings.Controller=new();SaveSettings();RenderControllers();}))));
        foreach(var platform in Enum.GetValues<ConsoleKind>().Where(p=>p!=ConsoleKind.Xbox360))panel.Children.Add(UI.Panel(UI.Column(UI.Heading(platform==ConsoleKind.PS2?"DualShock 2 manual mapping":platform==ConsoleKind.GameCube?"GameCube manual mapping (action mode off)":"N64 manual mapping (action mode off)",20),UI.Text(string.Join("   ·   ",Controllers.Translate(platform,settings.Controller).Select(p=>$"{p.Key} → {p.Value}")),13,"Muted"))));content.Content=panel;
    }
}

