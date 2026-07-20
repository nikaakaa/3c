using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;

namespace ThirdPerson.Server.Host;

public static class ServerProductBuildManifestWriter
{
    public static string Write(string publishRoot, string buildId, ServerHostProductDefinition product)
    {
        string root = Path.GetFullPath(publishRoot);
        if (string.IsNullOrWhiteSpace(buildId))
            throw new ArgumentException("Server product BuildId is required.", nameof(buildId));
        var manifest = new ServerProductBuildManifest
        {
            SchemaVersion = ServerProductBuildManifestReader.SchemaVersion,
            BuildId = buildId.Trim(),
            ServerProductId = product.ProductId,
            AuthorityHost = product.AuthorityHost == null
                ? null
                : new ServerAuthorityHostProductManifest
                {
                    HostProductId = product.AuthorityHost.HostProductId,
                    RouteKind = product.AuthorityHost.RouteKind,
                    LaunchKind = product.AuthorityHost.LaunchKind,
                    ManifestSchemaVersion = product.AuthorityHost.ManifestSchemaVersion,
                    AuthoritySolverId = product.AuthorityHost.AuthoritySolverId,
                    AuthoritySolverVersion = product.AuthorityHost.AuthoritySolverVersion,
                    AuthoritySolverCapabilities = product.AuthorityHost.AuthoritySolverCapabilities,
                    AuthoritySolverFeatures = product.AuthorityHost.AuthoritySolverFeatures,
                    DescriptorHash = product.AuthorityHost.DescriptorHash
                },
            Executable = Record(root, "server.executable", product.ExecutableName),
            Configuration = Record(root, "server.configuration", product.ConfigurationFileName),
            SceneTypes = ReadSceneTypes(Path.Combine(root, product.ConfigurationFileName)).ToList()
        };
        foreach (ServerEntityModuleDescriptor module in product.EntityModules.OrderBy(value => value.ModuleId, StringComparer.Ordinal))
        {
            string file = $"{module.MarkerType.Assembly.GetName().Name}.dll";
            manifest.EntityModules.Add(Record(root, module.ModuleId, file));
        }
        foreach (ServerHotfixModuleDescriptor module in product.HotfixModules.OrderBy(value => value.LoadOrder))
        {
            ServerProductFileRecord record = Record(root, module.ModuleId, module.AssemblyFileName);
            record.LoadOrder = module.LoadOrder;
            record.PdbRelativePath = module.PdbFileName;
            record.PdbSha256 = Hash(Path.Combine(root, module.PdbFileName));
            manifest.HotfixModules.Add(record);
        }
        HashSet<string> ownedAssemblies = manifest.EntityModules.Concat(manifest.HotfixModules)
            .Select(value => value.RelativePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        string[] actualAssemblies = Directory.GetFiles(root, "*.dll", SearchOption.TopDirectoryOnly)
                     .Select(Path.GetFileName)
                     .Where(value => !string.IsNullOrWhiteSpace(value) && !ownedAssemblies.Contains(value))
                     .Select(value => value!)
                     .OrderBy(value => value, StringComparer.Ordinal)
                     .ToArray();
        string[] runtimeAssemblies = ServerProductDependencyManifestReader.ReadRuntimeAssemblyNames(root, product.ExecutableName);
        string[] allActualAssemblies = Directory.GetFiles(root, "*.dll", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (!allActualAssemblies.SequenceEqual(runtimeAssemblies, StringComparer.OrdinalIgnoreCase))
            throw new InvalidDataException("Server product publish directory does not match its dependency manifest.");
        foreach (string file in actualAssemblies)
        {
            manifest.PortableDependencies.Add(Record(root, Path.GetFileNameWithoutExtension(file), file));
        }
        HashSet<string> dependencies = manifest.PortableDependencies.Select(value => value.ModuleId)
            .ToHashSet(StringComparer.Ordinal);
        if (product.RequiredDependencyIds.Any(value => !dependencies.Contains(value)))
            throw new InvalidDataException("Server product is missing a required dependency.");
        if (product.ForbiddenModuleIds.Any(value => dependencies.Contains(value)))
            throw new InvalidDataException("Server product contains a forbidden dependency.");
        foreach (ServerProductArtifactDescriptor artifact in product.AuthorityArtifacts)
            manifest.AuthorityArtifacts.Add(Record(root, artifact.ArtifactId, artifact.RelativePath));
        ServerProductArtifactClosureValidator.RequireExactFiles(root, product);
        foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                     .Select(value => Path.GetRelativePath(root, value).Replace('\\', '/'))
                     .Where(value => !string.Equals(
                         value,
                         ServerProductBuildManifestReader.FileName,
                         StringComparison.OrdinalIgnoreCase))
                     .OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
        {
            manifest.FileClosure.Add(Record(root, file, file));
        }
        string path = Path.Combine(root, ServerProductBuildManifestReader.FileName);
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        File.WriteAllText(path, JsonSerializer.Serialize(manifest, options) + Environment.NewLine);
        ServerProductBuildManifestReader.LoadAndValidate(root, product);
        return path;
    }

    public static string HashFile(string path) => Hash(path);

    static ServerProductFileRecord Record(string root, string moduleId, string relativePath) => new()
    {
        ModuleId = moduleId,
        RelativePath = relativePath.Replace('\\', '/'),
        Sha256 = Hash(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)))
    };

    static string Hash(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Server product file is missing.", path);
        return Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
    }

    static string[] ReadSceneTypes(string path)
    {
        XDocument document = XDocument.Load(path);
        XNamespace ns = document.Root?.Name.Namespace ?? throw new InvalidDataException("Fantasy.config has no root element.");
        return document.Descendants(ns + "scene")
            .Select(value => (string?)value.Attribute("sceneTypeString"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }
}
