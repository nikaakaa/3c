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
                ResolveRawTravelElapsedSeconds(1f))
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
        internal float PlannedLandingElapsedSeconds => ResolveRawTravelElapsedSeconds(1f);
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
            float elapsedSeconds = ResolveRawTravelElapsedSeconds(Mathf.Clamp01(eventPhase));
            return PresentedBodyStartPosition + ResolvePlanarTravel(elapsedSeconds);
        }

        internal Vector3 EvaluatePresentedBodyVelocityAtEventPhase(float eventPhase)
        {
            float elapsedSinceGeneration = ResolveRawTravelElapsedSeconds(
                Mathf.Clamp01(eventPhase));
            CharacterFutureBodyTrajectorySample sample = FutureBodyTrajectory.Evaluate(
                elapsedSinceGeneration);
            return new Vector3(sample.VelocityX, sample.VelocityY, sample.VelocityZ);
        }

        internal Vector3 EvaluatePresentedBodyLandingPosition() =>
            PresentedBodyStartPosition + ResolvePlanarTravel(
                ResolveRawTravelElapsedSeconds(1f));

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

        internal sealed class Builder
        {
            readonly CharacterPredictiveFootPlacementPlan m_Target;

            internal Builder(CharacterPredictiveFootPlacementPlan target)
            {
                m_Target = target ?? throw new ArgumentNullException(nameof(target));
            }

            internal void CopyFrom(CharacterPredictiveFootPlacementPlan source) =>
                m_Target.CopyFrom(source);

            internal void BuildExecutable(
                ulong sequence,
                ulong generatedFrame,
                in AnimationPredictedFootStepSample step,
                Vector3 start,
                Vector3 landing,
                in CharacterPredictiveFootRootTrajectory rootTrajectory,
                Vector3 predictedHip,
                in CharacterPredictiveFootPlacementQueryResult query)
            {
                m_Target.ClearForBuild();
                m_Target.Commit(
                    sequence,
                    generatedFrame,
                    in step,
                    start,
                    landing,
                    in rootTrajectory,
                    predictedHip,
                    in query);
            }

            internal void BuildRejected(
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
                m_Target.ClearForBuild();
                m_Target.Reject(
                    sequence,
                    generatedFrame,
                    in step,
                    start,
                    landing,
                    in rootTrajectory,
                    predictedHip,
                    rejectReason,
                    in query);
            }

            internal void Clear() => m_Target.ClearForBuild();
        }

        internal ulong Sequence { get; private set; }
        internal ulong LandingEventIdentity { get; private set; }
        internal ulong SourceSampleIdentity { get; private set; }
        internal int SourceSampleCycle { get; private set; }
        internal int EventOrdinal { get; private set; }
        internal ulong InitialContributionContinuityIdentity { get; private set; }
        internal ulong GeneratedFrame { get; private set; }
        internal CharacterPredictiveFootPlanState CreationState { get; private set; }
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
        internal float SoleSupportRadius { get; private set; }
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
        internal bool OwnsEvent => LandingEventIdentity != 0;
        internal bool HasAttempt =>
            CreationState == CharacterPredictiveFootPlanState.Planned ||
            CreationState == CharacterPredictiveFootPlanState.Executing ||
            CreationState == CharacterPredictiveFootPlanState.Rejected;
        internal bool WasBuiltExecutable =>
            CreationState == CharacterPredictiveFootPlanState.Planned ||
            CreationState == CharacterPredictiveFootPlanState.Executing;
        internal bool HasPathGeometry => GroundEnvelopeSegmentCount > 0;

        void CopyFrom(CharacterPredictiveFootPlacementPlan source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (source.m_Side != m_Side ||
                source.m_PathSegments.Length != m_PathSegments.Length)
            {
                throw new ArgumentException("Predictive Foot Plan copy layout is incompatible.", nameof(source));
            }
            Sequence = source.Sequence;
            LandingEventIdentity = source.LandingEventIdentity;
            SourceSampleIdentity = source.SourceSampleIdentity;
            SourceSampleCycle = source.SourceSampleCycle;
            EventOrdinal = source.EventOrdinal;
            InitialContributionContinuityIdentity = source.InitialContributionContinuityIdentity;
            GeneratedFrame = source.GeneratedFrame;
            CreationState = source.CreationState;
            Start = source.Start;
            Landing = source.Landing;
            RootStart = source.RootStart;
            RootStartRotation = source.RootStartRotation;
            RootLanding = source.RootLanding;
            RootLandingRotation = source.RootLandingRotation;
            PredictedHip = source.PredictedHip;
            RootTrajectory = source.RootTrajectory;
            BodySupportPath = source.BodySupportPath;
            AuthoredFootPlanarRoute = source.AuthoredFootPlanarRoute;
            RootLocalHipRoute = source.RootLocalHipRoute;
            AnimationClearanceHeights = source.AnimationClearanceHeights;
            ReleasePhase = source.ReleasePhase;
            ApproachContactPhase = source.ApproachContactPhase;
            FrozenWorldFootRoute = source.FrozenWorldFootRoute;
            FrozenWorldFootRoutePhases = source.FrozenWorldFootRoutePhases;
            GroundPathRatePhases = source.GroundPathRatePhases;
            GroundPathRates = source.GroundPathRates;
            AnimationClearanceContinuityOffset = source.AnimationClearanceContinuityOffset;
            LandingDelayAtGeneration = source.LandingDelayAtGeneration;
            EventPhaseAtGeneration = source.EventPhaseAtGeneration;
            LiftOffPhase = source.LiftOffPhase;
            ActionStepDurationSeconds = source.ActionStepDurationSeconds;
            SoleSupportRadius = source.SoleSupportRadius;
            FutureSupport = source.FutureSupport;
            FutureLandingRequest = source.FutureLandingRequest;
            VirtualGroundSplitEventPhase = source.VirtualGroundSplitEventPhase;
            VirtualGroundOpposingLanding = source.VirtualGroundOpposingLanding;
            VirtualGroundSplitRoutePoint = source.VirtualGroundSplitRoutePoint;
            VirtualGroundSplitPlanarError = source.VirtualGroundSplitPlanarError;
            VirtualGroundSplitFraction = source.VirtualGroundSplitFraction;
            VirtualGroundSplitSupport = source.VirtualGroundSplitSupport;
            VirtualGroundSplitLandingEventIdentity = source.VirtualGroundSplitLandingEventIdentity;
            GroundEnvelopeSegmentCount = source.GroundEnvelopeSegmentCount;
            GroundEnvelopeRejectReason = source.GroundEnvelopeRejectReason;
            QueryCount = source.QueryCount;
            RawHitCount = source.RawHitCount;
            RouteSampleCount = source.RouteSampleCount;
            FootRateSampleCount = source.FootRateSampleCount;
            AcceptedHitCount = source.AcceptedHitCount;
            EdgePlaneCandidateCount = source.EdgePlaneCandidateCount;
            AcceptedEdgePlaneCount = source.AcceptedEdgePlaneCount;
            RejectedQueryCount = source.RejectedQueryCount;
            QueryRejectCounts = source.QueryRejectCounts;
            CreationRejectReason = source.CreationRejectReason;
            QueryRequestSnapshotCount = source.QueryRequestSnapshotCount;
            AcceptedSupportSnapshotCount = source.AcceptedSupportSnapshotCount;
            RejectedGeometrySnapshotCount = source.RejectedGeometrySnapshotCount;
            GeometrySnapshot = source.GeometrySnapshot;
            Array.Copy(source.m_PathSegments, m_PathSegments, m_PathSegments.Length);
            Array.Copy(source.m_QueryRouteEventPhases, m_QueryRouteEventPhases, m_QueryRouteEventPhases.Length);
            Array.Copy(source.m_QueryRouteFractions, m_QueryRouteFractions, m_QueryRouteFractions.Length);
            Array.Copy(source.m_GroundProbeRoute, m_GroundProbeRoute, m_GroundProbeRoute.Length);
            Array.Copy(source.m_FootRateEventPhases, m_FootRateEventPhases, m_FootRateEventPhases.Length);
            Array.Copy(source.m_FootRateProgress, m_FootRateProgress, m_FootRateProgress.Length);
            Array.Copy(source.m_QueryRequests, m_QueryRequests, m_QueryRequests.Length);
            Array.Copy(source.m_AcceptedSupports, m_AcceptedSupports, m_AcceptedSupports.Length);
            Array.Copy(source.m_RejectedGeometry, m_RejectedGeometry, m_RejectedGeometry.Length);
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

        void Commit(
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
            CreationState = step.ActionStepClock.Phase + 0.000001f < ReleasePhase
                ? CharacterPredictiveFootPlanState.Planned
                : CharacterPredictiveFootPlanState.Executing;
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
            ResolveAnimationClearanceContinuity();
            GeometrySnapshot = BuildGeometrySnapshot();
        }

        void Reject(
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
            CreationState = CharacterPredictiveFootPlanState.Rejected;
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

        internal float ResolveActionProgress(float eventPhase) => Mathf.Clamp01(
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

        internal void EvaluateGroundPathLocal(
            float progress,
            out Vector3 pathPosition,
            out FootPlacementSurface support)
        {
            if (!WasBuiltExecutable || GroundEnvelopeSegmentCount <= 0)
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
            support = t <= 0.000001f
                ? segment.StartSurface
                : segment.EndSurface;
        }

        internal void EvaluateFootMotionLocal(
            float eventPhase,
            out Vector3 planarSole,
            out float animationClearanceHeight,
            out AnimationFootConstraintMode constraintMode,
            out AnimationFootSupportPhase supportPhase,
            out AnimationFootOrientationPolicy orientationPolicy,
            out AnimationBodyRotationPivotMode bodyPivotMode)
        {
            float phase = Mathf.Clamp01(eventPhase);
            planarSole = RootTrajectory.EvaluateFootRoute(phase);
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

        internal void EvaluateBodyPathLocal(
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
        }

        internal void EvaluateClearancePathLocal(
            float eventPhase,
            out Vector3 groundPath,
            out Vector3 root,
            out Vector3 hip,
            out FootPlacementSurface support,
            out Vector3 sole)
        {
            float phase = Mathf.Clamp01(eventPhase);
            EvaluateGroundPathLocal(
                EvaluateGroundPathProgress(phase),
                out Vector3 envelopePoint,
                out support);
            EvaluateBodyPathLocal(phase, out root, out hip);
            EvaluateFootMotionLocal(
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

        internal void EvaluateAnimationClearance(
            float eventPhase,
            out float authoredClearanceHeight,
            out float continuityOffset,
            out float continuityContribution,
            out float reachClearanceHeight,
            out float compositeClearanceHeight)
        {
            if (!WasBuiltExecutable)
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

        internal FootPlacementGroundEnvelopeSegment GetPathSegmentLocal(int index)
        {
            if (index < 0 || index >= GroundEnvelopeSegmentCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            FootPlacementGroundEnvelopeSegment segment = m_PathSegments[index];
            return segment;
        }

        internal Vector3 GetPlannedFootRouteSampleLocal(int index)
        {
            if (!OwnsEvent || index < 0 || index >= FrozenWorldFootRoute.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return FrozenWorldFootRoute[index];
        }

        void ClearForBuild()
        {
            CreationState = CharacterPredictiveFootPlanState.Inactive;
            Sequence = 0;
            LandingEventIdentity = 0;
            SourceSampleIdentity = 0;
            SourceSampleCycle = 0;
            EventOrdinal = 0;
            InitialContributionContinuityIdentity = 0;
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
            SoleSupportRadius = 0f;
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
            int envelopeCount = WasBuiltExecutable ? GroundEnvelopeSegmentCount : 0;
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
                WasBuiltExecutable,
                WasBuiltExecutable && FutureSupport.IsValid,
                WasBuiltExecutable && FutureSupport.IsValid ? Landing : Vector3.zero,
                WasBuiltExecutable && VirtualGroundSplitSupport.IsValid,
                VirtualGroundSplitEventPhase,
                VirtualGroundOpposingLanding,
                VirtualGroundSplitRoutePoint,
                VirtualGroundSplitPlanarError,
                VirtualGroundSplitFraction,
                VirtualGroundSplitLandingEventIdentity,
                WasBuiltExecutable && VirtualGroundSplitSupport.IsValid
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
            if (!WasBuiltExecutable || GroundEnvelopeSegmentCount <= 0)
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
                EvaluateClearancePathLocal(
                    startPhase,
                    out _,
                    out Vector3 rootStart,
                    out Vector3 hipStart,
                    out FootPlacementSurface startSupport,
                    out Vector3 start);
                EvaluateClearancePathLocal(
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
        }

        void ResolveAnimationClearanceContinuity()
        {
            AnimationClearanceContinuityOffset = 0f;
            if (!WasBuiltExecutable || GroundEnvelopeSegmentCount <= 0)
                return;
            float pathStartPhase = RootTrajectory.PathStartPhase;
            Vector3 up = RootTrajectory.Up;
            EvaluateGroundPathLocal(
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
            InitialContributionContinuityIdentity = step.ContributionContinuityIdentity;
            GeneratedFrame = generatedFrame;
        }

        void AssignTiming(in AnimationPredictedFootStepSample step)
        {
            LandingDelayAtGeneration = step.ActionStepClock.TimeToLandingSeconds;
            EventPhaseAtGeneration = step.ActionStepClock.Phase;
            ReleasePhase = step.ReleasePhase;
            LiftOffPhase = step.ActionStepClock.LiftOffPhase;
            ActionStepDurationSeconds = step.ActionStepClock.DurationSeconds;
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

    internal struct CharacterPredictiveFootPlanExecutionState
    {
        internal CharacterPredictiveFootPlanState State;
        internal CharacterPredictiveFootPlanTransitionReason TransitionReason;
        internal CharacterPredictiveFootPlanEndReason EndReason;
        internal ulong ContributionContinuityIdentity;
        internal float ActionStepPhase;
        internal float ActionProgress;
        internal float GroundPathProgress;
        internal float MotionLinearLandingError;
        internal float MotionAngularLandingError;
        internal float MotionLandingError;
        internal float MotionLandingTolerance;
        internal ulong ActionClockFrame;
        internal Vector3 WorldProjectionExpectedRoot;
        internal Vector3 WorldProjectionCurrentRoot;
        internal Quaternion WorldProjectionRotation;
        internal bool HasWorldProjection;
        internal bool WorldProjectionFrozen;

        internal void Initialize(CharacterPredictiveFootPlacementPlan plan)
        {
            State = plan.CreationState;
            TransitionReason = plan.CreationState == CharacterPredictiveFootPlanState.Rejected
                ? CharacterPredictiveFootPlanTransitionReason.PlanRejected
                : CharacterPredictiveFootPlanTransitionReason.PlanGenerated;
            EndReason = CharacterPredictiveFootPlanEndReason.None;
            ContributionContinuityIdentity = plan.InitialContributionContinuityIdentity;
            ActionStepPhase = plan.EventPhaseAtGeneration;
            bool startsExecuting = State == CharacterPredictiveFootPlanState.Planned &&
                                   ActionStepPhase + 0.000001f >= plan.ReleasePhase;
            if (startsExecuting)
                State = CharacterPredictiveFootPlanState.Executing;
            ActionProgress = startsExecuting &&
                             ActionStepPhase + 0.000001f >= plan.RootTrajectory.PathStartPhase
                ? plan.ResolveActionProgress(ActionStepPhase)
                : 0f;
            GroundPathProgress = startsExecuting &&
                                 ActionStepPhase + 0.000001f >= plan.RootTrajectory.PathStartPhase
                ? plan.EvaluateGroundPathProgress(ActionStepPhase)
                : 0f;
            MotionLinearLandingError = 0f;
            MotionAngularLandingError = 0f;
            MotionLandingError = 0f;
            MotionLandingTolerance = 0f;
            ActionClockFrame = plan.GeneratedFrame;
            WorldProjectionExpectedRoot = Vector3.zero;
            WorldProjectionCurrentRoot = Vector3.zero;
            WorldProjectionRotation = Quaternion.identity;
            HasWorldProjection = false;
            WorldProjectionFrozen = false;
        }

        internal void Reset(CharacterPredictiveFootPlanEndReason reason)
        {
            if (reason == CharacterPredictiveFootPlanEndReason.None)
                throw new ArgumentOutOfRangeException(nameof(reason));
            this = default;
            State = CharacterPredictiveFootPlanState.Inactive;
            TransitionReason = CharacterPredictiveFootPlanTransitionReason.PlanEnded;
            EndReason = reason;
            WorldProjectionRotation = Quaternion.identity;
        }
    }

    internal static class CharacterPredictiveFootPlanEvaluator
    {
        internal static void BeginFrame(ref CharacterPredictiveFootPlanExecutionState state)
        {
            state.TransitionReason = CharacterPredictiveFootPlanTransitionReason.None;
            state.EndReason = CharacterPredictiveFootPlanEndReason.None;
        }

        internal static void SynchronizePoseContribution(
            CharacterPredictiveFootPlacementPlan plan,
            ref CharacterPredictiveFootPlanExecutionState state,
            in AnimationPredictedFootStepSample step)
        {
            if (!plan.MatchesAuthoritativeEvent(in step))
                throw new ArgumentException("Predictive Foot Plan pose contribution does not belong to its action event.");
            state.ContributionContinuityIdentity = step.ContributionContinuityIdentity;
        }

        internal static void SynchronizeActionClock(
            CharacterPredictiveFootPlacementPlan plan,
            ref CharacterPredictiveFootPlanExecutionState state,
            ulong renderFrame,
            in AnimationPredictedFootStepSample step)
        {
            if (!IsExecutable(in state))
                return;
            if (renderFrame == 0 || renderFrame <= state.ActionClockFrame ||
                !plan.MatchesAuthoritativeEvent(in step))
            {
                throw new ArgumentException("Predictive Foot Plan action clock input is invalid.");
            }
            AnimationActionStepClockSample clock = step.ActionStepClock;
            if (!float.IsFinite(clock.DurationSeconds) || clock.DurationSeconds <= 0f ||
                clock.Phase + 0.00001f < state.ActionStepPhase)
            {
                Complete(ref state, CharacterPredictiveFootPlanEndReason.ActionClockInvalid);
                return;
            }
            state.ActionStepPhase = clock.Phase;
            state.ActionClockFrame = renderFrame;
            if (state.ActionStepPhase >= 0.9999f)
            {
                state.ActionStepPhase = 1f;
                state.ActionProgress = 1f;
                state.GroundPathProgress = 1f;
                return;
            }
            if (state.ActionStepPhase + 0.000001f < plan.ReleasePhase)
            {
                state.ActionProgress = 0f;
                state.GroundPathProgress = 0f;
                return;
            }
            if (state.State == CharacterPredictiveFootPlanState.Planned)
            {
                state.State = CharacterPredictiveFootPlanState.Executing;
                state.TransitionReason =
                    CharacterPredictiveFootPlanTransitionReason.PlanExecutionStarted;
            }
            if (state.ActionStepPhase + 0.000001f < plan.RootTrajectory.PathStartPhase)
            {
                state.ActionProgress = 0f;
                state.GroundPathProgress = 0f;
                return;
            }
            state.ActionProgress = plan.ResolveActionProgress(state.ActionStepPhase);
            state.GroundPathProgress = plan.EvaluateGroundPathProgress(state.ActionStepPhase);
        }

        internal static void UpdateWorldProjection(
            CharacterPredictiveFootPlacementPlan plan,
            ref CharacterPredictiveFootPlanExecutionState state,
            Vector3 currentRootPosition,
            Quaternion currentRootRotation)
        {
            if (!IsExecutable(in state) || state.WorldProjectionFrozen)
                return;
            if (!IsFinite(currentRootPosition) || !IsFinite(currentRootRotation))
                throw new ArgumentException("Predictive Foot world projection input is invalid.");
            plan.RootTrajectory.EvaluateEventPhase(
                state.ActionStepPhase,
                out Vector3 expectedRootPosition,
                out Quaternion expectedRootRotation);
            Vector3 up = plan.RootTrajectory.Up;
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
            state.WorldProjectionExpectedRoot = expectedRootPosition;
            state.WorldProjectionCurrentRoot = expectedRootPosition + Vector3.ProjectOnPlane(
                currentRootPosition - expectedRootPosition,
                up);
            state.WorldProjectionRotation = Quaternion.AngleAxis(yaw, up);
            state.HasWorldProjection = true;
            if (state.ActionStepPhase + 0.000001f >= plan.ApproachContactPhase)
                state.WorldProjectionFrozen = true;
        }

        internal static void ObserveWorldMotionDeviation(
            CharacterPredictiveFootPlacementPlan plan,
            ref CharacterPredictiveFootPlanExecutionState state,
            Vector3 currentPresentedBodyPosition,
            Quaternion currentPresentedBodyRotation,
            float landingPlanarTolerance)
        {
            if (!IsExecutable(in state))
                return;
            state.MotionLinearLandingError = 0f;
            state.MotionAngularLandingError = 0f;
            state.MotionLandingError = 0f;
            state.MotionLandingTolerance = float.IsFinite(landingPlanarTolerance) &&
                                           landingPlanarTolerance > 0f
                ? landingPlanarTolerance
                : 0f;
            if (!IsFinite(currentPresentedBodyPosition) ||
                !IsFinite(currentPresentedBodyRotation) ||
                !float.IsFinite(landingPlanarTolerance) ||
                landingPlanarTolerance <= 0f ||
                state.ActionStepPhase + 0.000001f < plan.LiftOffPhase ||
                state.ActionStepPhase >= 0.9999f)
            {
                return;
            }
            Vector3 expectedBodyPosition = plan.RootTrajectory
                .EvaluatePresentedBodyPositionAtEventPhase(state.ActionStepPhase);
            float linearLandingError = Vector3.ProjectOnPlane(
                    currentPresentedBodyPosition - ProjectWorldPoint(in state, expectedBodyPosition),
                    plan.RootTrajectory.Up)
                .magnitude;
            state.MotionLinearLandingError = linearLandingError;
            plan.RootTrajectory.EvaluateEventPhase(
                state.ActionStepPhase,
                out _,
                out Quaternion expectedBodyRotation);
            float angularDifference = Quaternion.Angle(
                (state.WorldProjectionRotation * expectedBodyRotation).normalized,
                currentPresentedBodyRotation) * Mathf.Deg2Rad;
            float angularLever = Mathf.Max(
                plan.SoleSupportRadius,
                plan.RootTrajectory.EvaluateRemainingPlanarDistance(state.ActionStepPhase));
            float angularLandingError =
                2f * angularLever * Mathf.Sin(angularDifference * 0.5f);
            state.MotionAngularLandingError = angularLandingError;
            state.MotionLandingError = Mathf.Sqrt(
                linearLandingError * linearLandingError +
                angularLandingError * angularLandingError);
        }

        internal static Matrix4x4 ResolveWorldProjectionMatrix(
            in CharacterPredictiveFootPlanExecutionState state) =>
            state.HasWorldProjection
                ? Matrix4x4.TRS(
                      state.WorldProjectionCurrentRoot,
                      state.WorldProjectionRotation,
                      Vector3.one) *
                  Matrix4x4.Translate(-state.WorldProjectionExpectedRoot)
                : Matrix4x4.identity;

        internal static Vector3 ProjectWorldPoint(
            in CharacterPredictiveFootPlanExecutionState state,
            Vector3 point) =>
            state.HasWorldProjection
                ? state.WorldProjectionCurrentRoot +
                  state.WorldProjectionRotation * (point - state.WorldProjectionExpectedRoot)
                : point;

        internal static Quaternion ProjectWorldRotation(
            in CharacterPredictiveFootPlanExecutionState state,
            Quaternion rotation) =>
            state.HasWorldProjection
                ? (state.WorldProjectionRotation * rotation).normalized
                : rotation;

        internal static FootPlacementSurface ProjectSurface(
            in CharacterPredictiveFootPlanExecutionState state,
            FootPlacementSurface surface)
        {
            if (!surface.IsValid || !state.HasWorldProjection)
                return surface;
            Vector3 normal = state.WorldProjectionRotation * surface.Normal;
            return new FootPlacementSurface(
                surface.Collider,
                ProjectWorldPoint(in state, surface.Point),
                normal.normalized);
        }

        internal static bool IsExecutable(in CharacterPredictiveFootPlanExecutionState state) =>
            state.State == CharacterPredictiveFootPlanState.Planned ||
            state.State == CharacterPredictiveFootPlanState.Executing;

        static void Complete(
            ref CharacterPredictiveFootPlanExecutionState state,
            CharacterPredictiveFootPlanEndReason reason)
        {
            if (reason == CharacterPredictiveFootPlanEndReason.None)
                throw new ArgumentOutOfRangeException(nameof(reason));
            state.State = CharacterPredictiveFootPlanState.Completed;
            state.TransitionReason = CharacterPredictiveFootPlanTransitionReason.PlanEnded;
            state.EndReason = reason;
        }

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        static bool IsFinite(Quaternion value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) &&
            float.IsFinite(value.z) && float.IsFinite(value.w) &&
            Quaternion.Dot(value, value) > 0.000001f;
    }

    internal sealed class CharacterPredictiveFootPlanExecution
    {
        readonly CharacterPredictiveFootPlacementPlan m_Plan;
        readonly CharacterPredictiveFootPlacementPlan.Builder m_Builder;
        CharacterPredictiveFootPlanExecutionState m_State;

        internal CharacterPredictiveFootPlanExecution(CharacterFootSide side, int pathCapacity)
        {
            m_Plan = new CharacterPredictiveFootPlacementPlan(side, pathCapacity);
            m_Builder = new CharacterPredictiveFootPlacementPlan.Builder(m_Plan);
            m_State.Reset(CharacterPredictiveFootPlanEndReason.PresentationReset);
        }

        internal ulong Sequence => m_Plan.Sequence;
        internal ulong LandingEventIdentity => m_Plan.LandingEventIdentity;
        internal ulong SourceSampleIdentity => m_Plan.SourceSampleIdentity;
        internal int SourceSampleCycle => m_Plan.SourceSampleCycle;
        internal int EventOrdinal => m_Plan.EventOrdinal;
        internal ulong ContributionContinuityIdentity => m_State.ContributionContinuityIdentity;
        internal ulong GeneratedFrame => m_Plan.GeneratedFrame;
        internal CharacterPredictiveFootPlanState State => m_State.State;
        internal CharacterPredictiveFootPlanTransitionReason TransitionReason =>
            m_State.TransitionReason;
        internal CharacterPredictiveFootPlanEndReason EndReason => m_State.EndReason;
        internal Vector3 Start => m_Plan.Start;
        internal Vector3 Landing => m_Plan.Landing;
        internal Vector3 RootStart => m_Plan.RootStart;
        internal Quaternion RootStartRotation => m_Plan.RootStartRotation;
        internal Vector3 RootLanding => m_Plan.RootLanding;
        internal Quaternion RootLandingRotation => m_Plan.RootLandingRotation;
        internal Vector3 PredictedHip => m_Plan.PredictedHip;
        internal CharacterPredictiveFootRootTrajectory RootTrajectory => m_Plan.RootTrajectory;
        internal CharacterPredictiveBodySupportPath BodySupportPath => m_Plan.BodySupportPath;
        internal FixedList512Bytes<Vector3> AuthoredFootPlanarRoute => m_Plan.AuthoredFootPlanarRoute;
        internal FixedList512Bytes<Vector3> RootLocalHipRoute => m_Plan.RootLocalHipRoute;
        internal FixedList128Bytes<float> AnimationClearanceHeights =>
            m_Plan.AnimationClearanceHeights;
        internal float ReleasePhase => m_Plan.ReleasePhase;
        internal float ApproachContactPhase => m_Plan.ApproachContactPhase;
        internal FixedList512Bytes<Vector3> FrozenWorldFootRoute => m_Plan.FrozenWorldFootRoute;
        internal FixedList128Bytes<float> FrozenWorldFootRoutePhases =>
            m_Plan.FrozenWorldFootRoutePhases;
        internal FixedList512Bytes<float> GroundPathRatePhases => m_Plan.GroundPathRatePhases;
        internal FixedList512Bytes<float> GroundPathRates => m_Plan.GroundPathRates;
        internal float AnimationClearanceContinuityOffset =>
            m_Plan.AnimationClearanceContinuityOffset;
        internal float LandingDelayAtGeneration => m_Plan.LandingDelayAtGeneration;
        internal float EventPhaseAtGeneration => m_Plan.EventPhaseAtGeneration;
        internal float LiftOffPhase => m_Plan.LiftOffPhase;
        internal float ActionStepDurationSeconds => m_Plan.ActionStepDurationSeconds;
        internal float ActionStepPhase => m_State.ActionStepPhase;
        internal float ActionProgress => m_State.ActionProgress;
        internal float GroundPathProgress => m_State.GroundPathProgress;
        internal float MotionLinearLandingError => m_State.MotionLinearLandingError;
        internal float MotionAngularLandingError => m_State.MotionAngularLandingError;
        internal float MotionLandingError => m_State.MotionLandingError;
        internal float MotionLandingTolerance => m_State.MotionLandingTolerance;
        internal bool WorldProjectionFrozen => m_State.WorldProjectionFrozen;
        internal Vector3 WorldProjectionExpectedRoot => m_State.WorldProjectionExpectedRoot;
        internal Vector3 WorldProjectionCurrentRoot => m_State.WorldProjectionCurrentRoot;
        internal Vector3 ExpectedPresentedBodyPosition =>
            RootTrajectory.EvaluatePresentedBodyPositionAtEventPhase(ActionStepPhase);
        internal Vector3 ProjectedExpectedPresentedBodyPosition =>
            ProjectWorldPoint(ExpectedPresentedBodyPosition);
        internal float SoleSupportRadius => m_Plan.SoleSupportRadius;
        internal ulong ActionClockFrame => m_State.ActionClockFrame;
        internal FootPlacementSurface FutureSupport => m_Plan.FutureSupport;
        internal CharacterFootPlacementQueryRequest FutureLandingRequest =>
            m_Plan.FutureLandingRequest;
        internal float VirtualGroundSplitEventPhase => m_Plan.VirtualGroundSplitEventPhase;
        internal Vector3 VirtualGroundOpposingLanding => m_Plan.VirtualGroundOpposingLanding;
        internal Vector3 VirtualGroundSplitRoutePoint => m_Plan.VirtualGroundSplitRoutePoint;
        internal float VirtualGroundSplitPlanarError => m_Plan.VirtualGroundSplitPlanarError;
        internal float VirtualGroundSplitFraction => m_Plan.VirtualGroundSplitFraction;
        internal FootPlacementSurface VirtualGroundSplitSupport =>
            m_Plan.VirtualGroundSplitSupport;
        internal ulong VirtualGroundSplitLandingEventIdentity =>
            m_Plan.VirtualGroundSplitLandingEventIdentity;
        internal int GroundEnvelopeSegmentCount => m_Plan.GroundEnvelopeSegmentCount;
        internal FootPlacementGroundEnvelopeRejectReason GroundEnvelopeRejectReason =>
            m_Plan.GroundEnvelopeRejectReason;
        internal int QueryCount => m_Plan.QueryCount;
        internal int RawHitCount => m_Plan.RawHitCount;
        internal int RouteSampleCount => m_Plan.RouteSampleCount;
        internal int FootRateSampleCount => m_Plan.FootRateSampleCount;
        internal int AcceptedHitCount => m_Plan.AcceptedHitCount;
        internal int EdgePlaneCandidateCount => m_Plan.EdgePlaneCandidateCount;
        internal int AcceptedEdgePlaneCount => m_Plan.AcceptedEdgePlaneCount;
        internal int RejectedQueryCount => m_Plan.RejectedQueryCount;
        internal CharacterPredictiveFootQueryRejectCounts QueryRejectCounts =>
            m_Plan.QueryRejectCounts;
        internal FootPredictionRejectReason CreationRejectReason => m_Plan.CreationRejectReason;
        internal int QueryRequestSnapshotCount => m_Plan.QueryRequestSnapshotCount;
        internal int AcceptedSupportSnapshotCount => m_Plan.AcceptedSupportSnapshotCount;
        internal int RejectedGeometrySnapshotCount => m_Plan.RejectedGeometrySnapshotCount;
        internal CharacterPredictiveFootPlanGeometrySnapshot GeometrySnapshot =>
            m_Plan.GeometrySnapshot;
        internal Matrix4x4 WorldProjectionMatrix =>
            CharacterPredictiveFootPlanEvaluator.ResolveWorldProjectionMatrix(in m_State);
        internal Vector3 ProjectedStart => ProjectWorldPoint(Start);
        internal Vector3 ProjectedLanding => ProjectWorldPoint(Landing);
        internal Vector3 ProjectedPredictedHip => ProjectWorldPoint(PredictedHip);
        internal Vector3 ProjectedRootStart => ProjectWorldPoint(RootStart);
        internal Quaternion ProjectedRootStartRotation => ProjectWorldRotation(RootStartRotation);
        internal Vector3 ProjectedRootLanding => ProjectWorldPoint(RootLanding);
        internal Vector3 ProjectedPresentedBodyLanding => ProjectWorldPoint(
            RootTrajectory.EvaluatePresentedBodyLandingPosition());
        internal FootPlacementSurface ProjectedFutureSupport => ProjectSurface(FutureSupport);
        internal Quaternion ProjectedRootLandingRotation => ProjectWorldRotation(RootLandingRotation);
        internal bool OwnsEvent => m_Plan.OwnsEvent;
        internal bool HasAttempt => m_Plan.HasAttempt;
        internal bool HasExecutablePath =>
            m_Plan.WasBuiltExecutable &&
            CharacterPredictiveFootPlanEvaluator.IsExecutable(in m_State);
        internal bool HasPathGeometry => m_Plan.HasPathGeometry;
        internal CharacterPredictiveFootPlacementPlan ImmutablePlan => m_Plan;

        internal void CopyFrom(CharacterPredictiveFootPlanExecution source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            m_Builder.CopyFrom(source.m_Plan);
            m_State = source.m_State;
        }

        internal void BeginFrame() =>
            CharacterPredictiveFootPlanEvaluator.BeginFrame(ref m_State);

        internal bool MatchesAuthoritativeEvent(in AnimationPredictedFootStepSample step) =>
            m_Plan.MatchesAuthoritativeEvent(in step);

        internal void SynchronizePoseContribution(in AnimationPredictedFootStepSample step) =>
            CharacterPredictiveFootPlanEvaluator.SynchronizePoseContribution(
                m_Plan,
                ref m_State,
                in step);

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
            m_Builder.BuildExecutable(
                sequence,
                generatedFrame,
                in step,
                start,
                landing,
                in rootTrajectory,
                predictedHip,
                in query);
            m_State.Initialize(m_Plan);
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
            m_Builder.BuildRejected(
                sequence,
                generatedFrame,
                in step,
                start,
                landing,
                in rootTrajectory,
                predictedHip,
                rejectReason,
                in query);
            m_State.Initialize(m_Plan);
        }

        internal void SynchronizeActionClock(
            ulong renderFrame,
            in AnimationPredictedFootStepSample step) =>
            CharacterPredictiveFootPlanEvaluator.SynchronizeActionClock(
                m_Plan,
                ref m_State,
                renderFrame,
                in step);

        internal float EvaluateGroundPathProgress(float eventPhase) =>
            m_Plan.EvaluateGroundPathProgress(eventPhase);

        internal float EvaluatePredictiveOutputWeight()
        {
            if (!HasExecutablePath || ActionStepPhase <= ReleasePhase)
                return 0f;
            if (ActionStepPhase >= LiftOffPhase)
                return 1f;
            float value = Mathf.InverseLerp(ReleasePhase, LiftOffPhase, ActionStepPhase);
            return value * value * (3f - 2f * value);
        }

        internal void ObserveWorldMotionDeviation(
            Vector3 currentPresentedBodyPosition,
            Quaternion currentPresentedBodyRotation,
            float landingPlanarTolerance) =>
            CharacterPredictiveFootPlanEvaluator.ObserveWorldMotionDeviation(
                m_Plan,
                ref m_State,
                currentPresentedBodyPosition,
                currentPresentedBodyRotation,
                landingPlanarTolerance);

        internal void UpdateWorldProjection(
            Vector3 currentRootPosition,
            Quaternion currentRootRotation) =>
            CharacterPredictiveFootPlanEvaluator.UpdateWorldProjection(
                m_Plan,
                ref m_State,
                currentRootPosition,
                currentRootRotation);

        internal void EvaluateGroundPath(
            float progress,
            out Vector3 pathPosition,
            out FootPlacementSurface support)
        {
            m_Plan.EvaluateGroundPathLocal(progress, out pathPosition, out support);
            pathPosition = ProjectWorldPoint(pathPosition);
            support = ProjectSurface(support);
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
            m_Plan.EvaluateFootMotionLocal(
                eventPhase,
                out planarSole,
                out animationClearanceHeight,
                out constraintMode,
                out supportPhase,
                out orientationPolicy,
                out bodyPivotMode);
            planarSole = ProjectWorldPoint(planarSole);
        }

        internal void EvaluateBodyPath(float eventPhase, out Vector3 root, out Vector3 hip)
        {
            m_Plan.EvaluateBodyPathLocal(eventPhase, out root, out hip);
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
            out AnimationBodyRotationPivotMode bodyPivotMode) =>
            m_Plan.EvaluateActionState(
                ActionStepPhase,
                out constraintMode,
                out supportPhase,
                out orientationPolicy,
                out bodyPivotMode);

        internal void EvaluateActionState(
            float eventPhase,
            out AnimationFootConstraintMode constraintMode,
            out AnimationFootSupportPhase supportPhase,
            out AnimationFootOrientationPolicy orientationPolicy,
            out AnimationBodyRotationPivotMode bodyPivotMode) =>
            m_Plan.EvaluateActionState(
                eventPhase,
                out constraintMode,
                out supportPhase,
                out orientationPolicy,
                out bodyPivotMode);

        internal void EvaluateCurrentAnimationClearance(
            out float authoredClearanceHeight,
            out float continuityOffset,
            out float continuityContribution,
            out float reachClearanceHeight,
            out float compositeClearanceHeight) =>
            m_Plan.EvaluateAnimationClearance(
                ActionStepPhase,
                out authoredClearanceHeight,
                out continuityOffset,
                out continuityContribution,
                out reachClearanceHeight,
                out compositeClearanceHeight);

        internal void EvaluateAnimationClearance(
            float eventPhase,
            out float authoredClearanceHeight,
            out float continuityOffset,
            out float continuityContribution,
            out float reachClearanceHeight,
            out float compositeClearanceHeight) =>
            m_Plan.EvaluateAnimationClearance(
                eventPhase,
                out authoredClearanceHeight,
                out continuityOffset,
                out continuityContribution,
                out reachClearanceHeight,
                out compositeClearanceHeight);

        internal FootPlacementGroundEnvelopeSegment GetPathSegment(int index)
        {
            FootPlacementGroundEnvelopeSegment segment = m_Plan.GetPathSegmentLocal(index);
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

        internal Vector3 GetPlannedFootRouteSample(int index) =>
            ProjectWorldPoint(m_Plan.GetPlannedFootRouteSampleLocal(index));

        internal void Reset(CharacterPredictiveFootPlanEndReason reason)
        {
            m_Builder.Clear();
            m_State.Reset(reason);
        }

        Vector3 ProjectWorldPoint(Vector3 point) =>
            CharacterPredictiveFootPlanEvaluator.ProjectWorldPoint(in m_State, point);

        Quaternion ProjectWorldRotation(Quaternion rotation) =>
            CharacterPredictiveFootPlanEvaluator.ProjectWorldRotation(in m_State, rotation);

        FootPlacementSurface ProjectSurface(FootPlacementSurface surface) =>
            CharacterPredictiveFootPlanEvaluator.ProjectSurface(in m_State, surface);
    }
}
