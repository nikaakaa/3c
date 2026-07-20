namespace ThirdPerson.Server.Host;

public sealed record ServerEntityModuleDescriptor(string ModuleId, Type MarkerType);

public sealed record ServerHotfixModuleDescriptor(
    string ModuleId,
    string AssemblyFileName,
    string PdbFileName,
    int LoadOrder);

public sealed record ServerProductArtifactDescriptor(string ArtifactId, string RelativePath);

public sealed record ServerAuthorityHostProductDescriptor(
    string HostProductId,
    string RouteKind,
    string LaunchKind,
    int ManifestSchemaVersion,
    string AuthoritySolverId,
    string AuthoritySolverVersion,
    ulong AuthoritySolverCapabilities,
    ulong AuthoritySolverFeatures,
    string DescriptorHash);

public sealed class ServerHostProductDefinition
{
    public ServerHostProductDefinition(
        string productId,
        ServerAuthorityHostProductDescriptor? authorityHost,
        string executableName,
        string configurationFileName,
        IReadOnlyList<ServerEntityModuleDescriptor> entityModules,
        IReadOnlyList<ServerHotfixModuleDescriptor> hotfixModules,
        IReadOnlyList<string> requiredSceneTypes,
        IReadOnlyList<string> requiredDependencyIds,
        IReadOnlyList<string> forbiddenModuleIds,
        string authorityArtifactDirectory,
        IReadOnlyList<ServerProductArtifactDescriptor> authorityArtifacts,
        Action installProductRuntime)
    {
        ProductId = Require(productId, nameof(productId));
        AuthorityHost = authorityHost;
        ExecutableName = Require(executableName, nameof(executableName));
        ConfigurationFileName = Require(configurationFileName, nameof(configurationFileName));
        EntityModules = entityModules?.ToArray() ?? throw new ArgumentNullException(nameof(entityModules));
        HotfixModules = hotfixModules?.OrderBy(value => value.LoadOrder).ToArray() ??
            throw new ArgumentNullException(nameof(hotfixModules));
        RequiredSceneTypes = requiredSceneTypes?.OrderBy(value => value, StringComparer.Ordinal).ToArray() ??
            throw new ArgumentNullException(nameof(requiredSceneTypes));
        RequiredDependencyIds = requiredDependencyIds?.OrderBy(value => value, StringComparer.Ordinal).ToArray() ??
            throw new ArgumentNullException(nameof(requiredDependencyIds));
        ForbiddenModuleIds = forbiddenModuleIds?.OrderBy(value => value, StringComparer.Ordinal).ToArray() ??
            throw new ArgumentNullException(nameof(forbiddenModuleIds));
        AuthorityArtifactDirectory = Require(authorityArtifactDirectory, nameof(authorityArtifactDirectory));
        AuthorityArtifacts = authorityArtifacts?.OrderBy(value => value.ArtifactId, StringComparer.Ordinal).ToArray() ??
            throw new ArgumentNullException(nameof(authorityArtifacts));
        InstallProductRuntime = installProductRuntime ?? throw new ArgumentNullException(nameof(installProductRuntime));
        Validate();
    }

    public string ProductId { get; }
    public ServerAuthorityHostProductDescriptor? AuthorityHost { get; }
    public string ExecutableName { get; }
    public string ConfigurationFileName { get; }
    public IReadOnlyList<ServerEntityModuleDescriptor> EntityModules { get; }
    public IReadOnlyList<ServerHotfixModuleDescriptor> HotfixModules { get; }
    public IReadOnlyList<string> RequiredSceneTypes { get; }
    public IReadOnlyList<string> RequiredDependencyIds { get; }
    public IReadOnlyList<string> ForbiddenModuleIds { get; }
    public string AuthorityArtifactDirectory { get; }
    public IReadOnlyList<ServerProductArtifactDescriptor> AuthorityArtifacts { get; }
    public Action InstallProductRuntime { get; }

    void Validate()
    {
        if (EntityModules.Count == 0 || HotfixModules.Count == 0 || RequiredSceneTypes.Count == 0)
            throw new InvalidOperationException("Server product requires Entity modules, Hotfix modules, and Scene types.");
        if (AuthorityHost != null)
        {
            _ = Require(AuthorityHost.HostProductId, nameof(AuthorityHost.HostProductId));
            _ = Require(AuthorityHost.RouteKind, nameof(AuthorityHost.RouteKind));
            _ = Require(AuthorityHost.LaunchKind, nameof(AuthorityHost.LaunchKind));
            _ = Require(AuthorityHost.AuthoritySolverId, nameof(AuthorityHost.AuthoritySolverId));
            _ = Require(AuthorityHost.AuthoritySolverVersion, nameof(AuthorityHost.AuthoritySolverVersion));
            _ = Require(AuthorityHost.DescriptorHash, nameof(AuthorityHost.DescriptorHash));
            if (AuthorityHost.ManifestSchemaVersion <= 0 || AuthorityHost.AuthoritySolverCapabilities == 0)
                throw new InvalidOperationException("Server product Authority Host declaration is incomplete.");
        }
        else if (AuthorityArtifacts.Count != 0)
        {
            throw new InvalidOperationException("A product without an Authority Host cannot declare Authority artifacts.");
        }
        RequireUnique(EntityModules.Select(value => Require(value.ModuleId, nameof(value.ModuleId))), "Entity ModuleId");
        RequireUnique(HotfixModules.Select(value => Require(value.ModuleId, nameof(value.ModuleId))), "Hotfix ModuleId");
        RequireUnique(HotfixModules.Select(value => Require(value.AssemblyFileName, nameof(value.AssemblyFileName))), "Hotfix assembly");
        RequireUnique(RequiredSceneTypes.Select(value => Require(value, nameof(RequiredSceneTypes))), "Scene type");
        RequireUnique(RequiredDependencyIds.Select(value => Require(value, nameof(RequiredDependencyIds))), "required dependency");
        RequireUnique(ForbiddenModuleIds.Select(value => Require(value, nameof(ForbiddenModuleIds))), "forbidden ModuleId");
        RequireUnique(AuthorityArtifacts.Select(value => Require(value.ArtifactId, nameof(value.ArtifactId))), "Authority artifact");
        RequireUnique(AuthorityArtifacts.Select(value => Require(value.RelativePath, nameof(value.RelativePath))), "Authority artifact path");
        string artifactRoot = NormalizeRelativePath(AuthorityArtifactDirectory);
        if (AuthorityArtifacts.Any(value =>
                !NormalizeRelativePath(value.RelativePath).StartsWith(artifactRoot + "/", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Authority artifact path is outside the product artifact directory.");
        }
        if (EntityModules.Any(value => value.MarkerType == null))
            throw new InvalidOperationException("Entity module marker type is required.");
        if (HotfixModules.Select(value => value.LoadOrder).Distinct().Count() != HotfixModules.Count)
            throw new InvalidOperationException("Hotfix module load order must be unique.");
        HashSet<string> modules = EntityModules.Select(value => value.ModuleId)
            .Concat(HotfixModules.Select(value => value.ModuleId))
            .ToHashSet(StringComparer.Ordinal);
        if (ForbiddenModuleIds.Any(modules.Contains))
            throw new InvalidOperationException("A required server module is also forbidden.");
        if (RequiredDependencyIds.Intersect(ForbiddenModuleIds, StringComparer.Ordinal).Any())
            throw new InvalidOperationException("A required server dependency is also forbidden.");
    }

    static void RequireUnique(IEnumerable<string> values, string label)
    {
        string[] array = values.ToArray();
        if (array.Distinct(StringComparer.Ordinal).Count() != array.Length)
            throw new InvalidOperationException($"Server product contains duplicate {label} values.");
    }

    static string Require(string value, string parameter) => string.IsNullOrWhiteSpace(value)
        ? throw new ArgumentException("Server product value is required.", parameter)
        : value.Trim();

    static string NormalizeRelativePath(string value)
    {
        string normalized = Require(value, nameof(value)).Replace('\\', '/').Trim('/');
        if (Path.IsPathFullyQualified(normalized) || normalized.Split('/').Any(segment => segment == ".."))
            throw new InvalidOperationException("Server product path must stay inside the publish root.");
        return normalized;
    }
}
