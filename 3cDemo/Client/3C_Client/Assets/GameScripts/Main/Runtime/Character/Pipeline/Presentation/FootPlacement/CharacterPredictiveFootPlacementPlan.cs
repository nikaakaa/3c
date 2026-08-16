using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal readonly struct CharacterPredictiveFootRootTrajectory
    {
        internal CharacterPredictiveFootRootTrajectory(
            Vector3 startPosition,
            Quaternion startRotation,
            Vector3 presentedBodyStartPosition,
            Vector3 routeStart,
            Vector3 executionSoleAtGeneration,
            Vector3 committedBodyVelocity,
            float trajectoryCurvatureDegreesPerSecond,
            bool trajectoryCurvatureAvailable,
            in CommittedLocomotionPlanarMotionTimeline motionTimeline,
            double movementPlaybackTime,
            CharacterFutureBodyTrajectory futureBodyTrajectory,
            Vector3 up,
            in AnimationPredictedFootStepSample step)
        {
            if (!step.IsAuthoritative)
                throw new ArgumentException("Predictive Foot Root Trajectory requires an authoritative event.", nameof(step));
            if (!IsFinite(startPosition) || !IsFinite(startRotation) ||
                !IsFinite(presentedBodyStartPosition) ||
                !IsFinite(routeStart) ||
                !IsFinite(executionSoleAtGeneration) ||
                !IsFinite(committedBodyVelocity) ||
                !float.IsFinite(trajectoryCurvatureDegreesPerSecond) ||
                !motionTimeline.IsValid || !double.IsFinite(movementPlaybackTime) || movementPlaybackTime < 0d ||
                futureBodyTrajectory == null ||
                !IsFinite(up) || up.sqrMagnitude <= 0.000001f)
            {
                throw new ArgumentException("Predictive Foot Root Trajectory origin is invalid.");
            }
            StartPosition = startPosition;
            StartRotation = startRotation.normalized;
            PresentedBodyStartPosition = presentedBodyStartPosition;
            ExecutionSoleAtGeneration = executionSoleAtGeneration;
            Up = up.normalized;
            Vector3 currentVelocity = Vector3.ProjectOnPlane(
                committedBodyVelocity,
                Up);
            Vector3 continuationVelocity = Vector3.ProjectOnPlane(
                new Vector3(motionTimeline.ContinuationVelocityX, 0f, motionTimeline.ContinuationVelocityZ),
                Up);
            Vector3 motionVelocity = Vector3.ProjectOnPlane(
                new Vector3(motionTimeline.CurrentVelocityX, 0f, motionTimeline.CurrentVelocityZ),
                Up);
            float switchDelay = motionTimeline.CurrentSegmentDurationTicks > 0
                ? Mathf.Max(0f, (float)(motionTimeline.CurrentSegmentDurationSeconds - movementPlaybackTime))
                : float.PositiveInfinity;
            bool hasContinuation = motionTimeline.HasContinuation;
            FrozenPlanarVelocity = currentVelocity;
            FrozenMotionPlanarVelocity = motionVelocity;
            FrozenTrajectoryCurvatureDegreesPerSecond =
                trajectoryCurvatureDegreesPerSecond;
            FrozenTrajectoryCurvatureAvailable = trajectoryCurvatureAvailable;
            ContinuationPlanarVelocity = continuationVelocity;
            CurrentSegmentSwitchDelaySeconds = switchDelay;
            HasContinuation = hasContinuation;
            FrozenYawVelocityDegreesPerSecond = futureBodyTrajectory
                .Evaluate(step.PredictionLeadSeconds)
                .YawVelocityDegreesPerSecond;
            FrozenMaximumYawVelocityDegreesPerSecond =
                motionTimeline.MaximumYawVelocityDegreesPerSecond;
            PredictionLeadSeconds = step.PredictionLeadSeconds;
            EventPhaseAtGeneration = step.ActionStepClock.Phase;
            LiftOffPhase = step.ActionStepClock.LiftOffPhase;
            PathStartPhase = Mathf.Max(EventPhaseAtGeneration, LiftOffPhase);
            LandingDelayAtGeneration = step.ActionStepClock.TimeToLandingSeconds;
            ActionStepDurationSeconds = step.ActionStepClock.DurationSeconds;
            Step = step;
            FutureBodyTrajectory = futureBodyTrajectory;
            FootRoutePlanarAlignment = Vector3.zero;
            FootRoutePlanarAlignment = Vector3.ProjectOnPlane(
                routeStart - EvaluateUnalignedFootRoute(PathStartPhase),
                Up);
            if (futureBodyTrajectory.DurationSeconds + 0.0001f <
                PredictionLeadSeconds + LandingDelayAtGeneration)
            {
                throw new ArgumentException("Future Body Trajectory does not cover the landing event.", nameof(futureBodyTrajectory));
            }
        }

        internal Vector3 StartPosition { get; }
        internal Quaternion StartRotation { get; }
        internal Vector3 PresentedBodyStartPosition { get; }
        internal Vector3 ExecutionSoleAtGeneration { get; }
        internal Vector3 Up { get; }
        internal Vector3 FrozenPlanarVelocity { get; }
        internal Vector3 FrozenMotionPlanarVelocity { get; }
        internal float FrozenTrajectoryCurvatureDegreesPerSecond { get; }
        internal bool FrozenTrajectoryCurvatureAvailable { get; }
        internal Vector3 ContinuationPlanarVelocity { get; }
        internal float FrozenYawVelocityDegreesPerSecond { get; }
        internal float FrozenMaximumYawVelocityDegreesPerSecond { get; }
        internal float CurrentSegmentSwitchDelaySeconds { get; }
        internal bool HasContinuation { get; }
        internal float ActionStepDurationSeconds { get; }
        internal float EventPhaseAtGeneration { get; }
        internal float LiftOffPhase { get; }
        internal float PathStartPhase { get; }
        internal float LandingDelayAtGeneration { get; }
        internal float PredictionLeadSeconds { get; }
        internal Vector3 FootRoutePlanarAlignment { get; }
        internal bool HasPlanarMotion =>
            FrozenPlanarVelocity.sqrMagnitude > 0.000001f ||
            HasContinuation && ContinuationPlanarVelocity.sqrMagnitude > 0.000001f;
        readonly AnimationPredictedFootStepSample Step { get; }
        readonly CharacterFutureBodyTrajectory FutureBodyTrajectory;
        internal void EvaluateSwing(float progress, out Vector3 position, out Quaternion rotation)
        {
            float eventPhase = Mathf.Lerp(PathStartPhase, 1f, Mathf.Clamp01(progress));
            EvaluateEventPhase(eventPhase, out position, out rotation);
        }

        internal void EvaluateEventPhase(float eventPhase, out Vector3 position, out Quaternion rotation)
        {
            float phase = Mathf.Clamp01(eventPhase);
            float elapsedSeconds = ResolveRawTravelElapsedSeconds(phase);
            position = StartPosition + ResolvePlanarTravel(elapsedSeconds);
            rotation = EvaluateRotation(elapsedSeconds);
        }

        internal Vector3 EvaluateFootRoute(float eventPhase)
        {
            return EvaluateUnalignedFootRoute(eventPhase) + FootRoutePlanarAlignment;
        }

        Vector3 EvaluateUnalignedFootRoute(float eventPhase)
        {
            float phase = Mathf.Clamp01(eventPhase);
            float elapsedSeconds = ResolveRawTravelElapsedSeconds(phase);
            Quaternion rotation = EvaluateRotation(elapsedSeconds);
            Vector3 root = StartPosition + ResolvePlanarTravel(elapsedSeconds);
            Vector3 localFoot = Step.EvaluateRootLocalFootRoute(phase);
            Vector3 localPlanar = Step.EvaluateAuthoredFootPlanarRoute(phase);
            float virtualGroundHeight = localFoot.y - Step.EvaluateAnimationClearanceHeight(phase);
            return root + rotation * new Vector3(
                localPlanar.x,
                virtualGroundHeight,
                localPlanar.z);
        }

        internal float EvaluateRemainingPlanarDistance(float eventPhase)
        {
            Vector3 current = ResolvePlanarTravel(
                ResolveRawTravelElapsedSeconds(Mathf.Clamp01(eventPhase)));
            Vector3 landing = ResolvePlanarTravel(ResolveRawTravelElapsedSeconds(1f));
            return Vector3.ProjectOnPlane(landing - current, Up).magnitude;
        }

        internal Vector3 EvaluatePresentedBodyPositionAtEventPhase(float eventPhase)
        {
            float elapsedSeconds = ResolveElapsedSinceGeneration(eventPhase);
            return PresentedBodyStartPosition + ResolvePlanarTravel(elapsedSeconds);
        }

        internal Vector3 EvaluatePresentedBodyVelocityAtEventPhase(float eventPhase)
        {
            float elapsedSinceGeneration = ResolveElapsedSinceGeneration(eventPhase);
            CharacterFutureBodyTrajectorySample sample = FutureBodyTrajectory.Evaluate(
                elapsedSinceGeneration);
            return new Vector3(sample.VelocityX, sample.VelocityY, sample.VelocityZ);
        }

        internal Vector3 EvaluatePresentedBodyLandingPosition() =>
            PresentedBodyStartPosition + ResolvePlanarTravel(
                PredictionLeadSeconds + LandingDelayAtGeneration);

        internal Vector3 EvaluateHipRoute(float eventPhase)
        {
            float phase = Mathf.Clamp01(eventPhase);
            float elapsedSeconds = ResolveRawTravelElapsedSeconds(phase);
            Quaternion rotation = EvaluateRotation(elapsedSeconds);
            return StartPosition + ResolvePlanarTravel(elapsedSeconds) +
                   rotation * Step.EvaluateRootLocalHipRoute(phase);
        }

        internal Vector3 EvaluateAnkleRoute(float eventPhase)
        {
            float phase = Mathf.Clamp01(eventPhase);
            float elapsedSeconds = ResolveRawTravelElapsedSeconds(phase);
            Quaternion rotation = EvaluateRotation(elapsedSeconds);
            return StartPosition + ResolvePlanarTravel(elapsedSeconds) +
                   rotation * Step.EvaluateRootLocalAnkleRoute(phase);
        }

        internal Vector3 EvaluateSoleToAnkle(float eventPhase)
        {
            float phase = Mathf.Clamp01(eventPhase);
            Quaternion rotation = EvaluateRotation(ResolveRawTravelElapsedSeconds(phase));
            Vector3 authored = Step.EvaluateRootLocalAnkleRoute(phase) -
                               Step.EvaluateRootLocalFootRoute(phase);
            return rotation * authored;
        }

        internal float EvaluateAuthoredReach(float eventPhase) =>
            Vector3.Distance(EvaluateHipRoute(eventPhase), EvaluateAnkleRoute(eventPhase));

        internal float EvaluateConstraintWeight(float eventPhase) =>
            Step.EvaluateConstraintWeight(Mathf.Clamp01(eventPhase));

        internal float EvaluateSupportWeight(float eventPhase) =>
            Step.EvaluateSupportWeight(Mathf.Clamp01(eventPhase));

        Quaternion EvaluateRotation(float elapsedSeconds)
        {
            float yawDegrees = FutureBodyTrajectory
                .Evaluate(elapsedSeconds)
                .RelativeYawDegrees;
            return (Quaternion.AngleAxis(yawDegrees, Up) * StartRotation).normalized;
        }

        internal Vector3 EvaluateRemainingPlannedIntentDisplacement(float eventPhase)
        {
            float startSeconds = ResolveRawTravelElapsedSeconds(Mathf.Clamp01(eventPhase));
            float endSeconds = ResolveRawTravelElapsedSeconds(1f);
            if (endSeconds <= startSeconds)
                return Vector3.zero;
            return ResolvePlanarTravel(endSeconds) - ResolvePlanarTravel(startSeconds);
        }

        float ResolveRawTravelElapsedSeconds(float phase)
        {
            return Mathf.Max(
                0f,
                PredictionLeadSeconds +
                (phase - EventPhaseAtGeneration) * ActionStepDurationSeconds);
        }

        float ResolveElapsedSinceGeneration(float eventPhase)
        {
            return Mathf.Max(
                0f,
                (Mathf.Clamp01(eventPhase) - EventPhaseAtGeneration) *
                ActionStepDurationSeconds);
        }

        internal bool CanCoverEventPhase(float eventPhase)
        {
            float elapsedSeconds = ResolveRawTravelElapsedSeconds(Mathf.Clamp01(eventPhase));
            return elapsedSeconds <= FutureBodyTrajectory.DurationSeconds + 0.0001f;
        }

        Vector3 ResolvePlanarTravel(float elapsedSeconds)
        {
            CharacterFutureBodyTrajectorySample sample = FutureBodyTrajectory.Evaluate(elapsedSeconds);
            return new Vector3(
                sample.RelativePositionX,
                sample.RelativePositionY,
                sample.RelativePositionZ);
        }

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        static bool IsFinite(Quaternion value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) &&
            float.IsFinite(value.z) && float.IsFinite(value.w) &&
            Quaternion.Dot(value, value) > 0.000001f;

    }

    internal readonly struct CharacterPredictiveBodySupportPath
    {
        internal CharacterPredictiveBodySupportPath(
            float startPhase,
            Vector3 up,
            Vector3 startRoot,
            Vector3 startHip,
            bool hasSplit,
            float splitPhase,
            Vector3 splitRoot,
            Vector3 splitHip,
            Vector3 endRoot,
            Vector3 endHip)
        {
            if (!float.IsFinite(startPhase) || startPhase < 0f || startPhase >= 1f ||
                !IsFinite(up) || up.sqrMagnitude <= 0.000001f ||
                !IsFinite(startRoot) || !IsFinite(startHip) ||
                !IsFinite(endRoot) || !IsFinite(endHip) ||
                hasSplit &&
                (!float.IsFinite(splitPhase) ||
                 splitPhase <= startPhase || splitPhase >= 1f ||
                 !IsFinite(splitRoot) || !IsFinite(splitHip)))
            {
                throw new ArgumentException("Predictive Body Support Path is invalid.");
            }
            IsValid = true;
            StartPhase = startPhase;
            Up = up.normalized;
            StartRootHeight = Vector3.Dot(startRoot, Up);
            StartHipHeight = Vector3.Dot(startHip, Up);
            HasSplit = hasSplit;
            SplitPhase = hasSplit ? splitPhase : 1f;
            SplitRootHeight = hasSplit ? Vector3.Dot(splitRoot, Up) : StartRootHeight;
            SplitHipHeight = hasSplit ? Vector3.Dot(splitHip, Up) : StartHipHeight;
            EndRootHeight = Vector3.Dot(endRoot, Up);
            EndHipHeight = Vector3.Dot(endHip, Up);
        }

        internal bool IsValid { get; }
        readonly float StartPhase;
        readonly Vector3 Up;
        readonly float StartRootHeight;
        readonly float StartHipHeight;
        readonly bool HasSplit;
        readonly float SplitPhase;
        readonly float SplitRootHeight;
        readonly float SplitHipHeight;
        readonly float EndRootHeight;
        readonly float EndHipHeight;

        internal void Evaluate(
            in CharacterPredictiveFootRootTrajectory rootTrajectory,
            float eventPhase,
            out Vector3 root,
            out Vector3 hip)
        {
            if (!IsValid)
                throw new InvalidOperationException("Predictive Body Support Path is unavailable.");
            float phase = Mathf.Clamp(eventPhase, StartPhase, 1f);
            rootTrajectory.EvaluateEventPhase(phase, out root, out _);
            hip = rootTrajectory.EvaluateHipRoute(phase);
            ResolveHeights(phase, out float rootHeight, out float hipHeight);
            root += Up * (rootHeight - Vector3.Dot(root, Up));
            hip += Up * (hipHeight - Vector3.Dot(hip, Up));
        }

        void ResolveHeights(float phase, out float rootHeight, out float hipHeight)
        {
            if (HasSplit && phase <= SplitPhase)
            {
                float progress = Mathf.InverseLerp(StartPhase, SplitPhase, phase);
                rootHeight = Mathf.Lerp(StartRootHeight, SplitRootHeight, progress);
                hipHeight = Mathf.Lerp(StartHipHeight, SplitHipHeight, progress);
                return;
            }
            float segmentStartPhase = HasSplit ? SplitPhase : StartPhase;
            float segmentStartRootHeight = HasSplit ? SplitRootHeight : StartRootHeight;
            float segmentStartHipHeight = HasSplit ? SplitHipHeight : StartHipHeight;
            float segmentProgress = Mathf.InverseLerp(segmentStartPhase, 1f, phase);
            rootHeight = Mathf.Lerp(segmentStartRootHeight, EndRootHeight, segmentProgress);
            hipHeight = Mathf.Lerp(segmentStartHipHeight, EndHipHeight, segmentProgress);
        }

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }

    internal sealed class CharacterPredictiveFootPlacementPlan
    {
        readonly CharacterFootSide m_Side;
        readonly FootPlacementGroundEnvelopeSegment[] m_PathSegments;
        readonly float[] m_QueryRouteEventPhases;
        readonly float[] m_QueryRouteFractions;
        readonly Vector3[] m_GroundProbeRoute;
        readonly float[] m_FootRateEventPhases;
        readonly float[] m_FootRateProgress;
        readonly CharacterPredictiveFootQueryRequestSnapshot[] m_QueryRequests;
        readonly CharacterPredictiveFootQueryGeometrySnapshot[] m_AcceptedSupports;
        readonly CharacterPredictiveFootQueryGeometrySnapshot[] m_RejectedGeometry;
        Vector3 m_WorldProjectionExpectedRoot;
        Vector3 m_WorldProjectionCurrentRoot;
        Quaternion m_WorldProjectionRotation = Quaternion.identity;
        bool m_HasWorldProjection;
        bool m_WorldProjectionFrozen;

        internal CharacterPredictiveFootPlacementPlan(CharacterFootSide side, int pathCapacity)
        {
            if (pathCapacity < 2)
                throw new ArgumentOutOfRangeException(nameof(pathCapacity));
            m_Side = side;
            m_PathSegments = new FootPlacementGroundEnvelopeSegment[pathCapacity];
            m_QueryRouteEventPhases = new float[CharacterPredictiveFootPlacementQuery.MaximumRouteSampleCount];
            m_QueryRouteFractions = new float[CharacterPredictiveFootPlacementQuery.MaximumRouteSampleCount];
            m_GroundProbeRoute = new Vector3[CharacterPredictiveFootPlacementQuery.MaximumRouteSampleCount];
            m_FootRateEventPhases = new float[CharacterPredictiveFootPlacementQuery.MaximumRouteSampleCount];
            m_FootRateProgress = new float[CharacterPredictiveFootPlacementQuery.MaximumRouteSampleCount];
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
        internal CharacterPredictiveBodySupportPath BodySupportPath { get; private set; }
        internal FixedList512Bytes<Vector3> AuthoredFootPlanarRoute { get; private set; }
        internal FixedList512Bytes<Vector3> RootLocalHipRoute { get; private set; }
        internal FixedList128Bytes<float> AnimationClearanceHeights { get; private set; }
        internal float ReleasePhase { get; private set; }
        internal float ApproachContactPhase { get; private set; }
        internal FixedList512Bytes<Vector3> FrozenWorldFootRoute { get; private set; }
        internal FixedList128Bytes<float> FrozenWorldFootRoutePhases { get; private set; }
        internal FixedList512Bytes<float> GroundPathRatePhases { get; private set; }
        internal FixedList512Bytes<float> GroundPathRates { get; private set; }
        internal float AnimationClearanceContinuityOffset { get; private set; }
        internal float LandingDelayAtGeneration { get; private set; }
        internal float EventPhaseAtGeneration { get; private set; }
        internal float LiftOffPhase { get; private set; }
        internal float ActionStepDurationSeconds { get; private set; }
        internal float ActionStepPhase { get; private set; }
        internal float ActionProgress { get; private set; }
        internal float GroundPathProgress { get; private set; }
        internal float MotionLinearLandingError { get; private set; }
        internal float MotionAngularLandingError { get; private set; }
        internal float MotionLandingError { get; private set; }
        internal float MotionLandingTolerance { get; private set; }
        internal float SoleSupportRadius { get; private set; }
        internal ulong ActionClockFrame { get; private set; }
        internal FootPlacementSurface FutureSupport { get; private set; }
        internal CharacterFootPlacementQueryRequest FutureLandingRequest { get; private set; }
        internal float VirtualGroundSplitEventPhase { get; private set; }
        internal Vector3 VirtualGroundOpposingLanding { get; private set; }
        internal Vector3 VirtualGroundSplitRoutePoint { get; private set; }
        internal float VirtualGroundSplitPlanarError { get; private set; }
        internal float VirtualGroundSplitFraction { get; private set; }
        internal FootPlacementSurface VirtualGroundSplitSupport { get; private set; }
        internal ulong VirtualGroundSplitLandingEventIdentity { get; private set; }
        internal int GroundEnvelopeSegmentCount { get; private set; }
        internal FootPlacementGroundEnvelopeRejectReason GroundEnvelopeRejectReason { get; private set; }
        internal int QueryCount { get; private set; }
        internal int RawHitCount { get; private set; }
        internal int RouteSampleCount { get; private set; }
        internal int FootRateSampleCount { get; private set; }
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
        internal Matrix4x4 WorldProjectionMatrix => m_HasWorldProjection
            ? Matrix4x4.TRS(
                  m_WorldProjectionCurrentRoot,
                  m_WorldProjectionRotation,
                  Vector3.one) *
              Matrix4x4.Translate(-m_WorldProjectionExpectedRoot)
            : Matrix4x4.identity;
        internal Vector3 ProjectedStart => ProjectWorldPoint(Start);
        internal Vector3 ProjectedLanding => ProjectWorldPoint(Landing);
        internal Vector3 ProjectedPredictedHip => ProjectWorldPoint(PredictedHip);
        internal Vector3 ProjectedRootStart => ProjectWorldPoint(RootStart);
        internal Quaternion ProjectedRootStartRotation =>
            (m_WorldProjectionRotation * RootStartRotation).normalized;
        internal Vector3 ProjectedRootLanding => ProjectWorldPoint(RootLanding);
        internal Vector3 ProjectedPresentedBodyLanding => ProjectWorldPoint(
            RootTrajectory.EvaluatePresentedBodyLandingPosition());
        internal FootPlacementSurface ProjectedFutureSupport => ProjectSurface(FutureSupport);
        internal Quaternion ProjectedRootLandingRotation =>
            (m_WorldProjectionRotation * RootLandingRotation).normalized;
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
                   EventOrdinal == step.EventOrdinal;
        }

        internal void SynchronizePoseContribution(in AnimationPredictedFootStepSample step)
        {
            if (!MatchesAuthoritativeEvent(in step))
                throw new ArgumentException("Predictive Foot Plan pose contribution does not belong to its action event.");
            ContributionContinuityIdentity = step.ContributionContinuityIdentity;
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
            AssignTiming(in step);
            State = step.ActionStepClock.Phase + 0.000001f < ReleasePhase
                ? CharacterPredictiveFootPlanState.Planned
                : CharacterPredictiveFootPlanState.Executing;
            TransitionReason = CharacterPredictiveFootPlanTransitionReason.PlanGenerated;
            AssignTrajectory(in rootTrajectory, in step);
            PredictedHip = predictedHip;
            FutureSupport = query.FutureLandingSupport;
            FutureLandingRequest = query.FutureLandingRequest;
            VirtualGroundSplitEventPhase = query.VirtualGroundSplitEventPhase;
            VirtualGroundOpposingLanding = query.VirtualGroundOpposingLanding;
            VirtualGroundSplitRoutePoint = query.VirtualGroundSplitRoutePoint;
            VirtualGroundSplitPlanarError = query.VirtualGroundSplitPlanarError;
            VirtualGroundSplitFraction = query.VirtualGroundSplitFraction;
            VirtualGroundSplitSupport = query.VirtualGroundSplitSupport;
            VirtualGroundSplitLandingEventIdentity = query.VirtualGroundSplitLandingEventIdentity;
            BodySupportPath = query.BodySupportPath;
            SoleSupportRadius = query.SoleSupportRadius;
            GroundEnvelopeSegmentCount = query.GroundEnvelope.CopyTo(m_PathSegments);
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
            ResolveGroundPathRates();
            GroundPathProgress = 0f;
            ResolveAnimationClearanceContinuity();
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
            AssignTiming(in step);
            AssignTrajectory(in rootTrajectory, in step);
            PredictedHip = predictedHip;
            FutureSupport = query.FutureLandingSupport;
            FutureLandingRequest = query.FutureLandingRequest;
            VirtualGroundSplitEventPhase = query.VirtualGroundSplitEventPhase;
            VirtualGroundOpposingLanding = query.VirtualGroundOpposingLanding;
            VirtualGroundSplitRoutePoint = query.VirtualGroundSplitRoutePoint;
            VirtualGroundSplitPlanarError = query.VirtualGroundSplitPlanarError;
            VirtualGroundSplitFraction = query.VirtualGroundSplitFraction;
            VirtualGroundSplitSupport = query.VirtualGroundSplitSupport;
            VirtualGroundSplitLandingEventIdentity = query.VirtualGroundSplitLandingEventIdentity;
            BodySupportPath = query.BodySupportPath;
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
            ActionStepPhase = clock.Phase;
            ActionClockFrame = renderFrame;
            if (ActionStepPhase >= 0.9999f)
            {
                ActionStepPhase = 1f;
                ActionProgress = 1f;
                GroundPathProgress = 1f;
                return;
            }
            if (ActionStepPhase + 0.000001f < ReleasePhase)
            {
                ActionProgress = 0f;
                GroundPathProgress = 0f;
                return;
            }
            if (State == CharacterPredictiveFootPlanState.Planned)
            {
                State = CharacterPredictiveFootPlanState.Executing;
                TransitionReason = CharacterPredictiveFootPlanTransitionReason.PlanExecutionStarted;
            }
            if (ActionStepPhase + 0.000001f < RootTrajectory.PathStartPhase)
            {
                ActionProgress = 0f;
                GroundPathProgress = 0f;
                return;
            }
            ActionProgress = ResolveActionProgress();
            GroundPathProgress = EvaluateGroundPathProgress(ActionStepPhase);
        }

        float ResolveActionProgress() => ResolveActionProgress(ActionStepPhase);

        float ResolveActionProgress(float eventPhase) => Mathf.Clamp01(
            (eventPhase - RootTrajectory.PathStartPhase) /
            Mathf.Max(0.000001f, 1f - RootTrajectory.PathStartPhase));

        void ResolveGroundPathRates()
        {
            var resolvedPhases = new FixedList512Bytes<float>();
            var resolvedRates = new FixedList512Bytes<float>();
            float previousPhase = -1f;
            float previousRate = -1f;
            for (int i = 0; i < FootRateSampleCount; i++)
            {
                float phase = m_FootRateEventPhases[i];
                float rate = m_FootRateProgress[i];
                if (!float.IsFinite(phase) || !float.IsFinite(rate) ||
                    phase <= previousPhase || rate + 0.000001f < previousRate)
                {
                    throw new InvalidOperationException("Predictive Foot Rate is not monotonic.");
                }
                resolvedPhases.Add(phase);
                resolvedRates.Add(rate);
                previousPhase = phase;
                previousRate = rate;
            }
            if (resolvedPhases.Length < 2 || resolvedRates.Length != resolvedPhases.Length)
                throw new InvalidOperationException("Predictive Foot Rate could not be resolved.");
            GroundPathRatePhases = resolvedPhases;
            GroundPathRates = resolvedRates;
        }

        internal static bool HasValidGroundPathRateRange(
            in CharacterPredictiveFootRootTrajectory rootTrajectory,
            in CharacterPredictiveFootPlacementQueryResult query)
        {
            int count = query.FootRateSampleCount;
            if (count < 2 || query.FootRateEventPhases == null ||
                query.FootRateProgress == null ||
                query.FootRateEventPhases.Length < count || query.FootRateProgress.Length < count)
            {
                return false;
            }
            if (Mathf.Abs(query.FootRateEventPhases[0] - rootTrajectory.PathStartPhase) > 0.00001f ||
                Mathf.Abs(query.FootRateEventPhases[count - 1] - 1f) > 0.00001f ||
                Mathf.Abs(query.FootRateProgress[0]) > 0.00001f ||
                Mathf.Abs(query.FootRateProgress[count - 1] - 1f) > 0.00001f)
            {
                return false;
            }
            for (int i = 1; i < count; i++)
            {
                if (query.FootRateEventPhases[i] <= query.FootRateEventPhases[i - 1] ||
                    query.FootRateProgress[i] + 0.000001f < query.FootRateProgress[i - 1])
                {
                    return false;
                }
            }
            return true;
        }

        internal float EvaluateGroundPathProgress(float eventPhase)
        {
            if (GroundPathRatePhases.Length < 2 ||
                GroundPathRatePhases.Length != GroundPathRates.Length)
            {
                throw new InvalidOperationException("Predictive Foot Rate is unavailable.");
            }
            float phase = Mathf.Clamp01(eventPhase);
            if (phase <= GroundPathRatePhases[0])
                return GroundPathRates[0];
            int last = GroundPathRatePhases.Length - 1;
            if (phase >= GroundPathRatePhases[last])
                return GroundPathRates[last];
            for (int i = 1; i <= last; i++)
            {
                if (phase > GroundPathRatePhases[i])
                    continue;
                float duration = GroundPathRatePhases[i] - GroundPathRatePhases[i - 1];
                float t = duration > 0.000001f
                    ? Mathf.Clamp01((phase - GroundPathRatePhases[i - 1]) / duration)
                    : 1f;
                return Mathf.Lerp(GroundPathRates[i - 1], GroundPathRates[i], t);
            }
            return GroundPathRates[last];
        }

        static float ResolveActionProgress(float pathStartPhase, float eventPhase) => Mathf.Clamp01(
            (eventPhase - pathStartPhase) / Mathf.Max(0.000001f, 1f - pathStartPhase));

        internal float EvaluatePredictiveOutputWeight()
        {
            if (!HasExecutablePath)
                return 0f;
            if (ActionStepPhase <= ReleasePhase)
                return 0f;
            if (ActionStepPhase >= LiftOffPhase)
                return 1f;
            float value = Mathf.InverseLerp(ReleasePhase, LiftOffPhase, ActionStepPhase);
            return value * value * (3f - 2f * value);
        }

        internal void ObserveWorldMotionDeviation(
            Vector3 currentPresentedBodyPosition,
            Quaternion currentPresentedBodyRotation,
            float landingPlanarTolerance)
        {
            if (!HasExecutablePath)
                return;
            MotionLinearLandingError = 0f;
            MotionAngularLandingError = 0f;
            MotionLandingError = 0f;
            MotionLandingTolerance = float.IsFinite(landingPlanarTolerance) &&
                                     landingPlanarTolerance > 0f
                ? landingPlanarTolerance
                : 0f;
            if (!IsFinite(currentPresentedBodyPosition) ||
                !IsFinite(currentPresentedBodyRotation) ||
                !float.IsFinite(landingPlanarTolerance) ||
                landingPlanarTolerance <= 0f)
            {
                return;
            }
            if (ActionStepPhase + 0.000001f < LiftOffPhase ||
                ActionStepPhase >= 0.9999f)
                return;
            Vector3 expectedBodyPosition = RootTrajectory
                .EvaluatePresentedBodyPositionAtEventPhase(ActionStepPhase);
            float linearLandingError = Vector3.ProjectOnPlane(
                    currentPresentedBodyPosition - ProjectWorldPoint(expectedBodyPosition),
                    RootTrajectory.Up)
                .magnitude;
            MotionLinearLandingError = linearLandingError;
            RootTrajectory.EvaluateEventPhase(
                ActionStepPhase,
                out _,
                out Quaternion expectedBodyRotation);
            float angularDifference = Quaternion.Angle(
                (m_WorldProjectionRotation * expectedBodyRotation).normalized,
                currentPresentedBodyRotation) * Mathf.Deg2Rad;
            float angularLever = Mathf.Max(
                SoleSupportRadius,
                RootTrajectory.EvaluateRemainingPlanarDistance(ActionStepPhase));
            float angularLandingError =
                2f * angularLever * Mathf.Sin(angularDifference * 0.5f);
            MotionAngularLandingError = angularLandingError;
            MotionLandingError = Mathf.Sqrt(
                linearLandingError * linearLandingError +
                angularLandingError * angularLandingError);
        }

        internal void UpdateWorldProjection(
            Vector3 currentRootPosition,
            Quaternion currentRootRotation)
        {
            if (!HasExecutablePath || m_WorldProjectionFrozen)
                return;
            if (!IsFinite(currentRootPosition) || !IsFinite(currentRootRotation))
                throw new ArgumentException("Predictive Foot world projection input is invalid.");
            RootTrajectory.EvaluateEventPhase(
                ActionStepPhase,
                out Vector3 expectedRootPosition,
                out Quaternion expectedRootRotation);
            Vector3 up = RootTrajectory.Up;
            Vector3 expectedForward = Vector3.ProjectOnPlane(
                expectedRootRotation * Vector3.forward,
                up);
            Vector3 currentForward = Vector3.ProjectOnPlane(
                currentRootRotation * Vector3.forward,
                up);
            float yaw = expectedForward.sqrMagnitude > 0.000001f &&
                        currentForward.sqrMagnitude > 0.000001f
                ? Vector3.SignedAngle(expectedForward, currentForward, up)
                : 0f;
            m_WorldProjectionExpectedRoot = expectedRootPosition;
            m_WorldProjectionCurrentRoot = expectedRootPosition + Vector3.ProjectOnPlane(
                currentRootPosition - expectedRootPosition,
                up);
            m_WorldProjectionRotation = Quaternion.AngleAxis(yaw, up);
            m_HasWorldProjection = true;
            if (ActionStepPhase + 0.000001f >= ApproachContactPhase)
                m_WorldProjectionFrozen = true;
        }

        internal void EvaluateGroundPath(
            float progress,
            out Vector3 pathPosition,
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
            pathPosition = ProjectWorldPoint(
                Vector3.Lerp(segment.EdgeStart, segment.EdgeEnd, t));
            support = ProjectSurface(
                t <= 0.000001f
                    ? segment.StartSurface
                    : segment.EndSurface);
        }

        internal void EvaluateFootMotion(
            float eventPhase,
            out Vector3 planarSole,
            out float animationClearanceHeight,
            out AnimationFootConstraintMode constraintMode,
            out AnimationFootSupportPhase supportPhase,
            out AnimationFootOrientationPolicy orientationPolicy,
            out AnimationBodyRotationPivotMode bodyPivotMode)
        {
            float phase = Mathf.Clamp01(eventPhase);
            planarSole = ProjectWorldPoint(RootTrajectory.EvaluateFootRoute(phase));
            animationClearanceHeight = Mathf.Max(
                0f,
                EvaluateFloatRoute(AnimationClearanceHeights, phase) +
                EvaluateAnimationClearanceContinuity(ResolveSwingProgress(phase)));
            EvaluateResolvedActionState(
                phase,
                out constraintMode,
                out supportPhase,
                out orientationPolicy,
                out bodyPivotMode);
        }

        internal void EvaluateBodyPath(
            float eventPhase,
            out Vector3 root,
            out Vector3 hip)
        {
            if (!BodySupportPath.IsValid)
                throw new InvalidOperationException("Predictive Body Support Path is unavailable.");
            CharacterPredictiveFootRootTrajectory rootTrajectory = RootTrajectory;
            BodySupportPath.Evaluate(
                in rootTrajectory,
                Mathf.Clamp01(eventPhase),
                out root,
                out hip);
            root = ProjectWorldPoint(root);
            hip = ProjectWorldPoint(hip);
        }

        internal void EvaluateClearancePath(
            float eventPhase,
            out Vector3 groundPath,
            out Vector3 root,
            out Vector3 hip,
            out FootPlacementSurface support,
            out Vector3 sole)
        {
            float phase = Mathf.Clamp01(eventPhase);
            EvaluateGroundPath(
                EvaluateGroundPathProgress(phase),
                out Vector3 envelopePoint,
                out support);
            EvaluateBodyPath(phase, out root, out hip);
            EvaluateFootMotion(
                phase,
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
            EvaluateResolvedActionState(
                ActionStepPhase,
                out constraintMode,
                out supportPhase,
                out orientationPolicy,
                out bodyPivotMode);
        }

        internal void EvaluateActionState(
            float eventPhase,
            out AnimationFootConstraintMode constraintMode,
            out AnimationFootSupportPhase supportPhase,
            out AnimationFootOrientationPolicy orientationPolicy,
            out AnimationBodyRotationPivotMode bodyPivotMode)
        {
            if (!OwnsEvent)
                throw new InvalidOperationException("Predictive Foot Plan action state is unavailable.");
            EvaluateResolvedActionState(
                Mathf.Clamp01(eventPhase),
                out constraintMode,
                out supportPhase,
                out orientationPolicy,
                out bodyPivotMode);
        }

        internal void EvaluateCurrentAnimationClearance(
            out float authoredClearanceHeight,
            out float continuityOffset,
            out float continuityContribution,
            out float reachClearanceHeight,
            out float compositeClearanceHeight)
        {
            EvaluateAnimationClearance(
                ActionStepPhase,
                out authoredClearanceHeight,
                out continuityOffset,
                out continuityContribution,
                out reachClearanceHeight,
                out compositeClearanceHeight);
        }

        internal void EvaluateAnimationClearance(
            float eventPhase,
            out float authoredClearanceHeight,
            out float continuityOffset,
            out float continuityContribution,
            out float reachClearanceHeight,
            out float compositeClearanceHeight)
        {
            if (!HasExecutablePath)
                throw new InvalidOperationException("Predictive Foot Plan clearance is unavailable.");
            float phase = Mathf.Clamp01(eventPhase);
            authoredClearanceHeight = EvaluateFloatRoute(
                AnimationClearanceHeights,
                phase);
            continuityOffset = AnimationClearanceContinuityOffset;
            continuityContribution = EvaluateAnimationClearanceContinuity(
                ResolveSwingProgress(phase));
            reachClearanceHeight = 0f;
            compositeClearanceHeight = Mathf.Max(
                0f,
                authoredClearanceHeight +
                continuityContribution);
        }

        internal FootPlacementGroundEnvelopeSegment GetPathSegment(int index)
        {
            if (index < 0 || index >= GroundEnvelopeSegmentCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            FootPlacementGroundEnvelopeSegment segment = m_PathSegments[index];
            return new FootPlacementGroundEnvelopeSegment(
                segment.StartFraction,
                segment.EndFraction,
                ProjectSurface(segment.StartSurface),
                ProjectSurface(segment.EndSurface),
                ProjectWorldPoint(segment.EdgeStart),
                ProjectWorldPoint(segment.EdgeEnd),
                segment.StartSoleHeight,
                segment.EndSoleHeight,
                segment.IsVirtualPlane);
        }

        internal Vector3 GetPlannedFootRouteSample(int index)
        {
            if (!OwnsEvent || index < 0 || index >= FrozenWorldFootRoute.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return ProjectWorldPoint(FrozenWorldFootRoute[index]);
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
            ReleasePhase = 0f;
            ApproachContactPhase = 0f;
            FrozenWorldFootRoute = default;
            FrozenWorldFootRoutePhases = default;
            GroundPathRatePhases = default;
            GroundPathRates = default;
            AnimationClearanceContinuityOffset = 0f;
            LandingDelayAtGeneration = 0f;
            EventPhaseAtGeneration = 0f;
            LiftOffPhase = 0f;
            ActionStepDurationSeconds = 0f;
            ActionStepPhase = 0f;
            ActionProgress = 0f;
            GroundPathProgress = 0f;
            MotionLinearLandingError = 0f;
            MotionAngularLandingError = 0f;
            MotionLandingError = 0f;
            MotionLandingTolerance = 0f;
            SoleSupportRadius = 0f;
            ActionClockFrame = 0;
            FutureSupport = default;
            FutureLandingRequest = default;
            BodySupportPath = default;
            VirtualGroundSplitEventPhase = 0f;
            VirtualGroundOpposingLanding = default;
            VirtualGroundSplitRoutePoint = default;
            VirtualGroundSplitPlanarError = 0f;
            VirtualGroundSplitFraction = 0f;
            VirtualGroundSplitSupport = default;
            VirtualGroundSplitLandingEventIdentity = 0;
            GroundEnvelopeSegmentCount = 0;
            GroundEnvelopeRejectReason = FootPlacementGroundEnvelopeRejectReason.None;
            QueryCount = 0;
            RawHitCount = 0;
            RouteSampleCount = 0;
            FootRateSampleCount = 0;
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
            ResetWorldProjection();
        }

        void CopyQuerySnapshots(in CharacterPredictiveFootPlacementQueryResult query)
        {
            Copy(query.RouteEventPhases, query.RouteSampleCount, m_QueryRouteEventPhases);
            Copy(query.RouteFractions, query.RouteSampleCount, m_QueryRouteFractions);
            Copy(query.GroundProbeRoute, query.RouteSampleCount, m_GroundProbeRoute);
            FootRateSampleCount = query.FootRateSampleCount;
            Copy(query.FootRateEventPhases, FootRateSampleCount, m_FootRateEventPhases);
            Copy(query.FootRateProgress, FootRateSampleCount, m_FootRateProgress);
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
            int groundProbeCount = RouteSampleCount >= 2
                ? Mathf.Min(
                    RouteSampleCount,
                    CharacterPredictiveFootPlacementQuery.MaximumRouteSampleCount)
                : 0;
            var groundProbeRoute = new CharacterPredictiveFootRoutePointSnapshot[groundProbeCount];
            for (int i = 0; i < groundProbeRoute.Length; i++)
            {
                groundProbeRoute[i] = new CharacterPredictiveFootRoutePointSnapshot(
                    m_QueryRouteFractions[i],
                    m_GroundProbeRoute[i]);
            }
            var animationFootRoute = new CharacterPredictiveFootRoutePointSnapshot[
                FrozenWorldFootRoute.Length];
            for (int i = 0; i < animationFootRoute.Length; i++)
            {
                animationFootRoute[i] = new CharacterPredictiveFootRoutePointSnapshot(
                    FrozenWorldFootRoutePhases[i],
                    FrozenWorldFootRoute[i]);
            }
            var footRate = new CharacterPredictiveFootRatePointSnapshot[GroundPathRatePhases.Length];
            for (int i = 0; i < footRate.Length; i++)
            {
                footRate[i] = new CharacterPredictiveFootRatePointSnapshot(
                    GroundPathRatePhases[i],
                    GroundPathRates[i]);
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
                VirtualGroundSplitEventPhase,
                VirtualGroundOpposingLanding,
                VirtualGroundSplitRoutePoint,
                VirtualGroundSplitPlanarError,
                VirtualGroundSplitFraction,
                VirtualGroundSplitLandingEventIdentity,
                HasExecutablePath && VirtualGroundSplitSupport.IsValid
                    ? VirtualGroundSplitSupport.Point
                    : Vector3.zero,
                RootTrajectory.FrozenMotionPlanarVelocity,
                RootTrajectory.ContinuationPlanarVelocity,
                RootTrajectory.CurrentSegmentSwitchDelaySeconds,
                RootTrajectory.HasContinuation,
                RootTrajectory.FrozenYawVelocityDegreesPerSecond,
                RootTrajectory.FrozenMaximumYawVelocityDegreesPerSecond,
                groundProbeRoute,
                animationFootRoute,
                footRate,
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
            const int uniformCount = CharacterPredictiveFootPlacementQuery.MaximumRouteSampleCount;
            var phases = new List<float>(uniformCount + GroundPathRatePhases.Length);
            for (int i = 0; i < uniformCount; i++)
                phases.Add(Mathf.Lerp(RootTrajectory.PathStartPhase, 1f, i / (uniformCount - 1f)));
            for (int i = 0; i < GroundPathRatePhases.Length; i++)
                phases.Add(GroundPathRatePhases[i]);
            phases.Sort();
            var uniquePhases = new List<float>(phases.Count);
            float previous = -1f;
            for (int i = 0; i < phases.Count; i++)
            {
                float phase = Mathf.Clamp01(phases[i]);
                if (previous >= 0f && Mathf.Abs(phase - previous) <= 0.00001f)
                    continue;
                uniquePhases.Add(phase);
                previous = phase;
            }
            var segments = new CharacterPredictiveFootClearanceSegmentSnapshot[
                Mathf.Max(0, uniquePhases.Count - 1)];
            for (int i = 0; i < segments.Length; i++)
            {
                float startPhase = uniquePhases[i];
                float endPhase = uniquePhases[i + 1];
                EvaluateClearancePath(
                    startPhase,
                    out _,
                    out Vector3 rootStart,
                    out Vector3 hipStart,
                    out FootPlacementSurface startSupport,
                    out Vector3 start);
                EvaluateClearancePath(
                    endPhase,
                    out _,
                    out Vector3 rootEnd,
                    out Vector3 hipEnd,
                    out FootPlacementSurface endSupport,
                    out Vector3 end);
                FootPlacementSurface surface = endSupport.IsValid ? endSupport : startSupport;
                segments[i] = new CharacterPredictiveFootClearanceSegmentSnapshot(
                    startPhase,
                    endPhase,
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
            ApproachContactPhase = step.ApproachContactPhase;
            var frozenWorldFootRoute = new FixedList512Bytes<Vector3>();
            var frozenWorldFootRoutePhases = new FixedList128Bytes<float>();
            for (int i = 0; i < AuthoredFootPlanarRoute.Length; i++)
            {
                float progress = i / (AuthoredFootPlanarRoute.Length - 1f);
                float eventPhase = Mathf.Lerp(rootTrajectory.PathStartPhase, 1f, progress);
                frozenWorldFootRoutePhases.Add(eventPhase);
                frozenWorldFootRoute.Add(rootTrajectory.EvaluateFootRoute(eventPhase));
            }
            FrozenWorldFootRoute = frozenWorldFootRoute;
            FrozenWorldFootRoutePhases = frozenWorldFootRoutePhases;
            RootStart = rootTrajectory.StartPosition;
            RootStartRotation = rootTrajectory.StartRotation;
            rootTrajectory.EvaluateEventPhase(1f, out Vector3 rootLanding, out Quaternion rootLandingRotation);
            RootLanding = rootLanding;
            RootLandingRotation = rootLandingRotation;
            ResetWorldProjection();
        }

        Vector3 ProjectWorldPoint(Vector3 point)
        {
            if (!m_HasWorldProjection)
                return point;
            return m_WorldProjectionCurrentRoot +
                   m_WorldProjectionRotation * (point - m_WorldProjectionExpectedRoot);
        }

        Vector3 ProjectWorldDirection(Vector3 direction) =>
            m_HasWorldProjection
                ? m_WorldProjectionRotation * direction
                : direction;

        FootPlacementSurface ProjectSurface(FootPlacementSurface surface)
        {
            if (!surface.IsValid || !m_HasWorldProjection)
                return surface;
            Vector3 normal = ProjectWorldDirection(surface.Normal);
            return new FootPlacementSurface(
                surface.Collider,
                ProjectWorldPoint(surface.Point),
                normal.normalized);
        }

        void ResetWorldProjection()
        {
            m_WorldProjectionExpectedRoot = Vector3.zero;
            m_WorldProjectionCurrentRoot = Vector3.zero;
            m_WorldProjectionRotation = Quaternion.identity;
            m_HasWorldProjection = false;
            m_WorldProjectionFrozen = false;
        }

        void ResolveAnimationClearanceContinuity()
        {
            AnimationClearanceContinuityOffset = 0f;
            if (!HasExecutablePath || GroundEnvelopeSegmentCount <= 0)
                return;
            float pathStartPhase = RootTrajectory.PathStartPhase;
            Vector3 up = RootTrajectory.Up;
            EvaluateGroundPath(
                EvaluateGroundPathProgress(pathStartPhase),
                out Vector3 envelopePoint,
                out _);
            float currentClearance = Vector3.Dot(
                RootTrajectory.ExecutionSoleAtGeneration - envelopePoint,
                up);
            float authoredClearance = EvaluateFloatRoute(
                AnimationClearanceHeights,
                pathStartPhase);
            float offset = currentClearance - authoredClearance;
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

        float ResolveSwingProgress(float eventPhase) => Mathf.Clamp01(
            (eventPhase - RootTrajectory.PathStartPhase) /
            Mathf.Max(0.000001f, 1f - RootTrajectory.PathStartPhase));

        void EvaluateResolvedActionState(
            float eventPhase,
            out AnimationFootConstraintMode constraintMode,
            out AnimationFootSupportPhase supportPhase,
            out AnimationFootOrientationPolicy orientationPolicy,
            out AnimationBodyRotationPivotMode bodyPivotMode)
        {
            float phase = Mathf.Clamp01(eventPhase);
            constraintMode = AnimationFootConstraintFacts.ResolveConstraintMode(
                phase,
                ReleasePhase,
                LiftOffPhase);
            supportPhase = AnimationFootConstraintFacts.ResolveSupportPhase(
                phase,
                ReleasePhase,
                LiftOffPhase,
                ApproachContactPhase);
            orientationPolicy = supportPhase == AnimationFootSupportPhase.Unsupported
                ? AnimationFootOrientationPolicy.PreserveAnimation
                : AnimationFootOrientationPolicy.LandingSurface;
            bodyPivotMode = AnimationFootConstraintFacts.ResolveBodyPivotMode(
                phase,
                LiftOffPhase);
            RequireAuthoritativeConstraint(
                phase >= LiftOffPhase && phase < 0.9999f,
                phase < LiftOffPhase,
                constraintMode,
                supportPhase,
                bodyPivotMode);
        }

        internal static void RequireAuthoritativeConstraint(
            bool isSwing,
            bool isPreSwing,
            AnimationFootConstraintMode constraintMode,
            AnimationFootSupportPhase supportPhase,
            AnimationBodyRotationPivotMode bodyPivotMode)
        {
            if (isSwing)
            {
                if (constraintMode != AnimationFootConstraintMode.Unlocked ||
                    supportPhase != AnimationFootSupportPhase.Unsupported &&
                    supportPhase != AnimationFootSupportPhase.ApproachingContact ||
                    bodyPivotMode != AnimationBodyRotationPivotMode.Pelvis)
                {
                    throw new InvalidOperationException(
                        "Predictive Foot swing constraint facts are inconsistent.");
                }
                return;
            }
            if (!isPreSwing)
                return;
            if (constraintMode == AnimationFootConstraintMode.Unlocked ||
                supportPhase != AnimationFootSupportPhase.Supporting &&
                supportPhase != AnimationFootSupportPhase.Releasing ||
                bodyPivotMode != AnimationBodyRotationPivotMode.SupportFoot)
            {
                throw new InvalidOperationException(
                    "Predictive Foot pre-swing constraint facts are inconsistent.");
            }
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

        void AssignTiming(in AnimationPredictedFootStepSample step)
        {
            LandingDelayAtGeneration = step.ActionStepClock.TimeToLandingSeconds;
            EventPhaseAtGeneration = step.ActionStepClock.Phase;
            ReleasePhase = step.ReleasePhase;
            LiftOffPhase = step.ActionStepClock.LiftOffPhase;
            ActionStepDurationSeconds = step.ActionStepClock.DurationSeconds;
            ActionStepPhase = EventPhaseAtGeneration;
            ActionProgress = 0f;
            ActionClockFrame = GeneratedFrame;
        }

        static float EvaluateFloatRoute(FixedList128Bytes<float> route, float phase)
        {
            EvaluateRouteIndices(route.Length, phase, out int first, out int second, out float t);
            return Mathf.Lerp(route[first], route[second], t);
        }

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        static bool IsFinite(Quaternion value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) &&
            float.IsFinite(value.z) && float.IsFinite(value.w) &&
            Quaternion.Dot(value, value) > 0.000001f;

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
