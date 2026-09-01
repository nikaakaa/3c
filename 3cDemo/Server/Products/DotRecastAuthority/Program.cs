using Fantasy;
using ThirdPerson.Server.Host;
using ThirdPersonSimulation.DotRecastAuthority;

try
{
    var product = new ServerHostProductDefinition(
        DotRecastAuthorityHostProduct.ServerProductId,
        new ServerAuthorityHostProductDescriptor(
            DotRecastAuthorityHostProduct.ProductId.Value,
            DotRecastAuthorityHostProduct.Descriptor.RouteKind.ToString(),
            DotRecastAuthorityHostProduct.Descriptor.LaunchKind,
            DotRecastAuthorityHostProduct.Descriptor.ManifestSchemaVersion,
            DotRecastAuthorityHostProduct.Descriptor.AuthoritySolverId.Value,
            DotRecastAuthorityHostProduct.Descriptor.AuthoritySolverVersion,
            (ulong)DotRecastAuthorityHostProduct.Descriptor.AuthoritySolverCapabilities,
            (ulong)DotRecastAuthorityHostProduct.Descriptor.AuthoritySolverFeatures,
            DotRecastAuthorityHostProduct.Descriptor.DescriptorHash.ToString()),
        "ThirdPerson.DotRecastAuthority.Server.exe",
        "Fantasy.config",
        new[]
        {
            new ServerEntityModuleDescriptor("thirdperson.server.gate.entity", typeof(GateEntityModuleMarker)),
            new ServerEntityModuleDescriptor("thirdperson.server.dotrecast-authority.entity", typeof(DotRecastAuthorityEntityModuleMarker))
        },
        new[]
        {
            new ServerHotfixModuleDescriptor("thirdperson.server.gate.hotfix", "ThirdPerson.Server.Gate.Hotfix.dll", "ThirdPerson.Server.Gate.Hotfix.pdb", 100),
            new ServerHotfixModuleDescriptor("thirdperson.server.dotrecast-authority.hotfix", "ThirdPerson.Server.DotRecastAuthority.Hotfix.dll", "ThirdPerson.Server.DotRecastAuthority.Hotfix.pdb", 200)
        },
        new[] { "DotRecastAuthority", "Gate" },
        new[]
        {
            "ThirdPersonSimulation.DotRecast",
            "ThirdPersonSimulation.DotRecastAuthority",
            "ThirdPersonSimulation.ServerAuthoritative",
            "ThirdPersonSimulation.ServerAuthoritative.Transport"
        },
        new[]
        {
            "thirdperson.server.unity-authority.entity",
            "thirdperson.server.unity-authority.hotfix",
            "ThirdPerson.Server.UnityAuthority.Entity",
            "ThirdPerson.Server.UnityAuthority.Hotfix"
        },
        "Authority",
        new[]
        {
            new ServerProductArtifactDescriptor("thirdperson.authority.manifest", "Authority/DotRecastAuthorityScene.manifest"),
            new ServerProductArtifactDescriptor("thirdperson.authority.program", "Authority/Artifacts/CharacterProgram.csim"),
            new ServerProductArtifactDescriptor("thirdperson.authority.navigation", "Authority/Artifacts/NavigationSurface.navsurface")
        },
        () =>
        {
            ServerAuthoritativeAuthorityHostRouteAdapterRegistry.Install(new DotRecastAuthorityHostRouteAdapter());
            if (!string.Equals(
                    ServerAuthoritativeAuthorityHostRouteAdapterRegistry.Adapter.ProductId,
                    DotRecastAuthorityHostProduct.ServerProductId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("DotRecast Authority route adapter product identity is invalid.");
            }
        });
    if (args.Length > 0 &&
        string.Equals(args[0], "--write-server-product-manifest", StringComparison.Ordinal))
    {
        if (args.Length != 2)
            throw new InvalidOperationException("DotRecast Authority Server manifest command requires exactly one CandidateId.");
        Console.WriteLine(ServerProductBuildManifestWriter.Write(AppContext.BaseDirectory, args[1], product));
        return;
    }
    await ServerHostBootstrap.RunAsync(product);
}
catch (Exception exception)
{
    Console.Error.WriteLine($"DotRecast Authority Server failed to start: {exception}");
    Environment.ExitCode = 1;
}
