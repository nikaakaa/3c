using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.DeterministicRollback;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using ThirdPersonGameplay.Lab;
using ThirdPersonSimulation;
using ThirdPersonSimulation.DeterministicRollback;
using ThirdPersonSimulation.Fixed;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    public static class DeterministicRollbackNetworkTestBuildAndRun
    {
        internal const string DefinitionPath = "Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset";
        const string ConfigDirectory = "Assets/Configs/Simulation/DeterministicRollback";
        internal const string FixedProgramPath = ConfigDirectory + "/Programs/CorinFixedProgram.asset";
        const string FixedRuntimePath = ConfigDirectory + "/Programs/CorinFixedProgramRuntime.asset";
        const string BackendPath = ConfigDirectory + "/Pipelines/CorinFixedPassBackend.asset";
        internal const string PipelinePath = ConfigDirectory + "/Pipelines/CorinDeterministicRollbackPipeline.asset";
        const string CollisionPath = ConfigDirectory + "/World/CorinDeterministicCollisionWorld.asset";
        const string SolverPath = ConfigDirectory + "/World/CorinDeterministicKcc.asset";
        internal const string EndpointPath = ConfigDirectory + "/Networking/CorinRollbackEndpoint.asset";
        internal const string SourcePath = ConfigDirectory + "/Networking/CorinRollbackSessionSource.asset";
        const string CompositionPath = ConfigDirectory + "/Compositions/CorinRollbackComposition.asset";
        const string DebugScenePath = "Assets/Scenes/GameplayLab/GameplayLab.unity";
        const string LocalFixedVariantPath = "Assets/Configs/Simulation/GameplayLab/Variants/GameplayLabLocalFixedVariant.asset";
        const string RollbackVariantPath = "Assets/Configs/Simulation/GameplayLab/Variants/GameplayLabDeterministicRollbackVariant.asset";
        internal const string SessionId = "corin-deterministic-rollback-demo";
        const string MapId = "deterministic-rollback-demo";

        internal static readonly string[] BuildScenes = { DebugScenePath };

        public static void PrepareAssetsAndScenes()
        {
            RequireEditorIdle();
            IGameplayLabLauncherOperations operations = GameplayLabLauncherRegistry.Operations ??
                throw new InvalidOperationException("Gameplay Lab asset synchronization is not registered.");
            operations.SyncAssets();
        }

        internal static void PrepareBuildInputs()
        {
            DeterministicRollbackProductClosure closure = RequireProductClosure();
            ValidateSharedDebugScene(closure);
            Debug.Log(
                $"Deterministic Rollback inputs are closed. Program={closure.Program.ProgramHash}; " +
                $"Projection={closure.Projection.ProjectionRevision}; World={closure.Collision.ContentHash}; " +
                $"KCC={closure.KccIdentityHash}");
        }

        public static void Build() => NetworkTestProductBuildWorkflow.Build(
            new NetworkTestProductBuildRequest(NetworkTestProductAdapters.DeterministicRollback));

        public static void Run() => NetworkTestProductBuildWorkflow.Run(
            new NetworkTestProductRunRequest(NetworkTestProductAdapters.DeterministicRollback, true));

        [MenuItem("Tools/3C/Internal/Prepare Deterministic Rollback")]
        static void PrepareFromInternalMenu() => PrepareAssetsAndScenes();

        [MenuItem("Tools/3C/Internal/Build Deterministic Rollback")]
        static void BuildFromInternalMenu() => Build();

        [MenuItem("Tools/3C/Internal/Run Deterministic Rollback")]
        static void RunFromInternalMenu() => Run();

        internal static DeterministicRollbackProductClosure RequireProductClosure()
        {
            GameplayLabSessionVariantDefinition local =
                NetworkTestProductAdapterUtility.RequireAsset<GameplayLabSessionVariantDefinition>(
                    LocalFixedVariantPath);
            GameplayLabSessionVariantDefinition rollback =
                NetworkTestProductAdapterUtility.RequireAsset<GameplayLabSessionVariantDefinition>(
                    RollbackVariantPath);
            if (local.IsExternalLaunchVariant || !rollback.IsExternalLaunchVariant)
                throw new InvalidOperationException("Rollback Product requires exact Local Fixed and external Rollback Variants.");
            local.ValidateComposition(local.Composition);
            rollback.ValidateComposition(rollback.Composition);
            if (!string.Equals(local.DefinitionGuid, rollback.DefinitionGuid, StringComparison.Ordinal) ||
                local.FixedProgram != rollback.FixedProgram ||
                local.PresentationProjection != rollback.PresentationProjection ||
                local.WorldSolver != rollback.WorldSolver ||
                local.CollisionWorld != rollback.CollisionWorld)
            {
                throw new InvalidOperationException(
                    "Local Fixed and Rollback Variants do not share Program, Projection, KCC and Collision closure.");
            }

            string definitionPath = AssetDatabase.GUIDToAssetPath(rollback.DefinitionGuid);
            if (!string.Equals(definitionPath, DefinitionPath, StringComparison.Ordinal))
                throw new InvalidOperationException($"Rollback Variant targets another Character Definition: {definitionPath}");
            CharacterPipelineDefinition definition =
                NetworkTestProductAdapterUtility.RequireAsset<CharacterPipelineDefinition>(definitionPath);
            if (!definition.InputProfile || definition.PresentationProjection != rollback.PresentationProjection)
                throw new InvalidOperationException("Rollback Variant Character Definition or Projection reference is stale.");

            FixedCharacterSimulationProgramAsset fixedProgram =
                rollback.FixedProgram as FixedCharacterSimulationProgramAsset ??
                throw new InvalidOperationException("Rollback Variant Fixed Program product has the wrong type.");
            ThirdPersonSimulation.Fixed.CharacterSimulationProgram program = fixedProgram.Load();
            CharacterPresentationProjectionAsset projection = rollback.PresentationProjection;
            CharacterPresentationSemanticContract contract =
                FixedCharacterPresentationContractAdapter.Create(program);
            CharacterPresentationProjection publishedProjection = projection.Load(contract);
            ProgramId expectedProgramId = CharacterSemanticFrontendCompiler.ComputeProgramId(definition);
            ProgramRevision expectedSourceRevision = CharacterSemanticFrontendCompiler.ComputeSourceRevision(definition);
            if (!string.Equals(fixedProgram.DefinitionGuid, rollback.DefinitionGuid, StringComparison.Ordinal) ||
                !string.Equals(program.Manifest.ProgramId.Value, expectedProgramId.Value, StringComparison.Ordinal) ||
                !string.Equals(program.Manifest.SourceRevision.Value, expectedSourceRevision.Value, StringComparison.Ordinal) ||
                !string.Equals(publishedProjection.ProgramId, expectedProgramId.Value, StringComparison.Ordinal) ||
                !string.Equals(publishedProjection.SourceRevision, expectedSourceRevision.Value, StringComparison.Ordinal) ||
                !string.Equals(publishedProjection.SemanticHash, program.Manifest.SemanticHash.ToString(), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Rollback Variant Program or Projection is stale against its exact Definition.");
            }
            if (!CharacterPresentationProjectionCompiler.TryComputePublishedRevision(
                    definition,
                    contract,
                    publishedProjection,
                    out string expectedProjectionRevision) ||
                !string.Equals(projection.ProjectionRevision, expectedProjectionRevision, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Rollback Variant Presentation Projection revision is stale.");
            }

            SimulationSessionCompositionDefinition localComposition = local.Composition;
            SimulationSessionCompositionDefinition rollbackComposition = rollback.Composition;
            if (localComposition.ProgramRuntime != rollbackComposition.ProgramRuntime ||
                localComposition.ExecutionBackend != rollbackComposition.ExecutionBackend ||
                localComposition.WorldSolver != rollbackComposition.WorldSolver ||
                localComposition.TickRate != rollbackComposition.TickRate ||
                localComposition.RequiredWorldFeatures != rollbackComposition.RequiredWorldFeatures ||
                !string.Equals(localComposition.WorldId, rollbackComposition.WorldId, StringComparison.Ordinal) ||
                !string.Equals(localComposition.MapId, rollbackComposition.MapId, StringComparison.Ordinal) ||
                !string.Equals(localComposition.WorldRevision, rollbackComposition.WorldRevision, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Local Fixed and Rollback Compositions differ outside Session Source and Network Model assembly.");
            }
            DeterministicRollbackSessionSourceDefinition source =
                rollbackComposition.SessionSource as DeterministicRollbackSessionSourceDefinition ??
                throw new InvalidOperationException("Rollback Variant Composition requires the formal Rollback Session Source.");
            DeterministicRollbackPipelineDefinition pipeline =
                rollbackComposition.Pipeline as DeterministicRollbackPipelineDefinition ??
                throw new InvalidOperationException("Rollback Variant Composition requires the formal Rollback Pipeline.");
            DeterministicKccWorldSolverDefinition solver =
                rollback.WorldSolver as DeterministicKccWorldSolverDefinition ??
                throw new InvalidOperationException("Rollback Variant World Solver product has the wrong type.");
            DeterministicCollisionWorldAsset collision =
                rollback.CollisionWorld as DeterministicCollisionWorldAsset ??
                throw new InvalidOperationException("Rollback Variant Collision product has the wrong type.");
            if (source.FixedProgram != fixedProgram ||
                source.Pipeline != pipeline ||
                source.WorldSolver != solver ||
                solver.CollisionWorld != collision ||
                !string.Equals(collision.MapId, rollbackComposition.MapId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Rollback Variant Composition contains split Program, Pipeline, KCC or Collision references.");
            }
            DeterministicRollbackModelDefinition model = source.BuildModelDefinition();
            string kccIdentityHash = solver.BuildKccIdentityHash(rollbackComposition.TickRate).Value;
            if (!string.Equals(model.SemanticHash.ToString(), fixedProgram.SemanticHash, StringComparison.Ordinal) ||
                !string.Equals(model.FixedProgramHash.ToString(), fixedProgram.ProgramHash, StringComparison.Ordinal) ||
                !string.Equals(model.CollisionWorldHash.Value, collision.ContentHash, StringComparison.Ordinal) ||
                !string.Equals(model.KccIdentityHash.Value, kccIdentityHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Rollback Model identity differs from the shared Variant closure.");
            }
            return new DeterministicRollbackProductClosure(
                local,
                rollback,
                definition,
                fixedProgram,
                program,
                projection,
                publishedProjection,
                rollbackComposition,
                source,
                pipeline,
                solver,
                collision,
                source.Endpoint,
                model,
                kccIdentityHash);
        }

        static void ValidateSharedDebugScene(DeterministicRollbackProductClosure closure)
        {
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(DebugScenePath))
                throw new InvalidOperationException($"Shared Gameplay Debug Scene is missing: {DebugScenePath}");
            GameplayLabSessionVariantDefinition variant = closure.RollbackVariant;
            GameObject root = variant.RuntimeRootPrefab;
            SimulationSessionHost session = root.GetComponentsInChildren<SimulationSessionHost>(true).SingleOrDefault() ??
                throw new InvalidOperationException("Rollback runtime root requires exactly one SimulationSessionHost.");
            RequireObjectReference(session, "m_Composition", closure.Composition);
            DeterministicRollbackCharacterHost[] actors = root.GetComponentsInChildren<DeterministicRollbackCharacterHost>(true)
                .OrderBy(value => value.ActorId.Value, StringComparer.Ordinal)
                .ToArray();
            if (actors.Length != 2 ||
                !string.Equals(actors[0].ActorId.Value, "rollback-actor-a", StringComparison.Ordinal) ||
                !string.Equals(actors[1].ActorId.Value, "rollback-actor-b", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Rollback runtime root must contain exactly two Fixed Actor Hosts.");
            }
            if (root.GetComponentsInChildren<CharacterPipelineHost>(true).Length != 0)
                throw new InvalidOperationException("Rollback runtime root contains a legacy CharacterPipelineHost.");
            for (int i = 0; i < actors.Length; i++)
            {
                if (actors[i].SessionHost != session)
                    throw new InvalidOperationException($"Rollback Actor '{actors[i].ActorId}' targets another Session Host.");
                RequireObjectReference(actors[i], "m_Endpoint", closure.Endpoint);
                RequireObjectReference(actors[i], "m_Program", closure.ProgramAsset);
                RequireObjectReference(actors[i], "m_PresentationProjection", closure.Projection);
                RequireObjectReference(actors[i], "m_InputProfile", closure.Definition.InputProfile);
            }
            DeterministicRollbackDemoStatusOverlay overlay =
                root.GetComponentsInChildren<DeterministicRollbackDemoStatusOverlay>(true).SingleOrDefault() ??
                throw new InvalidOperationException("Rollback runtime root requires exactly one diagnostics overlay.");
            SerializedProperty overlayActors = new SerializedObject(overlay).FindProperty("m_Actors") ??
                throw new InvalidOperationException("Rollback diagnostics overlay Actor binding is missing.");
            if (overlayActors.arraySize != actors.Length)
                throw new InvalidOperationException("Rollback diagnostics overlay roster is incomplete.");
            for (int i = 0; i < actors.Length; i++)
            {
                if (overlayActors.GetArrayElementAtIndex(i).objectReferenceValue != actors[i])
                    throw new InvalidOperationException("Rollback diagnostics overlay Actor order is invalid.");
            }
            variant.ValidateComposition(closure.Composition);

            Scene scene = EditorSceneManager.OpenScene(DebugScenePath, OpenSceneMode.Additive);
            try
            {
                if (scene.GetRootGameObjects().SelectMany(value => value.GetComponentsInChildren<SimulationSessionHost>(true)).Any())
                    throw new InvalidOperationException("Shared Gameplay Debug Scene cannot contain a pre-instantiated Session Host.");
                GameplayLabBootstrap[] bootstraps = scene.GetRootGameObjects()
                    .SelectMany(value => value.GetComponentsInChildren<GameplayLabBootstrap>(true))
                    .ToArray();
                if (bootstraps.Length != 1 ||
                    bootstraps[0].Variants.Count != 2 ||
                    !bootstraps[0].Variants.Contains(closure.LocalVariant) ||
                    !bootstraps[0].Variants.Contains(variant))
                    throw new InvalidOperationException("Shared Gameplay Debug Scene does not reference the exact two Variants.");
                DeterministicCollisionWorldAuthoring[] worlds = scene.GetRootGameObjects()
                    .SelectMany(value => value.GetComponentsInChildren<DeterministicCollisionWorldAuthoring>(true))
                    .ToArray();
                if (worlds.Length != 1 || worlds[0].Output != closure.Collision ||
                    !string.Equals(worlds[0].MapId, MapId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Shared Gameplay Debug Scene deterministic world binding is stale.");
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        static void RequireObjectReference(Object target, string propertyName, Object expected)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(propertyName) ??
                throw new InvalidOperationException($"Serialized property '{propertyName}' is missing on '{target.GetType().Name}'.");
            if (property.objectReferenceValue != expected)
                throw new InvalidOperationException($"Rollback Peer Scene reference '{propertyName}' is stale on '{target.name}'.");
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
            GameplayLabSessionVariantDefinition localVariant,
            GameplayLabSessionVariantDefinition rollbackVariant,
            CharacterPipelineDefinition definition,
            FixedCharacterSimulationProgramAsset programAsset,
            ThirdPersonSimulation.Fixed.CharacterSimulationProgram program,
            CharacterPresentationProjectionAsset projection,
            CharacterPresentationProjection publishedProjection,
            SimulationSessionCompositionDefinition composition,
            DeterministicRollbackSessionSourceDefinition source,
            DeterministicRollbackPipelineDefinition pipeline,
            DeterministicKccWorldSolverDefinition solver,
            DeterministicCollisionWorldAsset collision,
            RollbackEndpointAuthoringDefinition endpoint,
            DeterministicRollbackModelDefinition model,
            string kccIdentityHash)
        {
            LocalVariant = localVariant;
            RollbackVariant = rollbackVariant;
            Definition = definition;
            ProgramAsset = programAsset;
            Program = program;
            Projection = projection;
            PublishedProjection = publishedProjection;
            Composition = composition;
            Source = source;
            Pipeline = pipeline;
            Solver = solver;
            Collision = collision;
            Endpoint = endpoint;
            Model = model;
            KccIdentityHash = kccIdentityHash;
        }

        public GameplayLabSessionVariantDefinition LocalVariant { get; }
        public GameplayLabSessionVariantDefinition RollbackVariant { get; }
        public CharacterPipelineDefinition Definition { get; }
        public FixedCharacterSimulationProgramAsset ProgramAsset { get; }
        public ThirdPersonSimulation.Fixed.CharacterSimulationProgram Program { get; }
        public CharacterPresentationProjectionAsset Projection { get; }
        public CharacterPresentationProjection PublishedProjection { get; }
        public SimulationSessionCompositionDefinition Composition { get; }
        public DeterministicRollbackSessionSourceDefinition Source { get; }
        public DeterministicRollbackPipelineDefinition Pipeline { get; }
        public DeterministicKccWorldSolverDefinition Solver { get; }
        public DeterministicCollisionWorldAsset Collision { get; }
        public RollbackEndpointAuthoringDefinition Endpoint { get; }
        public DeterministicRollbackModelDefinition Model { get; }
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
            ThirdPersonSimulation.Fixed.CharacterSimulationProgram program = closure.Program;
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
                ProgramIdentity(program),
                closure.Pipeline.BuildPortableDescriptor().PipelineId.Value,
                model.ModelIdentity.ToString(),
                "thirdperson.runtime-topology.deterministic-rollback.relay-two-peers.v1",
                "unity-client-player",
                "thirdperson.network-test.deterministic-rollback.player",
                new[]
                {
                    NetworkTestProductAdapterUtility.Field("collisionWorldHash", closure.Collision.ContentHash),
                    NetworkTestProductAdapterUtility.Field("contractHash", closure.Projection.ContractHash),
                    NetworkTestProductAdapterUtility.Field(
                        "kccId",
                        $"{solverIdentity.Identity.ComponentId}@{closure.KccIdentityHash}"),
                    NetworkTestProductAdapterUtility.Field("kccIdentityHash", closure.KccIdentityHash),
                    NetworkTestProductAdapterUtility.Field("transport", "UDP"),
                    NetworkTestProductAdapterUtility.Field("ports", "24100,24101,24102"),
                    NetworkTestProductAdapterUtility.Field("programHash", closure.ProgramAsset.ProgramHash),
                    NetworkTestProductAdapterUtility.Field("programId", closure.ProgramAsset.ProgramId),
                    NetworkTestProductAdapterUtility.Field(
                        "projectionRevision",
                        closure.Projection.ProjectionRevision),
                    NetworkTestProductAdapterUtility.Field("semanticHash", closure.ProgramAsset.SemanticHash),
                    NetworkTestProductAdapterUtility.Field(
                        "sourceRevision",
                        closure.ProgramAsset.SourceRevision)
                });
        }

        public IReadOnlyList<NetworkTestRuntimeArtifactResult> PublishAdditionalArtifacts(
            NetworkTestProductContext context,
            NetworkTestProductDescriptor descriptor,
            string productRoot,
            string buildId)
        {
            const string serverProductId = "thirdperson.server-product.deterministic-rollback-relay";
            const string serverManifestFileName = "DeterministicRollbackServerManifest.json";
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
            RollbackEndpointDefinition endpoint = endpointAuthoring.Build();
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
            var serverManifest = new DeterministicRollbackServerManifest
            {
                schemaVersion = DeterministicRollbackServerManifest.CurrentSchemaVersion,
                buildId = buildId,
                productId = serverProductId,
                sessionId = endpoint.SessionId,
                listenAddress = endpoint.Address.ToString(),
                listenPort = endpoint.Port,
                relayServerPeerId = endpointAuthoring.RelayServerPeerId,
                peers = peerManifests,
                modelId = model.ModelIdentity.ComponentId,
                modelVersion = model.ModelIdentity.SemanticVersion,
                modelConfigurationHash = model.ModelIdentity.ConfigurationHash.Value,
                protocolId = DeterministicRollbackModelIdentity.Protocol.ProtocolId,
                protocolVersion = DeterministicRollbackModelIdentity.Protocol.SemanticVersion,
                protocolSchemaHash = DeterministicRollbackModelIdentity.Protocol.SchemaHash.Value,
                tickRate = model.TickRate,
                programId = closure.ProgramAsset.ProgramId,
                sourceRevision = closure.ProgramAsset.SourceRevision,
                projectionRevision = closure.Projection.ProjectionRevision,
                semanticHash = model.SemanticHash.ToString(),
                fixedProgramHash = model.FixedProgramHash.ToString(),
                fixedLayoutHash = model.FixedLayoutHash.ToString(),
                collisionWorldHash = model.CollisionWorldHash.Value,
                kccIdentityHash = model.KccIdentityHash.Value,
                offensiveRequestDelayTicks = policy.OffensiveRequestDelayTicks,
                confirmationDelayTicks = policy.ConfirmationDelayTicks,
                historyLengthTicks = policy.HistoryLengthTicks,
                hashCadenceTicks = policy.HashCadenceTicks,
                maximumRollbackDepthTicks = policy.MaximumRollbackDepthTicks,
                maximumQueuedBundles = policy.MaximumQueuedBundles,
                maximumQueuedSnapshots = policy.MaximumQueuedSnapshots,
                maximumOutputRecords = policy.MaximumOutputRecords,
                missingInputPolicy = policy.MissingInputPolicy.ToString(),
                snapshotAuthority = policy.SnapshotAuthority.ToString(),
                maximumDatagramBytes = endpoint.MaximumDatagramBytes,
                maximumQueuedMessages = endpoint.MaximumQueuedMessages,
                maximumFragmentsPerMessage = endpoint.MaximumFragmentsPerMessage,
                reliableResendMilliseconds = endpoint.ReliableResendMilliseconds,
                inputRedundancyCount = endpointAuthoring.InputRedundancyCount
            };
            serverManifest.manifestHash = serverManifest.ValidateAndComputeHash().Value;
            string manifestPath = Path.Combine(serverDirectory, serverManifestFileName);
            File.WriteAllText(
                manifestPath,
                JsonUtility.ToJson(serverManifest, true),
                new System.Text.UTF8Encoding(false));
            serverManifest = JsonUtility.FromJson<DeterministicRollbackServerManifest>(
                File.ReadAllText(manifestPath, System.Text.Encoding.UTF8));
            serverManifest.RequireValidHash();

            string executable = "ThirdPerson.DeterministicRollback.Server.exe";
            if (!File.Exists(Path.Combine(serverDirectory, executable)))
                throw new InvalidOperationException("Deterministic Rollback Relay Server publish output is missing.");
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
                        NetworkTestProductAdapterUtility.Field("endpoint", $"{endpoint.Address}:{endpoint.Port}"),
                        NetworkTestProductAdapterUtility.Field("protocol", model.Handshake.Protocol.ToString())
                    })
            };
        }

        public void ValidateProduct(
            NetworkTestProductContext context,
            NetworkTestProductDescriptor descriptor,
            NetworkTestProductBuildManifest manifest)
        {
            DeterministicRollbackProductClosure closure =
                DeterministicRollbackNetworkTestBuildAndRun.RequireProductClosure();
            NetworkTestRuntimeArtifactManifest relay = NetworkTestProductAdapterUtility.RequireManagedArtifact(
                manifest,
                "deterministic-relay-server",
                "thirdperson.server-product.deterministic-rollback-relay",
                context.ProductRoot);
            string serverManifestPath = Path.Combine(
                context.ProductRoot,
                relay.manifestPath.Replace('/', Path.DirectorySeparatorChar));
            DeterministicRollbackServerManifest serverManifest =
                JsonUtility.FromJson<DeterministicRollbackServerManifest>(
                    File.ReadAllText(serverManifestPath, System.Text.Encoding.UTF8));
            serverManifest.RequireValidHash();
            RollbackRoster expectedRoster = closure.Endpoint.BuildRoster();
            RollbackRoster actualRoster = serverManifest.BuildRoster();
            if (!string.Equals(serverManifest.programId, closure.ProgramAsset.ProgramId, StringComparison.Ordinal) ||
                !string.Equals(serverManifest.sourceRevision, closure.ProgramAsset.SourceRevision, StringComparison.Ordinal) ||
                !string.Equals(serverManifest.projectionRevision, closure.Projection.ProjectionRevision, StringComparison.Ordinal) ||
                !string.Equals(serverManifest.semanticHash, closure.ProgramAsset.SemanticHash, StringComparison.Ordinal) ||
                !string.Equals(serverManifest.fixedProgramHash, closure.ProgramAsset.ProgramHash, StringComparison.Ordinal) ||
                !string.Equals(serverManifest.collisionWorldHash, closure.Collision.ContentHash, StringComparison.Ordinal) ||
                !string.Equals(serverManifest.kccIdentityHash, closure.KccIdentityHash, StringComparison.Ordinal) ||
                actualRoster.Entries.Count != expectedRoster.Entries.Count)
            {
                throw new InvalidOperationException("Rollback Relay manifest does not match the exact Variant product closure.");
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

        static string ProgramIdentity(ThirdPersonSimulation.Fixed.CharacterSimulationProgram program) =>
            $"program={program.Manifest.ProgramId.Value};compiler={program.Manifest.CompilerVersion};operations={program.Manifest.OperationSetVersion};numeric={program.Manifest.NumericProfile.Id.Value};abi={program.Manifest.NumericProfile.AbiVersion.Value};programHash={program.ProgramHash};layoutHash={program.LayoutHash};stateCodec={ThirdPersonSimulation.Fixed.CharacterSimulationStateCodec.CodecIdentity};source={program.Manifest.SourceRevision.Value}";
    }
}
