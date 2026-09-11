using ThirdPersonSimulation;
using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class FixedPassPipelineRuntimeHandle : ISimulationSessionRuntimeHandle, ICharacterFutureBodyTranslationSource, ISimulationSessionProgramAdoption, ISimulationSessionCheckpointRuntime, ISimulationSessionInputReplayRuntime
    {
        readonly SimulationSessionLifecycleController m_Lifecycle;
        readonly CompiledSimulationPipelinePlan m_Pipeline;
        readonly SimulationWorldStateStore m_StateStore;
        readonly FixedPipelineTransaction m_Transaction;
        readonly IReadOnlyList<IFixedCompiledPipelinePassRuntime> m_Passes;
        readonly SimulationSessionResourceRegistry m_Resources;
        readonly ICharacterFutureBodyTranslationSource m_FutureBodyTranslationSource;
        SimulationProgramEpoch m_ProgramEpoch;
        ulong m_LatestOuterTick;
        bool m_Disposed;

        internal FixedPassPipelineRuntimeHandle(
            SimulationSessionCompositionDescriptor descriptor,
            CompiledSimulationPipelinePlan pipeline,
            SimulationWorldStateStore stateStore,
            FixedPipelineTransaction transaction,
            IReadOnlyList<IFixedCompiledPipelinePassRuntime> passes,
            SimulationSessionResourceRegistry resources,
            ICharacterFutureBodyTranslationSource futureBodyTranslationSource)
        {
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            m_Pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            m_StateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
            m_Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            m_Passes = passes ?? throw new ArgumentNullException(nameof(passes));
            m_Resources = resources ?? throw new ArgumentNullException(nameof(resources));
            m_FutureBodyTranslationSource = futureBodyTranslationSource;
            m_Lifecycle = new SimulationSessionLifecycleController(descriptor);
            m_Lifecycle.BeginPreparing();
            m_Lifecycle.Activate(descriptor);
            m_ProgramEpoch = SimulationProgramEpoch.Initial(descriptor.ProgramCatalogHash);
        }

        public SimulationSessionCompositionDescriptor Descriptor { get; }
        public SimulationSessionLifecycleState LifecycleState => m_Lifecycle.State;
        public SimulationSessionFailure Failure => m_Lifecycle.Failure;
        public SimulationSessionDiagnosticsSnapshot Diagnostics => BuildDiagnostics();
        public SimulationProgramEpoch ProgramEpoch => m_ProgramEpoch;

        public SimulationSessionCheckpoint CaptureCheckpoint()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(FixedPassPipelineRuntimeHandle));
            return m_Transaction.CaptureCheckpoint();
        }

        public bool IsInputRecording => FixedCharacterInputTraceModule.IsRecording;

        public void RestoreCheckpoint(SimulationSessionCheckpoint checkpoint)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(FixedPassPipelineRuntimeHandle));
            m_Transaction.RestoreCheckpoint(checkpoint);
            m_LatestOuterTick = checkpoint.Tick.Value;
        }

        public SimulationProgramEpoch PrepareProgramEpoch(
            ProgramRevision sourceRevision,
            IReadOnlyList<ISimulationProgramBinding> bindings)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(FixedPassPipelineRuntimeHandle));
            return m_Transaction.PrepareProgramEpoch(m_ProgramEpoch, sourceRevision, bindings);
        }

        public SimulationProgramAdoptionResult AdoptProgramEpoch(
            SimulationProgramEpoch epoch,
            IReadOnlyList<ISimulationProgramBinding> bindings)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(FixedPassPipelineRuntimeHandle));
            if (!epoch.IsValid || epoch.Value <= m_ProgramEpoch.Value)
            {
                return new SimulationProgramAdoptionResult(
                    SimulationProgramAdoptionStatus.Rejected,
                    m_ProgramEpoch,
                    epoch,
                    "program_epoch_not_newer",
                    "Program Epoch is not newer than the active Session Epoch.");
            }
            SimulationProgramAdoptionResult result;
            try
            {
                result = m_Transaction.AdoptProgramEpoch(epoch, bindings);
            }
            catch (InvalidOperationException exception)
            {
                return new SimulationProgramAdoptionResult(
                    SimulationProgramAdoptionStatus.Rejected,
                    m_ProgramEpoch,
                    epoch,
                    "program_epoch_incompatible",
                    exception.Message);
            }
            if (result.IsApplied)
                m_ProgramEpoch = epoch;
            return result;
        }
        public string PredictionSourceIdentity =>
            m_FutureBodyTranslationSource?.PredictionSourceIdentity ?? string.Empty;

        public bool TryGetCurrentBody(ActorId actorId, out WorldBodyState body)
        {
            for (int i = 0; i < m_StateStore.Current.WorldState.Bodies.Count; i++)
            {
                WorldBodyState candidate = m_StateStore.Current.WorldState.Bodies[i];
                if (candidate.ActorId == actorId)
                {
                    body = candidate;
                    return true;
                }
            }
            body = default;
            return false;
        }

        public bool TryStartInputRecording(out string error)
        {
            error = string.Empty;
            if (m_StateStore.Current.Actors.Count != 1)
            {
                error = "Fixed input recording requires exactly one Actor.";
                return false;
            }
            ActorId actorId = m_StateStore.Current.Actors[0].ActorId;
            if (!TryGetCurrentBody(actorId, out WorldBodyState body))
            {
                error = "Fixed input recording could not resolve the current Actor Body.";
                return false;
            }
            try
            {
                FixedCharacterInputTraceModule.PrepareRecordingFromCheckpoint(actorId, body);
                FixedCharacterInputTraceModule.StartRecording();
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public bool TryStopInputRecording(out string error)
        {
            try
            {
                FixedCharacterInputTraceModule.StopRecording();
                error = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public bool TryCancelInputReplay(out string error)
        {
            if (!FixedCharacterInputTraceModule.IsReplayActive)
            {
                error = "Fixed input replay is not active.";
                return false;
            }
            FixedCharacterInputTraceModule.Stop();
            error = string.Empty;
            return true;
        }

        public bool TryReplayInputRange(
            SimulationSessionCheckpoint checkpoint,
            ulong fromTick,
            ulong toTick,
            out string error)
        {
            error = string.Empty;
            if (fromTick >= toTick)
            {
                error = "Fixed replay range is invalid.";
                return false;
            }
            if (HasExternalReplaySource())
            {
                error = "Fixed replay requires an external result journal for the active Pipeline Source.";
                return false;
            }
            if (FixedCharacterInputTraceModule.IsRecording)
            {
                error = "Fixed replay cannot start while input recording is active.";
                return false;
            }
            FixedCharacterInputTrace recorded = FixedCharacterInputTraceModule.LastCompletedTrace;
            if (recorded == null)
            {
                error = "No completed Fixed input trace is available.";
                return false;
            }
            if (m_StateStore.Current.Actors.Count != 1)
            {
                error = "Fixed input replay requires exactly one Actor.";
                return false;
            }
            ActorId actorId = m_StateStore.Current.Actors[0].ActorId;
            if (recorded.ActorId != actorId)
            {
                error = "Fixed input trace Actor does not match the active Session Actor.";
                return false;
            }
            var frames = new List<FixedCharacterInputTraceFrame>();
            for (int i = 0; i < recorded.Frames.Count; i++)
            {
                FixedCharacterInputTraceFrame frame = recorded.Frames[i];
                if (frame.Tick.Value > fromTick && frame.Tick.Value <= toTick)
                    frames.Add(frame);
            }
            if (frames.Count == 0 ||
                frames[0].Tick.Value != checked(fromTick + 1) ||
                frames[frames.Count - 1].Tick.Value != toTick)
            {
                error = "Fixed input trace does not cover every Tick in the requested replay range.";
                return false;
            }
            if (!TryGetCurrentBody(actorId, out WorldBodyState body))
            {
                error = "Fixed input replay could not resolve the current Actor Body.";
                return false;
            }
            SimulationSessionCheckpoint rollback = null;
            bool replayPrepared = false;
            try
            {
                rollback = m_Transaction.CaptureCheckpoint();
                RestoreCheckpoint(checkpoint);
                TryGetCurrentBody(actorId, out body);
                var trace = new FixedCharacterInputTrace(
                    $"{recorded.TraceId}/range/{fromTick}-{toTick}",
                    actorId,
                    recorded.TickRate,
                    frames);
                FixedCharacterInputTraceModule.PrepareReplayFromCheckpoint(trace, 0, body, true);
                replayPrepared = true;
                FixedCharacterInputTraceModule.StartReplay();
                return true;
            }
            catch (Exception exception)
            {
                if (replayPrepared)
                    FixedCharacterInputTraceModule.Stop();
                if (rollback != null)
                {
                    try
                    {
                        RestoreCheckpoint(rollback);
                    }
                    catch (Exception rollbackException)
                    {
                        error = $"{exception.Message} Rollback failed: {rollbackException.Message}";
                        return false;
                    }
                }
                error = exception.Message;
                return false;
            }
        }

        public bool TryPredict(
            in CharacterFutureBodyTranslationRequest request,
            CharacterFutureBodyTranslation output)
        {
            if (m_Disposed || LifecycleState != SimulationSessionLifecycleState.Active ||
                m_FutureBodyTranslationSource == null)
            {
                output?.Clear();
                return false;
            }
            return m_FutureBodyTranslationSource.TryPredict(in request, output);
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
            FixedCharacterInputTraceModule.ClearCompletedTrace();
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

        bool HasExternalReplaySource()
        {
            for (int i = 0; i < m_Pipeline.Passes.Count; i++)
            {
                if (m_Pipeline.Passes[i].Descriptor.StateClass == SimulationPipelinePassStateClass.ExternalSource)
                    return true;
            }
            return false;
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

