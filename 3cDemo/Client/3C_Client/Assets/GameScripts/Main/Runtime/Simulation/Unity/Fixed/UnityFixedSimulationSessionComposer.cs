using System;
using System.Collections.Generic;
using ThirdPersonSimulation;
using ThirdPersonSimulation.DeterministicKcc;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    internal sealed class UnityFixedSimulationSessionComposer : ISimulationSessionComposer
    {
        const string CommitterId = "thirdperson.simulation.committer.fixed-character-output";
        const string DiagnosticsId = "thirdperson.simulation.diagnostics.fixed-character-roster";
        readonly FixedProgramRuntimeDefinition m_ProgramRuntimeDefinition;

        public UnityFixedSimulationSessionComposer(FixedProgramRuntimeDefinition programRuntimeDefinition)
        {
            m_ProgramRuntimeDefinition = programRuntimeDefinition ? programRuntimeDefinition :
                throw new ArgumentNullException(nameof(programRuntimeDefinition));
        }

        public SimulationSessionPreparedRuntime Compose(SimulationSessionCompositionBuildRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (!ReferenceEquals(request.Definition.ProgramRuntime, m_ProgramRuntimeDefinition))
                throw Failure("program_runtime_definition_changed", "Composition Program Runtime Definition changed during preparation.");
            if (request.Source is not IFixedSimulationPreparedSource source)
                throw Failure("source_target_abi_mismatch", "Prepared Session Source does not implement the Fixed Source contract.");
            if (request.Definition.ExecutionBackend is not FixedPassExecutionBackendDefinition)
                throw Failure("backend_target_abi_mismatch", "Execution Backend Definition is not the canonical Fixed Pass Backend.");
            if (request.Definition.WorldSolver is not DeterministicKccWorldSolverDefinition solverDefinition)
                throw Failure("solver_target_abi_mismatch", "World Solver Definition is not the Deterministic KCC Solver.");

            IReadOnlyList<IFixedSimulationActorRegistration> registrations = RequireFixedRegistrations(request.Registrations);
            FixedProgramRuntime programRuntime = m_ProgramRuntimeDefinition.CreateRuntime(registrations);
            SimulationExecutionBackendDescriptor backend = request.Definition.ExecutionBackend.BuildPortableDescriptor();
            SimulationWorldSolverDefinitionDescriptor solverDescriptor = solverDefinition.BuildDescriptor(request.Definition.TickRate);
            SimulationComponentIdentity snapshotIdentity = FixedSimulationSessionComposer.BuildSnapshotCodecIdentity(programRuntime.Descriptor, backend);
            var snapshotCodec = new FixedSimulationSessionSnapshotCodec(snapshotIdentity);
            var outputs = new FixedSimulationOutputAggregate(registrations, source.MaximumBodySamplesPerActor);
            var diagnostics = new FixedSimulationDiagnosticsAggregate(registrations);
            SimulationComponentIdentity committerIdentity = BuildCommitterIdentity(registrations);
            FixedSimulationSourceRuntimeBinding binding = source.BindRuntime(
                new FixedSimulationSourceRuntimeBindingRequest(
                    request.Definition.Pipeline,
                    snapshotCodec,
                    outputs,
                    diagnostics,
                    committerIdentity,
                    registrations));
            SimulationPipelineDescriptor selectedPipeline = request.Definition.Pipeline.BuildPortableDescriptor();
            SimulationPipelineDescriptor runtimePipeline = binding.PipelineRuntimePackage.Pipeline;
            if (!selectedPipeline.PipelineId.Equals(runtimePipeline.PipelineId) ||
                !selectedPipeline.Revision.Equals(runtimePipeline.Revision) ||
                !selectedPipeline.SchemaVersion.Equals(runtimePipeline.SchemaVersion) ||
                !selectedPipeline.DescriptorHash.Equals(runtimePipeline.DescriptorHash))
            {
                throw Failure("pipeline_runtime_package_mismatch", "Fixed Source runtime package does not match the selected Pipeline Definition.");
            }

            DeterministicKccWorldSolver solver = null;
            try
            {
                solver = solverDefinition.CreateSolver(request.Definition.TickRate, registrations);
                ThirdPersonSimulation.Fixed.WorldSimulationState world = solver.Create(
                    new WorldRevision(request.Definition.WorldRevision),
                    InitialBodies(registrations));
                ThirdPersonSimulation.Fixed.SimulationWorldStateSet initialState = programRuntime.CreateInitialState(world);
                var portableRequest = new FixedSimulationSessionCompositionRequest(
                    new SimulationSessionId(request.Definition.SessionId),
                    new SimulationWorldId(request.Definition.WorldId),
                    new SimulationSourceClockId(request.Definition.SourceClockId),
                    request.Definition.TickRate,
                    programRuntime,
                    backend,
                    binding.PipelineRuntimePackage,
                    source.Descriptor,
                    source.RuntimePorts,
                    binding.RestoreSource,
                    solverDescriptor,
                    solver,
                    request.Definition.RequiredWorldFeatures,
                    initialState,
                    binding.PipelineInitialState,
                    binding.Committer,
                    BuildDiagnosticsIdentity(registrations),
                    diagnostics,
                    OutputRoutes(registrations),
                    new IDisposable[] { source },
                    registrations);
                FixedPassBackendCompositionResult result = binding.RuntimeLauncher.Launch(portableRequest);
                return new SimulationSessionPreparedRuntime(
                    result.LaunchPlan,
                    result.RuntimeHandle,
                    outputs,
                    source.Descriptor.OuterTickKind);
            }
            catch
            {
                solver?.Dispose();
                source.Dispose();
                throw;
            }
        }

        static IReadOnlyList<IFixedSimulationActorRegistration> RequireFixedRegistrations(
            IReadOnlyList<ISimulationActorRegistration> registrations)
        {
            if (registrations == null || registrations.Count == 0)
                throw Failure("actor_roster_missing", "Fixed Composer requires an Actor roster.");
            var values = new IFixedSimulationActorRegistration[registrations.Count];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = registrations[i] as IFixedSimulationActorRegistration ??
                    throw Failure(
                        "actor_target_abi_mismatch",
                        $"Actor '{registrations[i]?.ActorId}' does not provide a Fixed registration.");
            }
            return values;
        }

        static IReadOnlyList<ThirdPersonSimulation.Fixed.WorldBodyState> InitialBodies(
            IReadOnlyList<IFixedSimulationActorRegistration> registrations)
        {
            var values = new ThirdPersonSimulation.Fixed.WorldBodyState[registrations.Count];
            for (int i = 0; i < values.Length; i++)
                values[i] = registrations[i].InitialBody;
            return values;
        }

        static IReadOnlyList<SimulationOutputRouteDescriptor> OutputRoutes(
            IReadOnlyList<IFixedSimulationActorRegistration> registrations)
        {
            var values = new SimulationOutputRouteDescriptor[registrations.Count];
            for (int i = 0; i < values.Length; i++)
                values[i] = registrations[i].OutputRoute;
            return values;
        }

        static SimulationComponentIdentity BuildCommitterIdentity(
            IReadOnlyList<IFixedSimulationActorRegistration> registrations)
        {
            var values = new string[registrations.Count + 1];
            values[0] = CommitterId;
            for (int i = 0; i < registrations.Count; i++)
                values[i + 1] = $"{registrations[i].ActorId}:{registrations[i].OutputRoute.ConfigurationHash}";
            return new SimulationComponentIdentity(
                SimulationComponentRole.Committer,
                CommitterId,
                "2",
                StableHash.Compute(values));
        }

        static SimulationComponentIdentity BuildDiagnosticsIdentity(
            IReadOnlyList<IFixedSimulationActorRegistration> registrations)
        {
            var values = new string[registrations.Count + 1];
            values[0] = DiagnosticsId;
            for (int i = 0; i < registrations.Count; i++)
                values[i + 1] = $"{registrations[i].ActorId}:{registrations[i].DiagnosticsConfigurationHash}";
            return new SimulationComponentIdentity(
                SimulationComponentRole.Diagnostics,
                DiagnosticsId,
                "2",
                StableHash.Compute(values));
        }

        static SimulationSessionCompositionException Failure(string code, string message)
        {
            return new SimulationSessionCompositionException(new SimulationSessionFailure(
                SimulationSessionFailureStage.Composition,
                code,
                message,
                FixedProgramRuntime.ComponentId));
        }
    }
}
