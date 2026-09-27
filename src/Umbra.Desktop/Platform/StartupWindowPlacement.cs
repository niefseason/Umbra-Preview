using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Umbra.Core.Emulation;
namespace Umbra.Desktop.Platform;

// Work-area coordinates are physical pixels; WPF dimensions are device-independent.
// Apply once before the first frame, without resizing a running game or changing DPI.
static class StartupWindowPlacement
{
    public static void Apply(Window window)
    {
        var handle=new WindowInteropHelper(window).Handle;
        var info=new MonitorInfo{Size=Marshal.SizeOf<MonitorInfo>()};
        if(!GetMonitorInfo(MonitorFromWindow(handle,2),ref info))return;
        var dpi=GetDpiForWindow(handle);var scale=(dpi==0?96:dpi)/96.0;
        var work=info.Work;
        var size=DisplayManager.StartupSize(work.Right-work.Left,work.Bottom-work.Top,scale);
        window.MinWidth=Math.Min(980,size.Width/scale);
        window.MinHeight=Math.Min(680,size.Height/scale);
        window.Width=size.Width/scale;window.Height=size.Height/scale;
        window.Left=(work.Left+(work.Right-work.Left-size.Width)/2)/scale;
        window.Top=(work.Top+(work.Bottom-work.Top-size.Height)/2)/scale;
        SetWindowPos(handle,IntPtr.Zero,work.Left+(work.Right-work.Left-size.Width)/2,
            work.Top+(work.Bottom-work.Top-size.Height)/2,size.Width,size.Height,0x14);
    }
    public static string Verify(Window window)
    {
        var handle=new WindowInteropHelper(window).Handle;
        var info=new MonitorInfo{Size=Marshal.SizeOf<MonitorInfo>()};
        if(!GetMonitorInfo(MonitorFromWindow(handle,2),ref info)||!GetWindowRect(handle,out var rect))
            throw new InvalidOperationException("Cannot inspect startup bounds.");
        var work=info.Work;
        if(rect.Left<work.Left || rect.Top<work.Top || rect.Right>work.Right || rect.Bottom>work.Bottom)
            throw new InvalidOperationException($"Startup window {rect.Left},{rect.Top},{rect.Right},{rect.Bottom} exceeds work area {work.Left},{work.Top},{work.Right},{work.Bottom} at DPI {GetDpiForWindow(handle)}.");
        return $"Startup bounds: {rect.Left},{rect.Top} {rect.Right-rect.Left}x{rect.Bottom-rect.Top}; work area: {work.Left},{work.Top} {work.Right-work.Left}x{work.Bottom-work.Top}; DPI: {GetDpiForWindow(handle)}; contained";
    }
    [StructLayout(LayoutKind.Sequential)] struct Rect{public int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)] struct MonitorInfo{public int Size;public Rect Monitor,Work;public uint Flags;}
    [DllImport("user32.dll")] static extern IntPtr MonitorFromWindow(IntPtr window,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
    [DllImport("user32.dll")] static extern uint GetDpiForWindow(IntPtr window);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr window,out Rect rect);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
}
