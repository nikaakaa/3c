using Fantasy;
using ThirdPerson.Server.Host;

namespace ThirdPerson.Startup.Server;

internal static class Program
{
    const string ProductId = "thirdperson.startup.server";

    public static async Task Main(string[] args)
    {
        try
        {
            var product = new ServerHostProductDefinition(
                ProductId,
                null,
                "ThirdPerson.Startup.Server.exe",
                "Fantasy.config",
                new[]
                {
                    new ServerEntityModuleDescriptor(
                        "thirdperson.server.startup-auth.entity",
                        typeof(StartupAuthEntityModuleMarker))
                },
                new[]
                {
                    new ServerHotfixModuleDescriptor(
                        "thirdperson.server.startup-auth.hotfix",
                        "ThirdPerson.Server.StartupAuth.Hotfix.dll",
                        "ThirdPerson.Server.StartupAuth.Hotfix.pdb",
                        100)
                },
                new[] { "AuthGateway" },
                new[] { "ThirdPerson.Server.Host" },
                new[]
                {
                    "thirdperson.server.gate.entity",
                    "thirdperson.server.gate.hotfix",
                    "ThirdPerson.Server.Gate.Entity",
                    "ThirdPerson.Server.Gate.Hotfix",
                    "ThirdPerson.Server.UnityAuthority.Entity",
                    "ThirdPerson.Server.UnityAuthority.Hotfix",
                    "ThirdPerson.Server.DotRecastAuthority.Entity",
                    "ThirdPerson.Server.DotRecastAuthority.Hotfix",
                    "ThirdPersonSimulation.Core",
                    "ThirdPersonSimulation.Float32",
                    "ThirdPersonSimulation.ServerAuthoritative",
                    "ThirdPersonSimulation.ServerAuthoritative.Transport",
                    "ThirdPersonSimulation.DotRecast",
                    "ThirdPersonSimulation.DotRecastAuthority"
                },
                "Authority",
                Array.Empty<ServerProductArtifactDescriptor>(),
                StartupServerDeploymentBoundary.Validate);
            if (args.Length > 0 &&
                string.Equals(args[0], "--write-server-product-manifest", StringComparison.Ordinal))
            {
                if (args.Length != 2)
                {
                    throw new InvalidOperationException(
                        "Startup Server manifest command requires exactly one CandidateId.");
                }

                Console.WriteLine(ServerProductBuildManifestWriter.Write(AppContext.BaseDirectory, args[1], product));
                return;
            }

            await ServerHostBootstrap.RunAsync(product);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Startup Server failed to start: {exception}");
            Environment.ExitCode = 1;
        }
    }
}
