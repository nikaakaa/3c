using System;
using System.Collections.Generic;
using ThirdPersonSimulation;
using ThirdPersonSimulation.DeterministicKcc;
using ThirdPersonSimulation.DeterministicRollback;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonCharacter.Pipeline.Simulation.DeterministicRollback
{
    internal sealed class UnityFixedSimulationSessionComposer : ISimulationSessionComposer
    {
        const string DownstreamCommitterId = "thirdperson.simulation.committer.fixed-character-output";
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
            if (request.Source is not IDeterministicRollbackPreparedSource source)
                throw Failure("source_target_abi_mismatch", "Prepared Session Source does not implement the Deterministic Rollback Fixed Source contract.");
            if (request.Definition.ExecutionBackend is not FixedPassExecutionBackendDefinition)
                throw Failure("backend_target_abi_mismatch", "Execution Backend Definition is not the canonical Fixed Pass Backend.");
            if (request.Definition.Pipeline is not DeterministicRollbackPipelineDefinition pipelineDefinition)
                throw Failure("pipeline_target_abi_mismatch", "Pipeline Definition is not the canonical Deterministic Rollback Pipeline.");
            if (request.Definition.WorldSolver is not DeterministicKccWorldSolverDefinition solverDefinition)
                throw Failure("solver_target_abi_mismatch", "World Solver Definition is not the Deterministic KCC Solver.");

            IReadOnlyList<IFixedSimulationActorRegistration> registrations =
                RequireFixedRegistrations(request.Registrations);
            FixedProgramRuntime programRuntime = m_ProgramRuntimeDefinition.CreateRuntime(registrations);
            SimulationExecutionBackendDescriptor backend = request.Definition.ExecutionBackend.BuildPortableDescriptor();
            SimulationWorldSolverDefinitionDescriptor solverDescriptor =
                solverDefinition.BuildDescriptor(request.Definition.TickRate);
            SimulationSessionSourceDescriptor sourceDescriptor = source.Descriptor;
            DeterministicRollbackModelPolicy policy = pipelineDefinition.BuildPolicy();
            if (!source.ModelDefinition.Policy.ConfigurationHash.Equals(policy.ConfigurationHash) ||
                source.ModelDefinition.TickRate != request.Definition.TickRate ||
                !source.ModelDefinition.KccIdentityHash.Equals(solverDescriptor.Identity.ConfigurationHash))
            {
                throw Failure(
                    "rollback_model_binding_mismatch",
                    "Prepared Rollback Model does not match the selected Pipeline policy, TickRate, or KCC Solver.");
            }
            SimulationComponentIdentity snapshotIdentity =
                FixedSimulationSessionComposer.BuildSnapshotCodecIdentity(programRuntime.Descriptor, backend);
            var snapshotCodec = new FixedSimulationSessionSnapshotCodec(snapshotIdentity);
            var rosterDescriptor = new SimulationActorRosterDescriptor(ActorIds(registrations));
            var rollbackState = new RollbackRuntimeState(
                policy,
                rosterDescriptor.RosterHash,
                source.LocalPeerId);
            IFixedSourceEgressOutputPort sourceEgress = source.BindRuntime(
                rollbackState,
                snapshotCodec,
                policy);
            FixedSimulationPipelineRuntimePackage pipelinePackage =
                pipelineDefinition.BuildRuntimePackage(rollbackState);

            DeterministicKccWorldSolver solver = null;
            try
            {
                solver = solverDefinition.CreateSolver(request.Definition.TickRate, registrations);
                var outputs = new FixedSimulationOutputAggregate(registrations, policy.HistoryLengthTicks);
                var diagnostics = new FixedSimulationDiagnosticsAggregate(registrations);
                ThirdPersonSimulation.Fixed.WorldSimulationState world = solver.Create(
                    new WorldRevision(request.Definition.WorldRevision),
                    InitialBodies(registrations));
                ThirdPersonSimulation.Fixed.SimulationWorldStateSet initialState = programRuntime.CreateInitialState(world);
                var downstream = new RollbackOutputCommitter(
                    BuildCommitterIdentity(registrations),
                    rollbackState,
                    policy.MaximumOutputRecords,
                    outputs,
                    sourceEgress,
                    diagnostics);
                BindRuntimeDiagnostics(registrations, rollbackState, downstream, sourceEgress);
                var historyCommitter = new RollbackHistoryCommitter(
                    downstream,
                    rollbackState,
                    policy.HistoryLengthTicks);
                var portableRequest = new FixedSimulationSessionCompositionRequest(
                    new SimulationSessionId(request.Definition.SessionId),
                    new SimulationWorldId(request.Definition.WorldId),
                    new SimulationSourceClockId(request.Definition.SourceClockId),
                    request.Definition.TickRate,
                    programRuntime,
                    backend,
                    pipelinePackage,
                    sourceDescriptor,
                    source.RuntimePorts,
                    source.RestoreSource,
                    solverDescriptor,
                    solver,
                    request.Definition.RequiredWorldFeatures,
                    initialState,
                    ThirdPersonSimulation.Fixed.SimulationPipelineInitialStateSource.CaptureActivatedDefaults,
                    historyCommitter,
                    BuildDiagnosticsIdentity(registrations),
                    diagnostics,
                    OutputRoutes(registrations),
                    new IDisposable[] { source },
                    registrations);
                FixedPassBackendCompositionResult result = source.RuntimeLauncher.Launch(portableRequest);
                return new SimulationSessionPreparedRuntime(
                    result.LaunchPlan,
                    result.RuntimeHandle,
                    outputs,
                    sourceDescriptor.OuterTickKind);
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

        static void BindRuntimeDiagnostics(
            IReadOnlyList<IFixedSimulationActorRegistration> registrations,
            RollbackRuntimeState state,
            RollbackOutputCommitter outputCommitter,
            IFixedSourceEgressOutputPort sourceEgress)
        {
            if (sourceEgress is not IRollbackNetworkDiagnosticsSource networkDiagnostics)
                throw Failure("rollback_network_diagnostics_missing", "Rollback Source Egress has no network diagnostics contract.");
            for (int i = 0; i < registrations.Count; i++)
            {
                if (registrations[i] is not IDeterministicRollbackSimulationActorRegistration registration)
                    throw Failure("rollback_diagnostics_registration_missing", $"Actor '{registrations[i].ActorId}' has no Rollback diagnostics binding.");
                registration.BindRuntimeDiagnostics(state, outputCommitter, networkDiagnostics);
            }
        }

        static IReadOnlyList<ThirdPersonSimulation.Fixed.WorldBodyState> InitialBodies(
            IReadOnlyList<IFixedSimulationActorRegistration> registrations)
        {
            var values = new ThirdPersonSimulation.Fixed.WorldBodyState[registrations.Count];
            for (int i = 0; i < values.Length; i++)
                values[i] = registrations[i].InitialBody;
            return values;
        }

        static IReadOnlyList<ActorId> ActorIds(
            IReadOnlyList<IFixedSimulationActorRegistration> registrations)
        {
            var values = new ActorId[registrations.Count];
            for (int i = 0; i < values.Length; i++)
                values[i] = registrations[i].ActorId;
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
            values[0] = DownstreamCommitterId;
            for (int i = 0; i < registrations.Count; i++)
                values[i + 1] = $"{registrations[i].ActorId}:{registrations[i].OutputRoute.ConfigurationHash}";
            return new SimulationComponentIdentity(
                SimulationComponentRole.Committer,
                DownstreamCommitterId,
                "1",
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
                "1",
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
