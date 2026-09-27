using System.Diagnostics;
using System.Runtime.InteropServices;
namespace Umbra.Core.Emulation;

public sealed record SessionResult(int ExitCode,double Seconds,bool Forced);
public sealed class GameSession(AppPaths paths,EventLog log) : IDisposable
{
    Process? process;
    OwnedJob? job;
    bool forced;
    public bool Active => process is not null;
    public string? ActiveGameId { get; private set; }
    public IntPtr WindowHandle { get { if(process is null || process.HasExited) return IntPtr.Zero; process.Refresh(); return process.MainWindowHandle; } }
    public event Action? Started;
    public async Task<SessionResult> Run(LaunchPlan plan,Game game)
    {
        if(Active) throw new UserError("already-playing","A game is already running. Return to it or quit it first.");
        var timer=Stopwatch.StartNew(); forced=false;
        using var launched=new Process { StartInfo=plan.StartInfo() };
        process=launched; ActiveGameId=game.Id;
        try
        {
            job=OperatingSystem.IsWindows()?new OwnedJob():null;
            if(!launched.Start()) throw new UserError("launch-failed","The console engine could not start. Check System Status.");
            try { job?.Attach(launched); }
            catch { if(!launched.HasExited) launched.Kill(true); throw; }
            JsonStore.Write(Path.Combine(paths.Root,"session.json"),new { gameId=game.Id,platform=game.Platform,pid=launched.Id,started=DateTimeOffset.UtcNow });
            log.Write("backend.launch",game.Platform.ToString()); Started?.Invoke();
            await launched.WaitForExitAsync();
            timer.Stop(); log.Write("backend.exit",$"{game.Platform};code={launched.ExitCode};forced={forced}");
            return new(launched.ExitCode,timer.Elapsed.TotalSeconds,forced);
        }
        finally
        {
            job?.Dispose(); job=null; process=null; ActiveGameId=null;
            var journal=Path.Combine(paths.Root,"session.json"); if(File.Exists(journal)) File.Delete(journal);
        }
    }
    public bool RequestStop()
    {
        if(process is null || process.HasExited) return true;
        process.Refresh(); return process.CloseMainWindow();
    }
    public void ForceStop() { if(process is not null && !process.HasExited) { forced=true; process.Kill(true); } }
    public void Dispose() { job?.Dispose(); job=null; }
}
// A Windows Job owns exactly this launch tree. The OS reaps it if the launcher crashes.
internal sealed class OwnedJob : IDisposable
{
    IntPtr handle;
    public OwnedJob()
    {
        handle=CreateJobObject(IntPtr.Zero,null);
        if(handle==IntPtr.Zero) throw new UserError("process-owner","Windows could not create a protected game session.");
        var info=new ExtendedLimit { Basic=new BasicLimit { Flags=0x2000 } };
        if(!SetInformationJobObject(handle,9,ref info,(uint)Marshal.SizeOf<ExtendedLimit>())) { Dispose(); throw new UserError("process-owner","Windows could not protect this game session. Restart UMBRA."); }
    }
    public void Attach(Process process)
    { if(!AssignProcessToJobObject(handle,process.Handle) && !process.HasExited) throw new UserError("process-owner","This Windows environment does not allow UMBRA to manage the game process safely."); }
    public void Dispose() { if(handle!=IntPtr.Zero) { CloseHandle(handle); handle=IntPtr.Zero; } }
    [StructLayout(LayoutKind.Sequential)] struct BasicLimit { public long PerProcess,PerJob; public uint Flags; public UIntPtr Minimum,Maximum; public uint Active; public UIntPtr Affinity; public uint Priority,Scheduling; }
    [StructLayout(LayoutKind.Sequential)] struct IoCounters { public ulong Read,Write,Other,ReadBytes,WriteBytes,OtherBytes; }
    [StructLayout(LayoutKind.Sequential)] struct ExtendedLimit { public BasicLimit Basic; public IoCounters Io; public UIntPtr ProcessMemory,JobMemory,PeakProcess,PeakJob; }
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern IntPtr CreateJobObject(IntPtr attributes,string? name);
    [DllImport("kernel32.dll")] [return:MarshalAs(UnmanagedType.Bool)] static extern bool SetInformationJobObject(IntPtr job,int infoClass,ref ExtendedLimit info,uint length);
    [DllImport("kernel32.dll")] [return:MarshalAs(UnmanagedType.Bool)] static extern bool AssignProcessToJobObject(IntPtr job,IntPtr process);
    [DllImport("kernel32.dll")] [return:MarshalAs(UnmanagedType.Bool)] static extern bool CloseHandle(IntPtr handle);
}
