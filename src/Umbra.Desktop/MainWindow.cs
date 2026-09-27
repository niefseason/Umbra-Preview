using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using Umbra.Core;
using Umbra.Core.Emulation;
using Umbra.Desktop.Platform;
namespace Umbra.Desktop;
public sealed partial class MainWindow : Window
{
    readonly AppPaths paths;
    readonly EventLog log;
    readonly LibraryService library;
    readonly SaveService saves;
    readonly GameSession session;
    Settings settings;
    List<Game> games;
    readonly ContentControl content=new();
    readonly TextBlock toast=UI.Text("",13,"Accent");
    readonly TextBlock controllerStatus=UI.Text("",12,"Muted");
    readonly TextBlock sessionStatus=UI.Text("",12,"Accent");
    readonly StackPanel navigation=new();
    readonly StackPanel sessionControls=new(){Visibility=Visibility.Collapsed};
    readonly DispatcherTimer controllerTimer=new(){Interval=TimeSpan.FromSeconds(2)};
    string page="Home";
    string query="";
    int sort;
    int previousControllers=-1;
    bool busy;
    readonly bool smoke;
    public MainWindow(AppPaths paths,bool smoke=false)
    {
        this.paths=paths; this.smoke=smoke;
        log=new(paths); library=new(paths,log); saves=new(paths,log); session=new(paths,log);
        var warnings=new List<string>(); settings=JsonStore.LoadSettings(paths.Config,warnings.Add); games=library.Load(warnings.Add);
        Title=App.BrandName+" · Your originals"; Width=1320; Height=880; MinWidth=980; MinHeight=680; WindowStartupLocation=WindowStartupLocation.Manual;
        SourceInitialized+=(_,_)=>StartupWindowPlacement.Apply(this);
        Background=UI.Brush("Bg");Foreground=UI.Brush("Ink");FontSize=14;
        FontFamily=new FontFamily(App.Brand.GetValueOrDefault("font","Segoe UI"));
        var root=new Grid{Background=UI.Brush("Bg")}; root.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(228)});root.ColumnDefinitions.Add(new ColumnDefinition());
        var sidebar=new DockPanel{Margin=new Thickness(25,28,20,20)};
        var logo=UI.Heading(App.Brand.GetValueOrDefault("logo","◐")+"  "+App.BrandName,25); DockPanel.SetDock(logo,Dock.Top); sidebar.Children.Add(logo);
        var label=UI.Text("THE ORIGINALS, TOGETHER",9,"Muted"); label.Margin=new Thickness(0,0,0,32);DockPanel.SetDock(label,Dock.Top);sidebar.Children.Add(label);
        sessionControls.Children.Add(UI.Button("Quit active game",()=>{session.RequestStop();Notice("Quit requested. Finish saving in the game, then confirm its exit if prompted.");}));
        sessionControls.Children.Add(UI.Button("Force stop…",()=>{if(session.Active && MessageBox.Show(this,"Force stop can lose unsaved progress. Your pre-launch backup is retained. Stop now?","Stop game",MessageBoxButton.YesNo)==MessageBoxResult.Yes)session.ForceStop();}));
        var foot=UI.Column(controllerStatus,sessionStatus,sessionControls,UI.Text("DEVELOPMENT PREVIEW  /  0.1",9,"Muted"));DockPanel.SetDock(foot,Dock.Bottom);sidebar.Children.Add(foot);
        foreach(var name in new[]{"Home","Library","PlayStation 2","GameCube","Nintendo 64","Xbox 360","Favorites","Controllers","Settings","System Status"})
        {
            var button=UI.Button(name,()=>Navigate(name));button.HorizontalContentAlignment=HorizontalAlignment.Left;button.Margin=new Thickness(0,0,0,6);button.Background=Brushes.Transparent;button.Tag=name;navigation.Children.Add(button);
            if(name=="Favorites") navigation.Children.Add(new Border{Height=1,Background=new SolidColorBrush(Color.FromRgb(43,45,49)),Margin=new Thickness(0,16,0,22)});
        }
        sidebar.Children.Add(new ScrollViewer{Content=navigation,VerticalScrollBarVisibility=ScrollBarVisibility.Auto}); root.Children.Add(sidebar);
        var body=new DockPanel{Margin=new Thickness(28,28,34,18)}; Grid.SetColumn(body,1);
        var top=new Grid{Margin=new Thickness(0,0,0,26)}; top.ColumnDefinitions.Add(new ColumnDefinition());top.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
        var breadcrumb=UI.Text("YOUR COLLECTION   /   YOUR WAY",10,"Muted"); breadcrumb.VerticalAlignment=VerticalAlignment.Center;top.Children.Add(breadcrumb);
        var actions=UI.Row(UI.Button("↻  Rescan",()=>SafeAsync(Rescan)),UI.Button("+  Add game",AddGame,true)); Grid.SetColumn(actions,1);top.Children.Add(actions);DockPanel.SetDock(top,Dock.Top);body.Children.Add(top);
        toast.Margin=new Thickness(0,14,0,0);DockPanel.SetDock(toast,Dock.Bottom);body.Children.Add(toast);body.Children.Add(new ScrollViewer{Content=content});root.Children.Add(body);Content=root;
        controllerTimer.Tick+=(_,_)=>UpdateControllers();controllerTimer.Start();UpdateControllers();
        session.Started+=()=>Dispatcher.Invoke(()=>{sessionStatus.Text="●  GAME IN PROGRESS";sessionControls.Visibility=Visibility.Visible; WindowState=WindowState.Minimized;});
        Closing+=(_,e)=> { if(session.Active || busy) { e.Cancel=true; Notice(session.Active?"Quit the running game before closing UMBRA. Native saves need time to finish writing.":"Wait for the current operation to finish before closing UMBRA."); } };
        Closed+=(_,_)=> {controllerTimer.Stop();session.Dispose();};
        PreviewKeyDown+=(_,e)=> {if(e.Key==Key.F11) {WindowState=WindowState==WindowState.Maximized?WindowState.Normal:WindowState.Maximized;e.Handled=true;} };
        Navigate("Home");log.Write("app.start");
        if(File.Exists(Path.Combine(paths.Root,"session.json"))) { warnings.Add("The previous session ended unexpectedly. Saves were retained; you can restore a pre-launch backup in Settings."); File.Move(Path.Combine(paths.Root,"session.json"),Path.Combine(paths.Root,"session-interrupted.json"),true); }
        if(warnings.Count>0) Notice(string.Join(" ",warnings));
        Loaded+=async(_,_)=> { StartupWindowPlacement.Apply(this); await Dispatcher.InvokeAsync(()=>{},DispatcherPriority.Loaded); if(smoke) await Smoke(); else if(Environment.GetCommandLineArgs().Contains("--runtime-probe")) await RuntimeProbe(); else if(!settings.SetupComplete) ShowWizard(); };
    }
    void UpdateControllers()
    {
        var count=ControllerMonitor.Connected().Count;controllerStatus.Text=count==0?"○  No XInput controller":$"●  {count} controller{(count==1?"":"s")} connected";
        if(count!=previousControllers) {log.Write("controller.change",count.ToString());previousControllers=count;if(page=="Controllers") RenderControllers();}
    }
    void Notice(string message)=>toast.Text=message;
    void SaveSettings() {JsonStore.Write(paths.Config,settings);log.Write("config.change");}
    void Safe(Action action)
    {
        try {action();} catch(Exception e) {Handle(e);}
    }
    async void SafeAsync(Func<Task> action)
    {
        if(busy) {Notice("Another operation is still finishing.");return;}
        busy=true;try {await action();} catch(Exception e) {Handle(e);}finally{busy=false;}
    }
    void Handle(Exception e)
    {
        log.Write("error",e is UserError u?u.Code:e.GetType().Name);
        Notice(e is UserError?e.Message:e is UnauthorizedAccessException?"Access was denied. Check the folder permissions.":e is IOException?"A file could not be read or written. Check that its drive is connected and has free space.":"This action could not finish. Check the selected file and try again. Diagnostic details are in the event log.");
    }
    void Navigate(string target)
    {
        page=target; foreach(var button in navigation.Children.OfType<Button>()) {button.Background=(string)button.Tag==target?UI.Brush("Panel"):Brushes.Transparent;button.Foreground=(string)button.Tag==target?UI.Brush("Accent"):UI.Brush("Muted");}
        switch(target) {case "Home":RenderHome();break;case "Controllers":RenderControllers();break;case "Settings":RenderSettings();break;case "System Status":RenderStatus();break;default:RenderLibrary();break;}
    }
    void OpenFolder(string path)=>Safe(()=> {if(!Directory.Exists(path)) throw new UserError("folder-missing","This folder is unavailable. Reconnect the drive or choose its new location.");System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path){UseShellExecute=true});});
    void OpenWebsite(string url)=>Safe(()=>System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url){UseShellExecute=true}));
    void AddGame()=>Safe(()=>
    {
        var picker=new OpenFileDialog{Title="Add your game backups",Multiselect=true,Filter="Game backups|*.z64;*.n64;*.v64;*.iso;*.gcm;*.rvz;*.gcz;*.ciso;*.chd;*.cso;*.bin;*.elf;*.dol;*.xex|All files|*.*"};
        if(picker.ShowDialog(this)!=true)return;
        int added=0;var errors=new List<string>();
        foreach(var file in picker.FileNames)
        {
            try {var platform=ContentDetection.Detect(file); if(platform is null) {platform=SelectPlatform(Path.GetFileName(file));if(platform is null)continue;} var game=library.Import(file,platform);added+=LibraryService.Merge(games,[game]);}
            catch(Exception e) {Handle(e);errors.Add(e is UserError?e.Message:"A selected file could not be imported. Check System Status and file access.");}
        }
        library.Save(games);Navigate(page);Notice($"Added {added} game(s). Original files remain in their current locations. "+string.Join(" ",errors.Distinct()));
    });
    ConsoleKind? SelectPlatform(string name)
    {
        ConsoleKind? chosen=null;var dialog=new Window{Owner=this,Title="Choose console",Width=450,Height=280,ResizeMode=ResizeMode.NoResize,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        var choice=UI.Choice(["PlayStation 2","GameCube","Nintendo 64","Xbox 360"]);
        dialog.Content=new Border{Padding=new Thickness(26),Child=UI.Column(UI.Heading("Which console?",23),UI.Text(name,13,"Muted"),choice,UI.Button("Add game",()=>{chosen=(ConsoleKind)choice.SelectedIndex;dialog.DialogResult=true;},true))};dialog.ShowDialog();return chosen;
    }
    void AddFolder()=>Safe(()=>
    {
        var picker=new OpenFolderDialog{Title="Choose a game library folder"};if(picker.ShowDialog(this)!=true)return;
        if(!settings.LibraryFolders.Contains(picker.FolderName,StringComparer.OrdinalIgnoreCase)) settings.LibraryFolders.Add(picker.FolderName);SaveSettings();SafeAsync(Rescan);
    });
    async Task Rescan()
    {
        Notice("Scanning your library…");var folders=settings.LibraryFolders.ToArray();var result=await Task.Run(()=>library.Scan(folders));var added=LibraryService.Merge(games,result.Games);library.Save(games);Navigate(page);
        Notice($"{added} new game(s). {result.Skipped} file(s) need manual import or could not be read. "+string.Join(" ",result.Warnings));
    }
    void SelectBios()=>Safe(()=>
    {
        var picker=new OpenFileDialog{Title="Select your PS2 BIOS dump",Filter="BIOS files|*.bin;*.rom|All files|*.*"};if(picker.ShowDialog(this)!=true)return;
        if(session.Active)throw new UserError("bios-busy","Quit the game before importing firmware.");
        ContentDetection.ValidateBios(picker.FileName);
        var folder=Path.Combine(paths.Root,"bios");Directory.CreateDirectory(folder);SaveService.RejectLinks(folder,paths.Root);
        var hash=Adapters.HashExecutable(picker.FileName);var destination=Path.Combine(folder,hash+".bin");
        if(File.Exists(destination))
        {SaveService.RejectLinks(destination,paths.Root);if(Adapters.HashExecutable(destination)!=hash)throw new UserError("bios-changed","The existing imported BIOS changed. Keep it for recovery and select another data folder.");}
        else File.Copy(picker.FileName,destination);
        settings.BiosPath=destination;SaveSettings();Notice("BIOS imported; your original file was retained. Structural screening passed. PS2 runtime verification is still required; PCSX2 must accept this dump when a game boots.");if(page=="Settings")RenderSettings();
    });
    void SelectBackend(ConsoleKind platform)=>Safe(()=>
    {
        var picker=new OpenFileDialog{Title="Select "+Adapters.Name(platform)+" executable",Filter="Application|*.exe"};if(picker.ShowDialog(this)!=true)return;
        var executable=picker.FileName;var version=System.Diagnostics.FileVersionInfo.GetVersionInfo(executable).ProductVersion??"Unknown version";
        settings.Backends[platform]=new(){Executable=executable,Version=version,Sha256=Adapters.HashExecutable(executable)};SaveSettings();RenderStatus();Notice("Engine selected. Runtime validation is still required; import a legal game and press Play.");
    });
}
