using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    sealed class Float32SimulationSessionCompositionPreparation : ISimulationSessionCompositionPreparation
    {
        const string CommitterId = "thirdperson.simulation.committer.float32-session";
        const string DiagnosticsId = "thirdperson.simulation.diagnostics.float32-session";

        readonly SimulationSessionCompositionDefinition m_Definition;
        readonly IReadOnlyList<IFloat32CharacterRuntimeRegistration> m_Registrations;
        readonly Float32CharacterRuntime m_CharacterRuntime;
        readonly SimulationCharacterRuntimeDescriptor m_CharacterRuntimeDescriptor;
        readonly SimulationWorldSolverDefinitionDescriptor m_WorldSolverDescriptor;
        readonly SimulationWorldIdentityDescriptor m_WorldIdentity;
        readonly ISimulationSessionSourcePreparation m_SourcePreparation;
        SimulationSessionPreparedRuntime m_Prepared;
        bool m_RuntimeTaken;
        bool m_Disposed;
        ulong m_LatestSourceTick;

        public Float32SimulationSessionCompositionPreparation(
            SimulationSessionCompositionDefinition definition,
            IReadOnlyList<ISimulationActorRegistration> registrations)
        {
            m_Definition = definition ? definition : throw new ArgumentNullException(nameof(definition));
            m_Registrations = FreezeRoster(registrations);
            SimulationExecutionTargetManifest target = m_Definition.ExecutionTarget;
            m_CharacterRuntime = BuildCharacterRuntime(m_Definition, m_Registrations, target);
            m_CharacterRuntimeDescriptor = new SimulationCharacterRuntimeDescriptor(
                target,
                m_CharacterRuntime.GameplayContentHash,
                m_CharacterRuntime.StateSchemaHash,
                m_CharacterRuntime.RosterDescriptor);
            m_WorldSolverDescriptor = m_Definition.WorldSolver.BuildDescriptor(m_Definition.TickRate);
            m_WorldIdentity = m_Definition.WorldSolver.BuildWorldIdentity(
                m_Definition.TickRate,
                new SimulationWorldId(m_Definition.WorldId),
                m_Definition.MapId,
                new WorldRevision(m_Definition.WorldRevision));
            m_SourcePreparation = m_Definition.SessionSource.CreatePreparation(
                new SimulationSessionSourcePreparationContext(
                    new SimulationSessionId(m_Definition.SessionId),
                    new SimulationSourceClockId(m_Definition.SourceClockId),
                    m_Definition.TickRate,
                    m_CharacterRuntimeDescriptor,
                    m_Definition.ExecutionBackend,
                    m_WorldSolverDescriptor,
                    m_WorldIdentity,
                    m_Registrations));
        }

        public SimulationSessionPreparationStatus Status { get; private set; } = SimulationSessionPreparationStatus.Pending;
        public SimulationSessionFailure Failure { get; private set; }
        public SimulationSessionLaunchPlan LaunchPlan => m_Prepared?.LaunchPlan;
        public SimulationSessionSourceDescriptor SourceDescriptor => m_SourcePreparation.Descriptor;
        public SimulationSessionDiagnosticsSnapshot Diagnostics =>
            m_Prepared?.RuntimeHandle.Diagnostics ?? BuildPreparationDiagnostics();

        public SimulationSessionPreparationStatus Step(SimulationSessionLogicTickContext context)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(Float32SimulationSessionCompositionPreparation));
            if (Status != SimulationSessionPreparationStatus.Pending)
                return Status;
            try
            {
                m_LatestSourceTick = context.Source.SourceTick;
                SimulationSessionPreparationStatus sourceStatus = m_SourcePreparation.Step(context);
                if (sourceStatus == SimulationSessionPreparationStatus.Pending)
                    return Status;
                if (sourceStatus == SimulationSessionPreparationStatus.Failed)
                {
                    Failure = m_SourcePreparation.Failure ?? new SimulationSessionFailure(
                        SimulationSessionFailureStage.Preparation,
                        "float32_source_preparation_failed",
                        "Float32 Session Source preparation failed without a structured failure.",
                        m_SourcePreparation.Descriptor.Identity.ToString());
                    Status = SimulationSessionPreparationStatus.Failed;
                    return Status;
                }
                ISimulationSessionPreparedSource prepared = m_SourcePreparation.TakePreparedSource();
                if (prepared is not IFloat32SimulationSessionPreparedSource source)
                {
                    prepared?.Dispose();
                    throw new InvalidOperationException("Float32 Session Source returned no Float32 prepared Source.");
                }
                m_Prepared = BuildPreparedRuntime(source);
                Status = SimulationSessionPreparationStatus.Ready;
                return Status;
            }
            catch (SimulationSessionCompositionException exception)
            {
                Failure = exception.Failure;
                Status = SimulationSessionPreparationStatus.Failed;
                return Status;
            }
            catch (Exception exception)
            {
                Failure = new SimulationSessionFailure(
                    SimulationSessionFailureStage.Composition,
                    "float32_session_composition_failed",
                    exception.ToString(),
                    m_Definition.name);
                Status = SimulationSessionPreparationStatus.Failed;
                return Status;
            }
        }

        public SimulationSessionPreparedRuntime TakePreparedRuntime()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(Float32SimulationSessionCompositionPreparation));
            if (Status != SimulationSessionPreparationStatus.Ready || m_Prepared == null || m_RuntimeTaken)
                throw new InvalidOperationException("Float32 Prepared Session Runtime is not available.");
            m_RuntimeTaken = true;
            return m_Prepared;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            if (!m_RuntimeTaken)
                m_Prepared?.RuntimeHandle.Dispose();
            m_SourcePreparation.Dispose();
        }

        SimulationSessionPreparedRuntime BuildPreparedRuntime(IFloat32SimulationSessionPreparedSource source)
        {
            ICharacterWorldSolver solver = null;
            try
            {
                SimulationExecutionBackendDescriptor backend = m_Definition.ExecutionBackend.BuildPortableDescriptor();
                if (m_Definition.Pipeline is not IFloat32SimulationPipelineRuntimePackageProvider provider)
                    throw new InvalidOperationException(
                        $"Pipeline Definition '{m_Definition.Pipeline?.name}' has no Float32 runtime package provider.");
                Float32SimulationPipelineRuntimePackage pipeline = provider.BuildRuntimePackage();
                SimulationComponentIdentity diagnosticsIdentity = new SimulationComponentIdentity(
                    SimulationComponentRole.Diagnostics,
                    DiagnosticsId,
                    "1",
                    StableHash.Compute(
                        "float32-session-diagnostics/1",
                        m_Definition.SessionId,
                        m_CharacterRuntime.GameplayContentHash.ToString()));
                SimulationComponentIdentity committerIdentity = new SimulationComponentIdentity(
                    SimulationComponentRole.Committer,
                    CommitterId,
                    "1",
                    StableHash.Compute(
                        "float32-session-committer/1",
                        m_Definition.SessionId,
                        m_CharacterRuntime.GameplayContentHash.ToString()));
                var output = new Float32SimulationOutputAggregate(ActorRegistrations());
                var diagnostics = new Float32SimulationDiagnosticsAggregate(ActorRegistrations());
                var committer = new Float32SimulationCommitterAdapter(
                    committerIdentity,
                    new SimulationCommitter(output, output),
                    source.SourceEgress,
                    output);
                solver = CreateSolver();
                SimulationWorldStateSet initialState = CreateInitialState(solver);
                var request = new Float32SimulationSessionCompositionRequest(
                    new SimulationSessionId(m_Definition.SessionId),
                    new SimulationWorldId(m_Definition.WorldId),
                    new SimulationSourceClockId(m_Definition.SourceClockId),
                    m_Definition.TickRate,
                    m_CharacterRuntime,
                    backend,
                    pipeline,
                    source.Descriptor,
                    source.RuntimePorts,
                    source.RestoreSource,
                    m_WorldSolverDescriptor,
                    solver,
                    m_Definition.RequiredWorldFeatures,
                    initialState,
                    SimulationPipelineInitialStateSource.CaptureActivatedDefaults,
                    committer,
                    diagnosticsIdentity,
                    diagnostics,
                    OutputRoutes(),
                    new IDisposable[] { source },
                    ActorResources());
                Float32PassBackendCompositionResult result = source.RuntimeLauncher.Launch(request);
                return new SimulationSessionPreparedRuntime(
                    result.LaunchPlan,
                    result.RuntimeHandle,
                    output,
                    source.Descriptor.OuterTickKind);
            }
            catch
            {
                solver?.Dispose();
                source.Dispose();
                throw;
            }
        }

        ICharacterWorldSolver CreateSolver()
        {
            if (m_Definition.WorldSolver is not Float32WorldSolverDefinition definition)
                throw new InvalidOperationException("Float32 Pass Backend requires a Float32 World Solver Definition.");
            return definition.CreateSolver(m_Definition.TickRate, ActorRegistrations());
        }

        SimulationWorldStateSet CreateInitialState(ICharacterWorldSolver solver)
        {
            var actors = new SimulationActorState[m_Registrations.Count];
            var bodies = new WorldBodyState[m_Registrations.Count];
            for (int i = 0; i < m_Registrations.Count; i++)
            {
                IFloat32CharacterRuntimeRegistration registration = m_Registrations[i];
                if (registration.CharacterBinding.ActorId != registration.ActorId ||
                    registration.InitialBody.ActorId != registration.ActorId)
                {
                    throw new InvalidOperationException(
                        $"Float32 Actor '{registration.ActorId}' Runtime binding and initial body identities do not match.");
                }
                actors[i] = new SimulationActorState(
                    registration.ActorId,
                    m_CharacterRuntime.CreateInitialState(i));
                bodies[i] = registration.InitialBody;
            }
            WorldSimulationState world = solver.Create(
                new WorldRevision(m_Definition.WorldRevision),
                bodies);
            return new SimulationWorldStateSet(0, actors, world);
        }

        SimulationSessionDiagnosticsSnapshot BuildPreparationDiagnostics()
        {
            SimulationSessionComponentDiagnosticState sourceState = Status switch
            {
                SimulationSessionPreparationStatus.Pending => SimulationSessionComponentDiagnosticState.Pending,
                SimulationSessionPreparationStatus.Ready => SimulationSessionComponentDiagnosticState.Ready,
                SimulationSessionPreparationStatus.Failed => SimulationSessionComponentDiagnosticState.Failed,
                _ => SimulationSessionComponentDiagnosticState.Pending
            };
            var components = new List<SimulationSessionComponentDiagnostic>
            {
                new SimulationSessionComponentDiagnostic(
                    "CharacterRuntime",
                    m_CharacterRuntimeDescriptor.Identity.ToString(),
                    SimulationSessionComponentDiagnosticState.Ready),
                new SimulationSessionComponentDiagnostic(
                    "ExecutionBackendDefinition",
                    m_Definition.ExecutionBackend.name,
                    SimulationSessionComponentDiagnosticState.Pending),
                new SimulationSessionComponentDiagnostic(
                    "PipelineDefinition",
                    m_Definition.Pipeline.name,
                    SimulationSessionComponentDiagnosticState.Pending),
                new SimulationSessionComponentDiagnostic(
                    "SessionSource",
                    m_SourcePreparation.Descriptor.Identity.ToString(),
                    sourceState,
                    m_SourcePreparation.Failure?.ToString() ?? string.Empty),
                new SimulationSessionComponentDiagnostic(
                    "WorldSolverDefinition",
                    m_Definition.WorldSolver.name,
                    SimulationSessionComponentDiagnosticState.Pending)
            };
            if (Failure != null)
            {
                components.Add(new SimulationSessionComponentDiagnostic(
                    "Failure",
                    $"{Failure.Stage}:{Failure.Code}",
                    SimulationSessionComponentDiagnosticState.Failed,
                    $"{Failure.Message} | Component={Failure.ComponentIdentity} | Pass={Failure.PassIdentity} | Product={Failure.ProductIdentity}"));
            }
            return new SimulationSessionDiagnosticsSnapshot(
                new SimulationSessionId(m_Definition.SessionId),
                Status == SimulationSessionPreparationStatus.Failed
                    ? SimulationSessionLifecycleState.Failed
                    : SimulationSessionLifecycleState.Preparing,
                Status,
                m_LatestSourceTick,
                Failure,
                components);
        }

        IReadOnlyList<IFloat32SimulationActorRegistration> ActorRegistrations()
        {
            var values = new IFloat32SimulationActorRegistration[m_Registrations.Count];
            for (int i = 0; i < values.Length; i++)
                values[i] = m_Registrations[i];
            return values;
        }

        IReadOnlyList<SimulationOutputRouteDescriptor> OutputRoutes()
        {
            var values = new SimulationOutputRouteDescriptor[m_Registrations.Count];
            for (int i = 0; i < values.Length; i++)
                values[i] = m_Registrations[i].OutputRoute;
            return values;
        }

        IReadOnlyList<IDisposable> ActorResources()
        {
            var values = new IDisposable[m_Registrations.Count];
            for (int i = 0; i < values.Length; i++)
                values[i] = m_Registrations[i];
            return values;
        }

        static IReadOnlyList<IFloat32CharacterRuntimeRegistration> FreezeRoster(
            IReadOnlyList<ISimulationActorRegistration> registrations)
        {
            if (registrations == null || registrations.Count == 0)
                throw new ArgumentException("Float32 Session preparation requires an Actor roster.", nameof(registrations));
            var values = new List<IFloat32CharacterRuntimeRegistration>(registrations.Count);
            for (int i = 0; i < registrations.Count; i++)
            {
                if (registrations[i] is not IFloat32CharacterRuntimeRegistration registration)
                    throw new InvalidOperationException(
                        $"Actor '{registrations[i]?.ActorId}' has no Float32 Character Runtime registration.");
                values.Add(registration);
            }
            values.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            for (int i = 1; i < values.Count; i++)
            {
                if (values[i - 1].ActorId == values[i].ActorId)
                    throw new ArgumentException("Float32 Session preparation contains a duplicate ActorId.", nameof(registrations));
            }
            return values.AsReadOnly();
        }

        static Float32CharacterRuntime BuildCharacterRuntime(
            SimulationSessionCompositionDefinition definition,
            IReadOnlyList<IFloat32CharacterRuntimeRegistration> registrations,
            SimulationExecutionTargetManifest target)
        {
            var bindings = new SimulationActorBinding[registrations.Count];
            for (int i = 0; i < bindings.Length; i++)
                bindings[i] = registrations[i].CharacterBinding;
            return new Float32CharacterRuntime(
                bindings,
                target.NumericProfile,
                definition.TickRate,
                target.OperationSetVersion,
                CharacterControlRuntimeModuleCatalog.Create());
        }
    }
}
