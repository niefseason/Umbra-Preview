using System.Text.RegularExpressions;
namespace Umbra.Core;

// GoldenEye 007, in-game scheme 1.2 Solitaire. No ROM/save patching.
public static class GoldenEyeControls
{
    static readonly string[] Native = ["A Button","B Button","Start","Z Trig","L Trig","R Trig",
        "X Axis","Y Axis","C Button L","C Button R","C Button U","C Button D",
        "DPad U","DPad D","DPad L","DPad R","Mempak switch","Rumblepak switch",
        "X Button","Y Button","C Button X Axis","C Button Y Axis","Start Button"];
    public static Dictionary<string,string> EmptyBindings()=>Native.ToDictionary(k=>k,_=>"");

    public static Dictionary<string,string> Translate(string mapping,bool invertLook=false,bool reloadOnWest=false,bool weaponOnLeft=false,int movementThreshold=8000)
    {
        new ModernStickTuning{MovementThreshold=movementThreshold}.Validate();
        if(string.IsNullOrWhiteSpace(mapping) || mapping.Length>8192 || mapping.Any(char.IsControl))
            throw Missing("device mapping");
        var fields=mapping.Split(',');
        if(fields.Length<3)throw Missing("device mapping");
        var inputs=new Dictionary<string,string>(StringComparer.Ordinal);
        foreach(var field in fields.Skip(2))
        {
            var parts=field.Split(':',2);
            if(parts.Length==2)inputs[parts[0]]=parts[1];
        }
        string Get(string key)=>inputs.TryGetValue(key,out var value)&&value.Length>0?value:throw Missing(key);
        string Button(string key,bool optional=false)
        {
            if(optional&&!inputs.ContainsKey(key))return "";
            var value=Get(key);
            var button=Regex.Match(value,@"^b(\d{1,3})$");
            if(button.Success)return $"button({button.Groups[1].Value})";
            var axis=Regex.Match(value,@"^([+-]?)a(\d{1,3})(~?)$");
            if(axis.Success)
            {
                var negative=(axis.Groups[1].Value=="-") ^ (axis.Groups[3].Value=="~");
                return $"axis({axis.Groups[2].Value}{(negative?"-":"+")},16000)";
            }
            var hat=Regex.Match(value,@"^h(\d{1,3})\.([1248])$");
            if(hat.Success)
                return $"hat({hat.Groups[1].Value} {hat.Groups[2].Value switch {"1"=>"Up","2"=>"Right","4"=>"Down",_=>"Left"}})";
            throw Missing(key);
        }
        (string Index,bool Inverted) Axis(string key)
        {
            // Split/half axes cannot provide an independent full camera stick.
            var match=Regex.Match(Get(key),@"^a(\d{1,3})(~?)$");
            if(!match.Success)throw Missing(key+" (full axis required)");
            return (match.Groups[1].Value,match.Groups[2].Value=="~");
        }
        string Stick(string key,bool invert=false)
        {
            var (index,flipped)=Axis(key);
            return flipped ^ invert ? $"axis({index}+,{index}-)" : $"axis({index}-,{index}+)";
        }
        string Direction(string key,bool positive)
        {
            var (index,flipped)=Axis(key);
            return $"axis({index}{(positive ^ flipped?"+":"-")},{movementThreshold})";
        }
        // Mupen SDL parses only the first binding of each input type. Never
        // emit duplicate button()/axis()/hat() clauses that look like an OR.
        string Join(params string[] expressions)=>string.Join(" ",expressions.Where(x=>x.Length>0)
            .GroupBy(x=>x[..x.IndexOf('(')]).Select(group=>group.First()));
        var result=EmptyBindings();
        result["X Axis"]=Stick("rightx");
        result["Y Axis"]=Stick("righty",invertLook);
        result["C Button L"]=Join(Direction("leftx",false),Button("dpleft",true));
        result["C Button R"]=Join(Direction("leftx",true),Button("dpright",true));
        result["C Button U"]=Join(Direction("lefty",false),Button("dpup",true));
        result["C Button D"]=Join(Direction("lefty",true),Button("dpdown",true));
        result["R Trig"]=Button("lefttrigger");
        result["Z Trig"]=Button("righttrigger");
        // Native B combines context-sensitive interaction and reloading.
        result["B Button"]=Button(reloadOnWest?"x":"a");
        // One weapon bumper; hold it and tap RT for reverse.
        result["A Button"]=Button(weaponOnLeft?"leftshoulder":"rightshoulder");
        result["Start"]=Button("start");
        return result;
    }
    static UserError Missing(string input)=>new("goldeneye-device",
        $"This controller lacks a usable SDL mapping for {input}. The modern 1.2 profile needs two independent sticks, triggers, face buttons and bumpers. Configure an SDL controller mapping or choose another controller; no guessed bindings were applied.");
}
