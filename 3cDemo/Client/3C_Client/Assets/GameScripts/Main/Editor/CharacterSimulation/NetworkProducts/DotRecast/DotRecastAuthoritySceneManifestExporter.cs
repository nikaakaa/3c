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
        const string ProgramRelativePath = "Artifacts/CharacterProgram.csim";
        const string NavigationRelativePath = "Artifacts/NavigationSurface.navsurface";

        public static LoadedDotRecastAuthoritySceneManifest Export(
            DotRecastAuthoritySceneManifestExportRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            string authorityDirectory = Path.Combine(request.ServerPublishDirectory, AuthorityRelativeDirectory);
            RequireEmptyOutput(authorityDirectory);
            CharacterPipelineDefinition definition = request.CharacterDefinition;
            CharacterSimulationProgramAsset programAsset = definition.SimulationProgram
                ? definition.SimulationProgram
                : throw new InvalidOperationException("Character Definition has no formal Simulation Program asset.");
            string definitionPath = AssetDatabase.GetAssetPath(definition);
            string definitionGuid = AssetDatabase.AssetPathToGUID(definitionPath);
            CharacterSimulationProgram program = programAsset.Load();
            byte[] programBytes = programAsset.CopyCanonicalArtifact();
            LoadedCharacterTargetProgramArtifact inspectedProgram = CharacterTargetProgramArtifactLoader.Inspect(definitionGuid, programBytes);
            if (!inspectedProgram.Program.ProgramHash.Equals(program.ProgramHash) ||
                !inspectedProgram.Program.LayoutHash.Equals(program.LayoutHash))
            {
                throw new InvalidOperationException("Character Definition Program asset does not match its canonical artifact.");
            }

            ServerAuthoritativeAuthoritySessionSourceDefinition source = request.AuthoritySource;
            ServerAuthoritativeHybridModelDefinition model = source.Model;
            model.RequireComplete();
            if (definition.SimulationTickRate != model.SimulationTickRate || program.Manifest.TickRate != model.SimulationTickRate)
                throw new InvalidOperationException("Character Definition, Program, and Authority Model TickRate do not match.");

            SimulationExecutionBackendDescriptor backend = request.ExecutionBackend.BuildPortableDescriptor();
            SimulationWorldSolverDefinitionDescriptor solver = request.WorldSolver.BuildDescriptor(model.SimulationTickRate);
            DotRecastAuthorityHostProduct.Descriptor.RequireAuthoritySolver(solver);
            ServerAuthoritativePipelineCompatibilityIdentity compatibility = model.BuildCompatibility(
                program,
                Float32ProgramRuntime.DescriptorDefinition,
                request.ExecutionBackend);
            SimulationPipelineDescriptor authorityPipeline = model.AuthorityPipeline.BuildPortableDescriptor();
            ServerAuthoritativeAuthoritySourcePolicy sourcePolicy = source.BuildPolicy();
            SimulationSessionSourceAuthoringDescriptor sourceDescriptor = source.BuildAuthoringDescriptor();
            NavigationSurfaceAsset surfaceAsset = request.WorldSolver.NavigationSurface
                ? request.WorldSolver.NavigationSurface
                : throw new InvalidOperationException("DotRecast World Solver has no Navigation Surface asset.");
            NavigationSurfaceArtifact surface = surfaceAsset.Load();
            byte[] surfaceBytes = surfaceAsset.CopyCanonicalArtifact();
            ActorContactShape contactShape = request.WorldSolver.ContactShape;
            ActorContactSolverConfiguration contactConfiguration = request.WorldSolver.ContactConfiguration;

            var actorBindings = new DotRecastAuthorityActorBinding[request.Roster.Count];
            var routes = new SimulationOutputRouteDescriptor[request.Roster.Count];
            for (int i = 0; i < actorBindings.Length; i++)
            {
                DotRecastAuthorityActorExportBinding actor = request.Roster[i];
                CharacterSimulationState state = CharacterSimulationState.CreateInitial(program);
                byte[] stateBytes = CharacterSimulationStateCodec.Write(state);
                if (actor.ContactShape != contactShape)
                    throw new InvalidOperationException($"DotRecast Authority Actor '{actor.Roster.ActorId}' contact shape does not match the World Solver configuration.");
                routes[i] = actor.OutputRoute;
                actorBindings[i] = new DotRecastAuthorityActorBinding(
                    actor.Roster,
                    actor.WorldBodyBindingId,
                    stateBytes,
                    CharacterSimulationStateCodec.ComputeHash(state),
                    actor.InitialBody,
                    actor.ContactShape,
                    actor.OutputRoute);
            }

            var programBinding = new DotRecastAuthorityProgramArtifactBinding(
                ProgramRelativePath,
                definitionGuid,
                program.Manifest.ProgramId,
                program.ProgramHash,
                program.LayoutHash,
                CharacterTargetProgramArtifactLoader.ComputeBytesHash(programBytes),
                programBytes.Length,
                program.Manifest.CompilerVersion,
                program.Manifest.OperationSetVersion,
                program.Manifest.SourceRevision,
                program.Manifest.SemanticHash,
                program.Manifest.NumericProfile.Id,
                program.Manifest.NumericProfile.AbiVersion,
                program.Manifest.Capabilities.RequiredWorldCapabilities);
            var pipelineBinding = new DotRecastAuthorityPipelineBinding(
                compatibility.PredictionPipeline,
                compatibility.AuthorityPipeline,
                authorityPipeline.DescriptorHash,
                backend.Identity,
                sourceDescriptor.Source,
                sourceDescriptor.SourcePorts,
                sourcePolicy,
                model.ReplicationPolicy);
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
                Float32SimulationSessionComposer.BuildSnapshotCodecIdentity(Float32ProgramRuntime.DescriptorDefinition, backend),
                DotRecastAuthorityRuntimeIdentityCatalog.BuildCommitter(routes),
                model.Endpoint.BuildIdentity(),
                DotRecastAuthorityRuntimeIdentityCatalog.BuildDiagnostics(DotRecastAuthorityHostProduct.ProductId));
            var manifest = new DotRecastAuthoritySceneManifest(
                DotRecastAuthorityHostProduct.ProductId,
                request.HostId,
                request.Scene,
                request.RoomId,
                request.DataEndpoint,
                programBinding,
                pipelineBinding,
                worldBinding,
                runtimeIdentities,
                actorBindings);

            string artifactsDirectory = Path.Combine(authorityDirectory, "Artifacts");
            Directory.CreateDirectory(artifactsDirectory);
            File.WriteAllBytes(Path.Combine(authorityDirectory, ProgramRelativePath.Replace('/', Path.DirectorySeparatorChar)), programBytes);
            File.WriteAllBytes(Path.Combine(authorityDirectory, NavigationRelativePath.Replace('/', Path.DirectorySeparatorChar)), surfaceBytes);
            string manifestPath = Path.Combine(authorityDirectory, ManifestFileName);
            File.WriteAllBytes(manifestPath, DotRecastAuthoritySceneManifestCodec.Write(manifest));
            return DotRecastAuthoritySceneManifestLoader.LoadFile(manifestPath);
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
