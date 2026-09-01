using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonGameplay.Networking.ServerAuthoritative;
using ThirdPersonSimulation;
using ThirdPersonSimulation.ServerAuthoritative;
using ThirdPersonSimulation.UnityAuthority;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal static class NetworkTestProductAdapters
    {
        public static readonly INetworkTestProductBuildAdapter UnityAuthority = new UnityAuthorityNetworkTestProductAdapter();
        public static readonly INetworkTestProductBuildAdapter DotRecastAuthority = new DotRecastAuthorityNetworkTestProductAdapter();
        public static readonly INetworkTestProductBuildAdapter DeterministicRollback = new DeterministicRollbackNetworkTestProductAdapter();

        public static readonly INetworkTestProductBuildAdapter[] All =
        {
            UnityAuthority,
            DotRecastAuthority,
            DeterministicRollback
        };
    }

    internal sealed class UnityAuthorityNetworkTestProductAdapter : INetworkTestProductBuildAdapter
    {
        const string DefinitionPath = "Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset";
        const string EndpointPath = "Assets/Configs/Simulation/ServerAuthoritative/CorinServerAuthoritativeFantasyEndpoint.asset";
        const string LaunchPath = "Assets/Configs/Simulation/ServerAuthoritative/Launches/CorinAuthorityWorkerLaunch.asset";
        const string AuthoritySourcePath = "Assets/Configs/Simulation/ServerAuthoritative/SessionSources/CorinAuthoritySessionSource.asset";
        const string ServerConfiguration = "Debug";
        static readonly string[] Scenes =
        {
            "Assets/Scenes/ServerAuthoritative/ServerAuthoritativeNetworkTestBootstrap.unity",
            "Assets/Scenes/ServerAuthoritative/ServerAuthoritativeClient.unity",
            "Assets/Scenes/ServerAuthoritative/ServerAuthoritativeAuthorityWorker.unity"
        };

        public string ProductId => "thirdperson.network-test.unity-authority";
        public string DisplayName => "Unity Authority Network Test";
        public string OutputDirectoryName => "UnityAuthority";
        public string PlayerBuildWorkspaceDirectoryName => ".w-ua";
        public string ManifestFileName => "NetworkTestProduct.json";

        public void PrepareBuildInputs(NetworkTestProductContext context)
        {
            CharacterPipelineDefinition definition =
                NetworkTestProductAdapterUtility.RequireAsset<CharacterPipelineDefinition>(DefinitionPath);
            CharacterSimulationBuildResult result = CharacterSimulationBuildOrchestrator.Build(
                new CharacterSimulationBuildRequest(
                    definition,
                    CharacterSimulationBuildPublicationMode.Publish,
                    new[] { CharacterSimulationTargetCatalog.Float32(definition) }));
            if (!result.IsValid)
                throw new InvalidOperationException("Unity Authority Float32 Character target failed to build.");
        }

        public NetworkTestProductDescriptor CreateDescriptor(NetworkTestProductContext context)
        {
            CharacterPipelineDefinition definition = NetworkTestProductAdapterUtility.RequireAsset<CharacterPipelineDefinition>(DefinitionPath);
            CharacterSimulationProgram program = definition.SimulationProgram.Load();
            ServerAuthoritativeFantasyEndpointDefinition endpoint = NetworkTestProductAdapterUtility.RequireAsset<ServerAuthoritativeFantasyEndpointDefinition>(EndpointPath);
            ServerAuthoritativeLaunchDefinition launch = NetworkTestProductAdapterUtility.RequireAsset<ServerAuthoritativeLaunchDefinition>(LaunchPath);
            ServerAuthoritativeAuthoritySessionSourceDefinition source = NetworkTestProductAdapterUtility.RequireAsset<ServerAuthoritativeAuthoritySessionSourceDefinition>(AuthoritySourcePath);
            int controlPort = endpoint.Port;
            int dataPort = launch.BuildDataPlaneLaunch().BindEndPoint.Port;
            if (controlPort == dataPort)
                throw new InvalidOperationException($"Unity Authority control and data ports conflict: {controlPort}.");
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
                "3cDemo/Tools/ServerAuthoritative/Start-ServerAuthoritativeDemo.ps1",
                NetworkTestProductAdapterUtility.ProgramIdentity(program),
                source.Requirements.RequiredPipelineId.Value,
                "thirdperson.network-model.server-authoritative-hybrid",
                "thirdperson.runtime-topology.unity-authority.four-process.v1",
                "unity-player",
                "thirdperson.network-test.unity-authority.player",
                new[]
                {
                    NetworkTestProductAdapterUtility.Field("controlPort", controlPort.ToString(CultureInfo.InvariantCulture)),
                    NetworkTestProductAdapterUtility.Field("authorityDataPort", dataPort.ToString(CultureInfo.InvariantCulture))
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
                "UnityAuthority",
                "ThirdPerson.UnityAuthority.Server.csproj");
            Directory.CreateDirectory(serverDirectory);
            context.Processes.ExecuteDotNetBuild(
                ProductId,
                $"publish {NetworkTestExternalProcessExecutor.Quote(project)} --configuration {ServerConfiguration} --output {NetworkTestExternalProcessExecutor.Quote(serverDirectory)}",
                context.RepositoryRoot);
            NetworkTestProductAdapterUtility.PublishFantasyConfig(project, serverDirectory);
            return new[] { ServerProductBuildManifestUtility.Write(
                context,
                serverDirectory,
                candidateId,
                "unity-authority-gate-server",
                ServerConfiguration,
                UnityAuthorityHostProduct.ServerProductId,
                "ThirdPerson.UnityAuthority.Server.exe") };
        }

        public void ValidateProduct(
            NetworkTestProductContext context,
            NetworkTestProductDescriptor descriptor,
            NetworkTestProductBuildManifest manifest)
        {
            NetworkTestProductAdapterUtility.RequireManagedArtifact(
                manifest,
                "unity-authority-gate-server",
                UnityAuthorityHostProduct.ServerProductId,
                context.ProductRoot);
        }
    }

    public static class UnityAuthorityNetworkTestBuildAndRun
    {
        public static void Build(string candidateLabel) => NetworkTestProductBuildWorkflow.Build(
            new NetworkTestProductBuildRequest(NetworkTestProductAdapters.UnityAuthority, candidateLabel));

        public static void Run(string candidateId) => NetworkTestProductBuildWorkflow.Run(
            new NetworkTestProductRunRequest(NetworkTestProductAdapters.UnityAuthority, candidateId, "default"));
    }
}
