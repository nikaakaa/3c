using System;
using ThirdPerson.NetworkTest.Contracts;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonSimulation.DotRecastAuthority;
using UnityEditor;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal sealed class DotRecastAuthorityNetworkTestProductAdapter : INetworkTestProductBuildAdapter
    {
        const string BuildProfilePath = "Assets/Configs/Simulation/ServerAuthoritative/DotRecastAuthorityNetworkTestBuildProfile.asset";
        const string ServerConfiguration = "Debug";
        static readonly string[] Scenes =
        {
            "Assets/Scenes/ServerAuthoritative/ServerAuthoritativeNetworkTestBootstrap.unity",
            "Assets/Scenes/ServerAuthoritative/DotRecastAuthorityClient.unity"
        };

        public string ProductId => "thirdperson.network-test.dotrecast-authority";
        public string DisplayName => "DotRecast Authority Network Test";
        public string OutputDirectoryName => "DotRecastAuthority";
        public string PlayerBuildWorkspaceDirectoryName => ".w-da";
        public string ManifestFileName => "NetworkTestProduct.json";

        public void PrepareBuildInputs(NetworkTestProductContext context)
        {
            DotRecastAuthorityNetworkTestBuildProfile profile = LoadBuildProfile();
            DotRecastAuthoritySceneManifestExportRequest export = profile.BuildExportRequest(
                Path.Combine(context.ProductRoot, "Server"));
            CharacterSimulationBuildResult result = CharacterSimulationBuildOrchestrator.Build(
                new CharacterSimulationBuildRequest(
                    export.CharacterDefinition,
                    CharacterSimulationBuildPublicationMode.Publish,
                    new[] { CharacterSimulationTargetCatalog.Float32(export.CharacterDefinition) }));
            if (!result.IsValid)
                throw new InvalidOperationException("DotRecast Authority Float32 Character target failed to build.");
        }

        public NetworkTestProductDescriptor CreateDescriptor(NetworkTestProductContext context)
        {
            DotRecastAuthorityNetworkTestBuildProfile profile = LoadBuildProfile();
            DotRecastAuthoritySceneManifestExportRequest export = profile.BuildExportRequest(
                Path.Combine(context.ProductRoot, "Server"));
            ThirdPersonSimulation.CharacterSimulationProgram program = export.CharacterDefinition.SimulationProgram.Load();
            int controlPort = profile.Endpoint.Port;
            int dataPort = profile.DataPort;
            if (controlPort == dataPort)
                throw new InvalidOperationException($"DotRecast Authority control and data ports conflict: {controlPort}.");
            return new NetworkTestProductDescriptor(
                ProductId,
                DisplayName,
                OutputDirectoryName,
                ManifestFileName,
                Scenes,
                BuildTarget.StandaloneWindows64,
                BuildTargetGroup.Standalone,
                BuildOptions.Development | BuildOptions.StrictMode,
                "Development, StrictMode",
                ScriptingImplementation.Mono2x,
                "3cDemo/Tools/ServerAuthoritative/Start-DotRecastAuthorityDemo.ps1",
                NetworkTestProductAdapterUtility.ProgramIdentity(program),
                export.AuthoritySource.Requirements.RequiredPipelineId.Value,
                "thirdperson.network-model.server-authoritative-hybrid",
                "thirdperson.runtime-topology.dotrecast-authority.three-process.v1",
                "unity-client-player",
                "thirdperson.network-test.dotrecast-authority.player",
                new[]
                {
                    NetworkTestProductAdapterUtility.Field(
                        "controlPort",
                        controlPort.ToString(CultureInfo.InvariantCulture)),
                    NetworkTestProductAdapterUtility.Field(
                        "authorityDataPort",
                        dataPort.ToString(CultureInfo.InvariantCulture))
                },
                new[]
                {
                    NetworkTestProductAdapterUtility.SessionRole(
                        "fantasy-server", "RuntimeArtifact", "dotrecast-authority-server", true,
                        "Hidden", "adapter:udp-ready", Array.Empty<string>(), Array.Empty<string>()),
                    NetworkTestProductAdapterUtility.SessionRole(
                        "client-a", "RuntimeArtifact", "unity-client-player", true,
                        "Visible", "adapter:udp-ready", new[] { "fantasy-server" }, Array.Empty<string>(), "client-a"),
                    NetworkTestProductAdapterUtility.SessionRole(
                        "client-b", "RuntimeArtifact", "unity-client-player", true,
                        "Visible", "adapter:udp-ready", new[] { "client-a" }, Array.Empty<string>(), "client-b")
                },
                new[] { "default" },
                null);
        }

        public IReadOnlyList<NetworkTestRuntimeArtifactResult> PublishAdditionalArtifacts(
            NetworkTestProductContext context,
            NetworkTestProductDescriptor descriptor,
            string productRoot,
            string candidateId)
        {
            string serverDirectory = Path.Combine(productRoot, "Server");
            string project = Path.Combine(
                context.RepositoryRoot,
                "3cDemo",
                "Server",
                "Products",
                "DotRecastAuthority",
                "ThirdPerson.DotRecastAuthority.Server.csproj");
            Directory.CreateDirectory(serverDirectory);
            context.Processes.ExecuteDotNetBuild(
                ProductId,
                $"publish {NetworkTestExternalProcessExecutor.Quote(project)} --configuration {ServerConfiguration} --output {NetworkTestExternalProcessExecutor.Quote(serverDirectory)}",
                context.RepositoryRoot);
            NetworkTestProductAdapterUtility.PublishFantasyConfig(project, serverDirectory);
            RequireServerConfiguration(Path.Combine(serverDirectory, "Fantasy.config"));
            DotRecastAuthorityNetworkTestBuildProfile profile = LoadBuildProfile();
            LoadedDotRecastAuthoritySceneManifest loaded =
                DotRecastAuthoritySceneManifestExporter.Export(profile.BuildExportRequest(serverDirectory));
            NetworkTestRuntimeArtifactResult server = ServerProductBuildManifestUtility.Write(
                context,
                serverDirectory,
                candidateId,
                "dotrecast-authority-server",
                ServerConfiguration,
                DotRecastAuthorityHostProduct.ServerProductId,
                "ThirdPerson.DotRecastAuthority.Server.exe",
                new[]
                {
                    NetworkTestProductAdapterUtility.Field(
                        "authorityManifestHash",
                        loaded.Manifest.ManifestHash.ToString()),
                    NetworkTestProductAdapterUtility.Field(
                        "authorityManifestFileHash",
                        NetworkTestArtifactFileUtility.Sha256(Path.Combine(
                            serverDirectory,
                            DotRecastAuthoritySceneManifest.PublishDirectoryName,
                            DotRecastAuthoritySceneManifest.FileName)))
                });
            return new[] { server };
        }

        public void ValidateProduct(
            NetworkTestProductContext context,
            NetworkTestProductDescriptor descriptor,
            NetworkTestProductBuildManifest manifest)
        {
            NetworkTestProductAdapterUtility.RequireManagedArtifact(
                manifest,
                "dotrecast-authority-server",
                DotRecastAuthorityHostProduct.ServerProductId,
                context.ProductRoot);
            string serverDirectory = Path.Combine(context.ProductRoot, "Server");
            RequireServerConfiguration(Path.Combine(serverDirectory, "Fantasy.config"));
            string authorityManifest = Path.Combine(
                serverDirectory,
                DotRecastAuthoritySceneManifest.PublishDirectoryName,
                DotRecastAuthoritySceneManifest.FileName);
            string expectedHash = RequireField(
                NetworkTestProductBuildWorkflow.RequireArtifact(manifest, "dotrecast-authority-server").fields,
                "authorityManifestHash");
            LoadedDotRecastAuthoritySceneManifest loaded = DotRecastAuthoritySceneManifestLoader.LoadFile(authorityManifest);
            if (!string.Equals(loaded.Manifest.ManifestHash.ToString(), expectedHash, StringComparison.Ordinal))
                throw new InvalidOperationException("DotRecast Authority manifest hash does not match the Network Test Product manifest.");
        }

        static DotRecastAuthorityNetworkTestBuildProfile LoadBuildProfile() =>
            NetworkTestProductAdapterUtility.RequireAsset<DotRecastAuthorityNetworkTestBuildProfile>(BuildProfilePath);

        static void RequireServerConfiguration(string path)
        {
            XDocument document = XDocument.Load(path);
            XNamespace ns = document.Root?.Name.Namespace ??
                throw new InvalidOperationException("DotRecast Fantasy.config has no root element.");
            XElement[] scenes = document.Descendants(ns + "scene").ToArray();
            bool hasGate = scenes.Count(value => (string)value.Attribute("sceneTypeString") == "Gate") == 1;
            bool hasAuthority = scenes.Count(value => (string)value.Attribute("sceneTypeString") == "DotRecastAuthority") == 1;
            if (scenes.Length != 2 || !hasGate || !hasAuthority)
                throw new InvalidOperationException("DotRecast Fantasy.config must contain exactly the Gate and DotRecastAuthority Scenes.");
        }

        static string RequireField(NetworkTestProductManifestField[] source, string key)
        {
            NetworkTestProductManifestField field = (source ?? Array.Empty<NetworkTestProductManifestField>()).SingleOrDefault(value =>
                value != null && string.Equals(value.key, key, StringComparison.Ordinal));
            return field == null || string.IsNullOrWhiteSpace(field.value)
                ? throw new InvalidOperationException($"DotRecast Authority Product manifest field '{key}' is missing.")
                : field.value;
        }
    }

    public static class DotRecastAuthorityNetworkTestBuildAndRun
    {
        public static void Build(string candidateLabel) => NetworkTestProductBuildWorkflow.Build(
            new NetworkTestProductBuildRequest(NetworkTestProductAdapters.DotRecastAuthority, candidateLabel));

        public static void Run(string candidateId) => NetworkTestProductBuildWorkflow.Run(
            new NetworkTestProductRunRequest(NetworkTestProductAdapters.DotRecastAuthority, candidateId, "default"));
    }
}
