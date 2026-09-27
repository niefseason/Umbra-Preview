namespace Umbra.Core;
public static class Controllers
{
    public static readonly string[] Allowed = ["A","B","X","Y","Start","Back","LeftShoulder","RightShoulder"];
    public static Dictionary<string,string> Translate(ConsoleKind platform, ControllerProfile profile)
    {
        if(platform==ConsoleKind.Xbox360)return new(); // Xenia owns its native XInput mapping.
        if (profile.Players is < 1 or > 4) throw new UserError("players", "Choose one to four players.");
        var labels = platform switch
        {
            ConsoleKind.PS2 => new[]{"Cross","Circle","Square","Triangle","Start","Select","L1","R1"},
            ConsoleKind.GameCube => new[]{"A","B","X","Y","Start","Z","L","R"},
            _ => new[]{"A","B","C Left","C Up","Start","Z Trig","L Trig","R Trig"}
        };
        var logical = new[]{"South","East","West","North","Start","Select","L","R"};
        var result = new Dictionary<string,string>();
        for (int i=0;i<logical.Length;i++)
        {
            if (!profile.Buttons.TryGetValue(logical[i],out var button) || !Allowed.Contains(button)) throw new UserError("mapping", "Controller mapping contains an unsupported button. Reset the mapping in Controllers.");
            result[labels[i]]=button;
        }
        return result;
    }
    public static int SdlButton(string button) => button switch { "A"=>0,"B"=>2,"X"=>3,"Y"=>1,"Back"=>6,"Start"=>7,"LeftShoulder"=>4,"RightShoulder"=>5,_=>throw new UserError("mapping","Unknown button.") };
    // PCSX2's SDL settings use positions, not the labels printed on a pad.
    public static string PcsxButton(string button) => button switch
    {
        "A"=>"FaceSouth", "B"=>"FaceEast", "X"=>"FaceWest", "Y"=>"FaceNorth",
        "LeftShoulder" or "RightShoulder" or "Back" or "Start"=>button,
        _=>throw new UserError("mapping","Unsupported PCSX2 controller button.")
    };
    public static ControllerProfile ForN64Preset(ControllerProfile profile, string? preset)
    {
        var result=new ControllerProfile
        {
            Players=profile.Players,
            XboxActionLayout=profile.XboxActionLayout,
            N64RightStickCButtons=profile.N64RightStickCButtons,
            N64AnalogDeadzone=profile.N64AnalogDeadzone,
            GoldenEyeInvertLook=profile.GoldenEyeInvertLook,
            GoldenEyeReloadOnWest=profile.GoldenEyeReloadOnWest,
            GoldenEyeWeaponOnLeft=profile.GoldenEyeWeaponOnLeft,
            ModernTuning=profile.ModernTuning??new(),
            Buttons=new(profile.Buttons),
            SdlDevices=new(profile.SdlDevices)
        };
        switch(preset)
        {
            case null:
            case "":
                return result;
            case "classic":
                result.XboxActionLayout=false;
                result.N64RightStickCButtons=false;
                return result;
            case "goldeneye-solitaire":
            case "modern-12":
                result.GoldenEyeSolitaire=true;
                result.XboxActionLayout=false;
                result.N64RightStickCButtons=false;
                return result;
            case "modern-shooter":
                result.XboxActionLayout=true;
                result.N64RightStickCButtons=true;
                return result;
            case "no-c":
                result.XboxActionLayout=true;
                result.N64RightStickCButtons=false;
                return result;
            default:
                throw new UserError("controller-preset","Unknown N64 controller preset.");
        }
    }
    public static SdlDeviceProfile DefaultSdlDevice(int player=0) => new()
    {
        Name = "Generic SDL gamepad", Inputs = new Dictionary<string,string>
        {
            // SDL's Xbox controller layout uses the standard face buttons, but the
            // analog triggers are axes and the SDL2 joystick Start button is 7.
            // Writing trigger(4) as a digital button causes phantom Z presses.
            ["A Button"]="button(0)",["B Button"]="button(2)",["X Button"]="button(3)",["Y Button"]="button(1)",["Start"]="button(7)",["Z Trig"]="axis(4+)",
            ["L Trig"]="button(4)",["R Trig"]="button(5)",
            // Official Xbox SDL layout: the left stick is axes 0/1 and the
            // right stick is axes 2/3. Keep movement and C-actions separate.
            ["X Axis"]="axis(0-,0+)",["Y Axis"]="axis(1-,1+)",
            ["C Button R"]="axis(2+,24000)",["C Button L"]="axis(2-,24000) button(3)",["C Button D"]="axis(3+,24000) button(1)",["C Button U"]="axis(3-,24000)",["DPad U"]="hat(0 Up)",["DPad D"]="hat(0 Down)",["DPad L"]="hat(0 Left)",["DPad R"]="hat(0 Right)"
        }
    };
    public static Dictionary<string,string> TranslateN64Sdl(SdlDeviceProfile device, ControllerProfile? profile=null)
    {
        if(profile?.GoldenEyeSolitaire==true)
            return GoldenEyeControls.Translate(device.StandardMapping,profile.GoldenEyeInvertLook,profile.GoldenEyeReloadOnWest,profile.GoldenEyeWeaponOnLeft,profile.ModernTuning.MovementThreshold);
        var map = new Dictionary<string,string>(DefaultSdlDevice().Inputs, StringComparer.OrdinalIgnoreCase);
        foreach(var pair in device.Inputs) if(!string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value) && pair.Key.Length<64 && pair.Value.Length<128 && !pair.Value.Contains('"')) map[pair.Key]=pair.Value;
        // Apply the user's logical profile to the native N64 control names. The SDL
        // expression remains device-specific, while the logical button choice selects
        // which physical input supplies A/B/C/Start/Z/L/R.
        if(profile?.XboxActionLayout == true)
        {
            // Official Mupen64Plus SDL Xbox layout: left stick = axes 0/1,
            // right stick = axes 2/3, LT/RT = axes 4/5.
            map["X Axis"]="axis(0-,0+)"; map["Y Axis"]="axis(1-,1+)";
            if(profile.N64RightStickCButtons)
            {
                map["C Button R"]="axis(2+,24000)"; map["C Button L"]="axis(2-,24000)";
                map["C Button D"]="axis(3+,24000)"; map["C Button U"]="axis(3-,24000)";
            }
            else
            {
                map["C Button R"]=""; map["C Button L"]=""; map["C Button D"]=""; map["C Button U"]="";
                map["C Button X Axis"]=""; map["C Button Y Axis"]="";
            }
            map["A Button"]="button(0)"; map["B Button"]="axis(5+,24000) button(2)";
            map["Z Trig"]="axis(4+)"; map["L Trig"]="button(4)"; map["R Trig"]="button(5)";
            map["Start"]="button(7)";
        }
        else if(profile is not null)
        {
            var native=Translate(ConsoleKind.N64,profile);
            var target=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase)
            { ["A"]="A Button",["B"]="B Button",["C Left"]="C Button L",["C Up"]="C Button U",["Start"]="Start",["Z Trig"]="Z Trig",["L Trig"]="L Trig",["R Trig"]="R Trig" };
            // Resolve every physical expression from the original device map before
            // writing any destination key. This prevents A/B swaps from cascading.
            var physical = new Dictionary<string,string>(map, StringComparer.OrdinalIgnoreCase);
            foreach(var pair in native)
            {
                if(!target.TryGetValue(pair.Key,out var nativeKey)) continue;
                var physicalKey=pair.Value switch { "A"=>"A Button", "B"=>"B Button", "X"=>"X Button", "Y"=>"Y Button", "Start"=>"Start", "Back"=>"Z Trig", "LeftShoulder"=>"L Trig", "RightShoulder"=>"R Trig", _=>"" };
                if(physicalKey.Length>0 && physical.TryGetValue(physicalKey,out var expression)) map[nativeKey]=expression;
            }
            // Legacy/manual layouts do not own the C-stick axes. Clear every
            // C-direction key so an older generated config cannot leak actions.
            map["C Button R"]=""; map["C Button L"] = map.GetValueOrDefault("C Button L","");
            map["C Button D"]=""; map["C Button U"] = map.GetValueOrDefault("C Button U","");
            map["C Button X Axis"]=""; map["C Button Y Axis"]="";
        }
        return map;
    }
}
