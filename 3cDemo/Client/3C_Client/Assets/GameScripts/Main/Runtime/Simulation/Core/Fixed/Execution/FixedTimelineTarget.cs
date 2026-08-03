using ThirdPersonSimulation;
using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedTimelineControlStatePort : ITimelineControlStatePort
    {
        readonly FixedProgramAccess m_Access;
        readonly FixedStatePort m_State;

        public FixedTimelineControlStatePort(FixedProgramAccess access, FixedStatePort state)
        {
            m_Access = access ?? throw new ArgumentNullException(nameof(access));
            m_State = state ?? throw new ArgumentNullException(nameof(state));
        }

        public TimelinePlaybackStatus ReadPlayback(OperationHandle operation) =>
            (TimelinePlaybackStatus)m_State.Get(Require(operation, ProgramStateSemantic.TimelinePlayback)).Int32;

        public bool TryReadPlayback(OperationHandle operation, out TimelinePlaybackStatus status)
        {
            int slot = Find(operation, ProgramStateSemantic.TimelinePlayback);
            status = slot < 0 ? TimelinePlaybackStatus.Dormant : (TimelinePlaybackStatus)m_State.Get(slot).Int32;
            return slot >= 0;
        }

        public void WritePlayback(OperationHandle operation, TimelinePlaybackStatus status) =>
            m_State.Set(Require(operation, ProgramStateSemantic.TimelinePlayback), CharacterStateValue.FromInt32((int)status));

        public TimelineTreeClipStatus ReadTreeClipStatus(OperationHandle operation) =>
            (TimelineTreeClipStatus)m_State.Get(Require(operation, ProgramStateSemantic.TimelinePlayback)).Int32;

        public bool TryReadTreeClipStatus(OperationHandle operation, out TimelineTreeClipStatus status)
        {
            int slot = Find(operation, ProgramStateSemantic.TimelinePlayback);
            status = slot < 0 ? TimelineTreeClipStatus.Dormant : (TimelineTreeClipStatus)m_State.Get(slot).Int32;
            return slot >= 0;
        }

        public void WriteTreeClipStatus(OperationHandle operation, TimelineTreeClipStatus status) =>
            m_State.Set(Require(operation, ProgramStateSemantic.TimelinePlayback), CharacterStateValue.FromInt32((int)status));

        public bool ReadLoop(OperationHandle operation) =>
            m_State.Get(Require(operation, ProgramStateSemantic.TimelineLoop)).Boolean;

        public void WriteLoop(OperationHandle operation, bool loop) =>
            m_State.Set(Require(operation, ProgramStateSemantic.TimelineLoop), CharacterStateValue.FromBoolean(loop));

        public int ReadCycle(OperationHandle operation) =>
            m_State.Get(Require(operation, ProgramStateSemantic.TimelineTreeClipCycle)).Int32;

        public bool TryReadCycle(OperationHandle operation, out int cycle)
        {
            int slot = Find(operation, ProgramStateSemantic.TimelineTreeClipCycle);
            cycle = slot < 0 ? 0 : m_State.Get(slot).Int32;
            return slot >= 0;
        }

        public void WriteCycle(OperationHandle operation, int cycle)
        {
            if (cycle < 0)
                throw new ArgumentOutOfRangeException(nameof(cycle));
            m_State.Set(Require(operation, ProgramStateSemantic.TimelineTreeClipCycle), CharacterStateValue.FromInt32(cycle));
        }

        public TimelineActionContextIdentity ReadRetainedActionContext(OperationHandle operation)
        {
            FixedActionInstanceReference value = m_State
                .Get(Require(operation, ProgramStateSemantic.TimelineRetentionIdentity))
                .ActionInstanceReference;
            return value.IsValid
                ? new TimelineActionContextIdentity(value.ActionId, value.ContextId, value.InstanceId, value.PredictionKey)
                : default;
        }

        public void WriteRetainedActionContext(OperationHandle operation, TimelineActionContextIdentity identity)
        {
            FixedActionInstanceReference value = identity.IsValid
                ? new FixedActionInstanceReference(identity.ActionId, identity.ContextId, identity.InstanceId, identity.PredictionKey)
                : default;
            m_State.Set(
                Require(operation, ProgramStateSemantic.TimelineRetentionIdentity),
                CharacterStateValue.FromActionInstanceReference(value));
        }

        int Find(OperationHandle operation, ProgramStateSemantic semantic) =>
            m_Access.FindOperationSlot(operation, semantic);

        int Require(OperationHandle operation, ProgramStateSemantic semantic) =>
            m_Access.RequireOperationSlot(operation, semantic);
    }

    internal sealed class FixedTimelineTargetLeaf : FixedOperationModule, ITimelineTargetLeaf<FixedScalar>
    {
        readonly FixedStatePort m_State;
        readonly FixedEvaluationFrame m_Frame;
        readonly IFixedActionContextReader m_Actions;
        readonly IFixedActivationReader m_Activations;
        readonly IFixedBlackboardPort m_Blackboard;
        readonly IFixedMotionContributionSink m_Motion;
        readonly IFixedMotionModifierSampleSink m_MotionModifiers;
        readonly FixedFactSink m_Facts;
        readonly FixedPresentationSink m_Presentation;
        readonly FixedTraceSink m_Trace;

        public FixedTimelineTargetLeaf(
            FixedProgramAccess access,
            FixedStatePort state,
            FixedEvaluationFrame frame,
            IFixedActionContextReader actions,
            IFixedActivationReader activations,
            IFixedBlackboardPort blackboard,
            IFixedMotionContributionSink motion,
            IFixedMotionModifierSampleSink motionModifiers,
            FixedFactSink facts,
            FixedPresentationSink presentation,
            FixedTraceSink trace)
            : base(access)
        {
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            m_Actions = actions ?? throw new ArgumentNullException(nameof(actions));
            m_Activations = activations ?? throw new ArgumentNullException(nameof(activations));
            m_Blackboard = blackboard ?? throw new ArgumentNullException(nameof(blackboard));
            m_Motion = motion ?? throw new ArgumentNullException(nameof(motion));
            m_MotionModifiers = motionModifiers ?? throw new ArgumentNullException(nameof(motionModifiers));
            m_Facts = facts ?? throw new ArgumentNullException(nameof(facts));
            m_Presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            m_Trace = trace ?? throw new ArgumentNullException(nameof(trace));
        }

        public FixedScalar Zero => FixedScalar.Zero;
        public bool DiagnosticsEnabled => m_Trace.Enabled;
        public FixedScalar One => FixedScalar.One;
        public FixedScalar TickDelta => FixedScalar.One / FixedScalar.FromInt64(m_Program.Manifest.TickRate);
        public FixedScalar Epsilon => FixedScalar.FromRatio(1, 1000000);
        public int TimelineOperationCount => Access.Topology.TimelineOperationCount;
        public int Compare(FixedScalar left, FixedScalar right) => left.CompareTo(right);
        public FixedScalar FromInt32(int value) => FixedScalar.FromInt64(value);
        public FixedScalar Add(FixedScalar left, FixedScalar right) => left + right;
        public FixedScalar Subtract(FixedScalar left, FixedScalar right) => left - right;
        public FixedScalar Multiply(FixedScalar left, FixedScalar right) => left * right;
        public FixedScalar Divide(FixedScalar left, FixedScalar right) => left / right;
        public FixedScalar Min(FixedScalar left, FixedScalar right) => FixedScalar.Min(left, right);
        public FixedScalar Max(FixedScalar left, FixedScalar right) => FixedScalar.Max(left, right);
        public FixedScalar Clamp(FixedScalar value, FixedScalar minimum, FixedScalar maximum) =>
            FixedScalar.Clamp(value, minimum, maximum);
        public string Format(FixedScalar value) => value.ToString();
        public OperationExecutionDescriptor TimelineOperationAt(int index) => Access.Topology.TimelineOperationAt(index);
        public OperationHandle TimelineOwner(OperationHandle child) => Access.Topology.TimelineOwner(child);
        public OperationExecutionDescriptor Operation(OperationHandle operation) => Access.Topology.Operation(operation);
        public new IReadOnlyList<ProgramControlFlowEdge> Edges(OperationHandle source, ProgramControlFlowKind kind) =>
            Access.Edges(source, kind);
        public string SourcePath(OperationHandle operation) => Access.SourcePath(Access.Operation(operation));
        public bool IsLoop(OperationHandle operation) => Access.Operation(operation).Integer0 == 1;

        public bool IsTrackMuted(OperationHandle operation)
        {
            ProgramCatalogEntry clip = RequireClipCatalog(Access.Operation(operation));
            string trackIdentity = CatalogIdentity(clip, ProgramCatalogFieldId.Track);
            ProgramCatalogEntry track = RequireCatalog(ProgramCatalogEntryKind.TimelineTrack, trackIdentity);
            return CatalogBoolean(track, ProgramCatalogFieldId.Muted);
        }

        public IReadOnlyList<OperationHandle> AnimationProducerRepresentatives(OperationHandle timeline) =>
            m_Layout.TimelineAnimationRepresentatives(timeline);

        public FixedScalar TimelineDuration(OperationHandle operation)
        {
            ProgramCatalogEntry timeline = RequireCatalog(Access.Operation(operation), ProgramCatalogEntryKind.Timeline);
            int maxFrame = CatalogInt32(timeline, ProgramCatalogFieldId.MaxFrame);
            int frameRate = CatalogInt32(timeline, ProgramCatalogFieldId.FrameRate);
            if (maxFrame < 0 || frameRate <= 0)
                throw new InvalidOperationException($"Timeline catalog '{timeline.Identity}' has invalid frame range.");
            return FixedScalar.FromInt64(maxFrame) / FixedScalar.FromInt64(frameRate);
        }

        public FixedScalar ClipTime(OperationHandle operation, TimelineClipTimePoint point)
        {
            ProgramCatalogFieldId field = point switch
            {
                TimelineClipTimePoint.Start => ProgramCatalogFieldId.StartFrame,
                TimelineClipTimePoint.End => ProgramCatalogFieldId.EndFrame,
                TimelineClipTimePoint.CurveEnd => ProgramCatalogFieldId.CurveEndFrame,
                TimelineClipTimePoint.EaseIn => ProgramCatalogFieldId.EaseInFrame,
                TimelineClipTimePoint.EaseOut => ProgramCatalogFieldId.EaseOutFrame,
                _ => throw new ArgumentOutOfRangeException(nameof(point))
            };
            ProgramCatalogEntry clip = RequireClipCatalog(Access.Operation(operation));
            int frame = CatalogInt32(clip, field);
            string trackIdentity = CatalogIdentity(clip, ProgramCatalogFieldId.Track);
            ProgramCatalogEntry track = RequireCatalog(ProgramCatalogEntryKind.TimelineTrack, trackIdentity);
            string timelineIdentity = CatalogIdentity(track, ProgramCatalogFieldId.Timeline);
            ProgramCatalogEntry timeline = RequireCatalog(ProgramCatalogEntryKind.Timeline, timelineIdentity);
            int frameRate = CatalogInt32(timeline, ProgramCatalogFieldId.FrameRate);
            if (frameRate <= 0)
                throw new InvalidOperationException($"Timeline '{timeline.Identity}' has invalid FrameRate '{frameRate}'.");
            return FixedScalar.FromInt64(frame) / FixedScalar.FromInt64(frameRate);
        }

        public FixedScalar ClipScalar(OperationHandle operation, TimelineClipScalarValue value)
        {
            ProgramCatalogFieldId field = value switch
            {
                TimelineClipScalarValue.Intensity => ProgramCatalogFieldId.Intensity,
                _ => throw new ArgumentOutOfRangeException(nameof(value))
            };
            return CatalogScalar(RequireClipCatalog(Access.Operation(operation)), field);
        }

        public FixedScalar SampleCurve(
            OperationHandle operation,
            TimelineCurveChannel channel,
            FixedScalar time,
            FixedScalar fallback)
        {
            ProgramCatalogFieldId field = channel switch
            {
                TimelineCurveChannel.Weight => ProgramCatalogFieldId.WeightCurve,
                TimelineCurveChannel.EaseIn => ProgramCatalogFieldId.EaseInCurve,
                TimelineCurveChannel.EaseOut => ProgramCatalogFieldId.EaseOutCurve,
                TimelineCurveChannel.PositionX => ProgramCatalogFieldId.PositionX,
                TimelineCurveChannel.PositionY => ProgramCatalogFieldId.PositionY,
                TimelineCurveChannel.PositionZ => ProgramCatalogFieldId.PositionZ,
                TimelineCurveChannel.Yaw => ProgramCatalogFieldId.Yaw,
                _ => throw new ArgumentOutOfRangeException(nameof(channel))
            };
            ProgramCatalogEntry entry = RequireClipCatalog(Access.Operation(operation));
            ProgramConstant constant = CatalogConstant(entry, field);
            if (constant.Kind != ProgramConstantKind.Bytes)
                throw new InvalidOperationException($"Catalog curve '{entry.Identity}/{field}' is not Bytes.");
            ProgramCurve curve = Access.Services.RequireTimelineCurve(constant, $"{entry.Identity}/{field}");
            return curve.Evaluate(FixedScalar.Clamp(time, FixedScalar.Zero, FixedScalar.One), fallback);
        }

        public ProgramControlFlowEdge TreeClipEdge(OperationHandle operation, TimelineTreeClipEdgeKind kind)
        {
            ProgramControlFlowKind flow = kind == TimelineTreeClipEdgeKind.Root || kind == TimelineTreeClipEdgeKind.Enable
                ? ProgramControlFlowKind.Enter
                : ProgramControlFlowKind.Exit;
            string port = kind switch
            {
                TimelineTreeClipEdgeKind.Root => "TreeClip",
                TimelineTreeClipEdgeKind.Enable => "OnEnable",
                TimelineTreeClipEdgeKind.Disable => "OnDisable",
                TimelineTreeClipEdgeKind.Destroy => "OnDestroy",
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
            ProgramControlFlowEdge match = null;
            IReadOnlyList<ProgramControlFlowEdge> edges = Access.Edges(operation, flow);
            for (int i = 0; i < edges.Count; i++)
            {
                if (!string.Equals(edges[i].SourcePort, port, StringComparison.Ordinal))
                    continue;
                if (match != null)
                    throw new InvalidOperationException($"TreeClip '{SourcePath(operation)}' has duplicate '{port}' lifecycle edges.");
                match = edges[i];
            }
            return match ?? throw new InvalidOperationException($"TreeClip '{SourcePath(operation)}' has no compiled '{port}' lifecycle edge.");
        }

        public FixedScalar ReadLogicTime(OperationHandle operation) =>
            m_State.Get(Access.RequireOperationSlot(operation, ProgramStateSemantic.TimelineLogicTime)).Scalar;

        public void WriteLogicTime(OperationHandle operation, FixedScalar value) =>
            m_State.Set(
                Access.RequireOperationSlot(operation, ProgramStateSemantic.TimelineLogicTime),
                CharacterStateValue.FromScalar(value));

        public string ProducerIdentity(OperationHandle operation) => RequireProducer(Access.Operation(operation)).Identity;
        public ulong ReadActivationGeneration(OperationHandle operation) => m_Activations.ReadGeneration(operation);

        public bool TryCaptureActionContext(OperationHandle operation, out TimelineActionContextIdentity identity)
        {
            string contextId = Access.GetStringConstant(Access.Operation(operation), OperationNamedConstant.ActionContext, string.Empty);
            if (string.IsNullOrEmpty(contextId))
            {
                identity = default;
                return true;
            }
            if (m_Actions.FindActive(contextId, out FixedActionInstanceState action) < 0)
            {
                identity = default;
                return false;
            }
            identity = new TimelineActionContextIdentity(action.ActionId, action.ContextId, action.InstanceId, action.PredictionKey);
            return true;
        }

        public bool IsActionContextCurrent(OperationHandle operation, TimelineActionContextIdentity identity)
        {
            string contextId = Access.GetStringConstant(Access.Operation(operation), OperationNamedConstant.ActionContext, string.Empty);
            if (string.IsNullOrEmpty(contextId))
                return true;
            if (!identity.IsValid || !string.Equals(identity.ContextId, contextId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Running Timeline '{SourcePath(operation)}' has no valid retained Action Context '{contextId}'.");
            }
            return RequireAction(identity).IsActive;
        }

        public IDisposable PushTimelineContext(
            OperationHandle timeline,
            OperationHandle clip,
            int cycle,
            TimelineActionContextIdentity identity)
        {
            return m_Blackboard.PushTimelineContext(
                Access.Operation(timeline),
                Access.Operation(clip),
                cycle,
                identity.IsValid ? RequireAction(identity) : default);
        }

        public void ResetTreeClipState(OperationHandle operation)
        {
            SimulationOperation clip = Access.Operation(operation);
            for (int i = 0; i < clip.StateSlots.Count; i++)
            {
                int slotIndex = clip.StateSlots[i];
                ProgramStateSlot slot = m_Program.StateSlots[slotIndex];
                CharacterStateValue value = slot.DefaultConstantIndex >= 0
                    ? CharacterStateValue.FromConstant(m_Program.Constants[slot.DefaultConstantIndex], slot.ValueKind)
                    : CharacterStateValue.Default(slot.ValueKind);
                m_State.Set(slotIndex, value);
            }
        }

        public void SampleMotionCurve(OperationHandle operation, TimelineSegment<FixedScalar> segment)
        {
            FixedScalar start = ClipTime(operation, TimelineClipTimePoint.Start);
            FixedScalar end = ClipTime(operation, TimelineClipTimePoint.End);
            if (segment.Current <= start || segment.Previous >= end)
                return;
            FixedScalar duration = FixedScalar.Max(Epsilon, end - start);
            FixedScalar previousSelf = FixedScalar.Clamp(segment.Previous - start, FixedScalar.Zero, duration);
            FixedScalar self = FixedScalar.Clamp(segment.Current - start, FixedScalar.Zero, duration);
            if (previousSelf == self)
                return;
            FixedScalar curveEnd = ClipTime(operation, TimelineClipTimePoint.CurveEnd);
            FixedScalar curveDuration = FixedScalar.Max(Epsilon, curveEnd - start);
            FixedScalar previousCurveTime = FixedScalar.Clamp(segment.Previous - start, FixedScalar.Zero, curveDuration);
            FixedScalar currentCurveTime = FixedScalar.Clamp(segment.Current - start, FixedScalar.Zero, curveDuration);
            FixedScalar previousCurve = previousCurveTime / curveDuration;
            FixedScalar currentCurve = currentCurveTime / curveDuration;
            FixedScalar normalized = FixedScalar.Clamp(self / duration, FixedScalar.Zero, FixedScalar.One);
            FixedScalar weight = SampleClipWeight(
                operation,
                normalized,
                self,
                FixedScalar.Max(FixedScalar.Zero, end - segment.Current));
            if (weight <= FixedScalar.Zero)
                return;
            FixedVector3 previousPosition = new FixedVector3(
                SampleCurve(operation, TimelineCurveChannel.PositionX, previousCurve, FixedScalar.Zero),
                SampleCurve(operation, TimelineCurveChannel.PositionY, previousCurve, FixedScalar.Zero),
                SampleCurve(operation, TimelineCurveChannel.PositionZ, previousCurve, FixedScalar.Zero));
            FixedVector3 currentPosition = new FixedVector3(
                SampleCurve(operation, TimelineCurveChannel.PositionX, currentCurve, FixedScalar.Zero),
                SampleCurve(operation, TimelineCurveChannel.PositionY, currentCurve, FixedScalar.Zero),
                SampleCurve(operation, TimelineCurveChannel.PositionZ, currentCurve, FixedScalar.Zero));
            FixedScalar yaw = SampleCurve(operation, TimelineCurveChannel.Yaw, currentCurve, FixedScalar.Zero) -
                                SampleCurve(operation, TimelineCurveChannel.Yaw, previousCurve, FixedScalar.Zero);
            ProgramCatalogEntry definition = RequireClipCatalog(Access.Operation(operation));
            var contribution = new SimulationMotionContribution(
                SourcePath(operation),
                operation,
                currentPosition - previousPosition,
                yaw,
                CatalogInt32(definition, ProgramCatalogFieldId.Space) == 1
                    ? SimulationMotionContributionSpace.World
                    : SimulationMotionContributionSpace.ActorLocal,
                weight,
                CatalogInt32(definition, ProgramCatalogFieldId.Priority),
                (SimulationMotionChannel)CatalogInt32(definition, ProgramCatalogFieldId.Channel),
                (SimulationMotionBlendMode)CatalogInt32(definition, ProgramCatalogFieldId.BlendMode),
                CatalogBoolean(definition, ProgramCatalogFieldId.ConsumeLowerChannels));
            if (contribution.CanResolve)
                m_Motion.Submit(contribution);
        }

        public void SampleMotionWarp(
            OperationHandle operation,
            TimelineSegment<FixedScalar> segment,
            TimelineActionContextIdentity actionContext)
        {
            FixedScalar start = ClipTime(operation, TimelineClipTimePoint.Start);
            FixedScalar end = ClipTime(operation, TimelineClipTimePoint.End);
            if (segment.Current <= start || segment.Previous >= end)
                return;
            FixedScalar previous = FixedScalar.Clamp(segment.Previous, start, end);
            FixedScalar current = FixedScalar.Clamp(segment.Current, start, end);
            if (previous == current)
                return;
            OperationHandle timeline = TimelineOwner(operation);
            FixedActionInstanceState action = RequireAction(actionContext);
            if (!action.IsActive)
                throw new InvalidOperationException($"MotionWarp '{SourcePath(operation)}' lost its sampled Action instance.");
            ulong playbackGeneration = MotionWarpRuntimeSemantics.ComposePlaybackGeneration(
                ReadActivationGeneration(timeline),
                segment.Cycle);
            m_MotionModifiers.Submit(new MotionWarpSample<FixedScalar, FixedActionInstanceState>(
                operation,
                new TimelineSegment<FixedScalar>(previous, current, segment.Cycle, segment.StartsCycle),
                playbackGeneration,
                actionContext,
                action));
        }

        public void EmitPresentation(TimelinePresentationOutput<FixedScalar> output)
        {
            SimulationOperation operation = Access.Operation(output.Operation);
            PresentationCommandKind kind = output.Kind switch
            {
                TimelinePresentationOutputKind.SelectProducer => PresentationCommandKind.SelectProducer,
                TimelinePresentationOutputKind.SampleProducer => PresentationCommandKind.SampleProducer,
                TimelinePresentationOutputKind.CompleteProducer => PresentationCommandKind.CompleteProducer,
                TimelinePresentationOutputKind.ReleaseProducer => PresentationCommandKind.ReleaseProducer,
                TimelinePresentationOutputKind.Camera => PresentationCommandKind.Camera,
                TimelinePresentationOutputKind.Cue => PresentationCommandKind.Cue,
                _ => throw new ArgumentOutOfRangeException(nameof(output))
            };
            SimulationEventHeader header = m_Presentation.Next(operation);
            m_Presentation.Add(new PresentationCommand(
                header,
                kind,
                ProducerIdentity(output.Operation),
                output.SampleTime,
                output.Weight,
                output.ProducerGeneration,
                output.Cycle,
                output.SourceActionInstanceId,
                output.VisualTimeScale));
        }

        public void EmitCue(TimelineCueOutput<FixedScalar> output)
        {
            SimulationOperation operation = Access.Operation(output.Operation);
            ProgramCatalogEntry definition = RequireClipCatalog(operation);
            string cueId = CatalogString(definition, ProgramCatalogFieldId.CueId);
            string cueType = CatalogString(definition, ProgramCatalogFieldId.CueType);
            SimulationEventHeader factHeader = m_Facts.Next(operation);
            m_Facts.Add(new GameplayFact(factHeader, new GameplayCueFact(cueId, cueType, definition.Identity, 0)));
            EmitPresentation(new TimelinePresentationOutput<FixedScalar>(
                output.Operation,
                TimelinePresentationOutputKind.Cue,
                output.SampleTime,
                FixedScalar.One,
                0,
                output.Cycle,
                0,
                FixedScalar.Zero));
        }

        public void EmitTrace(TimelineTraceOutput output)
        {
            SimulationTraceSeverity severity = output.Severity switch
            {
                TimelineTraceSeverity.Detail => SimulationTraceSeverity.Detail,
                TimelineTraceSeverity.Information => SimulationTraceSeverity.Information,
                TimelineTraceSeverity.Warning => SimulationTraceSeverity.Warning,
                TimelineTraceSeverity.Error => SimulationTraceSeverity.Error,
                _ => throw new ArgumentOutOfRangeException(nameof(output))
            };
            m_Trace.Add(Access.Operation(output.Operation), output.Code, severity, output.Detail);
        }

        FixedActionInstanceState RequireAction(TimelineActionContextIdentity identity)
        {
            return m_Actions.RequireActive(new FixedActionInstanceReference(
                identity.ActionId,
                identity.ContextId,
                identity.InstanceId,
                identity.PredictionKey));
        }

        FixedScalar SampleClipWeight(
            OperationHandle operation,
            FixedScalar normalized,
            FixedScalar selfTime,
            FixedScalar remainTime)
        {
            FixedScalar easeIn = ClipTime(operation, TimelineClipTimePoint.EaseIn);
            FixedScalar easeOut = ClipTime(operation, TimelineClipTimePoint.EaseOut);
            FixedScalar fadeIn = FixedScalar.One;
            if (easeIn > FixedScalar.Zero && selfTime < easeIn)
                fadeIn = SampleCurve(operation, TimelineCurveChannel.EaseIn, selfTime / easeIn, FixedScalar.One);
            FixedScalar fadeOut = FixedScalar.One;
            if (easeOut > FixedScalar.Zero && remainTime < easeOut)
            {
                fadeOut = FixedScalar.One - SampleCurve(
                    operation,
                    TimelineCurveChannel.EaseOut,
                    FixedScalar.One - remainTime / easeOut,
                    FixedScalar.Zero);
            }
            return FixedScalar.Clamp(
                SampleCurve(operation, TimelineCurveChannel.Weight, normalized, FixedScalar.One) * fadeIn * fadeOut,
                FixedScalar.Zero,
                FixedScalar.One);
        }

        ProgramCatalogEntry RequireClipCatalog(SimulationOperation operation)
        {
            return FindCatalog(operation, ProgramCatalogEntryKind.TimelineClip) ??
                   FindCatalog(operation, ProgramCatalogEntryKind.MotionCurve) ??
                   throw new InvalidOperationException($"Timeline clip operation '{SourcePath(operation.Handle)}' has no clip catalog reference.");
        }

        ProgramProducer RequireProducer(SimulationOperation operation)
        {
            ProgramProducer producer = null;
            IReadOnlyList<ProgramReference> references = References(operation.Handle, ProgramReferenceKind.Producer);
            for (int i = 0; i < references.Count; i++)
            {
                if (producer != null)
                    throw new InvalidOperationException($"Timeline producer operation '{SourcePath(operation.Handle)}' has multiple producer references.");
                producer = m_Program.Producers[references[i].TargetIndex];
            }
            return producer ?? throw new InvalidOperationException($"Timeline producer operation '{SourcePath(operation.Handle)}' has no producer reference.");
        }
    }
}
