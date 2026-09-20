using System;
using ThirdPersonSimulation.Fixed;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThirdPersonSimulation
{


    public enum TimelineClipTimePoint : byte
    {
        Start = 1,
        End = 2,
        CurveEnd = 3,
        EaseIn = 4,
        EaseOut = 5
    }

    public enum TimelineCurveChannel : byte
    {
        Weight = 1,
        EaseIn = 2,
        EaseOut = 3,
        PositionX = 4,
        PositionY = 5,
        PositionZ = 6,
        Yaw = 7
    }

    public enum TimelineClipScalarValue : byte
    {
        Intensity = 1
    }

    public enum TimelineTreeClipEdgeKind : byte
    {
        Root = 1,
        Enable = 2,
        Disable = 3,
        Destroy = 4
    }

    public enum TimelinePresentationOutputKind : byte
    {
        SelectProducer = 1,
        SampleProducer = 2,
        CompleteProducer = 3,
        ReleaseProducer = 4,
        Camera = 5,
        Cue = 6,
        ForceProducer = 7
    }

    public enum TimelineTraceSeverity : byte
    {
        Detail = 1,
        Information = 2,
        Warning = 3,
        Error = 4
    }

    public readonly struct TimelineActionContextIdentity : IEquatable<TimelineActionContextIdentity>
    {
        public TimelineActionContextIdentity(
            string actionId,
            string contextId,
            ulong instanceId,
            ulong predictionKey,
            CharacterSkillId skillId = default,
            OperationHandle skillEntryOperation = default,
            ulong skillExecutionGeneration = 0)
        {
            if (skillId.IsValid != skillEntryOperation.IsValid || !skillId.IsValid && skillExecutionGeneration != 0)
                throw new ArgumentException("Timeline Action Context skill execution identity is incomplete.");
            ActionId = actionId ?? string.Empty;
            ContextId = contextId ?? string.Empty;
            InstanceId = instanceId;
            PredictionKey = predictionKey;
            SkillId = skillId;
            SkillEntryOperation = skillEntryOperation;
            SkillExecutionGeneration = skillExecutionGeneration;
        }

        public string ActionId { get; }
        public string ContextId { get; }
        public ulong InstanceId { get; }
        public ulong PredictionKey { get; }
        public CharacterSkillId SkillId { get; }
        public OperationHandle SkillEntryOperation { get; }
        public ulong SkillExecutionGeneration { get; }
        public bool HasSkillExecution => SkillId.IsValid && SkillEntryOperation.IsValid;
        public bool IsValid =>
            !string.IsNullOrEmpty(ActionId) &&
            !string.IsNullOrEmpty(ContextId) &&
            InstanceId != 0 &&
            PredictionKey != 0;

        public bool Equals(TimelineActionContextIdentity other) =>
            string.Equals(ActionId, other.ActionId, StringComparison.Ordinal) &&
            string.Equals(ContextId, other.ContextId, StringComparison.Ordinal) &&
            InstanceId == other.InstanceId &&
            PredictionKey == other.PredictionKey &&
            SkillId == other.SkillId &&
            SkillEntryOperation.Equals(other.SkillEntryOperation) &&
            SkillExecutionGeneration == other.SkillExecutionGeneration;

        public override int GetHashCode() => HashCode.Combine(ActionId, ContextId, InstanceId, PredictionKey, SkillId, SkillEntryOperation, SkillExecutionGeneration);
    }

    public readonly struct AbilityTimelineInvocationSource
    {
        public AbilityTimelineInvocationSource(
            int operationIndex,
            string graphAuthoringId,
            string nodeAuthoringId,
            string graphInvocationPath,
            ulong invocationGeneration)
        {
            if (operationIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(operationIndex));
            OperationIndex = operationIndex;
            GraphAuthoringId = SimulationIdentity.Require(graphAuthoringId, nameof(graphAuthoringId));
            NodeAuthoringId = SimulationIdentity.Require(nodeAuthoringId, nameof(nodeAuthoringId));
            GraphInvocationPath = SimulationIdentity.Require(graphInvocationPath, nameof(graphInvocationPath));
            if (invocationGeneration == 0)
                throw new ArgumentOutOfRangeException(nameof(invocationGeneration));
            InvocationGeneration = invocationGeneration;
        }

        public int OperationIndex { get; }
        public string GraphAuthoringId { get; }
        public string NodeAuthoringId { get; }
        public string GraphInvocationPath { get; }
        public ulong InvocationGeneration { get; }
        public bool IsValid => OperationIndex >= 0 &&
                               !string.IsNullOrEmpty(GraphAuthoringId) &&
                               !string.IsNullOrEmpty(NodeAuthoringId) &&
                               !string.IsNullOrEmpty(GraphInvocationPath) &&
                               InvocationGeneration != 0;
    }

    public readonly struct TimelineSegment<TTime>
        where TTime : struct
    {
        public TimelineSegment(TTime previous, TTime current, int cycle, bool startsCycle)
        {
            if (cycle < 0)
                throw new ArgumentOutOfRangeException(nameof(cycle));
            Previous = previous;
            Current = current;
            Cycle = cycle;
            StartsCycle = startsCycle;
        }

        public TTime Previous { get; }
        public TTime Current { get; }
        public int Cycle { get; }
        public bool StartsCycle { get; }
    }

    internal readonly struct MotionWarpSample<TTime, TAction>
        where TTime : struct
        where TAction : struct
    {
        public MotionWarpSample(
            OperationHandle operation,
            TimelineSegment<TTime> segment,
            ulong playbackGeneration,
            TimelineActionContextIdentity actionContext,
            TAction action)
        {
            if (!operation.IsValid)
                throw new ArgumentException("MotionWarp sample requires a valid operation.", nameof(operation));
            if (playbackGeneration == 0)
                throw new ArgumentOutOfRangeException(nameof(playbackGeneration));
            if (!actionContext.IsValid)
                throw new ArgumentException("MotionWarp sample requires a valid Action Context.", nameof(actionContext));
            Operation = operation;
            Segment = segment;
            PlaybackGeneration = playbackGeneration;
            ActionContext = actionContext;
            Action = action;
        }

        public OperationHandle Operation { get; }
        public TimelineSegment<TTime> Segment { get; }
        public ulong PlaybackGeneration { get; }
        public TimelineActionContextIdentity ActionContext { get; }
        public TAction Action { get; }
    }

    internal enum MotionWarpLifecycleDecision : byte
    {
        Initialize = 1,
        Continue = 2
    }

    internal static class MotionModifierDiagnosticCode
    {
        public const string SourceNotResolved = "motion_warp_source_not_resolved";
        public const string TargetSnapshotRequired = "motion_warp_target_snapshot_required";
        public const string NoTargetByOptionalPolicy = "motion_warp_no_target_by_optional_policy";
        public const string AmbiguousModifier = "motion_warp_ambiguous_modifier";
        public const string InvalidState = "motion_warp_invalid_state";
        public const string FaceTargetZeroDirection = "motion_warp_face_target_zero_direction";
        public const string ApproachDirectionZero = "motion_warp_approach_direction_zero";
        public const string ScaleSourcePositionZero = "motion_warp_scale_source_position_zero";
        public const string ScaleSourceYawZero = "motion_warp_scale_source_yaw_zero";
        public const string TargetResolved = "motion_warp_target_resolved";
        public const string Applied = "motion_warp_applied";
        public const string AppliedClamped = "motion_warp_applied_clamped";
        public const string PreservedByLimitPolicy = "motion_warp_preserved_by_limit_policy";
    }

    internal static class MotionWarpRuntimeSemantics
    {
        public static ulong ComposePlaybackGeneration(ulong activationGeneration, int cycle)
        {
            if (activationGeneration == 0 || activationGeneration > uint.MaxValue)
                throw new InvalidOperationException($"MotionWarp Timeline activation generation '{activationGeneration}' exceeds the canonical lifecycle range.");
            if (cycle < 0)
                throw new ArgumentOutOfRangeException(nameof(cycle));
            return activationGeneration << 32 | (uint)cycle;
        }

        public static MotionWarpLifecycleDecision ResolveLifecycle(
            bool active,
            bool initialized,
            ulong storedPlaybackGeneration,
            TimelineActionContextIdentity storedAction,
            int storedSourceOperation,
            ulong currentPlaybackGeneration,
            TimelineActionContextIdentity currentAction,
            OperationHandle expectedSourceOperation)
        {
            if (!currentAction.IsValid || !expectedSourceOperation.IsValid || currentPlaybackGeneration == 0)
                throw new InvalidOperationException($"{MotionModifierDiagnosticCode.InvalidState}: current MotionWarp lifecycle identity is incomplete.");
            if (active != initialized)
                throw new InvalidOperationException($"{MotionModifierDiagnosticCode.InvalidState}: active and initialized state disagree.");
            if (!active)
                return MotionWarpLifecycleDecision.Initialize;
            if (storedSourceOperation != expectedSourceOperation.Value)
                throw new InvalidOperationException($"{MotionModifierDiagnosticCode.InvalidState}: restored source operation does not match the Program descriptor.");
            if (storedPlaybackGeneration != currentPlaybackGeneration)
                return MotionWarpLifecycleDecision.Initialize;
            if (!storedAction.Equals(currentAction))
                throw new InvalidOperationException($"{MotionModifierDiagnosticCode.InvalidState}: restored Action instance does not match the active Action Context.");
            return MotionWarpLifecycleDecision.Continue;
        }
    }

    internal interface IMotionModifierTarget<TTime, TAction, TChannel>
        where TTime : struct
        where TAction : struct
        where TChannel : struct
    {
        void Reset(ProgramMotionModifierDescriptor descriptor);
        void TraceSourceNotResolved(ProgramMotionModifierDescriptor descriptor, OperationHandle resolvedOwner);
        void ApplyMotionWarp(
            ProgramMotionModifierDescriptor descriptor,
            MotionWarpSample<TTime, TAction> sample,
            ref TChannel channel);
        void Fail(string code, ProgramMotionModifierDescriptor descriptor, string detail);
    }

    internal static class ProgramMotionModifierRuntime
    {
        public static void ApplyActionWarp<TTime, TAction, TChannel, TTarget>(
            ReadOnlySpan<ProgramMotionModifierDescriptor> descriptors,
            IReadOnlyList<MotionWarpSample<TTime, TAction>> samples,
            OperationHandle resolvedOwner,
            ref TChannel channel,
            TTarget target)
            where TTime : struct
            where TAction : struct
            where TChannel : struct
            where TTarget : IMotionModifierTarget<TTime, TAction, TChannel>
        {
            int selectedDescriptor = -1;
            int selectedSample = -1;
            for (int descriptorIndex = 0; descriptorIndex < descriptors.Length; descriptorIndex++)
            {
                ProgramMotionModifierDescriptor descriptor = descriptors[descriptorIndex];
                int sampleIndex = FindOnlySample<TTime, TAction, TChannel, TTarget>(samples, descriptor.Operation, target, descriptor);
                if (sampleIndex < 0)
                {
                    target.Reset(descriptor);
                    continue;
                }
                if (!descriptor.SourceMotionOperation.Equals(resolvedOwner))
                {
                    target.Reset(descriptor);
                    target.TraceSourceNotResolved(descriptor, resolvedOwner);
                    continue;
                }
                if (selectedDescriptor >= 0)
                {
                    target.Fail(
                        MotionModifierDiagnosticCode.AmbiguousModifier,
                        descriptor,
                        $"Action channel owner '{resolvedOwner}' has multiple active MotionWarp modifiers.");
                    return;
                }
                selectedDescriptor = descriptorIndex;
                selectedSample = sampleIndex;
            }
            if (selectedDescriptor >= 0)
                target.ApplyMotionWarp(descriptors[selectedDescriptor], samples[selectedSample], ref channel);
        }

        static int FindOnlySample<TTime, TAction, TChannel, TTarget>(
            IReadOnlyList<MotionWarpSample<TTime, TAction>> samples,
            OperationHandle operation,
            TTarget target,
            ProgramMotionModifierDescriptor descriptor)
            where TTime : struct
            where TAction : struct
            where TChannel : struct
            where TTarget : IMotionModifierTarget<TTime, TAction, TChannel>
        {
            int found = -1;
            for (int i = 0; i < samples.Count; i++)
            {
                if (!samples[i].Operation.Equals(operation))
                    continue;
                if (found >= 0)
                {
                    target.Fail(
                        MotionModifierDiagnosticCode.AmbiguousModifier,
                        descriptor,
                        $"MotionWarp operation '{operation}' produced multiple active samples in one logic Tick.");
                    return -1;
                }
                found = i;
            }
            return found;
        }
    }

    public readonly struct TimelinePresentationOutput<TTime>
        where TTime : struct
    {
        public TimelinePresentationOutput(
            OperationHandle operation,
            SimulationExecutionSource source,
            TimelinePresentationOutputKind kind,
            TTime sampleTime,
            TTime weight,
            ulong producerGeneration,
            int cycle,
            ulong sourceActionInstanceId,
            TTime visualTimeScale)
        {
            if (!operation.IsValid)
                throw new ArgumentException("Timeline presentation output requires a valid operation.", nameof(operation));
            if (!source.IsValid)
                throw new ArgumentException("Timeline presentation output source is invalid.", nameof(source));
            if (cycle < 0)
                throw new ArgumentOutOfRangeException(nameof(cycle));
            if (RequiresGeneration(kind) && producerGeneration == 0)
                throw new ArgumentOutOfRangeException(nameof(producerGeneration));
            Operation = operation;
            Source = source;
            Kind = kind;
            SampleTime = sampleTime;
            Weight = weight;
            ProducerGeneration = producerGeneration;
            Cycle = cycle;
            SourceActionInstanceId = sourceActionInstanceId;
            VisualTimeScale = visualTimeScale;
        }

        public OperationHandle Operation { get; }
        public SimulationExecutionSource Source { get; }
        public TimelinePresentationOutputKind Kind { get; }
        public TTime SampleTime { get; }
        public TTime Weight { get; }
        public ulong ProducerGeneration { get; }
        public int Cycle { get; }
        public ulong SourceActionInstanceId { get; }
        public TTime VisualTimeScale { get; }

        static bool RequiresGeneration(TimelinePresentationOutputKind kind) =>
            kind == TimelinePresentationOutputKind.SelectProducer ||
            kind == TimelinePresentationOutputKind.SampleProducer ||
            kind == TimelinePresentationOutputKind.CompleteProducer ||
            kind == TimelinePresentationOutputKind.ReleaseProducer ||
            kind == TimelinePresentationOutputKind.Camera ||
            kind == TimelinePresentationOutputKind.Cue ||
            kind == TimelinePresentationOutputKind.ForceProducer;
    }

    public readonly struct TimelineCueOutput<TTime>
        where TTime : struct
    {
        public TimelineCueOutput(
            OperationHandle operation,
            SimulationExecutionSource source,
            TTime sampleTime,
            int cycle,
            ulong producerGeneration,
            ulong sourceActionInstanceId)
        {
            if (!operation.IsValid)
                throw new ArgumentException("Timeline cue output requires a valid operation.", nameof(operation));
            if (!source.IsValid)
                throw new ArgumentException("Timeline cue output source is invalid.", nameof(source));
            if (producerGeneration == 0)
                throw new ArgumentOutOfRangeException(nameof(producerGeneration));
            if (cycle < 0)
                throw new ArgumentOutOfRangeException(nameof(cycle));
            Operation = operation;
            Source = source;
            SampleTime = sampleTime;
            Cycle = cycle;
            ProducerGeneration = producerGeneration;
            SourceActionInstanceId = sourceActionInstanceId;
        }

        public OperationHandle Operation { get; }
        public SimulationExecutionSource Source { get; }
        public TTime SampleTime { get; }
        public int Cycle { get; }
        public ulong ProducerGeneration { get; }
        public ulong SourceActionInstanceId { get; }
    }

    public readonly struct TimelineTraceOutput
    {
        public TimelineTraceOutput(
            OperationHandle operation,
            string code,
            TimelineTraceSeverity severity,
            string detail)
            : this(
                operation,
                OperationHandle.Invalid,
                code,
                severity,
                detail,
                0f,
                0,
                0,
                default)
        {
        }

        public TimelineTraceOutput(
            OperationHandle operation,
            OperationHandle timelineOperation,
            string code,
            TimelineTraceSeverity severity,
            string detail,
            float time,
            ulong playbackGeneration,
            int cycle,
            TimelineActionContextIdentity actionContext)
        {
            if (!operation.IsValid)
                throw new ArgumentException("Timeline trace output requires a valid operation.", nameof(operation));
            if (timelineOperation.IsValid && playbackGeneration == 0)
                throw new ArgumentOutOfRangeException(nameof(playbackGeneration));
            if (!float.IsFinite(time))
                throw new ArgumentOutOfRangeException(nameof(time));
            if (cycle < 0)
                throw new ArgumentOutOfRangeException(nameof(cycle));
            Operation = operation;
            TimelineOperation = timelineOperation;
            Code = SimulationIdentity.Require(code, nameof(code));
            Severity = severity;
            Detail = detail ?? string.Empty;
            Time = time;
            PlaybackGeneration = playbackGeneration;
            Cycle = cycle;
            ActionContext = actionContext;
        }

        public OperationHandle Operation { get; }
        public OperationHandle TimelineOperation { get; }
        public string Code { get; }
        public TimelineTraceSeverity Severity { get; }
        public string Detail { get; }
        public float Time { get; }
        public ulong PlaybackGeneration { get; }
        public int Cycle { get; }
        public TimelineActionContextIdentity ActionContext { get; }
    }

    public enum AbilityTimelinePlaybackMode : byte
    {
        Once = 0,
        Loop = 1
    }

    public enum AbilityTimelineRuntimeStatus : byte
    {
        Running = 1,
        Succeeded = 2,
        Failed = 3,
        Cancelled = 4
    }

    public readonly struct AbilityTimelineStartRequest
    {
        public AbilityTimelineStartRequest(
            string timelineId,
            bool loop,
            TimelineActionContextIdentity actionContext,
            AbilityTimelineInvocationSource invocationSource,
            ulong inputSequence,
            SimulationTick tick)
        {
            TimelineId = SimulationIdentity.Require(timelineId, nameof(timelineId));
            if (!actionContext.IsValid || !actionContext.HasSkillExecution || actionContext.SkillExecutionGeneration == 0)
                throw new ArgumentException("Ability Timeline Action context is incomplete.", nameof(actionContext));
            if (!invocationSource.IsValid)
                throw new ArgumentException("Ability Timeline invocation source is incomplete.", nameof(invocationSource));
            Loop = loop;
            InputSequence = inputSequence;
            ActionContext = actionContext;
            InvocationSource = invocationSource;
            Tick = tick;
            if (!tick.IsValid)
                throw new ArgumentOutOfRangeException(nameof(tick));
        }

        public string TimelineId { get; }
        public bool Loop { get; }
        public TimelineActionContextIdentity ActionContext { get; }
        public AbilityTimelineInvocationSource InvocationSource { get; }
        public SimulationTick Tick { get; }
        public ulong InputSequence { get; }
    }

    public readonly struct AbilityTimelinePlaybackControl : IEquatable<AbilityTimelinePlaybackControl>
    {
        public AbilityTimelinePlaybackControl(FixedScalar rate, bool paused)
        {
            if (rate < FixedScalar.Zero)
                throw new ArgumentOutOfRangeException(nameof(rate));
            Rate = rate;
            Paused = paused;
        }

        public FixedScalar Rate { get; }
        public bool Paused { get; }
        public bool IsPaused => Paused || Rate == FixedScalar.Zero;
        public static AbilityTimelinePlaybackControl Normal => new AbilityTimelinePlaybackControl(FixedScalar.One, false);
        public bool Equals(AbilityTimelinePlaybackControl other) => Rate == other.Rate && Paused == other.Paused;
        public override bool Equals(object obj) => obj is AbilityTimelinePlaybackControl other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Rate.Raw, Paused);
    }

    public enum AbilityTimelineProgressState : byte
    {
        Active = 0,
        Completed = 1,
        Stopped = 2
    }

    public readonly struct AbilityTimelineProgress : IEquatable<AbilityTimelineProgress>
    {
        public AbilityTimelineProgress(
            string timelineId,
            string contentRevision,
            ulong generation,
            ulong logicTick,
            FixedScalar duration,
            FixedScalar previousTime,
            FixedScalar time,
            int previousCycle,
            int cycle,
            bool loop,
            AbilityTimelineProgressState state,
            AbilityTimelinePlaybackControl control)
        {
            TimelineId = SimulationIdentity.Require(timelineId, nameof(timelineId));
            ContentRevision = SimulationIdentity.Require(contentRevision, nameof(contentRevision));
            if (generation == 0 || logicTick == 0)
                throw new ArgumentOutOfRangeException(nameof(generation));
            if (duration < FixedScalar.Zero || previousTime < FixedScalar.Zero ||
                previousTime > duration || time < FixedScalar.Zero || time > duration ||
                previousCycle < 0 || cycle < previousCycle ||
                cycle == previousCycle && time < previousTime ||
                !loop && (previousCycle != 0 || cycle != 0))
                throw new ArgumentException("Timeline progress must describe a forward interval within its content.");
            Generation = generation;
            LogicTick = logicTick;
            Duration = duration;
            PreviousTime = previousTime;
            Time = time;
            PreviousCycle = previousCycle;
            Cycle = cycle;
            Loop = loop;
            if (state > AbilityTimelineProgressState.Stopped)
                throw new ArgumentOutOfRangeException(nameof(state));
            State = state;
            Control = control;
        }

        public string TimelineId { get; }
        public string ContentRevision { get; }
        public ulong Generation { get; }
        public ulong LogicTick { get; }
        public FixedScalar Duration { get; }
        public FixedScalar PreviousTime { get; }
        public FixedScalar Time { get; }
        public int PreviousCycle { get; }
        public int Cycle { get; }
        public bool Loop { get; }
        public AbilityTimelineProgressState State { get; }
        public AbilityTimelinePlaybackControl Control { get; }
        public bool IsTerminal => State != AbilityTimelineProgressState.Active;
        public bool IsValid => Generation != 0;

        public bool Equals(AbilityTimelineProgress other) =>
            string.Equals(TimelineId, other.TimelineId, StringComparison.Ordinal) &&
            string.Equals(ContentRevision, other.ContentRevision, StringComparison.Ordinal) &&
            Generation == other.Generation && LogicTick == other.LogicTick &&
            Duration == other.Duration && PreviousTime == other.PreviousTime && Time == other.Time &&
            PreviousCycle == other.PreviousCycle && Cycle == other.Cycle &&
            Loop == other.Loop && State == other.State && Control.Equals(other.Control);

        public override bool Equals(object obj) => obj is AbilityTimelineProgress other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(TimelineId, ContentRevision, Generation, LogicTick, Time.Raw, Cycle);
    }

    public readonly struct AbilityTimelineAdvancePending
    {
        public AbilityTimelineAdvancePending(int runtimeHandle, ulong sequence, AbilityTimelineProgress progress)
        {
            if (runtimeHandle <= 0 || sequence == 0 || !progress.IsValid)
                throw new ArgumentException("Timeline pending advance requires its runtime handle, sequence and progress.");
            RuntimeHandle = runtimeHandle;
            Sequence = sequence;
            Progress = progress;
        }

        public int RuntimeHandle { get; }
        public ulong Sequence { get; }
        public AbilityTimelineProgress Progress { get; }
        public bool IsValid => RuntimeHandle > 0 && Sequence != 0;
    }

    public readonly struct AbilityTimelineTickResult
    {
        public AbilityTimelineTickResult(AbilityTimelineRuntimeStatus status, AbilityTimelineAdvancePending pending)
        {
            Status = status;
            Pending = pending;
            if (status == AbilityTimelineRuntimeStatus.Running && (!pending.IsValid || !pending.Progress.IsValid))
                throw new ArgumentException("A running Ability Timeline advance requires a pending commit candidate.");
        }

        public AbilityTimelineRuntimeStatus Status { get; }
        public AbilityTimelineAdvancePending Pending { get; }
        public AbilityTimelineProgress Progress => Pending.Progress;
    }

    public readonly struct AbilityTimelineStopPending
    {
        public AbilityTimelineStopPending(int runtimeHandle, ulong sequence, AbilityTimelineProgress progress)
        {
            if (runtimeHandle <= 0 || sequence == 0)
                throw new ArgumentException("Timeline stop candidate requires its runtime handle and sequence.");
            RuntimeHandle = runtimeHandle;
            Sequence = sequence;
            Progress = progress;
        }

        public int RuntimeHandle { get; }
        public ulong Sequence { get; }
        public AbilityTimelineProgress Progress { get; }
        public bool IsValid => RuntimeHandle > 0 && Sequence != 0;
    }

    public readonly struct AbilityTimelineStopResult
    {
        public AbilityTimelineStopResult(AbilityTimelineRuntimeStatus status, AbilityTimelineStopPending pending, ulong sourceActionInstanceId)
        {
            Status = status;
            Pending = pending;
            SourceActionInstanceId = sourceActionInstanceId;
            if (status == AbilityTimelineRuntimeStatus.Running && !pending.IsValid)
                throw new ArgumentException("A running Ability Timeline stop requires a pending commit candidate.");
        }

        public AbilityTimelineRuntimeStatus Status { get; }
        public AbilityTimelineStopPending Pending { get; }
        public ulong SourceActionInstanceId { get; }
        public AbilityTimelineProgress Progress => Pending.Progress;
    }
    public enum AbilityTimelineSnapshotMode : byte
    {
        Once = 0,
        Loop = 1
    }

    public enum AbilityTimelineSnapshotState : byte
    {
        Prepared = 0,
        Running = 1,
        Stopping = 2,
        Completed = 3,
        Stopped = 4,
        Failed = 5,
        Disposed = 6
    }

    public enum AbilityTimelineSnapshotStopCause : byte
    {
        None = 0,
        SelfAbort = 1,
        LowerPriorityAbort = 2,
        ExplicitParentStop = 3,
        StateTransition = 4,
        Reset = 5,
        Shutdown = 6
    }

    public readonly struct AbilityTimelineTreeClipState
    {
        public AbilityTimelineTreeClipState(string clipAuthoringId, string treeGraphId, int cycle)
        {
            ClipAuthoringId = SimulationIdentity.Require(clipAuthoringId, nameof(clipAuthoringId));
            TreeGraphId = SimulationIdentity.Require(treeGraphId, nameof(treeGraphId));
            if (cycle < 0)
                throw new ArgumentOutOfRangeException(nameof(cycle));
            Cycle = cycle;
        }

        public string ClipAuthoringId { get; }
        public string TreeGraphId { get; }
        public int Cycle { get; }
    }

    public readonly struct AbilityTimelineRuntimeSnapshot
    {
        public bool IsValid => RuntimeHandle != 0 && Generation != 0;
        public AbilityTimelineRuntimeSnapshot(
            int runtimeHandle,
            ulong generation,
            string requestId,
            string ownerIdentity,
            string callIdentity,
            ulong executionInstanceId,
            AbilityTimelineSnapshotMode playbackMode,
            string contentRevision,
            AbilityTimelineSnapshotState state,
            FixedScalar cursorTime,
            int cycle,
            int timeCarry,
            AbilityTimelinePlaybackControl control,
            TimelineSnapshotItems<string> treeDecisionExits,
            TimelineSnapshotItems<string> pendingTreeDecisionExits,
            string sectionId,
            TimelineSnapshotItems<string> activeClipIds,
            TimelineSnapshotItems<AbilityTimelineTreeClipState> activeTreeClips,
            bool hasStopContext,
            AbilityTimelineSnapshotStopCause stopCause,
            ulong stopLocalLogicTick,
            bool initialBoundaryPending,
            string timelineId,
            bool loop,
            TimelineActionContextIdentity actionContext,
            AbilityTimelineInvocationSource invocationSource,
            ulong inputSequence,
            SimulationTick startTick)
        {
            if (runtimeHandle == 0)
                throw new ArgumentOutOfRangeException(nameof(runtimeHandle));
            if (generation == 0)
                throw new ArgumentOutOfRangeException(nameof(generation));
            RequestId = SimulationIdentity.Require(requestId, nameof(requestId));
            OwnerIdentity = SimulationIdentity.Require(ownerIdentity, nameof(ownerIdentity));
            CallIdentity = SimulationIdentity.Require(callIdentity, nameof(callIdentity));
            if (executionInstanceId == 0)
                throw new ArgumentOutOfRangeException(nameof(executionInstanceId));
            if ((byte)playbackMode > (byte)AbilityTimelineSnapshotMode.Loop)
                throw new ArgumentOutOfRangeException(nameof(playbackMode));
            ContentRevision = SimulationIdentity.Require(contentRevision, nameof(contentRevision));
            if ((byte)state > (byte)AbilityTimelineSnapshotState.Disposed)
                throw new ArgumentOutOfRangeException(nameof(state));
            if (cursorTime < FixedScalar.Zero || cycle < 0)
                throw new ArgumentOutOfRangeException(nameof(cursorTime));
            TimeCarry = timeCarry;
            Control = control;
            TreeDecisionExits = treeDecisionExits;
            PendingTreeDecisionExits = pendingTreeDecisionExits;
            SectionId = sectionId ?? string.Empty;
            ActiveClipIds = activeClipIds;
            for (int index = 0; index < activeTreeClips.Count; index++)
            {
                AbilityTimelineTreeClipState clip = activeTreeClips[index];
                if (string.IsNullOrWhiteSpace(clip.ClipAuthoringId) || string.IsNullOrWhiteSpace(clip.TreeGraphId) ||
                    clip.Cycle < 0 || clip.Cycle > cycle)
                    throw new ArgumentException("Timeline active TreeClip identity or cycle is invalid.", nameof(activeTreeClips));
            }
            ActiveTreeClips = activeTreeClips;
            StopCause = stopCause;
            StopLocalLogicTick = stopLocalLogicTick;
            if (hasStopContext && StopCause == AbilityTimelineSnapshotStopCause.None)
                throw new ArgumentException("A Timeline stop context requires a stop cause.", nameof(stopCause));
            TimelineId = SimulationIdentity.Require(timelineId, nameof(timelineId));
            if (!actionContext.IsValid || !actionContext.HasSkillExecution || actionContext.SkillExecutionGeneration == 0)
                throw new ArgumentException("Ability Timeline snapshot Action context is incomplete.", nameof(actionContext));
            if (!invocationSource.IsValid)
                throw new ArgumentException("Ability Timeline snapshot invocation source is incomplete.", nameof(invocationSource));
            ActionContext = actionContext;
            InvocationSource = invocationSource;
            StartTick = startTick;
            if (!startTick.IsValid)
                throw new ArgumentException("Ability Timeline snapshot start tick is invalid.", nameof(startTick));
            RuntimeHandle = runtimeHandle;
            Generation = generation;
            ExecutionInstanceId = executionInstanceId;
            PlaybackMode = playbackMode;
            State = state;
            CursorTime = cursorTime;
            Cycle = cycle;
            HasStopContext = hasStopContext;
            InitialBoundaryPending = initialBoundaryPending;
            Loop = loop;
            InputSequence = inputSequence;
        }

        public int RuntimeHandle { get; }
        public ulong Generation { get; }
        public string RequestId { get; }
        public string OwnerIdentity { get; }
        public string CallIdentity { get; }
        public ulong ExecutionInstanceId { get; }
        public AbilityTimelineSnapshotMode PlaybackMode { get; }
        public string ContentRevision { get; }
        public AbilityTimelineSnapshotState State { get; }
        public FixedScalar CursorTime { get; }
        public int Cycle { get; }
        public int TimeCarry { get; }
        public AbilityTimelinePlaybackControl Control { get; }
        public TimelineSnapshotItems<string> TreeDecisionExits { get; }
        public TimelineSnapshotItems<string> PendingTreeDecisionExits { get; }
        public string SectionId { get; }
        public TimelineSnapshotItems<string> ActiveClipIds { get; }
        public TimelineSnapshotItems<AbilityTimelineTreeClipState> ActiveTreeClips { get; }
        public bool HasStopContext { get; }
        public AbilityTimelineSnapshotStopCause StopCause { get; }
        public ulong StopLocalLogicTick { get; }
        public bool InitialBoundaryPending { get; }
        public string TimelineId { get; }
        public bool Loop { get; }
        public TimelineActionContextIdentity ActionContext { get; }
        public AbilityTimelineInvocationSource InvocationSource { get; }
        public ulong InputSequence { get; }
        public SimulationTick StartTick { get; }


    }
    public enum AbilityTreeClipHook : byte
    {
        OnEnable = 0,
        OnDisable = 1,
        OnDestroy = 2,
        Root = 3
    }

    public readonly struct AbilityTreeClipInvocation
    {
        public AbilityTreeClipInvocation(
            string clipAuthoringId,
            string treeGraphId,
            AbilityTreeClipHook hook,
            int cycle,
            ulong actionInstanceId,
            int timelineRuntimeHandle)
        {
            ClipAuthoringId = SimulationIdentity.Require(clipAuthoringId, nameof(clipAuthoringId));
            TreeGraphId = SimulationIdentity.Require(treeGraphId, nameof(treeGraphId));
            if ((byte)hook > (byte)AbilityTreeClipHook.Root)
                throw new ArgumentOutOfRangeException(nameof(hook));
            if (cycle < 0)
                throw new ArgumentOutOfRangeException(nameof(cycle));
            if (actionInstanceId == 0)
                throw new ArgumentOutOfRangeException(nameof(actionInstanceId));
            if (timelineRuntimeHandle == 0)
                throw new ArgumentOutOfRangeException(nameof(timelineRuntimeHandle));
            Hook = hook;
            Cycle = cycle;
            ActionInstanceId = actionInstanceId;
            TimelineRuntimeHandle = timelineRuntimeHandle;
        }

        public string ClipAuthoringId { get; }
        public string TreeGraphId { get; }
        public AbilityTreeClipHook Hook { get; }
        public int Cycle { get; }
        public ulong ActionInstanceId { get; }
        public int TimelineRuntimeHandle { get; }
    }

    public interface IAbilityTreeClipInvoker
    {
        bool InvokeTreeClip(in AbilityTreeClipInvocation invocation);
    }

    public interface IAbilityTreeClipInvokerHost
    {
        void PushTreeClipInvoker(IAbilityTreeClipInvoker invoker);
        void PopTreeClipInvoker();
    }

    public interface IAbilityTimelineRuntime
    {
        int Start(in AbilityTimelineStartRequest request);
        AbilityTimelineTickResult Tick(int runtimeHandle, ulong logicTick, int tickCount, AbilityTimelinePlaybackControl control);
        void Commit(AbilityTimelineAdvancePending pending);
        void Discard(AbilityTimelineAdvancePending pending);
        AbilityTimelineRuntimeSnapshot Capture(int runtimeHandle);
        int ApplyRestore(AbilityTimelineRuntimeSnapshot snapshot);
        void DiscardUnpublishedPlaybacks(IReadOnlyList<AbilityTimelineRuntimeSnapshot> snapshots);
        void ReleaseUnreferencedPlaybacks(IReadOnlyList<AbilityTimelineRuntimeSnapshot> snapshots, ulong committedTick);
        AbilityTimelineStopResult Stop(int runtimeHandle, ulong logicTick);
        void CommitStop(AbilityTimelineStopPending pending);
        void DiscardStop(AbilityTimelineStopPending pending);
        bool RequestTreeClipExit(int runtimeHandle, string clipAuthoringId);
    }


}

