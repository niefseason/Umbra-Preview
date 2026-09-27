using System.IO.Compression;
using System.Security.Cryptography;
namespace Umbra.Core.Emulation;
public static class EngineInstaller
{
    public const string N64Url="https://github.com/mupen64plus/mupen64plus-core/releases/download/2.6.0/mupen64plus-bundle-win64-2.6.0.zip";
    public const string N64Hash="8292C68D6FFC3428D4181AC09B1368EF2ADB2CC568325545AAC78CB6C1E66E21";
    public static async Task<BackendSettings> InstallN64(AppPaths paths,CancellationToken token=default)
    {
        var root=Path.Combine(paths.Root,"backends");Directory.CreateDirectory(root);
        var download=Path.Combine(root,"download-"+Guid.NewGuid().ToString("N")+".zip");
        var target=Path.Combine(root,"mupen64plus-2.6.0");var stage=target+"-"+Guid.NewGuid().ToString("N")+".staging";
        try
        {
            using var client=new HttpClient{Timeout=TimeSpan.FromMinutes(3)};
            using var response=await client.GetAsync(N64Url,HttpCompletionOption.ResponseHeadersRead,token);response.EnsureSuccessStatusCode();
            await using(var source=await response.Content.ReadAsStreamAsync(token))
            await using(var destination=File.Create(download))
            {var buffer=new byte[65536];long total=0;int count;while((count=await source.ReadAsync(buffer,token))>0){total+=count;if(total>64_000_000)throw new UserError("engine-size","The engine download exceeded its expected size.");await destination.WriteAsync(buffer.AsMemory(0,count),token);}}
            using(var file=File.OpenRead(download))if(Convert.ToHexString(SHA256.HashData(file))!=N64Hash)throw new UserError("engine-integrity","The engine download did not match the verified release. No engine was installed.");
            Directory.CreateDirectory(stage);
            using(var zip=ZipFile.OpenRead(download))foreach(var entry in zip.Entries)
            {
                if(!entry.FullName.StartsWith("Release/",StringComparison.Ordinal))continue;
                var relative=entry.FullName[8..];if(string.IsNullOrEmpty(relative)||relative.EndsWith('/'))continue;
                // Keep official executable, plugins, data and license notices. Do not install any game content.
                if(ContentDetection.Extensions.Contains(Path.GetExtension(relative).ToLowerInvariant()))continue;
                var output=Path.GetFullPath(Path.Combine(stage,relative));
                if(!output.StartsWith(stage+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new UserError("engine-path","The engine archive contains an unsafe path.");
                Directory.CreateDirectory(Path.GetDirectoryName(output)!);entry.ExtractToFile(output);
            }
            if(!File.Exists(Path.Combine(stage,"mupen64plus-ui-console.exe")))throw new UserError("engine-missing","The downloaded archive did not contain the expected engine.");
            if(Directory.Exists(target))target+="-"+DateTime.UtcNow.ToString("yyyyMMddHHmmss")+"-"+Guid.NewGuid().ToString("N")[..6];
            Directory.Move(stage,target);
            var executable=Path.Combine(target,"mupen64plus-ui-console.exe");
            return new(){Executable=executable,Version="2.6.0",Sha256=Adapters.HashExecutable(executable)};
        }
        catch(HttpRequestException){throw new UserError("engine-network","The official engine download is unavailable. Check your connection, try later, or select an existing engine. Your library still works offline.");}
        finally{if(File.Exists(download))File.Delete(download);if(Directory.Exists(stage))Directory.Delete(stage,true);}
    }
}
