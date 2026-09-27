using System.Security.Cryptography;
using System.Text;
namespace Umbra.Core;

public static class ContentDetection
{
    public static readonly string[] Extensions = [".z64", ".n64", ".v64", ".iso", ".gcm", ".rvz", ".gcz", ".ciso", ".chd", ".cso", ".bin", ".elf", ".dol", ".xex"];
    public static ConsoleKind? Detect(string path)
    {
        if (!Extensions.Contains(Path.GetExtension(path).ToLowerInvariant())) return null;
        using var file = File.OpenRead(path);
        var bytes = new byte[(int)Math.Min(file.Length, 4 * 1024 * 1024)]; file.ReadExactly(bytes);
        if(bytes.Length>=24 && bytes.AsSpan(0,4).SequenceEqual("XEX2"u8))return ConsoleKind.Xbox360;
        if (bytes.Length >= 4 && (bytes.AsSpan(0,4).SequenceEqual(new byte[]{0x80,0x37,0x12,0x40}) || bytes.AsSpan(0,4).SequenceEqual(new byte[]{0x37,0x80,0x40,0x12}) || bytes.AsSpan(0,4).SequenceEqual(new byte[]{0x40,0x12,0x37,0x80}))) return ConsoleKind.N64;
        if (bytes.Length >= 32 && bytes.AsSpan(28,4).SequenceEqual(new byte[]{0xc2,0x33,0x9f,0x3d})) return ConsoleKind.GameCube;
        if (Encoding.ASCII.GetString(bytes).Contains("BOOT2", StringComparison.Ordinal)) return ConsoleKind.PS2;
        // Containers and generic disc images need explicit classification; no unsafe extension-only guesses.
        return null;
    }
    public static string Fingerprint(string path)
    {
        using var stream = File.OpenRead(path);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(BitConverter.GetBytes(stream.Length));
        var bytes = new byte[(int)Math.Min(stream.Length, 65536)]; stream.ReadExactly(bytes); hash.AppendData(bytes);
        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }
    public static void ValidatePlatformFormat(string path,ConsoleKind platform)
    {
        using var stream=File.OpenRead(path);Span<byte> magic=stackalloc byte[8];var count=stream.Read(magic);
        if(platform==ConsoleKind.Xbox360)
        {
            var extension=Path.GetExtension(path).ToLowerInvariant();
            if(extension==".xex" && stream.Length>=24 && magic[..4].SequenceEqual("XEX2"u8))return;
            if(extension==".iso")
            {
                // XDVDFS is shared with original Xbox: require explicit Xbox 360
                // selection for ISO, and let Xenia validate the guest executable.
                Span<byte> signature=stackalloc byte[20];
                foreach(long partition in new long[]{0,0xFB20,0x20600,0x2080000,0xFD90000})
                {
                    var offset=partition+32*2048;
                    if(stream.Length<offset+20)continue;
                    stream.Position=offset;stream.ReadExactly(signature);
                    if(signature.SequenceEqual("MICROSOFT*XBOX*MEDIA"u8))return;
                }
            }
            throw new UserError("xenia-format","Select a complete Xbox 360 default.xex with its game folder, or an Xbox 360 XDVDFS ISO. ZIP, CHD, original Xbox games and packaged downloads are not supported by this integration.");
        }
        if(Path.GetExtension(path).Equals(".xex",StringComparison.OrdinalIgnoreCase))throw new UserError("platform-mismatch","XEX files require Xbox 360 support.");
        var chd=Path.GetExtension(path).Equals(".chd",StringComparison.OrdinalIgnoreCase) || (count==8 && magic.SequenceEqual("MComprHD"u8));
        if(chd && platform!=ConsoleKind.PS2)throw new UserError("wrong-container","This is a CHD disc image. Dolphin/GameCube and the N64 engine cannot open it. In game details, correct the console only if this is a PS2 dump; Dreamcast and arcade games need a different emulator. Renaming the file will not convert it.");
    }
    public static void ValidateBios(string path)
    {
        if (!File.Exists(path)) throw new UserError("bios-missing", "PlayStation 2 needs your console BIOS. Select your own BIOS file in Settings.");
        var length = new FileInfo(path).Length;
        if (length is < 4 * 1024 * 1024 or > 8 * 1024 * 1024) throw new UserError("bios-invalid", "This file does not look like a PS2 BIOS dump. Select the original 4–8 MB ROM dump from your console.");
        var text = Encoding.ASCII.GetString(File.ReadAllBytes(path));
        if (!text.Contains("ROMVER", StringComparison.Ordinal) || !text.Contains("RESET", StringComparison.Ordinal)) throw new UserError("bios-invalid", "The BIOS ROM directory was not found. Select a complete PS2 BIOS dump.");
        // Structural screening only; PCSX2 performs authoritative validation.
    }
}
public sealed record ScanResult(List<Game> Games, int Skipped, List<string> Warnings);
public sealed class LibraryService(AppPaths paths, EventLog log)
{
    public List<Game> Load(Action<string>? warning = null) => JsonStore.Read(paths.Library, ()=>new List<Game>(), warning)
        .Where(g=>g is not null && Guid.TryParseExact(g.Id,"N",out _) && !string.IsNullOrWhiteSpace(g.Path) && Enum.IsDefined(g.Platform)).ToList();
    public void Save(List<Game> games) => JsonStore.Write(paths.Library, games);
    public Game Import(string path, ConsoleKind? platform = null)
    {
        path = Path.GetFullPath(path);
        if (!File.Exists(path)) throw new UserError("game-missing", "The game file is no longer here. Locate it again or reconnect its drive.");
        if (!ContentDetection.Extensions.Contains(Path.GetExtension(path).ToLowerInvariant())) throw new UserError("format", "This file format is not supported. Import an extracted game dump, not a ZIP or program.");
        var detected = ContentDetection.Detect(path);
        if (detected.HasValue && platform.HasValue && detected != platform) throw new UserError("platform-mismatch", "The file header belongs to a different console. Select the detected console.");
        var selected = detected ?? platform ?? throw new UserError("ambiguous", "This container needs a console selection. Choose PlayStation 2, GameCube, or Nintendo 64 when adding it.");
        ContentDetection.ValidatePlatformFormat(path,selected);
        return new Game { Path=path, Platform=selected, Title=Path.GetFileNameWithoutExtension(path), ContentId=ContentDetection.Fingerprint(path) };
    }
    public ScanResult Scan(IEnumerable<string> folders, CancellationToken token = default)
    {
        var games = new List<Game>(); var warnings = new List<string>(); int skipped = 0;
        foreach (var folder in folders.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(folder)) { warnings.Add("A library folder is unavailable. Reconnect its drive or choose its new location."); continue; }
            try
            {
                var options = new EnumerationOptions { RecurseSubdirectories=true, IgnoreInaccessible=true, AttributesToSkip=FileAttributes.ReparsePoint, MaxRecursionDepth=32 };
                foreach (var file in Directory.EnumerateFiles(folder, "*", options))
                {
                    token.ThrowIfCancellationRequested();
                    if (!ContentDetection.Extensions.Contains(Path.GetExtension(file).ToLowerInvariant())) continue;
                    try { games.Add(Import(file)); }
                    catch (Exception e) when (e is IOException or UnauthorizedAccessException or UserError) { skipped++; }
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { warnings.Add("One library folder could not be read."); }
        }
        log.Write("library.scan", $"found={games.Count};unclassified={skipped}");
        return new(games, skipped, warnings);
    }
    public static int Merge(List<Game> library, IEnumerable<Game> incoming)
    {
        var existing = library.Select(g=>Path.GetFullPath(g.Path)).ToHashSet(StringComparer.OrdinalIgnoreCase); int added=0;
        foreach (var game in incoming) if (existing.Add(game.Path)) { library.Add(game); added++; }
        return added;
    }
}
