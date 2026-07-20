using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public sealed class SimulationSessionCompositionPreparation : ISimulationSessionPreparation
    {
        readonly SimulationSessionCompositionDefinition m_Definition;
        readonly IReadOnlyList<ISimulationActorRegistration> m_Registrations;
        readonly SimulationProgramRuntimeDescriptor m_ProgramRuntime;
        readonly ISimulationSessionComposer m_Composer;
        readonly ISimulationSessionSourcePreparation m_SourcePreparation;
        SimulationSessionPreparedRuntime m_Prepared;
        ulong m_LatestSourceTick;
        bool m_RuntimeTaken;
        bool m_Disposed;

        internal SimulationSessionCompositionPreparation(
            SimulationSessionCompositionDefinition definition,
            IReadOnlyList<ISimulationActorRegistration> registrations)
        {
            m_Definition = definition ? definition : throw new ArgumentNullException(nameof(definition));
            m_Registrations = FreezeRoster(registrations);
            m_ProgramRuntime = definition.ProgramRuntime.BuildDescriptor();
            m_Composer = definition.ProgramRuntime.CreateComposer() ??
                throw new InvalidOperationException("Program Runtime Definition returned no target-specific Composer.");
            SimulationWorldSolverDefinitionDescriptor worldSolver =
                definition.WorldSolver.BuildDescriptor(definition.TickRate);
            SimulationWorldIdentityDescriptor worldIdentity = definition.WorldSolver.BuildWorldIdentity(
                definition.TickRate,
                new SimulationWorldId(definition.WorldId),
                definition.MapId,
                new WorldRevision(definition.WorldRevision));
            var sourceContext = new SimulationSessionSourcePreparationContext(
                new SimulationSessionId(definition.SessionId),
                new SimulationSourceClockId(definition.SourceClockId),
                definition.TickRate,
                m_ProgramRuntime,
                definition.ExecutionBackend,
                worldSolver,
                worldIdentity,
                m_Registrations);
            m_SourcePreparation = definition.SessionSource.CreatePreparation(sourceContext) ??
                throw new InvalidOperationException("Session Source Definition returned no preparation.");
        }

        public SimulationSessionPreparationStatus Status { get; private set; } = SimulationSessionPreparationStatus.Pending;
        public SimulationSessionFailure Failure { get; private set; }
        public SimulationSessionLaunchPlan LaunchPlan => m_Prepared?.LaunchPlan;
        public SimulationSessionDiagnosticsSnapshot Diagnostics =>
            m_Prepared?.RuntimeHandle.Diagnostics ?? BuildPreparationDiagnostics();
        public SimulationSessionSourceDescriptor SourceDescriptor => m_SourcePreparation.Descriptor;

        public SimulationSessionPreparationStatus Step(SimulationSessionLogicTickContext context)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(SimulationSessionCompositionPreparation));
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
                        "session_source_preparation_failed",
                        "Session Source preparation failed without a structured failure.");
                    Status = SimulationSessionPreparationStatus.Failed;
                    return Status;
                }
                ISimulationSessionPreparedSource source = m_SourcePreparation.TakePreparedSource() ??
                    throw new InvalidOperationException("Ready Session Source preparation returned no prepared Source.");
                m_Prepared = m_Composer.Compose(new SimulationSessionCompositionBuildRequest(
                    m_Definition,
                    m_ProgramRuntime,
                    source,
                    m_Registrations));
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
                    "session_composition_failed",
                    exception.Message,
                    m_Definition.name);
                Status = SimulationSessionPreparationStatus.Failed;
                return Status;
            }
        }

        public SimulationSessionPreparedRuntime TakePreparedRuntime()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(SimulationSessionCompositionPreparation));
            if (Status != SimulationSessionPreparationStatus.Ready || m_Prepared == null || m_RuntimeTaken)
                throw new InvalidOperationException("Prepared Session Runtime is not available.");
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

        static IReadOnlyList<ISimulationActorRegistration> FreezeRoster(
            IReadOnlyList<ISimulationActorRegistration> registrations)
        {
            if (registrations == null || registrations.Count == 0)
                throw new ArgumentException("Session preparation requires at least one Actor registration.", nameof(registrations));
            var values = new List<ISimulationActorRegistration>(registrations);
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == null)
                    throw new ArgumentException("Session preparation contains a missing Actor registration.", nameof(registrations));
            }
            values.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0 && values[i - 1].ActorId.Equals(values[i].ActorId))
                    throw new ArgumentException("Session preparation contains a duplicate Actor registration.", nameof(registrations));
            }
            return values.AsReadOnly();
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
                    "ProgramRuntime",
                    m_ProgramRuntime.Identity.ToString(),
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
            SimulationSessionLifecycleState lifecycle = Status == SimulationSessionPreparationStatus.Failed
                ? SimulationSessionLifecycleState.Failed
                : SimulationSessionLifecycleState.Preparing;
            return new SimulationSessionDiagnosticsSnapshot(
                new SimulationSessionId(m_Definition.SessionId),
                lifecycle,
                Status,
                m_LatestSourceTick,
                Failure,
                components);
        }
    }
}
