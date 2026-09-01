using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace ThirdPerson.Server.Host;

public sealed class ServerProductFileRecord
{
    public string ModuleId { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public int LoadOrder { get; set; }
    public string PdbRelativePath { get; set; } = string.Empty;
    public string PdbSha256 { get; set; } = string.Empty;
}

public sealed class ServerProductBuildManifest
{
    public int SchemaVersion { get; set; }
    public string CandidateId { get; set; } = string.Empty;
    public string ServerProductId { get; set; } = string.Empty;
    public ServerAuthorityHostProductManifest? AuthorityHost { get; set; }
    public ServerProductFileRecord Executable { get; set; } = new();
    public ServerProductFileRecord Configuration { get; set; } = new();
    public List<string> SceneTypes { get; set; } = new();
    public List<ServerProductFileRecord> EntityModules { get; set; } = new();
    public List<ServerProductFileRecord> HotfixModules { get; set; } = new();
    public List<ServerProductFileRecord> PortableDependencies { get; set; } = new();
    public List<ServerProductFileRecord> AuthorityArtifacts { get; set; } = new();
    public List<ServerProductFileRecord> FileClosure { get; set; } = new();
}

public sealed class ServerAuthorityHostProductManifest
{
    public string HostProductId { get; set; } = string.Empty;
    public string RouteKind { get; set; } = string.Empty;
    public string LaunchKind { get; set; } = string.Empty;
    public int ManifestSchemaVersion { get; set; }
    public string AuthoritySolverId { get; set; } = string.Empty;
    public string AuthoritySolverVersion { get; set; } = string.Empty;
    public ulong AuthoritySolverCapabilities { get; set; }
    public ulong AuthoritySolverFeatures { get; set; }
    public string DescriptorHash { get; set; } = string.Empty;
}

internal static class ServerProductBuildManifestReader
{
    public const int SchemaVersion = 3;
    public const string FileName = "ServerProductBuild.json";

    public static ServerProductBuildManifest LoadAndValidate(
        string publishRoot,
        ServerHostProductDefinition product)
    {
        string path = Path.Combine(publishRoot, FileName);
        if (!File.Exists(path))
            throw new FileNotFoundException("Server product build manifest is missing.", path);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };
        ServerProductBuildManifest manifest = JsonSerializer.Deserialize<ServerProductBuildManifest>(File.ReadAllText(path), options) ??
            throw new InvalidDataException("Server product build manifest is empty.");
        if (manifest.SchemaVersion != SchemaVersion ||
            !string.Equals(manifest.ServerProductId, product.ProductId, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Server product build manifest identity is incompatible.");
        }
        ValidateAuthorityHost(manifest.AuthorityHost, product.AuthorityHost);
        if (string.IsNullOrWhiteSpace(manifest.CandidateId))
            throw new InvalidDataException("Server product CandidateId is missing.");
        ValidateRecord(publishRoot, manifest.Executable, product.ExecutableName);
        ValidateRecord(publishRoot, manifest.Configuration, product.ConfigurationFileName);
        if (!string.Equals(manifest.Executable.ModuleId, "server.executable", StringComparison.Ordinal) ||
            !string.Equals(manifest.Configuration.ModuleId, "server.configuration", StringComparison.Ordinal))
        {
            throw new InvalidDataException("Server product executable or configuration identity is invalid.");
        }
        string[] scenes = ReadSceneTypes(Path.Combine(publishRoot, manifest.Configuration.RelativePath));
        RequireExactSet(scenes, product.RequiredSceneTypes, "Scene type");
        RequireExactSet(manifest.SceneTypes, product.RequiredSceneTypes, "manifest Scene type");
        ValidateModules(publishRoot, manifest.EntityModules, product.EntityModules.Select(value => value.ModuleId), "Entity");
        ValidateModules(publishRoot, manifest.HotfixModules, product.HotfixModules.Select(value => value.ModuleId), "Hotfix");
        ValidateModules(
            publishRoot,
            manifest.AuthorityArtifacts,
            product.AuthorityArtifacts.Select(value => value.ArtifactId),
            "Authority artifact");
        foreach (ServerProductFileRecord record in manifest.PortableDependencies)
            ValidateRecord(publishRoot, record, null);
        foreach (ServerProductFileRecord record in manifest.FileClosure)
            ValidateRecord(publishRoot, record, null);
        RequireExactSet(
            manifest.FileClosure.Select(value => Normalize(value.RelativePath)),
            Directory.GetFiles(publishRoot, "*", SearchOption.AllDirectories)
                .Select(value => Normalize(Path.GetRelativePath(publishRoot, value)))
                .Where(value => !string.Equals(value, FileName, StringComparison.OrdinalIgnoreCase)),
            "file closure");
        RequireUniqueRecords(manifest.FileClosure);
        RequireUniqueRecords(
            manifest.EntityModules
                .Concat(manifest.HotfixModules)
                .Concat(manifest.PortableDependencies)
                .Concat(manifest.AuthorityArtifacts));
        foreach (ServerEntityModuleDescriptor descriptor in product.EntityModules)
        {
            ServerProductFileRecord record = manifest.EntityModules.Single(value =>
                string.Equals(value.ModuleId, descriptor.ModuleId, StringComparison.Ordinal));
            string expectedFileName = $"{descriptor.MarkerType.Assembly.GetName().Name}.dll";
            if (!string.Equals(record.RelativePath, expectedFileName, StringComparison.Ordinal))
                throw new InvalidDataException($"Entity module '{descriptor.ModuleId}' does not match the product definition.");
        }
        foreach (ServerHotfixModuleDescriptor descriptor in product.HotfixModules)
        {
            ServerProductFileRecord record = manifest.HotfixModules.Single(value =>
                string.Equals(value.ModuleId, descriptor.ModuleId, StringComparison.Ordinal));
            if (record.LoadOrder != descriptor.LoadOrder ||
                !string.Equals(record.RelativePath, descriptor.AssemblyFileName, StringComparison.Ordinal) ||
                !string.Equals(record.PdbRelativePath, descriptor.PdbFileName, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Hotfix module '{descriptor.ModuleId}' does not match the product definition.");
            }
            ValidateHash(Path.Combine(publishRoot, record.PdbRelativePath), record.PdbSha256);
        }
        foreach (ServerProductArtifactDescriptor descriptor in product.AuthorityArtifacts)
        {
            ServerProductFileRecord record = manifest.AuthorityArtifacts.Single(value =>
                string.Equals(value.ModuleId, descriptor.ArtifactId, StringComparison.Ordinal));
            if (!string.Equals(record.RelativePath, Normalize(descriptor.RelativePath), StringComparison.Ordinal))
                throw new InvalidDataException($"Authority artifact '{descriptor.ArtifactId}' does not match the product definition.");
        }
        ServerProductArtifactClosureValidator.RequireExactFiles(publishRoot, product);
        HashSet<string> dependencyIds = manifest.PortableDependencies
            .Select(value => value.ModuleId)
            .ToHashSet(StringComparer.Ordinal);
        if (product.RequiredDependencyIds.Any(value => !dependencyIds.Contains(value)))
            throw new InvalidDataException("Server product is missing a required portable dependency.");
        HashSet<string> declared = manifest.EntityModules
            .Concat(manifest.HotfixModules)
            .Concat(manifest.PortableDependencies)
            .Select(value => Normalize(value.RelativePath))
            .Append(Normalize(manifest.Executable.RelativePath))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        string[] actualAssemblies = Directory.GetFiles(publishRoot, "*.dll", SearchOption.TopDirectoryOnly)
            .Select(value => Normalize(Path.GetRelativePath(publishRoot, value)))
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        RequireExactSet(
            actualAssemblies,
            ServerProductDependencyManifestReader.ReadRuntimeAssemblyNames(publishRoot, product.ExecutableName),
            "runtime assembly");
        if (actualAssemblies.Any(value => !declared.Contains(value)))
            throw new InvalidDataException("Server product directory contains undeclared assemblies.");
        foreach (string forbidden in product.ForbiddenModuleIds)
        {
            if (manifest.EntityModules.Concat(manifest.HotfixModules).Concat(manifest.PortableDependencies)
                .Any(value => string.Equals(value.ModuleId, forbidden, StringComparison.Ordinal)))
            {
                throw new InvalidDataException($"Server product contains forbidden module '{forbidden}'.");
            }
        }
        return manifest;
    }

    static void ValidateAuthorityHost(
        ServerAuthorityHostProductManifest? manifest,
        ServerAuthorityHostProductDescriptor? expected)
    {
        if (manifest == null && expected == null)
            return;
        if (manifest == null || expected == null ||
            !string.Equals(manifest.HostProductId, expected.HostProductId, StringComparison.Ordinal) ||
            !string.Equals(manifest.RouteKind, expected.RouteKind, StringComparison.Ordinal) ||
            !string.Equals(manifest.LaunchKind, expected.LaunchKind, StringComparison.Ordinal) ||
            manifest.ManifestSchemaVersion != expected.ManifestSchemaVersion ||
            !string.Equals(manifest.AuthoritySolverId, expected.AuthoritySolverId, StringComparison.Ordinal) ||
            !string.Equals(manifest.AuthoritySolverVersion, expected.AuthoritySolverVersion, StringComparison.Ordinal) ||
            manifest.AuthoritySolverCapabilities != expected.AuthoritySolverCapabilities ||
            manifest.AuthoritySolverFeatures != expected.AuthoritySolverFeatures ||
            !string.Equals(manifest.DescriptorHash, expected.DescriptorHash, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Server product Authority Host declaration is incompatible.");
        }
    }

    static void RequireUniqueRecords(IEnumerable<ServerProductFileRecord> records)
    {
        ServerProductFileRecord[] values = records.ToArray();
        if (values.Select(value => value.ModuleId).Distinct(StringComparer.Ordinal).Count() != values.Length)
            throw new InvalidDataException("Server product manifest contains duplicate ModuleId values.");
        if (values.Select(value => Normalize(value.RelativePath)).Distinct(StringComparer.OrdinalIgnoreCase).Count() != values.Length)
            throw new InvalidDataException("Server product manifest contains duplicate file paths.");
    }

    static void ValidateModules(
        string publishRoot,
        IReadOnlyList<ServerProductFileRecord> records,
        IEnumerable<string> expectedIds,
        string label)
    {
        RequireExactSet(records.Select(value => value.ModuleId), expectedIds, $"{label} ModuleId");
        foreach (ServerProductFileRecord record in records)
            ValidateRecord(publishRoot, record, null);
    }

    static void ValidateRecord(string root, ServerProductFileRecord record, string? expectedFileName)
    {
        if (record == null || string.IsNullOrWhiteSpace(record.ModuleId) ||
            string.IsNullOrWhiteSpace(record.RelativePath) || string.IsNullOrWhiteSpace(record.Sha256))
        {
            throw new InvalidDataException("Server product file record is incomplete.");
        }
        string normalized = Normalize(record.RelativePath);
        if (Path.IsPathFullyQualified(normalized) || normalized.StartsWith("../", StringComparison.Ordinal))
            throw new InvalidDataException("Server product file record escapes the publish root.");
        if (expectedFileName != null && !string.Equals(normalized, expectedFileName, StringComparison.Ordinal))
            throw new InvalidDataException($"Server product expected '{expectedFileName}' but manifest selected '{normalized}'.");
        ValidateHash(Path.Combine(root, normalized), record.Sha256);
    }

    static void ValidateHash(string path, string expected)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Server product file is missing.", path);
        string actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
            throw new InvalidDataException($"Server product file hash mismatch: {path}");
    }

    static string[] ReadSceneTypes(string configPath)
    {
        XDocument document = XDocument.Load(configPath);
        XNamespace ns = document.Root?.Name.Namespace ?? throw new InvalidDataException("Fantasy.config has no root element.");
        return document.Descendants(ns + "scene")
            .Select(value => (string?)value.Attribute("sceneTypeString"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }

    static void RequireExactSet(IEnumerable<string> actual, IEnumerable<string> expected, string label)
    {
        string[] left = actual.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        string[] right = expected.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        if (!left.SequenceEqual(right, StringComparer.Ordinal))
            throw new InvalidDataException($"Server product {label} set does not match its definition.");
    }

    static string Normalize(string value) => value.Replace('\\', '/');
}

internal static class ServerProductArtifactClosureValidator
{
    public static void RequireExactFiles(string publishRoot, ServerHostProductDefinition product)
    {
        string relativeDirectory = Normalize(product.AuthorityArtifactDirectory).Trim('/');
        string directory = Path.Combine(publishRoot, relativeDirectory.Replace('/', Path.DirectorySeparatorChar));
        string[] actual = Directory.Exists(directory)
            ? Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                .Select(value => Normalize(Path.GetRelativePath(publishRoot, value)))
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : Array.Empty<string>();
        string[] expected = product.AuthorityArtifacts
            .Select(value => Normalize(value.RelativePath))
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (!actual.SequenceEqual(expected, StringComparer.OrdinalIgnoreCase))
            throw new InvalidDataException("Server product Authority artifact directory does not match its definition.");
    }

    static string Normalize(string value) => value.Replace('\\', '/');
}

internal static class ServerProductDependencyManifestReader
{
    public static string[] ReadRuntimeAssemblyNames(string publishRoot, string executableName)
    {
        string path = Path.Combine(publishRoot, Path.ChangeExtension(executableName, ".deps.json"));
        if (!File.Exists(path))
            throw new FileNotFoundException("Server product dependency manifest is missing.", path);
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("targets", out JsonElement targets) ||
            targets.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("Server product dependency manifest has no targets.");
        }
        var assemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (JsonProperty target in targets.EnumerateObject())
        {
            foreach (JsonProperty library in target.Value.EnumerateObject())
            {
                AddRuntimeAssets(library.Value, "runtime", assemblies);
                AddRuntimeAssets(library.Value, "runtimeTargets", assemblies);
            }
        }
        if (assemblies.Count == 0)
            throw new InvalidDataException("Server product dependency manifest contains no runtime assemblies.");
        return assemblies.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    static void AddRuntimeAssets(JsonElement library, string propertyName, ISet<string> assemblies)
    {
        if (!library.TryGetProperty(propertyName, out JsonElement assets) || assets.ValueKind != JsonValueKind.Object)
            return;
        foreach (JsonProperty asset in assets.EnumerateObject())
        {
            if (!asset.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                continue;
            string fileName = Path.GetFileName(asset.Name.Replace('/', Path.DirectorySeparatorChar));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new InvalidDataException("Server product dependency manifest contains an invalid runtime asset path.");
            assemblies.Add(fileName);
        }
    }
}
