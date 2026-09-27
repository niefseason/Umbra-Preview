namespace Umbra.Core;
public sealed class CompatibilityDatabase
{
    public int SchemaVersion { get; set; } = 1;
    public int Revision { get; set; } = 1;
    public List<CompatibilityEntry> Entries { get; set; } = [];
    public DisplaySettings? Resolve(Game game,string backendHash)
    {
        if(SchemaVersion!=1 || Entries is null) return null;
        var entry=Entries.FirstOrDefault(e=>e.ContentId==game.ContentId && e.Platform==game.Platform && e.BackendSha256==backendHash);
        if(entry is null)return null;
        JsonStore.ValidateDisplay(entry.Display);
        // Automatic compatibility may not introduce enhancements or change original speed.
        if(entry.Display.Profile is GraphicsProfile.Enhanced or GraphicsProfile.Custom || entry.Display.Stretch) return null;
        return entry.Display;
    }
}
public sealed class CompatibilityEntry
{
    public string ContentId { get; set; } = "";
    public ConsoleKind Platform { get; set; }
    public string BackendSha256 { get; set; } = "";
    public string Reason { get; set; } = "";
    public DisplaySettings Display { get; set; } = new();
}
