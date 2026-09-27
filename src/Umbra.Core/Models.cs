using System.Text.Json.Serialization;
namespace Umbra.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConsoleKind { PS2, GameCube, N64, Xbox360 }
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GraphicsProfile { OriginalHardware, CleanOriginal, Enhanced, Custom }
public sealed class Game
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Path { get; set; } = "";
    public string Title { get; set; } = "";
    public ConsoleKind Platform { get; set; }
    public bool Favorite { get; set; }
    public DateTimeOffset? LastPlayed { get; set; }
    public double PlaySeconds { get; set; }
    public string ContentId { get; set; } = "";
    public Metadata Metadata { get; set; } = new();
    [JsonIgnore] public bool Missing => !File.Exists(Path);
    [JsonIgnore] public string PlatformLabel => ConsoleNames.Display(Platform).ToUpperInvariant();
    [JsonIgnore] public string Playtime => PlaySeconds >= 3600 ? $"{PlaySeconds / 3600:0.0} hours" : $"{PlaySeconds / 60:0} min";
}
public sealed class Metadata
{
    public string Developer { get; set; } = "";
    public string Publisher { get; set; } = "";
    public string Genre { get; set; } = "";
    public string Region { get; set; } = "";
    public int? Year { get; set; }
    public string CoverPath { get; set; } = "";
}
public sealed class DisplaySettings
{
    public GraphicsProfile Profile { get; set; } = GraphicsProfile.OriginalHardware;
    public bool Fullscreen { get; set; } = true;
    public bool Stretch { get; set; }
    public bool VSync { get; set; } = true;
    public int Scale { get; set; } = 1;
}
public sealed class BackendSettings
{
    public string Executable { get; set; } = "";
    public string Version { get; set; } = "Not verified";
    public string Sha256 { get; set; } = "";
}
public sealed class ControllerProfile
{
    public int Players { get; set; } = 1;
    // Xbox-friendly action preset: movement/camera sticks are separated and the
    // triggers/shoulders are kept available for aim, shoot and weapon actions.
    public bool XboxActionLayout { get; set; } = true;
    // N64 has no second analog camera axis. Leave this off for games whose C
    // buttons are actions; enable it only for titles that use C buttons as camera.
    public bool N64RightStickCButtons { get; set; }
    // Mupen64Plus SDL uses 4096,4096 as its official default. Keep this
    // configurable for worn sticks without sacrificing the normal range.
    public int N64AnalogDeadzone { get; set; } = 4096;
    public bool GoldenEyeSolitaire { get; set; }
    public bool GoldenEyeInvertLook { get; set; }
    public bool GoldenEyeReloadOnWest { get; set; }
    public bool GoldenEyeWeaponOnLeft { get; set; }
    public ModernStickTuning ModernTuning { get; set; } = new();
    // Logical Xbox-style positions. Backend adapters translate these into native labels.
    public Dictionary<string, string> Buttons { get; set; } = new()
    { ["South"]="A", ["East"]="B", ["West"]="X", ["North"]="Y", ["Start"]="Start", ["Select"]="Back", ["L"]="LeftShoulder", ["R"]="RightShoulder" };
    public Dictionary<int, SdlDeviceProfile> SdlDevices { get; set; } = new();
}
public sealed class SdlDeviceProfile
{
    public int? DeviceIndex { get; set; }
    public string StandardMapping { get; set; } = "";
    public string Guid { get; set; } = "";
    public string Name { get; set; } = "";
    public Dictionary<string, string> Inputs { get; set; } = new();
}
public sealed class Settings
{
    public int SchemaVersion { get; set; } = 2;
    public bool SetupComplete { get; set; }
    public List<string> LibraryFolders { get; set; } = [];
    public string BiosPath { get; set; } = "";
    public bool UseBiosFreePs2 { get; set; }
    public BackendSettings BiosFreePs2 { get; set; } = new();
    public DisplaySettings Display { get; set; } = new();
    public ControllerProfile Controller { get; set; } = new();
    public Dictionary<ConsoleKind, BackendSettings> Backends { get; set; } = Enum.GetValues<ConsoleKind>().ToDictionary(p=>p, _=>new BackendSettings());
}
public sealed class GameOverride
{
    public DisplaySettings? Display { get; set; }
    // Empty means use the global controller profile. N64 presets are title
    // specific because C-buttons are not a universal camera API.
    public string ControllerPreset { get; set; } = "";
    public string N64VideoPlugin { get; set; } = "";
    public string PlayRenderer { get; set; } = "opengl";
    public PcsxTuning? PcsxTuning { get; set; }
    public BackendSettings? XeniaAudioBackend { get; set; }
    public ControllerProfile? SavedController { get; set; }
    public ModernStickTuning? StickTuning { get; set; }
    public bool InvertLook { get; set; }
    public bool ReloadOnWest { get; set; }
    public bool WeaponOnLeft { get; set; }
    public int? StartupStateSlot { get; set; }
    public string Notes { get; set; } = "";
}
public sealed record Issue(string Component, string State, string Detail);
public static class ConsoleNames
{
    public static string Display(ConsoleKind platform)=>platform switch
    {ConsoleKind.PS2=>"PlayStation 2",ConsoleKind.GameCube=>"GameCube",ConsoleKind.N64=>"Nintendo 64",ConsoleKind.Xbox360=>"Xbox 360",_=>throw new ArgumentOutOfRangeException(nameof(platform))};
}
public sealed class UserError(string code, string message) : Exception(message) { public string Code { get; } = code; }
