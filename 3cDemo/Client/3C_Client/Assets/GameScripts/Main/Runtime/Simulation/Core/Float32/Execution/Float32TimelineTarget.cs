using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    internal sealed class Float32TimelineControlStatePort : ITimelineControlStatePort
    {
        readonly Float32ProgramAccess m_Access;
        readonly Float32StatePort m_State;

        public Float32TimelineControlStatePort(Float32ProgramAccess access, Float32StatePort state)
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
            Float32ActionInstanceReference value = m_State
                .Get(Require(operation, ProgramStateSemantic.TimelineRetentionIdentity))
                .ActionInstanceReference;
            return value.IsValid
                ? new TimelineActionContextIdentity(value.ActionId, value.ContextId, value.InstanceId, value.PredictionKey)
                : default;
        }

        public void WriteRetainedActionContext(OperationHandle operation, TimelineActionContextIdentity identity)
        {
            Float32ActionInstanceReference value = identity.IsValid
                ? new Float32ActionInstanceReference(identity.ActionId, identity.ContextId, identity.InstanceId, identity.PredictionKey)
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

    internal sealed class Float32TimelineTargetLeaf : Float32OperationModule, ITimelineTargetLeaf<Float32Scalar>
    {
        readonly Float32StatePort m_State;
        readonly Float32EvaluationFrame m_Frame;
        readonly IFloat32ActionContextReader m_Actions;
        readonly IFloat32ActivationReader m_Activations;
        readonly IFloat32BlackboardPort m_Blackboard;
        readonly IFloat32MotionContributionSink m_Motion;
        readonly IFloat32MotionModifierSampleSink m_MotionModifiers;
        readonly Float32FactSink m_Facts;
        readonly Float32PresentationSink m_Presentation;
        readonly Float32TraceSink m_Trace;

        public Float32TimelineTargetLeaf(
            Float32ProgramAccess access,
            Float32StatePort state,
            Float32EvaluationFrame frame,
            IFloat32ActionContextReader actions,
            IFloat32ActivationReader activations,
            IFloat32BlackboardPort blackboard,
            IFloat32MotionContributionSink motion,
            IFloat32MotionModifierSampleSink motionModifiers,
            Float32FactSink facts,
            Float32PresentationSink presentation,
            Float32TraceSink trace)
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

        public Float32Scalar Zero => Float32Scalar.Zero;
        public bool DiagnosticsEnabled => m_Trace.Enabled;
        public Float32Scalar One => Float32Scalar.One;
        public Float32Scalar TickDelta => Float32Scalar.One / Float32Scalar.FromInt64(m_Program.Manifest.TickRate);
        public Float32Scalar Epsilon => Float32Scalar.FromSingle(0.000001f);
        public int TimelineOperationCount => Access.Topology.TimelineOperationCount;
        public int Compare(Float32Scalar left, Float32Scalar right) => left.CompareTo(right);
        public Float32Scalar FromInt32(int value) => Float32Scalar.FromInt64(value);
        public Float32Scalar Add(Float32Scalar left, Float32Scalar right) => left + right;
        public Float32Scalar Subtract(Float32Scalar left, Float32Scalar right) => left - right;
        public Float32Scalar Multiply(Float32Scalar left, Float32Scalar right) => left * right;
        public Float32Scalar Divide(Float32Scalar left, Float32Scalar right) => left / right;
        public Float32Scalar Min(Float32Scalar left, Float32Scalar right) => Float32Scalar.Min(left, right);
        public Float32Scalar Max(Float32Scalar left, Float32Scalar right) => Float32Scalar.Max(left, right);
        public Float32Scalar Clamp(Float32Scalar value, Float32Scalar minimum, Float32Scalar maximum) =>
            Float32Scalar.Clamp(value, minimum, maximum);
        public string Format(Float32Scalar value) => value.ToString();
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

        public Float32Scalar TimelineDuration(OperationHandle operation)
        {
            ProgramCatalogEntry timeline = RequireCatalog(Access.Operation(operation), ProgramCatalogEntryKind.Timeline);
            int maxFrame = CatalogInt32(timeline, ProgramCatalogFieldId.MaxFrame);
            int frameRate = CatalogInt32(timeline, ProgramCatalogFieldId.FrameRate);
            if (maxFrame < 0 || frameRate <= 0)
                throw new InvalidOperationException($"Timeline catalog '{timeline.Identity}' has invalid frame range.");
            return Float32Scalar.FromInt64(maxFrame) / Float32Scalar.FromInt64(frameRate);
        }

        public Float32Scalar ClipTime(OperationHandle operation, TimelineClipTimePoint point)
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
            return Float32Scalar.FromInt64(frame) / Float32Scalar.FromInt64(frameRate);
        }

        public Float32Scalar ClipScalar(OperationHandle operation, TimelineClipScalarValue value)
        {
            ProgramCatalogFieldId field = value switch
            {
                TimelineClipScalarValue.Intensity => ProgramCatalogFieldId.Intensity,
                _ => throw new ArgumentOutOfRangeException(nameof(value))
            };
            return CatalogScalar(RequireClipCatalog(Access.Operation(operation)), field);
        }

        public Float32Scalar SampleCurve(
            OperationHandle operation,
            TimelineCurveChannel channel,
            Float32Scalar time,
            Float32Scalar fallback)
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
            return curve.Evaluate(Float32Scalar.Clamp(time, Float32Scalar.Zero, Float32Scalar.One), fallback);
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

        public Float32Scalar ReadLogicTime(OperationHandle operation) =>
            m_State.Get(Access.RequireOperationSlot(operation, ProgramStateSemantic.TimelineLogicTime)).Scalar;

        public void WriteLogicTime(OperationHandle operation, Float32Scalar value) =>
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
            if (m_Actions.FindActive(contextId, out Float32ActionInstanceState action) < 0)
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

        public void SampleMotionCurve(OperationHandle timeline, OperationHandle operation, TimelineSegment<Float32Scalar> segment)
        {
            Float32Scalar start = ClipTime(operation, TimelineClipTimePoint.Start);
            Float32Scalar end = ClipTime(operation, TimelineClipTimePoint.End);
            if (segment.Current <= start || segment.Previous >= end)
                return;
            Float32Scalar duration = Float32Scalar.Max(Epsilon, end - start);
            Float32Scalar previousSelf = Float32Scalar.Clamp(segment.Previous - start, Float32Scalar.Zero, duration);
            Float32Scalar self = Float32Scalar.Clamp(segment.Current - start, Float32Scalar.Zero, duration);
            if (previousSelf == self)
                return;
            Float32Scalar curveEnd = ClipTime(operation, TimelineClipTimePoint.CurveEnd);
            Float32Scalar curveDuration = Float32Scalar.Max(Epsilon, curveEnd - start);
            Float32Scalar previousCurveTime = Float32Scalar.Clamp(segment.Previous - start, Float32Scalar.Zero, curveDuration);
            Float32Scalar currentCurveTime = Float32Scalar.Clamp(segment.Current - start, Float32Scalar.Zero, curveDuration);
            Float32Scalar previousCurve = previousCurveTime / curveDuration;
            Float32Scalar currentCurve = currentCurveTime / curveDuration;
            Float32Scalar normalized = Float32Scalar.Clamp(self / duration, Float32Scalar.Zero, Float32Scalar.One);
            Float32Scalar weight = SampleClipWeight(
                operation,
                normalized,
                self,
                Float32Scalar.Max(Float32Scalar.Zero, end - segment.Current));
            if (weight <= Float32Scalar.Zero)
                return;
            Float32Vector3 previousPosition = new Float32Vector3(
                SampleCurve(operation, TimelineCurveChannel.PositionX, previousCurve, Float32Scalar.Zero),
                SampleCurve(operation, TimelineCurveChannel.PositionY, previousCurve, Float32Scalar.Zero),
                SampleCurve(operation, TimelineCurveChannel.PositionZ, previousCurve, Float32Scalar.Zero));
            Float32Vector3 currentPosition = new Float32Vector3(
                SampleCurve(operation, TimelineCurveChannel.PositionX, currentCurve, Float32Scalar.Zero),
                SampleCurve(operation, TimelineCurveChannel.PositionY, currentCurve, Float32Scalar.Zero),
                SampleCurve(operation, TimelineCurveChannel.PositionZ, currentCurve, Float32Scalar.Zero));
            Float32Scalar yaw = SampleCurve(operation, TimelineCurveChannel.Yaw, currentCurve, Float32Scalar.Zero) -
                                SampleCurve(operation, TimelineCurveChannel.Yaw, previousCurve, Float32Scalar.Zero);
            ProgramCatalogEntry definition = RequireClipCatalog(Access.Operation(operation));
            var channel = (SimulationMotionChannel)CatalogInt32(definition, ProgramCatalogFieldId.Channel);
            CommittedMovementPlaybackClock movementPlaybackClock = channel == SimulationMotionChannel.Locomotion
                ? new CommittedMovementPlaybackClock(
                    SourcePath(timeline),
                    ReadActivationGeneration(timeline),
                    m_Frame.Tick,
                    checked((int)Math.Round(
                        (Float32Scalar.FromInt64(segment.Cycle) * TimelineDuration(timeline) + segment.Current).ToDouble() * m_Program.Manifest.TickRate,
                        MidpointRounding.AwayFromZero)),
                    m_Program.Manifest.TickRate)
                : default;
            var contribution = new SimulationMotionContribution(
                SourcePath(operation),
                operation,
                currentPosition - previousPosition,
                yaw,
                Float32Vector2.Zero,
                CatalogInt32(definition, ProgramCatalogFieldId.Space) == 1
                    ? SimulationMotionContributionSpace.World
                    : SimulationMotionContributionSpace.ActorLocal,
                weight,
                CatalogInt32(definition, ProgramCatalogFieldId.Priority),
                channel,
                (SimulationMotionBlendMode)CatalogInt32(definition, ProgramCatalogFieldId.BlendMode),
                CatalogBoolean(definition, ProgramCatalogFieldId.ConsumeLowerChannels),
                movementPlaybackClock);
            if (contribution.CanResolve)
                m_Motion.Submit(contribution);
        }

        public void SampleMotionWarp(
            OperationHandle operation,
            TimelineSegment<Float32Scalar> segment,
            TimelineActionContextIdentity actionContext)
        {
            Float32Scalar start = ClipTime(operation, TimelineClipTimePoint.Start);
            Float32Scalar end = ClipTime(operation, TimelineClipTimePoint.End);
            if (segment.Current <= start || segment.Previous >= end)
                return;
            Float32Scalar previous = Float32Scalar.Clamp(segment.Previous, start, end);
            Float32Scalar current = Float32Scalar.Clamp(segment.Current, start, end);
            if (previous == current)
                return;
            OperationHandle timeline = TimelineOwner(operation);
            Float32ActionInstanceState action = RequireAction(actionContext);
            if (!action.IsActive)
                throw new InvalidOperationException($"MotionWarp '{SourcePath(operation)}' lost its sampled Action instance.");
            ulong playbackGeneration = MotionWarpRuntimeSemantics.ComposePlaybackGeneration(
                ReadActivationGeneration(timeline),
                segment.Cycle);
            m_MotionModifiers.Submit(new MotionWarpSample<Float32Scalar, Float32ActionInstanceState>(
                operation,
                new TimelineSegment<Float32Scalar>(previous, current, segment.Cycle, segment.StartsCycle),
                playbackGeneration,
                actionContext,
                action));
        }

        public void EmitPresentation(TimelinePresentationOutput<Float32Scalar> output)
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

        public void EmitCue(TimelineCueOutput<Float32Scalar> output)
        {
            SimulationOperation operation = Access.Operation(output.Operation);
            ProgramCatalogEntry definition = RequireClipCatalog(operation);
            string cueId = CatalogString(definition, ProgramCatalogFieldId.CueId);
            string cueType = CatalogString(definition, ProgramCatalogFieldId.CueType);
            SimulationEventHeader factHeader = m_Facts.Next(operation);
            m_Facts.Add(new GameplayFact(factHeader, new GameplayCueFact(cueId, cueType, definition.Identity, 0)));
            EmitPresentation(new TimelinePresentationOutput<Float32Scalar>(
                output.Operation,
                TimelinePresentationOutputKind.Cue,
                output.SampleTime,
                Float32Scalar.One,
                0,
                output.Cycle,
                0,
                Float32Scalar.Zero));
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

        Float32ActionInstanceState RequireAction(TimelineActionContextIdentity identity)
        {
            return m_Actions.RequireActive(new Float32ActionInstanceReference(
                identity.ActionId,
                identity.ContextId,
                identity.InstanceId,
                identity.PredictionKey));
        }

        Float32Scalar SampleClipWeight(
            OperationHandle operation,
            Float32Scalar normalized,
            Float32Scalar selfTime,
            Float32Scalar remainTime)
        {
            Float32Scalar easeIn = ClipTime(operation, TimelineClipTimePoint.EaseIn);
            Float32Scalar easeOut = ClipTime(operation, TimelineClipTimePoint.EaseOut);
            Float32Scalar fadeIn = Float32Scalar.One;
            if (easeIn > Float32Scalar.Zero && selfTime < easeIn)
                fadeIn = SampleCurve(operation, TimelineCurveChannel.EaseIn, selfTime / easeIn, Float32Scalar.One);
            Float32Scalar fadeOut = Float32Scalar.One;
            if (easeOut > Float32Scalar.Zero && remainTime < easeOut)
            {
                fadeOut = Float32Scalar.One - SampleCurve(
                    operation,
                    TimelineCurveChannel.EaseOut,
                    Float32Scalar.One - remainTime / easeOut,
                    Float32Scalar.Zero);
            }
            return Float32Scalar.Clamp(
                SampleCurve(operation, TimelineCurveChannel.Weight, normalized, Float32Scalar.One) * fadeIn * fadeOut,
                Float32Scalar.Zero,
                Float32Scalar.One);
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
