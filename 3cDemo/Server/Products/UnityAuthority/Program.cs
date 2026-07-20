using Fantasy;
using ThirdPerson.Server.Host;
using ThirdPersonSimulation.UnityAuthority;

try
{
    var product = new ServerHostProductDefinition(
        UnityAuthorityHostProduct.ServerProductId,
        new ServerAuthorityHostProductDescriptor(
            UnityAuthorityHostProduct.ProductId.Value,
            UnityAuthorityHostProduct.Descriptor.RouteKind.ToString(),
            UnityAuthorityHostProduct.Descriptor.LaunchKind,
            UnityAuthorityHostProduct.Descriptor.ManifestSchemaVersion,
            UnityAuthorityHostProduct.Descriptor.AuthoritySolverId.Value,
            UnityAuthorityHostProduct.Descriptor.AuthoritySolverVersion,
            (ulong)UnityAuthorityHostProduct.Descriptor.AuthoritySolverCapabilities,
            (ulong)UnityAuthorityHostProduct.Descriptor.AuthoritySolverFeatures,
            UnityAuthorityHostProduct.Descriptor.DescriptorHash.ToString()),
        "ThirdPerson.UnityAuthority.Server.exe",
        "Fantasy.config",
        new[]
        {
            new ServerEntityModuleDescriptor("thirdperson.server.gate.entity", typeof(GateEntityModuleMarker)),
            new ServerEntityModuleDescriptor("thirdperson.server.unity-authority.entity", typeof(UnityAuthorityEntityModuleMarker))
        },
        new[]
        {
            new ServerHotfixModuleDescriptor("thirdperson.server.gate.hotfix", "ThirdPerson.Server.Gate.Hotfix.dll", "ThirdPerson.Server.Gate.Hotfix.pdb", 100),
            new ServerHotfixModuleDescriptor("thirdperson.server.unity-authority.hotfix", "ThirdPerson.Server.UnityAuthority.Hotfix.dll", "ThirdPerson.Server.UnityAuthority.Hotfix.pdb", 200)
        },
        new[] { "Gate" },
        Array.Empty<string>(),
        new[]
        {
            "thirdperson.server.dotrecast-authority.entity",
            "thirdperson.server.dotrecast-authority.hotfix",
            "ThirdPerson.Server.DotRecastAuthority.Entity",
            "ThirdPerson.Server.DotRecastAuthority.Hotfix",
            "ThirdPersonSimulation.DotRecast",
            "ThirdPersonSimulation.DotRecastAuthority"
        },
        "Authority",
        Array.Empty<ServerProductArtifactDescriptor>(),
        () =>
        {
            ServerAuthoritativeAuthorityHostRouteAdapterRegistry.Install(new UnityAuthorityHostRouteAdapter());
            if (!string.Equals(
                    ServerAuthoritativeAuthorityHostRouteAdapterRegistry.Adapter.ProductId,
                    UnityAuthorityHostProduct.ServerProductId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Unity Authority route adapter product identity is invalid.");
            }
        });
    if (args.Length > 0 &&
        string.Equals(args[0], "--write-server-product-manifest", StringComparison.Ordinal))
    {
        if (args.Length != 2)
            throw new InvalidOperationException("Unity Authority Server manifest command requires exactly one BuildId.");
        Console.WriteLine(ServerProductBuildManifestWriter.Write(AppContext.BaseDirectory, args[1], product));
        return;
    }
    await ServerHostBootstrap.RunAsync(product);
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Unity Authority Server failed to start: {exception}");
    Environment.ExitCode = 1;
}
