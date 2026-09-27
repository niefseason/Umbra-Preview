using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Umbra.Core;
using Umbra.Core.Emulation;

var root=Path.Combine(Path.GetTempPath(),"umbra-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
var paths=new AppPaths(Path.Combine(root,"data"));var log=new EventLog(paths);var library=new LibraryService(paths,log);int passed=0,failed=0;
void Assert(bool condition,string message="Assertion failed") {if(!condition)throw new Exception(message);}
void Test(string name,Action action) {try{action();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e.Message);}}
void Throws<T>(Action action) where T:Exception {try{action();throw new Exception("Expected "+typeof(T).Name);}catch(T){}}
string Make(string name,byte[] data) {var file=Path.Combine(root,name);Directory.CreateDirectory(Path.GetDirectoryName(file)!);File.WriteAllBytes(file,data);return file;}
Test("renderer-aware fullscreen policy",()=>{Assert(DisplayManager.UsesManagedFullscreen(ConsoleKind.N64,true,"rice"));Assert(!DisplayManager.UsesManagedFullscreen(ConsoleKind.N64,true,"glide64mk2"));Assert(!DisplayManager.UsesManagedFullscreen(ConsoleKind.N64,false,"rice"));});
Test("startup window fits scaled and short work areas",()=>{
    foreach(var scale in new[]{1.0,1.25,1.5,2.0})foreach(var area in new[]{(1920,1040),(1366,728),(1280,680)}){
        var size=DisplayManager.StartupSize(area.Item1,area.Item2,scale);
        Assert(size.Width>0 && size.Width<area.Item1 && size.Height>0 && size.Height<area.Item2);
    }
    Assert(DisplayManager.StartupSize(1920,1040,1)==(1320,880));
    Assert(DisplayManager.StartupSize(1920,1040,1.5)==(1872,992));
});
Test("invalid startup dimensions rejected",()=>{
    Throws<ArgumentOutOfRangeException>(()=>DisplayManager.StartupSize(0,100,1));
    Throws<ArgumentOutOfRangeException>(()=>DisplayManager.StartupSize(100,100,double.NaN));
});
Test("modern sensitivity keeps default response and separates axes",()=>{
    var tuning=new ModernStickTuning();Assert(tuning.Peak(4096,false)==32768 && tuning.Peak(4096,true)==32768);
    tuning.HorizontalPercent=200;tuning.VerticalPercent=50;
    Assert(tuning.Peak(4096,false)==18432 && tuning.Peak(4096,true)==61440);
    Throws<UserError>(()=>tuning.Peak(32768,false));
    tuning.HorizontalPercent=201;Throws<UserError>(()=>tuning.Validate());
});
Test("modern tuning range and working restore",()=>{
    Throws<UserError>(()=>new ModernStickTuning{CameraDeadzone=32768}.Validate());
    Throws<UserError>(()=>new ModernStickTuning{MovementThreshold=0}.Validate());
    var g=new Game();var service=new WorkingProfiles(paths);
    service.Save(g,new Settings(),new GameOverride{StickTuning=new(){HorizontalPercent=125,MovementThreshold=10000}},new DisplaySettings());
    var restored=service.Restore(g,new GameOverride());Assert(restored.StickTuning!.HorizontalPercent==125 && restored.StickTuning.MovementThreshold==10000);
    Assert(service.Undo(g,restored).StickTuning is null);
});
Test("Play Xbox bindings use native XInput provider and separate sticks",()=>{
    var folder=Path.Combine(root,"play-config");Directory.CreateDirectory(Path.Combine(folder,"Play Data Files","inputprofiles"));
    var original=Path.Combine(folder,"Play Data Files","inputprofiles","default.xml");File.WriteAllText(original,"preserve keyboard");
    PlayConfiguration.Configure(folder,new ControllerProfile(),new[]{2},"vulkan");
    var doc=System.Xml.Linq.XDocument.Load(Path.Combine(folder,"Play Data Files","inputprofiles","umbra-xinput.xml"));
    string V(string key)=>(string)doc.Root!.Elements().Single(x=>(string?)x.Attribute("Name")==key).Attribute("Value")!;
    Assert(V("input.pad1.start.bindingtarget1.providerId")=="2020175472" && V("input.pad1.start.bindingtarget1.keyId")=="10");
    Assert(V("input.pad1.cross.bindingtarget1.keyId")=="16" && V("input.pad1.analog_right_x.bindingtarget1.keyId")=="2");
    Assert(V("input.pad1.analog_right_x.bindingtarget1.keyType")=="1" && V("input.pad1.r2.bindingtarget1.keyType")=="0");
    Assert(V("input.pad1.start.bindingtarget1.deviceId")=="2:0:0:0:0:0" && V("input.pad2.start.bindingtype")=="0");
    Assert(File.ReadAllText(original)=="preserve keyboard");
    var config=File.ReadAllText(Path.Combine(folder,"Play Data Files","config.xml"));Assert(config.Contains("umbra-xinput") && config.Contains("Value=\"1\""));
});
Test("Play preserves unrelated XML preferences and rejects unsafe XML",()=>{
    var folder=Path.Combine(root,"play-xml");Directory.CreateDirectory(Path.Combine(folder,"Play Data Files"));
    var file=Path.Combine(folder,"Play Data Files","config.xml");File.WriteAllText(file,"<Config><Preference Name=\"audio.enableoutput\" Type=\"boolean\" Value=\"false\" /></Config>");
    PlayConfiguration.Configure(folder,null,null);Assert(File.ReadAllText(file).Contains("audio.enableoutput") && File.ReadAllText(file).Contains("false"));
    var bad="<!DOCTYPE Config [<!ENTITY x SYSTEM 'file:///missing'>]><Config>&x;</Config>";File.WriteAllText(file,bad);
    Throws<UserError>(()=>PlayConfiguration.Configure(folder,new ControllerProfile(),new[]{0}));Assert(File.ReadAllText(file)==bad);
    Throws<UserError>(()=>PlayConfiguration.Configure(folder,null,new[]{0,0}));
});
Test("4:3 pillarbox geometry",()=>Assert(DisplayManager.Fit(1920,1080)==(240,0,1440,1080)));
Test("4:3 portrait letterbox geometry",()=>Assert(DisplayManager.Fit(1080,1920)==(0,555,1080,810)));
var n64=Make("games/space & quote ' original.z64",[0x80,0x37,0x12,0x40,0,0,0,0]);
Test("working profile freezes effective settings without mutating global",()=>{
    var g=library.Import(n64);var service=new WorkingProfiles(paths);var global=new Settings();global.Controller.N64AnalogDeadzone=6000;
    var options=new GameOverride{ControllerPreset="goldeneye-solitaire",N64VideoPlugin="rice",StartupStateSlot=2,Notes="keep"};
    service.Save(g,global,options,new DisplaySettings{Profile=GraphicsProfile.Custom,Stretch=true});
    global.Controller.N64AnalogDeadzone=15000;options.ControllerPreset="no-c";options.StartupStateSlot=7;
    var restored=service.Restore(g,options);
    Assert(restored.SavedController!.N64AnalogDeadzone==6000 && global.Controller.N64AnalogDeadzone==15000);
    Assert(restored.Display!.Stretch && restored.ControllerPreset=="goldeneye-solitaire" && restored.StartupStateSlot==7 && restored.Notes=="keep");
    Assert(options.SavedController is null && service.CanUndo(g));
    var undo=service.Undo(g,restored);Assert(undo.SavedController is null && undo.ControllerPreset=="no-c" && undo.StartupStateSlot==7);
});
Test("damaged working profile cannot overwrite current game settings",()=>{
    var g=new Game();var service=new WorkingProfiles(paths);service.Save(g,new Settings(),new GameOverride(),new DisplaySettings());
    JsonStore.Write(paths.Override(g.Id),new GameOverride{Notes="original"});var before=File.ReadAllText(paths.Override(g.Id));
    File.WriteAllText(Path.Combine(paths.Root,"profiles",g.Id,"working.json"),"not json");
    Throws<UserError>(()=>service.Restore(g,new GameOverride()));Assert(File.ReadAllText(paths.Override(g.Id))==before && !service.CanUndo(g));
});
Test("working profile identity and schema are checked",()=>{
    var g=new Game();var service=new WorkingProfiles(paths);service.Save(g,new Settings(),new GameOverride(),new DisplaySettings());
    var file=Path.Combine(paths.Root,"profiles",g.Id,"working.json");var original=File.ReadAllText(file);
    File.WriteAllText(file,original.Replace(g.Id,Guid.NewGuid().ToString("N")));Throws<UserError>(()=>service.Restore(g,new GameOverride()));
    File.WriteAllText(file,original.Replace("\"Version\": 1","\"Version\": 999"));Throws<UserError>(()=>service.Restore(g,new GameOverride()));
});
Test("working profile refuses traversal IDs and invalid mappings",()=>{
    var service=new WorkingProfiles(paths);Throws<UserError>(()=>service.Exists(new Game{Id="../outside"}));
    var invalid=new Settings();invalid.Controller.Buttons["South"]="unsupported";
    Throws<UserError>(()=>service.Save(new Game(),invalid,new GameOverride(),new DisplaySettings()));
});
var gcdata=new byte[64];new byte[]{0xc2,0x33,0x9f,0x3d}.CopyTo(gcdata,28);var gc=Make("games/game.gcm",gcdata);
var ps2=Make("games/game.iso",Encoding.ASCII.GetBytes("fixture-only BOOT2 = cdrom0:\\HOME.ELF;1"));
Test("automatic modern profile identifies all N64 byte orders",()=>{
    foreach(var title in new[]{"Perfect Dark","GOLDENEYE"})foreach(var order in new[]{0,2,4}){
        var bytes=new byte[64];new byte[]{0x80,0x37,0x12,0x40}.CopyTo(bytes,0);Encoding.ASCII.GetBytes(title.PadRight(20)).CopyTo(bytes,32);
        if(order>0)for(int i=0;i<64;i+=order)Array.Reverse(bytes,i,order);
        Assert(N64GameProfiles.Resolve("auto-modern",Make(title+order+".rom",bytes))=="modern-12");
    }
});
Test("automatic modern refuses unknown title and truncated headers",()=>{
    Throws<UserError>(()=>N64GameProfiles.Resolve("auto-modern",n64));
    var bytes=new byte[64];new byte[]{0x80,0x37,0x12,0x40}.CopyTo(bytes,0);Encoding.ASCII.GetBytes("OTHER GAME").CopyTo(bytes,32);
    Throws<UserError>(()=>N64GameProfiles.Resolve("auto-modern",Make("GoldenEye misleading.z64",bytes)));
});
Test("shared modern preset preserves GoldenEye native mapping",()=>{
    var source=new ControllerProfile();var a=Controllers.ForN64Preset(source,"goldeneye-solitaire");var b=Controllers.ForN64Preset(source,"modern-12");
    Assert(a.GoldenEyeSolitaire && b.GoldenEyeSolitaire && !source.GoldenEyeSolitaire && a.N64AnalogDeadzone==b.N64AnalogDeadzone);
});
Test("detect N64 big endian",()=>Assert(ContentDetection.Detect(n64)==ConsoleKind.N64));
Test("detect N64 byte swapped",()=>Assert(ContentDetection.Detect(Make("swap.v64",[0x37,0x80,0x40,0x12]))==ConsoleKind.N64));
Test("detect N64 little endian",()=>Assert(ContentDetection.Detect(Make("little.n64",[0x40,0x12,0x37,0x80]))==ConsoleKind.N64));
Test("detect GameCube magic",()=>Assert(ContentDetection.Detect(gc)==ConsoleKind.GameCube));
Test("detect PS2 BOOT2",()=>Assert(ContentDetection.Detect(ps2)==ConsoleKind.PS2));
Test("ambiguous ISO stays unknown",()=>Assert(ContentDetection.Detect(Make("unknown.iso",new byte[32])) is null));
Test("scripts never imported",()=>Throws<UserError>(()=>library.Import(Make("run.ps1",Encoding.UTF8.GetBytes("exit")),ConsoleKind.N64)));
Test("platform conflict rejected",()=>Throws<UserError>(()=>library.Import(n64,ConsoleKind.PS2)));
Test("GameCube rejects CHD before engine launch",()=>Throws<UserError>(()=>library.Import(Make("wrong-console.chd",Encoding.ASCII.GetBytes("MComprHD")),ConsoleKind.GameCube)));
Test("renaming CHD to ISO does not bypass platform validation",()=>Throws<UserError>(()=>ContentDetection.ValidatePlatformFormat(Make("renamed-chd.iso",Encoding.ASCII.GetBytes("MComprHD")),ConsoleKind.GameCube)));
Test("PS2 CHD remains available with explicit console selection",()=>Assert(library.Import(Make("ps2-container.chd",Encoding.ASCII.GetBytes("MComprHD")),ConsoleKind.PS2).Platform==ConsoleKind.PS2));
Test("empty BIOS rejected",()=>Throws<UserError>(()=>ContentDetection.ValidateBios(Make("empty.bin",[]))));
Test("random BIOS rejected",()=>Throws<UserError>(()=>ContentDetection.ValidateBios(Make("random.bin",new byte[4*1024*1024]))));
Test("missing BIOS rejected",()=>Throws<UserError>(()=>ContentDetection.ValidateBios(Path.Combine(root,"absent.bin"))));
var biosData=new byte[4*1024*1024];Encoding.ASCII.GetBytes("RESET ROMVER").CopyTo(biosData,0);var bios=Make("fixture-bios.bin",biosData);
Test("BIOS structural screening",()=>ContentDetection.ValidateBios(bios));
Test("scan finds supported headers without copying",()=> {var before=File.ReadAllBytes(n64);var result=library.Scan([Path.Combine(root,"games")]);Assert(result.Games.Count==3);Assert(File.ReadAllBytes(n64).SequenceEqual(before));});
Test("scan missing directory is nonfatal",()=>Assert(library.Scan([Path.Combine(root,"missing")]).Warnings.Count==1));
Test("scan cancellation",()=>{var cts=new CancellationTokenSource();cts.Cancel();Throws<OperationCanceledException>(()=>library.Scan([Path.Combine(root,"games")],cts.Token));});
Test("deduplicate without losing favorites",()=>{var list=new List<Game>();var game=library.Import(n64);game.Favorite=true;Assert(LibraryService.Merge(list,[game,library.Import(n64)])==1);Assert(list[0].Favorite);});
Test("library persists playtime and favorites",()=>{var game=library.Import(n64);game.Favorite=true;game.PlaySeconds=98;library.Save([game]);var loaded=library.Load();Assert(loaded.Count==1 && loaded[0].Favorite && loaded[0].PlaySeconds==98);});
Test("JSON atomic backup recovery",()=>{var file=Path.Combine(root,"recovery.json");JsonStore.Write(file,new[]{1});JsonStore.Write(file,new[]{2});File.WriteAllText(file,"{");var warnings=new List<string>();Assert(JsonStore.Read(file,()=>new int[0],warnings.Add)[0]==1);Assert(warnings.Count==1);Assert(Directory.GetFiles(root,"recovery.json.corrupt-*").Length==1);});
Test("corrupt config without backup preserved",()=>{var file=Make("bad-settings.json",Encoding.UTF8.GetBytes("{"));var s=JsonStore.LoadSettings(file);Assert(s.SchemaVersion==2);Assert(Directory.GetFiles(root,"bad-settings.json.corrupt-*").Length==1);});
Test("migrate v1",()=>{var file=Make("v1.json",Encoding.UTF8.GetBytes("{\"schemaVersion\":1}"));Assert(JsonStore.LoadSettings(file).SchemaVersion==2);Assert(File.Exists(file+".bak"));});
Test("future config not overwritten",()=>{var file=Make("future.json",Encoding.UTF8.GetBytes("{\"schemaVersion\":99}"));Throws<UserError>(()=>JsonStore.LoadSettings(file));Assert(File.ReadAllText(file).Contains("99"));});
Test("null nested config normalized",()=>{var file=Make("null.json",Encoding.UTF8.GetBytes("{\"display\":null,\"backends\":null,\"controller\":null}"));Assert(JsonStore.LoadSettings(file).Backends.Count==4);});
Test("invalid scale rejected",()=>Throws<UserError>(()=>JsonStore.ValidateDisplay(new(){Scale=99})));
Test("invalid backend dictionary is normalized",()=>{var file=Make("bad-backends.json",Encoding.UTF8.GetBytes("{\"backends\":{\"PS2\":null}}"));var settings=JsonStore.LoadSettings(file);Assert(settings.Backends.Count==4&&settings.Backends[ConsoleKind.PS2] is not null);});
Test("safe override ID",()=>Throws<UserError>(()=>paths.Override("../outside")));
Test("controller PS2 cross mapping",()=>Assert(Controllers.Translate(ConsoleKind.PS2,new())["Cross"]=="A"));
Test("PCSX2 SDL face names are positions and unknown inputs fail",()=>{
    Assert(Controllers.PcsxButton("A")=="FaceSouth" && Controllers.PcsxButton("B")=="FaceEast" && Controllers.PcsxButton("X")=="FaceWest" && Controllers.PcsxButton("Y")=="FaceNorth");
    Throws<UserError>(()=>Controllers.PcsxButton("Bogus"));
});
Test("controller GC shoulders mapping",()=>Assert(Controllers.Translate(ConsoleKind.GameCube,new())["L"]=="LeftShoulder"));
Test("controller N64 mapping intent",()=>Assert(Controllers.Translate(ConsoleKind.N64,new())["Z Trig"]=="Back"));
Test("controller invalid input rejected",()=>{var c=new ControllerProfile();c.Buttons["South"]="script";Throws<UserError>(()=>Controllers.Translate(ConsoleKind.N64,c));});
Test("device-aware SDL mapping applies known GUID inputs",()=>{var device=new SdlDeviceProfile{Guid="030000005e0400008e02000000000000",Name="Xbox",Inputs=new(){["A Button"]="button(1)",["Z Trig"]="button(5)"}};var map=Controllers.TranslateN64Sdl(device);Assert(map["A Button"]=="button(1)"&&map["Z Trig"]=="button(5)");});
Test("Xbox SDL defaults keep trigger analog and Start distinct",()=>{var map=Controllers.DefaultSdlDevice().Inputs;Assert(map["Z Trig"]=="axis(4+)"&&map["Start"]=="button(7)"&&map["X Axis"]=="axis(0-,0+)");});
Test("N64 manual mapping applies logical profile to SDL inputs",()=>{var profile=new ControllerProfile{XboxActionLayout=false};profile.Buttons["West"]="X";profile.Buttons["North"]="Y";var map=Controllers.TranslateN64Sdl(new SdlDeviceProfile{Inputs=new(){["X Button"]="button(8)",["Y Button"]="button(7)"}},profile);Assert(map["C Button L"]=="button(8)"&&map["C Button U"]=="button(7)");});
Test("N64 logical remap resolves from original device snapshot",()=>{var profile=new ControllerProfile{XboxActionLayout=false};profile.Buttons["South"]="B";profile.Buttons["East"]="A";var map=Controllers.TranslateN64Sdl(new SdlDeviceProfile{Inputs=new(){["A Button"]="button(10)",["B Button"]="button(11)"}},profile);Assert(map["A Button"]=="button(11)"&&map["B Button"]=="button(10)");});
Test("Xbox action layout separates movement camera and actions",()=>{var map=Controllers.TranslateN64Sdl(new SdlDeviceProfile(),new ControllerProfile{N64RightStickCButtons=true});Assert(map["X Axis"]=="axis(0-,0+)"&&map["C Button R"]=="axis(2+,24000)"&&map["Z Trig"]=="axis(4+)"&&map["B Button"].Contains("axis(5+"));});
Test("N64 action layout can suppress game-specific C-button actions",()=>{var map=Controllers.TranslateN64Sdl(new SdlDeviceProfile(),new ControllerProfile());Assert(map["C Button R"]==""&&map["C Button U"]==""&&map["C Button X Axis"]=="");});
Test("N64 per-game presets override global action mode",()=>{var global=new ControllerProfile{XboxActionLayout=true,N64RightStickCButtons=false};var classic=Controllers.ForN64Preset(global,"classic");var modern=Controllers.ForN64Preset(global,"modern-shooter");var safe=Controllers.ForN64Preset(global,"no-c");var classicMap=Controllers.TranslateN64Sdl(new SdlDeviceProfile(),classic);var modernMap=Controllers.TranslateN64Sdl(new SdlDeviceProfile(),modern);var safeMap=Controllers.TranslateN64Sdl(new SdlDeviceProfile(),safe);Assert(!classic.XboxActionLayout&&!classic.N64RightStickCButtons&&modern.XboxActionLayout&&modern.N64RightStickCButtons&&safe.XboxActionLayout&&!safe.N64RightStickCButtons&&classicMap["C Button R"]==""&&modernMap["C Button R"]=="axis(2+,24000)"&&safeMap["C Button R"]==""&&global.XboxActionLayout);});
Test("SDL mapping rejects unsafe expressions",()=>{var map=Controllers.TranslateN64Sdl(new SdlDeviceProfile{Inputs=new(){["A Button"]="\"; run"}});Assert(map["A Button"]=="button(0)");});
Test("capability catalog remains explicit",()=>{var caps=CapabilityCatalog.For(ConsoleKind.N64);Assert(caps.StartupState&&caps.Screenshots&&!caps.AutosaveOnExit);});
Test("register state file preserves selected slot",()=>{var source=Make("engine-state.bin",[1,2,3,4]);var service=new StateSlotService(paths,log);service.RegisterStateFile(ConsoleKind.N64,2,source);Assert(service.SlotPath(ConsoleKind.N64,2).Contains(Path.Combine("N64","states"))&&File.ReadAllBytes(service.SlotPath(ConsoleKind.N64,2)).SequenceEqual(new byte[]{1,2,3,4}));});
Test("state slots use native engine folders",()=>{var service=new StateSlotService(paths,log);Assert(service.Folder(ConsoleKind.PS2).EndsWith(Path.Combine("PS2","PCSX2","sstates"))&&service.Folder(ConsoleKind.GameCube).EndsWith(Path.Combine("GameCube","StateSaves")));});
var saves=new SaveService(paths,log);
Test("create save paths for all platforms",()=>{foreach(var p in Enum.GetValues<ConsoleKind>())saves.Ensure(p);Assert(Directory.Exists(Path.Combine(paths.Engine(ConsoleKind.PS2),"memcards")));});
Test("save path unavailable blocks writes",()=>{var blockedPaths=new AppPaths(Path.Combine(root,"blocked-data"));Directory.CreateDirectory(blockedPaths.Engine(ConsoleKind.N64));File.WriteAllText(Path.Combine(blockedPaths.Engine(ConsoleKind.N64),"save"),"occupied");Throws<IOException>(()=>new SaveService(blockedPaths,new EventLog(blockedPaths)).Ensure(ConsoleKind.N64));});
Test("backup and recovery preserves prior save",()=>{var file=Path.Combine(paths.Engine(ConsoleKind.N64),"save","progress.sra");File.WriteAllText(file,"original progress");var backup=saves.Backup(ConsoleKind.N64);File.WriteAllText(file,"new progress");saves.Restore(backup,ConsoleKind.N64);Assert(File.ReadAllText(file)=="original progress");Assert(Directory.GetFiles(paths.Backups,"*.zip").Length>=2);});
Test("wrong console restore rejected",()=>{var backup=saves.Backup(ConsoleKind.N64);Throws<UserError>(()=>saves.Restore(backup,ConsoleKind.PS2));});
Test("malicious backup path rejected",()=>{var file=Path.Combine(root,"attack.zip");using(var zip=ZipFile.Open(file,ZipArchiveMode.Create)){using var writer=new StreamWriter(zip.CreateEntry("manifest.json").Open());writer.Write(JsonSerializer.Serialize(new SaveManifest(1,ConsoleKind.N64,DateTimeOffset.UtcNow,[new("save/../../escape",1,"x")]),JsonStore.Options));}Throws<UserError>(()=>saves.Restore(file,ConsoleKind.N64));Assert(!File.Exists(Path.Combine(paths.Root,"escape")));});
Test("backup tampering never changes live saves",()=>{var backup=saves.Backup(ConsoleKind.N64);using(var zip=ZipFile.Open(backup,ZipArchiveMode.Update)){zip.GetEntry("save/progress.sra")!.Delete();using var writer=new StreamWriter(zip.CreateEntry("save/progress.sra").Open());writer.Write("tampered progress");}Throws<UserError>(()=>saves.Restore(backup,ConsoleKind.N64));Assert(File.ReadAllText(Path.Combine(paths.Engine(ConsoleKind.N64),"save","progress.sra"))=="original progress");});
Test("metadata normalization and bounds",()=>{var file=Make("metadata.json",Encoding.UTF8.GetBytes("{\"title\":\"Homebrew\\nTitle\",\"year\":9999,\"genre\":\"Puzzle\"}"));var m=MetadataService.Read(file);Assert(m.Title=="HomebrewTitle" && m.Metadata.Year is null && m.Metadata.Genre=="Puzzle");});
Test("metadata without network",()=>{var file=Make("offline.json",Encoding.UTF8.GetBytes("{}"));Assert(MetadataService.Read(file).Title is null);});
Test("additional disc extensions require headers",()=>{foreach(var ext in new[]{".chd",".cso",".gcz",".ciso"})Assert(ContentDetection.Detect(Make("unknown"+ext,new byte[64])) is null);});
Test("compatibility refuses silent enhancements",()=>{var game=library.Import(n64);var db=new CompatibilityDatabase{Entries=[new(){ContentId=game.ContentId,Platform=game.Platform,BackendSha256="x",Display=new(){Profile=GraphicsProfile.Enhanced}}]};Assert(db.Resolve(game,"x") is null);});
Test("compatibility requires exact engine identity",()=>{var game=library.Import(n64);var db=new CompatibilityDatabase{Entries=[new(){ContentId=game.ContentId,Platform=game.Platform,BackendSha256="known",Display=new(){Profile=GraphicsProfile.OriginalHardware}}]};Assert(db.Resolve(game,"known") is not null&&db.Resolve(game,"changed") is null);});
Test("diagnostics redact imported paths",()=>{var s=new Settings{BiosPath=bios,LibraryFolders=[root]};var report=Diagnostics.Report(paths,s,0);Assert(!report.Contains(root) && !report.Contains(bios));});
Test("missing backend returns actionable error",()=>Throws<UserError>(()=>Adapters.Prepare(paths,new Settings(),library.Import(n64))));
Test("null SDL device entries recover without losing valid profiles",()=>{
    var file=Make("null-devices.json",Encoding.UTF8.GetBytes("""{"Controller":{"SdlDevices":{"0":null,"1":{"Name":"Saved pad","Inputs":null},"2":{"Inputs":{"A Button":"button(9)"}}}}}"""));
    var loaded=JsonStore.LoadSettings(file).Controller;
    Assert(loaded.SdlDevices[0] is not null && loaded.SdlDevices[1].Inputs is not null);
    Assert(Controllers.TranslateN64Sdl(loaded.SdlDevices[1])["A Button"]=="button(0)");
    Assert(loaded.SdlDevices[2].Inputs["A Button"]=="button(9)" && File.ReadAllText(file).Contains("Saved pad"));
});
Test("connected controller does not claim gameplay readiness",()=>Assert(Diagnostics.Check(paths,new Settings(),1).Single(x=>x.Component=="Controllers").State=="DETECTED"));

Test("Xbox 360 headers and containers are validated without reclassifying other consoles",()=>{
    var xex=new byte[24];"XEX2"u8.CopyTo(xex);var file=Make("xbox360/default.xex",xex);
    Assert(library.Import(file).Platform==ConsoleKind.Xbox360);
    Throws<UserError>(()=>library.Import(file,ConsoleKind.N64));
    Throws<UserError>(()=>library.Import(Make("xbox360/fake.xex",new byte[24]),ConsoleKind.Xbox360));
    Throws<UserError>(()=>library.Import(Make("xbox360/fake.iso",new byte[128]),ConsoleKind.Xbox360));
    var iso=new byte[65536+20];"MICROSOFT*XBOX*MEDIA"u8.CopyTo(iso.AsSpan(65536));var disc=Make("xbox360/disc.iso",iso);
    Assert(ContentDetection.Detect(disc)==null && library.Import(disc,ConsoleKind.Xbox360).Platform==ConsoleKind.Xbox360);
    Throws<UserError>(()=>library.Import(ps2,ConsoleKind.Xbox360));
});
Test("Xbox 360 saves backup only content and restore independently",()=>{
    var folder=Path.Combine(paths.Engine(ConsoleKind.Xbox360),"content","profile");Directory.CreateDirectory(folder);
    var save=Path.Combine(folder,"save.dat");File.WriteAllText(save,"before");
    var backup=saves.Backup(ConsoleKind.Xbox360);File.WriteAllText(save,"after");saves.Restore(backup,ConsoleKind.Xbox360);
    Assert(File.ReadAllText(save)=="before");Throws<UserError>(()=>saves.Restore(backup,ConsoleKind.PS2));
    Assert(!CapabilityCatalog.For(ConsoleKind.Xbox360).SaveStates);
    Throws<UserError>(()=>new StateSlotService(paths,log).RegisterStateFile(ConsoleKind.Xbox360,0,save));
});
if(args.Length>0)
{
    const string xboxSdl="guid,Xbox,a:b0,b:b1,x:b2,y:b3,start:b7,leftshoulder:b4,rightshoulder:b5,leftx:a0,lefty:a1,rightx:a2,righty:a3,lefttrigger:a4,righttrigger:a5,dpup:h0.1,dpdown:h0.4,dpleft:h0.8,dpright:h0.2,";
    const string sonySdl="guid,PlayStation,a:b1,b:b2,x:b0,y:b3,start:b9,leftshoulder:b4,rightshoulder:b5,leftx:a0,lefty:a1,rightx:a3,righty:a4,lefttrigger:a2,righttrigger:a5,";
    Test("movement tuning cannot change camera or fire bindings",()=>{
        var baseline=GoldenEyeControls.Translate(xboxSdl);var changed=GoldenEyeControls.Translate(xboxSdl,movementThreshold:12000);
        foreach(var key in baseline.Keys.Where(k=>!k.StartsWith("C Button ")))Assert(baseline[key]==changed[key]);
        Assert(changed["C Button L"].Contains("12000") && changed["C Button U"].Contains("12000"));
    });
    Test("GoldenEye separates movement look aim and fire",()=>{
        var map=GoldenEyeControls.Translate(xboxSdl);
        Assert(map["X Axis"]=="axis(2-,2+)" && map["Y Axis"]=="axis(3-,3+)");
        Assert(map["C Button U"]=="axis(1-,8000) hat(0 Up)" && map["C Button R"]=="axis(0+,8000) hat(0 Right)");
        Assert(map["R Trig"]=="axis(4+,16000)" && map["Z Trig"]=="axis(5+,16000)");
        Assert(map["B Button"]=="button(0)" && map["A Button"]=="button(5)" && map["L Trig"]=="");
    });
    Test("GoldenEye reads PlayStation physical indices instead of Xbox guesses",()=>{
        var map=GoldenEyeControls.Translate(sonySdl,false,true,true);
        Assert(map["X Axis"]=="axis(3-,3+)" && map["Y Axis"]=="axis(4-,4+)");
        Assert(map["B Button"]=="button(0)" && map["A Button"]=="button(4)" && map["R Trig"]=="axis(2+,16000)");
    });
    Test("GoldenEye inversion affects look only",()=>{
        var normal=GoldenEyeControls.Translate(xboxSdl);
        var inverted=GoldenEyeControls.Translate(xboxSdl,true);
        Assert(inverted["Y Axis"]=="axis(3+,3-)");
        Assert(normal.Where(p=>p.Key!="Y Axis").All(p=>inverted[p.Key]==p.Value));
    });
    Test("GoldenEye respects SDL inverted stick mapping",()=>{
        var map=GoldenEyeControls.Translate(xboxSdl.Replace("leftx:a0","leftx:a0~").Replace("righty:a3","righty:a3~"));
        Assert(map["C Button R"].StartsWith("axis(0-,8000)") && map["Y Axis"]=="axis(3+,3-)");
    });
    Test("GoldenEye rejects unknown missing and split stick mappings",()=>{
        foreach(var value in new[]{"",xboxSdl.Replace("rightx:a2,",""),xboxSdl.Replace("rightx:a2","rightx:+a2"),xboxSdl+"\nunsafe"})
            Throws<UserError>(()=>GoldenEyeControls.Translate(value));
    });
    Test("GoldenEye never emits silently ignored duplicate input clauses",()=>{
        foreach(var expression in GoldenEyeControls.Translate(xboxSdl).Values)
            foreach(var kind in new[]{"button(","axis(","hat("})
                Assert(expression.Split(kind).Length<=2);
    });
    Test("GoldenEye preset does not mutate global mapping",()=>{
        var global=new ControllerProfile();
        var golden=Controllers.ForN64Preset(global,"goldeneye-solitaire");
        Assert(golden.GoldenEyeSolitaire&&!global.GoldenEyeSolitaire);
        golden.Buttons["South"]="X";Assert(global.Buttons["South"]=="A");
        Assert(!Controllers.ForN64Preset(golden,"classic").GoldenEyeSolitaire);
        Assert(GoldenEyeControls.EmptyBindings().Values.All(string.IsNullOrEmpty));
    });
    var backend=Path.GetFullPath(args[0]);var settings=new Settings{BiosPath=bios};foreach(var p in Enum.GetValues<ConsoleKind>())settings.Backends[p]=new(){Executable=backend,Sha256=Adapters.HashExecutable(backend)};
    foreach(var (platform,file) in new[]{(ConsoleKind.N64,n64),(ConsoleKind.GameCube,gc),(ConsoleKind.PS2,ps2)})
    {
        var game=library.Import(file);var plan=Adapters.Prepare(paths,settings,game);
        Test(platform+" launch arguments isolate data and preserve file path",()=>{Assert(plan.Arguments.Contains(Path.GetFullPath(file)));Assert(plan.Arguments.Contains(platform==ConsoleKind.PS2?Path.GetDirectoryName(paths.Engine(platform))!:paths.Engine(platform)));Assert(!plan.StartInfo().UseShellExecute);});
        if(platform==ConsoleKind.N64) Test("N64 generated config uses manual native bindings",()=>{var text=File.ReadAllText(Path.Combine(paths.Engine(platform),"mupen64plus.cfg"));Assert(text.Contains("mode = 0")&&text.Contains("device = 0")&&text.Contains("AnalogDeadzone = 4096,4096")&&text.Contains("Start = button(7)")&&text.Contains("X Axis = axis(0-,0+)")&&text.Contains("C Button R = \n")&&!text.Contains("Start Button = button(7)"));});
        if(platform==ConsoleKind.N64) Test("N64 modern preset emits right-stick C axes",()=>{var modern=new Settings{BiosPath=bios,Controller=Controllers.ForN64Preset(settings.Controller,"modern-shooter")};foreach(var p in Enum.GetValues<ConsoleKind>())modern.Backends[p]=settings.Backends[p];Adapters.Prepare(paths,modern,library.Import(n64));var text=File.ReadAllText(Path.Combine(paths.Engine(platform),"mupen64plus.cfg"));Assert(text.Contains("C Button R = axis(2+,24000)")&&text.Contains("C Button U = axis(3-,24000)"));});
        Test(platform+" controlled process integration",()=>{var report=Path.Combine(root,platform+"-args.json");Environment.SetEnvironmentVariable("UMBRA_TEST_REPORT",report);using var session=new GameSession(paths,log);var result=session.Run(plan,game).GetAwaiter().GetResult();Assert(result.ExitCode==0 && result.Seconds>0 && !session.Active);Assert(File.ReadAllText(report).Contains(platform==ConsoleKind.PS2?"-datapath":platform==ConsoleKind.GameCube?"-u":"--configdir"));});
    }
    Test("PCSX2 original profile disables enhancements",()=>{var text=File.ReadAllText(Path.Combine(paths.Engine(ConsoleKind.PS2),"inis","PCSX2.ini"));Assert(text.Contains("upscale_multiplier = 1")&&text.Contains("EnableWideScreenPatches = False"));});
    Test("PCSX2 manual face remaps reach both ports and reset stale bindings",()=>{
        var scoped=new Settings{BiosPath=bios};scoped.Backends[ConsoleKind.PS2]=settings.Backends[ConsoleKind.PS2];scoped.Controller.Players=2;
        scoped.Controller.Buttons["South"]="B";scoped.Controller.Buttons["East"]="A";
        var game=library.Import(ps2);Adapters.Prepare(paths,scoped,game);
        var file=Path.Combine(paths.Engine(ConsoleKind.PS2),"inis","PCSX2.ini");var text=File.ReadAllText(file);
        foreach(var port in new[]{0,1})Assert(text.Contains($"Cross = SDL-{port}/FaceEast") && text.Contains($"Circle = SDL-{port}/FaceSouth") && text.Contains($"Square = SDL-{port}/FaceWest") && text.Contains($"Triangle = SDL-{port}/FaceNorth"));
        Adapters.Prepare(paths,settings,game);text=File.ReadAllText(file);
        Assert(text.Contains("Cross = SDL-0/FaceSouth") && text.Contains("Circle = SDL-0/FaceEast") && text.Contains("L2 = SDL-0/+LeftTrigger") && text.Contains("RUp = SDL-0/-RightY"));
        Assert(!text.Contains("Cross = SDL-0/A\n") && !text.Contains("Triangle = SDL-0/Y\n"));
    });
    Test("PCSX2 custom presentation and audio do not leak into next game",()=>{
        var game=library.Import(ps2);var file=Path.Combine(paths.Engine(ConsoleKind.PS2),"inis","PCSX2.ini");
        Adapters.Prepare(paths,settings,game,new DisplaySettings{Profile=GraphicsProfile.Custom,Scale=2,Stretch=true},pcsxTuning:new(){AudioBufferMs=100,ShowPerformance=true});
        var custom=File.ReadAllText(file);
        Assert(custom.Contains("AspectRatio = Stretch") && custom.Contains("upscale_multiplier = 2") && custom.Contains("BufferMS = 100") && custom.Contains("OsdShowSpeed = True") && custom.Contains("SyncMode = TimeStretch") && custom.Contains("NominalScalar = 1"));
        Adapters.Prepare(paths,settings,game,new DisplaySettings());
        var original=File.ReadAllText(file);
        Assert(original.Contains("AspectRatio = Auto 4:3/3:2") && original.Contains("upscale_multiplier = 1") && original.Contains("BufferMS = 50") && original.Contains("OsdShowSpeed = False"));
    });
    Test("PCSX2 invalid buffer does not rewrite native config",()=>{
        var file=Path.Combine(paths.Engine(ConsoleKind.PS2),"inis","PCSX2.ini");var before=File.ReadAllText(file);
        Throws<UserError>(()=>Adapters.Prepare(paths,settings,library.Import(ps2),pcsxTuning:new(){AudioBufferMs=-1}));
        Assert(File.ReadAllText(file)==before);
    });
    Test("working profile restores and undoes PS2 tuning",()=>{
        var game=library.Import(ps2);var profiles=new WorkingProfiles(paths);
        profiles.Save(game,settings,new GameOverride{PcsxTuning=new(){AudioBufferMs=100,ShowPerformance=true}},new DisplaySettings{Profile=GraphicsProfile.Custom,Stretch=true,Scale=2});
        var restored=profiles.Restore(game,new GameOverride{PcsxTuning=new(){AudioBufferMs=75}});
        Assert(restored.PcsxTuning!.AudioBufferMs==100 && restored.PcsxTuning.ShowPerformance && restored.Display!.Scale==2);
        Assert(profiles.Undo(game,restored).PcsxTuning!.AudioBufferMs==75);
        Throws<UserError>(()=>profiles.Save(game,settings,new GameOverride{PcsxTuning=new(){AudioBufferMs=999}},new DisplaySettings()));
    });
    Test("Dolphin enhanced scales without texture packs",()=>{settings.Display.Profile=GraphicsProfile.Enhanced;Adapters.Prepare(paths,settings,library.Import(gc));var text=File.ReadAllText(Path.Combine(paths.Engine(ConsoleKind.GameCube),"Config","GFX.ini"));var pad=File.ReadAllText(Path.Combine(paths.Engine(ConsoleKind.GameCube),"Config","GCPadNew.ini"));Assert(text.Contains("InternalResolution = 3")&&text.Contains("HiresTextures = False")&&pad.Contains("Buttons/B = `Trigger R`")&&pad.Contains("Buttons/Z = `Trigger L`")&&pad.Contains("Triggers/L = `Shoulder L`"));});
    Test("Dolphin preserves copy filter and default texture sampling",()=>{var text=File.ReadAllText(Path.Combine(paths.Engine(ConsoleKind.GameCube),"Config","GFX.ini"));Assert(text.Contains("DisableCopyFilter = False") && text.Contains("ForceTextureFiltering = 0"));});
    Test("Dolphin action triggers do not also drive analog shoulders",()=>{
        var pad=File.ReadAllText(Path.Combine(paths.Engine(ConsoleKind.GameCube),"Config","GCPadNew.ini"));
        Assert(pad.Contains("Triggers/L-Analog = `Shoulder L`") && pad.Contains("Triggers/R-Analog = `Shoulder R`"));
        Assert(!pad.Contains("Triggers/L-Analog = `Trigger L`") && !pad.Contains("Triggers/R-Analog = `Trigger R`"));
    });
    Test("Dolphin layout switching replaces stale analog bindings",()=>{
        settings.Controller.XboxActionLayout=false;
        Adapters.Prepare(paths,settings,library.Import(gc));
        var file=Path.Combine(paths.Engine(ConsoleKind.GameCube),"Config","GCPadNew.ini");
        var classic=File.ReadAllText(file);
        Assert(classic.Contains("Triggers/L-Analog = `Trigger L`") && classic.Contains("Buttons/B = `Button B`"));
        settings.Controller.XboxActionLayout=true;
        Adapters.Prepare(paths,settings,library.Import(gc));
        var action=File.ReadAllText(file);
        Assert(action.Contains("Triggers/L-Analog = `Shoulder L`") && !action.Contains("Triggers/L-Analog = `Trigger L`"));
    });
    Test("backend crash returns and clears journal",()=>{Environment.SetEnvironmentVariable("UMBRA_TEST_CRASH","1");using var session=new GameSession(paths,log);var game=library.Import(n64);var result=session.Run(Adapters.Prepare(paths,settings,game),game).GetAwaiter().GetResult();Assert(result.ExitCode==17&&!session.Active&&!File.Exists(Path.Combine(paths.Root,"session.json")));Environment.SetEnvironmentVariable("UMBRA_TEST_CRASH",null);});
    Test("BIOS-free launch is isolated and does not require Sony BIOS",()=>{var plan=BiosFreeAdapter.Prepare(paths,settings.Backends[ConsoleKind.PS2],library.Import(ps2),new DisplaySettings());Assert(plan.Arguments.Contains("--disc"));Assert(plan.WorkingDirectory.EndsWith("Play"));Assert(File.Exists(Path.Combine(plan.WorkingDirectory,"portable.txt")));});
    Test("BIOS-free backend rejects PCSX2 startup states",()=>Throws<UserError>(()=>BiosFreeAdapter.Prepare(paths,settings.Backends[ConsoleKind.PS2],library.Import(ps2),new DisplaySettings(),0)));
    Test("BIOS-free unavailable enhancement rejected",()=>Throws<UserError>(()=>BiosFreeAdapter.Prepare(paths,settings.Backends[ConsoleKind.PS2],library.Import(ps2),new DisplaySettings{Profile=GraphicsProfile.Enhanced})));
    Test("N64 renderer selection rejects unknown and missing plugins",()=>{
        Throws<UserError>(()=>Adapters.Prepare(paths,settings,library.Import(n64),n64VideoPlugin:"../bad"));
        Throws<UserError>(()=>Adapters.Prepare(paths,settings,library.Import(n64),n64VideoPlugin:"glide64mk2"));
    });
    Test("N64 renderer override is explicit and scoped to launch",()=>{
        var executable=Make("renderer-fixture/backend.exe",File.ReadAllBytes(backend));
        var plugin=Make("renderer-fixture/mupen64plus-video-glide64mk2.dll",[]);
        var scoped=new Settings();scoped.Backends[ConsoleKind.N64]=new(){Executable=executable,Sha256=Adapters.HashExecutable(executable)};
        var plan=Adapters.Prepare(paths,scoped,library.Import(n64),n64VideoPlugin:"glide64mk2");
        var index=plan.Arguments.IndexOf("--gfx");Assert(index>=0 && Path.GetFullPath(plan.Arguments[index+1])==Path.GetFullPath(plugin));
        var next=Adapters.Prepare(paths,scoped,library.Import(n64));Assert(next.Arguments[next.Arguments.IndexOf("--gfx")+1]=="mupen64plus-video-rice.dll");
    });
    Test("native fullscreen initializes output size and aspect",()=>{
        var plan=Adapters.Prepare(paths,settings,library.Import(n64),new DisplaySettings{Profile=GraphicsProfile.Custom,Fullscreen=true,Stretch=true},outputSize:(1920,1080));
        Assert(plan.Arguments.Contains("--fullscreen") && plan.Arguments[plan.Arguments.IndexOf("--resolution")+1]=="1920x1080");
        Assert(File.ReadAllText(Path.Combine(paths.Engine(ConsoleKind.N64),"mupen64plus.cfg")).Contains("aspect = 2"));
        Adapters.Prepare(paths,settings,library.Import(n64),new DisplaySettings());
        Assert(File.ReadAllText(Path.Combine(paths.Engine(ConsoleKind.N64),"mupen64plus.cfg")).Contains("aspect = 0"));
    });
    Test("N64 default renderer stays Rice after a game override",()=>{
        var plan=Adapters.Prepare(paths,settings,library.Import(n64));var index=plan.Arguments.IndexOf("--gfx");
        Assert(index>=0 && plan.Arguments[index+1]=="mupen64plus-video-rice.dll");
    });
    Test("Xenia launch isolates paths and rejects unsupported presentation and states",()=>{
        var game=library.Import(Path.Combine(root,"xbox360","default.xex"));
        var plan=Adapters.Prepare(paths,settings,game);
        Assert(plan.Executable==backend && plan.Arguments.Contains("--target="+game.Path) && plan.Arguments.Contains("--hid=xinput") && plan.Arguments.Contains("--content_root="+Path.Combine(paths.Engine(ConsoleKind.Xbox360),"content")));
        Assert(!plan.Arguments.Any(a=>a.Contains("bios",StringComparison.OrdinalIgnoreCase)));
        Throws<UserError>(()=>Adapters.Prepare(paths,settings,game,startupState:0));
        Throws<UserError>(()=>Adapters.Prepare(paths,settings,game,new DisplaySettings{Profile=GraphicsProfile.Custom,Stretch=true}));
        var report=Path.Combine(root,"xenia-args.json");Environment.SetEnvironmentVariable("UMBRA_TEST_REPORT",report);
        using var session=new GameSession(paths,log);Assert(session.Run(plan,game).GetAwaiter().GetResult().ExitCode==0);
        Assert(File.ReadAllText(report).Contains("--target="));
    });
    Test("Canary audio test is per-game with separate content and no default leakage",()=>{
        var game=library.Import(Path.Combine(root,"xbox360","default.xex"));var selected=new BackendSettings{Executable=backend,Sha256=Adapters.HashExecutable(backend)};
        var candidate=Adapters.Prepare(paths,settings,game,xeniaAudioBackend:selected);
        var config=candidate.Arguments.Single(a=>a.StartsWith("--config="))[9..];
        Assert(File.ReadAllText(config).Contains("xma_decoder = \"new\"") && File.ReadAllText(config).Contains("use_dedicated_xma_thread = false"));
        Assert(candidate.Arguments.Contains("--content_root="+Path.Combine(paths.Engine(ConsoleKind.Xbox360),"content-canary")));
        var regular=Adapters.Prepare(paths,settings,game);Assert(!regular.Arguments.Any(a=>a.Contains("xma")) && regular.WorkingDirectory!=candidate.WorkingDirectory);
        var profiles=new WorkingProfiles(paths);profiles.Save(game,settings,new(){XeniaAudioBackend=selected},new());
        Assert(profiles.Restore(game,new()).XeniaAudioBackend!.Sha256==selected.Sha256);
        selected.Sha256="changed";Throws<UserError>(()=>Adapters.Prepare(paths,settings,game,xeniaAudioBackend:selected));
    });
    Test("changed executable refused",()=>{var bad=new BackendSettings{Executable=backend,Sha256="not the hash"};Throws<UserError>(()=>Adapters.ValidateBackend(bad));});
}
else {Console.WriteLine("SKIP process integration: supply the compiled test-backend executable");}
Console.WriteLine($"RESULT {passed} passed; {failed} failed. Synthetic fixtures only; no emulation verified.");
// Test artifacts are retained for inspection, never confused with deliverable games.
Console.WriteLine("Test artifacts: "+root);
return failed==0?0:1;



