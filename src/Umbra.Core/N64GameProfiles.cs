using System.Text;
namespace Umbra.Core;

public static class N64GameProfiles
{
    // Header identity, not a filename/title guess or gameplay compatibility claim.
    public static string HeaderName(string path)
    {
        using var stream=File.OpenRead(path);
        var header=new byte[64];
        if(stream.Read(header)!=header.Length)return "";
        if(header[0]==0x37 && header[1]==0x80)
            for(int i=0;i<64;i+=2)(header[i],header[i+1])=(header[i+1],header[i]);
        else if(header[0]==0x40 && header[1]==0x12)
            for(int i=0;i<64;i+=4)Array.Reverse(header,i,4);
        if(!header.AsSpan(0,4).SequenceEqual(new byte[]{0x80,0x37,0x12,0x40}))return "";
        return Encoding.ASCII.GetString(header,32,20).Trim(' ','\0');
    }
    public static string Resolve(string preset,string path)
    {
        if(preset!="auto-modern")return preset;
        return HeaderName(path).ToUpperInvariant() switch
        {
            "GOLDENEYE" or "PERFECT DARK"=>"modern-12",
            _=>throw new UserError("unsupported-modern-profile","No modern control profile is known for this game yet. Choose its native controls; automatic mode will not guess camera/action bindings.")
        };
    }
}
