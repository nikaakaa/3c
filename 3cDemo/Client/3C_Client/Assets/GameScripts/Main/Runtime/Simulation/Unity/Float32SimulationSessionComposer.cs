using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    internal sealed class UnityFloat32SimulationSessionComposer : ISimulationSessionComposer
    {
        const string CommitterId = "thirdperson.simulation.committer.character-output";
        const string DiagnosticsId = "thirdperson.simulation.diagnostics.character-roster";
        const string CleanupFailuresDataKey = "thirdperson.simulation.composition.cleanup-failures";
        readonly Float32ProgramRuntimeDefinition m_ProgramRuntimeDefinition;

        public UnityFloat32SimulationSessionComposer(Float32ProgramRuntimeDefinition programRuntimeDefinition)
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
            if (request.Source is not IFloat32SimulationSessionPreparedSource source)
                throw Failure("source_target_abi_mismatch", "Prepared Session Source does not implement the Float32 Source contract.");
            if (request.Definition.ExecutionBackend is not Float32PassExecutionBackendDefinition)
                throw Failure("backend_target_abi_mismatch", "Execution Backend Definition is not the canonical Float32 Pass Backend.");
            if (request.Definition.WorldSolver is not Float32WorldSolverDefinition solverDefinition)
                throw Failure("solver_target_abi_mismatch", "World Solver Definition does not implement the Float32 Solver contract.");
            if (request.Definition.Pipeline is not IFloat32SimulationPipelineRuntimePackageProvider packageProvider)
                throw Failure("pipeline_target_abi_mismatch", "Pipeline Definition does not provide a Float32 runtime package.");
            IFloat32SimulationSessionRuntimeLauncher runtimeLauncher = source.RuntimeLauncher ??
                throw Failure("runtime_launcher_missing", "Prepared Session Source did not provide its Float32 Runtime Launcher.");
            Float32SimulationPipelineRuntimePackage pipelineRuntimePackage =
                packageProvider.BuildRuntimePackage() ??
                throw Failure("pipeline_runtime_package_missing", "Pipeline Definition returned no Float32 runtime package.");

            IReadOnlyList<IFloat32SimulationActorRegistration> registrations =
                RequireFloat32Registrations(request.Registrations);
            Float32ProgramRuntime programRuntime = m_ProgramRuntimeDefinition.CreateRuntime(registrations);
            SimulationExecutionBackendDescriptor backend = request.Definition.ExecutionBackend.BuildPortableDescriptor();
            SimulationSessionSourceDescriptor sourceDescriptor = source.Descriptor;
            SimulationWorldSolverDefinitionDescriptor solverDescriptor =
                solverDefinition.BuildDescriptor(request.Definition.TickRate);

            ICharacterWorldSolver solver = null;
            try
            {
                solver = solverDefinition.CreateSolver(request.Definition.TickRate, registrations);
                Float32SimulationOutputAggregate outputs = new Float32SimulationOutputAggregate(registrations);
                Float32SimulationDiagnosticsAggregate diagnostics = new Float32SimulationDiagnosticsAggregate(registrations);
                SimulationComponentIdentity committerIdentity = BuildCommitterIdentity(registrations);
                SimulationComponentIdentity diagnosticsIdentity = BuildDiagnosticsIdentity(registrations);
                WorldSimulationState world = solver.Create(
                    new WorldRevision(request.Definition.WorldRevision),
                    InitialBodies(registrations));
                SimulationWorldStateSet initialState = programRuntime.CreateInitialState(world);
                var committer = new Float32SimulationCommitterAdapter(
                    committerIdentity,
                    new SimulationCommitter(outputs, outputs),
                    source.SourceEgress,
                    outputs);
                var portableRequest = new Float32SimulationSessionCompositionRequest(
                    new SimulationSessionId(request.Definition.SessionId),
                    new SimulationWorldId(request.Definition.WorldId),
                    new SimulationSourceClockId(request.Definition.SourceClockId),
                    request.Definition.TickRate,
                    programRuntime,
                    backend,
                    pipelineRuntimePackage,
                    sourceDescriptor,
                    source.RuntimePorts,
                    source.RestoreSource,
                    solverDescriptor,
                    solver,
                    request.Definition.RequiredWorldFeatures,
                    initialState,
                    SimulationPipelineInitialStateSource.CaptureActivatedDefaults,
                    committer,
                    diagnosticsIdentity,
                    diagnostics,
                    OutputRoutes(registrations),
                    new IDisposable[] { source },
                    registrations);
                Float32PassBackendCompositionResult result = runtimeLauncher.Launch(portableRequest);
                return new SimulationSessionPreparedRuntime(
                    result.LaunchPlan,
                    result.RuntimeHandle,
                    outputs,
                    sourceDescriptor.OuterTickKind);
            }
            catch (Exception exception)
            {
                List<Exception> cleanupFailures = ReleaseFailedComposition(solver, source);
                if (cleanupFailures.Count != 0)
                {
                    exception.Data[CleanupFailuresDataKey] = new AggregateException(
                        "Float32 Session composition failed and one or more owned resources also failed to release.",
                        cleanupFailures);
                }
                throw;
            }
        }

        static List<Exception> ReleaseFailedComposition(
            ICharacterWorldSolver solver,
            IFloat32SimulationSessionPreparedSource source)
        {
            var failures = new List<Exception>();
            TryDispose(solver, failures);
            TryDispose(source, failures);
            return failures;
        }

        static void TryDispose(IDisposable resource, List<Exception> failures)
        {
            if (resource == null)
                return;
            try
            {
                resource.Dispose();
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }

        static IReadOnlyList<IFloat32SimulationActorRegistration> RequireFloat32Registrations(
            IReadOnlyList<ISimulationActorRegistration> registrations)
        {
            if (registrations == null || registrations.Count == 0)
                throw Failure("actor_roster_missing", "Float32 Composer requires an Actor roster.");
            var values = new IFloat32SimulationActorRegistration[registrations.Count];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = registrations[i] as IFloat32SimulationActorRegistration ??
                    throw Failure(
                        "actor_target_abi_mismatch",
                        $"Actor '{registrations[i]?.ActorId}' does not provide a Float32 registration.");
            }
            return values;
        }

        static IReadOnlyList<WorldBodyState> InitialBodies(
            IReadOnlyList<IFloat32SimulationActorRegistration> registrations)
        {
            var values = new WorldBodyState[registrations.Count];
            for (int i = 0; i < values.Length; i++)
                values[i] = registrations[i].InitialBody;
            return values;
        }

        static IReadOnlyList<SimulationOutputRouteDescriptor> OutputRoutes(
            IReadOnlyList<IFloat32SimulationActorRegistration> registrations)
        {
            var values = new SimulationOutputRouteDescriptor[registrations.Count];
            for (int i = 0; i < values.Length; i++)
                values[i] = registrations[i].OutputRoute;
            return values;
        }

        static SimulationComponentIdentity BuildCommitterIdentity(
            IReadOnlyList<IFloat32SimulationActorRegistration> registrations)
        {
            var values = new string[registrations.Count + 1];
            values[0] = CommitterId;
            for (int i = 0; i < registrations.Count; i++)
                values[i + 1] = $"{registrations[i].ActorId}:{registrations[i].OutputRoute.ConfigurationHash}";
            return new SimulationComponentIdentity(
                SimulationComponentRole.Committer,
                CommitterId,
                "1",
                StableHash.Compute(values));
        }

        static SimulationComponentIdentity BuildDiagnosticsIdentity(
            IReadOnlyList<IFloat32SimulationActorRegistration> registrations)
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
                Float32ProgramRuntime.ComponentId));
        }
    }
}
