using System;
using System.Collections.Generic;

namespace BTSMTL.Diagnostics
{
    public enum RuntimeDiagnosticsInterestKind
    {
        LiveState,
        Capture
    }

    public enum RuntimeDiagnosticsCaptureDetail
    {
        None,
        Boundary,
        Evaluation,
        Continuous
    }

    public readonly struct RuntimeDiagnosticsInterest
    {
        public RuntimeDiagnosticsInterest(
            RuntimeDiagnosticsInterestKind kind,
            RuntimeTraceChannel channels,
            RuntimeDiagnosticsCaptureDetail captureDetail = RuntimeDiagnosticsCaptureDetail.None)
        {
            Kind = kind;
            Channels = channels & RuntimeTraceChannel.All;
            CaptureDetail = kind == RuntimeDiagnosticsInterestKind.Capture
                ? captureDetail
                : RuntimeDiagnosticsCaptureDetail.None;
        }

        public RuntimeDiagnosticsInterestKind Kind { get; }
        public RuntimeTraceChannel Channels { get; }
        public RuntimeDiagnosticsCaptureDetail CaptureDetail { get; }
        public bool IsValid => Channels != RuntimeTraceChannel.None &&
                               (Kind != RuntimeDiagnosticsInterestKind.Capture || CaptureDetail != RuntimeDiagnosticsCaptureDetail.None);
    }

    public readonly struct RuntimeDiagnosticsInterestHandle : IEquatable<RuntimeDiagnosticsInterestHandle>
    {
        internal RuntimeDiagnosticsInterestHandle(Guid value)
        {
            Value = value;
        }

        internal Guid Value { get; }
        public bool IsValid => Value != Guid.Empty;
        public bool Equals(RuntimeDiagnosticsInterestHandle other) => Value.Equals(other.Value);
        public override bool Equals(object obj) => obj is RuntimeDiagnosticsInterestHandle other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
    }

    public readonly struct RuntimeLiveStateKey : IEquatable<RuntimeLiveStateKey>
    {
        public RuntimeLiveStateKey(RuntimeTraceChannel channel, RuntimeSourceElementHandle source, RuntimeInstanceKey instance, RuntimeTraceEventKind kind)
        {
            Channel = channel;
            Source = source;
            Instance = instance;
            Kind = kind;
        }

        public RuntimeTraceChannel Channel { get; }
        public RuntimeSourceElementHandle Source { get; }
        public RuntimeInstanceKey Instance { get; }
        public RuntimeTraceEventKind Kind { get; }

        public bool Equals(RuntimeLiveStateKey other)
        {
            return Channel == other.Channel && Source.Equals(other.Source) && Instance.Equals(other.Instance) && Kind == other.Kind;
        }

        public override bool Equals(object obj) => obj is RuntimeLiveStateKey other && Equals(other);
        public override int GetHashCode() => (((int)Channel * 397) ^ Source.GetHashCode()) * 397 ^ Instance.GetHashCode() ^ (int)Kind;
    }

    public readonly struct RuntimeLiveStateChange
    {
        public RuntimeLiveStateChange(long revision, RuntimeLiveStateKey key, RuntimeTraceEvent traceEvent)
        {
            Revision = revision;
            Key = key;
            TraceEvent = traceEvent;
        }

        public long Revision { get; }
        public RuntimeLiveStateKey Key { get; }
        public RuntimeTraceEvent TraceEvent { get; }
    }

    public readonly struct RuntimeLiveStateRead
    {
        public RuntimeLiveStateRead(long version, bool requiresFullSync, IReadOnlyList<RuntimeLiveStateChange> changes)
        {
            Version = version;
            RequiresFullSync = requiresFullSync;
            Changes = changes ?? Array.Empty<RuntimeLiveStateChange>();
        }

        public long Version { get; }
        public bool RequiresFullSync { get; }
        public IReadOnlyList<RuntimeLiveStateChange> Changes { get; }
    }

    public readonly struct RuntimeCaptureChange
    {
        public RuntimeCaptureChange(long revision, RuntimeTraceEvent traceEvent)
        {
            Revision = revision;
            TraceEvent = traceEvent;
        }

        public long Revision { get; }
        public RuntimeTraceEvent TraceEvent { get; }
    }

    public readonly struct RuntimeCaptureRead
    {
        public RuntimeCaptureRead(long version, bool requiresFullSync, IReadOnlyList<RuntimeCaptureChange> changes)
        {
            Version = version;
            RequiresFullSync = requiresFullSync;
            Changes = changes ?? Array.Empty<RuntimeCaptureChange>();
        }

        public long Version { get; }
        public bool RequiresFullSync { get; }
        public IReadOnlyList<RuntimeCaptureChange> Changes { get; }
    }

    public sealed class RuntimeCaptureSegmentSnapshot
    {
        public RuntimeCaptureSegmentSnapshot(RuntimeTraceDomain domain, ulong position, IReadOnlyList<RuntimeTraceEvent> events)
        {
            Domain = domain;
            Position = position;
            Events = events ?? Array.Empty<RuntimeTraceEvent>();
        }

        public RuntimeTraceDomain Domain { get; }
        public ulong Position { get; }
        public IReadOnlyList<RuntimeTraceEvent> Events { get; }
    }

    public sealed class RuntimeCaptureSnapshot
    {
        readonly IReadOnlyList<RuntimeCaptureSegmentSnapshot> m_Segments;

        public RuntimeCaptureSnapshot(Guid captureId, RuntimeTraceChannel channels, RuntimeDiagnosticsCaptureDetail detail, IReadOnlyList<RuntimeCaptureSegmentSnapshot> segments, long version)
        {
            CaptureId = captureId;
            Channels = channels & RuntimeTraceChannel.All;
            Detail = detail;
            m_Segments = segments ?? Array.Empty<RuntimeCaptureSegmentSnapshot>();
            Version = version;
        }

        public Guid CaptureId { get; }
        public RuntimeTraceChannel Channels { get; }
        public RuntimeDiagnosticsCaptureDetail Detail { get; }
        public long Version { get; }
        public IReadOnlyList<RuntimeCaptureSegmentSnapshot> Segments => m_Segments;
        public int SegmentCount => m_Segments.Count;

        public IReadOnlyList<RuntimeTraceEvent> GetEvents(int historyOffset)
        {
            int visibleSegments = Math.Max(0, m_Segments.Count - Math.Max(0, historyOffset));
            var events = new List<RuntimeTraceEvent>();
            for (int i = 0; i < visibleSegments; i++)
            {
                IReadOnlyList<RuntimeTraceEvent> segmentEvents = m_Segments[i].Events;
                for (int j = 0; j < segmentEvents.Count; j++)
                    events.Add(segmentEvents[j]);
            }
            return events;
        }
    }

    sealed class RuntimeLiveStateStore
    {
        readonly Dictionary<RuntimeLiveStateKey, RuntimeTraceEvent> m_Current = new Dictionary<RuntimeLiveStateKey, RuntimeTraceEvent>();
        readonly List<RuntimeLiveStateChange> m_Changes = new List<RuntimeLiveStateChange>();
        readonly int m_MaxChanges;
        long m_Version;

        public RuntimeLiveStateStore(int maxChanges = 4096)
        {
            if (maxChanges < 64)
                throw new ArgumentOutOfRangeException(nameof(maxChanges));
            m_MaxChanges = maxChanges;
        }

        public long Version => m_Version;

        public bool Upsert(RuntimeTraceEvent traceEvent)
        {
            var key = new RuntimeLiveStateKey(traceEvent.Channel, traceEvent.Source, traceEvent.RuntimeInstance, traceEvent.Kind);
            if (m_Current.TryGetValue(key, out RuntimeTraceEvent current) && StateEquivalent(current, traceEvent))
                return false;

            m_Version++;
            m_Current[key] = traceEvent;
            m_Changes.Add(new RuntimeLiveStateChange(m_Version, key, traceEvent));
            if (m_Changes.Count > m_MaxChanges)
                m_Changes.RemoveRange(0, m_Changes.Count - m_MaxChanges);
            return true;
        }

        public RuntimeLiveStateRead ReadSince(long cursor)
        {
            if (cursor >= m_Version)
                return new RuntimeLiveStateRead(m_Version, false, Array.Empty<RuntimeLiveStateChange>());

            long earliestAvailable = m_Changes.Count > 0 ? m_Changes[0].Revision : m_Version + 1;
            if (cursor < earliestAvailable - 1)
            {
                var current = new List<RuntimeLiveStateChange>(m_Current.Count);
                foreach (KeyValuePair<RuntimeLiveStateKey, RuntimeTraceEvent> pair in m_Current)
                    current.Add(new RuntimeLiveStateChange(m_Version, pair.Key, pair.Value));
                return new RuntimeLiveStateRead(m_Version, true, current);
            }

            var changes = new List<RuntimeLiveStateChange>();
            for (int i = 0; i < m_Changes.Count; i++)
            {
                RuntimeLiveStateChange change = m_Changes[i];
                if (change.Revision > cursor)
                    changes.Add(change);
            }
            return new RuntimeLiveStateRead(m_Version, false, changes);
        }

        public void Clear()
        {
            m_Current.Clear();
            m_Changes.Clear();
            m_Version++;
        }

        static bool StateEquivalent(in RuntimeTraceEvent left, in RuntimeTraceEvent right)
        {
            RuntimeTracePayload leftPayload = left.Payload;
            RuntimeTracePayload rightPayload = right.Payload;
            return left.Channel == right.Channel &&
                   left.Domain == right.Domain &&
                   left.RuntimeInstance.Equals(right.RuntimeInstance) &&
                   left.Source.Equals(right.Source) &&
                   left.Kind == right.Kind &&
                   PayloadEquivalent(in leftPayload, in rightPayload);
        }

        static bool PayloadEquivalent(in RuntimeTracePayload left, in RuntimeTracePayload right)
        {
            return string.Equals(left.Status, right.Status, StringComparison.Ordinal) &&
                   string.Equals(left.Name, right.Name, StringComparison.Ordinal) &&
                   string.Equals(left.Detail, right.Detail, StringComparison.Ordinal) &&
                   string.Equals(left.Cause, right.Cause, StringComparison.Ordinal) &&
                   string.Equals(left.AnimationChannelId, right.AnimationChannelId, StringComparison.Ordinal) &&
                   string.Equals(left.OwnerId, right.OwnerId, StringComparison.Ordinal) &&
                   string.Equals(left.RelatedElementId, right.RelatedElementId, StringComparison.Ordinal) &&
                   left.Time.Equals(right.Time) &&
                   left.SecondaryTime.Equals(right.SecondaryTime) &&
                   left.NormalizedTime.Equals(right.NormalizedTime) &&
                   left.Weight.Equals(right.Weight) &&
                   left.FinalWeight.Equals(right.FinalWeight) &&
                   left.Priority == right.Priority &&
                   left.Cycle == right.Cycle &&
                   left.TrackIndex == right.TrackIndex &&
                   left.ClipIndex == right.ClipIndex &&
                   left.Flag == right.Flag &&
                   DebugValueEquivalent(in left.Value, in right.Value) &&
                   TimelineProvenanceEquivalent(in left.TimelinePlayback, in right.TimelinePlayback) &&
                   FootIkEquivalent(in left.FootIk, in right.FootIk);
        }

        static bool FootIkEquivalent(in RuntimeFootIkTraceSnapshot left, in RuntimeFootIkTraceSnapshot right)
        {
            return left.IsAvailable == right.IsAvailable &&
                   left.FrameSequence == right.FrameSequence &&
                   left.ResetSequence == right.ResetSequence &&
                   left.GroundingCompletionIdentity == right.GroundingCompletionIdentity &&
                   left.ModifierCompletionIdentity == right.ModifierCompletionIdentity &&
                   left.SolverCompletionIdentity == right.SolverCompletionIdentity &&
                   left.HasPredictiveModifier == right.HasPredictiveModifier &&
                   string.Equals(left.SolverBackendIdentity, right.SolverBackendIdentity, StringComparison.Ordinal) &&
                   string.Equals(left.SolverFailure, right.SolverFailure, StringComparison.Ordinal) &&
                   left.NodeExecuted == right.NodeExecuted &&
                   left.BodyGrounded == right.BodyGrounded &&
                   left.PlacementAlpha.Equals(right.PlacementAlpha) &&
                   left.PresentationDeltaSeconds.Equals(right.PresentationDeltaSeconds) &&
                   left.PoseRootVerticalDelta.Equals(right.PoseRootVerticalDelta) &&
                   left.PoseRootWorldPosition.Equals(right.PoseRootWorldPosition) &&
                   left.PoseRootWorldRotation.Equals(right.PoseRootWorldRotation) &&
                   left.PelvisLyraTargetOffset.Equals(right.PelvisLyraTargetOffset) &&
                   left.PelvisResolvedTargetOffset.Equals(right.PelvisResolvedTargetOffset) &&
                   left.CurrentPelvisOffset.Equals(right.CurrentPelvisOffset) &&
                   left.PelvisSpringVelocity.Equals(right.PelvisSpringVelocity) &&
                   left.PreviousPelvisTarget.Equals(right.PreviousPelvisTarget) &&
                   left.PelvisSpringInitialized == right.PelvisSpringInitialized &&
                   left.PelvisPreSolveTranslation.Equals(right.PelvisPreSolveTranslation) &&
                   left.PelvisGoalPositionWeight.Equals(right.PelvisGoalPositionWeight) &&
                   string.Equals(left.PelvisGoalApplication, right.PelvisGoalApplication, StringComparison.Ordinal) &&
                   string.Equals(left.PelvisGoalSourceKind, right.PelvisGoalSourceKind, StringComparison.Ordinal) &&
                   left.PelvisSupportAvailable == right.PelvisSupportAvailable &&
                   string.Equals(left.PelvisSupportSide, right.PelvisSupportSide, StringComparison.Ordinal) &&
                   left.PelvisSupportSwitched == right.PelvisSupportSwitched &&
                   left.PelvisSupportPlanSequence == right.PelvisSupportPlanSequence &&
                   left.PelvisCurrentSupportTarget.Equals(right.PelvisCurrentSupportTarget) &&
                   left.PelvisSelectedSupportTarget.Equals(right.PelvisSelectedSupportTarget) &&
                   left.LeftPelvisHasActionConstraint == right.LeftPelvisHasActionConstraint &&
                   string.Equals(left.LeftPelvisConstraintMode, right.LeftPelvisConstraintMode, StringComparison.Ordinal) &&
                   string.Equals(left.LeftPelvisSupportPhase, right.LeftPelvisSupportPhase, StringComparison.Ordinal) &&
                   string.Equals(left.LeftPelvisBodyPivotMode, right.LeftPelvisBodyPivotMode, StringComparison.Ordinal) &&
                   left.LeftPelvisCandidate == right.LeftPelvisCandidate &&
                   left.LeftPelvisPlanSequence == right.LeftPelvisPlanSequence &&
                   left.LeftPelvisDisplacement.Equals(right.LeftPelvisDisplacement) &&
                   left.RightPelvisHasActionConstraint == right.RightPelvisHasActionConstraint &&
                   string.Equals(left.RightPelvisConstraintMode, right.RightPelvisConstraintMode, StringComparison.Ordinal) &&
                   string.Equals(left.RightPelvisSupportPhase, right.RightPelvisSupportPhase, StringComparison.Ordinal) &&
                   string.Equals(left.RightPelvisBodyPivotMode, right.RightPelvisBodyPivotMode, StringComparison.Ordinal) &&
                   left.RightPelvisCandidate == right.RightPelvisCandidate &&
                   left.RightPelvisPlanSequence == right.RightPelvisPlanSequence &&
                   left.RightPelvisDisplacement.Equals(right.RightPelvisDisplacement) &&
                   string.Equals(left.LyraSourceIdentity, right.LyraSourceIdentity, StringComparison.Ordinal) &&
                   string.Equals(left.SpringIdentity, right.SpringIdentity, StringComparison.Ordinal) &&
                   string.Equals(left.RigId, right.RigId, StringComparison.Ordinal) &&
                   string.Equals(left.RigRevision, right.RigRevision, StringComparison.Ordinal) &&
                   string.Equals(left.ProfileId, right.ProfileId, StringComparison.Ordinal) &&
                   string.Equals(left.ProfileRevision, right.ProfileRevision, StringComparison.Ordinal) &&
                   string.Equals(left.PosePlanHash, right.PosePlanHash, StringComparison.Ordinal) &&
                   string.Equals(left.CalibrationId, right.CalibrationId, StringComparison.Ordinal) &&
                   string.Equals(left.CalibrationRevision, right.CalibrationRevision, StringComparison.Ordinal) &&
                   left.PhysicsSceneIdentity == right.PhysicsSceneIdentity &&
                   left.SelfFilterIdentity == right.SelfFilterIdentity &&
                   left.BaselineProducerOperationIndex == right.BaselineProducerOperationIndex &&
                   left.BaselineProducerCallSiteIndex == right.BaselineProducerCallSiteIndex &&
                   left.BaselineGoalOffset == right.BaselineGoalOffset &&
                   left.BaselineGoalCount == right.BaselineGoalCount &&
                   string.Equals(left.BaselineRigId, right.BaselineRigId, StringComparison.Ordinal) &&
                   string.Equals(left.BaselineRigRevision, right.BaselineRigRevision, StringComparison.Ordinal) &&
                   FootIkLegEquivalent(in left.Left, in right.Left) &&
                   FootIkLegEquivalent(in left.Right, in right.Right);
        }

        static bool FootIkLegEquivalent(in RuntimeFootIkLegTraceSnapshot left, in RuntimeFootIkLegTraceSnapshot right)
        {
            return left.IsAvailable == right.IsAvailable &&
                   left.DidCurrentTraceHit == right.DidCurrentTraceHit &&
                   left.CurrentSurfaceIdentity == right.CurrentSurfaceIdentity &&
                   string.Equals(left.CurrentQueryShape, right.CurrentQueryShape, StringComparison.Ordinal) &&
                   string.Equals(left.CurrentQueryPurpose, right.CurrentQueryPurpose, StringComparison.Ordinal) &&
                   left.CurrentQueryFootIndex == right.CurrentQueryFootIndex &&
                   left.CurrentQueryOrigin.Equals(right.CurrentQueryOrigin) &&
                   left.CurrentQueryCapsuleEnd.Equals(right.CurrentQueryCapsuleEnd) &&
                   left.CurrentQueryDirection.Equals(right.CurrentQueryDirection) &&
                   left.CurrentQueryRadius.Equals(right.CurrentQueryRadius) &&
                   left.CurrentQueryMaximumDistance.Equals(right.CurrentQueryMaximumDistance) &&
                   left.CurrentQueryLayerMask == right.CurrentQueryLayerMask &&
                   left.CurrentQueryMinimumGroundNormalDot.Equals(right.CurrentQueryMinimumGroundNormalDot) &&
                   left.CurrentHitLocation.Equals(right.CurrentHitLocation) &&
                   left.CurrentImpactPoint.Equals(right.CurrentImpactPoint) &&
                   left.CurrentHitNormal.Equals(right.CurrentHitNormal) &&
                   left.CurrentHitDistance.Equals(right.CurrentHitDistance) &&
                   string.Equals(left.ContactState, right.ContactState, StringComparison.Ordinal) &&
                   string.Equals(left.TransitionReason, right.TransitionReason, StringComparison.Ordinal) &&
                   string.Equals(left.ContactDecision, right.ContactDecision, StringComparison.Ordinal) &&
                   left.ContactSurfaceValid == right.ContactSurfaceValid &&
                   left.ContactSurfaceDistanceAccepted == right.ContactSurfaceDistanceAccepted &&
                   left.ContactCaptureSpeedAccepted == right.ContactCaptureSpeedAccepted &&
                   left.ContactRetentionSpeedAccepted == right.ContactRetentionSpeedAccepted &&
                   left.ContactConfidenceAccepted == right.ContactConfidenceAccepted &&
                   left.MaximumContactSurfaceDistance.Equals(right.MaximumContactSurfaceDistance) &&
                   left.PlantSpeedThreshold.Equals(right.PlantSpeedThreshold) &&
                   left.UnalignmentSpeedThreshold.Equals(right.UnalignmentSpeedThreshold) &&
                   left.PlantConfidenceEnter.Equals(right.PlantConfidenceEnter) &&
                   left.PlantConfidenceExit.Equals(right.PlantConfidenceExit) &&
                   left.AnchorDistance.Equals(right.AnchorDistance) &&
                   left.AnchorDistanceAccepted == right.AnchorDistanceAccepted &&
                   left.MaximumAnchorDistance.Equals(right.MaximumAnchorDistance) &&
                   left.AnchorBlendSpeed.Equals(right.AnchorBlendSpeed) &&
                   left.HasSurfaceAnchor == right.HasSurfaceAnchor &&
                   left.SurfaceLocalAnchor.Equals(right.SurfaceLocalAnchor) &&
                   left.SurfaceLocalRotation.Equals(right.SurfaceLocalRotation) &&
                   left.AnchorWorldPosition.Equals(right.AnchorWorldPosition) &&
                   left.AnchorWorldRotation.Equals(right.AnchorWorldRotation) &&
                   left.PredictiveRewritten == right.PredictiveRewritten &&
                   string.Equals(left.PredictionRejectReason, right.PredictionRejectReason, StringComparison.Ordinal) &&
                   left.FutureSurfaceIdentity == right.FutureSurfaceIdentity &&
                   left.FutureSupportPoint.Equals(right.FutureSupportPoint) &&
                   left.FutureSupportNormal.Equals(right.FutureSupportNormal) &&
                   left.GroundEnvelopeSegmentCount == right.GroundEnvelopeSegmentCount &&
                   string.Equals(left.GroundEnvelopeRejectReason, right.GroundEnvelopeRejectReason, StringComparison.Ordinal) &&
                   left.PredictiveQueryCount == right.PredictiveQueryCount &&
                   left.PredictiveRejectedQueryCount == right.PredictiveRejectedQueryCount &&
                   left.PredictiveRawHitCount == right.PredictiveRawHitCount &&
                   left.PredictiveRejectNoCandidateCount == right.PredictiveRejectNoCandidateCount &&
                   left.PredictiveRejectHeightDiscontinuityCount == right.PredictiveRejectHeightDiscontinuityCount &&
                   left.PredictiveRejectEdgeGapCount == right.PredictiveRejectEdgeGapCount &&
                   left.PredictiveRejectSurfaceDiscontinuityCount == right.PredictiveRejectSurfaceDiscontinuityCount &&
                   left.PredictiveRejectReachExceededCount == right.PredictiveRejectReachExceededCount &&
                   left.PredictiveRejectSlopeExceededCount == right.PredictiveRejectSlopeExceededCount &&
                   left.PredictiveRejectStepExceededCount == right.PredictiveRejectStepExceededCount &&
                   left.PredictiveRejectInvalidCandidateCount == right.PredictiveRejectInvalidCandidateCount &&
                   left.PredictiveRejectUnsupportedCenterCount == right.PredictiveRejectUnsupportedCenterCount &&
                   left.FutureLandingQueryAvailable == right.FutureLandingQueryAvailable &&
                   string.Equals(left.FutureLandingQueryShape, right.FutureLandingQueryShape, StringComparison.Ordinal) &&
                   string.Equals(left.FutureLandingQueryPurpose, right.FutureLandingQueryPurpose, StringComparison.Ordinal) &&
                   left.FutureLandingQueryOrigin.Equals(right.FutureLandingQueryOrigin) &&
                   left.FutureLandingQueryDirection.Equals(right.FutureLandingQueryDirection) &&
                   left.FutureLandingQueryRadius.Equals(right.FutureLandingQueryRadius) &&
                   left.FutureLandingQueryMaximumDistance.Equals(right.FutureLandingQueryMaximumDistance) &&
                   left.FutureLandingQueryMinimumGroundNormalDot.Equals(right.FutureLandingQueryMinimumGroundNormalDot) &&
                   left.FootFeatureValid == right.FootFeatureValid &&
                   left.PredictedStepValid == right.PredictedStepValid &&
                   left.PredictedStepHasLandingEvent == right.PredictedStepHasLandingEvent &&
                   left.PredictedStepSourceBound == right.PredictedStepSourceBound &&
                   left.HasAuthoritativeLandingEvent == right.HasAuthoritativeLandingEvent &&
                   left.ExpectedLandingEventIdentity == right.ExpectedLandingEventIdentity &&
                   left.LandingEventIdentityValid == right.LandingEventIdentityValid &&
                   left.CurrentEventIsPreSwing == right.CurrentEventIsPreSwing &&
                   left.CurrentEventIsSwing == right.CurrentEventIsSwing &&
                   left.LandingEventIdentity == right.LandingEventIdentity &&
                   left.SourceSampleIdentity == right.SourceSampleIdentity &&
                   left.SourceSampleCycle == right.SourceSampleCycle &&
                   left.EventOrdinal == right.EventOrdinal &&
                   left.ContributionContinuityIdentity == right.ContributionContinuityIdentity &&
                   left.LandingConfidence.Equals(right.LandingConfidence) &&
                   left.AuthoredLandingDelaySeconds.Equals(right.AuthoredLandingDelaySeconds) &&
                   left.LandingEventPhase.Equals(right.LandingEventPhase) &&
                   left.LandingLiftOffPhase.Equals(right.LandingLiftOffPhase) &&
                   left.RootLocalLanding.Equals(right.RootLocalLanding) &&
                   left.RootLocalRouteSample0.Equals(right.RootLocalRouteSample0) &&
                   left.RootLocalRouteSample1.Equals(right.RootLocalRouteSample1) &&
                   left.RootLocalRouteSample2.Equals(right.RootLocalRouteSample2) &&
                   left.RootLocalRouteSample3.Equals(right.RootLocalRouteSample3) &&
                   left.RootLocalRouteSample4.Equals(right.RootLocalRouteSample4) &&
                   left.RootLocalRouteSample5.Equals(right.RootLocalRouteSample5) &&
                   left.RootLocalRouteSample6.Equals(right.RootLocalRouteSample6) &&
                   left.RootLocalRouteSample7.Equals(right.RootLocalRouteSample7) &&
                   left.RootLocalRouteSample8.Equals(right.RootLocalRouteSample8) &&
                   left.RootLocalRouteSample9.Equals(right.RootLocalRouteSample9) &&
                   left.RootLocalRouteSample10.Equals(right.RootLocalRouteSample10) &&
                   left.RootLocalRouteSample11.Equals(right.RootLocalRouteSample11) &&
                   left.RootLocalRouteSample12.Equals(right.RootLocalRouteSample12) &&
                   left.RootLocalRouteSample13.Equals(right.RootLocalRouteSample13) &&
                   left.RootLocalRouteSample14.Equals(right.RootLocalRouteSample14) &&
                   left.RootLocalRouteSample15.Equals(right.RootLocalRouteSample15) &&
                   left.RootLocalRouteSample16.Equals(right.RootLocalRouteSample16) &&
                   left.RootLocalRouteSample17.Equals(right.RootLocalRouteSample17) &&
                   left.RootLocalRouteSample18.Equals(right.RootLocalRouteSample18) &&
                   left.RootLocalRouteSample19.Equals(right.RootLocalRouteSample19) &&
                   left.RootLocalRouteSample20.Equals(right.RootLocalRouteSample20) &&
                   left.RootLocalRouteSample21.Equals(right.RootLocalRouteSample21) &&
                   left.RootLocalRouteSample22.Equals(right.RootLocalRouteSample22) &&
                   left.RootLocalRouteSample23.Equals(right.RootLocalRouteSample23) &&
                   left.RootLocalRouteSample24.Equals(right.RootLocalRouteSample24) &&
                   left.AuthoredFootRouteStart.Equals(right.AuthoredFootRouteStart) &&
                   left.AuthoredFootRouteLanding.Equals(right.AuthoredFootRouteLanding) &&
                   left.PredictivePlanSequence == right.PredictivePlanSequence &&
                   left.PredictivePlanGeneratedFrame == right.PredictivePlanGeneratedFrame &&
                   left.PredictivePlanGenerationPhase.Equals(right.PredictivePlanGenerationPhase) &&
                   left.IncomingPredictedStepValid == right.IncomingPredictedStepValid &&
                   left.IncomingLandingEventIdentityValid == right.IncomingLandingEventIdentityValid &&
                   left.IncomingLandingEventIdentity == right.IncomingLandingEventIdentity &&
                   left.IncomingEventPhase.Equals(right.IncomingEventPhase) &&
                   left.IncomingLiftOffPhase.Equals(right.IncomingLiftOffPhase) &&
                   string.Equals(left.PredictivePlanState, right.PredictivePlanState, StringComparison.Ordinal) &&
                   string.Equals(left.PredictivePlanTransitionReason, right.PredictivePlanTransitionReason, StringComparison.Ordinal) &&
                   string.Equals(left.PredictivePlanEndReason, right.PredictivePlanEndReason, StringComparison.Ordinal) &&
                   left.PredictiveExecutionProgress.Equals(right.PredictiveExecutionProgress) &&
                   left.PlanLandingEventIdentity == right.PlanLandingEventIdentity &&
                   left.PlanSourceSampleIdentity == right.PlanSourceSampleIdentity &&
                   left.PlanSourceSampleCycle == right.PlanSourceSampleCycle &&
                   left.PlanEventOrdinal == right.PlanEventOrdinal &&
                   left.PlanContributionContinuityIdentity == right.PlanContributionContinuityIdentity &&
                   left.PlanElapsedSeconds.Equals(right.PlanElapsedSeconds) &&
                   left.PlanSecondsToLiftOff.Equals(right.PlanSecondsToLiftOff) &&
                   left.PlanSwingDuration.Equals(right.PlanSwingDuration) &&
                   left.PlanHasPathGeometry == right.PlanHasPathGeometry &&
                   left.PlanHasExecutablePath == right.PlanHasExecutablePath &&
                   left.FrozenPlanarVelocity.Equals(right.FrozenPlanarVelocity) &&
                   left.MotionLinearLandingError.Equals(right.MotionLinearLandingError) &&
                   left.MotionAngularLandingError.Equals(right.MotionAngularLandingError) &&
                   left.MotionLandingError.Equals(right.MotionLandingError) &&
                   left.MotionLandingTolerance.Equals(right.MotionLandingTolerance) &&
                   left.FixedPathStartWorldPosition.Equals(right.FixedPathStartWorldPosition) &&
                   left.FixedLandingWorldPosition.Equals(right.FixedLandingWorldPosition) &&
                   left.CurrentPathWorldPosition.Equals(right.CurrentPathWorldPosition) &&
                   left.CurrentPathRootWorldPosition.Equals(right.CurrentPathRootWorldPosition) &&
                   left.CurrentPathHipWorldPosition.Equals(right.CurrentPathHipWorldPosition) &&
                   left.FrozenRootStartWorldPosition.Equals(right.FrozenRootStartWorldPosition) &&
                   left.FrozenRootStartWorldRotation.Equals(right.FrozenRootStartWorldRotation) &&
                   left.FrozenRootLandingWorldPosition.Equals(right.FrozenRootLandingWorldPosition) &&
                   left.FrozenRootLandingWorldRotation.Equals(right.FrozenRootLandingWorldRotation) &&
                   left.PredictiveRouteSampleCount == right.PredictiveRouteSampleCount &&
                   left.PredictiveAcceptedHitCount == right.PredictiveAcceptedHitCount &&
                   left.PredictiveEdgePlaneCandidateCount == right.PredictiveEdgePlaneCandidateCount &&
                   left.PredictiveAcceptedEdgePlaneCount == right.PredictiveAcceptedEdgePlaneCount &&
                   left.SoleSupportRadius.Equals(right.SoleSupportRadius) &&
                   left.CurrentPathSurfaceIdentity == right.CurrentPathSurfaceIdentity &&
                   left.CurrentPathSupportPoint.Equals(right.CurrentPathSupportPoint) &&
                   left.CurrentPathSupportNormal.Equals(right.CurrentPathSupportNormal) &&
                   left.PreClearanceHeelPathDistance.Equals(right.PreClearanceHeelPathDistance) &&
                   left.PreClearanceToePathDistance.Equals(right.PreClearanceToePathDistance) &&
                   left.PostClearanceHeelPathDistance.Equals(right.PostClearanceHeelPathDistance) &&
                   left.PostClearanceToePathDistance.Equals(right.PostClearanceToePathDistance) &&
                   left.PredictiveClearanceEvaluated == right.PredictiveClearanceEvaluated &&
                   left.PredictiveResidualPenetration.Equals(right.PredictiveResidualPenetration) &&
                   left.PlannedFootRouteWorldSampleCount == right.PlannedFootRouteWorldSampleCount &&
                   left.PlannedFootRouteWorldSample0.Equals(right.PlannedFootRouteWorldSample0) &&
                   left.PlannedFootRouteWorldSample1.Equals(right.PlannedFootRouteWorldSample1) &&
                   left.PlannedFootRouteWorldSample2.Equals(right.PlannedFootRouteWorldSample2) &&
                   left.PlannedFootRouteWorldSample3.Equals(right.PlannedFootRouteWorldSample3) &&
                   left.PlannedFootRouteWorldSample4.Equals(right.PlannedFootRouteWorldSample4) &&
                   left.PlannedFootRouteWorldSample5.Equals(right.PlannedFootRouteWorldSample5) &&
                   left.PlannedFootRouteWorldSample6.Equals(right.PlannedFootRouteWorldSample6) &&
                   left.PredictivePathDiagnosticSampleCount == right.PredictivePathDiagnosticSampleCount &&
                   PathSampleEquivalent(in left.PredictivePathSample0, in right.PredictivePathSample0) &&
                   PathSampleEquivalent(in left.PredictivePathSample1, in right.PredictivePathSample1) &&
                   PathSampleEquivalent(in left.PredictivePathSample2, in right.PredictivePathSample2) &&
                   PathSampleEquivalent(in left.PredictivePathSample3, in right.PredictivePathSample3) &&
                   PathSampleEquivalent(in left.PredictivePathSample4, in right.PredictivePathSample4) &&
                   PathSampleEquivalent(in left.PredictivePathSample5, in right.PredictivePathSample5) &&
                   PathSampleEquivalent(in left.PredictivePathSample6, in right.PredictivePathSample6) &&
                   PathSampleEquivalent(in left.PredictivePathSample7, in right.PredictivePathSample7) &&
                   left.RequiredLift.Equals(right.RequiredLift) &&
                   string.Equals(left.BaselineGoalApplication, right.BaselineGoalApplication, StringComparison.Ordinal) &&
                   string.Equals(left.FinalGoalSourceKind, right.FinalGoalSourceKind, StringComparison.Ordinal) &&
                   left.SolverResultAvailable == right.SolverResultAvailable &&
                   left.PlantConfidence.Equals(right.PlantConfidence) &&
                   left.PlantContact == right.PlantContact &&
                   left.SoleHeight.Equals(right.SoleHeight) &&
                   left.PlacementWeight.Equals(right.PlacementWeight) &&
                   left.AnimationFootSpeed.Equals(right.AnimationFootSpeed) &&
                   left.SurfaceDistance.Equals(right.SurfaceDistance) &&
                   left.SoleSupportSurfaceIdentity == right.SoleSupportSurfaceIdentity &&
                   left.SoleSupportPoint.Equals(right.SoleSupportPoint) &&
                   left.SoleSupportNormal.Equals(right.SoleSupportNormal) &&
                   left.SoleClearanceTarget.Equals(right.SoleClearanceTarget) &&
                   left.SoleClearanceTargetTranslation.Equals(right.SoleClearanceTargetTranslation) &&
                   left.SoleAnklePosition.Equals(right.SoleAnklePosition) &&
                   left.SoleHeelPosition.Equals(right.SoleHeelPosition) &&
                   left.SoleToePosition.Equals(right.SoleToePosition) &&
                   left.SoleHeelPlaneDistance.Equals(right.SoleHeelPlaneDistance) &&
                   left.SoleToePlaneDistance.Equals(right.SoleToePlaneDistance) &&
                   left.ResidualSolePenetration.Equals(right.ResidualSolePenetration) &&
                   left.FinalGoalSoleHeelPosition.Equals(right.FinalGoalSoleHeelPosition) &&
                   left.FinalGoalSoleToePosition.Equals(right.FinalGoalSoleToePosition) &&
                   left.SolvedSoleAnklePosition.Equals(right.SolvedSoleAnklePosition) &&
                   left.SolvedSoleHeelPosition.Equals(right.SolvedSoleHeelPosition) &&
                   left.SolvedSoleToePosition.Equals(right.SolvedSoleToePosition) &&
                   left.FinalPhysicalEvaluationAvailable == right.FinalPhysicalEvaluationAvailable &&
                   string.Equals(left.FinalPhysicalSupportKind, right.FinalPhysicalSupportKind, StringComparison.Ordinal) &&
                   left.FinalPhysicalSupportSurfaceIdentity == right.FinalPhysicalSupportSurfaceIdentity &&
                   left.FinalPhysicalSupportPoint.Equals(right.FinalPhysicalSupportPoint) &&
                   left.FinalPhysicalSupportNormal.Equals(right.FinalPhysicalSupportNormal) &&
                   left.FinalPhysicalHeelPlaneDistance.Equals(right.FinalPhysicalHeelPlaneDistance) &&
                   left.FinalPhysicalToePlaneDistance.Equals(right.FinalPhysicalToePlaneDistance) &&
                   left.FinalPhysicalResidualPenetration.Equals(right.FinalPhysicalResidualPenetration) &&
                   left.AnimatedAnkleComponentY.Equals(right.AnimatedAnkleComponentY) &&
                   left.AnchorBlendWeight.Equals(right.AnchorBlendWeight) &&
                   left.BaselineGoalPositionWeight.Equals(right.BaselineGoalPositionWeight) &&
                   left.BaselineGoalRotationWeight.Equals(right.BaselineGoalRotationWeight) &&
                   left.FinalGoalPositionWeight.Equals(right.FinalGoalPositionWeight) &&
                   left.FinalGoalRotationWeight.Equals(right.FinalGoalRotationWeight) &&
                   left.TargetOffset.Equals(right.TargetOffset) &&
                   left.OffsetTarget.Equals(right.OffsetTarget) &&
                   left.UnconstrainedOffset.Equals(right.UnconstrainedOffset) &&
                   left.SoleConstraintOffset.Equals(right.SoleConstraintOffset) &&
                   left.CurrentOffset.Equals(right.CurrentOffset) &&
                   left.OffsetSpringVelocity.Equals(right.OffsetSpringVelocity) &&
                   left.PreviousOffsetTarget.Equals(right.PreviousOffsetTarget) &&
                   left.OffsetSpringInitialized == right.OffsetSpringInitialized &&
                   left.TargetNormal.Equals(right.TargetNormal) &&
                   left.CurrentNormal.Equals(right.CurrentNormal) &&
                   left.NormalSpringVelocity.Equals(right.NormalSpringVelocity) &&
                   left.PreviousNormalTarget.Equals(right.PreviousNormalTarget) &&
                   left.NormalSpringInitialized == right.NormalSpringInitialized &&
                   left.PredictionHorizon.Equals(right.PredictionHorizon) &&
                   left.CurrentGroundingComponentPosition.Equals(right.CurrentGroundingComponentPosition) &&
                   left.BaselineGoalComponentPosition.Equals(right.BaselineGoalComponentPosition) &&
                   left.FinalGoalComponentPosition.Equals(right.FinalGoalComponentPosition) &&
                   left.SolvedComponentPosition.Equals(right.SolvedComponentPosition) &&
                   left.PositionResidual.Equals(right.PositionResidual) &&
                   left.RotationResidualDegrees.Equals(right.RotationResidualDegrees);
        }

        static bool PathSampleEquivalent(
            in RuntimeFootIkPathSampleSnapshot left,
            in RuntimeFootIkPathSampleSnapshot right) =>
            left.Fraction.Equals(right.Fraction) &&
            left.Position.Equals(right.Position) &&
            left.Normal.Equals(right.Normal) &&
            left.SurfaceIdentity == right.SurfaceIdentity &&
            left.AnimationRootPosition.Equals(right.AnimationRootPosition) &&
            left.HipPosition.Equals(right.HipPosition);

        static bool DebugValueEquivalent(in DebugValueSnapshot left, in DebugValueSnapshot right)
        {
            return left.Kind == right.Kind &&
                   left.Boolean == right.Boolean &&
                   left.Signed == right.Signed &&
                   left.Unsigned == right.Unsigned &&
                   left.Number.Equals(right.Number) &&
                   string.Equals(left.Text, right.Text, StringComparison.Ordinal) &&
                   left.Vector.Equals(right.Vector);
        }

        static bool TimelineProvenanceEquivalent(in RuntimeTimelinePlaybackProvenance left, in RuntimeTimelinePlaybackProvenance right)
        {
            return string.Equals(left.SourceGraphAuthoringId, right.SourceGraphAuthoringId, StringComparison.Ordinal) &&
                   string.Equals(left.SourceNodeAuthoringId, right.SourceNodeAuthoringId, StringComparison.Ordinal) &&
                   left.SourceGraphRuntimeId.Equals(right.SourceGraphRuntimeId) &&
                   left.SourceActivationGeneration == right.SourceActivationGeneration &&
                   string.Equals(left.StateMachineGraphAuthoringId, right.StateMachineGraphAuthoringId, StringComparison.Ordinal) &&
                   string.Equals(left.StateId, right.StateId, StringComparison.Ordinal) &&
                   left.StateMachineGraphRuntimeId.Equals(right.StateMachineGraphRuntimeId) &&
                   left.StateActivationGeneration == right.StateActivationGeneration &&
                   string.Equals(left.SourceName, right.SourceName, StringComparison.Ordinal);
        }
    }

    sealed class RuntimeCaptureStore : IDisposable
    {
        readonly List<Segment> m_Segments = new List<Segment>();
        readonly List<RuntimeCaptureChange> m_Changes = new List<RuntimeCaptureChange>();
        readonly int m_MaxSegments;
        readonly Guid m_CaptureId;
        RuntimeDiagnosticsCaptureDetail m_Detail;
        long m_Version;

        public RuntimeCaptureStore(Guid captureId, RuntimeDiagnosticsCaptureDetail detail, int maxSegments = 512)
        {
            if (maxSegments < 8)
                throw new ArgumentOutOfRangeException(nameof(maxSegments));
            m_CaptureId = captureId;
            m_Detail = detail;
            m_MaxSegments = maxSegments;
        }

        public Guid CaptureId => m_CaptureId;
        public RuntimeDiagnosticsCaptureDetail Detail => m_Detail;
        public long Version => m_Version;
        public int SegmentCount => m_Segments.Count;
        public int MaxSegments => m_MaxSegments;

        public void UpgradeDetail(RuntimeDiagnosticsCaptureDetail detail)
        {
            if (detail > m_Detail)
                m_Detail = detail;
        }

        public void Publish(RuntimeTraceEvent traceEvent)
        {
            Segment segment = m_Segments.Count > 0 ? m_Segments[m_Segments.Count - 1] : null;
            if (segment == null || segment.Domain != traceEvent.Domain || segment.Position != traceEvent.Position)
            {
                segment = new Segment(traceEvent.Domain, traceEvent.Position);
                m_Segments.Add(segment);
            }

            m_Version++;
            var change = new RuntimeCaptureChange(m_Version, traceEvent);
            segment.Events.Add(change);
            m_Changes.Add(change);
            TrimToCapacity();
        }

        public RuntimeCaptureRead ReadSince(long cursor)
        {
            if (cursor >= m_Version)
                return new RuntimeCaptureRead(m_Version, false, Array.Empty<RuntimeCaptureChange>());

            long earliestAvailable = m_Changes.Count > 0 ? m_Changes[0].Revision : m_Version + 1;
            if (cursor < earliestAvailable - 1)
                return new RuntimeCaptureRead(m_Version, true, CollectAllChanges());

            var changes = new List<RuntimeCaptureChange>();
            for (int i = 0; i < m_Changes.Count; i++)
            {
                RuntimeCaptureChange change = m_Changes[i];
                if (change.Revision > cursor)
                    changes.Add(change);
            }
            return new RuntimeCaptureRead(m_Version, false, changes);
        }

        public RuntimeCaptureSnapshot Freeze(RuntimeTraceChannel channels)
        {
            var segments = new List<RuntimeCaptureSegmentSnapshot>(m_Segments.Count);
            for (int i = 0; i < m_Segments.Count; i++)
            {
                Segment source = m_Segments[i];
                var events = new RuntimeTraceEvent[source.Events.Count];
                for (int j = 0; j < source.Events.Count; j++)
                    events[j] = source.Events[j].TraceEvent;
                segments.Add(new RuntimeCaptureSegmentSnapshot(source.Domain, source.Position, events));
            }
            return new RuntimeCaptureSnapshot(m_CaptureId, channels, m_Detail, segments, m_Version);
        }

        public void Dispose()
        {
            m_Segments.Clear();
            m_Changes.Clear();
        }

        void TrimToCapacity()
        {
            while (m_Segments.Count > m_MaxSegments)
            {
                Segment removed = m_Segments[0];
                m_Segments.RemoveAt(0);
                if (removed.Events.Count > 0)
                    m_Changes.RemoveRange(0, Math.Min(removed.Events.Count, m_Changes.Count));
            }
        }

        List<RuntimeCaptureChange> CollectAllChanges()
        {
            var changes = new List<RuntimeCaptureChange>();
            for (int i = 0; i < m_Segments.Count; i++)
                changes.AddRange(m_Segments[i].Events);
            return changes;
        }

        sealed class Segment
        {
            public Segment(RuntimeTraceDomain domain, ulong position)
            {
                Domain = domain;
                Position = position;
            }

            public RuntimeTraceDomain Domain { get; }
            public ulong Position { get; }
            public List<RuntimeCaptureChange> Events { get; } = new List<RuntimeCaptureChange>();
        }
    }

    public sealed class RuntimeDiagnosticsStore : IDisposable
    {
        readonly object m_Gate = new object();
        readonly Dictionary<Guid, RuntimeDiagnosticsInterest> m_Interests = new Dictionary<Guid, RuntimeDiagnosticsInterest>();
        readonly RuntimeLiveStateStore m_LiveState = new RuntimeLiveStateStore();
        RuntimeCaptureStore m_Capture;
        RuntimeTraceChannel m_LiveChannels;
        RuntimeTraceChannel m_CaptureChannels;
        RuntimeDiagnosticsCaptureDetail m_CaptureDetail;
        bool m_Terminated;
        bool m_Disposed;

        public RuntimeTraceChannel EffectiveChannels
        {
            get
            {
                lock (m_Gate)
                    return m_LiveChannels | m_CaptureChannels;
            }
        }

        public bool IsCaptureRecording
        {
            get
            {
                lock (m_Gate)
                    return m_Capture != null;
            }
        }

        public int CaptureSegmentCount
        {
            get
            {
                lock (m_Gate)
                    return m_Capture?.SegmentCount ?? 0;
            }
        }

        public int CaptureSegmentCapacity
        {
            get
            {
                lock (m_Gate)
                    return m_Capture?.MaxSegments ?? 0;
            }
        }

        public RuntimeDiagnosticsInterestHandle AcquireInterest(RuntimeDiagnosticsInterest interest)
        {
            if (!interest.IsValid)
                throw new ArgumentException("A diagnostics interest requires channels and a valid capture detail.", nameof(interest));

            lock (m_Gate)
            {
                if (m_Disposed || m_Terminated)
                    return default;
                Guid value = Guid.NewGuid();
                m_Interests.Add(value, interest);
                RecalculateLiveChannels();
                return new RuntimeDiagnosticsInterestHandle(value);
            }
        }

        public bool ReleaseInterest(RuntimeDiagnosticsInterestHandle handle)
        {
            if (!handle.IsValid)
                return false;

            lock (m_Gate)
            {
                if (!m_Interests.Remove(handle.Value))
                    return false;
                RecalculateLiveChannels();
                return true;
            }
        }

        public bool BeginCapture(RuntimeTraceChannel channels, RuntimeDiagnosticsCaptureDetail detail, out Guid captureId)
        {
            channels &= RuntimeTraceChannel.All;
            if (channels == RuntimeTraceChannel.None || detail == RuntimeDiagnosticsCaptureDetail.None)
            {
                captureId = Guid.Empty;
                return false;
            }

            lock (m_Gate)
            {
                if (m_Disposed || m_Terminated)
                {
                    captureId = Guid.Empty;
                    return false;
                }

                if (m_Capture == null)
                {
                    captureId = Guid.NewGuid();
                    m_Capture = new RuntimeCaptureStore(captureId, detail);
                    m_CaptureChannels = channels;
                    m_CaptureDetail = detail;
                    return true;
                }

                captureId = m_Capture.CaptureId;
                m_CaptureChannels |= channels;
                if (detail > m_CaptureDetail)
                {
                    m_CaptureDetail = detail;
                    m_Capture.UpgradeDetail(detail);
                }
                return true;
            }
        }

        public RuntimeCaptureSnapshot EndCapture()
        {
            lock (m_Gate)
            {
                if (m_Capture == null)
                    return null;

                RuntimeCaptureSnapshot snapshot = m_Capture.Freeze(m_CaptureChannels);
                m_Capture.Dispose();
                m_Capture = null;
                m_CaptureChannels = RuntimeTraceChannel.None;
                m_CaptureDetail = RuntimeDiagnosticsCaptureDetail.None;
                return snapshot;
            }
        }

        public RuntimeCaptureSnapshot FreezeActiveCapture()
        {
            lock (m_Gate)
                return m_Capture?.Freeze(m_CaptureChannels);
        }

        public RuntimeLiveStateRead ReadLiveStateSince(long cursor)
        {
            lock (m_Gate)
                return m_LiveState.ReadSince(cursor);
        }

        public RuntimeCaptureRead ReadCaptureSince(long cursor)
        {
            lock (m_Gate)
            {
                return m_Capture != null
                    ? m_Capture.ReadSince(cursor)
                    : new RuntimeCaptureRead(0, false, Array.Empty<RuntimeCaptureChange>());
            }
        }

        public bool ShouldPublish(RuntimeTraceChannel channel, RuntimeTraceEventKind kind)
        {
            lock (m_Gate)
            {
                if (m_Disposed || m_Terminated)
                    return false;
                bool live = (m_LiveChannels & channel) != 0;
                bool capture = m_Capture != null &&
                               (m_CaptureChannels & channel) != 0 &&
                               m_CaptureDetail >= RequiredCaptureDetail(kind);
                return live || capture;
            }
        }

        public bool ShouldCapture(RuntimeTraceChannel channel, RuntimeTraceEventKind kind)
        {
            lock (m_Gate)
            {
                return !m_Disposed &&
                       !m_Terminated &&
                       m_Capture != null &&
                       (m_CaptureChannels & channel) != 0 &&
                       m_CaptureDetail >= RequiredCaptureDetail(kind);
            }
        }

        public void Publish(RuntimeTraceEvent traceEvent)
        {
            lock (m_Gate)
            {
                if (m_Disposed || m_Terminated)
                    return;

                bool live = (m_LiveChannels & traceEvent.Channel) != 0;
                bool capture = m_Capture != null &&
                               (m_CaptureChannels & traceEvent.Channel) != 0 &&
                               m_CaptureDetail >= RequiredCaptureDetail(traceEvent.Kind);
                if (!live && !capture)
                    return;

                if (live)
                    m_LiveState.Upsert(traceEvent);
                if (capture)
                    m_Capture.Publish(traceEvent);
            }
        }

        public void Terminate()
        {
            lock (m_Gate)
            {
                if (m_Disposed || m_Terminated)
                    return;
                m_Terminated = true;
                m_Interests.Clear();
                m_LiveChannels = RuntimeTraceChannel.None;
            }
        }

        public void Dispose()
        {
            lock (m_Gate)
            {
                if (m_Disposed)
                    return;
                m_Disposed = true;
                m_Terminated = true;
                m_Interests.Clear();
                m_LiveChannels = RuntimeTraceChannel.None;
                m_CaptureChannels = RuntimeTraceChannel.None;
                m_CaptureDetail = RuntimeDiagnosticsCaptureDetail.None;
                m_Capture?.Dispose();
                m_Capture = null;
                m_LiveState.Clear();
            }
        }

        static RuntimeDiagnosticsCaptureDetail RequiredCaptureDetail(RuntimeTraceEventKind kind)
        {
            return kind switch
            {
                RuntimeTraceEventKind.EdgeEvaluated or
                RuntimeTraceEventKind.ConditionGraphEvaluated or
                RuntimeTraceEventKind.StateTransitionEvaluated => RuntimeDiagnosticsCaptureDetail.Evaluation,
                RuntimeTraceEventKind.NodeStatus or
                RuntimeTraceEventKind.TimelineLogicTime or
                RuntimeTraceEventKind.TimelineVisualTime or
                RuntimeTraceEventKind.TrackActive or
                RuntimeTraceEventKind.ClipActive or
                RuntimeTraceEventKind.TreeClipUpdated or
                RuntimeTraceEventKind.MotionContribution or
                RuntimeTraceEventKind.MotionResolved or
                RuntimeTraceEventKind.ActionSnapshot or
                RuntimeTraceEventKind.AnimationProducerSampled or
                RuntimeTraceEventKind.AnimationPlaybackPending or
                RuntimeTraceEventKind.AnimationPlaybackSelected or
                RuntimeTraceEventKind.AnimationPlaybackRetained or
                RuntimeTraceEventKind.AnimationPlaybackRetired or
                RuntimeTraceEventKind.AnimationMarkerSync or
                RuntimeTraceEventKind.MotionMatchingQuery or
                RuntimeTraceEventKind.MotionMatchingTrajectory or
                RuntimeTraceEventKind.MotionMatchingPoseHistory or
                RuntimeTraceEventKind.MotionMatchingAdmission or
                RuntimeTraceEventKind.MotionMatchingCandidateRejected or
                RuntimeTraceEventKind.MotionMatchingSearchTraversal or
                RuntimeTraceEventKind.MotionMatchingTopK or
                RuntimeTraceEventKind.MotionMatchingPlan or
                RuntimeTraceEventKind.MotionMatchingSelection or
                RuntimeTraceEventKind.MotionMatchingPoseSource or
                RuntimeTraceEventKind.MotionMatchingFrame or
                RuntimeTraceEventKind.PresentationInterpolated or
                RuntimeTraceEventKind.FootPlacementSnapshot or
                RuntimeTraceEventKind.CameraSnapshot or
                RuntimeTraceEventKind.CameraRequest => RuntimeDiagnosticsCaptureDetail.Continuous,
                _ => RuntimeDiagnosticsCaptureDetail.Boundary
            };
        }

        void RecalculateLiveChannels()
        {
            RuntimeTraceChannel channels = RuntimeTraceChannel.None;
            foreach (RuntimeDiagnosticsInterest interest in m_Interests.Values)
            {
                if (interest.Kind == RuntimeDiagnosticsInterestKind.LiveState)
                    channels |= interest.Channels;
            }
            m_LiveChannels = channels;
        }
    }
}
