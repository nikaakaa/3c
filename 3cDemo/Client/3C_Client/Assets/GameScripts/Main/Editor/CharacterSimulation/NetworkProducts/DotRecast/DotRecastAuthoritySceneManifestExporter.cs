using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonGameplay.Networking.ServerAuthoritative;
using ThirdPersonSimulation;
using ThirdPersonSimulation.DotRecast;
using ThirdPersonSimulation.DotRecastAuthority;
using ThirdPersonSimulation.ServerAuthoritative;
using UnityEditor;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    public sealed class DotRecastAuthorityActorExportBinding
    {
        public DotRecastAuthorityActorExportBinding(
            ServerAuthoritativeRosterEntry roster,
            string worldBodyBindingId,
            WorldBodyState initialBody,
            ActorContactShape contactShape,
            SimulationOutputRouteDescriptor outputRoute)
        {
            if (initialBody.ActorId != roster.ActorId || outputRoute.ActorId != roster.ActorId)
                throw new ArgumentException("DotRecast Authority export Actor identities do not match.");
            Roster = roster;
            WorldBodyBindingId = string.IsNullOrWhiteSpace(worldBodyBindingId)
                ? throw new ArgumentException("DotRecast Authority export requires a state-only World binding.", nameof(worldBodyBindingId))
                : worldBodyBindingId.Trim();
            InitialBody = initialBody;
            ContactShape = contactShape;
            OutputRoute = outputRoute;
        }

        public ServerAuthoritativeRosterEntry Roster { get; }
        public string WorldBodyBindingId { get; }
        public WorldBodyState InitialBody { get; }
        public ActorContactShape ContactShape { get; }
        public SimulationOutputRouteDescriptor OutputRoute { get; }
    }

    public sealed class DotRecastAuthoritySceneManifestExportRequest
    {
        readonly ReadOnlyCollection<DotRecastAuthorityActorExportBinding> m_Roster;

        public DotRecastAuthoritySceneManifestExportRequest(
            string serverPublishDirectory,
            CharacterPipelineDefinition characterDefinition,
            ServerAuthoritativeAuthoritySessionSourceDefinition authoritySource,
            SimulationExecutionBackendDefinition executionBackend,
            DotRecastWorldSolverDefinition worldSolver,
            string hostId,
            int fantasyProcessConfigId,
            int authoritySceneConfigId,
            string authoritySceneType,
            string roomId,
            string dataHost,
            int dataPort,
            string sessionId,
            string worldId,
            string sourceClockId,
            IEnumerable<DotRecastAuthorityActorExportBinding> roster)
        {
            ServerPublishDirectory = string.IsNullOrWhiteSpace(serverPublishDirectory)
                ? throw new ArgumentException("DotRecast Authority server publish directory is required.", nameof(serverPublishDirectory))
                : Path.GetFullPath(serverPublishDirectory);
            CharacterDefinition = characterDefinition
                ? characterDefinition
                : throw new ArgumentNullException(nameof(characterDefinition));
            AuthoritySource = authoritySource
                ? authoritySource
                : throw new ArgumentNullException(nameof(authoritySource));
            ExecutionBackend = executionBackend
                ? executionBackend
                : throw new ArgumentNullException(nameof(executionBackend));
            WorldSolver = worldSolver
                ? worldSolver
                : throw new ArgumentNullException(nameof(worldSolver));
            HostId = Require(hostId, nameof(hostId));
            Scene = new DotRecastAuthoritySceneIdentity(
                fantasyProcessConfigId,
                authoritySceneConfigId,
                Require(authoritySceneType, nameof(authoritySceneType)));
            RoomId = new ServerAuthoritativeRoomId(roomId);
            DataEndpoint = new DotRecastAuthorityEndpointDescriptor(dataHost, dataPort);
            SessionId = new SimulationSessionId(sessionId);
            WorldId = new SimulationWorldId(worldId);
            SourceClockId = new SimulationSourceClockId(sourceClockId);
            var values = roster == null
                ? new List<DotRecastAuthorityActorExportBinding>()
                : new List<DotRecastAuthorityActorExportBinding>(roster);
            values.Sort((left, right) => left.Roster.ActorId.CompareTo(right.Roster.ActorId));
            if (values.Count != 2 || values[0] == null || values[1] == null || values[0].Roster.ActorId == values[1].Roster.ActorId)
                throw new ArgumentException("DotRecast Authority export requires the locked two-Actor roster.", nameof(roster));
            m_Roster = values.AsReadOnly();
        }

        public string ServerPublishDirectory { get; }
        public CharacterPipelineDefinition CharacterDefinition { get; }
        public ServerAuthoritativeAuthoritySessionSourceDefinition AuthoritySource { get; }
        public SimulationExecutionBackendDefinition ExecutionBackend { get; }
        public DotRecastWorldSolverDefinition WorldSolver { get; }
        public string HostId { get; }
        public DotRecastAuthoritySceneIdentity Scene { get; }
        public ServerAuthoritativeRoomId RoomId { get; }
        public DotRecastAuthorityEndpointDescriptor DataEndpoint { get; }
        public SimulationSessionId SessionId { get; }
        public SimulationWorldId WorldId { get; }
        public SimulationSourceClockId SourceClockId { get; }
        public IReadOnlyList<DotRecastAuthorityActorExportBinding> Roster => m_Roster;

        static string Require(string value, string parameter) => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("DotRecast Authority export identity is required.", parameter)
            : value.Trim();
    }

    public static class DotRecastAuthoritySceneManifestExporter
    {
        public const string AuthorityRelativeDirectory = DotRecastAuthoritySceneManifest.PublishDirectoryName;
        public const string ManifestFileName = DotRecastAuthoritySceneManifest.FileName;
        const string AbilityRelativeDirectory = "Artifacts/Abilities";
        const string NavigationRelativePath = "Artifacts/NavigationSurface.navsurface";

        public static LoadedDotRecastAuthoritySceneManifest Export(
            DotRecastAuthoritySceneManifestExportRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            string authorityDirectory = Path.Combine(request.ServerPublishDirectory, AuthorityRelativeDirectory);
            RequireEmptyOutput(authorityDirectory);
            CharacterPipelineDefinition definition = request.CharacterDefinition;
            ServerAuthoritativeAuthoritySessionSourceDefinition source = request.AuthoritySource;
            ServerAuthoritativeSessionConfigurationDefinition configuration = source.Configuration;
            configuration.RequireComplete();
            if (definition.SimulationTickRate != configuration.SimulationTickRate)
                throw new InvalidOperationException("Character Definition and Session Configuration TickRate do not match.");

            SimulationExecutionBackendDescriptor backend = request.ExecutionBackend.BuildPortableDescriptor();
            SimulationExecutionTargetManifest target = Float32SimulationTarget.Manifest.ExecutionTarget;
            SimulationWorldSolverDefinitionDescriptor solver = request.WorldSolver.BuildDescriptor(configuration.SimulationTickRate);
            DotRecastAuthorityHostProduct.Descriptor.RequireAuthoritySolver(solver);
            CharacterControlModuleCatalog controlModules = CharacterControlRuntimeModuleCatalog.Create();
            CharacterControlRuntimeBinding controlRuntimeBinding = definition.BuildControlRuntimeBinding(controlModules);
            CharacterBodyMotionBinding bodyMotionBinding = definition.BuildBodyMotionRuntimeBinding();
            CharacterGameplayEffectRuntimeBinding gameplayEffectRuntimeBinding = definition.BuildGameplayEffectRuntimeBinding();
            CharacterEquipmentRuntimeBinding equipmentRuntimeBinding = definition.BuildEquipmentRuntimeBinding();
            GameplayAbilityProviderBinding providerBinding = definition.BuildGameplayAbilityProviderBinding();
            GameplayAbilityExecutionDataSet<Float32GameplayAbilityExecutionData> abilityData =
                definition.LoadFloat32AbilitySet();
            var characterBindings = new SimulationActorBinding[request.Roster.Count];
            for (int i = 0; i < characterBindings.Length; i++)
            {
                DotRecastAuthorityActorExportBinding actor = request.Roster[i];
                characterBindings[i] = new SimulationActorBinding(
                    actor.Roster.ActorId,
                    actor.WorldBodyBindingId,
                    controlRuntimeBinding,
                    bodyMotionBinding,
                    gameplayEffectRuntimeBinding,
                    equipmentRuntimeBinding,
                    abilityData);
            }
            Float32CharacterRuntime characterRuntime = new Float32CharacterRuntime(
                characterBindings,
                target.NumericProfile,
                configuration.SimulationTickRate,
                target.OperationSetVersion,
                controlModules);
            ServerAuthoritativePipelineCompatibilityIdentity compatibility = configuration.BuildCompatibility(
                characterRuntime,
                request.ExecutionBackend);
            SimulationPipelineDescriptor authorityPipeline = configuration.AuthorityPipeline.BuildPortableDescriptor();
            ServerAuthoritativeAuthoritySourcePolicy sourcePolicy = source.BuildPolicy();
            SimulationSessionSourceAuthoringDescriptor sourceDescriptor = source.BuildAuthoringDescriptor();
            NavigationSurfaceAsset surfaceAsset = request.WorldSolver.NavigationSurface
                ? request.WorldSolver.NavigationSurface
                : throw new InvalidOperationException("DotRecast World Solver has no Navigation Surface asset.");
            NavigationSurfaceArtifact surface = surfaceAsset.Load();
            byte[] surfaceBytes = surfaceAsset.CopyCanonicalArtifact();
            ActorContactShape contactShape = request.WorldSolver.ContactShape;
            ActorContactSolverConfiguration contactConfiguration = request.WorldSolver.ContactConfiguration;

            var abilityBindings = new List<DotRecastAuthorityAbilityArtifactBinding>(definition.Float32AbilityData.Count);
            var abilityArtifacts = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            for (int i = 0; i < definition.Float32AbilityData.Count; i++)
            {
                GameplayAbilityDataAsset asset = definition.Float32AbilityData[i]
                    ? definition.Float32AbilityData[i]
                    : throw new InvalidOperationException("Character Definition contains a missing Float32 Ability Data asset.");
                Float32GameplayAbilityExecutionData data = definition.LoadFloat32AbilityExecutionData(asset);
                byte[] bytes = asset.CopyCanonicalArtifact();
                string assetPath = AssetDatabase.GetAssetPath(asset);
                string abilityGuid = AssetDatabase.AssetPathToGUID(assetPath);
                string relativePath = $"{AbilityRelativeDirectory}/{data.AbilityId.Value}.ability";
                if (!abilityArtifacts.TryAdd(relativePath, bytes))
                    throw new InvalidOperationException($"Ability artifact path '{relativePath}' is duplicated.");
                abilityBindings.Add(new DotRecastAuthorityAbilityArtifactBinding(
                    relativePath,
                    abilityGuid,
                    data.AbilityId,
                    data.ContentHash,
                    data.StateSchemaHash,
                    Float32GameplayAbilityExecutionDataCodec.ComputeCanonicalBytesHash(bytes),
                    bytes.Length,
                    data.CompilerVersion,
                    data.OperationSetVersion,
                    data.TickRate,
                    data.SourceRevision,
                    data.SemanticHash,
                    data.NumericProfile.Id,
                    data.NumericProfile.AbiVersion,
                    data.ExecutionIdentity,
                    data.Root,
                    data.Capabilities.RequiredWorldCapabilities));
            }

            var actorBindings = new DotRecastAuthorityActorBinding[request.Roster.Count];
            var routes = new SimulationOutputRouteDescriptor[request.Roster.Count];
            for (int i = 0; i < actorBindings.Length; i++)
            {
                DotRecastAuthorityActorExportBinding actor = request.Roster[i];
                Float32CharacterRuntimeState state = characterRuntime.CreateInitialState(i);
                byte[] stateBytes = Float32CharacterRuntimeStateCodec.Write(state);
                if (actor.ContactShape != contactShape)
                    throw new InvalidOperationException($"DotRecast Authority Actor '{actor.Roster.ActorId}' contact shape does not match the World Solver configuration.");
                routes[i] = actor.OutputRoute;
                actorBindings[i] = new DotRecastAuthorityActorBinding(
                    actor.Roster,
                    actor.WorldBodyBindingId,
                    stateBytes,
                    Float32CharacterRuntimeStateCodec.ComputeHash(state),
                    actor.InitialBody,
                    actor.ContactShape,
                    actor.OutputRoute);
            }

            var pipelineBinding = new DotRecastAuthorityPipelineBinding(
                compatibility.PredictionPipeline,
                compatibility.AuthorityPipeline,
                authorityPipeline.DescriptorHash,
                backend.Identity,
                sourceDescriptor.Source,
                sourceDescriptor.SourcePorts,
                sourcePolicy,
                configuration.ReplicationPolicy);
            var worldBinding = new DotRecastAuthorityWorldBinding(
                request.WorldId,
                surface.MapId,
                new WorldRevision(surface.WorldRevision),
                DotRecastWorldConfigurationIdentity.Compute(
                    surface.WorldConfigurationHash,
                    contactShape,
                    contactConfiguration),
                surface.WorldConfigurationHash,
                solver,
                NavigationRelativePath,
                surface.ContentHash,
                SimulationCanonicalPayloadHash.Compute(surfaceBytes),
                surfaceBytes.Length,
                surface.QueryProfile.ConfigurationHash,
                contactConfiguration);
            var runtimeIdentities = new DotRecastAuthorityRuntimeIdentitySet(
                request.SessionId,
                request.SourceClockId,
                Float32SimulationSessionComposer.BuildSnapshotCodecIdentity(
                    target,
                    backend),
                DotRecastAuthorityRuntimeIdentityCatalog.BuildCommitter(routes),
                configuration.Endpoint.BuildIdentity(),
                DotRecastAuthorityRuntimeIdentityCatalog.BuildDiagnostics(DotRecastAuthorityHostProduct.ProductId));
            var manifest = new DotRecastAuthoritySceneManifest(
                DotRecastAuthorityHostProduct.ProductId,
                request.HostId,
                request.Scene,
                request.RoomId,
                request.DataEndpoint,
                abilityBindings,
                providerBinding,
                controlRuntimeBinding,
                bodyMotionBinding,
                gameplayEffectRuntimeBinding,
                equipmentRuntimeBinding,
                pipelineBinding,
                worldBinding,
                runtimeIdentities,
                actorBindings);

            Directory.CreateDirectory(Path.Combine(authorityDirectory, AbilityRelativeDirectory.Replace('/', Path.DirectorySeparatorChar)));
            foreach (KeyValuePair<string, byte[]> artifact in abilityArtifacts)
                File.WriteAllBytes(
                    Path.Combine(authorityDirectory, artifact.Key.Replace('/', Path.DirectorySeparatorChar)),
                    artifact.Value);
            File.WriteAllBytes(
                Path.Combine(authorityDirectory, NavigationRelativePath.Replace('/', Path.DirectorySeparatorChar)),
                surfaceBytes);
            string manifestPath = Path.Combine(authorityDirectory, ManifestFileName);
            File.WriteAllBytes(manifestPath, DotRecastAuthoritySceneManifestCodec.Write(manifest));
            return DotRecastAuthoritySceneManifestLoader.LoadFile(manifestPath, controlModules);
        }

        static void RequireEmptyOutput(string outputDirectory)
        {
            if (File.Exists(outputDirectory))
                throw new IOException("DotRecast Authority export path is a file.");
            if (Directory.Exists(outputDirectory) && Directory.EnumerateFileSystemEntries(outputDirectory).GetEnumerator().MoveNext())
                throw new IOException("DotRecast Authority export directory must be new or empty.");
            Directory.CreateDirectory(outputDirectory);
        }
    }
}
