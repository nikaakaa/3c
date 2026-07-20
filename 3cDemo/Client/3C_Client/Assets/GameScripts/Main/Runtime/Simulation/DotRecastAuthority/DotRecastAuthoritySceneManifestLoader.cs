using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using ThirdPersonSimulation.DotRecast;
using ThirdPersonSimulation.ServerAuthoritative;

namespace ThirdPersonSimulation.DotRecastAuthority
{
    public sealed class LoadedDotRecastAuthorityActor
    {
        public LoadedDotRecastAuthorityActor(DotRecastAuthorityActorBinding binding, CharacterSimulationState initialState)
        {
            Binding = binding ?? throw new ArgumentNullException(nameof(binding));
            InitialState = initialState ?? throw new ArgumentNullException(nameof(initialState));
        }

        public DotRecastAuthorityActorBinding Binding { get; }
        public CharacterSimulationState InitialState { get; }
    }

    public sealed class LoadedDotRecastAuthoritySceneManifest
    {
        readonly byte[] m_ProgramBytes;
        readonly byte[] m_NavigationSurfaceBytes;
        readonly ReadOnlyCollection<LoadedDotRecastAuthorityActor> m_Roster;

        internal LoadedDotRecastAuthoritySceneManifest(
            string manifestPath,
            DotRecastAuthoritySceneManifest manifest,
            LoadedCharacterTargetProgramArtifact programArtifact,
            byte[] programBytes,
            NavigationSurfaceArtifact navigationSurface,
            byte[] navigationSurfaceBytes,
            ServerAuthoritativeAuthorityPipelineCatalogSet pipelineCatalog,
            IEnumerable<LoadedDotRecastAuthorityActor> roster)
        {
            ManifestPath = manifestPath;
            Manifest = manifest;
            ProgramArtifact = programArtifact;
            m_ProgramBytes = (byte[])programBytes.Clone();
            NavigationSurface = navigationSurface;
            m_NavigationSurfaceBytes = (byte[])navigationSurfaceBytes.Clone();
            PipelineCatalog = pipelineCatalog;
            m_Roster = new List<LoadedDotRecastAuthorityActor>(roster).AsReadOnly();
        }

        public string ManifestPath { get; }
        public DotRecastAuthoritySceneManifest Manifest { get; }
        public LoadedCharacterTargetProgramArtifact ProgramArtifact { get; }
        public CharacterSimulationProgram Program => ProgramArtifact.Program;
        public NavigationSurfaceArtifact NavigationSurface { get; }
        public ServerAuthoritativeAuthorityPipelineCatalogSet PipelineCatalog { get; }
        public IReadOnlyList<LoadedDotRecastAuthorityActor> Roster => m_Roster;
        public byte[] CopyProgramBytes() => (byte[])m_ProgramBytes.Clone();
        public byte[] CopyNavigationSurfaceBytes() => (byte[])m_NavigationSurfaceBytes.Clone();
    }

    public static class DotRecastAuthoritySceneManifestLoader
    {
        public static LoadedDotRecastAuthoritySceneManifest LoadFile(string manifestPath)
        {
            if (string.IsNullOrWhiteSpace(manifestPath))
                throw new ArgumentException("An explicit DotRecast Authority Scene manifest path is required.", nameof(manifestPath));
            string fullManifestPath = Path.GetFullPath(manifestPath);
            if (!File.Exists(fullManifestPath))
                throw new FileNotFoundException("DotRecast Authority Scene manifest does not exist.", fullManifestPath);
            byte[] manifestBytes = File.ReadAllBytes(fullManifestPath);
            DotRecastAuthoritySceneManifest manifest = DotRecastAuthoritySceneManifestCodec.Read(manifestBytes);
            string root = Path.GetDirectoryName(fullManifestPath) ?? throw new InvalidDataException("Manifest has no parent directory.");

            string programPath = DotRecastAuthorityRelativePath.ResolveUnderRoot(root, manifest.Program.RelativePath);
            string surfacePath = DotRecastAuthorityRelativePath.ResolveUnderRoot(root, manifest.World.NavigationSurfaceRelativePath);
            byte[] programBytes = ReadRequiredArtifact(programPath, "Program");
            byte[] surfaceBytes = ReadRequiredArtifact(surfacePath, "Navigation surface");
            LoadedCharacterTargetProgramArtifact programArtifact = LoadProgram(manifest.Program, programBytes);
            NavigationSurfaceArtifact surface = LoadNavigationSurface(
                manifest.World,
                manifest.Roster[0].ContactShape,
                surfaceBytes);
            ServerAuthoritativeAuthorityPipelineCatalogSet pipelineCatalog = LoadPipeline(manifest, programArtifact.Program);
            IReadOnlyList<LoadedDotRecastAuthorityActor> roster = LoadRoster(manifest, programArtifact.Program);
            return new LoadedDotRecastAuthoritySceneManifest(
                fullManifestPath,
                manifest,
                programArtifact,
                programBytes,
                surface,
                surfaceBytes,
                pipelineCatalog,
                roster);
        }

        static LoadedCharacterTargetProgramArtifact LoadProgram(
            DotRecastAuthorityProgramArtifactBinding expected,
            byte[] bytes)
        {
            StableHash bytesHash = CharacterTargetProgramArtifactLoader.ComputeBytesHash(bytes);
            if (bytes.Length != expected.ArtifactByteLength || !bytesHash.Equals(expected.ArtifactBytesHash))
                throw new InvalidDataException("Program artifact bytes do not match the manifest.");
            LoadedCharacterTargetProgramArtifact artifact = CharacterTargetProgramArtifactLoader.Inspect(expected.DefinitionGuid, bytes);
            CharacterTargetProgramArtifactDescriptor actual = artifact.Descriptor;
            if (!actual.ProgramId.Equals(expected.ProgramId) ||
                !actual.ProgramHash.Equals(expected.ProgramHash) ||
                !actual.LayoutHash.Equals(expected.LayoutHash) ||
                !actual.CanonicalBytesHash.Equals(expected.ArtifactBytesHash) ||
                actual.CanonicalByteLength != expected.ArtifactByteLength ||
                !string.Equals(actual.CompilerVersion, expected.CompilerVersion, StringComparison.Ordinal) ||
                !actual.OperationSetVersion.Equals(expected.OperationSetVersion) ||
                !actual.SourceRevision.Equals(expected.SourceRevision) ||
                !actual.SemanticHash.Equals(expected.SemanticHash) ||
                !actual.NumericProfileId.Equals(expected.NumericProfileId) ||
                !actual.TargetAbiVersion.Equals(expected.TargetAbiVersion) ||
                actual.RequiredWorldCapabilities != expected.RequiredWorldCapabilities)
            {
                throw new InvalidDataException("Program artifact identity does not match the manifest.");
            }
            if (!actual.NumericProfileId.Equals(Float32SimulationNumericProfile.Value.Id) ||
                !actual.TargetAbiVersion.Equals(Float32SimulationNumericProfile.Value.AbiVersion) ||
                !actual.OperationSetVersion.Equals(SimulationKernel.SpecializationManifest.OperationSetVersion))
            {
                throw new InvalidDataException("Program artifact does not target the formal Float32 Kernel ABI.");
            }
            return artifact;
        }

        static NavigationSurfaceArtifact LoadNavigationSurface(
            DotRecastAuthorityWorldBinding expected,
            ActorContactShape contactShape,
            byte[] bytes)
        {
            StableHash bytesHash = SimulationCanonicalPayloadHash.Compute(bytes);
            if (bytes.Length != expected.NavigationSurfaceByteLength || !bytesHash.Equals(expected.NavigationSurfaceBytesHash))
                throw new InvalidDataException("Navigation surface artifact bytes do not match the manifest.");
            NavigationSurfaceArtifact surface = NavigationSurfaceArtifactCodec.Read(bytes);
            if (!string.Equals(surface.MapId, expected.MapId, StringComparison.Ordinal) ||
                !string.Equals(surface.WorldRevision, expected.WorldRevision.Value, StringComparison.Ordinal) ||
                !surface.ContentHash.Equals(expected.NavigationSurfaceContentHash) ||
                !surface.QueryProfile.ConfigurationHash.Equals(expected.QueryProfileHash) ||
                !surface.WorldConfigurationHash.Equals(expected.NavigationSurfaceConfigurationHash) ||
                !DotRecastWorldConfigurationIdentity.Compute(
                    surface.WorldConfigurationHash,
                    contactShape,
                    expected.ContactConfiguration).Equals(expected.WorldConfigurationHash))
            {
                throw new InvalidDataException("Navigation surface identity does not match the manifest.");
            }
            CharacterWorldSolverDescriptor solver = DotRecastWorldSolver.DescriptorDefinition;
            SimulationWorldSolverDefinitionDescriptor definition = expected.SolverDefinition;
            StableHash definitionConfigurationHash = DotRecastWorldConfigurationIdentity.ComputeSolverDefinition(
                DotRecastWorldConfigurationIdentity.WorldSolverDefinitionComponentId,
                DotRecastWorldConfigurationIdentity.WorldSolverDefinitionSemanticVersion,
                surface.WorldConfigurationHash,
                contactShape,
                expected.ContactConfiguration,
                expected.SolverCapabilities,
                expected.SolverFeatures);
            if (!definition.NumericProfileId.Equals(Float32SimulationNumericProfile.Value.Id) ||
                !definition.TargetAbiVersion.Equals(Float32SimulationNumericProfile.Value.AbiVersion) ||
                definition.Identity.Role != SimulationComponentRole.WorldSolver ||
                !string.Equals(definition.Identity.ComponentId, DotRecastWorldConfigurationIdentity.WorldSolverDefinitionComponentId, StringComparison.Ordinal) ||
                !string.Equals(definition.Identity.SemanticVersion, DotRecastWorldConfigurationIdentity.WorldSolverDefinitionSemanticVersion, StringComparison.Ordinal) ||
                !definition.Identity.ConfigurationHash.Equals(definitionConfigurationHash) ||
                !solver.ImplementationId.Equals(expected.SolverId) ||
                !string.Equals(solver.Version, expected.SolverVersion, StringComparison.Ordinal) ||
                solver.Capabilities != expected.SolverCapabilities ||
                solver.Features != expected.SolverFeatures ||
                (definition.ExecutionSupport & SimulationPipelineExecutionSupport.Authoritative) == 0)
            {
                throw new InvalidDataException("DotRecast Solver identity does not match the manifest.");
            }
            return surface;
        }

        static ServerAuthoritativeAuthorityPipelineCatalogSet LoadPipeline(
            DotRecastAuthoritySceneManifest manifest,
            CharacterSimulationProgram program)
        {
            DotRecastAuthorityPipelineBinding expected = manifest.Pipeline;
            if (!expected.BackendIdentity.Equals(Float32PassExecutionBackend.Descriptor.Identity) ||
                expected.TickRate != program.Manifest.TickRate)
            {
                throw new InvalidDataException("Authority Pipeline Backend or TickRate does not match the Program.");
            }
            expected.ReplicationPolicy.RequireProgramCoverage(program);
            ServerAuthoritativeAuthorityPipelineCatalogSet catalog = ServerAuthoritativeAuthorityPipelineCatalog.Create(
                expected.SourcePolicy.ModelPolicy,
                expected.ReplicationPolicy);
            SimulationPipelineDescriptor descriptor = catalog.Descriptor;
            if (!descriptor.PipelineId.Equals(expected.Identity.Id) ||
                !descriptor.Revision.Equals(expected.Identity.Revision) ||
                !descriptor.SchemaVersion.Equals(expected.Identity.SchemaVersion) ||
                !descriptor.DescriptorHash.Equals(expected.DescriptorHash))
            {
                throw new InvalidDataException("Authority Pipeline catalog does not match the manifest descriptor.");
            }
            return catalog;
        }

        static IReadOnlyList<LoadedDotRecastAuthorityActor> LoadRoster(
            DotRecastAuthoritySceneManifest manifest,
            CharacterSimulationProgram program)
        {
            var actors = new LoadedDotRecastAuthorityActor[manifest.Roster.Count];
            for (int i = 0; i < actors.Length; i++)
            {
                DotRecastAuthorityActorBinding binding = manifest.Roster[i];
                CharacterSimulationState state = CharacterSimulationStateCodec.Read(
                    binding.CopyInitialCharacterStateBytes(),
                    program);
                CharacterStateHash stateHash = CharacterSimulationStateCodec.ComputeHash(state);
                if (!stateHash.Equals(binding.InitialCharacterStateHash))
                    throw new InvalidDataException($"Initial Character state hash for Actor '{binding.Roster.ActorId}' does not match the manifest.");
                if (state.LastCompletedTick != 0)
                    throw new InvalidDataException($"Initial Character state for Actor '{binding.Roster.ActorId}' is not at Tick 0.");
                actors[i] = new LoadedDotRecastAuthorityActor(binding, state);
            }
            return actors;
        }

        static byte[] ReadRequiredArtifact(string path, string label)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"{label} artifact does not exist.", path);
            byte[] bytes = File.ReadAllBytes(path);
            if (bytes.Length == 0)
                throw new InvalidDataException($"{label} artifact is empty.");
            return bytes;
        }
    }
}
