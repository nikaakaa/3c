using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThirdPersonSimulation
{
    public enum SimulationPipelineInitialStateMode : byte
    {
        CaptureActivatedDefaults = 1,
        RestoreProvidedSnapshot = 2
    }

    public sealed class SimulationPipelineInitialStateSource
    {
        static readonly SimulationPipelineInitialStateSource s_CaptureActivatedDefaults =
            new SimulationPipelineInitialStateSource(
                SimulationPipelineInitialStateMode.CaptureActivatedDefaults,
                null);

        SimulationPipelineInitialStateSource(
            SimulationPipelineInitialStateMode mode,
            SimulationPipelineStateSnapshot snapshot)
        {
            Mode = mode;
            Snapshot = snapshot;
        }

        public SimulationPipelineInitialStateMode Mode { get; }
        public SimulationPipelineStateSnapshot Snapshot { get; }
        public static SimulationPipelineInitialStateSource CaptureActivatedDefaults => s_CaptureActivatedDefaults;

        public static SimulationPipelineInitialStateSource Restore(SimulationPipelineStateSnapshot snapshot)
        {
            return new SimulationPipelineInitialStateSource(
                SimulationPipelineInitialStateMode.RestoreProvidedSnapshot,
                snapshot ?? throw new ArgumentNullException(nameof(snapshot)));
        }
    }

    public sealed class Float32PassBackendCompositionRequest
    {
        readonly ReadOnlyCollection<SimulationActorBinding> m_Roster;
        readonly ReadOnlyCollection<SimulationPortDescriptor> m_ExpectedSourcePorts;
        readonly ReadOnlyCollection<SimulationOutputRouteDescriptor> m_OutputRoutes;
        readonly ReadOnlyCollection<IDisposable> m_SourceResources;
        readonly ReadOnlyCollection<IDisposable> m_ActorResources;

        public Float32PassBackendCompositionRequest(
            SimulationSessionCompositionDescriptor descriptor,
            SimulationExecutionBackendDescriptor backend,
            CompiledSimulationPipelinePlan compiledPipeline,
            Float32ProgramRuntime programRuntime,
            SimulationWorldStateSet initialState,
            SimulationPipelineInitialStateSource pipelineInitialState,
            SimulationRuntimePortSet sourcePorts,
            IEnumerable<SimulationPortDescriptor> expectedSourcePorts,
            IFloat32SimulationRestoreSource restoreSource,
            ICharacterWorldSolver solver,
            IFloat32SimulationSessionSnapshotCodec snapshotCodec,
            IFloat32SimulationCommitter committer,
            SimulationComponentIdentity diagnosticsIdentity,
            ISimulationDiagnosticsSink diagnostics,
            IEnumerable<SimulationOutputRouteDescriptor> outputRoutes,
            Float32PipelinePassRuntimeFactoryCatalog passRuntimeFactories,
            Float32PipelineProductRuntimeCatalog productRuntimeFactories,
            IEnumerable<IDisposable> sourceResources = null,
            IEnumerable<IDisposable> actorResources = null)
        {
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            Backend = backend ?? throw new ArgumentNullException(nameof(backend));
            CompiledPipeline = compiledPipeline ?? throw new ArgumentNullException(nameof(compiledPipeline));
            ProgramRuntime = programRuntime ?? throw new ArgumentNullException(nameof(programRuntime));
            InitialState = initialState ?? throw new ArgumentNullException(nameof(initialState));
            PipelineInitialState = pipelineInitialState ?? throw new ArgumentNullException(nameof(pipelineInitialState));
            SourcePorts = sourcePorts ?? throw new ArgumentNullException(nameof(sourcePorts));
            RestoreSource = restoreSource;
            Solver = solver ?? throw new ArgumentNullException(nameof(solver));
            SnapshotCodec = snapshotCodec ?? throw new ArgumentNullException(nameof(snapshotCodec));
            Committer = committer ?? throw new ArgumentNullException(nameof(committer));
            if (!diagnosticsIdentity.IsValid || diagnosticsIdentity.Role != SimulationComponentRole.Diagnostics)
                throw new ArgumentException("Diagnostics identity is invalid.", nameof(diagnosticsIdentity));
            DiagnosticsIdentity = diagnosticsIdentity;
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            PassRuntimeFactories = passRuntimeFactories ?? throw new ArgumentNullException(nameof(passRuntimeFactories));
            ProductRuntimeFactories = productRuntimeFactories ?? throw new ArgumentNullException(nameof(productRuntimeFactories));
            m_Roster = FreezeRoster(programRuntime.Roster);
            m_ExpectedSourcePorts = FreezePorts(expectedSourcePorts);
            m_OutputRoutes = FreezeOutputRoutes(outputRoutes);
            m_SourceResources = FreezeResources(sourceResources, nameof(sourceResources));
            m_ActorResources = FreezeResources(actorResources, nameof(actorResources));
            Validate();
        }

        public SimulationSessionCompositionDescriptor Descriptor { get; }
        public SimulationExecutionBackendDescriptor Backend { get; }
        public CompiledSimulationPipelinePlan CompiledPipeline { get; }
        public Float32ProgramRuntime ProgramRuntime { get; }
        public SimulationProgramCatalog Catalog => ProgramRuntime.Catalog;
        public IReadOnlyList<SimulationActorBinding> Roster => m_Roster;
        public SimulationWorldStateSet InitialState { get; }
        public SimulationPipelineInitialStateSource PipelineInitialState { get; }
        public SimulationKernel Kernel => ProgramRuntime.Kernel;
        public SimulationRuntimePortSet SourcePorts { get; }
        public IReadOnlyList<SimulationPortDescriptor> ExpectedSourcePorts => m_ExpectedSourcePorts;
        public IFloat32SimulationRestoreSource RestoreSource { get; }
        public ICharacterWorldSolver Solver { get; }
        public IFloat32SimulationSessionSnapshotCodec SnapshotCodec { get; }
        public IFloat32SimulationCommitter Committer { get; }
        public SimulationComponentIdentity DiagnosticsIdentity { get; }
        public ISimulationDiagnosticsSink Diagnostics { get; }
        public IReadOnlyList<SimulationOutputRouteDescriptor> OutputRoutes => m_OutputRoutes;
        public Float32PipelinePassRuntimeFactoryCatalog PassRuntimeFactories { get; }
        public Float32PipelineProductRuntimeCatalog ProductRuntimeFactories { get; }
        public IReadOnlyList<IDisposable> SourceResources => m_SourceResources;
        public IReadOnlyList<IDisposable> ActorResources => m_ActorResources;

        void Validate()
        {
            SimulationNumericProfile profile = Catalog.NumericProfile;
            if (!Backend.Identity.Equals(Descriptor.ExecutionBackend) ||
                !CompiledPipeline.Backend.Equals(Descriptor.ExecutionBackend) ||
                !CompiledPipeline.Identity.Equals(Descriptor.Pipeline))
            {
                throw Failure("backend_composition_identity_mismatch", "Backend, compiled Pipeline and Session descriptor identities do not match.");
            }
            SimulationExecutionBackendTargetSupport support = Backend.RequireTarget(
                Descriptor.NumericProfileId,
                Descriptor.TargetAbiVersion,
                Descriptor.Pipeline.SchemaVersion);
            if (!profile.Id.Equals(Descriptor.NumericProfileId) ||
                !profile.AbiVersion.Equals(Descriptor.TargetAbiVersion) ||
                !Catalog.CatalogHash.Equals(Descriptor.ProgramCatalogHash) ||
                !Catalog.OperationSetVersion.Equals(Descriptor.OperationSetVersion) ||
                Catalog.TickRate != Descriptor.TickRate ||
                profile != Float32SimulationNumericProfile.Value)
            {
                throw Failure("float32_program_runtime_mismatch", "ProgramCatalog does not match the locked Float32 Program Runtime identity.");
            }
            if ((support.ExecutionSupport & CompiledPipeline.RequiredExecutionSupport) != CompiledPipeline.RequiredExecutionSupport ||
                CompiledPipeline.Deterministic && !support.Deterministic)
            {
                throw Failure("backend_execution_support_mismatch", "Execution Backend Target support does not cover the compiled Pipeline.");
            }
            if (!Kernel.Specialization.NumericProfile.Equals(profile) ||
                !Kernel.Specialization.OperationSetVersion.Equals(Catalog.OperationSetVersion))
            {
                throw Failure("kernel_specialization_mismatch", "Kernel specialization does not match ProgramCatalog.");
            }
            for (int i = 0; i < Catalog.Programs.Count; i++)
                Kernel.Specialization.RequireProgram(Catalog.Programs[i]);
            ValidateRoster();
            ValidateInitialState();
            ValidateSourcePorts();
            ValidateOutputRoutes();
            if (!Solver.Descriptor.NumericProfile.Equals(profile) ||
                !Solver.Descriptor.ImplementationId.Equals(Descriptor.SolverImplementationId) ||
                (Solver.Descriptor.Capabilities & Descriptor.SolverCapabilities) != Descriptor.SolverCapabilities ||
                Solver.Descriptor.Features != Descriptor.SolverFeatures ||
                !Descriptor.WorldSolver.IsValid)
            {
                throw Failure("world_solver_mismatch", "World Solver does not match the locked Session composition.");
            }
            WorldCapability requiredCapabilities = Catalog.RequiredWorldCapabilities | CollectPassWorldCapabilities();
            if (!Solver.Descriptor.Supports(requiredCapabilities) ||
                (Solver.Descriptor.Capabilities & WorldCapability.Reconstructible) == 0)
            {
                throw Failure("world_solver_capability_missing", $"World Solver is missing required capability '{requiredCapabilities & ~Solver.Descriptor.Capabilities}'.");
            }
            if (!SnapshotCodec.Identity.Equals(Descriptor.SnapshotCodec) || !Committer.Identity.Equals(Descriptor.Committer))
                throw Failure("session_output_component_mismatch", "Snapshot codec or Committer identity does not match the Session descriptor.");
            bool requiresRestore = (CompiledPipeline.RequiredExecutionSupport & SimulationPipelineExecutionSupport.Restore) != 0;
            if (requiresRestore != (RestoreSource != null))
                throw Failure("restore_source_mismatch", "Restore-capable Pipeline requires exactly one explicit restore Source port.");
            if (RestoreSource != null && !ContainsPortInstance(SourcePorts, RestoreSource))
                throw Failure("restore_source_port_missing", "Restore Source is not part of the locked Source port set.");
            ProductRuntimeFactories.RequireProducts(CompiledPipeline.Products);
            for (int i = 0; i < CompiledPipeline.Passes.Count; i++)
                PassRuntimeFactories.GetRequired(CompiledPipeline.Passes[i]);
        }

        WorldCapability CollectPassWorldCapabilities()
        {
            WorldCapability capabilities = WorldCapability.None;
            for (int i = 0; i < CompiledPipeline.Passes.Count; i++)
                capabilities |= CompiledPipeline.Passes[i].Descriptor.RequiredSolverCapabilities;
            return capabilities;
        }

        void ValidateRoster()
        {
            if (m_Roster.Count != Descriptor.Roster.Actors.Count || m_Roster.Count != InitialState.Actors.Count)
                throw Failure("actor_roster_count_mismatch", "Runtime roster does not match Session descriptor and initial state.");
            for (int i = 0; i < m_Roster.Count; i++)
            {
                SimulationActorBinding binding = m_Roster[i];
                if (!binding.ActorId.Equals(Descriptor.Roster.Actors[i]) ||
                    !binding.ActorId.Equals(InitialState.Actors[i].ActorId) ||
                    !binding.ActorId.Equals(InitialState.WorldState.Bodies[i].ActorId))
                {
                    throw Failure("actor_roster_binding_mismatch", $"Actor roster binding at index {i} does not match the Session descriptor.");
                }
                CharacterSimulationProgram program = Catalog.GetRequired(binding.ProgramId);
                if (!program.ProgramHash.Equals(binding.ProgramHash) || !program.LayoutHash.Equals(binding.LayoutHash) ||
                    !InitialState.Actors[i].State.ProgramHash.Equals(binding.ProgramHash) ||
                    !InitialState.Actors[i].State.LayoutHash.Equals(binding.LayoutHash))
                {
                    throw Failure("actor_program_binding_mismatch", $"Actor '{binding.ActorId}' Program binding is stale.");
                }
            }
        }

        void ValidateInitialState()
        {
            if (!InitialState.WorldState.NumericProfile.Equals(Catalog.NumericProfile) ||
                !InitialState.WorldState.SolverId.Equals(Solver.Descriptor.ImplementationId) ||
                !string.Equals(InitialState.WorldState.SolverVersion, Solver.Descriptor.Version, StringComparison.Ordinal))
            {
                throw Failure("initial_state_identity_mismatch", "Initial Character and World state identities do not match the composition.");
            }
            if (PipelineInitialState.Mode == SimulationPipelineInitialStateMode.RestoreProvidedSnapshot)
            {
                SimulationPipelineStateSnapshot snapshot = PipelineInitialState.Snapshot;
                if (snapshot == null || snapshot.LastCompletedTick != InitialState.LastCompletedTick ||
                    !snapshot.Pipeline.Equals(Descriptor.Pipeline) ||
                    !snapshot.Backend.Equals(Descriptor.ExecutionBackend))
                {
                    throw Failure("initial_pipeline_state_identity_mismatch", "Provided Pipeline state does not match the initial Character and World state.");
                }
            }
            else if (PipelineInitialState.Mode != SimulationPipelineInitialStateMode.CaptureActivatedDefaults ||
                     PipelineInitialState.Snapshot != null)
            {
                throw Failure("initial_pipeline_state_mode_invalid", "Pipeline initial state source is invalid.");
            }
        }

        void ValidateSourcePorts()
        {
            if (SourcePorts.Ports.Count != m_ExpectedSourcePorts.Count)
                throw Failure("source_port_count_mismatch", "Runtime Source port set does not match the compiled Source descriptor set.");
            for (int i = 0; i < SourcePorts.Ports.Count; i++)
            {
                SimulationPortDescriptor actual = SourcePorts.Ports[i].Descriptor;
                SimulationPortDescriptor expected = m_ExpectedSourcePorts[i];
                if (!string.Equals(actual.PortId, expected.PortId, StringComparison.Ordinal) ||
                    !string.Equals(actual.SchemaId, expected.SchemaId, StringComparison.Ordinal) ||
                    actual.SchemaVersion != expected.SchemaVersion || actual.Direction != expected.Direction ||
                    !string.Equals(actual.OwnerComponentId, expected.OwnerComponentId, StringComparison.Ordinal) ||
                    !actual.ConfigurationHash.Equals(expected.ConfigurationHash))
                {
                    throw Failure("source_port_identity_mismatch", $"Runtime Source port '{actual.PortId}' does not match the compiled Source descriptor.");
                }
            }
        }

        void ValidateOutputRoutes()
        {
            if (m_OutputRoutes.Count != Descriptor.Roster.Actors.Count)
                throw Failure("output_route_count_mismatch", "Session requires exactly one output route per Actor.");
            var actors = new HashSet<ActorId>();
            for (int i = 0; i < m_OutputRoutes.Count; i++)
            {
                if (!actors.Add(m_OutputRoutes[i].ActorId))
                    throw Failure("output_route_actor_duplicate", $"Actor '{m_OutputRoutes[i].ActorId}' has multiple output routes.");
            }
            for (int i = 0; i < Descriptor.Roster.Actors.Count; i++)
            {
                if (!actors.Contains(Descriptor.Roster.Actors[i]))
                    throw Failure("output_route_actor_missing", $"Actor '{Descriptor.Roster.Actors[i]}' has no output route.");
            }
        }

        static bool ContainsPortInstance(SimulationRuntimePortSet ports, ISimulationRuntimePort expected)
        {
            for (int i = 0; i < ports.Ports.Count; i++)
            {
                if (ReferenceEquals(ports.Ports[i], expected))
                    return true;
            }
            return false;
        }

        static ReadOnlyCollection<SimulationActorBinding> FreezeRoster(IEnumerable<SimulationActorBinding> roster)
        {
            var values = roster == null ? new List<SimulationActorBinding>() : new List<SimulationActorBinding>(roster);
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == null)
                    throw new ArgumentException("Runtime roster contains a missing Actor binding.", nameof(roster));
            }
            values.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            if (values.Count == 0)
                throw new ArgumentException("Runtime roster cannot be empty.", nameof(roster));
            for (int i = 1; i < values.Count; i++)
            {
                if (values[i - 1].ActorId.Equals(values[i].ActorId))
                    throw new ArgumentException("Runtime roster contains duplicate ActorId.", nameof(roster));
            }
            return values.AsReadOnly();
        }

        static ReadOnlyCollection<SimulationPortDescriptor> FreezePorts(IEnumerable<SimulationPortDescriptor> ports)
        {
            var values = ports == null ? new List<SimulationPortDescriptor>() : new List<SimulationPortDescriptor>(ports);
            values.Sort((left, right) => string.CompareOrdinal(left.PortId, right.PortId));
            for (int i = 1; i < values.Count; i++)
            {
                if (string.Equals(values[i - 1].PortId, values[i].PortId, StringComparison.Ordinal))
                    throw new ArgumentException("Expected Source port set contains duplicate identities.", nameof(ports));
            }
            return values.AsReadOnly();
        }

        static ReadOnlyCollection<SimulationOutputRouteDescriptor> FreezeOutputRoutes(
            IEnumerable<SimulationOutputRouteDescriptor> routes)
        {
            var values = routes == null
                ? new List<SimulationOutputRouteDescriptor>()
                : new List<SimulationOutputRouteDescriptor>(routes);
            values.Sort((left, right) => string.CompareOrdinal(left.RouteId, right.RouteId));
            for (int i = 1; i < values.Count; i++)
            {
                if (string.Equals(values[i - 1].RouteId, values[i].RouteId, StringComparison.Ordinal))
                    throw new ArgumentException("Output route set contains duplicate identities.", nameof(routes));
            }
            return values.AsReadOnly();
        }

        static ReadOnlyCollection<IDisposable> FreezeResources(IEnumerable<IDisposable> resources, string parameter)
        {
            var values = resources == null ? new List<IDisposable>() : new List<IDisposable>(resources);
            var unique = new HashSet<IDisposable>(ReferenceEqualityComparer<IDisposable>.Instance);
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == null || !unique.Add(values[i]))
                    throw new ArgumentException("Owned resource list contains a missing or duplicate instance.", parameter);
            }
            return values.AsReadOnly();
        }

        SimulationSessionCompositionException Failure(string code, string message)
        {
            return new SimulationSessionCompositionException(new SimulationSessionFailure(
                SimulationSessionFailureStage.Composition,
                code,
                message,
                Descriptor.ExecutionBackend.ToString()));
        }
    }

    sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        public static readonly ReferenceEqualityComparer<T> Instance = new ReferenceEqualityComparer<T>();
        public bool Equals(T left, T right) => ReferenceEquals(left, right);
        public int GetHashCode(T value) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
    }
}
