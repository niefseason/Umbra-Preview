namespace Umbra.Core.Emulation;
// Preserve unknown settings while changing only fields owned by the adapter.
public sealed class IniFile
{
    readonly Dictionary<string,Dictionary<string,string>> data = new(StringComparer.Ordinal);
    public static IniFile Load(string path)
    {
        var ini=new IniFile(); var section="";
        if (!File.Exists(path)) return ini;
        foreach(var raw in File.ReadLines(path))
        {
            var line=raw.Trim(); if (line.StartsWith(';') || line.StartsWith('#') || line.Length==0) continue;
            if (line.StartsWith('[') && line.EndsWith(']')) { section=line[1..^1]; continue; }
            var separator=line.IndexOf('='); if(separator>0) ini.Set(section,line[..separator].Trim(),line[(separator+1)..].Trim());
        }
        return ini;
    }
    public void Set(string section, string key, object value)
    {
        var text = value is bool b ? (b ? "True":"False") : Convert.ToString(value,System.Globalization.CultureInfo.InvariantCulture) ?? "";
        if ((section+key+text).IndexOfAny(['\r','\n','\0'])>=0) throw new UserError("unsafe-config", "A setting contains invalid characters.");
        if(!data.ContainsKey(section)) data[section]=new(StringComparer.Ordinal);
        data[section][key]=text;
    }
    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var text=string.Join("\n\n",data.Select(section=>$"[{section.Key}]\n"+string.Join("\n",section.Value.Select(pair=>$"{pair.Key} = {pair.Value}"))))+"\n";
        var temp=path+".tmp";
        File.WriteAllText(temp,text);
        if(File.Exists(path)) File.Copy(path,path+".bak",true);
        File.Move(temp,path,true);
    }
}
