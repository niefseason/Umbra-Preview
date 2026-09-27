using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Umbra.Core;
namespace Umbra.Desktop;
public sealed partial class MainWindow
{
    async Task RuntimeProbe()
    {
        // Explicit opt-in, isolated data required. Exercises the same import and launch methods
        // as the UI; it does not claim that file-picker or physical input interactions passed.
        try
        {
            var args=Environment.GetCommandLineArgs();var index=Array.IndexOf(args,"--runtime-probe");
            if(!args.Contains("--data") || index+1>=args.Length)throw new InvalidOperationException("Runtime probe requires --data and a lawful test file.");
            var imported=library.Import(args[index+1],args.Contains("--gamecube")?ConsoleKind.GameCube:args.Contains("--ps2")?ConsoleKind.PS2:null);LibraryService.Merge(games,[imported]);library.Save(games);
            var game=games.First(g=>g.ContentId==imported.ContentId);
            var run=RunGame(game);
            if(await Task.WhenAny(run,Task.Delay(20000))!=run){session.RequestStop();if(await Task.WhenAny(run,Task.Delay(5000))!=run)session.ForceStop();}
            await run;
            File.WriteAllText(Path.Combine(paths.Root,"runtime-probe.txt"),"Import and session completed. Consult backend exit log; no physical input, audible sound or gameplay claim.\n");
            Close();
        }
        catch(Exception e){File.WriteAllText(Path.Combine(paths.Root,"runtime-probe-error.txt"),e.ToString());session.ForceStop();Close();}
    }
    async Task Smoke()
    {
        try
        {
            var results=new List<string>();
            results.Add(Umbra.Desktop.Platform.StartupWindowPlacement.Verify(this));
            foreach(var target in new[]{"Home","Library","Controllers","Settings","System Status"})
            {
                Navigate(target); await Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);UpdateLayout();
                if(content.Content is not StackPanel stack || stack.Children.Count==0)throw new InvalidOperationException("Empty view: "+target);
                var surface=(FrameworkElement)Content;
                var bitmap=new RenderTargetBitmap((int)surface.ActualWidth,(int)surface.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(surface);
                var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var file=File.Create(Path.Combine(paths.Root,"smoke-"+target.Replace(" ","-")+".png")))encoder.Save(file);
                results.Add(target+": constructed and rendered");
            }
            var sample=new Game{Title="Homebrew sample · interface test",Path=Path.Combine(paths.Root,"missing.z64"),Platform=ConsoleKind.N64};ShowGame(sample);UpdateLayout();results.Add("Game details: constructed");ShowStickTuning(sample,true);if(!File.Exists(Path.Combine(paths.Root,"smoke-stick-tuning.png")))throw new InvalidOperationException("Stick tuning did not render.");results.Add("Stick tuning dialog: rendered and closed");
            ShowGame(new Game{Title="PS2 presentation interface test",Path=Path.Combine(paths.Root,"missing.iso"),Platform=ConsoleKind.PS2});UpdateLayout();results.Add("PS2 sound and presentation controls: constructed");
            Navigate("Xbox 360");UpdateLayout();ShowGame(new Game{Title="Xbox 360 interface fixture",Path=Path.Combine(paths.Root,"missing.xex"),Platform=ConsoleKind.Xbox360});UpdateLayout();results.Add("Xbox 360 library and capability-limited details: constructed");
            File.WriteAllLines(Path.Combine(paths.Root,"smoke-results.txt"),results);Environment.Exit(0);
        }
        catch(Exception e){File.WriteAllText(Path.Combine(paths.Root,"smoke-error.txt"),e.ToString());Environment.Exit(1);}
    }
}
