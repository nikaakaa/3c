using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using ThirdPersonSimulation.DotRecast;
using ThirdPersonSimulation.ServerAuthoritative;

namespace ThirdPersonSimulation.DotRecastAuthority
{
    public readonly struct DotRecastAuthorityEndpointDescriptor
    {
        public DotRecastAuthorityEndpointDescriptor(string host, int port)
        {
            Host = DotRecastAuthorityManifestIdentity.Require(host, nameof(host), 255);
            if (port <= 0 || port > 65535)
                throw new ArgumentOutOfRangeException(nameof(port));
            Port = port;
        }

        public string Host { get; }
        public int Port { get; }
    }

    public readonly struct DotRecastAuthoritySceneIdentity
    {
        public DotRecastAuthoritySceneIdentity(
            int processConfigId,
            int sceneConfigId,
            string sceneType)
        {
            if (processConfigId <= 0)
                throw new ArgumentOutOfRangeException(nameof(processConfigId));
            if (sceneConfigId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sceneConfigId));
            ProcessConfigId = processConfigId;
            SceneConfigId = sceneConfigId;
            SceneType = DotRecastAuthorityManifestIdentity.Require(sceneType, nameof(sceneType));
        }

        public int ProcessConfigId { get; }
        public int SceneConfigId { get; }
        public string SceneType { get; }
    }

    public sealed class DotRecastAuthorityProgramArtifactBinding
    {
        public DotRecastAuthorityProgramArtifactBinding(
            string relativePath,
            string definitionGuid,
            ProgramId programId,
            ProgramHash programHash,
            LayoutHash layoutHash,
            StableHash artifactBytesHash,
            int artifactByteLength,
            string compilerVersion,
            OperationSetVersion operationSetVersion,
            ProgramRevision sourceRevision,
            SemanticHash semanticHash,
            NumericProfileId numericProfileId,
            TargetAbiVersion targetAbiVersion,
            WorldCapability requiredWorldCapabilities)
        {
            RelativePath = DotRecastAuthorityRelativePath.Require(relativePath, nameof(relativePath));
            DefinitionGuid = CharacterTargetProgramArtifactLoader.RequireDefinitionGuid(definitionGuid);
            if (!programId.IsValid || !programHash.IsValid || !layoutHash.IsValid || !artifactBytesHash.IsValid ||
                artifactByteLength <= 0 || !operationSetVersion.IsValid || !semanticHash.IsValid ||
                !numericProfileId.IsValid || !targetAbiVersion.IsValid || requiredWorldCapabilities == WorldCapability.None)
            {
                throw new ArgumentException("DotRecast Authority Program artifact binding is incomplete.");
            }
            ProgramId = programId;
            ProgramHash = programHash;
            LayoutHash = layoutHash;
            ArtifactBytesHash = artifactBytesHash;
            ArtifactByteLength = artifactByteLength;
            CompilerVersion = DotRecastAuthorityManifestIdentity.Require(compilerVersion, nameof(compilerVersion));
            OperationSetVersion = operationSetVersion;
            SourceRevision = sourceRevision;
            SemanticHash = semanticHash;
            NumericProfileId = numericProfileId;
            TargetAbiVersion = targetAbiVersion;
            RequiredWorldCapabilities = requiredWorldCapabilities;
        }

        public string RelativePath { get; }
        public string DefinitionGuid { get; }
        public ProgramId ProgramId { get; }
        public ProgramHash ProgramHash { get; }
        public LayoutHash LayoutHash { get; }
        public StableHash ArtifactBytesHash { get; }
        public int ArtifactByteLength { get; }
        public string CompilerVersion { get; }
        public OperationSetVersion OperationSetVersion { get; }
        public ProgramRevision SourceRevision { get; }
        public SemanticHash SemanticHash { get; }
        public NumericProfileId NumericProfileId { get; }
        public TargetAbiVersion TargetAbiVersion { get; }
        public WorldCapability RequiredWorldCapabilities { get; }
    }

    public sealed class DotRecastAuthorityPipelineBinding
    {
        readonly ReadOnlyCollection<SimulationPortDescriptor> m_SourcePorts;

        public DotRecastAuthorityPipelineBinding(
            SimulationPipelineIdentity predictionIdentity,
            SimulationPipelineIdentity identity,
            StableHash descriptorHash,
            SimulationComponentIdentity backendIdentity,
            SimulationSessionSourceDescriptor source,
            IEnumerable<SimulationPortDescriptor> sourcePorts,
            ServerAuthoritativeAuthoritySourcePolicy sourcePolicy,
            ServerAuthoritativeReplicationPolicy replicationPolicy)
        {
            if (!predictionIdentity.IsValid || !identity.IsValid || !descriptorHash.IsValid || !backendIdentity.IsValid ||
                backendIdentity.Role != SimulationComponentRole.ExecutionBackend || source == null ||
                source.OuterTickKind != SimulationTickSourceKind.Authoritative ||
                !source.RequiredPipelineId.Equals(identity.Id) ||
                !string.Equals(source.RequiredBackendId, backendIdentity.ComponentId, StringComparison.Ordinal))
            {
                throw new ArgumentException("DotRecast Authority Pipeline binding is incomplete.");
            }
            var ports = sourcePorts == null
                ? new List<SimulationPortDescriptor>()
                : new List<SimulationPortDescriptor>(sourcePorts);
            ports.Sort((left, right) => string.CompareOrdinal(left.PortId, right.PortId));
            if (ports.Count == 0)
                throw new ArgumentException("DotRecast Authority Pipeline binding requires Source ports.", nameof(sourcePorts));
            for (int i = 0; i < ports.Count; i++)
            {
                if (!string.Equals(ports[i].OwnerComponentId, source.Identity.ComponentId, StringComparison.Ordinal) ||
                    i > 0 && string.Equals(ports[i - 1].PortId, ports[i].PortId, StringComparison.Ordinal))
                {
                    throw new ArgumentException("DotRecast Authority Source ports are inconsistent.", nameof(sourcePorts));
                }
            }
            PredictionIdentity = predictionIdentity;
            Identity = identity;
            DescriptorHash = descriptorHash;
            BackendIdentity = backendIdentity;
            Source = source;
            m_SourcePorts = ports.AsReadOnly();
            SourcePolicy = sourcePolicy ?? throw new ArgumentNullException(nameof(sourcePolicy));
            ReplicationPolicy = replicationPolicy ?? throw new ArgumentNullException(nameof(replicationPolicy));
        }

        public SimulationPipelineIdentity PredictionIdentity { get; }
        public SimulationPipelineIdentity Identity { get; }
        public StableHash DescriptorHash { get; }
        public SimulationComponentIdentity BackendIdentity { get; }
        public SimulationSessionSourceDescriptor Source { get; }
        public IReadOnlyList<SimulationPortDescriptor> SourcePorts => m_SourcePorts;
        public ServerAuthoritativeAuthoritySourcePolicy SourcePolicy { get; }
        public ServerAuthoritativeReplicationPolicy ReplicationPolicy { get; }
        public int TickRate => SourcePolicy.ModelPolicy.SimulationTickRate;
    }

    public sealed class DotRecastAuthorityWorldBinding
    {
        public DotRecastAuthorityWorldBinding(
            SimulationWorldId worldId,
            string mapId,
            WorldRevision worldRevision,
            StableHash worldConfigurationHash,
            StableHash navigationSurfaceConfigurationHash,
            SimulationWorldSolverDefinitionDescriptor solverDefinition,
            string navigationSurfaceRelativePath,
            StableHash navigationSurfaceContentHash,
            StableHash navigationSurfaceBytesHash,
            int navigationSurfaceByteLength,
            StableHash queryProfileHash,
            ActorContactSolverConfiguration contactConfiguration)
        {
            if (!worldId.IsValid || string.IsNullOrEmpty(worldRevision.Value) || !worldConfigurationHash.IsValid ||
                !navigationSurfaceConfigurationHash.IsValid || solverDefinition == null ||
                !navigationSurfaceContentHash.IsValid || !navigationSurfaceBytesHash.IsValid ||
                navigationSurfaceByteLength <= 0 || !queryProfileHash.IsValid)
            {
                throw new ArgumentException("DotRecast Authority World binding is incomplete.");
            }
            WorldId = worldId;
            MapId = DotRecastAuthorityManifestIdentity.Require(mapId, nameof(mapId));
            WorldRevision = worldRevision;
            WorldConfigurationHash = worldConfigurationHash;
            NavigationSurfaceConfigurationHash = navigationSurfaceConfigurationHash;
            SolverDefinition = solverDefinition;
            NavigationSurfaceRelativePath = DotRecastAuthorityRelativePath.Require(navigationSurfaceRelativePath, nameof(navigationSurfaceRelativePath));
            NavigationSurfaceContentHash = navigationSurfaceContentHash;
            NavigationSurfaceBytesHash = navigationSurfaceBytesHash;
            NavigationSurfaceByteLength = navigationSurfaceByteLength;
            QueryProfileHash = queryProfileHash;
            ContactConfiguration = contactConfiguration;
        }

        public SimulationWorldId WorldId { get; }
        public string MapId { get; }
        public WorldRevision WorldRevision { get; }
        public StableHash WorldConfigurationHash { get; }
        public StableHash NavigationSurfaceConfigurationHash { get; }
        public SimulationWorldSolverDefinitionDescriptor SolverDefinition { get; }
        public SolverImplementationId SolverId => SolverDefinition.ImplementationId;
        public string SolverVersion => SolverDefinition.ImplementationVersion;
        public WorldCapability SolverCapabilities => SolverDefinition.Capabilities;
        public WorldFeature SolverFeatures => SolverDefinition.Features;
        public string NavigationSurfaceRelativePath { get; }
        public StableHash NavigationSurfaceContentHash { get; }
        public StableHash NavigationSurfaceBytesHash { get; }
        public int NavigationSurfaceByteLength { get; }
        public StableHash QueryProfileHash { get; }
        public ActorContactSolverConfiguration ContactConfiguration { get; }
    }

    public sealed class DotRecastAuthorityActorBinding
    {
        readonly byte[] m_InitialCharacterStateBytes;

        public DotRecastAuthorityActorBinding(
            ServerAuthoritativeRosterEntry roster,
            string worldBodyBindingId,
            byte[] initialCharacterStateBytes,
            CharacterStateHash initialCharacterStateHash,
            WorldBodyState initialBody,
            ActorContactShape contactShape,
            SimulationOutputRouteDescriptor outputRoute)
        {
            if (!roster.ActorId.IsValid || initialBody.ActorId != roster.ActorId ||
                outputRoute.ActorId != roster.ActorId || !initialCharacterStateHash.IsValid)
            {
                throw new ArgumentException("DotRecast Authority Actor binding identity is inconsistent.");
            }
            m_InitialCharacterStateBytes = initialCharacterStateBytes == null
                ? throw new ArgumentNullException(nameof(initialCharacterStateBytes))
                : (byte[])initialCharacterStateBytes.Clone();
            if (m_InitialCharacterStateBytes.Length == 0)
                throw new ArgumentException("Initial Character state bytes are empty.", nameof(initialCharacterStateBytes));
            Roster = roster;
            WorldBodyBindingId = DotRecastAuthorityManifestIdentity.Require(worldBodyBindingId, nameof(worldBodyBindingId));
            InitialCharacterStateHash = initialCharacterStateHash;
            InitialBody = initialBody;
            ContactShape = contactShape;
            OutputRoute = outputRoute;
        }

        public ServerAuthoritativeRosterEntry Roster { get; }
        public string WorldBodyBindingId { get; }
        public CharacterStateHash InitialCharacterStateHash { get; }
        public WorldBodyState InitialBody { get; }
        public ActorContactShape ContactShape { get; }
        public SimulationOutputRouteDescriptor OutputRoute { get; }
        public byte[] CopyInitialCharacterStateBytes() => (byte[])m_InitialCharacterStateBytes.Clone();
    }

    public sealed class DotRecastAuthorityRuntimeIdentitySet
    {
        public DotRecastAuthorityRuntimeIdentitySet(
            SimulationSessionId sessionId,
            SimulationSourceClockId sourceClockId,
            SimulationComponentIdentity snapshotCodec,
            SimulationComponentIdentity committer,
            SimulationComponentIdentity transport,
            SimulationComponentIdentity diagnostics)
        {
            if (!sessionId.IsValid || !sourceClockId.IsValid ||
                !HasRole(snapshotCodec, SimulationComponentRole.SnapshotCodec) ||
                !HasRole(committer, SimulationComponentRole.Committer) ||
                !HasRole(transport, SimulationComponentRole.Endpoint) ||
                !HasRole(diagnostics, SimulationComponentRole.Diagnostics))
            {
                throw new ArgumentException("DotRecast Authority runtime identity set is incomplete.");
            }
            SessionId = sessionId;
            SourceClockId = sourceClockId;
            SnapshotCodec = snapshotCodec;
            Committer = committer;
            Transport = transport;
            Diagnostics = diagnostics;
        }

        public SimulationSessionId SessionId { get; }
        public SimulationSourceClockId SourceClockId { get; }
        public SimulationComponentIdentity SnapshotCodec { get; }
        public SimulationComponentIdentity Committer { get; }
        public SimulationComponentIdentity Transport { get; }
        public SimulationComponentIdentity Diagnostics { get; }

        static bool HasRole(SimulationComponentIdentity identity, SimulationComponentRole role) =>
            identity.IsValid && identity.Role == role;
    }

    public sealed class DotRecastAuthoritySceneManifest
    {
        readonly ReadOnlyCollection<DotRecastAuthorityActorBinding> m_Roster;

        public DotRecastAuthoritySceneManifest(
            HostProductId hostProductId,
            string hostId,
            DotRecastAuthoritySceneIdentity scene,
            ServerAuthoritativeRoomId roomId,
            DotRecastAuthorityEndpointDescriptor dataEndpoint,
            DotRecastAuthorityProgramArtifactBinding program,
            DotRecastAuthorityPipelineBinding pipeline,
            DotRecastAuthorityWorldBinding world,
            DotRecastAuthorityRuntimeIdentitySet runtime,
            IEnumerable<DotRecastAuthorityActorBinding> roster)
        {
            HostProductId = hostProductId.IsValid
                ? hostProductId
                : throw new ArgumentException("DotRecast Authority Scene manifest requires a Host Product identity.", nameof(hostProductId));
            HostId = DotRecastAuthorityManifestIdentity.Require(hostId, nameof(hostId));
            Scene = scene;
            if (!roomId.IsValid)
                throw new ArgumentException("DotRecast Authority Scene manifest requires a RoomId.", nameof(roomId));
            RoomId = roomId;
            DataEndpoint = dataEndpoint;
            Program = program ?? throw new ArgumentNullException(nameof(program));
            Pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            World = world ?? throw new ArgumentNullException(nameof(world));
            Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            DotRecastAuthorityHostProduct.Descriptor.RequireAuthoritySolver(world.SolverDefinition);
            if (pipeline.TickRate <= 0 || !program.NumericProfileId.Equals(Float32SimulationNumericProfile.Value.Id) ||
                !program.TargetAbiVersion.Equals(Float32SimulationNumericProfile.Value.AbiVersion) ||
                !pipeline.BackendIdentity.Equals(Float32PassExecutionBackend.Descriptor.Identity))
            {
                throw new ArgumentException("DotRecast Authority Scene manifest does not select the formal Float32/DotRecast target.");
            }
            var actors = roster == null ? new List<DotRecastAuthorityActorBinding>() : new List<DotRecastAuthorityActorBinding>(roster);
            actors.Sort((left, right) => left.Roster.ActorId.CompareTo(right.Roster.ActorId));
            if (actors.Count == 0)
                throw new ArgumentException("DotRecast Authority Scene manifest requires a locked roster.", nameof(roster));
            for (int i = 0; i < actors.Count; i++)
            {
                if (actors[i] == null || i > 0 && actors[i - 1].Roster.ActorId == actors[i].Roster.ActorId)
                    throw new ArgumentException("DotRecast Authority Scene roster is missing or duplicated.", nameof(roster));
                if (i > 0 && actors[i].ContactShape != actors[0].ContactShape)
                    throw new ArgumentException("DotRecast Authority Scene roster must use the canonical World contact shape.", nameof(roster));
            }
            StableHash expectedWorldConfigurationHash = DotRecastWorldConfigurationIdentity.Compute(
                world.NavigationSurfaceConfigurationHash,
                actors[0].ContactShape,
                world.ContactConfiguration);
            if (!expectedWorldConfigurationHash.Equals(world.WorldConfigurationHash))
                throw new ArgumentException("DotRecast Authority Scene WorldConfigurationHash does not cover the contact configuration.", nameof(world));
            m_Roster = actors.AsReadOnly();
            ManifestHash = DotRecastAuthoritySceneManifestCodec.ComputeHash(this);
        }

        public const string Magic = "thirdperson.dotrecast-authority-scene-manifest";
        public const int SchemaVersion = 5;
        public const string PublishDirectoryName = "Authority";
        public const string FileName = "DotRecastAuthorityScene.manifest";
        public HostProductId HostProductId { get; }
        public string HostId { get; }
        public DotRecastAuthoritySceneIdentity Scene { get; }
        public ServerAuthoritativeRoomId RoomId { get; }
        public DotRecastAuthorityEndpointDescriptor DataEndpoint { get; }
        public DotRecastAuthorityProgramArtifactBinding Program { get; }
        public DotRecastAuthorityPipelineBinding Pipeline { get; }
        public DotRecastAuthorityWorldBinding World { get; }
        public DotRecastAuthorityRuntimeIdentitySet Runtime { get; }
        public IReadOnlyList<DotRecastAuthorityActorBinding> Roster => m_Roster;
        public StableHash ManifestHash { get; }
    }

    public static class DotRecastAuthorityRelativePath
    {
        public static string Require(string value, string parameter)
        {
            if (string.IsNullOrWhiteSpace(value) || Path.IsPathRooted(value))
                throw new ArgumentException("Manifest artifact path must be relative.", parameter);
            string normalized = value.Trim().Replace('\\', '/');
            string[] segments = normalized.Split('/');
            if (segments.Length == 0)
                throw new ArgumentException("Manifest artifact path is empty.", parameter);
            for (int i = 0; i < segments.Length; i++)
            {
                if (string.IsNullOrEmpty(segments[i]) || segments[i] == "." || segments[i] == ".." || segments[i].IndexOf(':') >= 0)
                    throw new ArgumentException("Manifest artifact path contains an invalid segment.", parameter);
            }
            return string.Join("/", segments);
        }

        public static string ResolveUnderRoot(string rootDirectory, string relativePath)
        {
            string root = Path.GetFullPath(string.IsNullOrWhiteSpace(rootDirectory)
                ? throw new ArgumentException("Manifest root directory is required.", nameof(rootDirectory))
                : rootDirectory);
            string relative = Require(relativePath, nameof(relativePath));
            string resolved = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            string prefix = root.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? root
                : root + Path.DirectorySeparatorChar;
            if (!resolved.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Manifest artifact path escapes its root directory.");
            return resolved;
        }
    }

    static class DotRecastAuthorityManifestIdentity
    {
        public static string Require(string value, string parameter, int maximumLength = 128)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Manifest identity is required.", parameter);
            string normalized = value.Trim();
            if (normalized.Length > maximumLength)
                throw new ArgumentOutOfRangeException(parameter);
            return normalized;
        }
    }
}
