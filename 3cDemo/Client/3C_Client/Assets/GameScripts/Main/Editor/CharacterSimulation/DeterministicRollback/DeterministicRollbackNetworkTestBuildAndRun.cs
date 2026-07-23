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
        const string SceneDirectory = "Assets/Scenes/DeterministicRollback";
        internal const string FixedProgramPath = ConfigDirectory + "/CorinFixedProgram.asset";
        const string FixedRuntimePath = ConfigDirectory + "/CorinFixedProgramRuntime.asset";
        const string BackendPath = ConfigDirectory + "/CorinFixedPassBackend.asset";
        internal const string PipelinePath = ConfigDirectory + "/CorinDeterministicRollbackPipeline.asset";
        const string CollisionPath = ConfigDirectory + "/CorinDeterministicCollisionWorld.asset";
        const string SolverPath = ConfigDirectory + "/CorinDeterministicKcc.asset";
        internal const string EndpointPath = ConfigDirectory + "/CorinRollbackEndpoint.asset";
        internal const string SourcePath = ConfigDirectory + "/CorinRollbackSessionSource.asset";
        const string CompositionPath = ConfigDirectory + "/CorinRollbackComposition.asset";
        const string BootstrapScenePath = SceneDirectory + "/DeterministicRollbackBootstrap.unity";
        const string PeerScenePath = SceneDirectory + "/DeterministicRollbackPeer.unity";
        internal const string SessionId = "corin-deterministic-rollback-demo";
        const string MapId = "deterministic-rollback-demo";

        internal static readonly string[] BuildScenes = { BootstrapScenePath, PeerScenePath };

        public static void PrepareAssetsAndScenes()
        {
            RequireEditorIdle();
            NetworkTestEditorSceneSetup.Preserve(
                "Deterministic Rollback preparation",
                PrepareBuildInputs);
        }

        internal static void PrepareBuildInputs()
        {
            EnsureDirectory(ConfigDirectory);
            EnsureDirectory(SceneDirectory);

            CharacterPipelineDefinition definition = AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(DefinitionPath);
            if (!definition || !definition.InputProfile)
                throw new InvalidOperationException("Rollback Demo requires the complete Corin Character Pipeline Definition.");
            FixedCharacterSimulationProgramAsset fixedProgram =
                FixedCharacterSimulationProgramBuildService.Build(definition, FixedProgramPath);
            ThirdPersonSimulation.Fixed.CharacterSimulationProgram program = fixedProgram.Load();
            int tickRate = program.Manifest.TickRate;

            FixedProgramRuntimeDefinition runtime = CreateOrLoad<FixedProgramRuntimeDefinition>(FixedRuntimePath);
            FixedPassExecutionBackendDefinition backend = CreateOrLoad<FixedPassExecutionBackendDefinition>(BackendPath);
            DeterministicRollbackPipelineDefinition pipeline = CreateOrLoad<DeterministicRollbackPipelineDefinition>(PipelinePath);
            DeterministicCollisionWorldAsset collision = CreateOrLoad<DeterministicCollisionWorldAsset>(CollisionPath);
            BakeCollisionWorld(collision);
            DeterministicKccWorldSolverDefinition solver = CreateOrLoad<DeterministicKccWorldSolverDefinition>(SolverPath);
            SetObject(solver, "m_CollisionWorld", collision);
            RollbackEndpointAuthoringDefinition endpoint = CreateOrLoad<RollbackEndpointAuthoringDefinition>(EndpointPath);
            ConfigureEndpoint(endpoint);
            DeterministicRollbackSessionSourceDefinition source = CreateOrLoad<DeterministicRollbackSessionSourceDefinition>(SourcePath);
            ConfigureSource(source, tickRate, fixedProgram, pipeline, solver, endpoint);
            SimulationSessionCompositionDefinition composition = CreateOrLoad<SimulationSessionCompositionDefinition>(CompositionPath);
            composition.SetAuthoring(
                SessionId,
                "corin-deterministic-rollback-world",
                MapId,
                "deterministic-rollback-world-v1",
                "deterministic-rollback-local-logic",
                tickRate,
                runtime,
                backend,
                pipeline,
                source,
                solver,
                WorldFeature.Ground |
                WorldFeature.Slope |
                WorldFeature.Step |
                WorldFeature.WallSlide |
                WorldFeature.ActorCollision);
            EditorUtility.SetDirty(composition);

            BuildBootstrapScene();
            ValidatePeerScene(definition, fixedProgram, endpoint, composition);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SimulationSessionCompositionDefinition persistedComposition =
                AssetDatabase.LoadAssetAtPath<SimulationSessionCompositionDefinition>(CompositionPath);
            DeterministicRollbackSessionSourceDefinition persistedSource =
                AssetDatabase.LoadAssetAtPath<DeterministicRollbackSessionSourceDefinition>(SourcePath);
            DeterministicCollisionWorldAsset persistedCollision =
                AssetDatabase.LoadAssetAtPath<DeterministicCollisionWorldAsset>(CollisionPath);
            if (!persistedComposition || !persistedSource || !persistedCollision)
                throw new InvalidOperationException("Rollback Demo assets were not persisted after AssetDatabase refresh.");
            persistedComposition.RequireComplete();
            persistedSource.BuildModelDefinition();
            Debug.Log($"Deterministic Rollback Demo prepared. Program={program.ProgramHash}; TickRate={tickRate}; World={persistedCollision.ContentHash}");
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

        static void ConfigureEndpoint(RollbackEndpointAuthoringDefinition endpoint)
        {
            var serialized = new SerializedObject(endpoint);
            serialized.FindProperty("m_RelayServerAddress").stringValue = "127.0.0.1";
            serialized.FindProperty("m_RelayServerPort").intValue = 24100;
            serialized.FindProperty("m_RelayServerPeerId").stringValue = "rollback-input-relay";
            serialized.FindProperty("m_SessionId").stringValue = SessionId;
            serialized.FindProperty("m_MaximumDatagramBytes").intValue = 1200;
            serialized.FindProperty("m_MaximumQueuedMessages").intValue = 512;
            serialized.FindProperty("m_MaximumFragmentsPerMessage").intValue = 512;
            serialized.FindProperty("m_ReliableResendMilliseconds").intValue = 50;
            serialized.FindProperty("m_InputRedundancyCount").intValue = 4;
            serialized.FindProperty("m_MaximumPreparationTicks").intValue = 1200;
            SerializedProperty profiles = serialized.FindProperty("m_PeerProfiles");
            profiles.arraySize = 2;
            ConfigureProfile(profiles.GetArrayElementAtIndex(0), "peer-a", "rollback-peer-a", "rollback-player-a", "rollback-actor-a", 24101);
            ConfigureProfile(profiles.GetArrayElementAtIndex(1), "peer-b", "rollback-peer-b", "rollback-player-b", "rollback-actor-b", 24102);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(endpoint);
            endpoint.Build();
            endpoint.BuildRoster();
        }

        static void ConfigureProfile(
            SerializedProperty profile,
            string profileId,
            string peerId,
            string playerId,
            string actorId,
            int port)
        {
            profile.FindPropertyRelative("m_ProfileId").stringValue = profileId;
            profile.FindPropertyRelative("m_PeerId").stringValue = peerId;
            profile.FindPropertyRelative("m_PlayerId").stringValue = playerId;
            profile.FindPropertyRelative("m_ActorId").stringValue = actorId;
            profile.FindPropertyRelative("m_LocalAddress").stringValue = "127.0.0.1";
            profile.FindPropertyRelative("m_LocalPort").intValue = port;
        }

        static void ConfigureSource(
            DeterministicRollbackSessionSourceDefinition source,
            int tickRate,
            FixedCharacterSimulationProgramAsset program,
            DeterministicRollbackPipelineDefinition pipeline,
            DeterministicKccWorldSolverDefinition solver,
            RollbackEndpointAuthoringDefinition endpoint)
        {
            var serialized = new SerializedObject(source);
            serialized.FindProperty("m_TickRate").intValue = tickRate;
            serialized.FindProperty("m_FixedProgram").objectReferenceValue = program;
            serialized.FindProperty("m_Pipeline").objectReferenceValue = pipeline;
            serialized.FindProperty("m_WorldSolver").objectReferenceValue = solver;
            serialized.FindProperty("m_Endpoint").objectReferenceValue = endpoint;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(source);
        }

        static void BakeCollisionWorld(DeterministicCollisionWorldAsset output)
        {
            Scene scene = SceneManager.GetSceneByPath(PeerScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened)
                scene = EditorSceneManager.OpenScene(PeerScenePath, OpenSceneMode.Additive);
            try
            {
                DeterministicCollisionWorldAuthoring[] authorings = scene.GetRootGameObjects()
                    .SelectMany(value => value.GetComponentsInChildren<DeterministicCollisionWorldAuthoring>(true))
                    .ToArray();
                if (authorings.Length != 1)
                    throw new InvalidOperationException("Rollback Peer Scene requires exactly one Deterministic Collision World authoring root.");
                DeterministicCollisionWorldAuthoring authoring = authorings[0];
                if (authoring.Output != output)
                    throw new InvalidOperationException("Rollback Peer Scene collision authoring targets another output asset.");
                if (!string.Equals(authoring.MapId, MapId, StringComparison.Ordinal))
                    throw new InvalidOperationException("Rollback Peer Scene collision authoring MapId does not match the composition.");
                output.Replace(DeterministicCollisionWorldBaker.Build(authoring));
                EditorUtility.SetDirty(output);
            }
            finally
            {
                if (opened)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        static void BuildBootstrapScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("DeterministicRollbackBootstrap");
            DeterministicRollbackNetworkTestBootstrap bootstrap = root.AddComponent<DeterministicRollbackNetworkTestBootstrap>();
            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("m_PeerSceneName").stringValue = Path.GetFileNameWithoutExtension(PeerScenePath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SaveScene(scene, BootstrapScenePath);
        }

        static void ValidatePeerScene(
            CharacterPipelineDefinition definition,
            FixedCharacterSimulationProgramAsset program,
            RollbackEndpointAuthoringDefinition endpoint,
            SimulationSessionCompositionDefinition composition)
        {
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(PeerScenePath))
                throw new InvalidOperationException($"Rollback Peer Scene is missing: {PeerScenePath}");
            Scene scene = EditorSceneManager.OpenScene(PeerScenePath, OpenSceneMode.Single);
            SimulationSessionHost session = Object.FindObjectsOfType<SimulationSessionHost>(true).SingleOrDefault() ??
                throw new InvalidOperationException("Rollback Peer Scene requires exactly one SimulationSessionHost.");
            RequireObjectReference(session, "m_Composition", composition);
            DeterministicRollbackCharacterHost[] actors = Object.FindObjectsOfType<DeterministicRollbackCharacterHost>(true)
                .OrderBy(value => value.ActorId.Value, StringComparer.Ordinal)
                .ToArray();
            if (actors.Length != 2 ||
                !string.Equals(actors[0].ActorId.Value, "rollback-actor-a", StringComparison.Ordinal) ||
                !string.Equals(actors[1].ActorId.Value, "rollback-actor-b", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Rollback Peer Scene must contain exactly two Fixed Actor Hosts.");
            }
            if (Object.FindObjectsOfType<CharacterPipelineHost>(true).Length != 0)
                throw new InvalidOperationException("Rollback Peer Scene contains a legacy CharacterPipelineHost.");
            for (int i = 0; i < actors.Length; i++)
            {
                if (actors[i].SessionHost != session)
                    throw new InvalidOperationException($"Rollback Actor '{actors[i].ActorId}' targets another Session Host.");
                RequireObjectReference(actors[i], "m_Endpoint", endpoint);
                RequireObjectReference(actors[i], "m_Program", program);
                RequireObjectReference(actors[i], "m_PresentationProjection", definition.PresentationProjection);
                RequireObjectReference(actors[i], "m_InputProfile", definition.InputProfile);
            }
            DeterministicRollbackDemoStatusOverlay overlay =
                Object.FindObjectsOfType<DeterministicRollbackDemoStatusOverlay>(true).SingleOrDefault() ??
                throw new InvalidOperationException("Rollback Peer Scene requires exactly one diagnostics overlay.");
            SerializedProperty overlayActors = new SerializedObject(overlay).FindProperty("m_Actors") ??
                throw new InvalidOperationException("Rollback diagnostics overlay Actor binding is missing.");
            if (overlayActors.arraySize != actors.Length)
                throw new InvalidOperationException("Rollback diagnostics overlay roster is incomplete.");
            for (int i = 0; i < actors.Length; i++)
            {
                if (overlayActors.GetArrayElementAtIndex(i).objectReferenceValue != actors[i])
                    throw new InvalidOperationException("Rollback diagnostics overlay Actor order is invalid.");
            }
        }

        static void RequireObjectReference(Object target, string propertyName, Object expected)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(propertyName) ??
                throw new InvalidOperationException($"Serialized property '{propertyName}' is missing on '{target.GetType().Name}'.");
            if (property.objectReferenceValue != expected)
                throw new InvalidOperationException($"Rollback Peer Scene reference '{propertyName}' is stale on '{target.name}'.");
        }

        static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            T value = AssetDatabase.LoadAssetAtPath<T>(path);
            if (value)
                return value;
            value = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(value, path);
            return value;
        }

        static void SetObject(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName) ??
                throw new InvalidOperationException($"Serialized property '{propertyName}' is missing on '{target.GetType().Name}'.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        static void SaveScene(Scene scene, string path)
        {
            if (!EditorSceneManager.SaveScene(scene, path))
                throw new InvalidOperationException($"Failed to save Rollback Scene '{path}'.");
        }

        static void EnsureDirectory(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        static void RequireEditorIdle()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Deterministic Rollback tooling cannot run during Play Mode.");
        }
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
            FixedCharacterSimulationProgramAsset programAsset =
                NetworkTestProductAdapterUtility.RequireAsset<FixedCharacterSimulationProgramAsset>(
                    DeterministicRollbackNetworkTestBuildAndRun.FixedProgramPath);
            ThirdPersonSimulation.Fixed.CharacterSimulationProgram program = programAsset.Load();
            DeterministicRollbackPipelineDefinition pipeline =
                NetworkTestProductAdapterUtility.RequireAsset<DeterministicRollbackPipelineDefinition>(
                    DeterministicRollbackNetworkTestBuildAndRun.PipelinePath);
            DeterministicRollbackSessionSourceDefinition source =
                NetworkTestProductAdapterUtility.RequireAsset<DeterministicRollbackSessionSourceDefinition>(
                    DeterministicRollbackNetworkTestBuildAndRun.SourcePath);
            DeterministicRollbackModelDefinition model = source.BuildModelDefinition();
            return new NetworkTestProductDescriptor(
                ProductId,
                DisplayName,
                OutputDirectoryName,
                ManifestFileName,
                DeterministicRollbackNetworkTestBuildAndRun.BuildScenes,
                BuildTarget.StandaloneWindows64,
                BuildTargetGroup.Standalone,
                BuildOptions.Development | BuildOptions.StrictMode | BuildOptions.CleanBuildCache,
                "Development, StrictMode, CleanBuildCache",
                ScriptingImplementation.IL2CPP,
                "3cDemo/Tools/DeterministicRollback/Start-DeterministicRollbackDemo.ps1",
                ProgramIdentity(program),
                pipeline.BuildPortableDescriptor().PipelineId.Value,
                model.ModelIdentity.ToString(),
                "thirdperson.runtime-topology.deterministic-rollback.relay-two-peers.v1",
                "unity-client-player",
                "thirdperson.network-test.deterministic-rollback.player",
                new[]
                {
                    NetworkTestProductAdapterUtility.Field("transport", "UDP"),
                    NetworkTestProductAdapterUtility.Field("ports", "24100,24101,24102")
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

            DeterministicRollbackSessionSourceDefinition source =
                NetworkTestProductAdapterUtility.RequireAsset<DeterministicRollbackSessionSourceDefinition>(
                    DeterministicRollbackNetworkTestBuildAndRun.SourcePath);
            RollbackEndpointAuthoringDefinition endpointAuthoring =
                NetworkTestProductAdapterUtility.RequireAsset<RollbackEndpointAuthoringDefinition>(
                    DeterministicRollbackNetworkTestBuildAndRun.EndpointPath);
            DeterministicRollbackModelDefinition model = source.BuildModelDefinition();
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
        }

        static string ProgramIdentity(ThirdPersonSimulation.Fixed.CharacterSimulationProgram program) =>
            $"program={program.Manifest.ProgramId.Value};compiler={program.Manifest.CompilerVersion};operations={program.Manifest.OperationSetVersion};numeric={program.Manifest.NumericProfile.Id.Value};abi={program.Manifest.NumericProfile.AbiVersion.Value};programHash={program.ProgramHash};layoutHash={program.LayoutHash};stateCodec={ThirdPersonSimulation.Fixed.CharacterSimulationStateCodec.CodecIdentity};source={program.Manifest.SourceRevision.Value}";
    }
}
