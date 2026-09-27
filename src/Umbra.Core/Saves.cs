using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
namespace Umbra.Core;

public sealed record SaveEntry(string Path,long Length,string Sha256);
public sealed record SaveManifest(int SchemaVersion,ConsoleKind Platform,DateTimeOffset Created,List<SaveEntry> Files);
public sealed class SaveService(AppPaths paths,EventLog log)
{
    static string[] Folders(ConsoleKind platform)=>platform switch {ConsoleKind.PS2=>["memcards","sstates","Play"],ConsoleKind.GameCube=>["GC","StateSaves"],ConsoleKind.Xbox360=>["content","content-canary"],_=>["save","states"]};
    public void Ensure(ConsoleKind platform)
    {
        foreach(var folder in Folders(platform))
        {
            var target=Path.Combine(paths.Engine(platform),folder); Directory.CreateDirectory(target);
            RejectLinks(target,paths.Root);
            var probe=Path.Combine(target,".umbra-write-"+Guid.NewGuid().ToString("N"));
            try { File.WriteAllText(probe,""); } finally { if(File.Exists(probe)) File.Delete(probe); }
        }
    }
    public static void RejectLinks(string path,string root)
    {
        for(var p=Path.GetFullPath(path); p.Length>=Path.GetFullPath(root).Length; p=Path.GetDirectoryName(p)??"")
        { if((File.Exists(p)||Directory.Exists(p)) && (File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0) throw new UserError("save-link","A save location is a redirected link. Choose a regular local data directory to protect your files."); if(p==root) break; }
    }
    public string Backup(ConsoleKind platform)
    {
        Ensure(platform);
        var destination=Path.Combine(paths.Backups,$"{platform}-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.zip"); var temp=destination+".partial";
        var records=new List<SaveEntry>(); long total=0;
        try
        {
            using(var zip=ZipFile.Open(temp,ZipArchiveMode.Create))
            {
                foreach(var folder in Folders(platform)) foreach(var file in Directory.EnumerateFiles(Path.Combine(paths.Engine(platform),folder),"*",new EnumerationOptions{RecurseSubdirectories=true,AttributesToSkip=FileAttributes.ReparsePoint}))
                {
                    var length=new FileInfo(file).Length; total+=length;
                    if(total>2L*1024*1024*1024) throw new UserError("save-size","Save data exceeds the 2 GB backup limit. Archive older save states before playing.");
                    var relative=Path.GetRelativePath(paths.Engine(platform),file).Replace('\\','/');
                    using var input=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.Read);
                    var hash=Convert.ToHexString(SHA256.HashData(input)); input.Position=0;
                    using(var output=zip.CreateEntry(relative,CompressionLevel.Fastest).Open()) input.CopyTo(output);
                    records.Add(new(relative,length,hash));
                }
                using var manifest=zip.CreateEntry("manifest.json").Open(); JsonSerializer.Serialize(manifest,new SaveManifest(1,platform,DateTimeOffset.UtcNow,records),JsonStore.Options);
            }
            File.Move(temp,destination); log.Write("save.backup",platform.ToString()); return destination;
        }
        finally { if(File.Exists(temp)) File.Delete(temp); }
    }
    public void Restore(string archive,ConsoleKind platform)
    {
        using var zip=ZipFile.OpenRead(archive);
        var manifestEntry=zip.GetEntry("manifest.json")??throw new UserError("backup-invalid","This is not an UMBRA save backup.");
        if(manifestEntry.Length>4_000_000) throw new UserError("backup-invalid","The backup manifest is too large.");
        using var ms=manifestEntry.Open(); var manifest=JsonSerializer.Deserialize<SaveManifest>(ms,JsonStore.Options)??throw new UserError("backup-invalid","The backup manifest is missing.");
        if(manifest.SchemaVersion!=1 || manifest.Platform!=platform || manifest.Files is null || manifest.Files.Count>10000) throw new UserError("backup-invalid","This backup belongs to another console or version.");
        long total=0; var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Validate all files before any live save is touched. Stage outside live data.
        var staging=Path.Combine(paths.Backups,"restore-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(staging);
        try
        {
            foreach(var item in manifest.Files)
            {
                var relative=item.Path.Replace('\\','/'); var parts=relative.Split('/');
                if(parts.Length<2 || !Folders(platform).Contains(parts[0]) || parts.Any(p=>p is "" or "." or ".." || p.Contains(':') || p.IndexOfAny(Path.GetInvalidFileNameChars())>=0) || !names.Add(relative)) throw new UserError("backup-path","The backup contains an unsafe or duplicate path.");
                var entry=zip.GetEntry(relative)??throw new UserError("backup-invalid","A save file is missing from the backup.");
                total+=entry.Length;
                if(entry.Length!=item.Length || total>2L*1024*1024*1024) throw new UserError("backup-size","The backup is damaged or exceeds the restore limit.");
                var target=Path.Combine(staging,relative); Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                entry.ExtractToFile(target);
                using var input=File.OpenRead(target);
                if(Convert.ToHexString(SHA256.HashData(input))!=item.Sha256) throw new UserError("backup-hash","A save in this backup failed its integrity check. Your current saves have not been changed.");
            }
            Backup(platform); // Recovery point before replacing existing files; extra live files are retained.
            foreach(var item in manifest.Files)
            {
                var target=Path.Combine(paths.Engine(platform),item.Path); RejectLinks(target,paths.Root);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                var temp=target+".restore"; File.Copy(Path.Combine(staging,item.Path),temp,true); File.Move(temp,target,true);
            }
            log.Write("save.restore",platform.ToString());
        }
        finally { Directory.Delete(staging,true); }
    }
}
