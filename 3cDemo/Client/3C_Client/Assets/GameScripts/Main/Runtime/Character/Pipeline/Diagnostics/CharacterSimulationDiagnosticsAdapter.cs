using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics
{
    public sealed class CharacterSimulationDiagnosticsAdapter : ISimulationDiagnosticsSink, ISimulationValueTraceInterest, ISimulationControlTraceInterest
    {
        readonly RuntimeDiagnosticsContext m_Context;
        readonly IDebugSourceMap m_SourceMap;
        readonly Guid m_ExecutionId;
        readonly string[] m_GraphInvocationPaths;
        readonly CharacterPortValueDiagnostics m_PortValues;
        readonly CharacterControlFlowDiagnostics m_ControlEdges;
        readonly Dictionary<string, RuntimeSourceElementHandle> m_ControlModules = new Dictionary<string, RuntimeSourceElementHandle>(StringComparer.Ordinal);
        readonly Dictionary<string, RuntimeSourceElementHandle> m_ControlStates = new Dictionary<string, RuntimeSourceElementHandle>(StringComparer.Ordinal);
        readonly Dictionary<string, RuntimeSourceElementHandle> m_ControlTransitions = new Dictionary<string, RuntimeSourceElementHandle>(StringComparer.Ordinal);

        public CharacterSimulationDiagnosticsAdapter(
            RuntimeDiagnosticsContext context,
            CharacterSimulationProgram program)
        {
            m_Context = context ?? throw new ArgumentNullException(nameof(context));
            m_SourceMap = context.SourceMap;
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            m_ExecutionId = context.SessionId;
            m_PortValues = new CharacterPortValueDiagnostics(context, m_ExecutionId, program.SourceMap);
            m_ControlEdges = new CharacterControlFlowDiagnostics(context, m_ExecutionId, program.SourceMap, program.ControlFlow);
            m_GraphInvocationPaths = new string[program.Operations.Count];
            for (int i = 0; i < program.SourceMap.Count; i++)
            {
                ProgramSourceMapEntry entry = program.SourceMap[i];
                if (entry.TargetKind == ProgramSourceTargetKind.Operation)
                    m_GraphInvocationPaths[entry.TargetIndex] = entry.GraphInvocationPath;
                RuntimeSourceTarget target = entry.TargetKind switch
                {
                    ProgramSourceTargetKind.ControlModule => new RuntimeSourceTarget(RuntimeSourceTargetKind.ControlModule, entry.TargetIndex),
                    ProgramSourceTargetKind.ControlState => new RuntimeSourceTarget(RuntimeSourceTargetKind.ControlState, entry.TargetIndex),
                    ProgramSourceTargetKind.ControlTransition => new RuntimeSourceTarget(RuntimeSourceTargetKind.ControlTransition, entry.TargetIndex),
                    _ => default
                };
                if (!target.IsProgramTarget || !m_SourceMap.TryGetProgramTarget(target, out RuntimeSourceElementHandle handle))
                    continue;
                if (entry.TargetKind == ProgramSourceTargetKind.ControlModule)
                    m_ControlModules[entry.GraphId] = handle;
                else if (entry.TargetKind == ProgramSourceTargetKind.ControlState)
                    m_ControlStates[entry.NodeId] = handle;
                else
                    m_ControlTransitions[entry.EdgeId] = handle;
            }
            for (int i = 0; i < program.Operations.Count; i++)
            {
                if (!m_SourceMap.TryGetProgramTarget(
                        new RuntimeSourceTarget(RuntimeSourceTargetKind.Operation, i),
                        out _))
                    throw new InvalidOperationException($"Program operation '{i}' is absent from the Debug Source Map.");
            }
            for (int i = 0; i < program.StateSlots.Count; i++)
            {
                RuntimeSourceTarget target = CharacterRuntimeDebugTargetResolver.ForStateSlot(program.StateSlots[i]);
                if (!m_SourceMap.TryGetProgramTarget(target, out _))
                    throw new InvalidOperationException($"Program state target '{target}' for slot '{i}' is absent from the Debug Source Map.");
            }
        }

        public RuntimeDiagnosticsContext Context => m_Context;
        public bool IsEnabled => m_Context.Store.EffectiveChannels != RuntimeTraceChannel.None;
        public bool IsValueCaptureRequested(ActorId actorId) => m_Context.Store.IsInterested(RuntimeTraceChannel.Values, RuntimeTraceEventKind.ValueSampled);
        public bool IsControlCaptureRequested(ActorId actorId) =>
            m_Context.Store.IsInterested(RuntimeTraceChannel.Graph, RuntimeTraceEventKind.EdgeSelected) ||
            m_Context.Store.IsInterested(RuntimeTraceChannel.StateMachine, RuntimeTraceEventKind.StateTransitionSelected);

        public void PublishBoundary(SimulationBoundaryTraceRecord record)
        {
            if (!IsEnabled)
                return;
            m_Context.BeginLogicTick(record.Tick.Value);
            RuntimeTraceChannel channel = record.Kind == SimulationBoundaryTraceKind.WorldBatchStarted ||
                                          record.Kind == SimulationBoundaryTraceKind.WorldBatchCompleted
                ? RuntimeTraceChannel.Motion
                : RuntimeTraceChannel.Graph;
            RuntimeTraceEventKind kind = ResolveBoundaryKind(record.Kind);
            m_Context.Publish(
                channel,
                RuntimeTraceDomain.Lifecycle,
                kind,
                RuntimeSourceElementHandle.Invalid,
                RuntimeInstanceKey.Character(m_Context.CharacterRuntimeId),
                new RuntimeTracePayload
                {
                    Status = record.Success ? "Success" : "Failed",
                    Name = record.Kind.ToString(),
                    Detail = BuildDetail(record),
                    OwnerId = record.ActorId.IsValid ? record.ActorId.Value : string.Empty,
                    RelatedElementId = record.SnapshotIdentity,
                    Flag = record.Success,
                    Value = DebugValueSnapshot.Capture(record.Tick.Value)
                });
        }

        public void PublishOperation(SimulationTraceRecord record)
        {
            if (!IsEnabled)
                return;
            m_Context.BeginLogicTick(record.Header.Tick.Value);
            if (record.ControlFlow != null)
            {
                m_ControlEdges.Publish(record.ControlFlow, record.ControlFlowSelected, record.ControlFlowPassed,
                    record.SkillId, record.ActionInstanceId, record.SkillExecutionGeneration, record.Header.Activation.Generation,
                    record.GraphInvocationGeneration, record.ParentInvocationGeneration);
                return;
            }
            if (record.Code == "value_sampling_limit")
            {
                m_PortValues.PublishLimit(record.Detail);
                return;
            }
            if (record.ValueTrace != null)
            {
                m_PortValues.Publish(record.Header.Activation.Source.Operation.Value, record.ValueTrace.PortId,
                    record.ValueTrace.Direction, CaptureValue(record.ValueTrace.Value), record.SkillId, record.ActionInstanceId,
                    record.SkillExecutionGeneration, record.Header.Activation.Generation, record.GraphInvocationGeneration, record.ParentInvocationGeneration);
                return;
            }
            if (record.Header.Activation.Source.IsCharacterControl)
            {
                PublishControl(record);
                return;
            }
            if (!record.Header.Activation.Source.IsSkillOperation)
                throw new InvalidOperationException("Operation trace requires a Skill operation or Character control execution source.");
            var target = new RuntimeSourceTarget(
                RuntimeSourceTargetKind.Operation,
                record.Header.Activation.Source.Operation.Value);
            if (!m_SourceMap.TryGetProgramTarget(target, out RuntimeSourceElementHandle source))
                throw new InvalidOperationException($"Operation trace target '{target}' is absent from the Debug Source Map.");
            RuntimeTraceEventKind kind = ResolveOperationKind(record.Code);
            string callSite = m_GraphInvocationPaths[record.Header.Activation.Source.Operation.Value];
            if (record.ActionInstanceId != 0 && string.IsNullOrEmpty(callSite))
                throw new InvalidOperationException("技能诊断操作缺少编译图调用路径。");
            RuntimeInstanceKey runtimeInstance = record.ActionInstanceId != 0
                ? RuntimeInstanceKey.SkillExecution(
                    m_Context.CharacterRuntimeId,
                    m_ExecutionId,
                    record.SkillId,
                    record.ActionInstanceId,
                    callSite,
                    record.SkillExecutionGeneration,
                    record.GraphInvocationGeneration)
                : RuntimeInstanceKey.Runnable(
                    m_Context.CharacterRuntimeId,
                    m_ExecutionId,
                    record.Header.Activation.Source.Operation.Value.ToString(),
                    record.Header.Activation.Generation);
            m_Context.Publish(
                ResolveOperationChannel(kind, record.Code),
                RuntimeTraceDomain.Logic,
                kind,
                source,
                runtimeInstance,
                new RuntimeTracePayload
                {
                    Status = record.Severity.ToString(),
                    Name = record.Code,
                    Detail = record.Detail,
                    Cause = record.Boundary,
                    OwnerId = record.Header.ActorId.Value,
                    SkillId = record.SkillId,
                    ActionInstanceId = record.ActionInstanceId,
                    CallSiteId = callSite,
                    ActivationGeneration = record.Header.Activation.Generation,
                    SkillExecutionGeneration = record.SkillExecutionGeneration,
                    GraphInvocationGeneration = record.GraphInvocationGeneration,
                    ParentInvocationGeneration = record.ParentInvocationGeneration,
                    Flag = record.Severity != SimulationTraceSeverity.Error,
                    Value = DebugValueSnapshot.Capture(record.Header.Sequence)
                });
        }

        static DebugValueSnapshot CaptureValue(CharacterStateValue value) => value.Kind switch
        {
            ProgramStateValueKind.Boolean => DebugValueSnapshot.Capture(value.Boolean),
            ProgramStateValueKind.Int32 => DebugValueSnapshot.Capture(value.Int32),
            ProgramStateValueKind.UInt64 => DebugValueSnapshot.Capture(value.UInt64),
            ProgramStateValueKind.Scalar => DebugValueSnapshot.Capture(value.Scalar.Value),
            ProgramStateValueKind.Identity => DebugValueSnapshot.Capture(value.Identity),
            ProgramStateValueKind.Vector2 => DebugValueSnapshot.Capture(new UnityEngine.Vector2(value.Vector2.X.Value, value.Vector2.Y.Value)),
            ProgramStateValueKind.Vector3 => DebugValueSnapshot.Capture(new UnityEngine.Vector3(value.Vector3.X.Value, value.Vector3.Y.Value, value.Vector3.Z.Value)),
            ProgramStateValueKind.Yaw => DebugValueSnapshot.Capture(value.Yaw.Degrees.Value),
            _ => DebugValueSnapshot.Capture(value)
        };

        void PublishControl(SimulationTraceRecord record)
        {
            SimulationExecutionSource source = record.Header.Activation.Source;
            RuntimeSourceElementHandle sourceHandle = ResolveControlSource(source);
            RuntimeInstanceKey instance = record.ActionInstanceId != 0
                ? RuntimeInstanceKey.ActionInstance(
                    m_Context.CharacterRuntimeId,
                    m_ExecutionId,
                    record.SkillId,
                    record.ActionInstanceId,
                    record.Header.Activation.Generation)
                : source.TransitionId.IsValid
                ? RuntimeInstanceKey.ControlTransition(
                    m_Context.CharacterRuntimeId,
                    m_ExecutionId,
                    source.TransitionId.Value,
                    record.Header.Activation.Generation)
                : source.StateId.IsValid
                    ? RuntimeInstanceKey.ControlState(
                        m_Context.CharacterRuntimeId,
                        m_ExecutionId,
                        source.StateId.Value,
                        record.Header.Activation.Generation)
                    : RuntimeInstanceKey.ControlModule(m_Context.CharacterRuntimeId, m_ExecutionId);
            m_Context.Publish(
                RuntimeTraceChannel.StateMachine,
                RuntimeTraceDomain.Logic,
                ResolveControlKind(record.Code),
                sourceHandle,
                instance,
                new RuntimeTracePayload
                {
                    Status = record.Severity.ToString(),
                    Name = record.Code,
                    Detail = $"{record.Detail} | Source={source.Identity}",
                    OwnerId = source.ModuleId.Value,
                    RelatedElementId = source.TransitionId.IsValid
                        ? source.TransitionId.Value
                        : source.StateId.Value,
                    SkillId = record.SkillId,
                    ActionInstanceId = record.ActionInstanceId,
                    ActivationGeneration = record.Header.Activation.Generation,
                    Flag = record.Severity != SimulationTraceSeverity.Error,
                    Value = DebugValueSnapshot.Capture(record.Header.Activation.Generation)
                });
        }

        RuntimeSourceElementHandle ResolveControlSource(SimulationExecutionSource source)
        {
            if (source.TransitionId.IsValid && m_ControlTransitions.TryGetValue(source.TransitionId.Value, out RuntimeSourceElementHandle transition))
                return transition;
            if (source.StateId.IsValid && m_ControlStates.TryGetValue(source.StateId.Value, out RuntimeSourceElementHandle state))
                return state;
            return m_ControlModules.TryGetValue(source.ModuleId.Value, out RuntimeSourceElementHandle module)
                ? module
                : throw new InvalidOperationException($"Control source '{source.Identity}' is absent from the Debug Source Map.");
        }

        public void PublishPipeline(SimulationPipelineTraceRecord record)
        {
            if (!IsEnabled)
                return;
            m_Context.BeginLogicTick(record.Source.SourceTick);
            RuntimeTraceEventKind kind = ResolvePipelineKind(record);
            RuntimeTraceChannel channel = record.Phase == SimulationPipelinePhase.Step
                ? RuntimeTraceChannel.Motion
                : RuntimeTraceChannel.Graph;
            string name = record.PassId.IsValid ? record.PassId.Value : record.Kind.ToString();
            m_Context.Publish(
                channel,
                RuntimeTraceDomain.Lifecycle,
                kind,
                RuntimeSourceElementHandle.Invalid,
                RuntimeInstanceKey.Character(m_Context.CharacterRuntimeId),
                new RuntimeTracePayload
                {
                    Status = record.Success ? "Success" : "Failed",
                    Name = name,
                    Detail = BuildPipelineDetail(record),
                    OwnerId = record.Session.ToString(),
                    RelatedElementId = record.SnapshotParticipant,
                    Flag = record.Success,
                    Value = DebugValueSnapshot.Capture(record.ElapsedStopwatchTicks)
                });
        }

        public void PublishModel(SimulationModelTraceRecord record)
        {
            if (!IsEnabled)
                return;
            m_Context.BeginLogicTick(record.LocalSourceTick);
            m_Context.Publish(
                RuntimeTraceChannel.Network,
                RuntimeTraceDomain.Lifecycle,
                RuntimeTraceEventKind.SimulationNetworkModel,
                RuntimeSourceElementHandle.Invalid,
                RuntimeInstanceKey.Character(m_Context.CharacterRuntimeId),
                new RuntimeTracePayload
                {
                    Status = record.Success ? "Success" : "Failed",
                    Name = record.Code,
                    Detail = record.Detail,
                    Cause = record.Kind.ToString(),
                    OwnerId = record.ActorId.IsValid ? record.ActorId.Value : string.Empty,
                    RelatedElementId = $"authority={record.AuthorityTick};ack={record.AckSequence}",
                    Time = record.PrimaryValue,
                    SecondaryTime = record.SecondaryValue,
                    Priority = record.QueueDepth,
                    Cycle = record.ReplayCount,
                    Flag = record.Success,
                    Value = DebugValueSnapshot.Capture(record.InputSequence)
                });
        }

        public void PublishWorld(SimulationWorldTraceRecord record)
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
                    Detail = BuildWorldDetail(record),
                    OwnerId = record.ActorId.Value,
                    RelatedElementId = $"{record.SolverId.Value}@{record.SolverVersion}",
                    Priority = record.Region,
                    Cycle = record.TraversalCount,
                    Flag = record.Success,
                    Value = DebugValueSnapshot.Capture(record.Tick.Value)
                });
        }

        static RuntimeTraceEventKind ResolveBoundaryKind(SimulationBoundaryTraceKind kind)
        {
            return kind switch
            {
                SimulationBoundaryTraceKind.TickStarted => RuntimeTraceEventKind.SimulationTick,
                SimulationBoundaryTraceKind.RestoreRequested or
                SimulationBoundaryTraceKind.RestoreApplied => RuntimeTraceEventKind.SimulationRestore,
                SimulationBoundaryTraceKind.EvaluateStarted or
                SimulationBoundaryTraceKind.EvaluateCompleted => RuntimeTraceEventKind.SimulationEvaluate,
                SimulationBoundaryTraceKind.WorldBatchStarted or
                SimulationBoundaryTraceKind.WorldBatchCompleted => RuntimeTraceEventKind.SimulationWorldBatch,
                SimulationBoundaryTraceKind.FinalizeStarted or
                SimulationBoundaryTraceKind.FinalizeCompleted => RuntimeTraceEventKind.SimulationFinalize,
                SimulationBoundaryTraceKind.OutputPlanValidated => RuntimeTraceEventKind.SimulationCommit,
                SimulationBoundaryTraceKind.StatePublished => RuntimeTraceEventKind.SimulationStatePublished,
                SimulationBoundaryTraceKind.CommitStarted or
                SimulationBoundaryTraceKind.CommitCompleted => RuntimeTraceEventKind.SimulationCommit,
                SimulationBoundaryTraceKind.TickFailed => RuntimeTraceEventKind.SimulationFailure,
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
        }

        static RuntimeTraceEventKind ResolveOperationKind(string code)
        {
            return code switch
            {
                "operation_enter" => RuntimeTraceEventKind.NodeEntered,
                "operation_running" => RuntimeTraceEventKind.NodeRunning,
                "operation_waiting" => RuntimeTraceEventKind.NodeWaiting,
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

        static RuntimeTraceEventKind ResolveControlKind(string code)
        {
            return code switch
            {
                "control_state_entered" => RuntimeTraceEventKind.StateScopeEntered,
                "control_state_exited" => RuntimeTraceEventKind.StateScopeExited,
                "control_transition_evaluated" => RuntimeTraceEventKind.StateTransitionEvaluated,
                "control_transition_selected" => RuntimeTraceEventKind.StateTransitionSelected,
                _ => RuntimeTraceEventKind.NodeStatus
            };
        }

        static RuntimeTraceEventKind ResolvePipelineKind(SimulationPipelineTraceRecord record)
        {
            return record.Kind switch
            {
                SimulationPipelineTraceKind.RestorePrepared or
                SimulationPipelineTraceKind.RestoreApplied or
                SimulationPipelineTraceKind.SnapshotCaptured or
                SimulationPipelineTraceKind.SnapshotRestored => RuntimeTraceEventKind.SimulationRestore,
                SimulationPipelineTraceKind.StatePublished => RuntimeTraceEventKind.SimulationStatePublished,
                SimulationPipelineTraceKind.CommitCompleted or
                SimulationPipelineTraceKind.EgressCompleted => RuntimeTraceEventKind.SimulationCommit,
                SimulationPipelineTraceKind.PassFailed or
                SimulationPipelineTraceKind.OuterTickFailed => RuntimeTraceEventKind.SimulationFailure,
                SimulationPipelineTraceKind.PassCompleted when record.Phase == SimulationPipelinePhase.Step =>
                    RuntimeTraceEventKind.SimulationEvaluate,
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
            if (kind == RuntimeTraceEventKind.GameplayEffectLifecycle ||
                code.StartsWith("gameplay_", StringComparison.Ordinal))
                return RuntimeTraceChannel.GameplayEffect;
            if (kind == RuntimeTraceEventKind.EquipmentSnapshot ||
                kind == RuntimeTraceEventKind.EquipmentChange)
                return RuntimeTraceChannel.Equipment;
            return RuntimeTraceChannel.Graph;
        }

        static string BuildDetail(SimulationBoundaryTraceRecord record)
        {
            return $"{record.Detail} | Source={record.Source.Kind}:{record.Source.ClockId}/{record.Source.SourceTick} | Program={record.ProgramId.Value} | Solver={record.SolverId.Value} | Capabilities={record.SolverCapabilities} | StateHash={record.CharacterStateHash} | WorldHash={record.WorldHash} | Deterministic={record.DeterministicValidity}";
        }

        static string BuildPipelineDetail(SimulationPipelineTraceRecord record)
        {
            return $"{record.Detail} | Pipeline={record.Pipeline} | Source={record.Source.Kind}:{record.Source.ClockId}/{record.Source.SourceTick} | CompletedTick={record.CompletedTick} | Phase={record.Phase} | PassVersion={record.PassVersion} | Schedule={record.ScheduleStatus} | Restore={record.RestoreRequested} | Steps={record.StepCount} | Inputs={record.ProductInputs} | Outputs={record.ProductOutputs} | Snapshot={record.SnapshotParticipant}/{record.SnapshotHash}";
        }

        static string BuildWorldDetail(SimulationWorldTraceRecord record)
        {
            return $"{record.Detail} | Kind={record.Kind} | SourceRef={record.SourceReference} | ResultRef={record.ResultReference} | Region={record.Region} | Traversal={record.TraversalCount} | Filter={record.IncludeMask}/{record.ExcludeMask} | Status={record.LocalizationStatus}/{record.ResolveStatus}/{record.ProjectionStatus} | Elapsed={record.ElapsedStopwatchTicks} | Requested={record.RequestedDisplacement} | Applied={record.AppliedDisplacement} | Disposition={record.Disposition}";
        }
    }
}
