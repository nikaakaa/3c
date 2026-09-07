using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using AnimeStudio;

var options = Options.Parse(args);
if (Directory.Exists(options.Output))
    throw new InvalidOperationException("Output directory already exists");
var names = new HashSet<string>(File.ReadAllLines(options.NamesFile)
    .Select(x => x.Trim()).Where(x => x.Length > 0), StringComparer.OrdinalIgnoreCase);
if (names.Count == 0)
    throw new InvalidOperationException("Node name list is empty");
var files = InputFiles(options.Input).ToArray();
var game = GameManager.GetGame("ZZZ") as Mhy ?? throw new InvalidOperationException("ZZZ game definition unavailable");
var hits = new ConcurrentBag<NodeHit>();
var failures = new ConcurrentBag<NodeFailure>();
var processed = 0;
await Parallel.ForEachAsync(files, new ParallelOptions { MaxDegreeOfParallelism = options.Workers }, (path, _) =>
{
    try
    {
        Scan(path, game, names, hits);
    }
    catch (Exception exception)
    {
        failures.Add(new NodeFailure(path, exception.GetType().Name, exception.Message));
    }
    var current = Interlocked.Increment(ref processed);
    if (current % 100 == 0 || current == files.Length)
        Console.WriteLine($"Scanned {current}/{files.Length}, hits {hits.Count}, failures {failures.Count}");
    return ValueTask.CompletedTask;
});
Directory.CreateDirectory(options.Output);
var orderedHits = hits.OrderBy(x => x.Node, StringComparer.OrdinalIgnoreCase)
    .ThenBy(x => x.SourceBlock, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.ContainerOffset).ToArray();
var report = new NodeLocatorReport(
    "zzz-container-node-locator/1",
    options.Input,
    files.Length,
    options.Workers,
    names.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray(),
    orderedHits,
    failures.OrderBy(x => x.SourceBlock, StringComparer.OrdinalIgnoreCase).ToArray());
File.WriteAllText(Path.Combine(options.Output, "node-locations.json"),
    JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Complete: {options.Output}");

static void Scan(string path, Mhy game, HashSet<string> names, ConcurrentBag<NodeHit> hits)
{
    using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1 << 16, FileOptions.SequentialScan);
    if (input.Length < 4)
        return;
    Span<byte> magic = stackalloc byte[4];
    input.ReadExactly(magic);
    input.Position = 0;
    if (!magic.SequenceEqual("mhy1"u8))
        return;
    using var stream = new OffsetStream(input, 0);
    while (stream.Remaining > 0)
    {
        var start = stream.AbsolutePosition;
        stream.Offset = start;
        using var reader = new FileReader(Path.Combine(Path.GetDirectoryName(path)!, start.ToString("X8")), stream, true);
        if (reader.FileType != FileType.MhyFile)
            return;
        var container = new MhyFile(reader, game);
        foreach (var node in container.fileList)
        {
            try
            {
                var name = Path.GetFileName(node.path);
                if (names.Contains(name))
                    hits.Add(new NodeHit(path, start, node.path, node.stream.Length, Sha256(node.stream)));
            }
            finally
            {
                node.stream.Dispose();
            }
        }
        if (stream.AbsolutePosition <= start)
            throw new InvalidDataException($"Container at 0x{start:X} made no progress");
    }
}

static string Sha256(Stream stream)
{
    stream.Position = 0;
    var hash = Convert.ToHexString(SHA256.HashData(stream));
    stream.Position = 0;
    return hash;
}

static IEnumerable<string> InputFiles(string input)
{
    if (File.Exists(input))
    {
        yield return Path.GetFullPath(input);
        yield break;
    }
    var roots = new[]
    {
        Path.Combine(input, "StreamingAssets", "Blocks"),
        Path.Combine(input, "Persistent", "Blocks"),
        input
    }.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase);
    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var root in roots)
    foreach (var file in Directory.EnumerateFiles(root, "*.blk", SearchOption.AllDirectories))
    {
        var full = Path.GetFullPath(file);
        if (seen.Add(full))
            yield return full;
    }
}

sealed record Options(string Input, string Output, string NamesFile, int Workers)
{
    public static Options Parse(string[] args)
    {
        string? input = null;
        string? output = null;
        string? namesFile = null;
        var workers = Math.Clamp(Environment.ProcessorCount / 2, 1, 8);
        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--input": input = args[++index]; break;
                case "--output": output = args[++index]; break;
                case "--names-file": namesFile = args[++index]; break;
                case "--workers": workers = int.Parse(args[++index]); break;
                default: throw new ArgumentException($"Unknown argument {args[index]}");
            }
        }
        if (input is null || output is null || namesFile is null)
            throw new ArgumentException("--input, --output and --names-file are required");
        return new Options(Path.GetFullPath(input), Path.GetFullPath(output), Path.GetFullPath(namesFile), workers);
    }
}

sealed record NodeHit(string SourceBlock, long ContainerOffset, string Node, long NodeBytes, string Sha256);
sealed record NodeFailure(string SourceBlock, string ErrorType, string Message);
sealed record NodeLocatorReport(string Schema, string Input, int BlockCount, int Workers, string[] NodeNames,
    NodeHit[] Hits, NodeFailure[] Failures);
