using System;
using ThirdPerson.NetworkTest.Contracts;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.DeterministicRollback;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using ThirdPersonSimulation;
using ThirdPersonSimulation.DeterministicRollback;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    public static class DeterministicRollbackNetworkTestBuildAndRun
    {
        internal const string DefinitionPath = "Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset";
        const string ConfigDirectory = "Assets/Configs/Simulation/DeterministicRollback";
        const string BackendPath = ConfigDirectory + "/Pipelines/CorinFixedPassBackend.asset";
        internal const string PipelinePath = ConfigDirectory + "/Pipelines/CorinDeterministicRollbackPipeline.asset";
        const string CollisionPath = ConfigDirectory + "/World/CorinDeterministicCollisionWorld.asset";
        const string SolverPath = ConfigDirectory + "/World/CorinDeterministicKcc.asset";
        internal const string EndpointPath = ConfigDirectory + "/Networking/CorinRollbackEndpoint.asset";
        internal const string SourcePath = ConfigDirectory + "/Networking/CorinRollbackSessionSource.asset";
        const string CompositionPath = ConfigDirectory + "/Compositions/CorinRollbackComposition.asset";
        const string RuntimeRootPrefabPath = "Assets/Prefabs/GameplayLab/GameplayLabDeterministicRollback.prefab";
        const string DebugScenePath = "Assets/Scenes/GameplayLab/GameplayLab.unity";
        internal const string SessionId = "corin-deterministic-rollback-demo";
        const string MapId = "deterministic-rollback-demo";

        internal static readonly string[] BuildScenes = { DebugScenePath };

        public static void PrepareAssetsAndScenes()
        {
            RequireEditorIdle();
            PrepareBuildInputs();
        }

        internal static void PrepareBuildInputs()
        {
            DeterministicRollbackProductClosure closure = RequireProductClosure();
            ValidateSharedDebugScene(closure);
            Debug.Log(
                $"Deterministic Rollback inputs are closed. Content={closure.GameplayContentHash}; " +
                $"World={closure.Collision.ContentHash}; KCC={closure.KccIdentityHash}");
        }

        public static void Build(string candidateLabel) => NetworkTestProductBuildWorkflow.Build(
            new NetworkTestProductBuildRequest(NetworkTestProductAdapters.DeterministicRollback, candidateLabel));

        public static void Run(string candidateId, string slotId) => NetworkTestProductBuildWorkflow.Run(
            new NetworkTestProductRunRequest(NetworkTestProductAdapters.DeterministicRollback, candidateId, slotId));

        [MenuItem("Tools/3C/Internal/Prepare Deterministic Rollback")]
        static void PrepareFromInternalMenu() => PrepareAssetsAndScenes();

        internal static DeterministicRollbackProductClosure RequireProductClosure()
        {
            CharacterPipelineDefinition definition = NetworkTestProductAdapterUtility.RequireAsset<CharacterPipelineDefinition>(
                DefinitionPath);
            SimulationSessionCompositionDefinition composition =
                NetworkTestProductAdapterUtility.RequireAsset<SimulationSessionCompositionDefinition>(CompositionPath);
            FixedPassExecutionBackendDefinition backend =
                NetworkTestProductAdapterUtility.RequireAsset<FixedPassExecutionBackendDefinition>(BackendPath);
            DeterministicRollbackPipelineDefinition pipeline =
                NetworkTestProductAdapterUtility.RequireAsset<DeterministicRollbackPipelineDefinition>(PipelinePath);
            DeterministicCollisionWorldAsset collision =
                NetworkTestProductAdapterUtility.RequireAsset<DeterministicCollisionWorldAsset>(CollisionPath);
            DeterministicKccWorldSolverDefinition solver =
                NetworkTestProductAdapterUtility.RequireAsset<DeterministicKccWorldSolverDefinition>(SolverPath);
            RollbackEndpointAuthoringDefinition endpoint =
                NetworkTestProductAdapterUtility.RequireAsset<RollbackEndpointAuthoringDefinition>(EndpointPath);
            DeterministicRollbackSessionSourceDefinition source =
                NetworkTestProductAdapterUtility.RequireAsset<DeterministicRollbackSessionSourceDefinition>(SourcePath);
            GameObject runtimeRootPrefab =
                NetworkTestProductAdapterUtility.RequireAsset<GameObject>(RuntimeRootPrefabPath);

            composition.RequireComplete();
            if (!string.Equals(composition.SessionId, SessionId, StringComparison.Ordinal) ||
                !string.Equals(composition.MapId, MapId, StringComparison.Ordinal) ||
                composition.TickRate != definition.SimulationTickRate ||
                composition.ExecutionBackend != backend ||
                composition.Pipeline != pipeline ||
                composition.SessionSource != source ||
                composition.WorldSolver != solver)
            {
                throw new InvalidOperationException("Rollback Session Composition does not match the formal domain assets.");
            }
            if (source.Pipeline != pipeline || source.WorldSolver != solver || source.Endpoint != endpoint ||
                solver.CollisionWorld != collision)
            {
                throw new InvalidOperationException("Rollback Session Source contains split Pipeline, KCC, Collision or Endpoint references.");
            }

            DeterministicRollbackModelDefinition model = source.BuildModelDefinition();
            string kccIdentityHash = solver.BuildKccIdentityHash(composition.TickRate).Value;
            if (model.TickRate != composition.TickRate ||
                !string.Equals(model.CollisionWorldHash.Value, collision.ContentHash, StringComparison.Ordinal) ||
                !string.Equals(model.KccIdentityHash.Value, kccIdentityHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Rollback Model identity does not match the formal world closure.");
            }
            if (!definition.InputProfile || !definition.PresentationProjection)
                throw new InvalidOperationException("Rollback Character Definition requires its Input Profile and Presentation Projection.");

            StableHash gameplayContentHash = NetworkTestProductAdapterUtility.FixedCharacterContentHash(definition);
            StableHash stateSchemaHash = NetworkTestProductAdapterUtility.FixedCharacterStateSchemaHash(definition);
            SimulationSessionHost session = RequireSingle(
                runtimeRootPrefab.GetComponentsInChildren<SimulationSessionHost>(true),
                "Rollback runtime root requires exactly one SimulationSessionHost.");
            RequireObjectReference(session, "m_Composition", composition);
            DeterministicRollbackCharacterHost[] actors = runtimeRootPrefab
                .GetComponentsInChildren<DeterministicRollbackCharacterHost>(true)
                .OrderBy(value => value.ActorId.Value, StringComparer.Ordinal)
                .ToArray();
            if (actors.Length != 2 ||
                !string.Equals(actors[0].ActorId.Value, "rollback-actor-a", StringComparison.Ordinal) ||
                !string.Equals(actors[1].ActorId.Value, "rollback-actor-b", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Rollback runtime root must contain exactly two formal Rollback Actor Hosts.");
            }
            for (int i = 0; i < actors.Length; i++)
            {
                if (actors[i].SessionHost != session || actors[i].CharacterDefinition != definition)
                    throw new InvalidOperationException("Rollback Actor Host does not target the formal Session or Character Definition.");
                RequireObjectReference(actors[i], "m_Endpoint", endpoint);
                RequireObjectReference(actors[i], "m_InputProfile", definition.InputProfile);
                RequireObjectReference(actors[i], "m_CharacterDefinition", definition);
            }
            if (runtimeRootPrefab.GetComponentsInChildren<CharacterPipelineHost>(true).Length != 0)
                throw new InvalidOperationException("Rollback runtime root contains a legacy CharacterPipelineHost.");

            DeterministicRollbackDemoStatusOverlay overlay = RequireSingle(
                runtimeRootPrefab.GetComponentsInChildren<DeterministicRollbackDemoStatusOverlay>(true),
                "Rollback runtime root requires exactly one diagnostics overlay.");
            SerializedProperty overlayActors = new SerializedObject(overlay).FindProperty("m_Actors") ??
                throw new InvalidOperationException("Rollback diagnostics overlay Actor binding is missing.");
            if (overlayActors.arraySize != actors.Length)
                throw new InvalidOperationException("Rollback diagnostics overlay roster is incomplete.");
            for (int i = 0; i < actors.Length; i++)
            {
                if (overlayActors.GetArrayElementAtIndex(i).objectReferenceValue != actors[i])
                    throw new InvalidOperationException("Rollback diagnostics overlay Actor order is invalid.");
            }

            return new DeterministicRollbackProductClosure(
                runtimeRootPrefab,
                definition,
                composition,
                source,
                pipeline,
                solver,
                collision,
                endpoint,
                model,
                gameplayContentHash,
                stateSchemaHash,
                kccIdentityHash);
        }

        static void ValidateSharedDebugScene(DeterministicRollbackProductClosure closure)
        {
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(DebugScenePath))
                throw new InvalidOperationException($"Rollback Debug Scene is missing: {DebugScenePath}");

            Scene scene = EditorSceneManager.OpenScene(DebugScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                if (!roots.Any(value => string.Equals(value.name, closure.RuntimeRootPrefab.name, StringComparison.Ordinal)))
                    throw new InvalidOperationException("Rollback Debug Scene must contain the formal Rollback runtime root Prefab.");
                SimulationSessionHost session = RequireSingle(
                    roots.SelectMany(value => value.GetComponentsInChildren<SimulationSessionHost>(true)).ToArray(),
                    "Rollback Debug Scene requires exactly one SimulationSessionHost.");
                RequireObjectReference(session, "m_Composition", closure.Composition);
                DeterministicRollbackCharacterHost[] actors = roots
                    .SelectMany(value => value.GetComponentsInChildren<DeterministicRollbackCharacterHost>(true))
                    .OrderBy(value => value.ActorId.Value, StringComparer.Ordinal)
                    .ToArray();
                if (actors.Length != 2 || actors.Any(value => value.SessionHost != session))
                    throw new InvalidOperationException("Rollback Debug Scene Actor Host roster is not bound to its Session Host.");
                if (roots.SelectMany(value => value.GetComponentsInChildren<CharacterPipelineHost>(true)).Any())
                    throw new InvalidOperationException("Rollback Debug Scene contains a legacy CharacterPipelineHost.");
                DeterministicCollisionWorldAuthoring[] worlds = roots
                    .SelectMany(value => value.GetComponentsInChildren<DeterministicCollisionWorldAuthoring>(true))
                    .ToArray();
                if (worlds.Length != 1 || worlds[0].Output != closure.Collision ||
                    !string.Equals(worlds[0].MapId, MapId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Rollback Debug Scene deterministic world binding is stale.");
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        static T RequireSingle<T>(T[] values, string error) where T : UnityEngine.Object
        {
            return values.Length == 1 ? values[0] : throw new InvalidOperationException(error);
        }

        static void RequireObjectReference(Object target, string propertyName, Object expected)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(propertyName) ??
                throw new InvalidOperationException($"Serialized property '{propertyName}' is missing on '{target.GetType().Name}'.");
            if (property.objectReferenceValue != expected)
                throw new InvalidOperationException($"Rollback Scene reference '{propertyName}' is stale on '{target.name}'.");
        }

        static void RequireEditorIdle()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Deterministic Rollback tooling cannot run during Play Mode.");
        }
    }

    internal sealed class DeterministicRollbackProductClosure
    {
        public DeterministicRollbackProductClosure(
            GameObject runtimeRootPrefab,
            CharacterPipelineDefinition definition,
            SimulationSessionCompositionDefinition composition,
            DeterministicRollbackSessionSourceDefinition source,
            DeterministicRollbackPipelineDefinition pipeline,
            DeterministicKccWorldSolverDefinition solver,
            DeterministicCollisionWorldAsset collision,
            RollbackEndpointAuthoringDefinition endpoint,
            DeterministicRollbackModelDefinition model,
            StableHash gameplayContentHash,
            StableHash stateSchemaHash,
            string kccIdentityHash)
        {
            RuntimeRootPrefab = runtimeRootPrefab;
            Definition = definition;
            Composition = composition;
            Source = source;
            Pipeline = pipeline;
            Solver = solver;
            Collision = collision;
            Endpoint = endpoint;
            Model = model;
            GameplayContentHash = gameplayContentHash;
            StateSchemaHash = stateSchemaHash;
            KccIdentityHash = kccIdentityHash;
        }

        public GameObject RuntimeRootPrefab { get; }
        public CharacterPipelineDefinition Definition { get; }
        public SimulationSessionCompositionDefinition Composition { get; }
        public DeterministicRollbackSessionSourceDefinition Source { get; }
        public DeterministicRollbackPipelineDefinition Pipeline { get; }
        public DeterministicKccWorldSolverDefinition Solver { get; }
        public DeterministicCollisionWorldAsset Collision { get; }
        public RollbackEndpointAuthoringDefinition Endpoint { get; }
        public DeterministicRollbackModelDefinition Model { get; }
        public StableHash GameplayContentHash { get; }
        public StableHash StateSchemaHash { get; }
        public string GameplayContentIdentity => $"character-content={GameplayContentHash}";
        public string KccIdentityHash { get; }
    }

    internal sealed class DeterministicRollbackNetworkTestProductAdapter : INetworkTestProductBuildAdapter
    {
        public string ProductId => "thirdperson.network-test.deterministic-rollback";
        public string DisplayName => "Deterministic Rollback Network Test";
        public string OutputDirectoryName => "DeterministicRollback";
        public string PlayerBuildWorkspaceDirectoryName => ".w-dr";
        public string ManifestFileName => "NetworkTestProduct.json";

        public void PrepareBuildInputs(NetworkTestProductContext context)
        {
            DeterministicRollbackNetworkTestBuildAndRun.PrepareBuildInputs();
        }

        public NetworkTestProductDescriptor CreateDescriptor(NetworkTestProductContext context)
        {
            DeterministicRollbackProductClosure closure =
                DeterministicRollbackNetworkTestBuildAndRun.RequireProductClosure();
            DeterministicRollbackModelDefinition model = closure.Model;
            SimulationWorldSolverDefinitionDescriptor solverIdentity =
                closure.Solver.BuildDescriptor(closure.Composition.TickRate);
            return new NetworkTestProductDescriptor(
                ProductId,
                DisplayName,
                OutputDirectoryName,
                ManifestFileName,
                DeterministicRollbackNetworkTestBuildAndRun.BuildScenes,
                BuildTarget.StandaloneWindows64,
                BuildTargetGroup.Standalone,
                BuildOptions.Development | BuildOptions.StrictMode,
                "Development, StrictMode",
                ScriptingImplementation.IL2CPP,
                "3cDemo/Tools/DeterministicRollback/Start-DeterministicRollbackDemo.ps1",
                closure.GameplayContentIdentity,
                closure.Pipeline.BuildPortableDescriptor().PipelineId.Value,
                model.ModelIdentity.ToString(),
                RollbackGmProductBuild.Topology,
                "unity-client-player",
                "thirdperson.network-test.deterministic-rollback.player",
                new[]
                {
                    NetworkTestProductAdapterUtility.Field("gmProfileHash", AssetDatabase.GetAssetDependencyHash(RollbackGmProductBuild.ProfilePath).ToString()),
                    NetworkTestProductAdapterUtility.Field("gameplayContentHash", closure.GameplayContentHash.Value),
                    NetworkTestProductAdapterUtility.Field("collisionWorldHash", closure.Collision.ContentHash),
                    NetworkTestProductAdapterUtility.Field("presentationContractHash", closure.Definition.PresentationProjection.ContractHash),
                    NetworkTestProductAdapterUtility.Field("presentationRevision", closure.Definition.PresentationProjection.ProjectionRevision),
                    NetworkTestProductAdapterUtility.Field(
                        "kccId",
                        $"{solverIdentity.Identity.ComponentId}@{closure.KccIdentityHash}"),
                    NetworkTestProductAdapterUtility.Field("kccIdentityHash", closure.KccIdentityHash),
                    NetworkTestProductAdapterUtility.Field("transport", "UDP")
                },
                new[]
                {
                    NetworkTestProductAdapterUtility.SessionRole(
                        "relay", "RuntimeArtifact", "deterministic-relay-server", true,
                        "Hidden", "adapter:udp-http-identity", Array.Empty<string>(),
                        new[] { "rollback-relay", "rollback-relay-query" }),
                    NetworkTestProductAdapterUtility.SessionRole(
                        "gm", "ToolBundle", RollbackGmProductBuild.ToolId, false,
                        "Visible", "adapter:http-identity", new[] { "relay" },
                        new[] { "rollback-gm", "rollback-relay-query" }),
                    NetworkTestProductAdapterUtility.SessionRole(
                        "peer-a", "RuntimeArtifact", "unity-client-player", true,
                        "Visible", "adapter:process-alive", new[] { "relay" },
                        new[] { "rollback-relay", "rollback-peer-a" }, "peer-a"),
                    NetworkTestProductAdapterUtility.SessionRole(
                        "peer-b", "RuntimeArtifact", "unity-client-player", true,
                        "Visible", "adapter:process-alive", new[] { "peer-a" },
                        new[] { "rollback-relay", "rollback-peer-b" }, "peer-b")
                },
                new[] { "rollback-a", "rollback-b" },
                RollbackGmProductBuild.BuildToolBundles);
        }

        public IReadOnlyList<NetworkTestRuntimeArtifactResult> PublishAdditionalArtifacts(
            NetworkTestProductContext context,
            NetworkTestProductDescriptor descriptor,
            string productRoot,
            string candidateId)
        {
            const string serverProductId = "thirdperson.server-product.deterministic-rollback-relay";
            const string serverManifestFileName = "DeterministicRollbackCandidateManifest.json";
            string serverDirectory = Path.Combine(productRoot, "Server");
            string project = Path.Combine(
                context.RepositoryRoot,
                "3cDemo",
                "Server",
                "Products",
                "DeterministicRollback",
                "ThirdPerson.DeterministicRollback.Server.csproj");
            Directory.CreateDirectory(serverDirectory);
            context.Processes.ExecuteDotNetBuild(
                ProductId,
                $"publish {NetworkTestExternalProcessExecutor.Quote(project)} --configuration Debug --output {NetworkTestExternalProcessExecutor.Quote(serverDirectory)}",
                context.RepositoryRoot);

            DeterministicRollbackProductClosure closure =
                DeterministicRollbackNetworkTestBuildAndRun.RequireProductClosure();
            RollbackEndpointAuthoringDefinition endpointAuthoring = closure.Endpoint;
            DeterministicRollbackModelDefinition model = closure.Model;
            RollbackRoster roster = endpointAuthoring.BuildRoster();
            DeterministicRollbackModelPolicy policy = model.Policy;
            var peerManifests = roster.Entries
                .OrderBy(value => value.PeerId, StringComparer.Ordinal)
                .Select(value => new DeterministicRollbackServerPeerManifest
                {
                    peerId = value.PeerId,
                    playerId = value.PlayerId,
                    actorId = value.ActorId.Value
                })
                .ToArray();
            var serverManifest = new DeterministicRollbackServerCandidateManifest
            {
                schemaVersion = DeterministicRollbackServerCandidateManifest.CurrentSchemaVersion,
                candidateId = candidateId,
                productId = serverProductId,
                relayServerPeerId = endpointAuthoring.RelayServerPeerId,
                peers = peerManifests,
                modelId = model.ModelIdentity.ComponentId,
                modelVersion = model.ModelIdentity.SemanticVersion,
                modelConfigurationHash = model.ModelIdentity.ConfigurationHash.Value,
                protocolId = DeterministicRollbackModelIdentity.Protocol.ProtocolId,
                protocolVersion = DeterministicRollbackModelIdentity.Protocol.SemanticVersion,
                protocolSchemaHash = DeterministicRollbackModelIdentity.Protocol.SchemaHash.Value,
                tickRate = model.TickRate,
                gameplayContentHash = closure.GameplayContentHash.Value,
                stateSchemaHash = closure.StateSchemaHash.Value,
                collisionWorldHash = model.CollisionWorldHash.Value,
                kccIdentityHash = model.KccIdentityHash.Value,
                offensiveRequestDelayTicks = policy.OffensiveRequestDelayTicks,
                confirmationDelayTicks = policy.ConfirmationDelayTicks,
                historyLengthTicks = policy.HistoryLengthTicks,
                hashCadenceTicks = policy.HashCadenceTicks,
                maximumRollbackDepthTicks = policy.MaximumRollbackDepthTicks,
                maximumPredictionLeadTicks = policy.MaximumPredictionLeadTicks,
                maximumQueuedBundles = policy.MaximumQueuedBundles,
                maximumQueuedSnapshots = policy.MaximumQueuedSnapshots,
                maximumOutputRecords = policy.MaximumOutputRecords,
                missingInputPolicy = policy.MissingInputPolicy.ToString(),
                snapshotAuthority = policy.SnapshotAuthority.ToString(),
                maximumDatagramBytes = endpointAuthoring.MaximumDatagramBytes,
                maximumQueuedMessages = endpointAuthoring.MaximumQueuedMessages,
                maximumFragmentsPerMessage = endpointAuthoring.MaximumFragmentsPerMessage,
                reliableResendMilliseconds = endpointAuthoring.ReliableResendMilliseconds,
                inputRedundancyCount = endpointAuthoring.InputRedundancyCount
            };
            serverManifest.manifestHash = serverManifest.ValidateAndComputeHash().Value;
            string manifestPath = Path.Combine(serverDirectory, serverManifestFileName);
            File.WriteAllText(
                manifestPath,
                JsonUtility.ToJson(serverManifest, true),
                new System.Text.UTF8Encoding(false));
            serverManifest = JsonUtility.FromJson<DeterministicRollbackServerCandidateManifest>(
                File.ReadAllText(manifestPath, System.Text.Encoding.UTF8));
            serverManifest.RequireValidHash();

            string executable = "ThirdPerson.DeterministicRollback.Server.exe";
            if (!File.Exists(Path.Combine(serverDirectory, executable)))
                throw new InvalidOperationException("Deterministic Rollback Relay Server publish output is missing.");
            NetworkTestRuntimeArtifactResult gm = RollbackGmProductBuild.Publish(context, productRoot);
            return new[]
            {
                new NetworkTestRuntimeArtifactResult(
                    "deterministic-relay-server",
                    NetworkTestRuntimeArtifactKind.ManagedExecutable,
                    serverProductId,
                    "Server",
                    executable,
                    serverManifest.manifestHash,
                    $"Server/{serverManifestFileName}",
                    NetworkTestArtifactFileUtility.Sha256(manifestPath),
                    new[]
                    {
                        NetworkTestProductAdapterUtility.Field("protocol", model.Handshake.Protocol.ToString()),
                        NetworkTestProductAdapterUtility.Field("maximumPredictionLeadTicks", policy.MaximumPredictionLeadTicks.ToString())
                    }),
                gm
            };
        }

        public void ValidateProduct(
            NetworkTestProductContext context,
            NetworkTestProductDescriptor descriptor,
            NetworkTestProductBuildManifest manifest)
        {
            DeterministicRollbackProductClosure closure =
                DeterministicRollbackNetworkTestBuildAndRun.RequireProductClosure();
            if (!string.Equals(
                    descriptor.NetworkModelIdentity,
                    closure.Model.ModelIdentity.ToString(),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Rollback Product model identity changed during Build.");
            }
            NetworkTestRuntimeArtifactManifest relay = NetworkTestProductAdapterUtility.RequireManagedArtifact(
                manifest,
                "deterministic-relay-server",
                "thirdperson.server-product.deterministic-rollback-relay",
                context.ProductRoot);
            string serverManifestPath = Path.Combine(
                context.ProductRoot,
                relay.manifestPath.Replace('/', Path.DirectorySeparatorChar));
            DeterministicRollbackServerCandidateManifest serverManifest =
                JsonUtility.FromJson<DeterministicRollbackServerCandidateManifest>(
                    File.ReadAllText(serverManifestPath, System.Text.Encoding.UTF8));
            serverManifest.RequireValidHash();
            RollbackRoster expectedRoster = closure.Endpoint.BuildRoster();
            RollbackGmProductBuild.Validate(context, manifest);
            RollbackRoster actualRoster = serverManifest.BuildRoster();
            if (!string.Equals(serverManifest.gameplayContentHash, closure.GameplayContentHash.Value, StringComparison.Ordinal) ||
                !string.Equals(serverManifest.stateSchemaHash, closure.StateSchemaHash.Value, StringComparison.Ordinal) ||
                !string.Equals(serverManifest.modelId, closure.Model.ModelIdentity.ComponentId, StringComparison.Ordinal) ||
                !string.Equals(serverManifest.modelVersion, closure.Model.ModelIdentity.SemanticVersion, StringComparison.Ordinal) ||
                !string.Equals(serverManifest.modelConfigurationHash, closure.Model.ModelIdentity.ConfigurationHash.Value, StringComparison.Ordinal) ||
                !string.Equals(serverManifest.collisionWorldHash, closure.Collision.ContentHash, StringComparison.Ordinal) ||
                !string.Equals(serverManifest.kccIdentityHash, closure.KccIdentityHash, StringComparison.Ordinal) ||
                serverManifest.tickRate != closure.Model.TickRate ||
                serverManifest.maximumPredictionLeadTicks != closure.Model.Policy.MaximumPredictionLeadTicks ||
                actualRoster.Entries.Count != expectedRoster.Entries.Count)
            {
                throw new InvalidOperationException("Rollback Relay manifest does not match the formal domain product closure.");
            }
            for (int i = 0; i < expectedRoster.Entries.Count; i++)
            {
                RollbackRosterEntry expected = expectedRoster.Entries[i];
                RollbackRosterEntry actual = actualRoster.Entries[i];
                if (!string.Equals(actual.PeerId, expected.PeerId, StringComparison.Ordinal) ||
                    !string.Equals(actual.PlayerId, expected.PlayerId, StringComparison.Ordinal) ||
                    !actual.ActorId.Equals(expected.ActorId))
                {
                    throw new InvalidOperationException("Rollback Relay and Peer roster identities differ.");
                }
            }
        }
    }
}
