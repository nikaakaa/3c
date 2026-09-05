using System.Security.Cryptography;
using System.Text.Json;
using AnimeStudio;

var options = Options.Parse(args);
if (Directory.Exists(options.Output))
    throw new InvalidOperationException("Output directory already exists");
var names = new HashSet<string>(File.ReadAllLines(options.NamesFile)
    .Select(x => x.Trim()).Where(x => x.Length > 0), StringComparer.Ordinal);
var blockIds = File.ReadAllLines(options.BlockIdsFile).Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
var game = GameManager.GetGame("ZZZ") as Mhy ?? throw new InvalidOperationException("ZZZ game definition unavailable");
TypeFlags.SetTypes(new Dictionary<ClassIDType, (bool, bool)>
{
    [ClassIDType.Texture2D] = (true, false)
});
Directory.CreateDirectory(options.Output);
var textures = new Dictionary<string, TextureRecord>(StringComparer.Ordinal);
foreach (var blockId in blockIds)
{
    var block = Path.Combine(options.BlockRoot, blockId + ".blk");
    var manager = new AssetsManager { Game = game, Silent = true, SkipPostProcess = true };
    try
    {
        manager.LoadFiles(block);
        foreach (var texture in manager.assetsFileList.SelectMany(x => x.Objects).OfType<Texture2D>())
        {
            if (!names.Contains(texture.Name))
                continue;
            var bytes = texture.image_data.GetData();
            var hash = Convert.ToHexString(SHA256.HashData(bytes));
            var record = new TextureRecord(
                texture.Name,
                block,
                texture.assetsFile.fileName,
                texture.m_PathID,
                texture.m_Width,
                texture.m_Height,
                texture.m_TextureFormat.ToString(),
                texture.m_MipCount,
                texture.m_TextureSettings.m_FilterMode,
                texture.m_TextureSettings.m_Aniso,
                texture.m_TextureSettings.m_MipBias,
                texture.m_TextureSettings.m_WrapMode,
                bytes.Length,
                hash,
                texture.Name + ".bytes");
            if (textures.TryGetValue(texture.Name, out var existing))
            {
                if (existing.Sha256 != record.Sha256 || existing.Width != record.Width ||
                    existing.Height != record.Height || existing.Format != record.Format ||
                    existing.MipCount != record.MipCount)
                    throw new InvalidOperationException($"Texture {texture.Name} differs between source blocks");
                continue;
            }
            File.WriteAllBytes(Path.Combine(options.Output, record.OutputFile), bytes);
            textures.Add(texture.Name, record);
        }
    }
    finally
    {
        manager.Clear();
    }
}
var missing = names.Except(textures.Keys, StringComparer.Ordinal).OrderBy(x => x).ToArray();
if (missing.Length != 0)
    throw new InvalidOperationException("Missing textures: " + string.Join(", ", missing));
var report = new TextureExportReport(
    "zzz-texture-raw-export/1",
    options.BlockRoot,
    blockIds,
    textures.Values.OrderBy(x => x.Name, StringComparer.Ordinal).ToArray());
File.WriteAllText(Path.Combine(options.Output, "texture-raw-export.json"),
    JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));

sealed record Options(string BlockRoot, string BlockIdsFile, string NamesFile, string Output)
{
    public static Options Parse(string[] args)
    {
        string? blockRoot = null;
        string? blockIdsFile = null;
        string? namesFile = null;
        string? output = null;
        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--block-root": blockRoot = args[++index]; break;
                case "--block-ids-file": blockIdsFile = args[++index]; break;
                case "--names-file": namesFile = args[++index]; break;
                case "--output": output = args[++index]; break;
                default: throw new ArgumentException($"Unknown argument {args[index]}");
            }
        }
        if (blockRoot is null || blockIdsFile is null || namesFile is null || output is null)
            throw new ArgumentException("--block-root, --block-ids-file, --names-file and --output are required");
        return new Options(Path.GetFullPath(blockRoot), Path.GetFullPath(blockIdsFile),
            Path.GetFullPath(namesFile), Path.GetFullPath(output));
    }
}

sealed record TextureRecord(string Name, string SourceBlock, string SerializedFile, long PathId,
    int Width, int Height, string Format, int MipCount, int FilterMode, int Aniso, float MipBias,
    int WrapMode, int Bytes, string Sha256, string OutputFile);
sealed record TextureExportReport(string Schema, string BlockRoot, string[] BlockIds, TextureRecord[] Textures);
