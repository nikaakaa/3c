using System;
using BTSMTL.Diagnostics;
using Fixed = ThirdPersonSimulation.Fixed;

namespace ThirdPersonCharacter.Pipeline.Simulation.DeterministicRollback
{
    public sealed class FixedCharacterSimulationDiagnosticsAdapter : Fixed.ISimulationDiagnosticsSink
    {
        readonly RuntimeDiagnosticsContext m_Context;
        readonly IDebugSourceMap m_SourceMap;
        readonly Guid m_ExecutionId;

        public FixedCharacterSimulationDiagnosticsAdapter(
            RuntimeDiagnosticsContext context,
            Fixed.CharacterSimulationProgram program)
        {
            m_Context = context ?? throw new ArgumentNullException(nameof(context));
            m_SourceMap = context.SourceMap;
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            m_ExecutionId = context.SessionId;
            for (int i = 0; i < program.Operations.Count; i++)
            {
                if (!m_SourceMap.TryGetProgramTarget(
                        new RuntimeSourceTarget(RuntimeSourceTargetKind.Operation, i),
                        out _))
                    throw new InvalidOperationException($"Fixed Program operation '{i}' is absent from the Debug Source Map.");
            }
            for (int i = 0; i < program.StateSlots.Count; i++)
            {
                if (!m_SourceMap.TryGetProgramTarget(
                        new RuntimeSourceTarget(RuntimeSourceTargetKind.StateSlot, i),
                        out _))
                    throw new InvalidOperationException($"Fixed Program state slot '{i}' is absent from the Debug Source Map.");
            }
        }

        public bool IsEnabled => m_Context.Store.EffectiveChannels != RuntimeTraceChannel.None;

        public void PublishBoundary(Fixed.SimulationBoundaryTraceRecord record)
        {
            if (!IsEnabled)
                return;
            m_Context.BeginLogicTick(record.Tick.Value);
            RuntimeTraceChannel channel = record.Kind == Fixed.SimulationBoundaryTraceKind.WorldBatchStarted ||
                                          record.Kind == Fixed.SimulationBoundaryTraceKind.WorldBatchCompleted
                ? RuntimeTraceChannel.Motion
                : RuntimeTraceChannel.Graph;
            m_Context.Publish(
                channel,
                RuntimeTraceDomain.Lifecycle,
                ResolveBoundaryKind(record.Kind),
                RuntimeSourceElementHandle.Invalid,
                RuntimeInstanceKey.Character(m_Context.CharacterRuntimeId),
                new RuntimeTracePayload
                {
                    Status = record.Success ? "Success" : "Failed",
                    Name = record.Kind.ToString(),
                    Detail = $"{record.Detail} | Source={record.Source.Kind}:{record.Source.ClockId}/{record.Source.SourceTick} | Program={record.ProgramId.Value} | Solver={record.SolverId.Value} | Capabilities={record.SolverCapabilities} | StateHash={record.CharacterStateHash} | WorldHash={record.WorldHash} | Deterministic={record.DeterministicValidity}",
                    OwnerId = record.ActorId.IsValid ? record.ActorId.Value : string.Empty,
                    RelatedElementId = record.SnapshotIdentity,
                    Flag = record.Success,
                    Value = DebugValueSnapshot.Capture(record.Tick.Value)
                });
        }

        public void PublishOperation(Fixed.SimulationTraceRecord record)
        {
            if (!IsEnabled)
                return;
            m_Context.BeginLogicTick(record.Header.Tick.Value);
            var target = new RuntimeSourceTarget(
                RuntimeSourceTargetKind.Operation,
                record.Header.Activation.Operation.Value);
            if (!m_SourceMap.TryGetProgramTarget(target, out RuntimeSourceElementHandle source))
                throw new InvalidOperationException($"Fixed operation trace target '{target}' is absent from the Debug Source Map.");
            RuntimeTraceEventKind kind = ResolveOperationKind(record.Code);
            m_Context.Publish(
                ResolveOperationChannel(kind, record.Code),
                RuntimeTraceDomain.Logic,
                kind,
                source,
                RuntimeInstanceKey.Runnable(
                    m_Context.CharacterRuntimeId,
                    m_ExecutionId,
                    record.Header.Activation.Operation.Value.ToString(),
                    record.Header.Activation.Generation),
                new RuntimeTracePayload
                {
                    Status = record.Severity.ToString(),
                    Name = record.Code,
                    Detail = record.Detail,
                    Cause = record.Boundary,
                    OwnerId = record.Header.ActorId.Value,
                    Flag = record.Severity != Fixed.SimulationTraceSeverity.Error,
                    Value = DebugValueSnapshot.Capture(record.Header.Sequence)
                });
        }

        public void PublishPipeline(Fixed.SimulationPipelineTraceRecord record)
        {
            if (!IsEnabled)
                return;
            m_Context.BeginLogicTick(record.Source.SourceTick);
            RuntimeTraceEventKind kind = ResolvePipelineKind(record);
            string name = record.PassId.IsValid ? record.PassId.Value : record.Kind.ToString();
            m_Context.Publish(
                record.Phase == ThirdPersonSimulation.SimulationPipelinePhase.Step
                    ? RuntimeTraceChannel.Motion
                    : RuntimeTraceChannel.Graph,
                RuntimeTraceDomain.Lifecycle,
                kind,
                RuntimeSourceElementHandle.Invalid,
                RuntimeInstanceKey.Character(m_Context.CharacterRuntimeId),
                new RuntimeTracePayload
                {
                    Status = record.Success ? "Success" : "Failed",
                    Name = name,
                    Detail = $"{record.Detail} | Pipeline={record.Pipeline} | Source={record.Source.Kind}:{record.Source.ClockId}/{record.Source.SourceTick} | CompletedTick={record.CompletedTick} | Phase={record.Phase} | PassVersion={record.PassVersion} | Schedule={record.ScheduleStatus} | Restore={record.RestoreRequested} | Steps={record.StepCount} | Inputs={record.ProductInputs} | Outputs={record.ProductOutputs} | Snapshot={record.SnapshotParticipant}/{record.SnapshotHash}",
                    OwnerId = record.Session.ToString(),
                    RelatedElementId = record.SnapshotParticipant,
                    Flag = record.Success,
                    Value = DebugValueSnapshot.Capture(record.ElapsedStopwatchTicks)
                });
        }

        public void PublishModel(Fixed.SimulationModelTraceRecord record)
        {
            if (!IsEnabled)
                return;
            m_Context.BeginLogicTick(record.LocalSourceTick);
            RuntimeSourceElementHandle source = RuntimeSourceElementHandle.Invalid;
            if (record.Kind == Fixed.SimulationModelTraceKind.OutputDisposition)
            {
                if (record.InputSequence > int.MaxValue ||
                    !m_SourceMap.TryGetProgramTarget(
                        new RuntimeSourceTarget(RuntimeSourceTargetKind.Operation, checked((int)record.InputSequence)),
                        out source))
                {
                    throw new InvalidOperationException($"Rollback output trace operation '{record.InputSequence}' is absent from the Debug Source Map.");
                }
            }
            m_Context.Publish(
                RuntimeTraceChannel.Network,
                RuntimeTraceDomain.Lifecycle,
                RuntimeTraceEventKind.SimulationNetworkModel,
                source,
                RuntimeInstanceKey.Character(m_Context.CharacterRuntimeId),
                new RuntimeTracePayload
                {
                    Status = record.Success ? "Success" : "Failed",
                    Name = record.Code,
                    Detail = record.Detail,
                    Cause = record.Kind.ToString(),
                    OwnerId = record.ActorId.IsValid ? record.ActorId.Value : string.Empty,
                    RelatedElementId = $"confirmed={record.AuthorityTick};snapshot={record.SnapshotSequence};ack={record.AckSequence}",
                    Time = record.PrimaryValue.ToSingle(),
                    SecondaryTime = record.SecondaryValue.ToSingle(),
                    Priority = record.QueueDepth,
                    Cycle = record.ReplayCount,
                    Flag = record.Success,
                    Value = DebugValueSnapshot.Capture(record.InputSequence)
                });
        }

        public void PublishWorld(Fixed.SimulationWorldTraceRecord record)
        {
            if (!IsEnabled)
                return;
            m_Context.BeginLogicTick(record.Tick.Value);
            m_Context.Publish(
                RuntimeTraceChannel.Motion,
                RuntimeTraceDomain.Logic,
                RuntimeTraceEventKind.SimulationWorldBatch,
                RuntimeSourceElementHandle.Invalid,
                RuntimeInstanceKey.Character(m_Context.CharacterRuntimeId),
                new RuntimeTracePayload
                {
                    Status = record.Success ? "Success" : "Failed",
                    Name = record.Code,
                    Detail = $"{record.Detail} | Kind={record.Kind} | SourceRef={record.SourceReference} | ResultRef={record.ResultReference} | Region={record.Region} | Traversal={record.TraversalCount} | Filter={record.IncludeMask}/{record.ExcludeMask} | Status={record.LocalizationStatus}/{record.ResolveStatus}/{record.ProjectionStatus} | Elapsed={record.ElapsedStopwatchTicks} | Requested={record.RequestedDisplacement} | Applied={record.AppliedDisplacement} | Disposition={record.Disposition}",
                    OwnerId = record.ActorId.Value,
                    RelatedElementId = $"{record.SolverId.Value}@{record.SolverVersion}",
                    Priority = record.Region,
                    Cycle = record.TraversalCount,
                    Flag = record.Success,
                    Value = DebugValueSnapshot.Capture(record.Tick.Value)
                });
        }

        static RuntimeTraceEventKind ResolveBoundaryKind(Fixed.SimulationBoundaryTraceKind kind)
        {
            return kind switch
            {
                Fixed.SimulationBoundaryTraceKind.TickStarted => RuntimeTraceEventKind.SimulationTick,
                Fixed.SimulationBoundaryTraceKind.RestoreRequested or Fixed.SimulationBoundaryTraceKind.RestoreApplied => RuntimeTraceEventKind.SimulationRestore,
                Fixed.SimulationBoundaryTraceKind.EvaluateStarted or Fixed.SimulationBoundaryTraceKind.EvaluateCompleted => RuntimeTraceEventKind.SimulationEvaluate,
                Fixed.SimulationBoundaryTraceKind.WorldBatchStarted or Fixed.SimulationBoundaryTraceKind.WorldBatchCompleted => RuntimeTraceEventKind.SimulationWorldBatch,
                Fixed.SimulationBoundaryTraceKind.FinalizeStarted or Fixed.SimulationBoundaryTraceKind.FinalizeCompleted => RuntimeTraceEventKind.SimulationFinalize,
                Fixed.SimulationBoundaryTraceKind.OutputPlanValidated or Fixed.SimulationBoundaryTraceKind.CommitStarted or Fixed.SimulationBoundaryTraceKind.CommitCompleted => RuntimeTraceEventKind.SimulationCommit,
                Fixed.SimulationBoundaryTraceKind.StatePublished => RuntimeTraceEventKind.SimulationStatePublished,
                Fixed.SimulationBoundaryTraceKind.TickFailed => RuntimeTraceEventKind.SimulationFailure,
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
        }

        static RuntimeTraceEventKind ResolveOperationKind(string code)
        {
            return code switch
            {
                "operation_enter" => RuntimeTraceEventKind.NodeEntered,
                "operation_complete" => RuntimeTraceEventKind.NodeCompleted,
                "operation_stop_requested" => RuntimeTraceEventKind.NodeStopRequested,
                "operation_stopped" => RuntimeTraceEventKind.NodeStopped,
                "operation_force_stopped" => RuntimeTraceEventKind.NodeForceStopped,
                "state_transition_selected" => RuntimeTraceEventKind.StateTransitionSelected,
                "timeline_logic_time" => RuntimeTraceEventKind.TimelineLogicTime,
                "timeline_completed" => RuntimeTraceEventKind.TimelineCompleted,
                "timeline_action_context_ended" => RuntimeTraceEventKind.TimelineCancelled,
                "timeline_stopped" => RuntimeTraceEventKind.TimelineStopped,
                "tree_clip_entered" => RuntimeTraceEventKind.TreeClipEntered,
                "tree_clip_updated" => RuntimeTraceEventKind.TreeClipUpdated,
                "tree_clip_exited" => RuntimeTraceEventKind.TreeClipExited,
                "tree_clip_decision" => RuntimeTraceEventKind.ClipActive,
                "blackboard_action_window_projected" => RuntimeTraceEventKind.BlackboardProjected,
                "action_window_active" or
                "action_window_inactive" => RuntimeTraceEventKind.ActionWindowSampled,
                "action_request_unavailable" or
                "action_admission_preview" or
                "action_activation_rejected" => RuntimeTraceEventKind.ActionActivationRequested,
                "action_activated" => RuntimeTraceEventKind.ActionActivationRequested,
                "action_lifecycle" => RuntimeTraceEventKind.ActionLifecycleTransitioned,
                _ when code.StartsWith("gameplay_effect", StringComparison.Ordinal) => RuntimeTraceEventKind.GameplayEffectLifecycle,
                _ => RuntimeTraceEventKind.NodeStatus
            };
        }

        static RuntimeTraceEventKind ResolvePipelineKind(Fixed.SimulationPipelineTraceRecord record)
        {
            return record.Kind switch
            {
                Fixed.SimulationPipelineTraceKind.RestorePrepared or
                Fixed.SimulationPipelineTraceKind.RestoreApplied or
                Fixed.SimulationPipelineTraceKind.SnapshotCaptured or
                Fixed.SimulationPipelineTraceKind.SnapshotRestored => RuntimeTraceEventKind.SimulationRestore,
                Fixed.SimulationPipelineTraceKind.StatePublished => RuntimeTraceEventKind.SimulationStatePublished,
                Fixed.SimulationPipelineTraceKind.CommitCompleted or Fixed.SimulationPipelineTraceKind.EgressCompleted => RuntimeTraceEventKind.SimulationCommit,
                Fixed.SimulationPipelineTraceKind.PassFailed or Fixed.SimulationPipelineTraceKind.OuterTickFailed => RuntimeTraceEventKind.SimulationFailure,
                Fixed.SimulationPipelineTraceKind.PassCompleted when record.Phase == ThirdPersonSimulation.SimulationPipelinePhase.Step => RuntimeTraceEventKind.SimulationEvaluate,
                _ => RuntimeTraceEventKind.SimulationTick
            };
        }

        static RuntimeTraceChannel ResolveOperationChannel(RuntimeTraceEventKind kind, string code)
        {
            if (kind == RuntimeTraceEventKind.TimelineLogicTime ||
                kind == RuntimeTraceEventKind.TimelineCompleted ||
                kind == RuntimeTraceEventKind.TimelineCancelled ||
                kind == RuntimeTraceEventKind.TimelineStopped ||
                kind == RuntimeTraceEventKind.TreeClipEntered ||
                kind == RuntimeTraceEventKind.TreeClipUpdated ||
                kind == RuntimeTraceEventKind.TreeClipExited ||
                kind == RuntimeTraceEventKind.ClipActive)
                return RuntimeTraceChannel.Timeline;
            if (kind == RuntimeTraceEventKind.BlackboardProjected)
                return RuntimeTraceChannel.Blackboard;
            if (kind == RuntimeTraceEventKind.StateTransitionSelected ||
                kind == RuntimeTraceEventKind.ActionWindowSampled ||
                kind == RuntimeTraceEventKind.ActionActivationRequested ||
                kind == RuntimeTraceEventKind.ActionLifecycleTransitioned)
                return RuntimeTraceChannel.StateMachine;
            if (kind == RuntimeTraceEventKind.GameplayEffectLifecycle || code.StartsWith("gameplay_", StringComparison.Ordinal))
                return RuntimeTraceChannel.GameplayEffect;
            return RuntimeTraceChannel.Graph;
        }
    }
}
