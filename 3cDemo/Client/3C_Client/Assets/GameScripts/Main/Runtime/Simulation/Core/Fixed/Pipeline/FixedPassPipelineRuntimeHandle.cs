using ThirdPersonSimulation;
using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class FixedPassPipelineRuntimeHandle : ISimulationSessionRuntimeHandle, ICharacterFutureBodyTrajectorySource
    {
        readonly SimulationSessionLifecycleController m_Lifecycle;
        readonly CompiledSimulationPipelinePlan m_Pipeline;
        readonly SimulationWorldStateStore m_StateStore;
        readonly FixedPipelineTransaction m_Transaction;
        readonly IReadOnlyList<IFixedCompiledPipelinePassRuntime> m_Passes;
        readonly SimulationSessionResourceRegistry m_Resources;
        readonly ICharacterFutureBodyTrajectorySource m_FutureBodyTrajectorySource;
        ulong m_LatestOuterTick;
        bool m_Disposed;

        internal FixedPassPipelineRuntimeHandle(
            SimulationSessionCompositionDescriptor descriptor,
            CompiledSimulationPipelinePlan pipeline,
            SimulationWorldStateStore stateStore,
            FixedPipelineTransaction transaction,
            IReadOnlyList<IFixedCompiledPipelinePassRuntime> passes,
            SimulationSessionResourceRegistry resources,
            ICharacterFutureBodyTrajectorySource futureBodyTrajectorySource)
        {
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            m_Pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            m_StateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
            m_Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            m_Passes = passes ?? throw new ArgumentNullException(nameof(passes));
            m_Resources = resources ?? throw new ArgumentNullException(nameof(resources));
            m_FutureBodyTrajectorySource = futureBodyTrajectorySource;
            m_Lifecycle = new SimulationSessionLifecycleController(descriptor);
            m_Lifecycle.BeginPreparing();
            m_Lifecycle.Activate(descriptor);
        }

        public SimulationSessionCompositionDescriptor Descriptor { get; }
        public SimulationSessionLifecycleState LifecycleState => m_Lifecycle.State;
        public SimulationSessionFailure Failure => m_Lifecycle.Failure;
        public SimulationSessionDiagnosticsSnapshot Diagnostics => BuildDiagnostics();

        public bool TryPredict(
            in CharacterFutureBodyTrajectoryRequest request,
            out CharacterFutureBodyTrajectory trajectory)
        {
            if (m_Disposed || LifecycleState != SimulationSessionLifecycleState.Active ||
                m_FutureBodyTrajectorySource == null)
            {
                trajectory = null;
                return false;
            }
            return m_FutureBodyTrajectorySource.TryPredict(in request, out trajectory);
        }

        public void LogicTick(SimulationSessionLogicTickContext context)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(FixedPassPipelineRuntimeHandle));
            m_Lifecycle.RequireActive(Descriptor);
            if (!string.Equals(context.Source.ClockId, Descriptor.SourceClockId.Value, StringComparison.Ordinal) ||
                context.Source.SourceTick <= m_LatestOuterTick ||
                !context.WorldRevision.Equals(m_StateStore.Current.WorldState.WorldRevision))
            {
                Fail(new SimulationSessionFailure(
                    SimulationSessionFailureStage.Runtime,
                    "outer_tick_identity_mismatch",
                    "Outer LogicTick source clock, sequence or WorldRevision does not match the active Session.",
                    Descriptor.ExecutionBackend.ToString()));
            }
            try
            {
                m_Transaction.Execute(context);
                m_LatestOuterTick = context.Source.SourceTick;
            }
            catch (SimulationSessionCompositionException exception)
            {
                m_Lifecycle.Fail(exception.Failure);
                throw;
            }
            catch (Exception exception)
            {
                var failure = new SimulationSessionFailure(
                    SimulationSessionFailureStage.Runtime,
                    "Fixed_pipeline_runtime_failed",
                    exception.Message,
                    Descriptor.ExecutionBackend.ToString());
                m_Lifecycle.Fail(failure);
                throw new SimulationSessionCompositionException(failure, exception);
            }
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            try
            {
                m_Resources.Dispose();
            }
            catch (Exception exception)
            {
                var failure = new SimulationSessionFailure(
                    SimulationSessionFailureStage.Disposal,
                    "session_resource_disposal_failed",
                    exception.Message,
                    Descriptor.ExecutionBackend.ToString());
                m_Lifecycle.Fail(failure);
                m_Lifecycle.MarkDisposed();
                throw new SimulationSessionCompositionException(failure, exception);
            }
            m_Lifecycle.MarkDisposed();
        }

        void Fail(SimulationSessionFailure failure)
        {
            m_Lifecycle.Fail(failure);
            throw new SimulationSessionCompositionException(failure);
        }

        SimulationSessionDiagnosticsSnapshot BuildDiagnostics()
        {
            var components = new List<SimulationSessionComponentDiagnostic>
            {
                Component("ProgramRuntime", Descriptor.ProgramRuntime),
                Component("ExecutionBackend", Descriptor.ExecutionBackend),
                Component("SessionSource", Descriptor.SessionSource),
                Component("WorldSolver", Descriptor.WorldSolver),
                Component("SnapshotCodec", Descriptor.SnapshotCodec),
                Component("Committer", Descriptor.Committer),
                new SimulationSessionComponentDiagnostic(
                    "Pipeline",
                    $"{m_Pipeline.Identity}/{m_Pipeline.PlanHash}",
                    DiagnosticState())
            };
            for (int i = 0; i < m_Passes.Count; i++)
            {
                components.Add(new SimulationSessionComponentDiagnostic(
                    "Pass",
                    $"{i}:{m_Passes[i].Descriptor.VersionedIdentity}",
                    DiagnosticState(),
                    m_Passes[i].Phase.ToString()));
            }
            return new SimulationSessionDiagnosticsSnapshot(
                Descriptor,
                LifecycleState,
                SimulationSessionPreparationStatus.Ready,
                m_LatestOuterTick,
                Failure,
                components);
        }

        SimulationSessionComponentDiagnostic Component(string component, SimulationComponentIdentity identity)
        {
            return new SimulationSessionComponentDiagnostic(component, identity.ToString(), DiagnosticState());
        }

        SimulationSessionComponentDiagnosticState DiagnosticState()
        {
            return LifecycleState switch
            {
                SimulationSessionLifecycleState.Active => SimulationSessionComponentDiagnosticState.Active,
                SimulationSessionLifecycleState.Failed => SimulationSessionComponentDiagnosticState.Failed,
                SimulationSessionLifecycleState.Disposed => SimulationSessionComponentDiagnosticState.Disposed,
                SimulationSessionLifecycleState.Preparing => SimulationSessionComponentDiagnosticState.Ready,
                _ => SimulationSessionComponentDiagnosticState.Pending
            };
        }
    }
}

