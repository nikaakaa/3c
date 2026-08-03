using System;
using BTSMTL.Diagnostics;
using UnityEngine;
using FixedRuntime = ThirdPersonSimulation.Fixed;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    public sealed class FixedCharacterSimulationDiagnosticsAdapter : FixedRuntime.ISimulationDiagnosticsSink
    {
        readonly RuntimeDiagnosticsContext m_Context;
        readonly IDebugSourceMap m_SourceMap;
        readonly Guid m_ExecutionId;

        public FixedCharacterSimulationDiagnosticsAdapter(
            RuntimeDiagnosticsContext context,
            FixedRuntime.CharacterSimulationProgram program)
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

        public void PublishBoundary(FixedRuntime.SimulationBoundaryTraceRecord record)
        {
            if (!IsEnabled)
                return;
            m_Context.BeginLogicTick(record.Tick.Value);
            RuntimeTraceChannel channel = record.Kind == FixedRuntime.SimulationBoundaryTraceKind.WorldBatchStarted ||
                                          record.Kind == FixedRuntime.SimulationBoundaryTraceKind.WorldBatchCompleted
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

        public void PublishOperation(FixedRuntime.SimulationTraceRecord record)
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
                    Flag = record.Severity != FixedRuntime.SimulationTraceSeverity.Error,
                    Value = DebugValueSnapshot.Capture(record.Header.Sequence)
                });
            CharacterPipelineTraceCommandLine.LogOperation(record, kind, source, m_SourceMap);
        }

        public void PublishPipeline(FixedRuntime.SimulationPipelineTraceRecord record)
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

        public void PublishModel(FixedRuntime.SimulationModelTraceRecord record)
        {
            if (!IsEnabled)
                return;
            m_Context.BeginLogicTick(record.LocalSourceTick);
            RuntimeSourceElementHandle source = RuntimeSourceElementHandle.Invalid;
            if (record.Kind == FixedRuntime.SimulationModelTraceKind.OutputDisposition)
            {
                if (record.InputSequence > int.MaxValue ||
                    !m_SourceMap.TryGetProgramTarget(
                        new RuntimeSourceTarget(RuntimeSourceTargetKind.Operation, checked((int)record.InputSequence)),
                        out source))
                {
                    throw new InvalidOperationException($"Fixed output trace operation '{record.InputSequence}' is absent from the Debug Source Map.");
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

        public void PublishWorld(FixedRuntime.SimulationWorldTraceRecord record)
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

        static RuntimeTraceEventKind ResolveBoundaryKind(FixedRuntime.SimulationBoundaryTraceKind kind)
        {
            return kind switch
            {
                FixedRuntime.SimulationBoundaryTraceKind.TickStarted => RuntimeTraceEventKind.SimulationTick,
                FixedRuntime.SimulationBoundaryTraceKind.RestoreRequested or FixedRuntime.SimulationBoundaryTraceKind.RestoreApplied => RuntimeTraceEventKind.SimulationRestore,
                FixedRuntime.SimulationBoundaryTraceKind.EvaluateStarted or FixedRuntime.SimulationBoundaryTraceKind.EvaluateCompleted => RuntimeTraceEventKind.SimulationEvaluate,
                FixedRuntime.SimulationBoundaryTraceKind.WorldBatchStarted or FixedRuntime.SimulationBoundaryTraceKind.WorldBatchCompleted => RuntimeTraceEventKind.SimulationWorldBatch,
                FixedRuntime.SimulationBoundaryTraceKind.FinalizeStarted or FixedRuntime.SimulationBoundaryTraceKind.FinalizeCompleted => RuntimeTraceEventKind.SimulationFinalize,
                FixedRuntime.SimulationBoundaryTraceKind.OutputPlanValidated or FixedRuntime.SimulationBoundaryTraceKind.CommitStarted or FixedRuntime.SimulationBoundaryTraceKind.CommitCompleted => RuntimeTraceEventKind.SimulationCommit,
                FixedRuntime.SimulationBoundaryTraceKind.StatePublished => RuntimeTraceEventKind.SimulationStatePublished,
                FixedRuntime.SimulationBoundaryTraceKind.TickFailed => RuntimeTraceEventKind.SimulationFailure,
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
                "condition_value_evaluated" => RuntimeTraceEventKind.ConditionGraphEvaluated,
                "condition_graph_evaluated" => RuntimeTraceEventKind.ConditionGraphEvaluated,
                "state_transition_evaluated" => RuntimeTraceEventKind.StateTransitionEvaluated,
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
                "equipment_snapshot" => RuntimeTraceEventKind.EquipmentSnapshot,
                "equipment_change" => RuntimeTraceEventKind.EquipmentChange,
                "equipment_host" => RuntimeTraceEventKind.EquipmentHost,
                "motion_contribution" => RuntimeTraceEventKind.MotionContribution,
                "motion_channel_resolved" or
                "resolved_gameplay_motion" or
                "motion_warp_applied" or
                "world_result_applied" => RuntimeTraceEventKind.MotionResolved,
                _ when code.StartsWith("motion_warp_", StringComparison.Ordinal) => RuntimeTraceEventKind.MotionResolved,
                _ when code.StartsWith("gameplay_effect", StringComparison.Ordinal) => RuntimeTraceEventKind.GameplayEffectLifecycle,
                _ => RuntimeTraceEventKind.NodeStatus
            };
        }

        static RuntimeTraceEventKind ResolvePipelineKind(FixedRuntime.SimulationPipelineTraceRecord record)
        {
            return record.Kind switch
            {
                FixedRuntime.SimulationPipelineTraceKind.RestorePrepared or
                FixedRuntime.SimulationPipelineTraceKind.RestoreApplied or
                FixedRuntime.SimulationPipelineTraceKind.SnapshotCaptured or
                FixedRuntime.SimulationPipelineTraceKind.SnapshotRestored => RuntimeTraceEventKind.SimulationRestore,
                FixedRuntime.SimulationPipelineTraceKind.StatePublished => RuntimeTraceEventKind.SimulationStatePublished,
                FixedRuntime.SimulationPipelineTraceKind.CommitCompleted or FixedRuntime.SimulationPipelineTraceKind.EgressCompleted => RuntimeTraceEventKind.SimulationCommit,
                FixedRuntime.SimulationPipelineTraceKind.PassFailed or FixedRuntime.SimulationPipelineTraceKind.OuterTickFailed => RuntimeTraceEventKind.SimulationFailure,
                FixedRuntime.SimulationPipelineTraceKind.PassCompleted when record.Phase == ThirdPersonSimulation.SimulationPipelinePhase.Step => RuntimeTraceEventKind.SimulationEvaluate,
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
            if (kind == RuntimeTraceEventKind.MotionContribution ||
                kind == RuntimeTraceEventKind.MotionResolved)
                return RuntimeTraceChannel.Motion;
            if (kind == RuntimeTraceEventKind.StateTransitionEvaluated ||
                kind == RuntimeTraceEventKind.StateTransitionSelected ||
                kind == RuntimeTraceEventKind.ActionWindowSampled ||
                kind == RuntimeTraceEventKind.ActionActivationRequested ||
                kind == RuntimeTraceEventKind.ActionLifecycleTransitioned)
                return RuntimeTraceChannel.StateMachine;
            if (kind == RuntimeTraceEventKind.GameplayEffectLifecycle || code.StartsWith("gameplay_", StringComparison.Ordinal))
                return RuntimeTraceChannel.GameplayEffect;
            if (kind == RuntimeTraceEventKind.EquipmentSnapshot ||
                kind == RuntimeTraceEventKind.EquipmentChange ||
                kind == RuntimeTraceEventKind.EquipmentHost)
                return RuntimeTraceChannel.Equipment;
            return RuntimeTraceChannel.Graph;
        }
    }

    public static class CharacterPipelineTraceCommandLine
    {
        const string Argument = "--character-pipeline-trace";
        static readonly bool s_Requested = Array.Exists(
            Environment.GetCommandLineArgs(),
            value => string.Equals(value, Argument, StringComparison.Ordinal));

        public static void Enable(RuntimeDiagnosticsStore store)
        {
            if (!s_Requested)
                return;
            if (store == null)
                throw new ArgumentNullException(nameof(store));
            store.AcquireInterest(new RuntimeDiagnosticsInterest(
                RuntimeDiagnosticsInterestKind.LiveState,
                RuntimeTraceChannel.All));
        }

        internal static void LogOperation(
            FixedRuntime.SimulationTraceRecord record,
            RuntimeTraceEventKind kind,
            RuntimeSourceElementHandle source,
            IDebugSourceMap sourceMap)
        {
            if (!s_Requested ||
                kind != RuntimeTraceEventKind.StateTransitionEvaluated &&
                kind != RuntimeTraceEventKind.StateTransitionSelected &&
                kind != RuntimeTraceEventKind.ConditionGraphEvaluated &&
                kind != RuntimeTraceEventKind.MotionContribution &&
                kind != RuntimeTraceEventKind.MotionResolved)
            {
                return;
            }

            string sourceIdentity = source.ToString();
            if (sourceMap.TryGet(source, out DebugSourceMapEntry entry))
            {
                RuntimeSourceElementKey key = entry.Source;
                sourceIdentity =
                    $"{entry.DisplayName}|{key.Kind}|graph={key.GraphAuthoringId}|element={key.ElementAuthoringId}|timeline={key.TimelineAuthoringId}|track={key.TrackAuthoringId}|clip={key.ClipAuthoringId}";
            }
            Debug.Log(
                $"[CharacterPipelineTrace] Actor={record.Header.ActorId.Value} Tick={record.Header.Tick.Value} Code={record.Code} Source={sourceIdentity} Detail={record.Detail}");
        }
    }
}
