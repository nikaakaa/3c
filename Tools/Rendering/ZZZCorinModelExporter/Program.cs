using System.Text.Json;
using AnimeStudio;

var options = Options.Parse(args);
if (File.Exists(options.Output))
    throw new InvalidOperationException("Output file already exists");
var outputDirectory = Path.GetDirectoryName(options.Output) ?? throw new InvalidOperationException("Output directory is unavailable");
Directory.CreateDirectory(outputDirectory);
var game = GameManager.GetGame("ZZZ") as Mhy ?? throw new InvalidOperationException("ZZZ game definition unavailable");
var manager = new AssetsManager { Game = game, Silent = false, SkipProcess = false, SkipPostProcess = false };
manager.LoadFiles(options.Inputs);
var animator = manager.assetsFileList.SelectMany(x => x.Objects).OfType<Animator>()
    .Single(x => string.Equals(x.Name, options.Animator, StringComparison.Ordinal));
var converterOptions = new ModelConverter.Options
{
    imageFormat = ImageFormat.Png,
    game = game,
    collectAnimations = false,
    exportMaterials = false,
    materials = new HashSet<Material>(),
    uvs = Enumerable.Range(0, 8).ToDictionary(x => $"UV{x}", x => (true, x)),
    texs = new Dictionary<string, int>()
};
var converter = new ModelConverter(animator, converterOptions, Array.Empty<AnimationClip>());
if (converter.MeshList.Count == 0)
    throw new InvalidOperationException("Resolved model contains no meshes");
ModelExporter.ExportFbx(options.Output, converter, new Fbx.ExportOptions
{
    eulerFilter = true,
    filterPrecision = 0.25f,
    exportAllNodes = true,
    exportSkins = true,
    exportAnimations = false,
    exportBlendShape = true,
    castToBone = false,
    boneSize = 10,
    scaleFactor = 1f,
    fbxVersion = 3,
    fbxFormat = 0
});
var sourceMeshes = manager.assetsFileList.SelectMany(x => x.Objects).OfType<Mesh>()
    .Where(x => x.Name.StartsWith($"SeparateMesh_{options.Animator}_", StringComparison.Ordinal))
    .OrderBy(x => x.Name, StringComparer.Ordinal)
    .Select(x => new SourceMesh(x.Name, x.m_VertexCount, x.m_SubMeshes.Count, x.m_Indices.Count, x.m_BindPose?.Length ?? 0,
        x.m_Normals?.Length > 0, x.m_Tangents?.Length > 0, x.m_Colors?.Length > 0,
        Enumerable.Range(0, 8).Select(index => x.GetUV(index)?.Length > 0).ToArray()))
    .ToArray();
var importedMeshes = converter.MeshList.OrderBy(x => x.Path, StringComparer.Ordinal)
    .Select(x => new ImportedMeshEvidence(x.Path, x.VertexList.Count, x.SubmeshList.Count,
        x.SubmeshList.Select(y => y.Material).ToArray()))
    .ToArray();
var report = new ExportReport("zzz-corin-model-export/1", options.Animator, options.Inputs, sourceMeshes, importedMeshes,
    options.Output, new FileInfo(options.Output).Length);
File.WriteAllText(Path.Combine(outputDirectory, "model-export.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
manager.Clear();
Console.WriteLine($"Exported {converter.MeshList.Count} renderer meshes to {options.Output}");

sealed record Options(string[] Inputs, string Output, string Animator)
{
    public static Options Parse(string[] args)
    {
        var inputs = new List<string>();
        string? output = null;
        var animator = "Avatar_Female_Size01_Corin_Model";
        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--input": inputs.Add(Path.GetFullPath(args[++index])); break;
                case "--output": output = Path.GetFullPath(args[++index]); break;
                case "--animator": animator = args[++index]; break;
                default: throw new ArgumentException($"Unknown argument {args[index]}");
            }
        }
        if (inputs.Count == 0 || output is null)
            throw new ArgumentException("At least one --input and --output are required");
        return new Options(inputs.ToArray(), output, animator);
    }
}

sealed record SourceMesh(string Name, int VertexCount, int SubMeshCount, int IndexCount, int BindPoseCount,
    bool HasNormals, bool HasTangents, bool HasColors, bool[] UvChannels);
sealed record ImportedMeshEvidence(string Path, int VertexCount, int SubMeshCount, string[] Materials);
sealed record ExportReport(string Schema, string Animator, string[] Inputs, SourceMesh[] SourceMeshes,
    ImportedMeshEvidence[] ImportedMeshes, string Output, long OutputBytes);
