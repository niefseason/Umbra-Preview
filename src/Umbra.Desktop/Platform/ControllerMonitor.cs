using System.Runtime.InteropServices;
namespace Umbra.Desktop.Platform;
public static class ControllerMonitor
{
    [StructLayout(LayoutKind.Sequential)] public struct Gamepad { public ushort Buttons; public byte LeftTrigger,RightTrigger; public short LX,LY,RX,RY; }
    [StructLayout(LayoutKind.Sequential)] public struct State { public uint Packet; public Gamepad Pad; }
    [DllImport("xinput1_4.dll",EntryPoint="XInputGetState")] static extern uint GetState(uint index,out State state);
    public static List<(int Index,State State)> Connected()
    {
        var result=new List<(int,State)>();
        try { for(uint i=0;i<4;i++) if(GetState(i,out var state)==0) result.Add(((int)i,state)); }
        catch(DllNotFoundException) { }
        return result;
    }
}
