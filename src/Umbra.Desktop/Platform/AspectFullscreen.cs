using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Umbra.Core.Emulation;
namespace Umbra.Desktop.Platform;

// Rice has no aspect-preserving fullscreen viewport. Keep its normal renderer/input/audio
// and surround a 4:3 borderless window with black, without changing the monitor video mode.
sealed class AspectFullscreen : IDisposable
{
    readonly GameSession session; readonly bool stretch;
    readonly Action<string> report;
    readonly Window backdrop=new(){Title="UMBRA · Original 4:3",Background=Brushes.Black,WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,ShowInTaskbar=false};
    readonly DispatcherTimer timer=new(){Interval=TimeSpan.FromMilliseconds(300)};
    IntPtr native,background; int originalStyle; Rect original; bool fullscreen=true,disposed;
    HwndSource? source;
    public static (int Width,int Height) MonitorSize(Window window)
    {
        var monitor=new MonitorInfo{Size=Marshal.SizeOf<MonitorInfo>()};
        if(!GetMonitorInfo(MonitorFromWindow(new WindowInteropHelper(window).Handle,2),ref monitor))
            throw new Umbra.Core.UserError("monitor","Could not determine the screen size. Try windowed mode.");
        return (monitor.Monitor.Right-monitor.Monitor.Left,monitor.Monitor.Bottom-monitor.Monitor.Top);
    }
    public AspectFullscreen(GameSession session,Action<string> report,bool stretch=false)
    {
        this.session=session;this.report=report;this.stretch=stretch;
        backdrop.Show();background=new WindowInteropHelper(backdrop).Handle;
        source=HwndSource.FromHwnd(background);source.AddHook(Hook);
        RegisterHotKey(background,533,0x4000,0x7A); // F11; registration can fail if another app owns it.
        timer.Tick+=(_,_)=>Apply();timer.Start();
    }
    IntPtr Hook(IntPtr hwnd,int message,IntPtr wparam,IntPtr lparam,ref bool handled)
    {
        if(message==0x312 && wparam.ToInt32()==533 && native!=IntPtr.Zero)
        {
            fullscreen=!fullscreen;
            if(fullscreen){backdrop.Show();Apply();SetWindowPos(native,IntPtr.Zero,0,0,0,0,0x13);SetForegroundWindow(native);}else{Restore();backdrop.Hide();}
            handled=true;
        }
        return IntPtr.Zero;
    }
    void Apply()
    {
        if(disposed || !fullscreen)return;
        var handle=session.WindowHandle;if(handle==IntPtr.Zero)return;
        bool first=native!=handle;
        if(first){native=handle;originalStyle=GetWindowLong(native,-16);GetWindowRect(native,out original);}
        var monitor=new MonitorInfo{Size=Marshal.SizeOf<MonitorInfo>()};
        if(!GetMonitorInfo(MonitorFromWindow(native,2),ref monitor))return;
        var area=monitor.Monitor;var fit=stretch ? (X:0,Y:0,Width:area.Right-area.Left,Height:area.Bottom-area.Top) : DisplayManager.Fit(area.Right-area.Left,area.Bottom-area.Top);
        var targetX=area.Left+fit.X; var targetY=area.Top+fit.Y;
        var targetW=fit.Width; var targetH=fit.Height;
        var changed=GetWindowRect(native,out var current) &&
            (current.Left!=targetX || current.Top!=targetY || current.Right-current.Left!=targetW || current.Bottom-current.Top!=targetH || GetWindowLong(native,-16)!=(originalStyle & ~0x00CF0000));
        if(!changed && !first)return;
        SetWindowPos(background,IntPtr.Zero,area.Left,area.Top,area.Right-area.Left,area.Bottom-area.Top,0x14);
        SetWindowLong(native,-16,originalStyle & ~0x00CF0000);
        SetWindowPos(native,IntPtr.Zero,targetX,targetY,targetW,targetH,0x34);
        if(first){SetWindowPos(native,IntPtr.Zero,area.Left+fit.X,area.Top+fit.Y,fit.Width,fit.Height,0x20);SetForegroundWindow(native);}
        if(first && GetWindowRect(native,out var actual))report($"monitor={area.Right-area.Left}x{area.Bottom-area.Top};viewport={actual.Right-actual.Left}x{actual.Bottom-actual.Top};offset={actual.Left-area.Left},{actual.Top-area.Top}");
    }
    void Restore()
    {
        if(native==IntPtr.Zero || !IsWindow(native))return;
        SetWindowLong(native,-16,originalStyle);
        SetWindowPos(native,IntPtr.Zero,original.Left,original.Top,original.Right-original.Left,original.Bottom-original.Top,0x34);
    }
    public void Dispose()
    {
        disposed=true;timer.Stop();Restore();UnregisterHotKey(background,533);source?.RemoveHook(Hook);backdrop.Close();
    }
    [StructLayout(LayoutKind.Sequential)] struct Rect{public int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)] struct MonitorInfo{public int Size;public Rect Monitor,Work;public uint Flags;}
    [DllImport("user32.dll")] static extern IntPtr MonitorFromWindow(IntPtr window,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
    [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr window,int index);
    [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr window,int index,int value);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr window,out Rect rect);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] static extern bool IsWindow(IntPtr window);
    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr window,int id,uint modifiers,uint key);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr window,int id);
}
