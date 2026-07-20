using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    internal enum TimelinePlaybackStatus : byte
    {
        Dormant = 0,
        Running = 2,
        Succeeded = 3,
        Stopping = 4,
        Cancelled = 5,
        Completing = 6
    }

    internal enum TimelineTreeClipStatus : byte
    {
        Dormant = 0,
        Enabling = 1,
        Active = 2,
        Disabling = 3,
        StoppingRoot = 4,
        Destroying = 5
    }

    internal enum TimelineClipTimePoint : byte
    {
        Start = 1,
        End = 2,
        CurveEnd = 3,
        EaseIn = 4,
        EaseOut = 5
    }

    internal enum TimelineCurveChannel : byte
    {
        Weight = 1,
        EaseIn = 2,
        EaseOut = 3,
        PositionX = 4,
        PositionY = 5,
        PositionZ = 6,
        Yaw = 7
    }

    internal enum TimelineClipScalarValue : byte
    {
        Intensity = 1
    }

    internal enum TimelineTreeClipEdgeKind : byte
    {
        Root = 1,
        Enable = 2,
        Disable = 3,
        Destroy = 4
    }

    internal enum TimelinePresentationOutputKind : byte
    {
        SelectProducer = 1,
        SampleProducer = 2,
        CompleteProducer = 3,
        ReleaseProducer = 4,
        Camera = 5,
        Cue = 6
    }

    internal enum TimelineTraceSeverity : byte
    {
        Detail = 1,
        Information = 2,
        Warning = 3,
        Error = 4
    }

    internal readonly struct TimelineActionContextIdentity : IEquatable<TimelineActionContextIdentity>
    {
        public TimelineActionContextIdentity(
            string actionId,
            string contextId,
            ulong instanceId,
            ulong predictionKey)
        {
            ActionId = actionId ?? string.Empty;
            ContextId = contextId ?? string.Empty;
            InstanceId = instanceId;
            PredictionKey = predictionKey;
        }

        public string ActionId { get; }
        public string ContextId { get; }
        public ulong InstanceId { get; }
        public ulong PredictionKey { get; }
        public bool IsValid =>
            !string.IsNullOrEmpty(ActionId) &&
            !string.IsNullOrEmpty(ContextId) &&
            InstanceId != 0 &&
            PredictionKey != 0;

        public bool Equals(TimelineActionContextIdentity other) =>
            string.Equals(ActionId, other.ActionId, StringComparison.Ordinal) &&
            string.Equals(ContextId, other.ContextId, StringComparison.Ordinal) &&
            InstanceId == other.InstanceId &&
            PredictionKey == other.PredictionKey;

        public override int GetHashCode() => HashCode.Combine(ActionId, ContextId, InstanceId, PredictionKey);
    }

    internal readonly struct TimelineSegment<TTime>
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

    internal readonly struct MotionWarpSample<TTime>
        where TTime : struct
    {
        public MotionWarpSample(
            OperationHandle operation,
            TimelineSegment<TTime> segment,
            ulong playbackGeneration)
        {
            if (!operation.IsValid)
                throw new ArgumentException("MotionWarp sample requires a valid operation.", nameof(operation));
            if (playbackGeneration == 0)
                throw new ArgumentOutOfRangeException(nameof(playbackGeneration));
            Operation = operation;
            Segment = segment;
            PlaybackGeneration = playbackGeneration;
        }

        public OperationHandle Operation { get; }
        public TimelineSegment<TTime> Segment { get; }
        public ulong PlaybackGeneration { get; }
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

    internal interface IMotionModifierTarget<TTime, TChannel>
        where TTime : struct
        where TChannel : struct
    {
        void Reset(ProgramMotionModifierDescriptor descriptor);
        void TraceSourceNotResolved(ProgramMotionModifierDescriptor descriptor, OperationHandle resolvedOwner);
        void ApplyMotionWarp(
            ProgramMotionModifierDescriptor descriptor,
            MotionWarpSample<TTime> sample,
            ref TChannel channel);
        void Fail(string code, ProgramMotionModifierDescriptor descriptor, string detail);
    }

    internal static class ProgramMotionModifierRuntime
    {
        public static void ApplyActionWarp<TTime, TChannel, TTarget>(
            ReadOnlySpan<ProgramMotionModifierDescriptor> descriptors,
            IReadOnlyList<MotionWarpSample<TTime>> samples,
            OperationHandle resolvedOwner,
            ref TChannel channel,
            TTarget target)
            where TTime : struct
            where TChannel : struct
            where TTarget : IMotionModifierTarget<TTime, TChannel>
        {
            int selectedDescriptor = -1;
            int selectedSample = -1;
            for (int descriptorIndex = 0; descriptorIndex < descriptors.Length; descriptorIndex++)
            {
                ProgramMotionModifierDescriptor descriptor = descriptors[descriptorIndex];
                int sampleIndex = FindOnlySample<TTime, TChannel, TTarget>(samples, descriptor.Operation, target, descriptor);
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

        static int FindOnlySample<TTime, TChannel, TTarget>(
            IReadOnlyList<MotionWarpSample<TTime>> samples,
            OperationHandle operation,
            TTarget target,
            ProgramMotionModifierDescriptor descriptor)
            where TTime : struct
            where TChannel : struct
            where TTarget : IMotionModifierTarget<TTime, TChannel>
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

    internal readonly struct TimelinePresentationOutput<TTime>
        where TTime : struct
    {
        public TimelinePresentationOutput(
            OperationHandle operation,
            TimelinePresentationOutputKind kind,
            TTime sampleTime,
            TTime weight,
            ulong producerGeneration,
            int cycle)
        {
            if (!operation.IsValid)
                throw new ArgumentException("Timeline presentation output requires a valid operation.", nameof(operation));
            if (cycle < 0)
                throw new ArgumentOutOfRangeException(nameof(cycle));
            if (RequiresGeneration(kind) && producerGeneration == 0)
                throw new ArgumentOutOfRangeException(nameof(producerGeneration));
            Operation = operation;
            Kind = kind;
            SampleTime = sampleTime;
            Weight = weight;
            ProducerGeneration = producerGeneration;
            Cycle = cycle;
        }

        public OperationHandle Operation { get; }
        public TimelinePresentationOutputKind Kind { get; }
        public TTime SampleTime { get; }
        public TTime Weight { get; }
        public ulong ProducerGeneration { get; }
        public int Cycle { get; }

        static bool RequiresGeneration(TimelinePresentationOutputKind kind) =>
            kind == TimelinePresentationOutputKind.SelectProducer ||
            kind == TimelinePresentationOutputKind.SampleProducer ||
            kind == TimelinePresentationOutputKind.CompleteProducer ||
            kind == TimelinePresentationOutputKind.ReleaseProducer;
    }

    internal readonly struct TimelineCueOutput<TTime>
        where TTime : struct
    {
        public TimelineCueOutput(OperationHandle operation, TTime sampleTime, int cycle)
        {
            if (!operation.IsValid)
                throw new ArgumentException("Timeline cue output requires a valid operation.", nameof(operation));
            if (cycle < 0)
                throw new ArgumentOutOfRangeException(nameof(cycle));
            Operation = operation;
            SampleTime = sampleTime;
            Cycle = cycle;
        }

        public OperationHandle Operation { get; }
        public TTime SampleTime { get; }
        public int Cycle { get; }
    }

    internal readonly struct TimelineTraceOutput
    {
        public TimelineTraceOutput(
            OperationHandle operation,
            string code,
            TimelineTraceSeverity severity,
            string detail)
        {
            if (!operation.IsValid)
                throw new ArgumentException("Timeline trace output requires a valid operation.", nameof(operation));
            Operation = operation;
            Code = SimulationIdentity.Require(code, nameof(code));
            Severity = severity;
            Detail = detail ?? string.Empty;
        }

        public OperationHandle Operation { get; }
        public string Code { get; }
        public TimelineTraceSeverity Severity { get; }
        public string Detail { get; }
    }

    internal interface ITimelineControlStatePort
    {
        TimelinePlaybackStatus ReadPlayback(OperationHandle operation);
        bool TryReadPlayback(OperationHandle operation, out TimelinePlaybackStatus status);
        void WritePlayback(OperationHandle operation, TimelinePlaybackStatus status);
        TimelineTreeClipStatus ReadTreeClipStatus(OperationHandle operation);
        bool TryReadTreeClipStatus(OperationHandle operation, out TimelineTreeClipStatus status);
        void WriteTreeClipStatus(OperationHandle operation, TimelineTreeClipStatus status);
        bool ReadLoop(OperationHandle operation);
        void WriteLoop(OperationHandle operation, bool loop);
        int ReadCycle(OperationHandle operation);
        bool TryReadCycle(OperationHandle operation, out int cycle);
        void WriteCycle(OperationHandle operation, int cycle);
        TimelineActionContextIdentity ReadRetainedActionContext(OperationHandle operation);
        void WriteRetainedActionContext(OperationHandle operation, TimelineActionContextIdentity identity);
    }

    internal interface ITimelineTargetLeaf<TTime>
        where TTime : struct
    {
        bool DiagnosticsEnabled { get; }
        TTime Zero { get; }
        TTime One { get; }
        TTime TickDelta { get; }
        TTime Epsilon { get; }
        int TimelineOperationCount { get; }
        int Compare(TTime left, TTime right);
        TTime FromInt32(int value);
        TTime Add(TTime left, TTime right);
        TTime Subtract(TTime left, TTime right);
        TTime Multiply(TTime left, TTime right);
        TTime Divide(TTime left, TTime right);
        TTime Min(TTime left, TTime right);
        TTime Max(TTime left, TTime right);
        TTime Clamp(TTime value, TTime minimum, TTime maximum);
        string Format(TTime value);
        OperationExecutionDescriptor TimelineOperationAt(int index);
        OperationHandle TimelineOwner(OperationHandle child);
        OperationExecutionDescriptor Operation(OperationHandle operation);
        IReadOnlyList<ProgramControlFlowEdge> Edges(OperationHandle source, ProgramControlFlowKind kind);
        string SourcePath(OperationHandle operation);
        bool IsLoop(OperationHandle operation);
        bool IsTrackMuted(OperationHandle operation);
        IReadOnlyList<OperationHandle> AnimationProducerRepresentatives(OperationHandle timeline);
        TTime TimelineDuration(OperationHandle operation);
        TTime ClipTime(OperationHandle operation, TimelineClipTimePoint point);
        TTime ClipScalar(OperationHandle operation, TimelineClipScalarValue value);
        TTime SampleCurve(OperationHandle operation, TimelineCurveChannel channel, TTime time, TTime fallback);
        ProgramControlFlowEdge TreeClipEdge(OperationHandle operation, TimelineTreeClipEdgeKind kind);
        TTime ReadLogicTime(OperationHandle operation);
        void WriteLogicTime(OperationHandle operation, TTime value);
        ulong ReadActivationGeneration(OperationHandle operation);
        bool TryCaptureActionContext(OperationHandle operation, out TimelineActionContextIdentity identity);
        bool IsActionContextCurrent(OperationHandle operation, TimelineActionContextIdentity identity);
        IDisposable PushTimelineContext(
            OperationHandle timeline,
            OperationHandle clip,
            int cycle,
            TimelineActionContextIdentity identity);
        void ResetTreeClipState(OperationHandle operation);
        void SampleMotionCurve(OperationHandle operation, TimelineSegment<TTime> segment);
        void SampleMotionWarp(OperationHandle operation, TimelineSegment<TTime> segment);
        void EmitPresentation(TimelinePresentationOutput<TTime> output);
        void EmitCue(TimelineCueOutput<TTime> output);
        void EmitTrace(TimelineTraceOutput output);
    }
}
