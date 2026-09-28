using System;
using System.Collections.Generic;
using ThirdPersonSimulation;
using FixedActorBinding = ThirdPersonSimulation.Fixed.SimulationActorBinding;
using FixedActorState = ThirdPersonSimulation.Fixed.SimulationActorState;
using FixedCharacterRuntime = ThirdPersonSimulation.Fixed.FixedCharacterRuntime;
using FixedCompositionRequest = ThirdPersonSimulation.Fixed.FixedSimulationSessionCompositionRequest;
using FixedSimulationTarget = ThirdPersonSimulation.Fixed.FixedSimulationTarget;
using FixedPassBackendCompositionResult = ThirdPersonSimulation.Fixed.FixedPassBackendCompositionResult;
using FixedSimulationActorRegistration = ThirdPersonCharacter.Pipeline.Simulation.Fixed.IFixedCharacterRuntimeRegistration;
using FixedSimulationSessionSnapshotCodec = ThirdPersonSimulation.Fixed.FixedSimulationSessionSnapshotCodec;
using FixedSimulationSourceRuntimeBinding = ThirdPersonCharacter.Pipeline.Simulation.Fixed.FixedSimulationSourceRuntimeBinding;
using FixedSimulationSourceRuntimeBindingRequest = ThirdPersonCharacter.Pipeline.Simulation.Fixed.FixedSimulationSourceRuntimeBindingRequest;
using FixedSimulationWorldStateSet = ThirdPersonSimulation.Fixed.SimulationWorldStateSet;
using FixedWorldBodyState = ThirdPersonSimulation.Fixed.WorldBodyState;
using FixedWorldSimulationState = ThirdPersonSimulation.Fixed.WorldSimulationState;
using FixedWorldSolver = ThirdPersonSimulation.Fixed.ICharacterWorldSolver;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    sealed class FixedSimulationSessionCompositionPreparation : ISimulationSessionCompositionPreparation
    {
#if UNITY_EDITOR
        void MarkStartup(string phase)
        {
            SimulationSessionHost.ReportStartupMilestone(m_Definition.SessionId, phase);
        }
#endif
        const string CommitterId = "thirdperson.simulation.committer.fixed-session";
        const string DiagnosticsId = "thirdperson.simulation.diagnostics.fixed-session";

        readonly SimulationSessionCompositionDefinition m_Definition;
        readonly IReadOnlyList<FixedSimulationActorRegistration> m_Registrations;
        readonly FixedCharacterRuntime m_CharacterRuntime;
        readonly SimulationCharacterRuntimeDescriptor m_CharacterRuntimeDescriptor;
        readonly SimulationWorldSolverDefinitionDescriptor m_WorldSolverDescriptor;
        readonly SimulationWorldIdentityDescriptor m_WorldIdentity;
        readonly ISimulationSessionSourcePreparation m_SourcePreparation;
        SimulationSessionPreparedRuntime m_Prepared;
        bool m_RuntimeTaken;
        bool m_Disposed;
        ulong m_LatestSourceTick;

        public FixedSimulationSessionCompositionPreparation(
            SimulationSessionCompositionDefinition definition,
            IReadOnlyList<ISimulationActorRegistration> registrations)
        {
            m_Definition = definition ? definition : throw new ArgumentNullException(nameof(definition));
            m_Registrations = FreezeRoster(registrations);
#if UNITY_EDITOR
            MarkStartup("fixed-roster-frozen");
#endif
            SimulationExecutionTargetManifest target = m_Definition.ExecutionTarget;
            m_CharacterRuntime = BuildCharacterRuntime(m_Definition, m_Registrations, target);
#if UNITY_EDITOR
            MarkStartup("fixed-session-character-runtime-created");
#endif
            m_CharacterRuntimeDescriptor = new SimulationCharacterRuntimeDescriptor(
                target,
                m_CharacterRuntime.GameplayContentHash,
                m_CharacterRuntime.StateSchemaHash,
                m_CharacterRuntime.RosterDescriptor);
            m_WorldSolverDescriptor = m_Definition.WorldSolver.BuildDescriptor(m_Definition.TickRate);
#if UNITY_EDITOR
            MarkStartup("fixed-world-solver-descriptor-created");
#endif
            m_WorldIdentity = m_Definition.WorldSolver.BuildWorldIdentity(
                m_Definition.TickRate,
                new SimulationWorldId(m_Definition.WorldId),
                m_Definition.MapId,
                new WorldRevision(m_Definition.WorldRevision));
#if UNITY_EDITOR
            MarkStartup("fixed-world-identity-created");
#endif
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
#if UNITY_EDITOR
            MarkStartup("fixed-source-preparation-created");
#endif
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
                throw new ObjectDisposedException(nameof(FixedSimulationSessionCompositionPreparation));
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
                        "fixed_source_preparation_failed",
                        "Fixed Session Source preparation failed without a structured failure.",
                        m_SourcePreparation.Descriptor.Identity.ToString());
                    Status = SimulationSessionPreparationStatus.Failed;
                    return Status;
                }
                ISimulationSessionPreparedSource prepared = m_SourcePreparation.TakePreparedSource();
                if (prepared is not IFixedSimulationPreparedSource source)
                {
                    prepared?.Dispose();
                    throw new InvalidOperationException("Fixed Session Source returned no Fixed prepared Source.");
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
                    "fixed_session_composition_failed",
                    exception.ToString(),
                    m_Definition.name);
                Status = SimulationSessionPreparationStatus.Failed;
                return Status;
            }
        }

        public SimulationSessionPreparedRuntime TakePreparedRuntime()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(FixedSimulationSessionCompositionPreparation));
            if (Status != SimulationSessionPreparationStatus.Ready || m_Prepared == null || m_RuntimeTaken)
                throw new InvalidOperationException("Fixed Prepared Session Runtime is not available.");
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

        SimulationSessionPreparedRuntime BuildPreparedRuntime(IFixedSimulationPreparedSource source)
        {
            FixedWorldSolver solver = null;
            try
            {
                SimulationExecutionBackendDescriptor backend = m_Definition.ExecutionBackend.BuildPortableDescriptor();
                SimulationComponentIdentity snapshotIdentity = ThirdPersonSimulation.Fixed.FixedSimulationSessionComposer.BuildSnapshotCodecIdentity(
                    FixedSimulationTarget.Manifest.ExecutionTarget,
                    backend);
                var snapshotCodec = new FixedSimulationSessionSnapshotCodec(snapshotIdentity);
                SimulationComponentIdentity diagnosticsIdentity = new SimulationComponentIdentity(
                    SimulationComponentRole.Diagnostics,
                    DiagnosticsId,
                    "1",
                    StableHash.Compute(
                        "fixed-session-diagnostics/1",
                        m_Definition.SessionId,
                        m_CharacterRuntime.GameplayContentHash.ToString()));
                SimulationComponentIdentity committerIdentity = new SimulationComponentIdentity(
                    SimulationComponentRole.Committer,
                    CommitterId,
                    "1",
                    StableHash.Compute(
                        "fixed-session-committer/1",
                        m_Definition.SessionId,
                        m_CharacterRuntime.GameplayContentHash.ToString()));
                var output = new FixedSimulationOutputAggregate(
                    ActorRegistrations(),
                    source.MaximumBodySamplesPerActor);
                var diagnostics = new FixedSimulationDiagnosticsAggregate(ActorRegistrations());
                solver = CreateSolver();
                FixedSimulationWorldStateSet initialState = CreateInitialState(solver);
                FixedSimulationSourceRuntimeBinding sourceRuntime = source.BindRuntime(
                    new FixedSimulationSourceRuntimeBindingRequest(
                        m_Definition.Pipeline,
                        snapshotCodec,
                        output,
                        diagnostics,
                        committerIdentity,
                        ActorRegistrations()));
                var request = new FixedCompositionRequest(
                    new SimulationSessionId(m_Definition.SessionId),
                    new SimulationWorldId(m_Definition.WorldId),
                    new SimulationSourceClockId(m_Definition.SourceClockId),
                    m_Definition.TickRate,
                    m_CharacterRuntime,
                    backend,
                    sourceRuntime.PipelineRuntimePackage,
                    source.Descriptor,
                    source.RuntimePorts,
                    sourceRuntime.RestoreSource,
                    m_WorldSolverDescriptor,
                    solver,
                    m_Definition.RequiredWorldFeatures,
                    initialState,
                    sourceRuntime.PipelineInitialState,
                    sourceRuntime.Committer,
                    diagnosticsIdentity,
                    diagnostics,
                    OutputRoutes(),
                    new IDisposable[] { source },
                    ActorResources());
                FixedPassBackendCompositionResult result = sourceRuntime.RuntimeLauncher.Launch(request);
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

        FixedWorldSolver CreateSolver()
        {
            if (m_Definition.WorldSolver is not FixedWorldSolverDefinition definition)
                throw new InvalidOperationException("Fixed Pass Backend requires a Fixed World Solver Definition.");
            return definition.CreateSolver(m_Definition.TickRate, ActorRegistrations());
        }

        FixedSimulationWorldStateSet CreateInitialState(FixedWorldSolver solver)
        {
            var actors = new FixedActorState[m_Registrations.Count];
            var bodies = new FixedWorldBodyState[m_Registrations.Count];
            for (int i = 0; i < m_Registrations.Count; i++)
            {
                FixedSimulationActorRegistration registration = m_Registrations[i];
                if (registration.CharacterBinding.ActorId != registration.ActorId ||
                    registration.InitialBody.ActorId != registration.ActorId)
                {
                    throw new InvalidOperationException(
                        $"Fixed Actor '{registration.ActorId}' Runtime binding and initial body identities do not match.");
                }
                actors[i] = new FixedActorState(
                    registration.ActorId,
                    m_CharacterRuntime.CreateInitialState(i));
                bodies[i] = registration.InitialBody;
            }
            FixedWorldSimulationState world = solver.Create(
                new WorldRevision(m_Definition.WorldRevision),
                bodies);
            return new FixedSimulationWorldStateSet(0, actors, world);
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

        IReadOnlyList<IFixedSimulationActorRegistration> ActorRegistrations()
        {
            var values = new IFixedSimulationActorRegistration[m_Registrations.Count];
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

        static IReadOnlyList<FixedSimulationActorRegistration> FreezeRoster(
            IReadOnlyList<ISimulationActorRegistration> registrations)
        {
            if (registrations == null || registrations.Count == 0)
                throw new ArgumentException("Fixed Session preparation requires an Actor roster.", nameof(registrations));
            var values = new List<FixedSimulationActorRegistration>(registrations.Count);
            for (int i = 0; i < registrations.Count; i++)
            {
                if (registrations[i] is not FixedSimulationActorRegistration registration)
                    throw new InvalidOperationException(
                        $"Actor '{registrations[i]?.ActorId}' has no Fixed Character Runtime registration.");
                values.Add(registration);
            }
            values.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            for (int i = 1; i < values.Count; i++)
            {
                if (values[i - 1].ActorId == values[i].ActorId)
                    throw new ArgumentException("Fixed Session preparation contains a duplicate ActorId.", nameof(registrations));
            }
            return values.AsReadOnly();
        }

        static FixedCharacterRuntime BuildCharacterRuntime(
            SimulationSessionCompositionDefinition definition,
            IReadOnlyList<FixedSimulationActorRegistration> registrations,
            SimulationExecutionTargetManifest target)
        {
            var bindings = new FixedActorBinding[registrations.Count];
            for (int i = 0; i < bindings.Length; i++)
                bindings[i] = registrations[i].CharacterBinding;
            return new FixedCharacterRuntime(
                bindings,
                target.NumericProfile,
                definition.TickRate,
                target.OperationSetVersion,
                CharacterControlRuntimeModuleCatalog.Create());
        }
    }
}
