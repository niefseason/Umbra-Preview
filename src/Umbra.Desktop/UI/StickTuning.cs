using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Umbra.Core;
using Umbra.Desktop.Platform;
namespace Umbra.Desktop;

public sealed partial class MainWindow
{
    void ShowStickTuning(Game game,bool inspect=false)=>Safe(()=>
    {
        var saved=JsonStore.Read(paths.Override(game.Id),()=>new GameOverride());
        var tuning=saved.StickTuning??new ModernStickTuning();
        var baseController=saved.SavedController??settings.Controller;
        var speeds=new[]{50,75,100,125,150,175,200,tuning.HorizontalPercent,tuning.VerticalPercent}.Distinct().Order().ToArray();
        var thresholds=new[]{2000,4000,6000,8000,10000,12000,16000,20000,24000,tuning.MovementThreshold}.Distinct().Order().ToArray();
        var deadzones=new[]{0,2000,4096,6000,8192,12000,16000,20000,24000,tuning.CameraDeadzone??baseController.N64AnalogDeadzone}.Distinct().Order().ToArray();
        var horizontal=UI.Choice(speeds.Select(x=>$"{x}%"),Array.IndexOf(speeds,tuning.HorizontalPercent));
        var vertical=UI.Choice(speeds.Select(x=>$"{x}%"),Array.IndexOf(speeds,tuning.VerticalPercent));
        var movement=UI.Choice(thresholds.Select(x=>$"{x*100.0/32768:F0}% ({x})"),Array.IndexOf(thresholds,tuning.MovementThreshold));
        var camera=UI.Choice(deadzones.Select(x=>$"{x*100.0/32768:F0}% ({x})"),Array.IndexOf(deadzones,tuning.CameraDeadzone??baseController.N64AnalogDeadzone));
        var dialog=new Window{Owner=this,Title="Camera & stick tuning",Width=720,Height=680,WindowStartupLocation=WindowStartupLocation.Manual};
        dialog.SourceInitialized+=(_,_)=>StartupWindowPlacement.Apply(dialog);
        dialog.Loaded+=(_,_)=>StartupWindowPlacement.Apply(dialog);
        var live=UI.Text("Waiting for XInput readings…",13,"Muted");
        var measured=UI.Text("Leave both sticks untouched and start a 5-second check. Values are observations, not automatic calibration.",13,"Muted");
        DateTime? end=null;var peaks=new Dictionary<int,(int Left,int Right)>();var interrupted=false;
        var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(100)};
        int Magnitude(short x,short y)=>Math.Max(Math.Abs((int)x),Math.Abs((int)y));
        timer.Tick+=(_,_)=>
        {
            var pads=ControllerMonitor.Connected();
            live.Text=pads.Count==0?"No XInput controller connected. PlayStation/generic SDL live diagnostics are not available here.":string.Join("\n",pads.Select(p=>$"Pad {p.Index+1}: left {Magnitude(p.State.Pad.LX,p.State.Pad.LY)*100.0/32768:F1}% · right {Magnitude(p.State.Pad.RX,p.State.Pad.RY)*100.0/32768:F1}% · LT {p.State.Pad.LeftTrigger*100/255}% · RT {p.State.Pad.RightTrigger*100/255}%"));
            if(end is null)return;
            if(!pads.Select(p=>p.Index).Order().SequenceEqual(peaks.Keys.Order()))interrupted=true;
            foreach(var p in pads)
                if(peaks.TryGetValue(p.Index,out var prior))peaks[p.Index]=(Math.Max(prior.Left,Magnitude(p.State.Pad.LX,p.State.Pad.LY)),Math.Max(prior.Right,Magnitude(p.State.Pad.RX,p.State.Pad.RY)));
            if(DateTime.UtcNow<end){measured.Text=$"Keep sticks untouched… {Math.Max(0,(end.Value-DateTime.UtcNow).TotalSeconds):F1}s";return;}
            end=null;
            measured.Text=interrupted?"Controller connection changed. Reconnect and repeat the check.":string.Join("\n",peaks.Select(p=>$"Pad {p.Key+1}: observed resting maximum left {p.Value.Left}, right {p.Value.Right} (out of 32,768)."))+"\nIf a resting value reaches the chosen threshold, raise that threshold slightly and retest. Touching a stick invalidates this check. XInput readings do not verify SDL engine behavior.";
        };
        var body=UI.Column(UI.Heading("Modern shooter stick tuning",26),
            UI.Text("GoldenEye / Perfect Dark, in-game style 1.2. Changes apply on the next launch. 100% keeps the existing response. Below 100% reduces maximum turn speed; above 100% reaches maximum speed sooner. Movement remains digital eight-way.",13,"Muted"),
            UI.Text("Horizontal look sensitivity"),horizontal,UI.Text("Vertical look sensitivity"),vertical,
            UI.Text("Left-stick movement activation threshold"),movement,UI.Text("Right-stick camera deadzone"),camera,
            UI.Row(UI.Button("Save tuning",()=>Safe(()=>{
                if(session.Active || busy)throw new UserError("tuning-busy","Quit the game before changing its controls.");
                var current=JsonStore.Read(paths.Override(game.Id),()=>new GameOverride());
                if(current.ControllerPreset is not ("goldeneye-solitaire" or "modern-12" or "auto-modern"))throw new UserError("tuning-preset","Select a modern 1.2 game control preset before saving this tuning.");
                var next=new ModernStickTuning{HorizontalPercent=speeds[horizontal.SelectedIndex],VerticalPercent=speeds[vertical.SelectedIndex],MovementThreshold=thresholds[movement.SelectedIndex],CameraDeadzone=deadzones[camera.SelectedIndex]};next.Validate();
                current.StickTuning=next;JsonStore.Write(paths.Override(game.Id),current);dialog.Close();ShowGame(game);Notice("Stick tuning saved for the next launch. Your saved working profile is still available to restore.");
            }),true),UI.Button("Reset tuning",()=>Safe(()=>{
                if(session.Active || busy)throw new UserError("tuning-busy","Quit the game first.");
                var current=JsonStore.Read(paths.Override(game.Id),()=>new GameOverride());current.StickTuning=null;JsonStore.Write(paths.Override(game.Id),current);dialog.Close();ShowGame(game);Notice("Default sensitivity and movement threshold restored; camera deadzone follows the base controller profile.");
            }))),UI.Heading("Live Xbox / XInput drift check",21),live,measured,
            UI.Button("Start 5-second neutral check",()=>{
                peaks.Clear();foreach(var p in ControllerMonitor.Connected())peaks[p.Index]=(0,0);
                if(peaks.Count==0){measured.Text="Connect an XInput controller first.";return;}
                interrupted=false;end=DateTime.UtcNow.AddSeconds(5);
            }),UI.Button("Close",()=>dialog.Close()));
        dialog.Content=new ScrollViewer{Content=new Border{Padding=new Thickness(24),Child=body}};
        if(inspect)dialog.ContentRendered+=(_,_)=>{
            dialog.UpdateLayout();
            var surface=(FrameworkElement)dialog.Content;
            var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap((int)surface.ActualWidth,(int)surface.ActualHeight,96,96,System.Windows.Media.PixelFormats.Pbgra32);bitmap.Render(surface);
            var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
            using(var file=System.IO.File.Create(System.IO.Path.Combine(paths.Root,"smoke-stick-tuning.png")))encoder.Save(file);
            dialog.Close();
        };
        dialog.Closed+=(_,_)=>timer.Stop();timer.Start();dialog.ShowDialog();
    });
}
