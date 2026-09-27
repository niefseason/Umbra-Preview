using System.Windows;
using System.Windows.Controls;
using Umbra.Core;
using Umbra.Desktop.Platform;
namespace Umbra.Desktop;
public sealed partial class MainWindow
{
    void ShowWizard()
    {
        int step=0;var dialog=new Window{Owner=this,Title="Welcome to "+App.BrandName,Width=760,Height=620,MinWidth=640,MinHeight=500,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        var host=new ContentControl();dialog.Content=new ScrollViewer{Content=new Border{Padding=new Thickness(38),Child=host}};
        var titles=new[]{"Meet your new collection.","Give your games a home.","Bring your PlayStation 2 BIOS.","Connect. Get comfortable.","Choose your presentation.","A quick systems check.","Your collection awaits."};
        void Render()
        {
            var body=UI.Column(UI.Text($"SETUP   /   {step+1:00} OF 07",11,"Accent"),UI.Heading(titles[step],30));
            switch(step)
            {
                case 0:body.Children.Add(UI.Eclipse(150));body.Children.Add(UI.Text("One library for PlayStation 2, GameCube, Nintendo 64 and Xbox 360. Import games and system files you have the right to use. Your original files stay in place.",16,"Muted"));break;
                case 1:body.Children.Add(UI.Text($"{settings.LibraryFolders.Count} library folder(s) selected. You can add individual games any time. Compressed containers may need a console selection.",15,"Muted"));body.Children.Add(UI.Button("Choose library folder",()=>{AddFolder();Render();}));break;
                case 2:body.Children.Add(UI.Text(string.IsNullOrEmpty(settings.BiosPath)?"PS2 requires a BIOS dump from your own console. You may skip this and use the other systems first.":"A BIOS file is selected. Final validation happens in PCSX2.",15,"Muted"));body.Children.Add(UI.Row(UI.Button("Select my BIOS",()=>{SelectBios();Render();}),UI.Button("Official BIOS guide",()=>OpenWebsite("https://pcsx2.net/docs/setup/bios/"))));break;
                case 3:body.Children.Add(UI.Text($"{ControllerMonitor.Connected().Count} XInput controller(s) connected. Xbox-style controls receive sensible defaults. Connect a controller and press Refresh to check again.",15,"Muted"));body.Children.Add(UI.Button("Refresh controller check",Render));body.Children.Add(UI.Text("Other SDL devices and manual N64 remaps still need runtime validation in this preview.",13,"Muted"));break;
                case 4:
                    var choice=UI.Choice(["Original Hardware","Clean Original","Enhanced"],Math.Min(2,(int)settings.Display.Profile));choice.SelectionChanged+=(_,_)=>Safe(()=>{settings.Display.Profile=(GraphicsProfile)choice.SelectedIndex;settings.Display.Stretch=false;SaveSettings();});body.Children.Add(choice);body.Children.Add(UI.Text("Original keeps native resolution and original speed. Clean Original currently uses the same native settings. Enhanced uses 3× resolution. Widescreen patches and texture packs are never enabled automatically.",15,"Muted"));break;
                case 5:
                    foreach(var issue in Diagnostics.Check(paths,settings,ControllerMonitor.Connected().Count))body.Children.Add(UI.Text($"{issue.State}  /  {issue.Component}\n{issue.Detail}",12,"Muted"));
                    body.Children.Add(UI.Text("Choose each engine in System Status after setup. Actual rendering and full-speed gameplay require a game test.",13,"Accent"));break;
                case 6:body.Children.Add(UI.Eclipse(150));body.Children.Add(UI.Text("Setup is saved. Add your games, select engines in System Status, then press Play. You can revisit setup from Settings at any time.",16,"Muted"));break;
            }
            body.Children.Add(UI.Row(UI.Button(step==0?"Set up later":"Back",()=>{if(step==0)dialog.Close();else{step--;Render();}}),UI.Button(step==6?"Enter UMBRA":"Continue  →",()=>Safe(()=>{if(step==6){settings.SetupComplete=true;SaveSettings();dialog.Close();Navigate("Home");}else{step++;Render();}}),true)));host.Content=body;
        }
        Render();dialog.ShowDialog();
    }
}
