using System.Runtime.InteropServices;
namespace Umbra.Core;

// Read the same SDL joystick ordering and mapping database used by Mupen.
// No virtual driver, synthetic input, hardware calibration or global remap.
public static class SdlControllerDiscovery
{
    static readonly object Gate=new();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Init(uint flags);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Quit(uint flags);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Count();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr AtIndex(int index);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void Free(IntPtr pointer);
    public static List<SdlDeviceProfile> Read(string library)
    {
        if(!OperatingSystem.IsWindows() || !File.Exists(library))
            throw new UserError("sdl-missing","GoldenEye controller detection needs SDL2.dll beside the selected N64 engine.");
        lock(Gate)
        {
            var module=NativeLibrary.Load(Path.GetFullPath(library));
            T Function<T>(string name) where T:Delegate=>Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(module,name));
            const uint flags=0x00002000; // SDL_INIT_GAMECONTROLLER (includes joystick)
            bool initialized=false;
            try
            {
                if(Function<Init>("SDL_InitSubSystem")(flags)!=0)
                    throw new UserError("sdl-init","SDL could not detect controllers. Reconnect the controller and try again.");
                initialized=true;
                var count=Function<Count>("SDL_NumJoysticks")();
                var mappingAt=Function<AtIndex>("SDL_GameControllerMappingForDeviceIndex");
                var nameAt=Function<AtIndex>("SDL_JoystickNameForIndex");
                var free=Function<Free>("SDL_free");
                var result=new List<SdlDeviceProfile>();
                for(int index=0;index<count;index++)
                {
                    var pointer=mappingAt(index);
                    try
                    {
                        var mapping=pointer==IntPtr.Zero?"":Marshal.PtrToStringUTF8(pointer)??"";
                        result.Add(new(){DeviceIndex=index,Name=Marshal.PtrToStringUTF8(nameAt(index))??"SDL joystick",
                            Guid=mapping.Split(',')[0],StandardMapping=mapping});
                    }
                    finally{if(pointer!=IntPtr.Zero)free(pointer);}
                }
                return result;
            }
            finally
            {
                if(initialized)Function<Quit>("SDL_QuitSubSystem")(flags);
                NativeLibrary.Free(module);
            }
        }
    }
}
