using System.Text.Json;
namespace Umbra.Core;
public static class MetadataService
{
    // Offline-first sidecars are explicit imports, never executed or automatically trusted.
    public static (string? Title,Metadata Metadata) Read(string path)
    {
        if(new FileInfo(path).Length>1_000_000) throw new UserError("metadata-size","Metadata files must be smaller than 1 MB.");
        using var doc=JsonDocument.Parse(File.ReadAllText(path)); var root=doc.RootElement;
        string Text(string key)
        {
            if(!root.TryGetProperty(key,out var value) || value.ValueKind!=JsonValueKind.String) return "";
            var text=value.GetString()??"";
            return new string(text.Where(c=>!char.IsControl(c)).Take(256).ToArray());
        }
        int? year=null;
        if(root.TryGetProperty("year",out var y) && y.TryGetInt32(out var number) && number is >=1970 and <=2100) year=number;
        var title=Text("title");
        return (title.Length==0?null:title,new Metadata{Developer=Text("developer"),Publisher=Text("publisher"),Genre=Text("genre"),Region=Text("region"),Year=year});
    }
}

public interface IMetadataProvider
{
    string Name { get; }
    Task<(string? Title, Metadata Metadata, string Attribution)> LookupAsync(string title, ConsoleKind platform, CancellationToken token=default);
}

/// Wikidata is CC0 data; results are opt-in and attribution is retained with the imported metadata.
public sealed class WikidataProvider(HttpClient? client=null) : IMetadataProvider
{
    readonly HttpClient http=client??new HttpClient();
    public string Name => "Wikidata (CC0 data)";
    public async Task<(string? Title, Metadata Metadata, string Attribution)> LookupAsync(string title, ConsoleKind platform, CancellationToken token=default)
    {
        if(string.IsNullOrWhiteSpace(title)) throw new UserError("metadata-title","Enter a title before searching metadata.");
        var query=Uri.EscapeDataString(title.Trim()[..Math.Min(120,title.Trim().Length)]);
        using var request=new HttpRequestMessage(HttpMethod.Get,$"https://www.wikidata.org/w/api.php?action=wbsearchentities&search={query}&language=en&format=json&limit=1");
        request.Headers.UserAgent.ParseAdd("UMBRA/0.1 metadata opt-in; contact via application settings");
        using var response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,token);
        if(!response.IsSuccessStatusCode) throw new UserError("metadata-offline","Wikidata is unavailable. Existing local metadata remains usable.");
        using var document=JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
        var result=document.RootElement.GetProperty("search").EnumerateArray().FirstOrDefault();
        var label=result.ValueKind==JsonValueKind.Object && result.TryGetProperty("label",out var value)?value.GetString():null;
        if(string.IsNullOrWhiteSpace(label)) throw new UserError("metadata-none","No Wikidata result matched this title.");
        return (label,new Metadata(),"Wikidata (CC0), retrieved "+DateTimeOffset.UtcNow.ToString("u"));
    }
}
