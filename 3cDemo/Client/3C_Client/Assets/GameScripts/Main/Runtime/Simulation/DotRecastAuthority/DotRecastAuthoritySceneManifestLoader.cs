using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using ThirdPersonSimulation;
using ThirdPersonSimulation.DotRecast;
using ThirdPersonSimulation.ServerAuthoritative;

namespace ThirdPersonSimulation.DotRecastAuthority
{
    public sealed class LoadedDotRecastAuthorityActor
    {
        public LoadedDotRecastAuthorityActor(
            DotRecastAuthorityActorBinding binding,
            SimulationActorBinding characterBinding,
            Float32CharacterRuntimeState initialState)
        {
            Binding = binding ?? throw new ArgumentNullException(nameof(binding));
            CharacterBinding = characterBinding ?? throw new ArgumentNullException(nameof(characterBinding));
            InitialState = initialState ?? throw new ArgumentNullException(nameof(initialState));
        }

        public DotRecastAuthorityActorBinding Binding { get; }
        public SimulationActorBinding CharacterBinding { get; }
        public Float32CharacterRuntimeState InitialState { get; }
    }

    public sealed class LoadedDotRecastAuthoritySceneManifest
    {
        readonly byte[] m_NavigationSurfaceBytes;
        readonly ReadOnlyCollection<LoadedDotRecastAuthorityActor> m_Roster;

        internal LoadedDotRecastAuthoritySceneManifest(
            string manifestPath,
            DotRecastAuthoritySceneManifest manifest,
            Float32CharacterRuntime characterRuntime,
            NavigationSurfaceArtifact navigationSurface,
            byte[] navigationSurfaceBytes,
            ServerAuthoritativeAuthorityPipelineCatalogSet pipelineCatalog,
            IEnumerable<LoadedDotRecastAuthorityActor> roster)
        {
            ManifestPath = manifestPath;
            Manifest = manifest;
            CharacterRuntime = characterRuntime;
            NavigationSurface = navigationSurface;
            m_NavigationSurfaceBytes = (byte[])navigationSurfaceBytes.Clone();
            PipelineCatalog = pipelineCatalog;
            m_Roster = new List<LoadedDotRecastAuthorityActor>(roster).AsReadOnly();
        }

        public string ManifestPath { get; }
        public DotRecastAuthoritySceneManifest Manifest { get; }
        public Float32CharacterRuntime CharacterRuntime { get; }
        public NavigationSurfaceArtifact NavigationSurface { get; }
        public ServerAuthoritativeAuthorityPipelineCatalogSet PipelineCatalog { get; }
        public IReadOnlyList<LoadedDotRecastAuthorityActor> Roster => m_Roster;
        public byte[] CopyNavigationSurfaceBytes() => (byte[])m_NavigationSurfaceBytes.Clone();
    }

    public static class DotRecastAuthoritySceneManifestLoader
    {
        public static LoadedDotRecastAuthoritySceneManifest LoadFile(
            string manifestPath,
            CharacterControlModuleCatalog controlModules)
        {
            if (string.IsNullOrWhiteSpace(manifestPath))
                throw new ArgumentException("An explicit DotRecast Authority Scene manifest path is required.", nameof(manifestPath));
            if (controlModules == null)
                throw new ArgumentNullException(nameof(controlModules));
            string fullManifestPath = Path.GetFullPath(manifestPath);
            if (!File.Exists(fullManifestPath))
                throw new FileNotFoundException("DotRecast Authority Scene manifest does not exist.", fullManifestPath);
            byte[] manifestBytes = File.ReadAllBytes(fullManifestPath);
            DotRecastAuthoritySceneManifest manifest = DotRecastAuthoritySceneManifestCodec.Read(manifestBytes);
            string root = Path.GetDirectoryName(fullManifestPath) ?? throw new InvalidDataException("Manifest has no parent directory.");
            SimulationExecutionTargetManifest target = Float32SimulationTarget.Manifest.ExecutionTarget;
            GameplayAbilityExecutionDataSet<Float32GameplayAbilityExecutionData> abilityData = LoadAbilities(manifest, root);
            SimulationActorBinding[] characterBindings = BuildCharacterBindings(manifest, abilityData);
            Float32CharacterRuntime characterRuntime = new Float32CharacterRuntime(
                characterBindings,
                target.NumericProfile,
                manifest.Pipeline.TickRate,
                target.OperationSetVersion,
                controlModules);
            string surfacePath = DotRecastAuthorityRelativePath.ResolveUnderRoot(root, manifest.World.NavigationSurfaceRelativePath);
            byte[] surfaceBytes = ReadRequiredArtifact(surfacePath, "Navigation surface");
            NavigationSurfaceArtifact surface = LoadNavigationSurface(
                manifest.World,
                manifest.Roster[0].ContactShape,
                surfaceBytes);
            ServerAuthoritativeAuthorityPipelineCatalogSet pipelineCatalog = LoadPipeline(manifest, characterRuntime);
            IReadOnlyList<LoadedDotRecastAuthorityActor> roster = LoadRoster(manifest, characterRuntime);
            return new LoadedDotRecastAuthoritySceneManifest(
                fullManifestPath,
                manifest,
                characterRuntime,
                surface,
                surfaceBytes,
                pipelineCatalog,
                roster);
        }

        static GameplayAbilityExecutionDataSet<Float32GameplayAbilityExecutionData> LoadAbilities(
            DotRecastAuthoritySceneManifest manifest,
            string root)
        {
            var data = new List<Float32GameplayAbilityExecutionData>(manifest.Abilities.Count);
            for (int i = 0; i < manifest.Abilities.Count; i++)
            {
                DotRecastAuthorityAbilityArtifactBinding expected = manifest.Abilities[i];
                string path = DotRecastAuthorityRelativePath.ResolveUnderRoot(root, expected.RelativePath);
                byte[] bytes = ReadRequiredArtifact(path, $"Ability '{expected.AbilityId}'");
                data.Add(LoadAbility(expected, bytes, manifest.ProviderBinding));
            }
            return new GameplayAbilityExecutionDataSet<Float32GameplayAbilityExecutionData>(data, value => value.AbilityId);
        }

        static Float32GameplayAbilityExecutionData LoadAbility(
            DotRecastAuthorityAbilityArtifactBinding expected,
            byte[] bytes,
            GameplayAbilityProviderBinding providerBinding)
        {
            StableHash bytesHash = Float32GameplayAbilityExecutionDataCodec.ComputeCanonicalBytesHash(bytes);
            if (bytes.Length != expected.ArtifactByteLength || !bytesHash.Equals(expected.ArtifactBytesHash))
                throw new InvalidDataException($"Ability '{expected.AbilityId}' artifact bytes do not match the manifest.");
            Float32GameplayAbilityExecutionData data = Float32GameplayAbilityExecutionDataCodec.ReadArtifact(
                bytes,
                new Float32GameplayAbilityExecutionDataLoadExpectation(
                    expected.AbilityGuid,
                    expected.AbilityId.Value,
                    expected.CompilerVersion,
                    expected.OperationSetVersion.Value,
                    expected.SourceRevision.Value,
                    expected.SemanticHash.ToString(),
                    expected.NumericProfileId.Value,
                    expected.TargetAbiVersion.Value,
                    expected.ExecutionIdentity,
                    expected.ContentHash.ToString(),
                    expected.StateSchemaHash.ToString(),
                    expected.ArtifactBytesHash.ToString(),
                    expected.Root));
            data.ProviderContract.RequireBinding(providerBinding);
            if (!data.AbilityId.Equals(expected.AbilityId) ||
                !data.ContentHash.Equals(expected.ContentHash) ||
                !data.StateSchemaHash.Equals(expected.StateSchemaHash) ||
                !data.OperationSetVersion.Equals(expected.OperationSetVersion) ||
                data.TickRate != expected.TickRate ||
                !data.SourceRevision.Equals(expected.SourceRevision) ||
                !data.SemanticHash.Equals(expected.SemanticHash) ||
                data.NumericProfile.Id != expected.NumericProfileId ||
                data.NumericProfile.AbiVersion.Value != expected.TargetAbiVersion.Value ||
                !string.Equals(data.ExecutionIdentity, expected.ExecutionIdentity, StringComparison.Ordinal) ||
                data.Root != expected.Root ||
                data.Capabilities.RequiredWorldCapabilities != expected.RequiredWorldCapabilities)
            {
                throw new InvalidDataException($"Ability '{expected.AbilityId}' identity does not match the manifest.");
            }
            return data;
        }

        static SimulationActorBinding[] BuildCharacterBindings(
            DotRecastAuthoritySceneManifest manifest,
            GameplayAbilityExecutionDataSet<Float32GameplayAbilityExecutionData> abilityData)
        {
            var bindings = new SimulationActorBinding[manifest.Roster.Count];
            for (int i = 0; i < bindings.Length; i++)
            {
                DotRecastAuthorityActorBinding actor = manifest.Roster[i];
                bindings[i] = new SimulationActorBinding(
                    actor.Roster.ActorId,
                    actor.WorldBodyBindingId,
                    manifest.ControlRuntimeBinding,
                    manifest.BodyMotionBinding,
                    manifest.GameplayEffectRuntimeBinding,
                    manifest.EquipmentRuntimeBinding,
                    abilityData);
            }
            return bindings;
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
            Float32CharacterRuntime characterRuntime)
        {
            DotRecastAuthorityPipelineBinding expected = manifest.Pipeline;
            if (!expected.BackendIdentity.Equals(Float32PassExecutionBackend.Descriptor.Identity) ||
                expected.TickRate != characterRuntime.TickRate)
            {
                throw new InvalidDataException("Authority Pipeline Backend or TickRate does not match the Character Runtime.");
            }
            expected.ReplicationPolicy.RequireCharacterCoverage(characterRuntime);
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
            Float32CharacterRuntime characterRuntime)
        {
            var actors = new LoadedDotRecastAuthorityActor[manifest.Roster.Count];
            for (int i = 0; i < actors.Length; i++)
            {
                DotRecastAuthorityActorBinding binding = manifest.Roster[i];
                SimulationActorBinding characterBinding = characterRuntime.Roster[i];
                if (binding.Roster.ActorId != characterBinding.ActorId)
                    throw new InvalidDataException("Manifest Actor roster does not match the Character Runtime roster.");
                GameplayContentHash contentHash = new GameplayContentHash(characterBinding.GameplayContentHash);
                Float32CharacterRuntimeState state = Float32CharacterRuntimeStateCodec.Read(
                    binding.CopyInitialCharacterStateBytes(),
                    characterBinding.AbilityInstallations,
                    contentHash,
                    characterBinding.GameplayEffectRuntimeBinding,
                    characterBinding.EquipmentRuntimeBinding);
                CharacterStateHash stateHash = Float32CharacterRuntimeStateCodec.ComputeHash(state);
                if (!stateHash.Equals(binding.InitialCharacterStateHash))
                    throw new InvalidDataException($"Initial Character state hash for Actor '{binding.Roster.ActorId}' does not match the manifest.");
                if (state.LastCompletedTick != 0)
                    throw new InvalidDataException($"Initial Character state for Actor '{binding.Roster.ActorId}' is not at Tick 0.");
                actors[i] = new LoadedDotRecastAuthorityActor(binding, characterBinding, state);
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
