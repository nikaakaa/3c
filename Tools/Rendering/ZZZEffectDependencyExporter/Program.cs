using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AnimeStudio;

var options = Options.Parse(args);
if (Directory.Exists(options.Output))
    throw new InvalidOperationException("Output directory already exists");

var manifest = JsonDocument.Parse(File.ReadAllBytes(options.Manifest));
var roots = manifest.RootElement.GetProperty("roots").EnumerateArray()
    .Select(x => new EffectRoot(
        x.GetProperty("name").GetString()!,
        x.GetProperty("cab").GetString()!,
        x.GetProperty("pathId").GetInt64()))
    .ToArray();

var files = Directory.EnumerateFiles(options.ExtractRoot, "*_CAB-*", SearchOption.AllDirectories)
    .Where(x => !x.EndsWith(".resS", StringComparison.OrdinalIgnoreCase))
    .ToArray();
var resourceFilesByCab = Directory.EnumerateFiles(options.ExtractRoot, "*_CAB-*.resS", SearchOption.AllDirectories)
    .Select(path => (Path: path, Cab: CabFromResourceFileName(path)))
    .Where(x => x.Cab != null)
    .GroupBy(x => x.Cab!, StringComparer.OrdinalIgnoreCase)
    .ToDictionary(x => x.Key, x => x.Select(y => y.Path).OrderBy(y => y, StringComparer.OrdinalIgnoreCase).ToArray(),
        StringComparer.OrdinalIgnoreCase);
var textureTrace = options.TextureTrace == null ? null : JsonDocument.Parse(File.ReadAllText(options.TextureTrace, Encoding.UTF8));
var tracedTextures = textureTrace?.RootElement.GetProperty("candidates").EnumerateArray().ToArray() ?? [];
var extraFiles = tracedTextures
    .Select(x => x.GetProperty("source").GetString()!)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();
var cachedTextureDirectories = extraFiles
    .Select(Path.GetDirectoryName)
    .Where(x => !string.IsNullOrEmpty(x))
    .Select(x => x!)
    .ToHashSet(StringComparer.OrdinalIgnoreCase);
var shaderTrace = options.ShaderTrace == null ? null : JsonDocument.Parse(File.ReadAllText(options.ShaderTrace, Encoding.UTF8));
var tracedShaders = shaderTrace?.RootElement.GetProperty("candidates").EnumerateArray().ToArray() ?? [];
var shaderFiles = tracedShaders
    .Select(x => x.GetProperty("source").GetString()!)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();
var loadFiles = files.Concat(extraFiles).Concat(shaderFiles).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
var filesByCab = files
    .Select(path => (Path: path, Cab: CabFromFileName(path)))
    .Where(x => x.Cab != null)
    .GroupBy(x => x.Cab!, StringComparer.OrdinalIgnoreCase)
    .ToDictionary(x => x.Key, x => x.Select(y => y.Path).OrderBy(y => y, StringComparer.OrdinalIgnoreCase).ToArray(),
        StringComparer.OrdinalIgnoreCase);

var game = GameManager.GetGame("ZZZ") as Mhy ?? throw new InvalidOperationException("ZZZ game definition unavailable");
Logger.Default = new QuietLogger();
Logger.Flags = LoggerEvent.Error;
TypeFlags.SetTypes(new Dictionary<ClassIDType, (bool, bool)>
{
    [ClassIDType.AssetBundle] = (true, false),
    [ClassIDType.Material] = (true, false),
    [ClassIDType.Texture2D] = (true, false),
    [ClassIDType.Shader] = (true, false)
});

Directory.CreateDirectory(options.Output);
var manager = new AssetsManager
{
    Game = game,
    Silent = true,
    SkipProcess = false,
    SkipPostProcess = true
};

var dependencies = new List<DependencyRecord>();
var materials = new List<MaterialRecord>();
var textures = new Dictionary<string, TextureRecord>(StringComparer.Ordinal);
var shaders = new Dictionary<string, ShaderRecord>(StringComparer.Ordinal);
var errors = new List<ErrorRecord>();
var missingExternal = new List<MissingExternalRecord>();

try
{
    manager.LoadFiles(loadFiles);
    var objects = manager.assetsFileList
        .SelectMany(x => x.Objects)
        .GroupBy(x => (LoadedCab(x.assetsFile.fileName), x.m_PathID))
        .ToDictionary(x => x.Key, x => x.First());

    foreach (var root in roots)
    {
        if (!filesByCab.TryGetValue(root.Cab, out var rootFiles) || rootFiles.Length == 0)
        {
            errors.Add(new ErrorRecord(root.Cab, root.PathId, "RootCabinetNotFound"));
            continue;
        }
        var rootFile = manager.assetsFileList.FirstOrDefault(x => string.Equals(LoadedCab(x.fileName), root.Cab, StringComparison.OrdinalIgnoreCase));
        if (rootFile == null)
        {
            errors.Add(new ErrorRecord(root.Cab, root.PathId, "RootNotLoaded"));
            continue;
        }
        var bundle = rootFile.Objects.OfType<AssetBundle>().FirstOrDefault();
        if (bundle == null)
        {
        errors.Add(new ErrorRecord(root.Cab, root.PathId, "AssetBundleMissing file=" + rootFile.fileName));
            continue;
        }

        foreach (var pptr in bundle.m_PreloadTable)
        {
            if (pptr.m_FileID == 0)
                continue;
            var external = rootFile.m_Externals[pptr.m_FileID - 1];
            var targetCab = CabFromExternalName(external.fileName);
            var key = targetCab + ":" + pptr.m_PathID.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var target = objects.TryGetValue((targetCab, pptr.m_PathID), out var value) ? value : null;
            if (target == null)
            {
                missingExternal.Add(new MissingExternalRecord(root.Cab, root.Name, targetCab, pptr.m_PathID));
                continue;
            }
            dependencies.Add(new DependencyRecord(
                root.Cab,
                root.Name,
                bundle.m_Name,
                pptr.m_FileID - 1,
                targetCab,
                pptr.m_PathID,
                target.type.ToString(),
                target.Name,
                objects.TryGetValue((root.Cab, root.PathId), out var rootObject) ? rootObject.Name : root.Name,
                key));
        }
    }

    var materialKeys = dependencies
        .Where(x => x.TargetType == nameof(ClassIDType.Material))
        .Select(x => x.Key)
        .Distinct(StringComparer.Ordinal)
        .ToArray();
    foreach (var key in materialKeys)
    {
        var dependency = dependencies.First(x => x.Key == key);
        if (!objects.TryGetValue((dependency.TargetCab, dependency.TargetPathId), out var value) || value is not Material material)
        {
            errors.Add(new ErrorRecord(dependency.TargetCab, dependency.TargetPathId, "MaterialNotParsed"));
            continue;
        }
        try
        {
            var raw = material.GetRawData();
            var materialId = SafeName(dependency.TargetCab) + "__" + dependency.TargetPathId.ToString("X16", System.Globalization.CultureInfo.InvariantCulture);
            var rawName = materialId + ".material.bin";
            File.WriteAllBytes(Path.Combine(options.Output, rawName), raw);

            var textureEnvs = material.m_SavedProperties.m_TexEnvs.Select(item =>
            {
                var pptr = item.Value.m_Texture;
                var textureCab = pptr.m_FileID == 0
                    ? dependency.TargetCab
                    : CabFromExternalName(material.assetsFile.m_Externals[pptr.m_FileID - 1].fileName);
                var textureKey = textureCab + ":" + pptr.m_PathID.ToString(System.Globalization.CultureInfo.InvariantCulture);
                AnimeStudio.Object? textureObject = objects.TryGetValue((textureCab, pptr.m_PathID), out var found) ? found : null;
                string? exportedTexture = null;
                var targetState = pptr.m_FileID == 0 && pptr.m_PathID == 0 ? "Empty" : "Missing";
                if (textureObject is Texture2D texture)
                {
                    targetState = "Texture2D";
                    exportedTexture = ExportTexture(options.Output, textures, resourceFilesByCab, cachedTextureDirectories, textureCab, texture);
                }
                else if (targetState != "Empty")
                {
                    missingExternal.Add(new MissingExternalRecord(dependency.TargetCab, material.m_Name, textureCab, pptr.m_PathID));
                }
                return new TextureEnvRecord(
                    item.Key,
                    textureCab,
                    pptr.m_PathID,
                    targetState,
                    textureObject?.type.ToString(),
                    textureObject?.Name,
                    exportedTexture,
                    textureKey,
                    item.Value.m_Scale.X,
                    item.Value.m_Scale.Y,
                    item.Value.m_Offset.X,
                    item.Value.m_Offset.Y);
            }).ToArray();

            materials.Add(new MaterialRecord(
                dependency.TargetCab,
                dependency.TargetPathId,
                material.m_Name,
                dependency.RootCab,
                dependency.RootName,
                rawName,
                raw.Length,
                Hash(raw),
                new PPtrEvidence(
                    material.m_Shader.m_FileID,
                    material.m_Shader.m_PathID,
                    material.m_Shader.m_FileID == 0
                        ? dependency.TargetCab
                        : CabFromExternalName(material.assetsFile.m_Externals[material.m_Shader.m_FileID - 1].fileName)),
                (material.m_SavedProperties.m_Ints ?? []).Select(x => new NamedInt(x.Key, x.Value)).ToArray(),
                material.m_SavedProperties.m_Floats.Select(x => new NamedFloat(x.Key, x.Value)).ToArray(),
                material.m_SavedProperties.m_Colors.Select(x => new NamedColor(x.Key, x.Value.R, x.Value.G, x.Value.B, x.Value.A)).ToArray(),
                textureEnvs,
                material.m_ShaderKeywords,
                material.m_ValidKeywords,
                material.m_InvalidKeywords,
                material.m_LightmapFlags,
                material.m_EnableInstancingVariants,
                material.m_CustomRenderQueue,
                material.m_StringTagMap.Select(x => new NamedString(x.Key, x.Value)).ToArray(),
                material.m_DisabledShaderPasses,
                material.m_EnabledPassMask));
        }
        catch (Exception exception)
        {
            errors.Add(new ErrorRecord(dependency.TargetCab, dependency.TargetPathId, exception.ToString()));
        }
    }

    var shaderPointers = materials
        .Select(x => x.Shader)
        .DistinctBy(x => x.TargetCab + ":" + x.PathId.ToString(System.Globalization.CultureInfo.InvariantCulture))
        .ToArray();
    foreach (var pointer in shaderPointers)
    {
        var key = pointer.TargetCab + ":" + pointer.PathId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!objects.TryGetValue((pointer.TargetCab, pointer.PathId), out var value) || value is not Shader shader)
        {
            errors.Add(new ErrorRecord(pointer.TargetCab, pointer.PathId, "ShaderNotParsed"));
            continue;
        }
        var raw = shader.GetRawData();
        var id = SafeName(pointer.TargetCab) + "__" + pointer.PathId.ToString("X16", System.Globalization.CultureInfo.InvariantCulture);
        var outputFile = id + ".shader.bin";
        File.WriteAllBytes(Path.Combine(options.Output, outputFile), raw);
        var shaderMetaFile = id + ".shader.meta.json";
        var shaderMeta = new Dictionary<string, object>
        {
            ["platforms"] = shader.platforms.Select(p => p.ToString()).ToArray(),
            ["compressedBlob"] = Convert.ToBase64String(shader.compressedBlob),
            ["offsets"] = shader.offsets,
            ["compressedLengths"] = shader.compressedLengths,
            ["decompressedLengths"] = shader.decompressedLengths,
        };
        if (shader.m_ParsedForm != null)
        {
            shaderMeta["m_ParsedForm"] = JsonSerializer.Deserialize<JsonElement>(
                JsonSerializer.Serialize(shader.m_ParsedForm));
        }
        File.WriteAllText(Path.Combine(options.Output, shaderMetaFile),
            JsonSerializer.Serialize(shaderMeta));
        shaders.Add(key, new ShaderRecord(
            pointer.TargetCab,
            pointer.PathId,
            shader.Name,
            shader.m_ParsedForm?.m_Name ?? string.Empty,
            shader.m_ParsedForm?.m_CustomEditorName ?? string.Empty,
            shader.m_ParsedForm?.m_FallbackName ?? string.Empty,
            shader.m_ParsedForm?.m_PropInfo.m_Props.Count ?? 0,
            shader.m_ParsedForm?.m_SubShaders.Count ?? 0,
            shader.platforms.Select(x => x.ToString()).ToArray(),
            raw.Length,
            Hash(raw),
            outputFile));
    }
}
finally
{
    manager.Clear();
}

var report = new ExportReport(
    "zzz-effect-material-texture-export/2",
    options.Manifest,
    options.ExtractRoot,
    roots.Length,
    loadFiles.Length,
    dependencies.Count,
    dependencies.DistinctBy(x => x.Key).Count(),
    missingExternal.Count,
    materials.Count,
    textures.Count,
    shaders.Count,
    materials.SelectMany(x => x.TextureEnvs).Count(x => x.TargetState == "Empty"),
    materials.SelectMany(x => x.TextureEnvs).Count(x => x.TargetState == "Texture2D"),
    materials.SelectMany(x => x.TextureEnvs).Count(x => x.TargetState == "Missing"),
    errors.Count,
    dependencies.OrderBy(x => x.RootName).ThenBy(x => x.TargetCab).ThenBy(x => x.TargetPathId).ToArray(),
    materials.OrderBy(x => x.Name).ThenBy(x => x.Cab).ToArray(),
    textures.Values.OrderBy(x => x.Name).ThenBy(x => x.Cab).ToArray(),
    shaders.Values.OrderBy(x => x.Name).ThenBy(x => x.Cab).ToArray(),
    missingExternal.OrderBy(x => x.RootOrMaterialName).ThenBy(x => x.Cab).ThenBy(x => x.PathId).ToArray(),
    errors.ToArray());
var reportPath = Path.Combine(options.Output, "effect-material-texture-export.json");
File.WriteAllText(reportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions
{
    WriteIndented = true,
    NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
}), new UTF8Encoding(false));
Console.WriteLine(JsonSerializer.Serialize(new
{
    reportPath,
    roots = roots.Length,
    dependencyRefs = dependencies.Count,
    uniqueDependencies = report.UniqueDependencyCount,
    missingExternals = missingExternal.Count,
    materials = materials.Count,
    textures = textures.Count,
    shaders = shaders.Count,
    emptyTextureEnvs = report.EmptyTextureEnvCount,
    resolvedTextureEnvs = report.ResolvedTextureEnvCount,
    missingTextureEnvs = report.MissingTextureEnvCount,
    errors = errors.Count
}));

static string? CabFromFileName(string path)
{
    var name = Path.GetFileNameWithoutExtension(path);
    var match = System.Text.RegularExpressions.Regex.Match(name, @"_(CAB-[0-9a-fA-F]+)$");
    return match.Success ? match.Groups[1].Value : null;
}

static string CabFromExternalName(string value)
{
    var name = value.Replace('\\', '/').Split('/').Last();
    var match = System.Text.RegularExpressions.Regex.Match(name, @"^(CAB-[0-9a-fA-F]+)");
    if (!match.Success)
        return name;
    return match.Groups[1].Value;
}

static string LoadedCab(string fileName)
{
    var fromName = CabFromFileName(fileName);
    if (fromName != null)
        return fromName;
    var match = System.Text.RegularExpressions.Regex.Match(fileName, @"(CAB-[0-9a-fA-F]+)");
    return match.Success ? match.Groups[1].Value : fileName;
}

static string? CabFromResourceFileName(string path)
{
    var name = Path.GetFileName(path);
    var match = System.Text.RegularExpressions.Regex.Match(name, @"_(CAB-[0-9a-fA-F]+)\.resS$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    return match.Success ? match.Groups[1].Value : null;
}

static string CabFromResourcePath(string value)
{
    var match = System.Text.RegularExpressions.Regex.Match(value, @"(CAB-[0-9a-fA-F]+)\.resS$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    if (!match.Success)
        throw new ArgumentException("Resource path has no CAB resS target: " + value);
    return match.Groups[1].Value;
}

static string ExportTexture(string output, Dictionary<string, TextureRecord> textures, IReadOnlyDictionary<string, string[]> resourceFilesByCab,
    IReadOnlySet<string> cachedTextureDirectories,
    string cab, Texture2D texture)
{
    var key = cab + ":" + texture.m_PathID.ToString(System.Globalization.CultureInfo.InvariantCulture);
    if (textures.TryGetValue(key, out var existing))
        return existing.OutputFile;
    var stream = texture.m_StreamData;
    byte[] bytes;
    string resourceFile = string.Empty;
    var textureDirectory = Path.GetDirectoryName(texture.assetsFile.fullName) ?? string.Empty;
    if (stream != null && cachedTextureDirectories.Contains(textureDirectory))
    {
        bytes = texture.image_data.GetData();
    }
    else if (stream != null && !string.IsNullOrEmpty(stream.path))
    {
        var resourceCab = CabFromResourcePath(stream.path);
        if (!resourceFilesByCab.TryGetValue(resourceCab, out var candidates) || candidates.Length == 0)
            throw new FileNotFoundException("Can't find the resource CAB " + resourceCab);
        resourceFile = candidates.FirstOrDefault(x => string.Equals(Path.GetDirectoryName(x), textureDirectory, StringComparison.OrdinalIgnoreCase))
            ?? candidates[0];
        using var resourceReader = File.OpenRead(resourceFile);
        resourceReader.Position = stream.offset;
        bytes = new byte[stream.size];
        var read = resourceReader.Read(bytes, 0, bytes.Length);
        if (read != bytes.Length)
            throw new EndOfStreamException($"Resource stream ended after {read} of {bytes.Length} bytes");
    }
    else
    {
        bytes = texture.image_data.GetData();
    }
    var id = SafeName(cab) + "__" + texture.m_PathID.ToString("X16", System.Globalization.CultureInfo.InvariantCulture);
    var outputFile = id + ".texture.bytes";
    File.WriteAllBytes(Path.Combine(output, outputFile), bytes);
    var record = new TextureRecord(
        cab,
        texture.m_PathID,
        texture.Name,
        texture.m_Width,
        texture.m_Height,
        texture.m_TextureFormat.ToString(),
        texture.m_MipCount,
        texture.m_TextureSettings.m_FilterMode,
        texture.m_TextureSettings.m_Aniso,
        texture.m_TextureSettings.m_MipBias,
        texture.m_TextureSettings.m_WrapMode,
        texture.m_StreamData?.offset ?? 0,
        texture.m_StreamData?.path ?? string.Empty,
        Path.GetFileName(resourceFile),
        bytes.Length,
        Hash(bytes),
        outputFile);
    textures.Add(key, record);
    return outputFile;
}

static string SafeName(string value)
{
    var invalid = Path.GetInvalidFileNameChars().Concat(new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' });
    var builder = new StringBuilder(value.Length);
    foreach (var character in value)
        builder.Append(invalid.Contains(character) ? '_' : character);
    return builder.ToString();
}

static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

sealed record Options(string Manifest, string ExtractRoot, string Output, string? TextureTrace, string? ShaderTrace)
{
    public static Options Parse(string[] args)
    {
        string? manifest = null;
        string? extractRoot = null;
        string? output = null;
        string? textureTrace = null;
        string? shaderTrace = null;
        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--manifest": manifest = args[++index]; break;
                case "--extract-root": extractRoot = args[++index]; break;
                case "--output": output = args[++index]; break;
                case "--texture-trace": textureTrace = args[++index]; break;
                case "--shader-trace": shaderTrace = args[++index]; break;
                default: throw new ArgumentException("Unknown argument " + args[index]);
            }
        }
        if (manifest == null || extractRoot == null || output == null)
            throw new ArgumentException("--manifest, --extract-root and --output are required");
        return new Options(Path.GetFullPath(manifest), Path.GetFullPath(extractRoot), Path.GetFullPath(output),
            textureTrace == null ? null : Path.GetFullPath(textureTrace),
            shaderTrace == null ? null : Path.GetFullPath(shaderTrace));
    }
}

sealed record EffectRoot(string Name, string Cab, long PathId);
sealed record DependencyRecord(string RootCab, string RootName, string BundleName, int ExternalIndex,
    string TargetCab, long TargetPathId, string TargetType, string TargetName, string RootAssetName, string Key);
sealed record PPtrEvidence(int FileId, long PathId, string TargetCab);
sealed record NamedInt(string Name, int Value);
sealed record NamedFloat(string Name, float Value);
sealed record NamedColor(string Name, float R, float G, float B, float A);
sealed record NamedString(string Name, string Value);
sealed record TextureEnvRecord(string Name, string Cab, long PathId, string TargetState, string? Type, string? TextureName,
    string? OutputFile, string Key, float ScaleX, float ScaleY, float OffsetX, float OffsetY);
sealed record MaterialRecord(string Cab, long PathId, string Name, string RootCab, string RootName,
    string RawFile, int Bytes, string Sha256, PPtrEvidence Shader,
    NamedInt[] Ints, NamedFloat[] Floats, NamedColor[] Colors, TextureEnvRecord[] TextureEnvs,
    string ShaderKeywords, string[] ValidKeywords, string[] InvalidKeywords, uint LightmapFlags,
    bool EnableInstancingVariants, int CustomRenderQueue, NamedString[] StringTagMap,
    string[] DisabledShaderPasses, uint EnabledPassMask);
sealed record TextureRecord(string Cab, long PathId, string Name, int Width, int Height, string Format,
    int MipCount, int FilterMode, int Aniso, float MipBias, int WrapMode, long StreamOffset,
    string StreamPath, string ResourceFile, int Bytes, string Sha256, string OutputFile);
sealed record ShaderRecord(string Cab, long PathId, string Name, string ParsedName, string CustomEditorName,
    string FallbackName, int PropertyCount, int SubShaderCount, string[] Platforms, int Bytes,
    string Sha256, string OutputFile);
sealed record MissingExternalRecord(string RootOrMaterialCab, string RootOrMaterialName, string Cab, long PathId);
sealed record ErrorRecord(string Cab, long PathId, string Error);
sealed record ExportReport(string Schema, string Manifest, string ExtractRoot, int RootCount, int LoadedFileCount,
    int DependencyRefCount, int UniqueDependencyCount, int MissingExternalCount, int MaterialCount,
    int TextureCount, int ShaderCount, int EmptyTextureEnvCount, int ResolvedTextureEnvCount,
    int MissingTextureEnvCount, int ErrorCount, DependencyRecord[] Dependencies, MaterialRecord[] Materials,
    TextureRecord[] Textures, ShaderRecord[] Shaders, MissingExternalRecord[] MissingExternals, ErrorRecord[] Errors);

sealed class QuietLogger : ILogger
{
    public void Log(LoggerEvent loggerEvent, string message)
    {
    }
}

