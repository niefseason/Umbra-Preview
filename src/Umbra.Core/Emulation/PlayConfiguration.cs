using System.Xml;
using System.Xml.Linq;
namespace Umbra.Core.Emulation;

public static class PlayConfiguration
{
    // Play! 0.72 Windows provider IDs and native key IDs, verified against its source.
    const int XInputProvider=0x78696e70;
    public static void Configure(string root,ControllerProfile? controller,IReadOnlyList<int>? devices,string renderer="opengl")
    {
        if(renderer is not ("opengl" or "vulkan"))throw new UserError("play-renderer","Choose OpenGL or Vulkan for Play!.");
        if(devices is not null && (devices.Count>2 || devices.Distinct().Count()!=devices.Count || devices.Any(x=>x is <0 or >3)))
            throw new UserError("play-controller","Choose up to two distinct XInput devices.");
        var map=controller is null?null:Controllers.Translate(ConsoleKind.PS2,controller);
        var data=Path.Combine(root,"Play Data Files");
        var configFile=Path.Combine(data,"config.xml");
        SaveService.RejectLinks(configFile,root);
        var config=Read(configFile);
        Set(config,"video.gshandler","integer",renderer=="vulkan"?1:0);
        if(controller is not null && devices is {Count:>0})
        {
            var profile=new XDocument(new XElement("Config"));
            var ids=new Dictionary<string,int>{{"A",16},{"B",17},{"X",18},{"Y",19},{"Start",10},{"Back",11},{"LeftShoulder",14},{"RightShoulder",15}};
            var buttons=new Dictionary<string,int>{{"analog_left_x",0},{"analog_left_y",1},{"analog_right_x",2},{"analog_right_y",3},{"l2",4},{"r2",5},{"dpad_up",6},{"dpad_down",7},{"dpad_left",8},{"dpad_right",9},{"start",10},{"select",11},{"l3",12},{"r3",13},{"l1",14},{"r1",15},{"cross",16},{"circle",17},{"square",18},{"triangle",19}};
            foreach(var pair in map!)buttons[pair.Key.ToLowerInvariant()]=ids[pair.Value];
            for(int pad=0;pad<2;pad++)foreach(var (name,key) in buttons)
            {
                var prefix=$"input.pad{pad+1}.{name}";var enabled=pad<devices.Count;
                Set(profile,prefix+".bindingtype","integer",enabled?1:0);
                if(!enabled)continue;
                Set(profile,prefix+".bindingtarget1.providerId","integer",XInputProvider);
                Set(profile,prefix+".bindingtarget1.deviceId","string",$"{devices[pad]}:0:0:0:0:0");
                Set(profile,prefix+".bindingtarget1.keyId","integer",key);
                Set(profile,prefix+".bindingtarget1.keyType","integer",name.StartsWith("analog_")?1:0);
            }
            for(int pad=1;pad<=2;pad++)Set(profile,$"input.pad{pad}.analog.sensitivity","float","1.000000");
            Write(Path.Combine(data,"inputprofiles","umbra-xinput.xml"),profile,root);
            Set(config,"input.pad1.profile","string","umbra-xinput");
        }
        Write(configFile,config,root);
    }
    static XDocument Read(string path)
    {
        if(!File.Exists(path))return new(new XElement("Config"));
        try
        {
            using var reader=XmlReader.Create(path,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=1024*1024});
            var doc=XDocument.Load(reader);if(doc.Root?.Name!="Config")throw new XmlException();return doc;
        }
        catch(XmlException){throw new UserError("play-config","Play!'s configuration is damaged. It was left unchanged; restore its backup before launching.");}
    }
    static void Set(XDocument doc,string name,string type,object value)
    {
        doc.Root!.Elements("Preference").Where(x=>(string?)x.Attribute("Name")==name).Remove();
        doc.Root.Add(new XElement("Preference",new XAttribute("Name",name),new XAttribute("Type",type),new XAttribute("Value",value)));
    }
    static void Write(string path,XDocument doc,string root)
    {
        SaveService.RejectLinks(path,root);SaveService.RejectLinks(path+".bak",root);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{doc.Save(temp);if(File.Exists(path))File.Copy(path,path+".bak",true);File.Move(temp,path,true);}
        finally{if(File.Exists(temp))File.Delete(temp);}
    }
}
