using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using Umbra.Core;
namespace Umbra.Desktop;
public partial class App : Application
{
    Mutex? mutex;
    public static Dictionary<string,string> Brand { get; private set; }=new();
    public static string BrandName=>Brand.GetValueOrDefault("name","UMBRA");
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var dataIndex=Array.IndexOf(e.Args,"--data"); var data=dataIndex>=0 && dataIndex+1<e.Args.Length?e.Args[dataIndex+1]:null;
        var instanceName="Local\\Umbra-"+Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(data??Environment.UserName)))[..20];
        mutex=new Mutex(true,instanceName,out var created);
        if(!created) { MessageBox.Show("UMBRA is already open.","UMBRA"); Shutdown(); return; }
        DispatcherUnhandledException+=(_,args)=>{ MessageBox.Show("The action could not finish. Your original game files were not changed. Check storage permissions or reopen the application.",BrandName); args.Handled=true; };
        try
        {
            var branding=Path.Combine(AppContext.BaseDirectory,"config","branding.json");
            if(File.Exists(branding)) Brand=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(branding))??new();
            foreach(var pair in new[]{("Bg","background"),("Panel","panel"),("Ink","text"),("Muted","muted"),("Accent","accent")})
                if(Brand.TryGetValue(pair.Item2,out var color)) Resources[pair.Item1]=new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            var paths=new AppPaths(data); var window=new MainWindow(paths,e.Args.Contains("--smoke")); MainWindow=window; window.Show();
        }
        catch(Exception ex) { MessageBox.Show(ex is UserError?ex.Message:"UMBRA could not open its data folder. Check available space and folder permissions.",BrandName); Shutdown(1); }
    }
    protected override void OnExit(ExitEventArgs e) { mutex?.Dispose(); base.OnExit(e); }
}
