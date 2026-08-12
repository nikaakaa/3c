using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal readonly struct CharacterPredictiveFootRootTrajectory
    {
        internal CharacterPredictiveFootRootTrajectory(
            Vector3 startPosition,
            Quaternion startRotation,
            Vector3 frozenWorldVelocity,
            Vector3 nativeSoleAtGeneration,
            Vector3 nativeHipAtGeneration,
            Vector3 nativeAnkleAtGeneration,
            Vector3 up,
            in AnimationPredictedFootStepSample step)
        {
            if (!step.IsAuthoritative)
                throw new ArgumentException("Predictive Foot Root Trajectory requires an authoritative event.", nameof(step));
            if (!IsFinite(startPosition) || !IsFinite(startRotation) ||
                !IsFinite(frozenWorldVelocity) || !IsFinite(nativeSoleAtGeneration) ||
                !IsFinite(nativeHipAtGeneration) || !IsFinite(nativeAnkleAtGeneration) ||
                !IsFinite(up) || up.sqrMagnitude <= 0.000001f)
            {
                throw new ArgumentException("Predictive Foot Root Trajectory origin is invalid.");
            }
            StartPosition = startPosition;
            StartRotation = startRotation.normalized;
            Up = up.normalized;
            FrozenPlanarVelocity = Vector3.ProjectOnPlane(frozenWorldVelocity, Up);
            NativeSoleAtGeneration = nativeSoleAtGeneration;
            NativeHipAtGeneration = nativeHipAtGeneration;
            NativeAnkleAtGeneration = nativeAnkleAtGeneration;
            EventPhaseAtGeneration = step.ActionStepClock.Phase;
            LiftOffPhase = step.ActionStepClock.LiftOffPhase;
            ConstraintReleasePhase = ResolveConstraintReleasePhase(in step);
            PathStartPhase = Mathf.Max(
                EventPhaseAtGeneration,
                Mathf.Min(LiftOffPhase, ConstraintReleasePhase));
            LandingDelayAtGeneration = step.ActionStepClock.TimeToLandingSeconds;
            ActionStepDurationSeconds = step.ActionStepClock.DurationSeconds;
            Step = step;
            RootLocalFootAtGeneration = step.EvaluateAuthoredFootPlanarRoute(EventPhaseAtGeneration);
            RootLocalFullFootAtGeneration = step.EvaluateRootLocalFootRoute(EventPhaseAtGeneration);
            RootLocalHipAtGeneration = step.EvaluateRootLocalHipRoute(EventPhaseAtGeneration);
            RootLocalAnkleAtGeneration = step.EvaluateRootLocalAnkleRoute(EventPhaseAtGeneration);
            m_TerrainActionFractions = default;
            m_TerrainRouteFractions = default;
        }

        CharacterPredictiveFootRootTrajectory(
            CharacterPredictiveFootRootTrajectory source,
            FixedList512Bytes<float> terrainActionFractions,
            FixedList512Bytes<float> terrainRouteFractions)
        {
            StartPosition = source.StartPosition;
            StartRotation = source.StartRotation;
            Up = source.Up;
            FrozenPlanarVelocity = source.FrozenPlanarVelocity;
            NativeSoleAtGeneration = source.NativeSoleAtGeneration;
            NativeHipAtGeneration = source.NativeHipAtGeneration;
            NativeAnkleAtGeneration = source.NativeAnkleAtGeneration;
            ActionStepDurationSeconds = source.ActionStepDurationSeconds;
            EventPhaseAtGeneration = source.EventPhaseAtGeneration;
            LiftOffPhase = source.LiftOffPhase;
            ConstraintReleasePhase = source.ConstraintReleasePhase;
            PathStartPhase = source.PathStartPhase;
            LandingDelayAtGeneration = source.LandingDelayAtGeneration;
            Step = source.Step;
            RootLocalFootAtGeneration = source.RootLocalFootAtGeneration;
            RootLocalFullFootAtGeneration = source.RootLocalFullFootAtGeneration;
            RootLocalHipAtGeneration = source.RootLocalHipAtGeneration;
            RootLocalAnkleAtGeneration = source.RootLocalAnkleAtGeneration;
            m_TerrainActionFractions = terrainActionFractions;
            m_TerrainRouteFractions = terrainRouteFractions;
        }

        internal Vector3 StartPosition { get; }
        internal Quaternion StartRotation { get; }
        internal Vector3 Up { get; }
        internal Vector3 FrozenPlanarVelocity { get; }
        internal Vector3 NativeSoleAtGeneration { get; }
        internal Vector3 NativeHipAtGeneration { get; }
        internal Vector3 NativeAnkleAtGeneration { get; }
        internal float ActionStepDurationSeconds { get; }
        internal float EventPhaseAtGeneration { get; }
        internal float LiftOffPhase { get; }
        internal float ConstraintReleasePhase { get; }
        internal float PathStartPhase { get; }
        internal float LandingDelayAtGeneration { get; }
        internal bool HasPlanarMotion => FrozenPlanarVelocity.sqrMagnitude > 0.000001f;
        readonly AnimationPredictedFootStepSample Step { get; }
        readonly Vector3 RootLocalFootAtGeneration { get; }
        readonly Vector3 RootLocalFullFootAtGeneration { get; }
        readonly Vector3 RootLocalHipAtGeneration { get; }
        readonly Vector3 RootLocalAnkleAtGeneration { get; }
        readonly FixedList512Bytes<float> m_TerrainActionFractions;
        readonly FixedList512Bytes<float> m_TerrainRouteFractions;

        internal bool HasTerrainProgress => m_TerrainActionFractions.Length >= 2;

        internal CharacterPredictiveFootRootTrajectory WithTerrainProgress(
            FixedList512Bytes<float> actionFractions,
            FixedList512Bytes<float> routeFractions)
        {
            if (actionFractions.Length < 2 || actionFractions.Length != routeFractions.Length ||
                Mathf.Abs(actionFractions[0]) > 0.00001f ||
                Mathf.Abs(routeFractions[0]) > 0.00001f ||
                Mathf.Abs(actionFractions[actionFractions.Length - 1] - 1f) > 0.00001f)
            {
                throw new ArgumentException("Predictive Foot terrain progress is invalid.");
            }
            for (int i = 0; i < actionFractions.Length; i++)
            {
                if (!float.IsFinite(actionFractions[i]) || !float.IsFinite(routeFractions[i]) ||
                    actionFractions[i] < 0f || actionFractions[i] > 1f ||
                    routeFractions[i] < 0f || routeFractions[i] > 1f ||
                    i > 0 && (actionFractions[i] <= actionFractions[i - 1] ||
                              routeFractions[i] < routeFractions[i - 1]))
                {
                    throw new ArgumentException("Predictive Foot terrain progress is not monotonic.");
                }
            }
            return new CharacterPredictiveFootRootTrajectory(
                this,
                actionFractions,
                routeFractions);
        }

        internal void EvaluateSwing(float progress, out Vector3 position, out Quaternion rotation)
        {
            float eventPhase = Mathf.Lerp(PathStartPhase, 1f, Mathf.Clamp01(progress));
            EvaluateEventPhase(eventPhase, out position, out rotation);
        }

        internal void EvaluateEventPhase(float eventPhase, out Vector3 position, out Quaternion rotation)
        {
            float phase = Mathf.Clamp01(eventPhase);
            float elapsedSeconds = ResolveTravelElapsedSeconds(phase);
            position = StartPosition + FrozenPlanarVelocity * elapsedSeconds;
            rotation = StartRotation;
        }

        internal Vector3 EvaluateFootRoute(float eventPhase)
        {
            float phase = Mathf.Clamp01(eventPhase);
            float elapsedSeconds = ResolveTravelElapsedSeconds(phase);
            Vector3 localDelta = Step.EvaluateAuthoredFootPlanarRoute(phase) -
                                 RootLocalFootAtGeneration;
            return NativeSoleAtGeneration + FrozenPlanarVelocity * elapsedSeconds +
                   Vector3.ProjectOnPlane(StartRotation * localDelta, Up);
        }

        internal Vector3 EvaluateHipRoute(float eventPhase)
        {
            float phase = Mathf.Clamp01(eventPhase);
            float elapsedSeconds = ResolveTravelElapsedSeconds(phase);
            Vector3 localDelta = Step.EvaluateRootLocalHipRoute(phase) -
                                 RootLocalHipAtGeneration;
            return NativeHipAtGeneration + FrozenPlanarVelocity * elapsedSeconds +
                   StartRotation * localDelta;
        }

        internal Vector3 EvaluateAnkleRoute(float eventPhase)
        {
            float phase = Mathf.Clamp01(eventPhase);
            float elapsedSeconds = ResolveTravelElapsedSeconds(phase);
            Vector3 localDelta = Step.EvaluateRootLocalAnkleRoute(phase) -
                                 RootLocalAnkleAtGeneration;
            return NativeAnkleAtGeneration + FrozenPlanarVelocity * elapsedSeconds +
                   StartRotation * localDelta;
        }

        internal Vector3 EvaluateSoleToAnkle(float eventPhase)
        {
            float phase = Mathf.Clamp01(eventPhase);
            Vector3 authoredAtGeneration = RootLocalAnkleAtGeneration -
                                           RootLocalFullFootAtGeneration;
            Vector3 authored = Step.EvaluateRootLocalAnkleRoute(phase) -
                               Step.EvaluateRootLocalFootRoute(phase);
            return NativeAnkleAtGeneration - NativeSoleAtGeneration +
                   StartRotation * (authored - authoredAtGeneration);
        }

        internal float EvaluateAuthoredReach(float eventPhase) =>
            Vector3.Distance(EvaluateHipRoute(eventPhase), EvaluateAnkleRoute(eventPhase));

        float ResolveTravelElapsedSeconds(float phase)
        {
            if (!HasTerrainProgress || phase <= PathStartPhase)
            {
                return Mathf.Max(
                    0f,
                    (phase - EventPhaseAtGeneration) * ActionStepDurationSeconds);
            }
            float actionFraction = Mathf.Clamp01(
                (phase - PathStartPhase) /
                Mathf.Max(0.000001f, 1f - PathStartPhase));
            float routeFraction = EvaluateTerrainRouteFraction(actionFraction);
            float mappedPhase = Mathf.Lerp(PathStartPhase, 1f, routeFraction);
            return Mathf.Max(
                0f,
                (mappedPhase - EventPhaseAtGeneration) * ActionStepDurationSeconds);
        }

        float EvaluateTerrainRouteFraction(float actionFraction)
        {
            float value = Mathf.Clamp01(actionFraction);
            for (int i = 1; i < m_TerrainActionFractions.Length; i++)
            {
                if (value > m_TerrainActionFractions[i])
                    continue;
                float start = m_TerrainActionFractions[i - 1];
                float length = m_TerrainActionFractions[i] - start;
                float t = length > 0.000001f
                    ? Mathf.Clamp01((value - start) / length)
                    : 1f;
                return Mathf.Lerp(
                    m_TerrainRouteFractions[i - 1],
                    m_TerrainRouteFractions[i],
                    t);
            }
            return m_TerrainRouteFractions[m_TerrainRouteFractions.Length - 1];
        }

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        static bool IsFinite(Quaternion value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) &&
            float.IsFinite(value.z) && float.IsFinite(value.w) &&
            Quaternion.Dot(value, value) > 0.000001f;

        static float ResolveConstraintReleasePhase(in AnimationPredictedFootStepSample step)
        {
            int count = step.ConstraintModes.Length;
            if (count != AnimationPredictedFootStepCurveSet.RouteSampleCount)
                throw new ArgumentException("Predictive Foot constraint route is unavailable.");
            if ((AnimationFootConstraintMode)step.ConstraintModes[0] != AnimationFootConstraintMode.Locked)
                return 0f;
            for (int i = 1; i < count; i++)
            {
                if ((AnimationFootConstraintMode)step.ConstraintModes[i] == AnimationFootConstraintMode.Locked)
                    continue;
                return (i - 0.5f) / (count - 1f);
            }
            return step.ActionStepClock.LiftOffPhase;
        }
    }

    internal sealed class CharacterPredictiveFootPlacementPlan
    {
        readonly CharacterFootSide m_Side;
        readonly FootPlacementGroundEnvelopeSegment[] m_PathSegments;
        readonly CharacterPredictiveFootQueryRequestSnapshot[] m_QueryRequests;
        readonly CharacterPredictiveFootQueryGeometrySnapshot[] m_AcceptedSupports;
        readonly CharacterPredictiveFootQueryGeometrySnapshot[] m_RejectedGeometry;

        internal CharacterPredictiveFootPlacementPlan(CharacterFootSide side, int pathCapacity)
        {
            if (pathCapacity < 2)
                throw new ArgumentOutOfRangeException(nameof(pathCapacity));
            m_Side = side;
            m_PathSegments = new FootPlacementGroundEnvelopeSegment[pathCapacity];
            m_QueryRequests = new CharacterPredictiveFootQueryRequestSnapshot[
                CharacterPredictiveFootPlacementQuery.MaximumQueryRequestCount];
            m_AcceptedSupports = new CharacterPredictiveFootQueryGeometrySnapshot[
                CharacterPredictiveFootPlacementQuery.MaximumAcceptedGeometryCount];
            m_RejectedGeometry = new CharacterPredictiveFootQueryGeometrySnapshot[
                CharacterPredictiveFootPlacementQuery.MaximumRejectedGeometryCount];
        }

        internal ulong Sequence { get; private set; }
        internal ulong LandingEventIdentity { get; private set; }
        internal ulong SourceSampleIdentity { get; private set; }
        internal int SourceSampleCycle { get; private set; }
        internal int EventOrdinal { get; private set; }
        internal ulong ContributionContinuityIdentity { get; private set; }
        internal ulong GeneratedFrame { get; private set; }
        internal CharacterPredictiveFootPlanState State { get; private set; }
        internal CharacterPredictiveFootPlanTransitionReason TransitionReason { get; private set; }
        internal CharacterPredictiveFootPlanEndReason EndReason { get; private set; }
        internal Vector3 Start { get; private set; }
        internal Vector3 Landing { get; private set; }
        internal Vector3 RootStart { get; private set; }
        internal Quaternion RootStartRotation { get; private set; }
        internal Vector3 RootLanding { get; private set; }
        internal Quaternion RootLandingRotation { get; private set; }
        internal Vector3 PredictedHip { get; private set; }
        internal CharacterPredictiveFootRootTrajectory RootTrajectory { get; private set; }
        internal FixedList128Bytes<Vector3> AuthoredFootPlanarRoute { get; private set; }
        internal FixedList128Bytes<Vector3> RootLocalHipRoute { get; private set; }
        internal FixedList128Bytes<float> AnimationClearanceHeights { get; private set; }
        internal FixedList32Bytes<byte> ConstraintModes { get; private set; }
        internal FixedList32Bytes<byte> SupportPhases { get; private set; }
        internal FixedList32Bytes<byte> FootOrientationPolicies { get; private set; }
        internal FixedList32Bytes<byte> BodyRotationPivotModes { get; private set; }
        internal FixedList128Bytes<Vector3> FrozenWorldFootRoute { get; private set; }
        internal float AnimationClearanceContinuityOffset { get; private set; }
        internal float LandingDelayAtGeneration { get; private set; }
        internal float EventPhaseAtGeneration { get; private set; }
        internal float LiftOffPhase { get; private set; }
        internal float ActionStepDurationSeconds { get; private set; }
        internal float ActionStepPhase { get; private set; }
        internal float Progress { get; private set; }
        internal ulong ActionClockFrame { get; private set; }
        internal FootPlacementSurface FutureSupport { get; private set; }
        internal CharacterFootPlacementQueryRequest FutureLandingRequest { get; private set; }
        internal float VirtualGroundSplitFraction { get; private set; }
        internal FootPlacementSurface VirtualGroundSplitSupport { get; private set; }
        internal ulong VirtualGroundSplitLandingEventIdentity { get; private set; }
        internal int GroundEnvelopeSegmentCount { get; private set; }
        internal FootPlacementGroundEnvelopeRejectReason GroundEnvelopeRejectReason { get; private set; }
        internal int QueryCount { get; private set; }
        internal int RawHitCount { get; private set; }
        internal int RouteSampleCount { get; private set; }
        internal int AcceptedHitCount { get; private set; }
        internal int EdgePlaneCandidateCount { get; private set; }
        internal int AcceptedEdgePlaneCount { get; private set; }
        internal int RejectedQueryCount { get; private set; }
        internal CharacterPredictiveFootQueryRejectCounts QueryRejectCounts { get; private set; }
        internal FootPredictionRejectReason CreationRejectReason { get; private set; }
        internal int QueryRequestSnapshotCount { get; private set; }
        internal int AcceptedSupportSnapshotCount { get; private set; }
        internal int RejectedGeometrySnapshotCount { get; private set; }
        internal CharacterPredictiveFootPlanGeometrySnapshot GeometrySnapshot { get; private set; }
        internal bool OwnsEvent => LandingEventIdentity != 0;
        internal bool HasAttempt =>
            State == CharacterPredictiveFootPlanState.Planned ||
            State == CharacterPredictiveFootPlanState.Executing ||
            State == CharacterPredictiveFootPlanState.Rejected ||
            State == CharacterPredictiveFootPlanState.Completed;
        internal bool HasExecutablePath =>
            State == CharacterPredictiveFootPlanState.Planned ||
            State == CharacterPredictiveFootPlanState.Executing;
        internal bool HasPathGeometry => GroundEnvelopeSegmentCount > 0;
        internal void BeginFrame()
        {
            TransitionReason = CharacterPredictiveFootPlanTransitionReason.None;
            EndReason = CharacterPredictiveFootPlanEndReason.None;
        }

        internal bool MatchesAuthoritativeEvent(in AnimationPredictedFootStepSample step)
        {
            return OwnsEvent &&
                   step.IsAuthoritative &&
                   LandingEventIdentity == step.LandingEventIdentity &&
                   SourceSampleIdentity == step.SourceSampleIdentity &&
                   SourceSampleCycle == step.SourceSampleCycle &&
                   EventOrdinal == step.EventOrdinal &&
                   ContributionContinuityIdentity == step.ContributionContinuityIdentity;
        }

        internal void Commit(
            ulong sequence,
            ulong generatedFrame,
            in AnimationPredictedFootStepSample step,
            Vector3 start,
            Vector3 landing,
            in CharacterPredictiveFootRootTrajectory rootTrajectory,
            Vector3 predictedHip,
            in CharacterPredictiveFootPlacementQueryResult query)
        {
            RequireIdentity(sequence, generatedFrame, in step);
            AssignEvent(sequence, generatedFrame, in step);
            Start = start;
            Landing = landing;
            AssignTiming(in step, rootTrajectory.PathStartPhase);
            State = step.ActionStepClock.Phase + 0.000001f < rootTrajectory.PathStartPhase
                ? CharacterPredictiveFootPlanState.Planned
                : CharacterPredictiveFootPlanState.Executing;
            TransitionReason = CharacterPredictiveFootPlanTransitionReason.PlanGenerated;
            AssignTrajectory(in rootTrajectory, in step);
            PredictedHip = predictedHip;
            FutureSupport = query.FutureLandingSupport;
            FutureLandingRequest = query.FutureLandingRequest;
            VirtualGroundSplitFraction = query.VirtualGroundSplitFraction;
            VirtualGroundSplitSupport = query.VirtualGroundSplitSupport;
            VirtualGroundSplitLandingEventIdentity = query.VirtualGroundSplitLandingEventIdentity;
            GroundEnvelopeSegmentCount = query.GroundEnvelope.CopyTo(m_PathSegments);
            ResolveAnimationClearanceContinuity();
            GroundEnvelopeRejectReason = query.GroundEnvelope.RejectReason;
            QueryCount = query.QueryCount;
            RawHitCount = query.RawHitCount;
            RouteSampleCount = query.RouteSampleCount;
            AcceptedHitCount = query.AcceptedHitCount;
            EdgePlaneCandidateCount = query.EdgePlaneCandidateCount;
            AcceptedEdgePlaneCount = query.AcceptedEdgePlaneCount;
            RejectedQueryCount = query.RejectedCount;
            QueryRejectCounts = query.RejectCounts;
            CreationRejectReason = FootPredictionRejectReason.None;
            CopyQuerySnapshots(in query);
            GeometrySnapshot = BuildGeometrySnapshot();
        }

        internal void Reject(
            ulong sequence,
            ulong generatedFrame,
            in AnimationPredictedFootStepSample step,
            Vector3 start,
            Vector3 landing,
            in CharacterPredictiveFootRootTrajectory rootTrajectory,
            Vector3 predictedHip,
            FootPredictionRejectReason rejectReason,
            in CharacterPredictiveFootPlacementQueryResult query)
        {
            RequireIdentity(sequence, generatedFrame, in step);
            if (rejectReason == FootPredictionRejectReason.None)
                throw new ArgumentOutOfRangeException(nameof(rejectReason));
            AssignEvent(sequence, generatedFrame, in step);
            State = CharacterPredictiveFootPlanState.Rejected;
            TransitionReason = CharacterPredictiveFootPlanTransitionReason.PlanRejected;
            Start = start;
            Landing = landing;
            AssignTiming(in step, rootTrajectory.PathStartPhase);
            AssignTrajectory(in rootTrajectory, in step);
            PredictedHip = predictedHip;
            FutureSupport = query.FutureLandingSupport;
            FutureLandingRequest = query.FutureLandingRequest;
            VirtualGroundSplitFraction = query.VirtualGroundSplitFraction;
            VirtualGroundSplitSupport = query.VirtualGroundSplitSupport;
            VirtualGroundSplitLandingEventIdentity = query.VirtualGroundSplitLandingEventIdentity;
            GroundEnvelopeSegmentCount = query.GroundEnvelope.Count;
            GroundEnvelopeRejectReason = query.GroundEnvelope.RejectReason;
            QueryCount = query.QueryCount;
            RawHitCount = query.RawHitCount;
            RouteSampleCount = query.RouteSampleCount;
            AcceptedHitCount = query.AcceptedHitCount;
            EdgePlaneCandidateCount = query.EdgePlaneCandidateCount;
            AcceptedEdgePlaneCount = query.AcceptedEdgePlaneCount;
            RejectedQueryCount = query.RejectedCount;
            QueryRejectCounts = query.RejectCounts;
            CreationRejectReason = rejectReason;
            CopyQuerySnapshots(in query);
            GeometrySnapshot = BuildGeometrySnapshot();
        }

        internal void SynchronizeActionClock(
            ulong renderFrame,
            in AnimationPredictedFootStepSample step)
        {
            if (!HasExecutablePath)
                return;
            if (renderFrame == 0 || renderFrame <= ActionClockFrame ||
                !MatchesAuthoritativeEvent(in step))
            {
                throw new ArgumentException("Predictive Foot Plan action clock input is invalid.");
            }
            AnimationActionStepClockSample clock = step.ActionStepClock;
            if (!float.IsFinite(clock.DurationSeconds) || clock.DurationSeconds <= 0f ||
                clock.Phase + 0.00001f < ActionStepPhase)
            {
                Complete(CharacterPredictiveFootPlanEndReason.ActionClockInvalid);
                return;
            }
            ActionStepDurationSeconds = clock.DurationSeconds;
            ActionStepPhase = clock.Phase;
            Progress = Mathf.Clamp01(
                (ActionStepPhase - RootTrajectory.PathStartPhase) /
                Mathf.Max(0.000001f, 1f - RootTrajectory.PathStartPhase));
            ActionClockFrame = renderFrame;
            if (ActionStepPhase + 0.000001f < RootTrajectory.PathStartPhase)
            {
                Progress = 0f;
                return;
            }
            if (State == CharacterPredictiveFootPlanState.Planned)
            {
                State = CharacterPredictiveFootPlanState.Executing;
                TransitionReason = CharacterPredictiveFootPlanTransitionReason.PlanExecutionStarted;
            }
        }

        internal bool ShouldInterruptWorldMotion(Vector3 worldMotion)
        {
            if (!HasExecutablePath || !RootTrajectory.HasPlanarMotion)
                return false;
            if (!float.IsFinite(worldMotion.x) ||
                !float.IsFinite(worldMotion.y) ||
                !float.IsFinite(worldMotion.z))
                return true;
            Vector3 currentDirection = Vector3.ProjectOnPlane(worldMotion, RootTrajectory.Up);
            if (currentDirection.sqrMagnitude <= 0.000001f)
                return true;
            Vector3 plannedDirection = RootTrajectory.FrozenPlanarVelocity;
            return plannedDirection.sqrMagnitude <= 0.000001f ||
                   Vector3.Dot(plannedDirection.normalized, currentDirection.normalized) <= 0f;
        }

        internal void InterruptWorldMotion()
        {
            if (!HasExecutablePath)
                return;
            Complete(CharacterPredictiveFootPlanEndReason.ActionInterrupted);
        }

        internal void Evaluate(
            float progress,
            out Vector3 pathPosition,
            out Vector3 rootPosition,
            out Vector3 hipPosition,
            out FootPlacementSurface support)
        {
            if (!HasExecutablePath || GroundEnvelopeSegmentCount <= 0)
                throw new InvalidOperationException("Predictive Foot Plan has no executable path.");
            float value = Mathf.Clamp01(progress);
            FootPlacementGroundEnvelopeSegment segment = m_PathSegments[0];
            for (int i = 0; i < GroundEnvelopeSegmentCount; i++)
            {
                if (value > m_PathSegments[i].EndFraction)
                    continue;
                segment = m_PathSegments[i];
                break;
            }
            float length = segment.EndFraction - segment.StartFraction;
            float t = length > 0.000001f
                ? Mathf.Clamp01((value - segment.StartFraction) / length)
                : 1f;
            pathPosition = Vector3.Lerp(segment.EdgeStart, segment.EdgeEnd, t);
            rootPosition = Vector3.Lerp(segment.RootStart, segment.RootEnd, t);
            hipPosition = Vector3.Lerp(segment.HipStart, segment.HipEnd, t);
            support = segment.Surface;
        }

        internal void EvaluateFootMotion(
            float progress,
            Vector3 mappedRoot,
            out Vector3 planarSole,
            out float animationClearanceHeight,
            out AnimationFootConstraintMode constraintMode,
            out AnimationFootSupportPhase supportPhase,
            out AnimationFootOrientationPolicy orientationPolicy,
            out AnimationBodyRotationPivotMode bodyPivotMode)
        {
            float eventPhase = Mathf.Lerp(
                RootTrajectory.PathStartPhase,
                1f,
                Mathf.Clamp01(progress));
            planarSole = RootTrajectory.EvaluateFootRoute(eventPhase);
            animationClearanceHeight = Mathf.Max(
                0f,
                EvaluateFloatRoute(AnimationClearanceHeights, eventPhase) +
                EvaluateAnimationClearanceContinuity(progress));
            constraintMode = (AnimationFootConstraintMode)EvaluateByteRoute(ConstraintModes, eventPhase);
            supportPhase = (AnimationFootSupportPhase)EvaluateByteRoute(SupportPhases, eventPhase);
            orientationPolicy = (AnimationFootOrientationPolicy)EvaluateByteRoute(FootOrientationPolicies, eventPhase);
            bodyPivotMode = (AnimationBodyRotationPivotMode)EvaluateByteRoute(BodyRotationPivotModes, eventPhase);
        }

        internal void EvaluateClearancePath(
            float progress,
            out Vector3 groundPath,
            out Vector3 root,
            out Vector3 hip,
            out FootPlacementSurface support,
            out Vector3 sole)
        {
            Evaluate(progress, out Vector3 envelopePoint, out root, out hip, out support);
            EvaluateFootMotion(
                progress,
                root,
                out Vector3 planarSole,
                out float animationClearanceHeight,
                out _,
                out AnimationFootSupportPhase supportPhase,
                out _,
                out _);
            Vector3 up = RootTrajectory.Up;
            groundPath = planarSole + up * (
                Vector3.Dot(envelopePoint, up) -
                Vector3.Dot(planarSole, up));
            sole = groundPath + up * animationClearanceHeight;
            if (support.IsValid && supportPhase == AnimationFootSupportPhase.Unsupported)
                support = new FootPlacementSurface(support.Collider, groundPath, up);
        }

        internal void EvaluateActionState(
            out AnimationFootConstraintMode constraintMode,
            out AnimationFootSupportPhase supportPhase,
            out AnimationFootOrientationPolicy orientationPolicy,
            out AnimationBodyRotationPivotMode bodyPivotMode)
        {
            if (!OwnsEvent)
                throw new InvalidOperationException("Predictive Foot Plan action state is unavailable.");
            constraintMode = (AnimationFootConstraintMode)EvaluateByteRoute(
                ConstraintModes,
                ActionStepPhase);
            supportPhase = (AnimationFootSupportPhase)EvaluateByteRoute(
                SupportPhases,
                ActionStepPhase);
            orientationPolicy = (AnimationFootOrientationPolicy)EvaluateByteRoute(
                FootOrientationPolicies,
                ActionStepPhase);
            bodyPivotMode = (AnimationBodyRotationPivotMode)EvaluateByteRoute(
                BodyRotationPivotModes,
                ActionStepPhase);
        }

        internal float EvaluateCurrentAnimationClearanceHeight()
        {
            if (!HasExecutablePath)
                throw new InvalidOperationException("Predictive Foot Plan clearance is unavailable.");
            return Mathf.Max(
                0f,
                EvaluateFloatRoute(AnimationClearanceHeights, ActionStepPhase) +
                EvaluateAnimationClearanceContinuity(Progress));
        }

        internal FootPlacementGroundEnvelopeSegment GetPathSegment(int index)
        {
            if (index < 0 || index >= GroundEnvelopeSegmentCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_PathSegments[index];
        }

        internal Vector3 GetPlannedFootRouteSample(int index)
        {
            if (!OwnsEvent || index < 0 || index >= FrozenWorldFootRoute.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return FrozenWorldFootRoute[index];
        }

        internal void Reset(CharacterPredictiveFootPlanEndReason reason)
        {
            if (reason == CharacterPredictiveFootPlanEndReason.None)
                throw new ArgumentOutOfRangeException(nameof(reason));
            State = CharacterPredictiveFootPlanState.Inactive;
            TransitionReason = CharacterPredictiveFootPlanTransitionReason.PlanEnded;
            EndReason = reason;
            Sequence = 0;
            LandingEventIdentity = 0;
            SourceSampleIdentity = 0;
            SourceSampleCycle = 0;
            EventOrdinal = 0;
            ContributionContinuityIdentity = 0;
            GeneratedFrame = 0;
            Start = Vector3.zero;
            Landing = Vector3.zero;
            RootStart = Vector3.zero;
            RootStartRotation = Quaternion.identity;
            RootLanding = Vector3.zero;
            RootLandingRotation = Quaternion.identity;
            PredictedHip = Vector3.zero;
            RootTrajectory = default;
            AuthoredFootPlanarRoute = default;
            RootLocalHipRoute = default;
            AnimationClearanceHeights = default;
            ConstraintModes = default;
            SupportPhases = default;
            FootOrientationPolicies = default;
            BodyRotationPivotModes = default;
            FrozenWorldFootRoute = default;
            AnimationClearanceContinuityOffset = 0f;
            LandingDelayAtGeneration = 0f;
            EventPhaseAtGeneration = 0f;
            LiftOffPhase = 0f;
            ActionStepDurationSeconds = 0f;
            ActionStepPhase = 0f;
            Progress = 0f;
            ActionClockFrame = 0;
            FutureSupport = default;
            FutureLandingRequest = default;
            VirtualGroundSplitFraction = 0f;
            VirtualGroundSplitSupport = default;
            VirtualGroundSplitLandingEventIdentity = 0;
            GroundEnvelopeSegmentCount = 0;
            GroundEnvelopeRejectReason = FootPlacementGroundEnvelopeRejectReason.None;
            QueryCount = 0;
            RawHitCount = 0;
            RouteSampleCount = 0;
            AcceptedHitCount = 0;
            EdgePlaneCandidateCount = 0;
            AcceptedEdgePlaneCount = 0;
            RejectedQueryCount = 0;
            QueryRejectCounts = default;
            CreationRejectReason = FootPredictionRejectReason.None;
            QueryRequestSnapshotCount = 0;
            AcceptedSupportSnapshotCount = 0;
            RejectedGeometrySnapshotCount = 0;
            GeometrySnapshot = null;
        }

        void CopyQuerySnapshots(in CharacterPredictiveFootPlacementQueryResult query)
        {
            QueryRequestSnapshotCount = Copy(
                query.QueryRequests,
                query.QueryRequestCount,
                m_QueryRequests);
            AcceptedSupportSnapshotCount = Copy(
                query.AcceptedSupports,
                query.AcceptedSupportCount,
                m_AcceptedSupports);
            RejectedGeometrySnapshotCount = Copy(
                query.RejectedGeometry,
                query.RejectedGeometryCount,
                m_RejectedGeometry);
        }

        CharacterPredictiveFootPlanGeometrySnapshot BuildGeometrySnapshot()
        {
            int footRouteCount = RouteSampleCount >= 2
                ? Mathf.Min(
                    RouteSampleCount,
                    CharacterPredictiveFootPlacementQuery.MaximumRouteSampleCount)
                : 0;
            var footRoute = new CharacterPredictiveFootRoutePointSnapshot[footRouteCount];
            for (int i = 0; i < footRoute.Length; i++)
            {
                float progress = i / (footRoute.Length - 1f);
                float eventPhase = Mathf.Lerp(RootTrajectory.PathStartPhase, 1f, progress);
                footRoute[i] = new CharacterPredictiveFootRoutePointSnapshot(
                    progress,
                    RootTrajectory.EvaluateFootRoute(eventPhase));
            }
            int envelopeCount = HasExecutablePath ? GroundEnvelopeSegmentCount : 0;
            var envelope = new CharacterPredictiveFootEnvelopeSegmentSnapshot[envelopeCount];
            for (int i = 0; i < envelope.Length; i++)
                envelope[i] = new CharacterPredictiveFootEnvelopeSegmentSnapshot(in m_PathSegments[i]);
            CharacterPredictiveFootClearanceSegmentSnapshot[] clearancePath =
                BuildClearancePathSnapshot();
            var queryRequests = new CharacterPredictiveFootQueryRequestSnapshot[QueryRequestSnapshotCount];
            Array.Copy(m_QueryRequests, queryRequests, queryRequests.Length);
            var acceptedSupports = new CharacterPredictiveFootQueryGeometrySnapshot[AcceptedSupportSnapshotCount];
            Array.Copy(m_AcceptedSupports, acceptedSupports, acceptedSupports.Length);
            var rejectedGeometry = new CharacterPredictiveFootQueryGeometrySnapshot[RejectedGeometrySnapshotCount];
            Array.Copy(m_RejectedGeometry, rejectedGeometry, rejectedGeometry.Length);
            return new CharacterPredictiveFootPlanGeometrySnapshot(
                m_Side,
                Sequence,
                GeneratedFrame,
                LandingEventIdentity,
                HasExecutablePath,
                HasExecutablePath && FutureSupport.IsValid,
                HasExecutablePath && FutureSupport.IsValid ? Landing : Vector3.zero,
                HasExecutablePath && VirtualGroundSplitSupport.IsValid,
                VirtualGroundSplitFraction,
                VirtualGroundSplitLandingEventIdentity,
                HasExecutablePath && VirtualGroundSplitSupport.IsValid
                    ? VirtualGroundSplitSupport.Point
                    : Vector3.zero,
                footRoute,
                clearancePath,
                envelope,
                queryRequests,
                acceptedSupports,
                rejectedGeometry);
        }

        CharacterPredictiveFootClearanceSegmentSnapshot[] BuildClearancePathSnapshot()
        {
            if (!HasExecutablePath || GroundEnvelopeSegmentCount <= 0)
                return Array.Empty<CharacterPredictiveFootClearanceSegmentSnapshot>();
            int uniformCount = Mathf.Clamp(
                RouteSampleCount,
                2,
                CharacterPredictiveFootPlacementQuery.MaximumRouteSampleCount);
            var fractions = new List<float>(uniformCount + GroundEnvelopeSegmentCount * 2);
            for (int i = 0; i < uniformCount; i++)
                fractions.Add(i / (uniformCount - 1f));
            for (int i = 0; i < GroundEnvelopeSegmentCount; i++)
            {
                fractions.Add(m_PathSegments[i].StartFraction);
                fractions.Add(m_PathSegments[i].EndFraction);
            }
            fractions.Sort();
            var uniqueFractions = new List<float>(fractions.Count);
            float previous = -1f;
            for (int i = 0; i < fractions.Count; i++)
            {
                float fraction = Mathf.Clamp01(fractions[i]);
                if (previous >= 0f && Mathf.Abs(fraction - previous) <= 0.00001f)
                    continue;
                uniqueFractions.Add(fraction);
                previous = fraction;
            }
            var segments = new CharacterPredictiveFootClearanceSegmentSnapshot[
                Mathf.Max(0, uniqueFractions.Count - 1)];
            for (int i = 0; i < segments.Length; i++)
            {
                float startFraction = uniqueFractions[i];
                float endFraction = uniqueFractions[i + 1];
                EvaluateClearancePath(
                    startFraction,
                    out _,
                    out Vector3 rootStart,
                    out Vector3 hipStart,
                    out FootPlacementSurface startSupport,
                    out Vector3 start);
                EvaluateClearancePath(
                    endFraction,
                    out _,
                    out Vector3 rootEnd,
                    out Vector3 hipEnd,
                    out FootPlacementSurface endSupport,
                    out Vector3 end);
                FootPlacementSurface surface = endSupport.IsValid ? endSupport : startSupport;
                segments[i] = new CharacterPredictiveFootClearanceSegmentSnapshot(
                    startFraction,
                    endFraction,
                    start,
                    end,
                    surface,
                    rootStart,
                    rootEnd,
                    hipStart,
                    hipEnd,
                    Vector3.Dot(start, RootTrajectory.Up),
                    Vector3.Dot(end, RootTrajectory.Up));
            }
            return segments;
        }

        static int Copy<T>(T[] source, int count, T[] destination)
        {
            if (destination == null || count < 0 || count > destination.Length ||
                (count > 0 && (source == null || count > source.Length)))
                throw new ArgumentException("Predictive Foot query snapshot copy is invalid.");
            if (count > 0)
                Array.Copy(source, destination, count);
            return count;
        }

        void AssignTrajectory(
            in CharacterPredictiveFootRootTrajectory rootTrajectory,
            in AnimationPredictedFootStepSample step)
        {
            if (step.AuthoredFootPlanarRoute.Length != AnimationPredictedFootStepCurveSet.RouteSampleCount ||
                step.RootLocalHipRoute.Length != AnimationPredictedFootStepCurveSet.RouteSampleCount ||
                step.AnimationClearanceHeights.Length != AnimationPredictedFootStepCurveSet.RouteSampleCount)
            {
                throw new ArgumentException("Predictive Foot Plan action route is invalid.");
            }
            RootTrajectory = rootTrajectory;
            AuthoredFootPlanarRoute = step.AuthoredFootPlanarRoute;
            RootLocalHipRoute = step.RootLocalHipRoute;
            AnimationClearanceHeights = step.AnimationClearanceHeights;
            ConstraintModes = step.ConstraintModes;
            SupportPhases = step.SupportPhases;
            FootOrientationPolicies = step.FootOrientationPolicies;
            BodyRotationPivotModes = step.BodyRotationPivotModes;
            var frozenWorldFootRoute = new FixedList128Bytes<Vector3>();
            for (int i = 0; i < AuthoredFootPlanarRoute.Length; i++)
            {
                float progress = i / (AuthoredFootPlanarRoute.Length - 1f);
                float eventPhase = Mathf.Lerp(rootTrajectory.PathStartPhase, 1f, progress);
                frozenWorldFootRoute.Add(rootTrajectory.EvaluateFootRoute(eventPhase));
            }
            FrozenWorldFootRoute = frozenWorldFootRoute;
            RootStart = rootTrajectory.StartPosition;
            RootStartRotation = rootTrajectory.StartRotation;
            rootTrajectory.EvaluateEventPhase(1f, out Vector3 rootLanding, out Quaternion rootLandingRotation);
            RootLanding = rootLanding;
            RootLandingRotation = rootLandingRotation;
        }

        void ResolveAnimationClearanceContinuity()
        {
            AnimationClearanceContinuityOffset = 0f;
            if (State != CharacterPredictiveFootPlanState.Executing ||
                GroundEnvelopeSegmentCount <= 0 ||
                EventPhaseAtGeneration + 0.000001f < RootTrajectory.PathStartPhase)
            {
                return;
            }
            Vector3 up = RootTrajectory.Up;
            float nativeClearance = Vector3.Dot(
                RootTrajectory.NativeSoleAtGeneration - m_PathSegments[0].EdgeStart,
                up);
            float authoredClearance = EvaluateFloatRoute(
                AnimationClearanceHeights,
                EventPhaseAtGeneration);
            float offset = nativeClearance - authoredClearance;
            if (!float.IsFinite(offset))
                throw new InvalidOperationException("Predictive Foot Plan clearance continuity is invalid.");
            AnimationClearanceContinuityOffset = offset;
        }

        float EvaluateAnimationClearanceContinuity(float progress)
        {
            float value = Mathf.Clamp01(progress);
            float blend = 1f - value * value * (3f - 2f * value);
            return AnimationClearanceContinuityOffset * blend;
        }

        void AssignEvent(
            ulong sequence,
            ulong generatedFrame,
            in AnimationPredictedFootStepSample step)
        {
            Sequence = sequence;
            LandingEventIdentity = step.LandingEventIdentity;
            SourceSampleIdentity = step.SourceSampleIdentity;
            SourceSampleCycle = step.SourceSampleCycle;
            EventOrdinal = step.EventOrdinal;
            ContributionContinuityIdentity = step.ContributionContinuityIdentity;
            GeneratedFrame = generatedFrame;
            EndReason = CharacterPredictiveFootPlanEndReason.None;
        }

        void Complete(CharacterPredictiveFootPlanEndReason reason)
        {
            if (reason == CharacterPredictiveFootPlanEndReason.None)
                throw new ArgumentOutOfRangeException(nameof(reason));
            State = CharacterPredictiveFootPlanState.Completed;
            TransitionReason = CharacterPredictiveFootPlanTransitionReason.PlanEnded;
            EndReason = reason;
        }

        void AssignTiming(in AnimationPredictedFootStepSample step, float pathStartPhase)
        {
            LandingDelayAtGeneration = step.ActionStepClock.TimeToLandingSeconds;
            EventPhaseAtGeneration = step.ActionStepClock.Phase;
            LiftOffPhase = step.ActionStepClock.LiftOffPhase;
            ActionStepDurationSeconds = step.ActionStepClock.DurationSeconds;
            ActionStepPhase = EventPhaseAtGeneration;
            Progress = 0f;
            ActionClockFrame = GeneratedFrame;
        }

        static float EvaluateFloatRoute(FixedList128Bytes<float> route, float phase)
        {
            EvaluateRouteIndices(route.Length, phase, out int first, out int second, out float t);
            return Mathf.Lerp(route[first], route[second], t);
        }

        static byte EvaluateByteRoute(FixedList32Bytes<byte> route, float phase)
        {
            EvaluateRouteIndices(route.Length, phase, out int first, out int second, out float t);
            return t < 0.5f ? route[first] : route[second];
        }

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        static void EvaluateRouteIndices(
            int count,
            float phase,
            out int first,
            out int second,
            out float t)
        {
            if (count != AnimationPredictedFootStepCurveSet.RouteSampleCount)
                throw new InvalidOperationException("Predictive Foot Plan action route is unavailable.");
            float scaled = Mathf.Clamp01(phase) * (count - 1);
            first = Mathf.Min(count - 1, Mathf.FloorToInt(scaled));
            second = Mathf.Min(count - 1, first + 1);
            t = scaled - first;
        }

        static void RequireIdentity(
            ulong sequence,
            ulong generatedFrame,
            in AnimationPredictedFootStepSample step)
        {
            if (sequence == 0 || generatedFrame == 0 || !step.IsAuthoritative)
                throw new ArgumentException("Predictive Foot Plan identity is invalid.");
        }
    }
}
