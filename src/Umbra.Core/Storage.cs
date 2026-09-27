using System.Text.Json;
namespace Umbra.Core;

public sealed class AppPaths
{
    public string Root { get; }
    public AppPaths(string? root = null)
    {
        Root = Path.GetFullPath(root ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Umbra"));
        foreach (var folder in new[] { "config", "library", "logs", "backups", "overrides", "engines", "artwork" }) Directory.CreateDirectory(Path.Combine(Root, folder));
    }
    public string Config => Path.Combine(Root, "config", "settings.json");
    public string Library => Path.Combine(Root, "library", "games.json");
    public string Logs => Path.Combine(Root, "logs");
    public string Backups => Path.Combine(Root, "backups");
    public string Engine(ConsoleKind platform) => platform==ConsoleKind.PS2 ? Path.Combine(Root,"engines","PS2","PCSX2") : Path.Combine(Root, "engines", platform.ToString());
    public string Override(string id)
    {
        if (!Guid.TryParseExact(id, "N", out _)) throw new UserError("invalid-id", "The library entry is damaged. Import the game again.");
        return Path.Combine(Root, "overrides", id + ".json");
    }
}
public static class JsonStore
{
    public static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    public static void Write<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            { JsonSerializer.Serialize(stream, value, Options); stream.Flush(true); }
            if (File.Exists(path)) File.Copy(path, path + ".bak", true);
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public static T Read<T>(string path, Func<T> defaults, Action<string>? warning = null)
    {
        if (!File.Exists(path)) return defaults();
        try { return Parse<T>(path); }
        catch (Exception e) when (e is JsonException or InvalidDataException)
        {
            var quarantine = path + ".corrupt-" + Guid.NewGuid().ToString("N");
            File.Copy(path, quarantine);
            if (File.Exists(path + ".bak"))
            {
                try { var restored = Parse<T>(path + ".bak"); File.Copy(path + ".bak", path, true); warning?.Invoke("Recovered a damaged file from its previous copy. The damaged copy was preserved."); return restored; }
                catch (Exception ex) when (ex is JsonException or InvalidDataException) { }
            }
            warning?.Invoke("A damaged settings or library file was preserved for recovery. Defaults were loaded.");
            var value = defaults(); Write(path, value); return value;
        }
    }
    static T Parse<T>(string path)
    {
        if (new FileInfo(path).Length > 32 * 1024 * 1024) throw new InvalidDataException();
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options) ?? throw new InvalidDataException();
    }
    public static Settings LoadSettings(string path, Action<string>? warning = null)
    {
        var s = Read(path, ()=>new Settings(), warning);
        if (s.SchemaVersion > 2) throw new UserError("future-config", "These settings belong to a newer UMBRA version. Open that version to keep your settings safe.");
        s.Display ??= new(); s.Controller ??= new(); s.Backends ??= new(); s.LibraryFolders ??= [];
        s.BiosPath ??= "";
        s.BiosFreePs2 ??= new();
        foreach (var platform in Enum.GetValues<ConsoleKind>()) if (!s.Backends.TryGetValue(platform, out var backend) || backend is null) s.Backends[platform] = new();
        ValidateDisplay(s.Display);
        if (s.Controller.Players is < 1 or > 4) s.Controller.Players = 1;
        if (s.Controller.N64AnalogDeadzone is < 0 or > 32768) s.Controller.N64AnalogDeadzone = 4096;
        s.Controller.Buttons ??= new ControllerProfile().Buttons;
        s.Controller.SdlDevices ??= new();
        foreach(var player in s.Controller.SdlDevices.Keys.ToArray())
        {
            s.Controller.SdlDevices[player] ??= Controllers.DefaultSdlDevice(player);
            s.Controller.SdlDevices[player].Inputs ??= new();
        }
        foreach (var pair in new ControllerProfile().Buttons)
            if (!s.Controller.Buttons.TryGetValue(pair.Key, out var value) || !Controllers.Allowed.Contains(value)) s.Controller.Buttons[pair.Key] = pair.Value;
        if (s.SchemaVersion < 2) { s.SchemaVersion = 2; Write(path, s); }
        return s;
    }
    public static void ValidateDisplay(DisplaySettings d)
    {
        if (!Enum.IsDefined(d.Profile) || d.Scale is < 1 or > 6) throw new UserError("bad-profile", "Graphics settings are invalid. Choose a scale from 1 to 6 and a supported profile.");
    }
}
public sealed class EventLog(AppPaths paths)
{
    readonly object gate = new();
    public void Write(string eventName, string detail = "")
    {
        // Events contain controlled codes and internal IDs only, never imported paths or game titles.
        lock (gate)
        {
            try
            {
                var file = Path.Combine(paths.Logs, "umbra.jsonl");
                if (File.Exists(file) && new FileInfo(file).Length > 2_000_000) File.Move(file, file + ".1", true);
                File.AppendAllText(file, JsonSerializer.Serialize(new { time = DateTimeOffset.UtcNow, eventName, detail }) + Environment.NewLine);
            }
            catch (IOException) { /* Logging failure must not prevent save or shutdown. */ }
            catch (UnauthorizedAccessException) { }
        }
    }
}
