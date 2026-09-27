using System.Diagnostics;
using Umbra.Core;
using Umbra.Core.Emulation;
if(args.Length==4 && args[0]=="--xenia-audio-boot")
{
    var xp=new AppPaths(args[3]);var xs=new Settings();var xb=new BackendSettings{Executable=args[1],Sha256=Adapters.HashExecutable(args[1])};
    var xg=new LibraryService(xp,new EventLog(xp)).Import(args[2],ConsoleKind.Xbox360);
    var planX=Adapters.Prepare(xp,xs,xg,new DisplaySettings{Fullscreen=false},xeniaAudioBackend:xb);
    using var processX=Process.Start(planX.StartInfo())!;
    await Task.Delay(120000);bool running=!processX.HasExited;
    if(running){processX.CloseMainWindow();if(!processX.WaitForExit(5000))processX.Kill(true);}
    await processX.WaitForExitAsync();Console.WriteLine($"Xenia audio candidate alive at 120 seconds: {running}; exit {processX.ExitCode}. No audible quality or physical input verification.");return 0;
}
if(args.Length>=5 && args[0]=="--pcsx-boot")
{
    var data=new AppPaths(args[4]);var ps2settings=new Settings{BiosPath=args[3]};
    ps2settings.Backends[ConsoleKind.PS2]=new(){Executable=args[1],Sha256=Adapters.HashExecutable(args[1])};
    var ps2game=new LibraryService(data,new EventLog(data)).Import(args[2],ConsoleKind.PS2);
    var tune=args.Contains("--tuned");
    var ps2plan=Adapters.Prepare(data,ps2settings,ps2game,new DisplaySettings{Fullscreen=false,Profile=tune?GraphicsProfile.Custom:GraphicsProfile.OriginalHardware,Scale=tune?2:1,Stretch=tune},pcsxTuning:new(){AudioBufferMs=tune?100:50,ShowPerformance=tune});
    var ps2info=ps2plan.StartInfo();ps2info.RedirectStandardOutput=true;ps2info.RedirectStandardError=true;
    using var proc=Process.Start(ps2info)!;var ps2stdout=proc.StandardOutput.ReadToEndAsync();var ps2stderr=proc.StandardError.ReadToEndAsync();
    await Task.Delay(20000);bool live=!proc.HasExited;
    if(live){proc.CloseMainWindow();if(!proc.WaitForExit(5000))proc.Kill(true);}
    await proc.WaitForExitAsync();File.WriteAllText(Path.Combine(data.Root,"pcsx-probe.log"),await ps2stdout+"\n"+await ps2stderr);
    Console.WriteLine($"PCSX2 probe alive at 20 seconds: {live}; exit {proc.ExitCode}. Inspect logs; not gameplay/input evidence.");return 0;
}
if(args.Length==3 && args[0]=="--save-working")
{
    var data=new AppPaths(args[1]);var current=JsonStore.LoadSettings(data.Config);
    var selected=new LibraryService(data,new EventLog(data)).Load().Single(g=>g.Id==args[2]);
    var over=JsonStore.Read(data.Override(selected.Id),()=>new GameOverride());
    new WorkingProfiles(data).Save(selected,current,over,over.Display??current.Display);
    Console.WriteLine("Saved working profile for "+selected.Title+"; native saves unchanged.");return 0;
}
if(args.Length==2 && args[0]=="--sdl")
{
    var devices=SdlControllerDiscovery.Read(args[1]);
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(devices,JsonStore.Options));
    foreach(var device in devices)
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(GoldenEyeControls.Translate(device.StandardMapping),JsonStore.Options));
    return devices.Count>0?0:1;
}
if(args.Length==3 && args[0]=="--pcsx-config")
{
    var configPaths=new AppPaths(args[2]);var biosFixture=Path.Combine(configPaths.Root,"synthetic-bios.bin");var bytes=new byte[4*1024*1024];System.Text.Encoding.ASCII.GetBytes("RESET ROMVER").CopyTo(bytes,0);File.WriteAllBytes(biosFixture,bytes);
    var disc=Path.Combine(configPaths.Root,"synthetic.iso");File.WriteAllText(disc,"BOOT2 synthetic non-executable fixture");
    var configSettings=new Settings{BiosPath=biosFixture};configSettings.Backends[ConsoleKind.PS2]=new(){Executable=Path.GetFullPath(args[1]),Sha256=Adapters.HashExecutable(args[1])};
    var configGame=new LibraryService(configPaths,new EventLog(configPaths)).Import(disc);
    var configPlan=Adapters.Prepare(configPaths,configSettings,configGame);var configInfo=configPlan.StartInfo();configInfo.ArgumentList.Clear();
    foreach(var arg in new[]{"-testconfig","-datapath",Path.GetDirectoryName(configPaths.Engine(ConsoleKind.PS2))!,"-logfile",Path.Combine(configPaths.Root,"pcsx2-config.log")})configInfo.ArgumentList.Add(arg);
    using var configProcess=Process.Start(configInfo)!;
    try{await configProcess.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(15)).Token);}
    catch(OperationCanceledException){configProcess.Kill(true);Console.WriteLine("FAIL: PCSX2 configuration check timed out.");return 1;}
    Console.WriteLine("PCSX2 adapter-generated configuration exit: "+configProcess.ExitCode+". No BIOS/game boot attempted.");return configProcess.ExitCode;
}
if(args.Length==2 && args[0]=="--install")
{
    var installPaths=new AppPaths(args[1]);var installed=await EngineInstaller.InstallN64(installPaths);
    if(Directory.GetFiles(Path.GetDirectoryName(installed.Executable)!,"*",SearchOption.AllDirectories).Any(f=>ContentDetection.Extensions.Contains(Path.GetExtension(f))))throw new Exception("Engine installer copied game content.");
    Console.WriteLine("PASS: pinned official download installed, executable hashed, game content excluded.");return 0;
}
if(args.Length<3){Console.WriteLine("Usage: EngineProbe <engine.exe> <legal-test-rom> <data-directory>");return 2;}
var paths=new AppPaths(args[2]);var settings=new Settings{Display=new(){Fullscreen=false}};
if(args.Contains("--goldeneye"))settings.Controller=Controllers.ForN64Preset(settings.Controller,"goldeneye-solitaire");
if(args.Contains("--tuned"))settings.Controller.ModernTuning=new(){HorizontalPercent=125,VerticalPercent=75,CameraDeadzone=6000,MovementThreshold=10000};
settings.Backends[ConsoleKind.N64]=new(){Executable=Path.GetFullPath(args[0]),Sha256=Adapters.HashExecutable(args[0])};
var game=new LibraryService(paths,new EventLog(paths)).Import(args[1],ConsoleKind.N64);
if(args.Contains("--fullhd")){settings.Display.Fullscreen=true;settings.Display.Profile=GraphicsProfile.Custom;settings.Display.Stretch=true;}
var plan=Adapters.Prepare(paths,settings,game,n64VideoPlugin:args.Contains("--glide")?"glide64mk2":"",outputSize:args.Contains("--fullhd")?(1920,1080):null);plan.Arguments.InsertRange(plan.Arguments.Count-1,["--testshots",args.Contains("--long")?"300,600":"10,60"]);
var info=plan.StartInfo();info.RedirectStandardOutput=true;info.RedirectStandardError=true;
using var process=Process.Start(info)!;
var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();
try{await process.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(40)).Token);}
catch(OperationCanceledException){process.Kill(true);await process.WaitForExitAsync();Console.WriteLine("TIMEOUT: engine did not finish test frames.");}
var report=await stdout+"\n"+await stderr;File.WriteAllText(Path.Combine(paths.Root,"engine-probe.log"),report);Console.WriteLine(report);Console.WriteLine("Exit: "+process.ExitCode);
var screenshots=Directory.GetFiles(paths.Engine(ConsoleKind.N64),"*.png",SearchOption.AllDirectories);
Console.WriteLine("Screenshots: "+screenshots.Length);
return process.ExitCode==0 && screenshots.Length>=1?0:1;
