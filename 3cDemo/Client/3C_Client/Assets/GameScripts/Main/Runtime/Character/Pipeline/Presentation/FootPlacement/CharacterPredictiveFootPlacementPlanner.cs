using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public enum CharacterFootPlanTransitionKind : byte
    {
        None = 0,
        IntentRevision = 1,
        EventSuccessor = 2,
        PredictiveExit = 3
    }

    public enum CharacterFootPlanAttemptKind : byte
    {
        None = 0,
        Initial = 1,
        IntentRevision = 2,
        EventSuccessor = 3,
        CurrentEventReplacement = 4
    }

    public enum CharacterFootPlanBuildDecisionReason : byte
    {
        None = 0,
        Attempted = 1,
        ActivePlanExecuting = 2,
        NoAuthoritativeEvent = 3,
        OutsidePlanningWindow = 4,
        MotionTimelineUnavailable = 5,
        ConfidenceBelowMinimum = 6,
        StepComplete = 7,
        IncomingEventUnavailable = 8,
        AwaitingApproachContact = 9,
        IncomingEventAlreadyOwned = 10,
        MotionOutsideCommitTolerance = 11,
        TransitionOccupied = 12,
        OriginUnavailable = 13,
        FutureBodyUnavailable = 14,
        BuildRevisionAlreadyAttempted = 15,
        PredictiveExitActive = 16,
        EligibleButNotAttempted = 17
    }

    public enum CharacterFootPlanOriginKind : byte
    {
        None = 0,
        CurrentFrameSupport = 1,
        ActivePlanOutput = 2,
        ProjectedLanding = 3,
        LandingHandoff = 4,
        CommittedLanding = 5,
        CommittedStanceSupport = 6
    }

    internal readonly struct CharacterFootPlanSupportFact
    {
        internal CharacterFootPlanSupportFact(
            CharacterFootPlanOriginKind kind,
            ulong sourcePlanSequence,
            ulong sourceLandingEventIdentity,
            Vector3 sole,
            FootPlacementSurface support)
        {
            Kind = kind;
            SourcePlanSequence = sourcePlanSequence;
            SourceLandingEventIdentity = sourceLandingEventIdentity;
            Sole = sole;
            Support = support;
        }

        internal CharacterFootPlanOriginKind Kind { get; }
        internal ulong SourcePlanSequence { get; }
        internal ulong SourceLandingEventIdentity { get; }
        internal Vector3 Sole { get; }
        internal FootPlacementSurface Support { get; }
        internal bool IsAvailable => Kind != CharacterFootPlanOriginKind.None && IsFinite(Sole);
        internal bool HasSupport => IsAvailable && Support.IsValid;

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }

    internal readonly struct CharacterFootPlanSupportFacts
    {
        internal CharacterFootPlanSupportFacts(
            in CharacterFootPlanSupportFact currentQuery,
            in CharacterFootPlanSupportFact committedStance)
        {
            CurrentQuery = currentQuery;
            CommittedStance = committedStance;
        }

        internal CharacterFootPlanSupportFact CurrentQuery { get; }
        internal CharacterFootPlanSupportFact CommittedStance { get; }
    }

    internal readonly struct CharacterFootPlanBuildOrigin
    {
        internal CharacterFootPlanBuildOrigin(
            CharacterFootPlanOriginKind kind,
            ulong sourcePlanSequence,
            ulong sourceLandingEventIdentity,
            Vector3 sole,
            Vector3 groundPath,
            FootPlacementSurface support,
            Vector3 up)
        {
            Kind = kind;
            SourcePlanSequence = sourcePlanSequence;
            SourceLandingEventIdentity = sourceLandingEventIdentity;
            Sole = sole;
            GroundPath = groundPath;
            Support = support;
            SoleHeightAboveSupport = support.IsValid
                ? Vector3.Dot(sole - support.Point, up.normalized)
                : 0f;
        }

        internal CharacterFootPlanOriginKind Kind { get; }
        internal ulong SourcePlanSequence { get; }
        internal ulong SourceLandingEventIdentity { get; }
        internal Vector3 Sole { get; }
        internal Vector3 GroundPath { get; }
        internal FootPlacementSurface Support { get; }
        internal float SoleHeightAboveSupport { get; }
        internal bool IsAvailable => Kind != CharacterFootPlanOriginKind.None &&
                                     IsFinite(Sole) &&
                                     IsFinite(GroundPath);
        internal bool HasSupport => IsAvailable && Support.IsValid;

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }

    internal readonly struct CharacterFootPlanBuildRequest
    {
        internal CharacterFootPlanBuildRequest(
            CharacterFootPlanAttemptKind attemptKind,
            CharacterFootSide side,
            in AnimationPredictedFootStepSample step,
            ulong renderFrame,
            in CharacterFootPlanBuildOrigin origin,
            float soleSupportRadius,
            Vector3 rootStart,
            Quaternion rootStartRotation,
            Vector3 presentedBodyStartPosition,
            Vector3 committedBodyVelocity,
            float trajectoryCurvatureDegreesPerSecond,
            bool trajectoryCurvatureAvailable,
            in CommittedLocomotionPlanarMotionTimeline motionTimeline,
            double movementPlaybackTime,
            Vector3 up,
            float legLength)
        {
            AttemptKind = attemptKind;
            Side = side;
            Step = step;
            RenderFrame = renderFrame;
            Origin = origin;
            SoleSupportRadius = soleSupportRadius;
            RootStart = rootStart;
            RootStartRotation = rootStartRotation;
            PresentedBodyStartPosition = presentedBodyStartPosition;
            CommittedBodyVelocity = committedBodyVelocity;
            TrajectoryCurvatureDegreesPerSecond = trajectoryCurvatureDegreesPerSecond;
            TrajectoryCurvatureAvailable = trajectoryCurvatureAvailable;
            MotionTimeline = motionTimeline;
            MovementPlaybackTime = movementPlaybackTime;
            Up = up;
            LegLength = legLength;
        }

        internal CharacterFootPlanAttemptKind AttemptKind { get; }
        internal CharacterFootSide Side { get; }
        internal AnimationPredictedFootStepSample Step { get; }
        internal ulong RenderFrame { get; }
        internal CharacterFootPlanBuildOrigin Origin { get; }
        internal float SoleSupportRadius { get; }
        internal Vector3 RootStart { get; }
        internal Quaternion RootStartRotation { get; }
        internal Vector3 PresentedBodyStartPosition { get; }
        internal Vector3 CommittedBodyVelocity { get; }
        internal float TrajectoryCurvatureDegreesPerSecond { get; }
        internal bool TrajectoryCurvatureAvailable { get; }
        internal CommittedLocomotionPlanarMotionTimeline MotionTimeline { get; }
        internal double MovementPlaybackTime { get; }
        internal Vector3 Up { get; }
        internal float LegLength { get; }
    }

    public readonly struct CharacterFootPlanBuildRequestDiagnostics
    {
        internal CharacterFootPlanBuildRequestDiagnostics(in CharacterFootPlanBuildRequest request)
        {
            AnimationPredictedFootStepSample step = request.Step;
            CommittedLocomotionPlanarMotionTimeline motion = request.MotionTimeline;
            SourceSampleIdentity = step.SourceSampleIdentity;
            SourceSampleCycle = step.SourceSampleCycle;
            EventOrdinal = step.EventOrdinal;
            EventPhase = step.ActionStepClock.Phase;
            TimeToLandingSeconds = step.ActionStepClock.TimeToLandingSeconds;
            MotionGeneration = motion.Generation;
            MotionAuthorityTick = motion.AuthorityTick.Value;
            MotionCurrentVelocity = new Vector2(motion.CurrentVelocityX, motion.CurrentVelocityZ);
            MotionContinuationVelocity = new Vector2(
                motion.ContinuationVelocityX,
                motion.ContinuationVelocityZ);
            MotionYawVelocityDegreesPerSecond = motion.YawVelocityDegreesPerSecond;
            RootStart = request.RootStart;
            RootStartRotation = request.RootStartRotation;
            PresentedBodyStartPosition = request.PresentedBodyStartPosition;
            CommittedBodyVelocity = request.CommittedBodyVelocity;
            TrajectoryCurvatureDegreesPerSecond = request.TrajectoryCurvatureDegreesPerSecond;
            TrajectoryCurvatureAvailable = request.TrajectoryCurvatureAvailable;
            MovementPlaybackTime = request.MovementPlaybackTime;
            Up = request.Up;
            SoleSupportRadius = request.SoleSupportRadius;
            LegLength = request.LegLength;
        }

        public ulong SourceSampleIdentity { get; }
        public int SourceSampleCycle { get; }
        public int EventOrdinal { get; }
        public float EventPhase { get; }
        public float TimeToLandingSeconds { get; }
        public ulong MotionGeneration { get; }
        public ulong MotionAuthorityTick { get; }
        public Vector2 MotionCurrentVelocity { get; }
        public Vector2 MotionContinuationVelocity { get; }
        public float MotionYawVelocityDegreesPerSecond { get; }
        public Vector3 RootStart { get; }
        public Quaternion RootStartRotation { get; }
        public Vector3 PresentedBodyStartPosition { get; }
        public Vector3 CommittedBodyVelocity { get; }
        public float TrajectoryCurvatureDegreesPerSecond { get; }
        public bool TrajectoryCurvatureAvailable { get; }
        public double MovementPlaybackTime { get; }
        public Vector3 Up { get; }
        public float SoleSupportRadius { get; }
        public float LegLength { get; }
    }

    public readonly struct CharacterFootPlanAttemptDiagnostics
    {
        internal CharacterFootPlanAttemptDiagnostics(
            CharacterFootPlanAttemptKind kind,
            CharacterPredictiveFootPlanExecution plan,
            in CharacterFootPlanBuildRequest request)
            : this(
                kind,
                plan.Sequence,
                plan.GeneratedFrame,
                plan.LandingEventIdentity,
                plan.State,
                plan.CreationRejectReason,
                plan.GroundEnvelopeRejectReason,
                plan.QueryCount,
                plan.RawHitCount,
                plan.RejectedQueryCount,
                plan.GeometrySnapshot,
                in request)
        {
        }

        internal CharacterFootPlanAttemptDiagnostics(
            CharacterFootPlanAttemptKind kind,
            ulong sequence,
            ulong generatedFrame,
            ulong landingEventIdentity,
            CharacterPredictiveFootPlanState state,
            FootPredictionRejectReason rejectReason,
            FootPlacementGroundEnvelopeRejectReason groundEnvelopeRejectReason,
            int queryCount,
            int rawHitCount,
            int rejectedQueryCount,
            CharacterPredictiveFootPlanGeometrySnapshot geometrySnapshot,
            in CharacterFootPlanBuildRequest request)
        {
            CharacterFootPlanBuildOrigin origin = request.Origin;
            Kind = kind;
            Sequence = sequence;
            GeneratedFrame = generatedFrame;
            LandingEventIdentity = landingEventIdentity;
            State = state;
            RejectReason = rejectReason;
            GroundEnvelopeRejectReason = groundEnvelopeRejectReason;
            QueryCount = queryCount;
            RawHitCount = rawHitCount;
            RejectedQueryCount = rejectedQueryCount;
            GeometrySnapshot = geometrySnapshot;
            OriginKind = origin.Kind;
            OriginPlanSequence = origin.SourcePlanSequence;
            OriginLandingEventIdentity = origin.SourceLandingEventIdentity;
            OriginSole = origin.Sole;
            OriginGroundPath = origin.GroundPath;
            OriginSupportSurfaceIdentity = origin.Support.Identity;
            OriginSupportPoint = origin.Support.IsValid ? origin.Support.Point : default;
            OriginSupportNormal = origin.Support.IsValid ? origin.Support.Normal : default;
            OriginSoleHeightAboveSupport = origin.SoleHeightAboveSupport;
            BuildRequest = new CharacterFootPlanBuildRequestDiagnostics(in request);
        }

        public CharacterFootPlanAttemptKind Kind { get; }
        public ulong Sequence { get; }
        public ulong GeneratedFrame { get; }
        public ulong LandingEventIdentity { get; }
        public CharacterPredictiveFootPlanState State { get; }
        public FootPredictionRejectReason RejectReason { get; }
        public FootPlacementGroundEnvelopeRejectReason GroundEnvelopeRejectReason { get; }
        public int QueryCount { get; }
        public int RawHitCount { get; }
        public int RejectedQueryCount { get; }
        public CharacterPredictiveFootPlanGeometrySnapshot GeometrySnapshot { get; }
        public CharacterFootPlanOriginKind OriginKind { get; }
        public ulong OriginPlanSequence { get; }
        public ulong OriginLandingEventIdentity { get; }
        public Vector3 OriginSole { get; }
        public Vector3 OriginGroundPath { get; }
        public int OriginSupportSurfaceIdentity { get; }
        public Vector3 OriginSupportPoint { get; }
        public Vector3 OriginSupportNormal { get; }
        public float OriginSoleHeightAboveSupport { get; }
        public CharacterFootPlanBuildRequestDiagnostics BuildRequest { get; }
        public bool IsAvailable => Kind != CharacterFootPlanAttemptKind.None && Sequence != 0;
    }

    public readonly struct CharacterFootPlanBuildDecisionDiagnostics
    {
        internal CharacterFootPlanBuildDecisionDiagnostics(
            CharacterFootPlanAttemptKind candidateKind,
            CharacterFootPlanBuildDecisionReason reason,
            ulong landingEventIdentity,
            in CharacterFootPlanBuildOrigin origin,
            ulong motionGeneration,
            ulong motionAuthorityTick,
            bool attempted,
            bool currentPlanningCandidate,
            bool incomingPlanningCandidate,
            bool currentPlanMatches,
            bool activeEventReplaced,
            bool needsInitialPlan,
            bool intentRevisionRequested,
            bool canPrepareEventSuccessor,
            bool motionWithinCommitTolerance,
            bool canBeginTransition,
            bool futureBodyAvailable,
            bool currentPlanHasExecutablePath,
            bool planFadingOut)
        {
            CandidateKind = candidateKind;
            Reason = reason;
            LandingEventIdentity = landingEventIdentity;
            OriginKind = origin.Kind;
            OriginPlanSequence = origin.SourcePlanSequence;
            OriginLandingEventIdentity = origin.SourceLandingEventIdentity;
            OriginSupportSurfaceIdentity = origin.Support.Identity;
            MotionGeneration = motionGeneration;
            MotionAuthorityTick = motionAuthorityTick;
            Attempted = attempted;
            CurrentPlanningCandidate = currentPlanningCandidate;
            IncomingPlanningCandidate = incomingPlanningCandidate;
            CurrentPlanMatches = currentPlanMatches;
            ActiveEventReplaced = activeEventReplaced;
            NeedsInitialPlan = needsInitialPlan;
            IntentRevisionRequested = intentRevisionRequested;
            CanPrepareEventSuccessor = canPrepareEventSuccessor;
            MotionWithinCommitTolerance = motionWithinCommitTolerance;
            CanBeginTransition = canBeginTransition;
            FutureBodyAvailable = futureBodyAvailable;
            CurrentPlanHasExecutablePath = currentPlanHasExecutablePath;
            PlanFadingOut = planFadingOut;
        }

        public CharacterFootPlanAttemptKind CandidateKind { get; }
        public CharacterFootPlanBuildDecisionReason Reason { get; }
        public ulong LandingEventIdentity { get; }
        public CharacterFootPlanOriginKind OriginKind { get; }
        public ulong OriginPlanSequence { get; }
        public ulong OriginLandingEventIdentity { get; }
        public int OriginSupportSurfaceIdentity { get; }
        public ulong MotionGeneration { get; }
        public ulong MotionAuthorityTick { get; }
        public bool Attempted { get; }
        public bool CurrentPlanningCandidate { get; }
        public bool IncomingPlanningCandidate { get; }
        public bool CurrentPlanMatches { get; }
        public bool ActiveEventReplaced { get; }
        public bool NeedsInitialPlan { get; }
        public bool IntentRevisionRequested { get; }
        public bool CanPrepareEventSuccessor { get; }
        public bool MotionWithinCommitTolerance { get; }
        public bool CanBeginTransition { get; }
        public bool FutureBodyAvailable { get; }
        public bool CurrentPlanHasExecutablePath { get; }
        public bool PlanFadingOut { get; }
        public bool IsAvailable => Reason != CharacterFootPlanBuildDecisionReason.None;
    }

    internal readonly struct CharacterFootPlanTransitionCapture
    {
        internal CharacterFootPlanTransitionCapture(
            Vector3 originalAnklePosition,
            Quaternion originalAnkleRotation,
            Vector3 originalHipPosition)
        {
            if (!IsFinite(originalAnklePosition) || !IsUnit(originalAnkleRotation) ||
                !IsFinite(originalHipPosition))
            {
                throw new ArgumentException("Foot Plan transition capture is invalid.");
            }
            OriginalAnklePosition = originalAnklePosition;
            OriginalAnkleRotation = originalAnkleRotation.normalized;
            OriginalHipPosition = originalHipPosition;
        }

        internal Vector3 OriginalAnklePosition { get; }
        internal Quaternion OriginalAnkleRotation { get; }
        internal Vector3 OriginalHipPosition { get; }

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        static bool IsUnit(Quaternion value)
        {
            float magnitude = value.x * value.x + value.y * value.y +
                              value.z * value.z + value.w * value.w;
            return float.IsFinite(magnitude) && Mathf.Abs(magnitude - 1f) <= 0.01f;
        }
    }

    internal readonly struct CharacterFootPlanTransition
    {
        CharacterFootPlanTransition(
            CharacterFootPlanTransitionKind kind,
            CharacterPredictiveFootPlacementPlan previousPlan,
            CharacterPredictiveFootPlacementPlan nextPlan,
            float blend,
            ulong startedFrame,
            bool isSuccessorHandoff,
            bool hasContinuity,
            Vector3 ankleOffset,
            Quaternion ankleRotationOffset,
            Vector3 groundPath,
            FootPlacementSurface groundSupport,
            Vector3 pathRootOffset,
            Vector3 pathRootStartOffset,
            Vector3 pathHipOffset)
        {
            Kind = kind;
            PreviousPlan = previousPlan;
            NextPlan = nextPlan;
            Blend = Mathf.Clamp01(blend);
            StartedFrame = startedFrame;
            IsSuccessorHandoff = isSuccessorHandoff;
            HasContinuity = hasContinuity;
            AnkleOffset = ankleOffset;
            AnkleRotationOffset = ankleRotationOffset;
            GroundPath = groundPath;
            GroundSupport = groundSupport;
            PathRootOffset = pathRootOffset;
            PathRootStartOffset = pathRootStartOffset;
            PathHipOffset = pathHipOffset;
        }

        internal CharacterFootPlanTransitionKind Kind { get; }
        internal CharacterPredictiveFootPlacementPlan PreviousPlan { get; }
        internal CharacterPredictiveFootPlacementPlan NextPlan { get; }
        internal float Blend { get; }
        internal ulong StartedFrame { get; }
        internal bool IsSuccessorHandoff { get; }
        internal bool HasContinuity { get; }
        internal Vector3 AnkleOffset { get; }
        internal Quaternion AnkleRotationOffset { get; }
        internal Vector3 GroundPath { get; }
        internal FootPlacementSurface GroundSupport { get; }
        internal Vector3 PathRootOffset { get; }
        internal Vector3 PathRootStartOffset { get; }
        internal Vector3 PathHipOffset { get; }
        internal bool IsRevision =>
            Kind == CharacterFootPlanTransitionKind.IntentRevision ||
            Kind == CharacterFootPlanTransitionKind.EventSuccessor && !IsSuccessorHandoff;

        internal static CharacterFootPlanTransition Begin(
            CharacterFootPlanTransitionKind kind,
            CharacterPredictiveFootPlacementPlan previousPlan,
            CharacterPredictiveFootPlacementPlan nextPlan,
            ulong startedFrame = 0) =>
            new CharacterFootPlanTransition(
                kind,
                previousPlan,
                nextPlan,
                0f,
                startedFrame,
                false,
                false,
                Vector3.zero,
                Quaternion.identity,
                Vector3.zero,
                default,
                Vector3.zero,
                Vector3.zero,
                Vector3.zero);

        internal CharacterFootPlanTransition WithBlend(float blend) =>
            new CharacterFootPlanTransition(
                Kind,
                PreviousPlan,
                NextPlan,
                blend,
                StartedFrame,
                IsSuccessorHandoff,
                HasContinuity,
                AnkleOffset,
                AnkleRotationOffset,
                GroundPath,
                GroundSupport,
                PathRootOffset,
                PathRootStartOffset,
                PathHipOffset);

        internal CharacterFootPlanTransition WithStartedFrame(ulong startedFrame) =>
            new CharacterFootPlanTransition(
                Kind,
                PreviousPlan,
                NextPlan,
                Blend,
                startedFrame,
                IsSuccessorHandoff,
                HasContinuity,
                AnkleOffset,
                AnkleRotationOffset,
                GroundPath,
                GroundSupport,
                PathRootOffset,
                PathRootStartOffset,
                PathHipOffset);

        internal CharacterFootPlanTransition Rebind(
            CharacterPredictiveFootPlacementPlan previousPlan,
            CharacterPredictiveFootPlacementPlan nextPlan)
        {
            if (Kind == CharacterFootPlanTransitionKind.None)
                return default;
            return new CharacterFootPlanTransition(
                Kind,
                previousPlan,
                nextPlan,
                Blend,
                StartedFrame,
                IsSuccessorHandoff,
                HasContinuity,
                AnkleOffset,
                AnkleRotationOffset,
                GroundPath,
                GroundSupport,
                PathRootOffset,
                PathRootStartOffset,
                PathHipOffset);
        }

        internal CharacterFootPlanTransition WithContinuity(
            Vector3 ankleOffset,
            Quaternion ankleRotationOffset,
            Vector3 groundPath,
            FootPlacementSurface groundSupport,
            Vector3 pathRootOffset,
            Vector3 pathRootStartOffset,
            Vector3 pathHipOffset) =>
            new CharacterFootPlanTransition(
                Kind,
                PreviousPlan,
                NextPlan,
                Blend,
                StartedFrame,
                IsSuccessorHandoff,
                true,
                ankleOffset,
                ankleRotationOffset.normalized,
                groundPath,
                groundSupport,
                pathRootOffset,
                pathRootStartOffset,
                pathHipOffset);

        internal CharacterFootPlanTransition PromoteEventSuccessor(
            CharacterPredictiveFootPlacementPlan activePlan)
        {
            if (Kind != CharacterFootPlanTransitionKind.EventSuccessor ||
                IsSuccessorHandoff || !HasContinuity || activePlan == null)
            {
                throw new InvalidOperationException("Predictive Foot Event Successor handoff is invalid.");
            }
            return new CharacterFootPlanTransition(
                Kind,
                null,
                activePlan,
                0f,
                0,
                true,
                true,
                AnkleOffset,
                AnkleRotationOffset,
                GroundPath,
                GroundSupport,
                PathRootOffset,
                PathRootStartOffset,
                PathHipOffset);
        }
    }

    internal sealed class CharacterPredictiveFootPlacementPlanner
    {
        internal readonly struct StateSnapshot
        {
            internal StateSnapshot(
                ulong nextPlanSequence,
                float trajectoryCurvatureDegreesPerSecond,
                bool trajectoryCurvatureAvailable,
                in CharacterPredictiveFootPlacementDiagnostics diagnostics)
            {
                NextPlanSequence = nextPlanSequence;
                TrajectoryCurvatureDegreesPerSecond = trajectoryCurvatureDegreesPerSecond;
                TrajectoryCurvatureAvailable = trajectoryCurvatureAvailable;
                Diagnostics = diagnostics;
            }

            internal ulong NextPlanSequence { get; }
            internal float TrajectoryCurvatureDegreesPerSecond { get; }
            internal bool TrajectoryCurvatureAvailable { get; }
            internal CharacterPredictiveFootPlacementDiagnostics Diagnostics { get; }
        }

        internal readonly struct CharacterFootCompletedOutput
        {
            internal CharacterFootCompletedOutput(
                Vector3 originalAnklePosition,
                Quaternion originalAnkleRotation,
                Vector3 originalHipPosition,
                Vector3 finalAnklePosition,
                Quaternion finalAnkleRotation,
                Vector3 finalSolePosition,
                bool hasGroundPath,
                Vector3 groundPathPosition,
                in FootPlacementSurface groundSupport,
                ulong planSequence,
                Vector3 pathRootPosition,
                Vector3 pathRootStartPosition,
                Vector3 pathHipPosition)
            {
                IsAvailable = true;
                OriginalAnklePosition = originalAnklePosition;
                OriginalAnkleRotation = originalAnkleRotation.normalized;
                OriginalHipPosition = originalHipPosition;
                FinalAnklePosition = finalAnklePosition;
                FinalAnkleRotation = finalAnkleRotation.normalized;
                FinalSolePosition = finalSolePosition;
                HasGroundPath = hasGroundPath;
                GroundPathPosition = hasGroundPath ? groundPathPosition : Vector3.zero;
                GroundSupport = hasGroundPath ? groundSupport : default;
                PlanSequence = hasGroundPath ? planSequence : 0;
                PathRootPosition = hasGroundPath ? pathRootPosition : Vector3.zero;
                PathRootStartPosition = hasGroundPath ? pathRootStartPosition : Vector3.zero;
                PathHipPosition = hasGroundPath ? pathHipPosition : Vector3.zero;
            }

            internal bool IsAvailable { get; }
            internal Vector3 OriginalAnklePosition { get; }
            internal Quaternion OriginalAnkleRotation { get; }
            internal Vector3 OriginalHipPosition { get; }
            internal Vector3 FinalAnklePosition { get; }
            internal Quaternion FinalAnkleRotation { get; }
            internal Vector3 FinalSolePosition { get; }
            internal bool HasGroundPath { get; }
            internal Vector3 GroundPathPosition { get; }
            internal FootPlacementSurface GroundSupport { get; }
            internal ulong PlanSequence { get; }
            internal Vector3 PathRootPosition { get; }
            internal Vector3 PathRootStartPosition { get; }
            internal Vector3 PathHipPosition { get; }
        }

        internal sealed class CharacterFootPlanExecutionState
        {
            internal CharacterFootPlanExecutionState(CharacterFootSide side, int pathCapacity)
            {
                Active = new CharacterPredictiveFootPlanExecution(side, pathCapacity);
                Revision = new CharacterPredictiveFootPlanExecution(side, pathCapacity);
            }

            internal CharacterPredictiveFootPlanExecution Active { get; private set; }
            internal CharacterPredictiveFootPlanExecution Revision { get; private set; }
            CharacterFootPlanTransition m_Transition;
            internal CharacterFootPlanTransition Transition => m_Transition;
            internal CharacterFootPlanAttemptDiagnostics PlanAttempt { get; private set; }
            internal CharacterFootPlanBuildDecisionDiagnostics PlanBuildDecision { get; private set; }
            internal bool HasRevision => m_Transition.IsRevision;
            internal bool HasEventSuccessor =>
                m_Transition.Kind == CharacterFootPlanTransitionKind.EventSuccessor &&
                !m_Transition.IsSuccessorHandoff;
            internal bool HasEventSuccessorHandoff =>
                m_Transition.Kind == CharacterFootPlanTransitionKind.EventSuccessor &&
                m_Transition.IsSuccessorHandoff;
            internal bool CanBeginTransition =>
                m_Transition.Kind == CharacterFootPlanTransitionKind.None;
            internal bool HasIntentRevision =>
                m_Transition.Kind == CharacterFootPlanTransitionKind.IntentRevision;
            internal float TransitionBlendWeight => m_Transition.Blend;
            internal float SmoothedTransitionBlendWeight =>
                TransitionBlendWeight * TransitionBlendWeight * (3f - 2f * TransitionBlendWeight);
            internal float EventSuccessorHandoffBlendWeight => HasEventSuccessorHandoff
                ? SmoothedTransitionBlendWeight
                : 1f;
            internal bool IsFadingOut =>
                m_Transition.Kind == CharacterFootPlanTransitionKind.PredictiveExit;
            internal float FadeOutWeight => IsFadingOut ? m_Transition.Blend : 0f;
            internal ulong FadeOutStartedFrame => IsFadingOut ? m_Transition.StartedFrame : 0;
            internal float PredictiveRetentionWeight
            {
                get
                {
                    float value = Mathf.Clamp01(FadeOutWeight);
                    return IsFadingOut
                        ? 1f - value * value * (3f - 2f * value)
                        : 1f;
                }
            }
            internal float IntentLandingDisplacementError { get; private set; }
            internal float IntentLandingDisplacementThreshold { get; private set; }
            internal CharacterFootPlanAttemptKind LastPlanBuildAttemptKind { get; private set; }
            internal ulong LastPlanBuildAttemptSourceIdentity { get; private set; }
            internal ulong LastPlanBuildAttemptMotionGeneration { get; private set; }
            internal ulong LastPlanBuildAttemptAuthorityTick { get; private set; }
            internal CharacterFootCompletedOutput LastOutput { get; private set; }
            internal bool HasTransitionOrigin => m_Transition.HasContinuity;
            internal Vector3 TransitionOriginGroundPath => m_Transition.GroundPath;
            internal FootPlacementSurface TransitionOriginGroundSupport =>
                m_Transition.GroundSupport;
            ulong m_OutputContinuityPlanSequence;
            ulong m_OutputContinuityStartedFrame;
            float m_OutputContinuityWeight;
            Vector3 m_OutputContinuityPositionOffset;
            Quaternion m_OutputContinuityRotationOffset = Quaternion.identity;
            bool m_SuppressNextOutputContinuityCapture;

            internal void CopyFrom(CharacterFootPlanExecutionState source)
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                Active.CopyFrom(source.Active);
                Revision.CopyFrom(source.Revision);
                m_Transition = source.HasEventSuccessorHandoff
                    ? source.m_Transition.Rebind(null, Active.ImmutablePlan)
                    : source.m_Transition.Rebind(
                        Active.ImmutablePlan,
                        source.HasRevision ? Revision.ImmutablePlan : null);
                IntentLandingDisplacementError = source.IntentLandingDisplacementError;
                IntentLandingDisplacementThreshold = source.IntentLandingDisplacementThreshold;
                LastPlanBuildAttemptKind = source.LastPlanBuildAttemptKind;
                LastPlanBuildAttemptSourceIdentity = source.LastPlanBuildAttemptSourceIdentity;
                LastPlanBuildAttemptMotionGeneration = source.LastPlanBuildAttemptMotionGeneration;
                LastPlanBuildAttemptAuthorityTick = source.LastPlanBuildAttemptAuthorityTick;
                LastOutput = source.LastOutput;
                m_OutputContinuityPlanSequence = source.m_OutputContinuityPlanSequence;
                m_OutputContinuityStartedFrame = source.m_OutputContinuityStartedFrame;
                m_OutputContinuityWeight = source.m_OutputContinuityWeight;
                m_OutputContinuityPositionOffset = source.m_OutputContinuityPositionOffset;
                m_OutputContinuityRotationOffset = source.m_OutputContinuityRotationOffset;
                m_SuppressNextOutputContinuityCapture = source.m_SuppressNextOutputContinuityCapture;
                PlanAttempt = source.PlanAttempt;
                PlanBuildDecision = source.PlanBuildDecision;
            }

            internal void BeginFrame()
            {
                Active.BeginFrame();
                Revision.BeginFrame();
                PlanAttempt = default;
                PlanBuildDecision = default;
            }

            internal void RecordPlanAttempt(in CharacterFootPlanAttemptDiagnostics attempt)
            {
                if (!attempt.IsAvailable)
                    throw new InvalidOperationException("Predictive Foot plan attempt is invalid.");
                PlanAttempt = attempt;
            }

            internal void RecordPlanBuildDecision(
                in CharacterFootPlanBuildDecisionDiagnostics decision)
            {
                if (!decision.IsAvailable)
                    throw new InvalidOperationException("Predictive Foot plan build decision is invalid.");
                PlanBuildDecision = decision;
            }

            internal void BeginIntentRevision(in CharacterFootPlanTransitionCapture capture)
            {
                if (!Revision.HasExecutablePath)
                    throw new InvalidOperationException("Predictive Foot revision is not executable.");
                ClearOutputContinuity();
                m_Transition = CaptureTransitionOrigin(
                    CharacterFootPlanTransition.Begin(
                        CharacterFootPlanTransitionKind.IntentRevision,
                        Active.ImmutablePlan,
                        Revision.ImmutablePlan),
                    in capture);
            }

            internal void BeginEventSuccessor()
            {
                if (!Revision.HasExecutablePath)
                    throw new InvalidOperationException("Predictive Foot successor is not executable.");
                m_Transition = CharacterFootPlanTransition.Begin(
                    CharacterFootPlanTransitionKind.EventSuccessor,
                    Active.ImmutablePlan,
                    Revision.ImmutablePlan);
            }

            internal void PromoteRevision(in CharacterFootPlanTransitionCapture capture)
            {
                if (!HasRevision || !Revision.HasExecutablePath)
                    throw new InvalidOperationException("Predictive Foot revision cannot be promoted.");
                CharacterFootPlanTransition transition = m_Transition;
                bool preserveSuccessorHandoff = HasEventSuccessor &&
                                                HasCompleteOutputForPlan(Active.Sequence);
                if (preserveSuccessorHandoff)
                    transition = CaptureTransitionOrigin(transition, in capture);
                ClearOutputContinuity();
                m_SuppressNextOutputContinuityCapture = !preserveSuccessorHandoff;
                Active.Reset(CharacterPredictiveFootPlanEndReason.EventReplaced);
                CharacterPredictiveFootPlanExecution retired = Active;
                Active = Revision;
                Revision = retired;
                m_Transition = preserveSuccessorHandoff
                    ? transition.PromoteEventSuccessor(Active.ImmutablePlan)
                    : default;
            }

            internal void CompleteEventSuccessorHandoff()
            {
                if (HasEventSuccessorHandoff)
                    m_Transition = default;
            }

            internal void BeginFadeOut(
                CharacterPredictiveFootPlanEndReason reason,
                ulong renderFrame,
                in CharacterFootPlanTransitionCapture capture)
            {
                if (renderFrame == 0)
                    throw new ArgumentOutOfRangeException(nameof(renderFrame));
                if (IsFadingOut)
                {
                    if (Revision.OwnsEvent)
                        Revision.Reset(reason);
                    return;
                }
                CancelRevision(reason);
                if (!Active.HasExecutablePath || !HasCompleteOutputForPlan(Active.Sequence))
                {
                    Active.Reset(reason);
                    m_Transition = default;
                    return;
                }
                m_Transition = CaptureTransitionOrigin(
                    CharacterFootPlanTransition.Begin(
                        CharacterFootPlanTransitionKind.PredictiveExit,
                        Active.ImmutablePlan,
                        null,
                        renderFrame),
                    in capture);
            }

            internal void AdvanceTransition(
                ulong renderFrame,
                float deltaSeconds,
                float blendSpeed,
                in CharacterFootPlanTransitionCapture capture)
            {
                bool blendRevision = HasIntentRevision;
                if (blendRevision)
                {
                    if (Revision.State != CharacterPredictiveFootPlanState.Executing)
                    {
                        m_Transition = m_Transition.WithBlend(0f);
                        return;
                    }
                    if (renderFrame <= Revision.GeneratedFrame)
                        return;
                    m_Transition = m_Transition.WithBlend(Mathf.MoveTowards(
                        TransitionBlendWeight,
                        1f,
                        blendSpeed * deltaSeconds));
                    if (TransitionBlendWeight < 0.999999f)
                        return;
                    PromoteRevision(in capture);
                    return;
                }
                if (HasEventSuccessorHandoff)
                {
                    if (Active.State != CharacterPredictiveFootPlanState.Executing)
                        return;
                    if (m_Transition.StartedFrame == 0)
                    {
                        m_Transition = m_Transition.WithStartedFrame(renderFrame);
                        return;
                    }
                    if (renderFrame <= m_Transition.StartedFrame)
                        return;
                    m_Transition = m_Transition.WithBlend(Mathf.MoveTowards(
                        TransitionBlendWeight,
                        1f,
                        blendSpeed * deltaSeconds));
                    return;
                }
                if (!IsFadingOut)
                    return;
                if (renderFrame <= FadeOutStartedFrame)
                    return;
                m_Transition = m_Transition.WithBlend(Mathf.MoveTowards(
                    TransitionBlendWeight,
                    1f,
                    blendSpeed * deltaSeconds));
                if (FadeOutWeight < 0.999999f)
                    return;
                Active.Reset(CharacterPredictiveFootPlanEndReason.EventReplaced);
                m_Transition = default;
            }

            internal void CancelRevision(CharacterPredictiveFootPlanEndReason reason)
            {
                if (Revision.OwnsEvent)
                    Revision.Reset(reason);
                if (HasRevision)
                    m_Transition = default;
            }


            internal void ClearIntentObservation()
            {
                IntentLandingDisplacementError = 0f;
                IntentLandingDisplacementThreshold = 0f;
            }

            internal void ObserveIntentLandingDisplacement(float error, float threshold)
            {
                IntentLandingDisplacementError = float.IsFinite(error) ? error : 0f;
                IntentLandingDisplacementThreshold = float.IsFinite(threshold) ? threshold : 0f;
            }

            internal bool HasAttemptedPlanBuildRevision(
                CharacterFootPlanAttemptKind kind,
                ulong sourceIdentity,
                ulong motionGeneration,
                ulong authorityTick) =>
                kind != CharacterFootPlanAttemptKind.None &&
                sourceIdentity != 0 &&
                motionGeneration != 0 &&
                authorityTick != 0 &&
                LastPlanBuildAttemptKind == kind &&
                LastPlanBuildAttemptSourceIdentity == sourceIdentity &&
                LastPlanBuildAttemptMotionGeneration == motionGeneration &&
                LastPlanBuildAttemptAuthorityTick == authorityTick;

            internal void MarkPlanBuildRevisionAttempt(
                CharacterFootPlanAttemptKind kind,
                ulong sourceIdentity,
                in CommittedLocomotionPlanarMotionTimeline motionTimeline)
            {
                if (kind == CharacterFootPlanAttemptKind.None ||
                    sourceIdentity == 0 || !motionTimeline.IsValid)
                {
                    throw new ArgumentOutOfRangeException(nameof(sourceIdentity));
                }
                LastPlanBuildAttemptKind = kind;
                LastPlanBuildAttemptSourceIdentity = sourceIdentity;
                LastPlanBuildAttemptMotionGeneration = motionTimeline.Generation;
                LastPlanBuildAttemptAuthorityTick = motionTimeline.AuthorityTick.Value;
            }

            internal void RememberOutput(
                Vector3 animatedAnklePosition,
                Quaternion animatedAnkleRotation,
                Vector3 currentHip,
                Vector3 anklePosition,
                Quaternion ankleRotation,
                Vector3 sole,
                Vector3 groundPath,
                FootPlacementSurface groundSupport,
                ulong groundPlanSequence,
                Vector3 pathRoot,
                Vector3 pathRootStart,
                Vector3 pathHip)
            {
                if (!IsFinite(animatedAnklePosition) || !IsFinite(animatedAnkleRotation) ||
                    !IsFinite(currentHip) || !IsFinite(anklePosition) ||
                    !IsFinite(ankleRotation) || !IsFinite(sole))
                    return;
                bool hasGroundPath = groundSupport.IsValid && IsFinite(groundPath) &&
                                     groundPlanSequence != 0 && IsFinite(pathRoot) &&
                                     IsFinite(pathRootStart) && IsFinite(pathHip);
                FootPlacementSurface outputSupport = hasGroundPath
                    ? new FootPlacementSurface(
                        groundSupport.Collider,
                        groundPath,
                        groundSupport.Normal.normalized)
                    : default;
                hasGroundPath = outputSupport.IsValid;
                LastOutput = new CharacterFootCompletedOutput(
                    animatedAnklePosition,
                    animatedAnkleRotation,
                    currentHip,
                    anklePosition,
                    ankleRotation,
                    sole,
                    hasGroundPath,
                    groundPath,
                    in outputSupport,
                    groundPlanSequence,
                    pathRoot,
                    pathRootStart,
                    pathHip);
            }

            internal bool HasCompleteOutputForPlan(ulong planSequence) =>
                planSequence != 0 &&
                LastOutput.IsAvailable &&
                LastOutput.HasGroundPath &&
                LastOutput.PlanSequence == planSequence &&
                IsFinite(LastOutput.OriginalAnklePosition) &&
                IsFinite(LastOutput.OriginalAnkleRotation) &&
                IsFinite(LastOutput.OriginalHipPosition) &&
                IsFinite(LastOutput.FinalAnklePosition) &&
                IsFinite(LastOutput.FinalAnkleRotation) &&
                IsFinite(LastOutput.PathRootPosition) &&
                IsFinite(LastOutput.PathRootStartPosition) &&
                IsFinite(LastOutput.PathHipPosition);

            internal void PromoteUncommittedRevision(CharacterPredictiveFootPlanEndReason reason)
            {
                if (!Revision.HasExecutablePath)
                    throw new InvalidOperationException("Predictive Foot uncommitted revision is not executable.");
                Active.Reset(reason);
                CharacterPredictiveFootPlanExecution retired = Active;
                Active = Revision;
                Revision = retired;
                m_Transition = default;
                ClearIntentObservation();
            }

            internal void ResolveOutputContinuity(
                ulong renderFrame,
                float deltaSeconds,
                float blendSpeed,
                bool baselineOwnsFoot,
                bool allowOwnerChangeCapture,
                bool targetAvailable,
                ulong targetPlanSequence,
                Vector3 targetPosition,
                Quaternion targetRotation,
                out Vector3 resolvedPosition,
                out Quaternion resolvedRotation)
            {
                resolvedPosition = targetPosition;
                resolvedRotation = targetRotation;
                bool suppressCapture = m_SuppressNextOutputContinuityCapture;
                m_SuppressNextOutputContinuityCapture = false;
                if (baselineOwnsFoot || !targetAvailable || targetPlanSequence == 0 ||
                    !IsFinite(targetPosition) || !IsFinite(targetRotation))
                {
                    ClearOutputContinuity();
                    return;
                }
                if (!allowOwnerChangeCapture || suppressCapture)
                {
                    ClearOutputContinuity();
                    return;
                }
                bool outputOwnerChanged = LastOutput.PlanSequence != targetPlanSequence;
                if (outputOwnerChanged &&
                    m_OutputContinuityPlanSequence != targetPlanSequence &&
                    LastOutput.IsAvailable && IsFinite(LastOutput.FinalAnklePosition) &&
                    IsFinite(LastOutput.FinalAnkleRotation))
                {
                    m_OutputContinuityPlanSequence = targetPlanSequence;
                    m_OutputContinuityStartedFrame = renderFrame;
                    m_OutputContinuityWeight = 1f;
                    m_OutputContinuityPositionOffset = LastOutput.FinalAnklePosition - targetPosition;
                    m_OutputContinuityRotationOffset = (
                        LastOutput.FinalAnkleRotation * Quaternion.Inverse(targetRotation)).normalized;
                }
                else if (m_OutputContinuityWeight > 0f &&
                         renderFrame > m_OutputContinuityStartedFrame)
                {
                    m_OutputContinuityWeight = Mathf.MoveTowards(
                        m_OutputContinuityWeight,
                        0f,
                        blendSpeed * deltaSeconds);
                }
                if (m_OutputContinuityWeight <= 0.0001f)
                {
                    ClearOutputContinuity();
                    return;
                }
                float weight = m_OutputContinuityWeight * m_OutputContinuityWeight *
                               (3f - 2f * m_OutputContinuityWeight);
                resolvedPosition += m_OutputContinuityPositionOffset * weight;
                resolvedRotation = Quaternion.Slerp(
                    targetRotation,
                    (m_OutputContinuityRotationOffset * targetRotation).normalized,
                    weight).normalized;
            }

            void ClearOutputContinuity()
            {
                m_OutputContinuityPlanSequence = 0;
                m_OutputContinuityStartedFrame = 0;
                m_OutputContinuityWeight = 0f;
                m_OutputContinuityPositionOffset = Vector3.zero;
                m_OutputContinuityRotationOffset = Quaternion.identity;
            }

            internal void Reset(CharacterPredictiveFootPlanEndReason reason)
            {
                Active.Reset(reason);
                Revision.Reset(reason);
                m_Transition = default;
                ClearIntentObservation();
                LastOutput = default;
                LastPlanBuildAttemptKind = CharacterFootPlanAttemptKind.None;
                LastPlanBuildAttemptSourceIdentity = 0;
                LastPlanBuildAttemptMotionGeneration = 0;
                LastPlanBuildAttemptAuthorityTick = 0;
                ClearOutputContinuity();
                m_SuppressNextOutputContinuityCapture = false;
                PlanAttempt = default;
                PlanBuildDecision = default;
            }

            static bool IsFinite(Vector3 value) =>
                float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

            static bool IsFinite(Quaternion value) =>
                float.IsFinite(value.x) && float.IsFinite(value.y) &&
                float.IsFinite(value.z) && float.IsFinite(value.w);

            CharacterFootPlanTransition CaptureTransitionOrigin(
                CharacterFootPlanTransition transition,
                in CharacterFootPlanTransitionCapture capture)
            {
                if (!HasCompleteOutputForPlan(Active.Sequence))
                    throw new InvalidOperationException("Predictive Foot transition origin is unavailable.");
                return transition.WithContinuity(
                    LastOutput.FinalAnklePosition - capture.OriginalAnklePosition,
                    (LastOutput.FinalAnkleRotation *
                     Quaternion.Inverse(capture.OriginalAnkleRotation)).normalized,
                    LastOutput.GroundPathPosition,
                    LastOutput.GroundSupport.Rebuild(),
                    LastOutput.PathRootPosition - capture.OriginalHipPosition,
                    LastOutput.PathRootStartPosition - capture.OriginalHipPosition,
                    LastOutput.PathHipPosition - capture.OriginalHipPosition);
            }

            internal void ResolveTransitionOriginAnkle(
                Vector3 animatedAnklePosition,
                Quaternion animatedAnkleRotation,
                out Vector3 anklePosition,
                out Quaternion ankleRotation)
            {
                if (!HasTransitionOrigin || !IsFinite(animatedAnklePosition) ||
                    !IsFinite(animatedAnkleRotation))
                {
                    throw new InvalidOperationException("Predictive Foot transition ankle origin is unavailable.");
                }
                anklePosition = animatedAnklePosition + m_Transition.AnkleOffset;
                ankleRotation = (
                    m_Transition.AnkleRotationOffset *
                    animatedAnkleRotation).normalized;
            }

            internal void ResolveTransitionOriginBodyPath(
                Vector3 currentHip,
                out Vector3 pathRoot,
                out Vector3 pathRootStart,
                out Vector3 pathHip)
            {
                if (!HasTransitionOrigin || !IsFinite(currentHip))
                    throw new InvalidOperationException("Predictive Foot transition body origin is unavailable.");
                pathRoot = currentHip + m_Transition.PathRootOffset;
                pathRootStart = currentHip + m_Transition.PathRootStartOffset;
                pathHip = currentHip + m_Transition.PathHipOffset;
            }
        }

        readonly ActorId m_ActorId;
        readonly CharacterFootPlacementPoseRig m_Rig;
        readonly CharacterFootPlacementWorldQueryBackend m_World;
        readonly ICharacterFutureBodyTrajectorySource m_FutureBodyTrajectorySource;
        readonly int m_GroundLayerMask;
        readonly FixedString64Bytes m_RigId;
        readonly FixedString64Bytes m_RigRevision;
        CharacterPredictiveFootPlacementRuntimeSettings m_Settings;
        float m_TransitionBlendSpeed;
        CharacterPredictiveFootPlacementQuery m_Query;
        CharacterPredictiveFootPlacementDiagnostics m_Diagnostics;
        ulong m_NextPlanSequence = 1;
        float m_TrajectoryCurvatureDegreesPerSecond;
        bool m_TrajectoryCurvatureAvailable;
        CharacterPredictiveFootFrameSnapshot m_PendingDebugSnapshot;

        internal CharacterPredictiveFootPlacementPlanner(
            ActorId actorId,
            CharacterFootPlacementPoseRig rig,
            CharacterFootPlacementRuntimeSettings settings,
            CharacterFootPlacementWorldQueryBackend world,
            ICharacterFutureBodyTrajectorySource futureBodyTrajectorySource)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Predictive Foot Placement Actor identity is invalid.", nameof(actorId));
            m_ActorId = actorId;
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            m_Settings = settings?.PredictiveExtension ?? throw new ArgumentNullException(nameof(settings));
            m_TransitionBlendSpeed = settings.StanceStabilization.AnchorBlendSpeed;
            m_GroundLayerMask = settings.CurrentGrounding.GroundLayerMask;
            m_RigId = new FixedString64Bytes(rig.Rig.RigId);
            m_RigRevision = new FixedString64Bytes(rig.Rig.RigRevision);
            m_World = world ?? throw new ArgumentNullException(nameof(world));
            m_FutureBodyTrajectorySource = futureBodyTrajectorySource;
            m_Query = new CharacterPredictiveFootPlacementQuery(m_World, m_Settings);
        }

        internal CharacterPredictiveFootPlacementDiagnostics Diagnostics => m_Diagnostics;

        internal CharacterPredictiveFootFrameEvaluation EvaluateFrame(
            in CharacterFootPlacementFrameInput frame,
            in CharacterFootPlacementAnimatedPose pose,
            CharacterFootPlanExecutionState leftPlanState,
            CharacterFootPlanExecutionState rightPlanState,
            in CharacterFootLandingCommit leftLandingCommit,
            in CharacterFootPlanSupportFacts leftSupportFacts,
            in CharacterFootLandingCommit rightLandingCommit,
            in CharacterFootPlanSupportFacts rightSupportFacts)
        {
            if (frame.ActorId != m_ActorId || !frame.Body.IsValid ||
                frame.RenderFrame == 0 || frame.CompletionIdentity == 0)
            {
                throw new InvalidOperationException("Predictive Foot planning frame is invalid or duplicated.");
            }
            Vector3 rootWorldPosition = m_Rig.PoseRoot.position;
            Quaternion rootWorldRotation = m_Rig.PoseRoot.rotation;
            Vector3 presentedBodyPosition = frame.Body.VisiblePosition;
            AnimationFootFeatureSample leftFeature = frame.UpstreamPose.LeftFootFeatures;
            AnimationFootFeatureSample rightFeature = frame.UpstreamPose.RightFootFeatures;
            Vector3 committedBodyVelocity = frame.Body.TargetVelocity;
            m_TrajectoryCurvatureDegreesPerSecond = frame.TrajectoryCurvatureDegreesPerSecond;
            m_TrajectoryCurvatureAvailable = frame.TrajectoryCurvatureAvailable;
            float trajectoryCurvatureDegreesPerSecond = frame.TrajectoryCurvatureAvailable
                ? frame.TrajectoryCurvatureDegreesPerSecond
                : 0f;
            CommittedLocomotionPlanarMotionTimeline motionTimeline = frame.LocomotionMotionTimeline;
            PrepareFoot(
                CharacterFootSide.Left,
                leftPlanState,
                pose.Left,
                leftFeature,
                in leftLandingCommit,
                in leftSupportFacts,
                frame.RenderFrame,
                rootWorldPosition,
                rootWorldRotation,
                presentedBodyPosition,
                committedBodyVelocity,
                trajectoryCurvatureDegreesPerSecond,
                frame.TrajectoryCurvatureAvailable,
                in motionTimeline,
                frame.MovementPlaybackTime,
                m_Rig.LeftLegLength,
                frame.PresentationDeltaSeconds);
            PrepareFoot(
                CharacterFootSide.Right,
                rightPlanState,
                pose.Right,
                rightFeature,
                in rightLandingCommit,
                in rightSupportFacts,
                frame.RenderFrame,
                rootWorldPosition,
                rootWorldRotation,
                presentedBodyPosition,
                committedBodyVelocity,
                trajectoryCurvatureDegreesPerSecond,
                frame.TrajectoryCurvatureAvailable,
                in motionTimeline,
                frame.MovementPlaybackTime,
                m_Rig.RightLegLength,
                frame.PresentationDeltaSeconds);
            CharacterPredictiveFootStanceInput left = BuildStanceInput(
                leftPlanState,
                frame.UpstreamPose.LeftFootFeatures,
                pose.Left,
                out CharacterPredictiveFootGoalCandidates leftGoalCandidates);
            CharacterPredictiveFootStanceInput right = BuildStanceInput(
                rightPlanState,
                frame.UpstreamPose.RightFootFeatures,
                pose.Right,
                out CharacterPredictiveFootGoalCandidates rightGoalCandidates);
            return new CharacterPredictiveFootFrameEvaluation(
                frame.RenderFrame,
                frame.CompletionIdentity,
                in pose,
                in left,
                in right,
                in leftGoalCandidates,
                in rightGoalCandidates);
        }

        CharacterPredictiveFootStanceInput BuildStanceInput(
            CharacterFootPlanExecutionState runtime,
            AnimationFootFeatureSample feature,
            CharacterFootPlacementAnimatedFootPose pose,
            out CharacterPredictiveFootGoalCandidates goalCandidates)
        {
            goalCandidates = default;
            CharacterPredictiveFootPlanExecution plan = runtime.Active;
            AnimationPredictedFootStepSample step = feature.PredictedStep;
            if (!step.IsAuthoritative && !plan.HasExecutablePath)
                return default;
            bool activePlanMatches = step.IsAuthoritative &&
                                     plan.MatchesAuthoritativeEvent(in step);
            CharacterPredictiveFootPlanExecution revision = runtime.Revision;
            bool revisionMatches = runtime.HasRevision &&
                                   step.IsAuthoritative &&
                                   revision.MatchesAuthoritativeEvent(in step);
            bool outgoingIntentRevisionContributes = runtime.HasIntentRevision &&
                                                       !activePlanMatches &&
                                                       plan.OwnsEvent &&
                                                       revision.OwnsEvent;
            bool revisionContributes = runtime.HasIntentRevision &&
                                        revision.State == CharacterPredictiveFootPlanState.Executing &&
                                        (revisionMatches || outgoingIntentRevisionContributes);
            AnimationFootConstraintMode constraintMode;
            AnimationFootSupportPhase supportPhase;
            AnimationBodyRotationPivotMode bodyPivotMode;
            float constraintWeight;
            float supportWeight;
            bool currentEventOwnsState = step.IsAuthoritative &&
                                          (activePlanMatches ||
                                           runtime.IsFadingOut ||
                                           !plan.HasExecutablePath);
            if (currentEventOwnsState)
            {
                float phase = step.ActionStepClock.Phase;
                constraintMode = step.EvaluateConstraintMode(phase);
                supportPhase = step.EvaluateSupportPhase(phase);
                bodyPivotMode = step.EvaluateBodyRotationPivotMode(phase);
                RequireAuthoritativeConstraint(
                    in step,
                    constraintMode,
                    supportPhase,
                    bodyPivotMode);
                constraintWeight = step.CurrentConstraintWeight;
                supportWeight = step.CurrentSupportWeight;
            }
            else if (plan.HasExecutablePath)
            {
                ResolveCurrentActionState(
                    plan,
                    out constraintMode,
                    out supportPhase,
                    out _,
                    out bodyPivotMode);
                constraintWeight = plan.RootTrajectory.EvaluateConstraintWeight(plan.ActionStepPhase);
                supportWeight = plan.RootTrajectory.EvaluateSupportWeight(plan.ActionStepPhase);
            }
            else
            {
                constraintMode = AnimationFootConstraintMode.Unlocked;
                supportPhase = AnimationFootSupportPhase.Unsupported;
                bodyPivotMode = AnimationBodyRotationPivotMode.Pelvis;
                constraintWeight = 0f;
                supportWeight = 0f;
            }
            Vector3 pathPosition = default;
            Vector3 pathRoot = default;
            Vector3 pathRootStart = default;
            Vector3 pathHip = default;
            FootPlacementSurface contactSurface = default;
            Vector3 contactAnklePosition = default;
            Quaternion contactAnkleRotation = default;
            ulong contactPlanSequence = 0;
            ulong contactLandingEventIdentity = 0;
            Vector3 targetAnklePosition = pose.AnklePosition;
            bool hasContactTarget = false;
            if (plan.HasExecutablePath && !runtime.IsFadingOut)
            {
                plan.EvaluateGroundPath(
                    plan.GroundPathProgress,
                    out pathPosition,
                    out _);
                plan.EvaluateBodyPath(
                    plan.ActionStepPhase,
                    out pathRoot,
                    out pathHip);
                plan.EvaluateBodyPath(
                    plan.RootTrajectory.PathStartPhase,
                    out pathRootStart,
                    out _);
            }
            bool hasEventSuccessorHandoff = runtime.HasEventSuccessorHandoff &&
                                            runtime.HasTransitionOrigin;
            bool hasTransitionOrigin = runtime.HasTransitionOrigin &&
                                       (runtime.HasIntentRevision ||
                                        hasEventSuccessorHandoff ||
                                        runtime.IsFadingOut);
            Vector3 transitionOriginAnklePosition = default;
            if (hasTransitionOrigin)
            {
                runtime.ResolveTransitionOriginAnkle(
                    pose.AnklePosition,
                    pose.AnkleRotation,
                    out transitionOriginAnklePosition,
                    out _);
                runtime.ResolveTransitionOriginBodyPath(
                    pose.HipPosition,
                    out pathRoot,
                    out pathRootStart,
                    out pathHip);
                pathPosition = runtime.TransitionOriginGroundPath;
            }
            if (revisionContributes)
            {
                revision.EvaluateGroundPath(
                    revision.GroundPathProgress,
                    out Vector3 revisionPathPosition,
                    out _);
                revision.EvaluateBodyPath(
                    revision.ActionStepPhase,
                    out Vector3 revisionPathRoot,
                    out Vector3 revisionPathHip);
                revision.EvaluateBodyPath(
                    revision.RootTrajectory.PathStartPhase,
                    out Vector3 revisionPathRootStart,
                    out _);
                float blend = runtime.SmoothedTransitionBlendWeight;
                pathPosition = Vector3.Lerp(pathPosition, revisionPathPosition, blend);
                pathRoot = Vector3.Lerp(pathRoot, revisionPathRoot, blend);
                pathRootStart = Vector3.Lerp(pathRootStart, revisionPathRootStart, blend);
                pathHip = Vector3.Lerp(pathHip, revisionPathHip, blend);
            }
            CharacterPredictiveFootGoalCandidate activeCandidate = EvaluateGeometryCandidate(
                plan,
                pose,
                m_Rig.PoseRoot.up.normalized);
            CharacterPredictiveFootTarget activeTarget = activeCandidate.Target;
            bool activeTargetAvailable = !runtime.IsFadingOut && activeCandidate.Available;
            if (hasEventSuccessorHandoff && activeTargetAvailable)
            {
                float blend = plan.EvaluatePredictiveOutputWeight() *
                              runtime.EventSuccessorHandoffBlendWeight;
                pathPosition = Vector3.Lerp(
                    runtime.TransitionOriginGroundPath,
                    activeTarget.PathPosition,
                    blend);
                pathRoot = Vector3.Lerp(pathRoot, activeTarget.PathRoot, blend);
                pathHip = Vector3.Lerp(pathHip, activeTarget.PathHip, blend);
            }
            if (activeTargetAvailable)
                targetAnklePosition = activeTarget.AnklePosition;
            if (hasTransitionOrigin)
            {
                targetAnklePosition = hasEventSuccessorHandoff && activeTargetAvailable
                    ? Vector3.Lerp(
                        transitionOriginAnklePosition,
                        activeTarget.AnklePosition,
                        plan.EvaluatePredictiveOutputWeight() *
                        runtime.EventSuccessorHandoffBlendWeight)
                    : transitionOriginAnklePosition;
            }
            CharacterPredictiveFootGoalCandidate revisionCandidate = revisionContributes
                ? EvaluateGeometryCandidate(
                    revision,
                    pose,
                    m_Rig.PoseRoot.up.normalized)
                : default;
            if (revisionContributes && revisionCandidate.Available)
            {
                CharacterPredictiveFootTarget revisionTarget = revisionCandidate.Target;
                targetAnklePosition = activeTargetAvailable || hasTransitionOrigin
                    ? Vector3.Lerp(
                        hasTransitionOrigin
                            ? transitionOriginAnklePosition
                            : activeTarget.AnklePosition,
                        revisionTarget.AnklePosition,
                        runtime.SmoothedTransitionBlendWeight)
                    : revisionTarget.AnklePosition;
            }
            CharacterPredictiveFootPlanExecution contactPlan =
                runtime.HasIntentRevision && revisionMatches
                    ? revision
                    : activePlanMatches
                        ? plan
                        : null;
            if (contactPlan != null &&
                contactPlan.State == CharacterPredictiveFootPlanState.Executing &&
                IsMotionWithinCommitTolerance(contactPlan) &&
                supportPhase == AnimationFootSupportPhase.ApproachingContact &&
                TryResolveGeometryCandidate(
                    contactPlan,
                    in activeCandidate,
                    in revisionCandidate,
                    out CharacterPredictiveFootTarget target) &&
                target.Support.IsValid)
            {
                hasContactTarget = true;
                contactSurface = target.Support;
                contactAnklePosition = target.AnklePosition;
                contactAnkleRotation = target.AnkleRotation;
                contactPlanSequence = contactPlan.Sequence;
                contactLandingEventIdentity = contactPlan.LandingEventIdentity;
                pathPosition = target.PathPosition;
                pathRoot = target.PathRoot;
                pathHip = target.PathHip;
            }
            CharacterPredictiveFootPlanExecution timingPlan = revisionContributes
                ? revision
                : plan;
            float activePredictiveOutputWeight = plan.State == CharacterPredictiveFootPlanState.Executing
                ? plan.EvaluatePredictiveOutputWeight() *
                  runtime.PredictiveRetentionWeight *
                  runtime.EventSuccessorHandoffBlendWeight
                : 0f;
            float revisionPredictiveOutputWeight = revisionContributes
                ? revision.EvaluatePredictiveOutputWeight()
                : 0f;
            float predictiveOutputWeight = revisionContributes
                ? Mathf.Lerp(
                    activePredictiveOutputWeight,
                    revisionPredictiveOutputWeight,
                    runtime.SmoothedTransitionBlendWeight)
                : activePredictiveOutputWeight;
            float remainingSeconds = Mathf.Max(
                0f,
                currentEventOwnsState
                    ? step.ActionStepClock.TimeToLandingSeconds
                    : (1f - timingPlan.ActionStepPhase) * timingPlan.ActionStepDurationSeconds);
            goalCandidates = new CharacterPredictiveFootGoalCandidates(
                in activeCandidate,
                in revisionCandidate);
            return new CharacterPredictiveFootStanceInput(
                true,
                plan.HasExecutablePath,
                plan.State == CharacterPredictiveFootPlanState.Executing,
                plan.Sequence,
                currentEventOwnsState
                    ? step.LandingEventIdentity
                    : plan.LandingEventIdentity,
                hasContactTarget,
                contactPlanSequence,
                contactLandingEventIdentity,
                constraintMode,
                supportPhase,
                bodyPivotMode,
                constraintWeight,
                supportWeight,
                feature.PlantConfidence,
                plan.ActionProgress,
                remainingSeconds,
                contactSurface,
                contactAnklePosition,
                contactAnkleRotation,
                pathPosition,
                pathRoot,
                pathRootStart,
                pathHip,
                pose.HipPosition,
                targetAnklePosition,
                predictiveOutputWeight,
                currentEventOwnsState ? step.BiomechanicalSample.SupportLegLength : 0f,
                currentEventOwnsState ? step.BiomechanicalSample.SupportLegCompressionReserve : 0f,
                currentEventOwnsState ? step.BiomechanicalSample.SupportKneeBendPlane : Vector3.zero,
                currentEventOwnsState ? step.BiomechanicalSample.SupportFootPivotPosition : Vector3.zero,
                currentEventOwnsState ? step.BiomechanicalSample.SupportFootPivotWeight : 0f);
        }

        static CharacterPredictiveFootGoalCandidate EvaluateGeometryCandidate(
            CharacterPredictiveFootPlanExecution plan,
            CharacterFootPlacementAnimatedFootPose pose,
            Vector3 componentUp)
        {
            if (plan == null)
                return default;
            bool available = TryEvaluateFootTarget(
                plan,
                plan.ActionStepPhase,
                pose,
                componentUp,
                pose.HipPosition,
                0f,
                out CharacterPredictiveFootTarget target,
                out FootPredictionRejectReason rejectReason);
            return new CharacterPredictiveFootGoalCandidate(
                plan.Sequence,
                true,
                available,
                rejectReason,
                in target);
        }

        static bool TryResolveGeometryCandidate(
            CharacterPredictiveFootPlanExecution plan,
            in CharacterPredictiveFootGoalCandidate active,
            in CharacterPredictiveFootGoalCandidate revision,
            out CharacterPredictiveFootTarget target)
        {
            if (active.Available && active.PlanSequence == plan.Sequence)
            {
                target = active.Target;
                return true;
            }
            if (revision.Available && revision.PlanSequence == plan.Sequence)
            {
                target = revision.Target;
                return true;
            }
            target = default;
            return false;
        }

        internal CharacterFootPlacementFootGoalResolution ResolveFootGoals(
            in CharacterFootPlacementFrameInput frame,
            in CharacterPredictiveFootFrameEvaluation evaluation,
            CharacterFootPlanExecutionState leftPlanState,
            CharacterFootPlanExecutionState rightPlanState,
            in CharacterFullBodyIkGoalSetHeader ownerHeader,
            CharacterFullBodyIkGoal currentPelvis,
            CharacterFullBodyIkGoal stanceLeft,
            CharacterFullBodyIkGoal stanceRight,
            in CharacterFootGroundingDiagnostics stanceDiagnostics)
        {
            RequireValidInput(
                in frame,
                in ownerHeader,
                currentPelvis,
                stanceLeft,
                stanceRight,
                in stanceDiagnostics);
            if (!evaluation.Matches(in frame))
                throw new InvalidOperationException("Predictive Foot frame evaluation identity is invalid.");
            CharacterFootPlacementAnimatedPose pose = evaluation.OriginalPose;
            CharacterFootPlacementPoseInput upstreamPose = frame.UpstreamPose;
            AnimationPredictedFootStepSample leftStep =
                upstreamPose.LeftFootFeatures.PredictedStep;
            AnimationPredictedFootStepSample rightStep =
                upstreamPose.RightFootFeatures.PredictedStep;
            CharacterFootPlacementAnimatedFootPose leftPose = pose.Left;
            CharacterFootPlacementAnimatedFootPose rightPose = pose.Right;
            CharacterFootGroundingFootDiagnostics leftStanceDiagnostics = stanceDiagnostics.Left;
            CharacterFootGroundingFootDiagnostics rightStanceDiagnostics = stanceDiagnostics.Right;
            var leftInput = new CharacterFootPlacementFootGoalInput(
                CharacterFootSide.Left,
                in leftPose,
                upstreamPose.LeftFootFeatures,
                ResolveCurrentEventFootPoseWeight(
                    in upstreamPose,
                    CharacterFootSide.Left,
                in leftStep),
                stanceLeft,
                in leftStanceDiagnostics);
            var rightInput = new CharacterFootPlacementFootGoalInput(
                CharacterFootSide.Right,
                in rightPose,
                upstreamPose.RightFootFeatures,
                ResolveCurrentEventFootPoseWeight(
                    in upstreamPose,
                    CharacterFootSide.Right,
                in rightStep),
                stanceRight,
                in rightStanceDiagnostics);
            CharacterPredictiveFootGoalCandidates leftGoalCandidates =
                evaluation.LeftGoalCandidates;
            CharacterPredictiveFootGoalCandidates rightGoalCandidates =
                evaluation.RightGoalCandidates;
            CharacterFullBodyIkGoal left = ModifyFoot(
                leftPlanState,
                in leftInput,
                in leftGoalCandidates,
                frame.RenderFrame,
                frame.PresentationDeltaSeconds,
                m_Rig.LeftLegLength,
                ResolveAppliedHip(pose.Left.HipPosition, currentPelvis),
                out CharacterPredictiveFootPlacementFootDiagnostics leftDiagnostics,
                out CharacterPredictiveFootLegFrameSnapshot leftDebugSnapshot);
            CharacterFullBodyIkGoal right = ModifyFoot(
                rightPlanState,
                in rightInput,
                in rightGoalCandidates,
                frame.RenderFrame,
                frame.PresentationDeltaSeconds,
                m_Rig.RightLegLength,
                ResolveAppliedHip(pose.Right.HipPosition, currentPelvis),
                out CharacterPredictiveFootPlacementFootDiagnostics rightDiagnostics,
                out CharacterPredictiveFootLegFrameSnapshot rightDebugSnapshot);
            m_Diagnostics = new CharacterPredictiveFootPlacementDiagnostics(
                frame.RenderFrame,
                frame.CompletionIdentity,
                in ownerHeader,
                in leftDiagnostics,
                in rightDiagnostics);
            m_PendingDebugSnapshot = new CharacterPredictiveFootFrameSnapshot(
                m_ActorId,
                frame.RenderFrame,
                frame.CompletionIdentity,
                in leftDebugSnapshot,
                in rightDebugSnapshot);
            return new CharacterFootPlacementFootGoalResolution(left, right);
        }

        internal void ApplyTuning(
            CharacterPredictiveFootPlacementRuntimeSettings settings,
            float transitionBlendSpeed)
        {
            settings.RequireValid();
            if (!float.IsFinite(transitionBlendSpeed) || transitionBlendSpeed <= 0f)
                throw new ArgumentOutOfRangeException(nameof(transitionBlendSpeed));
            m_Settings = settings;
            m_TransitionBlendSpeed = transitionBlendSpeed;
            m_Query = new CharacterPredictiveFootPlacementQuery(m_World, settings);
        }

        internal void Reset(bool clearCommittedDebug = true)
        {
            m_NextPlanSequence = 1;
            m_TrajectoryCurvatureDegreesPerSecond = 0f;
            m_TrajectoryCurvatureAvailable = false;
            m_Diagnostics = default;
            m_PendingDebugSnapshot = null;
            if (clearCommittedDebug)
                CharacterPredictiveFootPlacementDebugSnapshotRegistry.Remove(m_ActorId);
        }

        internal StateSnapshot CaptureState() =>
            new StateSnapshot(
                m_NextPlanSequence,
                m_TrajectoryCurvatureDegreesPerSecond,
                m_TrajectoryCurvatureAvailable,
                in m_Diagnostics);

        internal void RestoreState(in StateSnapshot snapshot)
        {
            m_NextPlanSequence = snapshot.NextPlanSequence;
            m_TrajectoryCurvatureDegreesPerSecond = snapshot.TrajectoryCurvatureDegreesPerSecond;
            m_TrajectoryCurvatureAvailable = snapshot.TrajectoryCurvatureAvailable;
            m_Diagnostics = snapshot.Diagnostics;
            m_PendingDebugSnapshot = null;
        }

        internal void SealFrame()
        {
            if (m_PendingDebugSnapshot != null)
                CharacterPredictiveFootPlacementDebugSnapshotRegistry.Publish(m_PendingDebugSnapshot);
            m_PendingDebugSnapshot = null;
        }

        CharacterFullBodyIkGoal ModifyFoot(
            CharacterFootPlanExecutionState runtime,
            in CharacterFootPlacementFootGoalInput input,
            in CharacterPredictiveFootGoalCandidates goalCandidates,
            ulong renderFrame,
            float presentationDeltaSeconds,
            float legLength,
            Vector3 appliedHip,
            out CharacterPredictiveFootPlacementFootDiagnostics diagnostics,
            out CharacterPredictiveFootLegFrameSnapshot debugSnapshot)
        {
            CharacterFootSide side = input.Side;
            CharacterFootPlacementAnimatedFootPose pose = input.OriginalPose;
            AnimationFootFeatureSample feature = input.Feature;
            float currentEventFootPoseWeight = input.CurrentEventFootPoseWeight;
            CharacterFullBodyIkGoal stanceGoal = input.StanceGoal;
            CharacterFootGroundingFootDiagnostics grounding = input.StanceDiagnostics;
            var transitionCapture = new CharacterFootPlanTransitionCapture(
                pose.AnklePosition,
                pose.AnkleRotation,
                pose.HipPosition);
            CharacterPredictiveFootPlanExecution plan = runtime.Active;
            AnimationPredictedFootStepSample step = feature.PredictedStep;
            bool landingEventIdentityValid = step.HasConsistentLandingEventIdentity(side);
            Transform component = m_Rig.PoseRoot;
            Vector3 up = component.up.normalized;
            Vector3 stanceWorldPosition = component.TransformPoint(stanceGoal.ComponentPosition);
            Quaternion stanceWorldRotation = (component.rotation * stanceGoal.ComponentRotation).normalized;
            CharacterFootPlacementSoleContactPose baselineContacts = pose.ResolveSoleContacts(
                stanceWorldPosition,
                stanceWorldRotation);
            CharacterFootPlacementSoleContactPose nativeContacts = pose.ResolveSoleContacts(
                pose.AnklePosition,
                pose.AnkleRotation);
            Vector3 currentSole = (nativeContacts.HeelPosition + nativeContacts.ToePosition) * 0.5f;
            CharacterFullBodyIkGoal result = stanceGoal;
            bool rewritten = false;
            float appliedLift = 0f;
            float requiredLift = 0f;
            float predictionReachRatio = 0f;
            Vector3 currentPathPosition = default;
            Vector3 currentPathRoot = default;
            Vector3 currentPathRootStart = default;
            Vector3 currentPathHip = default;
            FootPlacementSurface currentPathSupport = default;
            float preHeelDistance = 0f;
            float preToeDistance = 0f;
            float postHeelDistance = 0f;
            float postToeDistance = 0f;
            bool clearanceEvaluated = false;
            bool predictiveOwnsSoleClearance = false;
            float authoredAnimationClearance = 0f;
            float animationClearanceContinuityOffset = 0f;
            float animationClearanceContinuityContribution = 0f;
            float reachClearance = 0f;
            float compositeAnimationClearance = 0f;
            bool allowsStanceHandoff = AllowsStanceHandoff(plan);
            CharacterPredictiveFootPlanExecution revisionPlan = runtime.Revision;
            bool hasIntentRevision = runtime.HasIntentRevision;
            bool hasEventSuccessorHandoff = runtime.HasEventSuccessorHandoff &&
                                            runtime.HasTransitionOrigin;
            bool hasTransitionOrigin = runtime.HasTransitionOrigin &&
                                       (hasIntentRevision ||
                                        hasEventSuccessorHandoff ||
                                        runtime.IsFadingOut);
            float predictiveOutputWeight = plan.State == CharacterPredictiveFootPlanState.Executing
                ? plan.EvaluatePredictiveOutputWeight()
                : 0f;
            float revisionPredictiveOutputWeight = hasIntentRevision &&
                                                   revisionPlan.State == CharacterPredictiveFootPlanState.Executing
                ? revisionPlan.EvaluatePredictiveOutputWeight()
                : 0f;
            float revisionTransitionBlend = hasIntentRevision
                ? runtime.SmoothedTransitionBlendWeight
                : 0f;
            float stanceTransitionBlend = plan.State == CharacterPredictiveFootPlanState.Executing
                ? Mathf.Clamp01(grounding.AnchorBlendWeight)
                : 0f;
            float activePlanPredictionBlend = predictiveOutputWeight *
                                              (1f - stanceTransitionBlend) *
                                              runtime.PredictiveRetentionWeight;
            float revisionPlanPredictionBlend = revisionPredictiveOutputWeight *
                                                (1f - stanceTransitionBlend);
            float planPredictionBlend = Mathf.Lerp(
                activePlanPredictionBlend,
                revisionPlanPredictionBlend,
                revisionTransitionBlend);
            float authoritativePredictionBlend = Mathf.Lerp(
                activePlanPredictionBlend,
                revisionPlanPredictionBlend,
                revisionTransitionBlend);
            AnimationFootConstraintMode currentConstraintMode = step.IsAuthoritative
                ? step.EvaluateConstraintMode(step.ActionStepClock.Phase)
                : AnimationFootConstraintMode.Unlocked;
            bool actionConstraintOwnsFoot = step.IsAuthoritative &&
                                           currentConstraintMode != AnimationFootConstraintMode.Unlocked &&
                                           authoritativePredictionBlend <= 0.000001f;
            bool physicalStanceOwnsFoot =
                grounding.ContactState != CharacterFootContactState.Swing &&
                allowsStanceHandoff &&
                grounding.AnchorBlendWeight >= 0.999999f;
            bool stanceOwnsFoot = physicalStanceOwnsFoot ||
                                  actionConstraintOwnsFoot && !hasEventSuccessorHandoff;
            bool currentSupportOwnsIdle = !step.IsAuthoritative &&
                                          plan.State == CharacterPredictiveFootPlanState.Inactive;
            bool baselineOwnsFoot = physicalStanceOwnsFoot || currentSupportOwnsIdle;
            if (!stanceOwnsFoot && !currentSupportOwnsIdle)
            {
                result = new CharacterFullBodyIkGoal(
                    stanceGoal.Slot,
                    component.InverseTransformPoint(pose.AnklePosition),
                    (Quaternion.Inverse(component.rotation) * pose.AnkleRotation).normalized,
                    stanceGoal.PositionWeight,
                    stanceGoal.RotationWeight,
                    stanceGoal.Application,
                    stanceGoal.SourceKind,
                    stanceGoal.DiagnosticMetadataIndex);
            }
            CharacterFootPlacementGoalOwner goalOwner = stanceOwnsFoot || currentSupportOwnsIdle
                ? CharacterFootPlacementGoalOwner.Stance
                : CharacterFootPlacementGoalOwner.OriginalAnimation;
            Vector3 preContinuityGoalWorldPosition = component.TransformPoint(result.ComponentPosition);
            Vector3 transitionOriginAnklePosition = default;
            Quaternion transitionOriginAnkleRotation = default;
            Vector3 transitionOriginPathRoot = default;
            Vector3 transitionOriginPathRootStart = default;
            Vector3 transitionOriginPathHip = default;
            CharacterPredictiveFootTarget targetData = default;
            FootPredictionRejectReason activeEvaluationRejectReason = FootPredictionRejectReason.None;
            CharacterPredictiveFootTarget activeGeometryTarget = goalCandidates.Active.Target;
            bool activeGeometryGoalAvailable = goalCandidates.Active.Available &&
                                               goalCandidates.Active.PlanSequence == plan.Sequence;
            bool activeReachGoalAvailable = activeGeometryGoalAvailable &&
                                            TryResolveFootTargetReach(
                                                pose,
                                                appliedHip,
                                                up,
                                                legLength * m_Settings.MaximumPredictionReachRatio,
                                                in activeGeometryTarget,
                                                out targetData,
                                                out activeEvaluationRejectReason);
            bool activeTargetAvailable = !runtime.IsFadingOut && activeReachGoalAvailable;
            if (!activeGeometryGoalAvailable && goalCandidates.Active.Evaluated)
                activeEvaluationRejectReason = goalCandidates.Active.RejectReason;
            if (!runtime.IsFadingOut &&
                plan.State == CharacterPredictiveFootPlanState.Executing &&
                !activeTargetAvailable &&
                !stanceOwnsFoot &&
                runtime.HasCompleteOutputForPlan(plan.Sequence))
            {
                runtime.BeginFadeOut(
                    activeEvaluationRejectReason == FootPredictionRejectReason.ReachExceeded
                        ? CharacterPredictiveFootPlanEndReason.TargetReachExceeded
                        : CharacterPredictiveFootPlanEndReason.TargetEvaluationInvalid,
                    renderFrame,
                    in transitionCapture);
                hasIntentRevision = false;
                hasTransitionOrigin = runtime.HasTransitionOrigin && runtime.IsFadingOut;
                revisionTransitionBlend = 0f;
                revisionPlanPredictionBlend = 0f;
                planPredictionBlend = activePlanPredictionBlend;
                authoritativePredictionBlend = activePlanPredictionBlend;
            }
            bool targetAvailable = activeTargetAvailable || hasTransitionOrigin;
            CharacterPredictiveFootTarget revisionTargetData = default;
            FootPredictionRejectReason revisionEvaluationRejectReason = FootPredictionRejectReason.None;
            CharacterPredictiveFootTarget revisionGeometryTarget = goalCandidates.Revision.Target;
            bool revisionGeometryGoalAvailable = goalCandidates.Revision.Available &&
                                                 goalCandidates.Revision.PlanSequence == revisionPlan.Sequence;
            bool revisionReachGoalAvailable = revisionGeometryGoalAvailable &&
                                              TryResolveFootTargetReach(
                                                  pose,
                                                  appliedHip,
                                                  up,
                                                  legLength * m_Settings.MaximumPredictionReachRatio,
                                                  in revisionGeometryTarget,
                                                  out revisionTargetData,
                                                  out revisionEvaluationRejectReason);
            bool revisionTargetAvailable = hasIntentRevision && revisionReachGoalAvailable;
            if (hasIntentRevision && !revisionGeometryGoalAvailable &&
                goalCandidates.Revision.Evaluated)
            {
                revisionEvaluationRejectReason = goalCandidates.Revision.RejectReason;
            }
            if (hasIntentRevision &&
                revisionPlan.State == CharacterPredictiveFootPlanState.Executing &&
                !revisionTargetAvailable)
            {
                runtime.CancelRevision(
                    revisionEvaluationRejectReason == FootPredictionRejectReason.ReachExceeded
                        ? CharacterPredictiveFootPlanEndReason.TargetReachExceeded
                        : CharacterPredictiveFootPlanEndReason.TargetEvaluationInvalid);
                hasIntentRevision = false;
                hasTransitionOrigin = false;
                revisionTransitionBlend = 0f;
                revisionPlanPredictionBlend = 0f;
                planPredictionBlend = activePlanPredictionBlend;
                authoritativePredictionBlend = activePlanPredictionBlend;
                targetAvailable = activeTargetAvailable;
            }
            if (targetAvailable)
            {
                float revisionBlend = revisionTargetAvailable
                    ? revisionTransitionBlend
                    : 0f;
                if (hasTransitionOrigin)
                {
                    runtime.ResolveTransitionOriginAnkle(
                        pose.AnklePosition,
                        pose.AnkleRotation,
                        out transitionOriginAnklePosition,
                        out transitionOriginAnkleRotation);
                    runtime.ResolveTransitionOriginBodyPath(
                        pose.HipPosition,
                        out transitionOriginPathRoot,
                        out transitionOriginPathRootStart,
                        out transitionOriginPathHip);
                }
                Vector3 outgoingAnklePosition = hasTransitionOrigin
                    ? transitionOriginAnklePosition
                    : targetData.AnklePosition;
                Quaternion outgoingAnkleRotation = hasTransitionOrigin
                    ? transitionOriginAnkleRotation
                    : targetData.AnkleRotation;
                Vector3 outgoingPathPosition = hasTransitionOrigin
                    ? runtime.TransitionOriginGroundPath
                    : targetData.PathPosition;
                Vector3 outgoingPathRoot = hasTransitionOrigin
                    ? transitionOriginPathRoot
                    : targetData.PathRoot;
                Vector3 outgoingPathHip = hasTransitionOrigin
                    ? transitionOriginPathHip
                    : targetData.PathHip;
                FootPlacementSurface outgoingPathSupport = hasTransitionOrigin
                    ? runtime.TransitionOriginGroundSupport
                    : targetData.Support;
                if (hasTransitionOrigin)
                    currentPathRootStart = transitionOriginPathRootStart;
                else
                    plan.EvaluateBodyPath(
                        plan.RootTrajectory.PathStartPhase,
                        out currentPathRootStart,
                        out _);
                float successorBlend = hasEventSuccessorHandoff && activeTargetAvailable
                    ? activePlanPredictionBlend * runtime.EventSuccessorHandoffBlendWeight
                    : 0f;
                Vector3 predictiveAnklePosition = revisionTargetAvailable
                    ? Vector3.Lerp(outgoingAnklePosition, revisionTargetData.AnklePosition, revisionBlend)
                    : hasEventSuccessorHandoff && activeTargetAvailable
                        ? Vector3.Lerp(
                            outgoingAnklePosition,
                            targetData.AnklePosition,
                            successorBlend)
                        : outgoingAnklePosition;
                clearanceEvaluated = true;
                currentPathPosition = revisionTargetAvailable
                    ? Vector3.Lerp(outgoingPathPosition, revisionTargetData.PathPosition, revisionBlend)
                    : hasEventSuccessorHandoff && activeTargetAvailable
                        ? Vector3.Lerp(
                            outgoingPathPosition,
                            targetData.PathPosition,
                            successorBlend)
                        : outgoingPathPosition;
                currentPathRoot = revisionTargetAvailable
                    ? Vector3.Lerp(outgoingPathRoot, revisionTargetData.PathRoot, revisionBlend)
                    : hasEventSuccessorHandoff && activeTargetAvailable
                        ? Vector3.Lerp(
                            outgoingPathRoot,
                            targetData.PathRoot,
                            successorBlend)
                        : outgoingPathRoot;
                currentPathHip = revisionTargetAvailable
                    ? Vector3.Lerp(outgoingPathHip, revisionTargetData.PathHip, revisionBlend)
                    : hasEventSuccessorHandoff && activeTargetAvailable
                        ? Vector3.Lerp(
                            outgoingPathHip,
                            targetData.PathHip,
                            successorBlend)
                        : outgoingPathHip;
                if (revisionTargetAvailable)
                {
                    revisionPlan.EvaluateBodyPath(
                        revisionPlan.RootTrajectory.PathStartPhase,
                        out Vector3 revisionPathRootStart,
                        out _);
                    currentPathRootStart = Vector3.Lerp(
                        currentPathRootStart,
                        revisionPathRootStart,
                        revisionBlend);
                }
                currentPathSupport = outgoingPathSupport;
                if (hasEventSuccessorHandoff && activeTargetAvailable &&
                    successorBlend >= 0.5f)
                {
                    currentPathSupport = targetData.Support;
                }
                authoredAnimationClearance = revisionTargetAvailable
                    ? Mathf.Lerp(targetData.AuthoredAnimationClearance, revisionTargetData.AuthoredAnimationClearance, revisionBlend)
                    : targetData.AuthoredAnimationClearance;
                animationClearanceContinuityOffset = revisionTargetAvailable
                    ? Mathf.Lerp(targetData.AnimationClearanceContinuityOffset, revisionTargetData.AnimationClearanceContinuityOffset, revisionBlend)
                    : targetData.AnimationClearanceContinuityOffset;
                animationClearanceContinuityContribution = revisionTargetAvailable
                    ? Mathf.Lerp(targetData.AnimationClearanceContinuityContribution, revisionTargetData.AnimationClearanceContinuityContribution, revisionBlend)
                    : targetData.AnimationClearanceContinuityContribution;
                reachClearance = revisionTargetAvailable
                    ? Mathf.Lerp(targetData.ReachClearance, revisionTargetData.ReachClearance, revisionBlend)
                    : targetData.ReachClearance;
                compositeAnimationClearance = revisionTargetAvailable
                    ? Mathf.Lerp(targetData.CompositeAnimationClearance, revisionTargetData.CompositeAnimationClearance, revisionBlend)
                    : targetData.CompositeAnimationClearance;
                preHeelDistance = Vector3.Dot(
                    baselineContacts.HeelPosition - currentPathPosition,
                    up);
                preToeDistance = Vector3.Dot(
                    baselineContacts.ToePosition - currentPathPosition,
                    up);
                requiredLift = Vector3.Dot(predictiveAnklePosition - pose.AnklePosition, up);
                Vector3 activeResolvedAnklePosition = hasEventSuccessorHandoff
                    ? activeTargetAvailable
                        ? Vector3.Lerp(
                            outgoingAnklePosition,
                            targetData.AnklePosition,
                            successorBlend)
                        : outgoingAnklePosition
                    : hasTransitionOrigin
                    ? runtime.IsFadingOut
                        ? Vector3.Lerp(
                            pose.AnklePosition,
                            outgoingAnklePosition,
                            activePlanPredictionBlend)
                        : outgoingAnklePosition
                    : Vector3.Lerp(
                        pose.AnklePosition,
                        targetData.AnklePosition,
                        activePlanPredictionBlend);
                Quaternion activeResolvedAnkleRotation = hasEventSuccessorHandoff
                    ? activeTargetAvailable
                        ? Quaternion.Slerp(
                            outgoingAnkleRotation,
                            targetData.AnkleRotation,
                            successorBlend).normalized
                        : outgoingAnkleRotation
                    : hasTransitionOrigin
                    ? runtime.IsFadingOut
                        ? Quaternion.Slerp(
                            pose.AnkleRotation,
                            outgoingAnkleRotation,
                            activePlanPredictionBlend).normalized
                        : outgoingAnkleRotation
                    : Quaternion.Slerp(
                        pose.AnkleRotation,
                        targetData.AnkleRotation,
                        activePlanPredictionBlend).normalized;
                Vector3 resolvedAnklePosition = activeResolvedAnklePosition;
                Quaternion resolvedAnkleRotation = activeResolvedAnkleRotation;
                if (revisionTargetAvailable)
                {
                    Vector3 revisionResolvedAnklePosition = Vector3.Lerp(
                        pose.AnklePosition,
                        revisionTargetData.AnklePosition,
                        revisionPlanPredictionBlend);
                    Quaternion revisionResolvedAnkleRotation = Quaternion.Slerp(
                        pose.AnkleRotation,
                        revisionTargetData.AnkleRotation,
                        revisionPlanPredictionBlend).normalized;
                    resolvedAnklePosition = Vector3.Lerp(
                        activeResolvedAnklePosition,
                        revisionResolvedAnklePosition,
                        revisionBlend);
                    resolvedAnkleRotation = Quaternion.Slerp(
                        activeResolvedAnkleRotation,
                        revisionResolvedAnkleRotation,
                        revisionBlend).normalized;
                }
                ulong outputPlanSequence = plan.Sequence;
                preContinuityGoalWorldPosition = resolvedAnklePosition;
                runtime.ResolveOutputContinuity(
                    renderFrame,
                    presentationDeltaSeconds,
                    m_TransitionBlendSpeed,
                    baselineOwnsFoot,
                    !revisionTargetAvailable && !hasEventSuccessorHandoff,
                    true,
                    outputPlanSequence,
                    resolvedAnklePosition,
                    resolvedAnkleRotation,
                    out resolvedAnklePosition,
                    out resolvedAnkleRotation);
                CharacterFootPlacementSoleContactPose resolvedContacts = pose.ResolveSoleContacts(
                    resolvedAnklePosition,
                    resolvedAnkleRotation);
                postHeelDistance = Vector3.Dot(
                    resolvedContacts.HeelPosition - currentPathPosition,
                    up);
                postToeDistance = Vector3.Dot(
                    resolvedContacts.ToePosition - currentPathPosition,
                    up);
                predictiveOwnsSoleClearance = !stanceOwnsFoot &&
                                              !runtime.IsFadingOut &&
                                              authoritativePredictionBlend >= 0.999999f;
                if (!stanceOwnsFoot)
                    appliedLift = Vector3.Dot(resolvedAnklePosition - pose.AnklePosition, up);
                bool finalReachValid = true;
                if (!stanceOwnsFoot && !runtime.IsFadingOut)
                {
                    finalReachValid = TryResolveAppliedReachClearance(
                        pose,
                        appliedHip,
                        resolvedAnklePosition,
                        up,
                        legLength * m_Settings.MaximumPredictionReachRatio,
                        out float finalReachClearance);
                    if (finalReachValid && finalReachClearance > 0f)
                    {
                        resolvedAnklePosition += up * finalReachClearance;
                        reachClearance += finalReachClearance;
                        compositeAnimationClearance += finalReachClearance;
                        appliedLift = Vector3.Dot(
                            resolvedAnklePosition - pose.AnklePosition,
                            up);
                        resolvedContacts = pose.ResolveSoleContacts(
                            resolvedAnklePosition,
                            resolvedAnkleRotation);
                        postHeelDistance = Vector3.Dot(
                            resolvedContacts.HeelPosition - currentPathPosition,
                            up);
                        postToeDistance = Vector3.Dot(
                            resolvedContacts.ToePosition - currentPathPosition,
                            up);
                    }
                }
                predictionReachRatio = Vector3.Distance(appliedHip, resolvedAnklePosition) / legLength;
                if (IsFinite(resolvedAnklePosition) && IsFinite(resolvedAnkleRotation) &&
                    float.IsFinite(predictionReachRatio) &&
                    finalReachValid &&
                    !stanceOwnsFoot)
                {
                    result = new CharacterFullBodyIkGoal(
                        stanceGoal.Slot,
                        component.InverseTransformPoint(resolvedAnklePosition),
                        (Quaternion.Inverse(component.rotation) * resolvedAnkleRotation).normalized,
                        stanceGoal.PositionWeight,
                        stanceGoal.RotationWeight,
                        CharacterFullBodyIkGoalApplication.FootPlacementEffectorTarget,
                        stanceGoal.SourceKind | CharacterFullBodyIkGoalSourceKind.PredictiveExtension,
                        stanceGoal.DiagnosticMetadataIndex);
                    rewritten = true;
                    goalOwner = hasTransitionOrigin
                        ? CharacterFootPlacementGoalOwner.PlanTransition
                        : revisionTargetAvailable
                            ? CharacterFootPlacementGoalOwner.RevisionPlan
                            : CharacterFootPlacementGoalOwner.ActivePlan;
                }
            }
            else
            {
                runtime.ResolveOutputContinuity(
                    renderFrame,
                    presentationDeltaSeconds,
                    m_TransitionBlendSpeed,
                    baselineOwnsFoot,
                    true,
                    false,
                    0,
                    Vector3.zero,
                    Quaternion.identity,
                    out _,
                    out _);
            }
            FootPredictionRejectReason rejectReason = ResolveRejectReason(
                plan,
                in step,
                landingEventIdentityValid,
                rewritten,
                predictionReachRatio,
                plan.OwnsEvent && plan.State == CharacterPredictiveFootPlanState.Executing
                    ? stanceOwnsFoot
                    : false);
            if (activeEvaluationRejectReason != FootPredictionRejectReason.None)
                rejectReason = activeEvaluationRejectReason;
            BuildFootDiagnostics(
                runtime,
                plan,
                side,
                in feature,
                in pose,
                in stanceGoal,
                in result,
                rewritten,
                rejectReason,
                currentEventFootPoseWeight,
                planPredictionBlend,
                authoritativePredictionBlend,
                currentSole,
                currentPathPosition,
                currentPathRoot,
                currentPathHip,
                in currentPathSupport,
                up,
                predictionReachRatio,
                preHeelDistance,
                preToeDistance,
                postHeelDistance,
                postToeDistance,
                clearanceEvaluated,
                predictiveOwnsSoleClearance,
                authoredAnimationClearance,
                animationClearanceContinuityOffset,
                animationClearanceContinuityContribution,
                reachClearance,
                compositeAnimationClearance,
                requiredLift,
                appliedLift,
                goalOwner,
                stanceWorldPosition,
                in goalCandidates,
                activeGeometryGoalAvailable,
                activeReachGoalAvailable,
                in targetData,
                activeEvaluationRejectReason,
                revisionGeometryGoalAvailable,
                revisionReachGoalAvailable,
                in revisionTargetData,
                revisionEvaluationRejectReason,
                hasTransitionOrigin,
                transitionOriginAnklePosition,
                preContinuityGoalWorldPosition,
                out diagnostics);
            Vector3 finalWorldPosition = component.TransformPoint(result.ComponentPosition);
            Quaternion finalWorldRotation = (component.rotation * result.ComponentRotation).normalized;
            CharacterFootPlacementSoleContactPose finalContacts = pose.ResolveSoleContacts(
                finalWorldPosition,
                finalWorldRotation);
            runtime.RememberOutput(
                pose.AnklePosition,
                pose.AnkleRotation,
                pose.HipPosition,
                finalWorldPosition,
                finalWorldRotation,
                (finalContacts.HeelPosition + finalContacts.ToePosition) * 0.5f,
                currentPathPosition,
                currentPathSupport,
                targetAvailable ? plan.Sequence : 0,
                currentPathRoot,
                currentPathRootStart,
                currentPathHip);
            if (hasEventSuccessorHandoff && activeTargetAvailable &&
                activePlanPredictionBlend >= 0.999999f &&
                runtime.EventSuccessorHandoffBlendWeight >= 0.999999f)
            {
                runtime.CompleteEventSuccessorHandoff();
            }
            BuildDebugSnapshot(
                runtime,
                plan,
                side,
                clearanceEvaluated,
                rewritten,
                requiredLift,
                appliedLift,
                currentPathPosition,
                stanceWorldPosition,
                baselineContacts.HeelPosition,
                baselineContacts.ToePosition,
                finalWorldPosition,
                finalContacts.HeelPosition,
                finalContacts.ToePosition,
                out debugSnapshot);
            return result;
        }

        void BuildFootDiagnostics(
            CharacterFootPlanExecutionState runtime,
            CharacterPredictiveFootPlanExecution plan,
            CharacterFootSide side,
            in AnimationFootFeatureSample feature,
            in CharacterFootPlacementAnimatedFootPose pose,
            in CharacterFullBodyIkGoal stanceGoal,
            in CharacterFullBodyIkGoal result,
            bool rewritten,
            FootPredictionRejectReason rejectReason,
            float currentEventFootPoseWeight,
            float planPredictionBlend,
            float authoritativePredictionBlend,
            Vector3 currentSole,
            Vector3 currentPathPosition,
            Vector3 currentPathRoot,
            Vector3 currentPathHip,
            in FootPlacementSurface currentPathSupport,
            Vector3 up,
            float predictionReachRatio,
            float preHeelDistance,
            float preToeDistance,
            float postHeelDistance,
            float postToeDistance,
            bool clearanceEvaluated,
            bool predictiveOwnsSoleClearance,
            float authoredAnimationClearance,
            float animationClearanceContinuityOffset,
            float animationClearanceContinuityContribution,
            float reachClearance,
            float compositeAnimationClearance,
            float requiredLift,
            float appliedLift,
            CharacterFootPlacementGoalOwner goalOwner,
            Vector3 stanceWorldPosition,
            in CharacterPredictiveFootGoalCandidates goalCandidates,
            bool activeGeometryGoalAvailable,
            bool activeReachGoalAvailable,
            in CharacterPredictiveFootTarget targetData,
            FootPredictionRejectReason activeEvaluationRejectReason,
            bool revisionGeometryGoalAvailable,
            bool revisionReachGoalAvailable,
            in CharacterPredictiveFootTarget revisionTargetData,
            FootPredictionRejectReason revisionEvaluationRejectReason,
            bool hasTransitionOrigin,
            Vector3 transitionOriginAnklePosition,
            Vector3 preContinuityGoalWorldPosition,
            out CharacterPredictiveFootPlacementFootDiagnostics diagnostics)
        {
            FixedList512Bytes<CharacterPredictiveFootPathSampleDiagnostics> pathSamples =
                BuildPathDiagnostics(plan);
            FixedList128Bytes<Vector3> plannedFootRouteWorld =
                BuildPlannedFootRouteDiagnostics(plan);
            var currentEventDiagnostics = new CharacterPredictiveFootEventDiagnostics(
                side,
                in feature);
            AnimationPredictedFootStepSample incomingStep = feature.IncomingPredictedStep;
            var incomingEventDiagnostics = new CharacterPredictiveFootEventDiagnostics(
                side,
                feature.IsValid,
                incomingStep);
            CharacterFootPlanAttemptDiagnostics planAttempt = runtime.PlanAttempt;
            var planLifecycleDiagnostics =
                new CharacterPredictiveFootPlanLifecycleDiagnostics(plan);
            var queryDiagnostics = new CharacterPredictiveFootQueryDiagnostics(plan);
            CharacterFootGroundingHitDiagnostics pathSupportDiagnostics =
                currentPathSupport.IsValid
                    ? new CharacterFootGroundingHitDiagnostics(
                        new FootPlacementSurface(
                            currentPathSupport.Collider,
                            currentPathPosition,
                            currentPathSupport.Normal))
                    : default;
            Transform component = m_Rig.PoseRoot;
            diagnostics = new CharacterPredictiveFootPlacementFootDiagnostics(
                side,
                rewritten,
                rejectReason,
                new CharacterFootGroundingHitDiagnostics(plan.ProjectedFutureSupport),
                in queryDiagnostics,
                in currentEventDiagnostics,
                in incomingEventDiagnostics,
                currentEventFootPoseWeight,
                m_TrajectoryCurvatureDegreesPerSecond,
                m_TrajectoryCurvatureAvailable,
                planPredictionBlend,
                authoritativePredictionBlend,
                runtime.HasRevision,
                runtime.HasRevision ? runtime.Revision.Sequence : 0,
                runtime.SmoothedTransitionBlendWeight,
                runtime.Transition.Kind,
                in planAttempt,
                runtime.PlanBuildDecision,
                runtime.IsFadingOut,
                runtime.PredictiveRetentionWeight,
                runtime.IntentLandingDisplacementError,
                runtime.IntentLandingDisplacementThreshold,
                plan.LandingDelayAtGeneration,
                plan.OwnsEvent
                    ? Vector3.Distance(plan.ProjectedStart, plan.ProjectedLanding)
                    : 0f,
                in planLifecycleDiagnostics,
                currentSole,
                plan.ProjectedStart,
                plan.ProjectedLanding,
                currentPathPosition,
                currentPathRoot,
                currentPathHip,
                plan.ProjectedPredictedHip,
                plan.ProjectedRootStart,
                plan.ProjectedRootStartRotation,
                plan.ProjectedRootLanding,
                plan.ProjectedRootLandingRotation,
                up,
                m_Settings.MinimumLandingConfidence,
                m_Settings.MaximumPredictionReachRatio,
                predictionReachRatio,
                m_Settings.CastAbove,
                m_Settings.CastBelow,
                m_Settings.PathSphereRadius,
                m_Settings.SwingCapsuleRadius,
                plan.SoleSupportRadius,
                pathSupportDiagnostics,
                preHeelDistance,
                preToeDistance,
                postHeelDistance,
                postToeDistance,
                clearanceEvaluated,
                predictiveOwnsSoleClearance,
                Mathf.Max(0f, -Mathf.Min(postHeelDistance, postToeDistance)),
                authoredAnimationClearance,
                animationClearanceContinuityOffset,
                animationClearanceContinuityContribution,
                reachClearance,
                compositeAnimationClearance,
                requiredLift,
                appliedLift,
                in plannedFootRouteWorld,
                in pathSamples,
                goalOwner,
                pose.AnklePosition,
                stanceWorldPosition,
                activeGeometryGoalAvailable,
                activeGeometryGoalAvailable
                    ? goalCandidates.Active.Target.AnklePosition
                    : default,
                activeReachGoalAvailable,
                activeReachGoalAvailable ? targetData.AnklePosition : default,
                activeEvaluationRejectReason,
                revisionGeometryGoalAvailable,
                revisionGeometryGoalAvailable
                    ? goalCandidates.Revision.Target.AnklePosition
                    : default,
                revisionReachGoalAvailable,
                revisionReachGoalAvailable ? revisionTargetData.AnklePosition : default,
                revisionEvaluationRejectReason,
                hasTransitionOrigin,
                hasTransitionOrigin ? transitionOriginAnklePosition : default,
                preContinuityGoalWorldPosition,
                stanceWorldPosition,
                component.TransformPoint(result.ComponentPosition),
                stanceGoal,
                result);
        }

        static void BuildDebugSnapshot(
            CharacterFootPlanExecutionState runtime,
            CharacterPredictiveFootPlanExecution plan,
            CharacterFootSide side,
            bool clearanceEvaluated,
            bool rewritten,
            float requiredLift,
            float appliedLift,
            Vector3 currentPathPosition,
            Vector3 stanceWorldPosition,
            Vector3 baselineHeelPosition,
            Vector3 baselineToePosition,
            Vector3 finalWorldPosition,
            Vector3 finalHeelPosition,
            Vector3 finalToePosition,
            out CharacterPredictiveFootLegFrameSnapshot debugSnapshot)
        {
            CharacterFootPlanAttemptDiagnostics planAttempt = runtime.PlanAttempt;
            debugSnapshot = new CharacterPredictiveFootLegFrameSnapshot(
                side,
                plan.State,
                plan.ActionProgress,
                plan.GroundPathProgress,
                plan.GeometrySnapshot,
                plan.WorldProjectionMatrix,
                runtime.HasRevision
                    ? runtime.Revision.State
                    : planAttempt.IsAvailable &&
                      planAttempt.GeometrySnapshot != null &&
                      !ReferenceEquals(planAttempt.GeometrySnapshot, plan.GeometrySnapshot)
                        ? planAttempt.State
                        : CharacterPredictiveFootPlanState.Inactive,
                runtime.HasRevision
                    ? runtime.Revision.GeometrySnapshot
                    : planAttempt.IsAvailable &&
                      !ReferenceEquals(planAttempt.GeometrySnapshot, plan.GeometrySnapshot)
                        ? planAttempt.GeometrySnapshot
                        : null,
                runtime.HasRevision
                    ? runtime.Revision.WorldProjectionMatrix
                    : Matrix4x4.identity,
                runtime.Transition.Kind != CharacterFootPlanTransitionKind.None
                    ? runtime.SmoothedTransitionBlendWeight
                    : 0f,
                clearanceEvaluated,
                rewritten,
                requiredLift,
                appliedLift,
                currentPathPosition,
                stanceWorldPosition,
                baselineHeelPosition,
                baselineToePosition,
                finalWorldPosition,
                finalHeelPosition,
                finalToePosition);
        }

        static float ResolveCurrentEventFootPoseWeight(
            in CharacterFootPlacementPoseInput upstreamPose,
            CharacterFootSide side,
            in AnimationPredictedFootStepSample step)
        {
            return step.IsAuthoritative
                ? ResolveFootPoseWeight(
                    in upstreamPose,
                    side,
                    step.ContributionContinuityIdentity)
                : 0f;
        }

        static float ResolveFootPoseWeight(
            in CharacterFootPlacementPoseInput upstreamPose,
            CharacterFootSide side,
            ulong contributionContinuityIdentity)
        {
            if (contributionContinuityIdentity == 0)
                return 0f;
            float weight = 0f;
            for (int i = 0; i < upstreamPose.ContributionCount; i++)
            {
                AnimationPoseSourceContribution contribution = upstreamPose.Contributions[i];
                if (contribution.ContributionContinuityIdentity !=
                    contributionContinuityIdentity)
                {
                    continue;
                }
                weight += side == CharacterFootSide.Left
                    ? contribution.LeftFootWeight
                    : side == CharacterFootSide.Right
                        ? contribution.RightFootWeight
                        : throw new ArgumentOutOfRangeException(nameof(side));
            }
            return Mathf.Clamp01(weight);
        }

        static Vector3 ResolvePathClearanceNormal(
            Vector3 sole,
            Vector3 pathPosition,
            Vector3 surfaceNormal,
            Vector3 up,
            float soleSupportRadius)
        {
            Vector3 normalizedUp = up.normalized;
            Vector3 normal = surfaceNormal.normalized;
            float planarDistance = Vector3.ProjectOnPlane(
                sole - pathPosition,
                normalizedUp).magnitude;
            return planarDistance <= Mathf.Max(0.0001f, soleSupportRadius) &&
                   Vector3.Dot(normalizedUp, normal) > 0.0001f
                ? normal
                : normalizedUp;
        }

        static bool TryEvaluateFootTarget(
            CharacterPredictiveFootPlanExecution plan,
            float eventPhase,
            CharacterFootPlacementAnimatedFootPose pose,
            Vector3 componentUp,
            Vector3 appliedHip,
            float maximumReach,
            out CharacterPredictiveFootTarget target)
        {
            return TryEvaluateFootTarget(
                plan,
                eventPhase,
                pose,
                componentUp,
                appliedHip,
                maximumReach,
                out target,
                out _);
        }

        static bool TryEvaluateFootTarget(
            CharacterPredictiveFootPlanExecution plan,
            float eventPhase,
            CharacterFootPlacementAnimatedFootPose pose,
            Vector3 componentUp,
            Vector3 appliedHip,
            float maximumReach,
            out CharacterPredictiveFootTarget target,
            out FootPredictionRejectReason rejectReason)
        {
            target = default;
            rejectReason = FootPredictionRejectReason.None;
            if (plan.State != CharacterPredictiveFootPlanState.Executing)
            {
                rejectReason = FootPredictionRejectReason.NoCommittedPlan;
                return false;
            }
            Vector3 up = componentUp.normalized;
            plan.EvaluateClearancePath(
                eventPhase,
                out Vector3 pathPosition,
                out Vector3 pathRoot,
                out Vector3 pathHip,
                out FootPlacementSurface pathSupport,
                out Vector3 predictedSole);
            plan.EvaluateActionState(
                eventPhase,
                out _,
                out _,
                out AnimationFootOrientationPolicy orientationPolicy,
                out _);
            FootPlacementSurface support = pathSupport.IsValid
                ? new FootPlacementSurface(pathSupport.Collider, pathPosition, pathSupport.Normal.normalized)
                : default;
            Vector3 supportNormal = support.IsValid ? support.Normal : up;
            Quaternion ankleRotation = orientationPolicy == AnimationFootOrientationPolicy.LandingSurface
                ? (Quaternion.FromToRotation(up, supportNormal) * pose.AnkleRotation).normalized
                : pose.AnkleRotation;
            CharacterFootPlacementSoleContactPose rotatedContacts = pose.ResolveSoleContacts(
                pose.AnklePosition,
                ankleRotation);
            Vector3 rotatedSole = (rotatedContacts.HeelPosition + rotatedContacts.ToePosition) * 0.5f;
            CharacterFootPlacementSoleContactPose nativeContacts = pose.ResolveSoleContacts(
                pose.AnklePosition,
                pose.AnkleRotation);
            Vector3 nativeSole = (nativeContacts.HeelPosition + nativeContacts.ToePosition) * 0.5f;
            Vector3 envelopeNormal = ResolvePathClearanceNormal(
                nativeSole,
                pathPosition,
                supportNormal,
                up,
                plan.SoleSupportRadius);
            plan.EvaluateAnimationClearance(
                eventPhase,
                out float authoredAnimationClearance,
                out float animationClearanceContinuityOffset,
                out float animationClearanceContinuityContribution,
                out _,
                out float compositeAnimationClearance);
            float nativeSoleHeight = Vector3.Dot(nativeSole, up);
            float predictedSoleHeight = Vector3.Dot(predictedSole, up);
            Vector3 targetSole = nativeSole + up * (
                predictedSoleHeight -
                nativeSoleHeight);
            Vector3 anklePosition = targetSole + pose.AnklePosition - rotatedSole;
            float authoredSoleHeight = Vector3.Dot(pathPosition, up) +
                                       authoredAnimationClearance;
            Vector3 authoredTargetSole = nativeSole + up * (
                authoredSoleHeight -
                nativeSoleHeight);
            Vector3 authoredAnklePosition =
                authoredTargetSole + pose.AnklePosition - rotatedSole;
            CharacterFootPlacementSoleContactPose contacts = pose.ResolveSoleContacts(
                anklePosition,
                ankleRotation);
            float heelDistance = Vector3.Dot(contacts.HeelPosition - pathPosition, envelopeNormal);
            float toeDistance = Vector3.Dot(contacts.ToePosition - pathPosition, envelopeNormal);
            float penetration = Mathf.Max(0f, -Mathf.Min(heelDistance, toeDistance));
            float upNormalDot = Vector3.Dot(up, envelopeNormal);
            if (penetration > 0f && upNormalDot > 0.0001f)
            {
                anklePosition += up * (penetration / upNormalDot);
                contacts = pose.ResolveSoleContacts(anklePosition, ankleRotation);
                heelDistance = Vector3.Dot(contacts.HeelPosition - pathPosition, envelopeNormal);
                toeDistance = Vector3.Dot(contacts.ToePosition - pathPosition, envelopeNormal);
            }
            CharacterFootPlacementSoleContactPose authoredContacts = pose.ResolveSoleContacts(
                authoredAnklePosition,
                ankleRotation);
            float authoredPenetration = Mathf.Max(
                0f,
                -Mathf.Min(
                    Vector3.Dot(authoredContacts.HeelPosition - pathPosition, envelopeNormal),
                    Vector3.Dot(authoredContacts.ToePosition - pathPosition, envelopeNormal)));
            if (authoredPenetration > 0f && upNormalDot > 0.0001f)
                authoredAnklePosition += up * (authoredPenetration / upNormalDot);
            if (!IsFinite(anklePosition) || !IsFinite(ankleRotation) ||
                !IsFinite(authoredAnklePosition) || !IsFinite(envelopeNormal) ||
                !float.IsFinite(heelDistance) || !float.IsFinite(toeDistance))
            {
                rejectReason = FootPredictionRejectReason.NonFinite;
                return false;
            }
            var geometryTarget = new CharacterPredictiveFootTarget(
                pathPosition,
                pathRoot,
                pathHip,
                support,
                envelopeNormal,
                authoredAnklePosition,
                anklePosition,
                ankleRotation,
                contacts,
                heelDistance,
                toeDistance,
                authoredAnimationClearance,
                animationClearanceContinuityOffset,
                animationClearanceContinuityContribution,
                0f,
                compositeAnimationClearance);
            if (maximumReach <= 0f)
            {
                target = geometryTarget;
                return true;
            }
            return TryResolveFootTargetReach(
                pose,
                appliedHip,
                up,
                maximumReach,
                in geometryTarget,
                out target,
                out rejectReason);
        }

        static bool TryResolveFootTargetReach(
            CharacterFootPlacementAnimatedFootPose pose,
            Vector3 appliedHip,
            Vector3 up,
            float maximumReach,
            in CharacterPredictiveFootTarget geometryTarget,
            out CharacterPredictiveFootTarget target,
            out FootPredictionRejectReason rejectReason)
        {
            target = default;
            rejectReason = FootPredictionRejectReason.None;
            Vector3 anklePosition = geometryTarget.AnklePosition;
            float animationClearanceContinuityContribution =
                geometryTarget.AnimationClearanceContinuityContribution;
            float compositeAnimationClearance = geometryTarget.CompositeAnimationClearance;
            if (!TryClampTransitionClearanceToReach(
                    pose,
                    appliedHip,
                    geometryTarget.AuthoredAnklePosition,
                    up,
                    maximumReach,
                    ref anklePosition,
                    out float transitionClearanceReduction))
            {
                rejectReason = FootPredictionRejectReason.ReachExceeded;
                return false;
            }
            CharacterFootPlacementSoleContactPose contacts = geometryTarget.Contacts;
            float heelDistance = geometryTarget.HeelPlaneDistance;
            float toeDistance = geometryTarget.ToePlaneDistance;
            if (transitionClearanceReduction > 0f)
            {
                animationClearanceContinuityContribution = Mathf.Max(
                    0f,
                    animationClearanceContinuityContribution - transitionClearanceReduction);
                compositeAnimationClearance = Mathf.Max(
                    geometryTarget.AuthoredAnimationClearance,
                    compositeAnimationClearance - transitionClearanceReduction);
                contacts = pose.ResolveSoleContacts(
                    anklePosition,
                    geometryTarget.AnkleRotation);
                heelDistance = Vector3.Dot(
                    contacts.HeelPosition - geometryTarget.PathPosition,
                    geometryTarget.ClearanceNormal);
                toeDistance = Vector3.Dot(
                    contacts.ToePosition - geometryTarget.PathPosition,
                    geometryTarget.ClearanceNormal);
            }
            if (!TryResolveAppliedReachClearance(
                    pose,
                    appliedHip,
                    anklePosition,
                    up,
                    maximumReach,
                    out float reachClearance))
            {
                rejectReason = FootPredictionRejectReason.ReachExceeded;
                return false;
            }
            if (reachClearance > 0f)
            {
                anklePosition += up * reachClearance;
                contacts = pose.ResolveSoleContacts(anklePosition, geometryTarget.AnkleRotation);
                heelDistance = Vector3.Dot(
                    contacts.HeelPosition - geometryTarget.PathPosition,
                    geometryTarget.ClearanceNormal);
                toeDistance = Vector3.Dot(
                    contacts.ToePosition - geometryTarget.PathPosition,
                    geometryTarget.ClearanceNormal);
            }
            if (!IsFinite(anklePosition) || !IsFinite(geometryTarget.AnkleRotation) ||
                !float.IsFinite(heelDistance) || !float.IsFinite(toeDistance))
            {
                rejectReason = FootPredictionRejectReason.NonFinite;
                return false;
            }
            target = new CharacterPredictiveFootTarget(
                geometryTarget.PathPosition,
                geometryTarget.PathRoot,
                geometryTarget.PathHip,
                geometryTarget.Support,
                geometryTarget.ClearanceNormal,
                geometryTarget.AuthoredAnklePosition,
                anklePosition,
                geometryTarget.AnkleRotation,
                contacts,
                heelDistance,
                toeDistance,
                geometryTarget.AuthoredAnimationClearance,
                geometryTarget.AnimationClearanceContinuityOffset,
                animationClearanceContinuityContribution,
                reachClearance,
                compositeAnimationClearance + reachClearance);
            return true;
        }

        static bool TryClampTransitionClearanceToReach(
            CharacterFootPlacementAnimatedFootPose pose,
            Vector3 appliedHip,
            Vector3 authoredAnklePosition,
            Vector3 up,
            float maximumReach,
            ref Vector3 targetAnklePosition,
            out float reduction)
        {
            reduction = 0f;
            if (maximumReach <= 0f)
                return true;
            if (!IsFinite(appliedHip) || !IsFinite(authoredAnklePosition) ||
                !IsFinite(targetAnklePosition) || !float.IsFinite(maximumReach))
            {
                return false;
            }
            float authoredReach = Vector3.Distance(appliedHip, pose.AnklePosition);
            float allowedReach = Mathf.Max(maximumReach, authoredReach);
            Vector3 hipToTarget = targetAnklePosition - appliedHip;
            float horizontalSquared = Vector3.ProjectOnPlane(hipToTarget, up).sqrMagnitude;
            float verticalSquared = allowedReach * allowedReach - horizontalSquared;
            if (!float.IsFinite(authoredReach) || verticalSquared < -0.0001f)
                return false;
            float maximumVertical = Mathf.Sqrt(Mathf.Max(0f, verticalSquared));
            float targetVertical = Vector3.Dot(hipToTarget, up);
            if (!float.IsFinite(targetVertical) || targetVertical <= maximumVertical + 0.0001f)
                return float.IsFinite(targetVertical);
            float authoredVertical = Vector3.Dot(authoredAnklePosition - appliedHip, up);
            if (!float.IsFinite(authoredVertical) || authoredVertical > maximumVertical + 0.0001f)
                return false;
            reduction = targetVertical - maximumVertical;
            targetAnklePosition -= up * reduction;
            return IsFinite(targetAnklePosition) && float.IsFinite(reduction);
        }

        Vector3 ResolveAppliedHip(
            Vector3 animatedHip,
            CharacterFullBodyIkGoal pelvis)
        {
            Vector3 translation = m_Rig.PoseRoot.rotation * pelvis.ComponentPosition *
                                  Mathf.Clamp01(pelvis.PositionWeight);
            return animatedHip + translation;
        }

        static bool TryResolveAppliedReachClearance(
            CharacterFootPlacementAnimatedFootPose pose,
            Vector3 appliedHip,
            Vector3 targetAnkle,
            Vector3 up,
            float maximumReach,
            out float clearance)
        {
            clearance = 0f;
            if (maximumReach <= 0f)
                return true;
            if (!IsFinite(appliedHip) || !IsFinite(targetAnkle) ||
                !float.IsFinite(maximumReach))
            {
                return false;
            }
            float authoredReach = Vector3.Distance(appliedHip, pose.AnklePosition);
            float allowedReach = Mathf.Max(maximumReach, authoredReach);
            Vector3 hipToAnkle = targetAnkle - appliedHip;
            float horizontalSquared = Vector3.ProjectOnPlane(hipToAnkle, up).sqrMagnitude;
            float verticalSquared = allowedReach * allowedReach - horizontalSquared;
            if (!float.IsFinite(authoredReach) || verticalSquared < -0.0001f)
                return false;
            float vertical = Vector3.Dot(hipToAnkle, up);
            float maximumVertical = Mathf.Sqrt(Mathf.Max(0f, verticalSquared));
            if (!float.IsFinite(vertical) || vertical > maximumVertical + 0.0001f)
                return false;
            clearance = Mathf.Max(0f, -maximumVertical - vertical);
            return float.IsFinite(clearance);
        }

        void PrepareFoot(
            CharacterFootSide side,
            CharacterFootPlanExecutionState runtime,
            CharacterFootPlacementAnimatedFootPose pose,
            AnimationFootFeatureSample feature,
            in CharacterFootLandingCommit landingCommit,
            in CharacterFootPlanSupportFacts supportFacts,
            ulong renderFrame,
            Vector3 rootWorldPosition,
            Quaternion rootWorldRotation,
            Vector3 presentedBodyPosition,
            Vector3 committedBodyVelocity,
            float trajectoryCurvatureDegreesPerSecond,
            bool trajectoryCurvatureAvailable,
            in CommittedLocomotionPlanarMotionTimeline motionTimeline,
            double movementPlaybackTime,
            float legLength,
            float presentationDeltaSeconds)
        {
            runtime.BeginFrame();
            runtime.ClearIntentObservation();
            var transitionCapture = new CharacterFootPlanTransitionCapture(
                pose.AnklePosition,
                pose.AnkleRotation,
                pose.HipPosition);
            CharacterPredictiveFootPlanExecution plan = runtime.Active;
            AnimationPredictedFootStepSample step = feature.PredictedStep;
            AnimationPredictedFootStepSample incomingStep = feature.IncomingPredictedStep;
            bool landingEventIdentityValid = step.HasConsistentLandingEventIdentity(side);
            bool incomingLandingEventIdentityValid =
                incomingStep.HasConsistentLandingEventIdentity(side);
            bool currentPlanMatches = landingEventIdentityValid &&
                                      plan.MatchesAuthoritativeEvent(in step);
            Transform component = m_Rig.PoseRoot;
            Vector3 up = component.up.normalized;
            CharacterFootPlacementSoleContactPose contacts = pose.ResolveSoleContacts(
                pose.AnklePosition,
                pose.AnkleRotation);
            Vector3 currentSole = (contacts.HeelPosition + contacts.ToePosition) * 0.5f;
            float soleSupportRadius = Mathf.Max(
                Vector3.ProjectOnPlane(contacts.HeelPosition - currentSole, up).magnitude,
                Vector3.ProjectOnPlane(contacts.ToePosition - currentSole, up).magnitude);
            bool planningCandidate = landingEventIdentityValid &&
                                     (step.IsPreSwing || step.ActionStepClock.IsSwing) &&
                                     motionTimeline.IsValid &&
                                     step.Confidence >= m_Settings.MinimumLandingConfidence &&
                                     step.ActionStepClock.Phase < 0.9999f;
            bool incomingPlanningCandidate = incomingLandingEventIdentityValid &&
                                             incomingStep.IsPreSwing &&
                                             motionTimeline.IsValid &&
                                             incomingStep.Confidence >= m_Settings.MinimumLandingConfidence &&
                                             incomingStep.ActionStepClock.Phase < 0.9999f;
            bool activeEventReplaced = plan.OwnsEvent && !currentPlanMatches;
            CharacterPredictiveFootPlanEndReason replacementReason = activeEventReplaced
                ? ResolveReplacementEndReason(plan)
                : CharacterPredictiveFootPlanEndReason.None;
            if (plan.HasExecutablePath && currentPlanMatches)
            {
                plan.SynchronizePoseContribution(in step);
                plan.SynchronizeActionClock(renderFrame, in step);
                plan.UpdateWorldProjection(rootWorldPosition, rootWorldRotation);
                plan.ObserveWorldMotionDeviation(
                    presentedBodyPosition,
                    rootWorldRotation,
                    Mathf.Max(m_Settings.PathSphereRadius, m_Settings.SwingCapsuleRadius));
            }
            bool intentRevisionRequested = currentPlanMatches &&
                                           plan.HasExecutablePath &&
                                           !runtime.IsFadingOut &&
                                           ShouldRequestIntentRevision(
                                               runtime,
                                               plan,
                                               in step,
                                               in motionTimeline,
                                               movementPlaybackTime,
                                               step.PredictionLeadSeconds,
                                               trajectoryCurvatureDegreesPerSecond,
                                               presentedBodyPosition,
                                               rootWorldRotation);
            if (runtime.HasEventSuccessor && intentRevisionRequested)
            {
                runtime.CancelRevision(
                    CharacterPredictiveFootPlanEndReason.MotionDeviationExceeded);
            }
            CharacterFootCompletedOutput lastOutput = runtime.LastOutput;
            bool outgoingOutputAvailable = lastOutput.HasGroundPath &&
                                           lastOutput.PlanSequence == plan.Sequence;
            CharacterFootPlanSupportFact currentQueryFact = supportFacts.CurrentQuery;
            CharacterFootPlanSupportFact committedStanceFact = supportFacts.CommittedStance;
            CharacterFootPlanBuildOrigin currentFrameOrigin = BuildPlanOrigin(
                in currentQueryFact,
                plan.Sequence,
                step.LandingEventIdentity,
                up);
            CharacterFootPlanBuildOrigin committedStanceOrigin = BuildPlanOrigin(
                in committedStanceFact,
                plan.Sequence,
                step.LandingEventIdentity,
                up);
            CharacterFootPlanBuildOrigin activeOutputOrigin = outgoingOutputAvailable &&
                                                               lastOutput.IsAvailable
                ? BuildPlanOrigin(
                    CharacterFootPlanOriginKind.ActivePlanOutput,
                    plan.Sequence,
                    plan.LandingEventIdentity,
                    lastOutput.FinalSolePosition,
                    lastOutput.GroundPathPosition,
                    lastOutput.GroundSupport,
                    up)
                : default;
            CharacterFootPlanBuildOrigin projectedLandingOrigin = default;
            if (plan.HasExecutablePath && plan.FutureSupport.IsValid)
            {
                FootPlacementSurface plannedLandingSupport = ResolveSupportAtRoutePoint(
                    plan.ProjectedFutureSupport,
                    plan.ProjectedLanding,
                    up);
                if (plannedLandingSupport.IsValid)
                {
                    projectedLandingOrigin = BuildPlanOrigin(
                        CharacterFootPlanOriginKind.ProjectedLanding,
                        plan.Sequence,
                        plan.LandingEventIdentity,
                        plan.ProjectedLanding,
                        plannedLandingSupport.Point,
                        plannedLandingSupport,
                        up);
                }
            }
            bool incomingSuccessorNeedsSlot = currentPlanMatches &&
                                              plan.HasExecutablePath &&
                                              !intentRevisionRequested &&
                                              CanPrepareEventSuccessor(plan) &&
                                              incomingPlanningCandidate &&
                                              !plan.MatchesAuthoritativeEvent(in incomingStep);
            if (runtime.HasIntentRevision &&
                (incomingSuccessorNeedsSlot || activeEventReplaced))
            {
                runtime.CancelRevision(CharacterPredictiveFootPlanEndReason.EventReplaced);
            }
            if (runtime.HasRevision)
            {
                CharacterPredictiveFootPlanExecution revision = runtime.Revision;
                if (runtime.HasEventSuccessor)
                {
                    bool revisionMatchesCurrent = landingEventIdentityValid &&
                                                  revision.MatchesAuthoritativeEvent(in step);
                    bool revisionMatchesIncoming = incomingLandingEventIdentityValid &&
                                                   revision.MatchesAuthoritativeEvent(in incomingStep);
                    if (revisionMatchesCurrent)
                    {
                        revision.SynchronizePoseContribution(in step);
                        revision.SynchronizeActionClock(renderFrame, in step);
                        revision.UpdateWorldProjection(rootWorldPosition, rootWorldRotation);
                        revision.ObserveWorldMotionDeviation(
                            presentedBodyPosition,
                            rootWorldRotation,
                            Mathf.Max(m_Settings.PathSphereRadius, m_Settings.SwingCapsuleRadius));
                        bool successorOriginValid;
                        if (landingCommit.TryResolve(
                                plan.LandingEventIdentity,
                                out Vector3 successorLandingSole,
                                out FootPlacementSurface successorLandingSupport))
                        {
                            successorOriginValid = IsEventSuccessorOriginCompatible(
                                revision,
                                successorLandingSole,
                                successorLandingSupport,
                                up);
                        }
                        else
                        {
                            CharacterFootPlanSupportFact currentQuery = supportFacts.CurrentQuery;
                            successorOriginValid = currentQuery.HasSupport &&
                                                   IsEventSuccessorOriginCompatible(
                                                       revision,
                                                       currentQuery.Sole,
                                                       currentQuery.Support,
                                                       up);
                        }
                        if (IsMotionWithinCommitTolerance(revision) && successorOriginValid)
                        {
                            runtime.PromoteRevision(in transitionCapture);
                            plan = runtime.Active;
                            currentPlanMatches = true;
                            activeEventReplaced = false;
                            replacementReason = CharacterPredictiveFootPlanEndReason.None;
                        }
                        else
                        {
                            runtime.CancelRevision(
                                CharacterPredictiveFootPlanEndReason.MotionDeviationExceeded);
                        }
                    }
                    else if (currentPlanMatches && revisionMatchesIncoming)
                    {
                        revision.SynchronizePoseContribution(in incomingStep);
                        revision.SynchronizeActionClock(renderFrame, in incomingStep);
                        revision.UpdateWorldProjection(rootWorldPosition, rootWorldRotation);
                    }
                    else
                    {
                        runtime.CancelRevision(ResolveReplacementEndReason(revision));
                    }
                }
                else
                {
                    bool retainOutgoingIntentRevision = activeEventReplaced &&
                                                        runtime.HasIntentRevision &&
                                                        revision.State == CharacterPredictiveFootPlanState.Executing;
                    if (!step.IsAuthoritative || !revision.MatchesAuthoritativeEvent(in step))
                    {
                        if (!retainOutgoingIntentRevision)
                            runtime.CancelRevision(ResolveReplacementEndReason(revision));
                    }
                    else
                    {
                        revision.SynchronizePoseContribution(in step);
                        revision.SynchronizeActionClock(renderFrame, in step);
                        revision.UpdateWorldProjection(rootWorldPosition, rootWorldRotation);
                    }
                }
            }
            bool hasCommittedLanding = landingCommit.TryResolve(
                plan.LandingEventIdentity,
                out Vector3 committedLandingSole,
                out FootPlacementSurface committedLandingSupport);
            CharacterFootPlanBuildOrigin committedLandingOrigin = default;
            if (hasCommittedLanding)
            {
                FootPlacementSurface resolvedCommittedSupport = ResolveSupportAtRoutePoint(
                    committedLandingSupport,
                    committedLandingSole,
                    up);
                committedLandingOrigin = BuildPlanOrigin(
                    CharacterFootPlanOriginKind.CommittedLanding,
                    plan.Sequence,
                    plan.LandingEventIdentity,
                    committedLandingSole,
                    resolvedCommittedSupport.IsValid
                        ? resolvedCommittedSupport.Point
                        : committedLandingSole,
                    resolvedCommittedSupport,
                    up);
            }
            CharacterFootPlanBuildOrigin eventSuccessorOrigin = committedLandingOrigin.HasSupport
                ? committedLandingOrigin
                : projectedLandingOrigin;
            CharacterFootPlanBuildOrigin replacementOrigin = committedLandingOrigin.HasSupport
                ? committedLandingOrigin
                : currentFrameOrigin;
            bool currentStepOwnsCommittedStance = step.IsAuthoritative &&
                                                   step.EvaluateConstraintMode(
                                                       step.ActionStepClock.Phase) !=
                                                   AnimationFootConstraintMode.Unlocked;
            CharacterFootPlanBuildOrigin initialOrigin =
                currentStepOwnsCommittedStance && committedStanceOrigin.HasSupport
                ? committedStanceOrigin
                : currentFrameOrigin;
            if (currentPlanMatches && plan.HasExecutablePath &&
                !intentRevisionRequested &&
                CanPrepareEventSuccessor(plan) &&
                IsMotionWithinCommitTolerance(plan) &&
                eventSuccessorOrigin.HasSupport &&
                incomingPlanningCandidate &&
                !plan.MatchesAuthoritativeEvent(in incomingStep) &&
                runtime.CanBeginTransition &&
                m_FutureBodyTrajectorySource != null &&
                !runtime.HasAttemptedPlanBuildRevision(
                    CharacterFootPlanAttemptKind.EventSuccessor,
                    plan.Sequence,
                    motionTimeline.Generation,
                    motionTimeline.AuthorityTick.Value))
            {
                var request = new CharacterFootPlanBuildRequest(
                    CharacterFootPlanAttemptKind.EventSuccessor,
                    side,
                    in incomingStep,
                    renderFrame,
                    in eventSuccessorOrigin,
                    soleSupportRadius,
                    rootWorldPosition,
                    rootWorldRotation,
                    presentedBodyPosition,
                    committedBodyVelocity,
                    trajectoryCurvatureDegreesPerSecond,
                    trajectoryCurvatureAvailable,
                    in motionTimeline,
                    movementPlaybackTime,
                    up,
                    legLength);
                bool created = CreatePlan(
                    runtime.Revision,
                    in request,
                    out CharacterFootPlanAttemptDiagnostics planAttempt);
                runtime.RecordPlanAttempt(in planAttempt);
                runtime.MarkPlanBuildRevisionAttempt(
                    CharacterFootPlanAttemptKind.EventSuccessor,
                    plan.Sequence,
                    in motionTimeline);
                if (created)
                    runtime.BeginEventSuccessor();
            }
            if (activeEventReplaced &&
                (runtime.CanBeginTransition || runtime.IsFadingOut))
            {
                ulong sourceSequence = step.LandingEventIdentity;
                bool canAttempt = planningCandidate &&
                                  replacementOrigin.IsAvailable &&
                                  m_FutureBodyTrajectorySource != null &&
                                   !runtime.HasAttemptedPlanBuildRevision(
                                       CharacterFootPlanAttemptKind.CurrentEventReplacement,
                                       sourceSequence,
                                       motionTimeline.Generation,
                                       motionTimeline.AuthorityTick.Value);
                bool created = false;
                if (canAttempt)
                {
                    var request = new CharacterFootPlanBuildRequest(
                        CharacterFootPlanAttemptKind.CurrentEventReplacement,
                        side,
                        in step,
                        renderFrame,
                        in replacementOrigin,
                        soleSupportRadius,
                        rootWorldPosition,
                        rootWorldRotation,
                        presentedBodyPosition,
                        committedBodyVelocity,
                        trajectoryCurvatureDegreesPerSecond,
                        trajectoryCurvatureAvailable,
                        in motionTimeline,
                        movementPlaybackTime,
                        up,
                        legLength);
                    created = CreatePlan(
                        runtime.Revision,
                        in request,
                        out CharacterFootPlanAttemptDiagnostics planAttempt);
                    runtime.RecordPlanAttempt(in planAttempt);
                    runtime.MarkPlanBuildRevisionAttempt(
                        CharacterFootPlanAttemptKind.CurrentEventReplacement,
                        sourceSequence,
                        in motionTimeline);
                }
                if (created)
                {
                    runtime.BeginEventSuccessor();
                    runtime.Revision.UpdateWorldProjection(rootWorldPosition, rootWorldRotation);
                    if (runtime.Revision.MatchesAuthoritativeEvent(in step) &&
                        IsMotionWithinCommitTolerance(runtime.Revision))
                    {
                        runtime.PromoteRevision(in transitionCapture);
                        plan = runtime.Active;
                        currentPlanMatches = true;
                        activeEventReplaced = false;
                        replacementReason = CharacterPredictiveFootPlanEndReason.None;
                    }
                    else
                    {
                        runtime.CancelRevision(
                            CharacterPredictiveFootPlanEndReason.MotionDeviationExceeded);
                        runtime.BeginFadeOut(
                            replacementReason,
                            renderFrame,
                            in transitionCapture);
                    }
                }
                else if (!runtime.IsFadingOut)
                    runtime.BeginFadeOut(
                        replacementReason,
                        renderFrame,
                        in transitionCapture);
            }
            runtime.AdvanceTransition(
                renderFrame,
                presentationDeltaSeconds,
                m_TransitionBlendSpeed,
                in transitionCapture);
            plan = runtime.Active;
            bool needsInitialPlan = !plan.OwnsEvent ||
                                    plan.State == CharacterPredictiveFootPlanState.Rejected &&
                                    plan.MatchesAuthoritativeEvent(in step);
            ulong initialAttemptIdentity = plan.Sequence != 0
                ? plan.Sequence
                : step.LandingEventIdentity;
            if (planningCandidate && needsInitialPlan && runtime.CanBeginTransition &&
                m_FutureBodyTrajectorySource != null &&
                !runtime.HasAttemptedPlanBuildRevision(
                    CharacterFootPlanAttemptKind.Initial,
                    initialAttemptIdentity,
                    motionTimeline.Generation,
                    motionTimeline.AuthorityTick.Value))
            {
                var request = new CharacterFootPlanBuildRequest(
                    CharacterFootPlanAttemptKind.Initial,
                    side,
                    in step,
                    renderFrame,
                    in initialOrigin,
                    soleSupportRadius,
                    rootWorldPosition,
                    rootWorldRotation,
                    presentedBodyPosition,
                    committedBodyVelocity,
                    trajectoryCurvatureDegreesPerSecond,
                    trajectoryCurvatureAvailable,
                    in motionTimeline,
                    movementPlaybackTime,
                    up,
                    legLength);
                CreatePlan(
                    plan,
                    in request,
                    out CharacterFootPlanAttemptDiagnostics planAttempt);
                runtime.RecordPlanAttempt(in planAttempt);
                runtime.MarkPlanBuildRevisionAttempt(
                    CharacterFootPlanAttemptKind.Initial,
                    plan.Sequence != 0 ? plan.Sequence : initialAttemptIdentity,
                    in motionTimeline);
                if (plan.HasExecutablePath)
                    plan.UpdateWorldProjection(rootWorldPosition, rootWorldRotation);
            }
            CharacterFootPlanBuildOrigin revisionOrigin = activeOutputOrigin.IsAvailable
                ? activeOutputOrigin
                : currentFrameOrigin;
            if (runtime.CanBeginTransition && plan.HasExecutablePath &&
                plan.MatchesAuthoritativeEvent(in step) &&
                intentRevisionRequested)
            {
                runtime.MarkPlanBuildRevisionAttempt(
                    CharacterFootPlanAttemptKind.IntentRevision,
                    plan.Sequence,
                    in motionTimeline);
                var request = new CharacterFootPlanBuildRequest(
                    CharacterFootPlanAttemptKind.IntentRevision,
                    side,
                    in step,
                    renderFrame,
                    in revisionOrigin,
                    soleSupportRadius,
                    rootWorldPosition,
                    rootWorldRotation,
                    presentedBodyPosition,
                    committedBodyVelocity,
                    trajectoryCurvatureDegreesPerSecond,
                    trajectoryCurvatureAvailable,
                    in motionTimeline,
                    movementPlaybackTime,
                    up,
                    legLength);
                bool created = CreatePlan(
                    runtime.Revision,
                    in request,
                    out CharacterFootPlanAttemptDiagnostics planAttempt);
                runtime.RecordPlanAttempt(in planAttempt);
                if (created)
                {
                    runtime.Revision.UpdateWorldProjection(rootWorldPosition, rootWorldRotation);
                    if (runtime.HasCompleteOutputForPlan(plan.Sequence))
                        runtime.BeginIntentRevision(in transitionCapture);
                    else
                        runtime.PromoteUncommittedRevision(
                            CharacterPredictiveFootPlanEndReason.MotionDeviationExceeded);
                }
            }
            bool currentPlanHasExecutablePath = plan.HasExecutablePath;
            bool canPrepareEventSuccessor = currentPlanHasExecutablePath &&
                                            CanPrepareEventSuccessor(plan);
            bool motionWithinCommitTolerance = currentPlanHasExecutablePath &&
                                               IsMotionWithinCommitTolerance(plan);
            CharacterFootPlanBuildDecisionDiagnostics buildDecision = ResolvePlanBuildDecision(
                side,
                runtime,
                plan,
                in step,
                in incomingStep,
                in motionTimeline,
                planningCandidate,
                incomingPlanningCandidate,
                currentPlanMatches,
                activeEventReplaced,
                needsInitialPlan,
                intentRevisionRequested,
                canPrepareEventSuccessor,
                motionWithinCommitTolerance,
                in eventSuccessorOrigin,
                in replacementOrigin,
                in initialOrigin,
                in revisionOrigin);
            runtime.RecordPlanBuildDecision(in buildDecision);
        }

        CharacterFootPlanBuildDecisionDiagnostics ResolvePlanBuildDecision(
            CharacterFootSide side,
            CharacterFootPlanExecutionState runtime,
            CharacterPredictiveFootPlanExecution plan,
            in AnimationPredictedFootStepSample step,
            in AnimationPredictedFootStepSample incomingStep,
            in CommittedLocomotionPlanarMotionTimeline motionTimeline,
            bool planningCandidate,
            bool incomingPlanningCandidate,
            bool currentPlanMatches,
            bool activeEventReplaced,
            bool needsInitialPlan,
            bool intentRevisionRequested,
            bool canPrepareEventSuccessor,
            bool motionWithinCommitTolerance,
            in CharacterFootPlanBuildOrigin eventSuccessorOrigin,
            in CharacterFootPlanBuildOrigin replacementOrigin,
            in CharacterFootPlanBuildOrigin initialOrigin,
            in CharacterFootPlanBuildOrigin revisionOrigin)
        {
            CharacterFootPlanAttemptDiagnostics attempt = runtime.PlanAttempt;
            CharacterFootPlanAttemptKind candidateKind = CharacterFootPlanAttemptKind.None;
            CharacterFootPlanBuildOrigin origin = default;
            ulong landingEventIdentity = 0;
            if (attempt.IsAvailable)
            {
                candidateKind = attempt.Kind;
                landingEventIdentity = attempt.LandingEventIdentity;
                origin = candidateKind switch
                {
                    CharacterFootPlanAttemptKind.EventSuccessor => eventSuccessorOrigin,
                    CharacterFootPlanAttemptKind.CurrentEventReplacement => replacementOrigin,
                    CharacterFootPlanAttemptKind.Initial => initialOrigin,
                    CharacterFootPlanAttemptKind.IntentRevision => revisionOrigin,
                    _ => default
                };
            }
            else if (activeEventReplaced)
            {
                candidateKind = CharacterFootPlanAttemptKind.CurrentEventReplacement;
                landingEventIdentity = step.LandingEventIdentity;
                origin = replacementOrigin;
            }
            else if (needsInitialPlan)
            {
                candidateKind = CharacterFootPlanAttemptKind.Initial;
                landingEventIdentity = step.LandingEventIdentity;
                origin = initialOrigin;
            }
            else if (intentRevisionRequested)
            {
                candidateKind = CharacterFootPlanAttemptKind.IntentRevision;
                landingEventIdentity = step.LandingEventIdentity;
                origin = revisionOrigin;
            }
            else if (currentPlanMatches && plan.HasExecutablePath && canPrepareEventSuccessor)
            {
                candidateKind = CharacterFootPlanAttemptKind.EventSuccessor;
                landingEventIdentity = incomingStep.LandingEventIdentity;
                origin = eventSuccessorOrigin;
            }

            CharacterFootPlanBuildDecisionReason reason = attempt.IsAvailable
                ? CharacterFootPlanBuildDecisionReason.Attempted
                : ResolvePlanBuildDecisionReason(
                    runtime,
                    plan,
                    candidateKind,
                    side,
                    in step,
                    in incomingStep,
                    in motionTimeline,
                    planningCandidate,
                    incomingPlanningCandidate,
                    currentPlanMatches,
                    canPrepareEventSuccessor,
                    motionWithinCommitTolerance,
                    in origin);
            return new CharacterFootPlanBuildDecisionDiagnostics(
                candidateKind,
                reason,
                landingEventIdentity,
                in origin,
                motionTimeline.Generation,
                motionTimeline.AuthorityTick.Value,
                attempt.IsAvailable,
                planningCandidate,
                incomingPlanningCandidate,
                currentPlanMatches,
                activeEventReplaced,
                needsInitialPlan,
                intentRevisionRequested,
                canPrepareEventSuccessor,
                motionWithinCommitTolerance,
                runtime.CanBeginTransition,
                m_FutureBodyTrajectorySource != null,
                plan.HasExecutablePath,
                runtime.IsFadingOut);
        }

        CharacterFootPlanBuildDecisionReason ResolvePlanBuildDecisionReason(
            CharacterFootPlanExecutionState runtime,
            CharacterPredictiveFootPlanExecution plan,
            CharacterFootPlanAttemptKind candidateKind,
            CharacterFootSide side,
            in AnimationPredictedFootStepSample step,
            in AnimationPredictedFootStepSample incomingStep,
            in CommittedLocomotionPlanarMotionTimeline motionTimeline,
            bool planningCandidate,
            bool incomingPlanningCandidate,
            bool currentPlanMatches,
            bool canPrepareEventSuccessor,
            bool motionWithinCommitTolerance,
            in CharacterFootPlanBuildOrigin origin)
        {
            if (runtime.IsFadingOut)
                return CharacterFootPlanBuildDecisionReason.PredictiveExitActive;
            if (candidateKind == CharacterFootPlanAttemptKind.None)
            {
                return currentPlanMatches && plan.HasExecutablePath
                    ? CharacterFootPlanBuildDecisionReason.ActivePlanExecuting
                    : ResolveStepPlanningReason(side, in step, in motionTimeline, false);
            }
            bool usesIncoming = candidateKind == CharacterFootPlanAttemptKind.EventSuccessor;
            if (usesIncoming && !canPrepareEventSuccessor)
                return CharacterFootPlanBuildDecisionReason.AwaitingApproachContact;
            if (usesIncoming && !incomingPlanningCandidate)
                return ResolveStepPlanningReason(side, in incomingStep, in motionTimeline, true);
            if (!usesIncoming && !planningCandidate &&
                candidateKind != CharacterFootPlanAttemptKind.IntentRevision)
            {
                return ResolveStepPlanningReason(side, in step, in motionTimeline, false);
            }
            if (usesIncoming && plan.MatchesAuthoritativeEvent(in incomingStep))
                return CharacterFootPlanBuildDecisionReason.IncomingEventAlreadyOwned;
            if (usesIncoming && !motionWithinCommitTolerance)
                return CharacterFootPlanBuildDecisionReason.MotionOutsideCommitTolerance;
            if (!runtime.CanBeginTransition)
                return CharacterFootPlanBuildDecisionReason.TransitionOccupied;
            if (!origin.IsAvailable || usesIncoming && !origin.HasSupport)
                return CharacterFootPlanBuildDecisionReason.OriginUnavailable;
            if (m_FutureBodyTrajectorySource == null)
                return CharacterFootPlanBuildDecisionReason.FutureBodyUnavailable;
            ulong sourceIdentity = candidateKind switch
            {
                CharacterFootPlanAttemptKind.EventSuccessor => plan.Sequence,
                CharacterFootPlanAttemptKind.IntentRevision => plan.Sequence,
                CharacterFootPlanAttemptKind.CurrentEventReplacement => step.LandingEventIdentity,
                CharacterFootPlanAttemptKind.Initial => plan.Sequence != 0
                    ? plan.Sequence
                    : step.LandingEventIdentity,
                _ => 0
            };
            if (runtime.HasAttemptedPlanBuildRevision(
                    candidateKind,
                    sourceIdentity,
                    motionTimeline.Generation,
                    motionTimeline.AuthorityTick.Value))
            {
                return CharacterFootPlanBuildDecisionReason.BuildRevisionAlreadyAttempted;
            }
            return CharacterFootPlanBuildDecisionReason.EligibleButNotAttempted;
        }

        CharacterFootPlanBuildDecisionReason ResolveStepPlanningReason(
            CharacterFootSide side,
            in AnimationPredictedFootStepSample step,
            in CommittedLocomotionPlanarMotionTimeline motionTimeline,
            bool incoming)
        {
            if (!step.HasConsistentLandingEventIdentity(side))
            {
                return incoming
                    ? CharacterFootPlanBuildDecisionReason.IncomingEventUnavailable
                    : CharacterFootPlanBuildDecisionReason.NoAuthoritativeEvent;
            }
            if (!motionTimeline.IsValid)
                return CharacterFootPlanBuildDecisionReason.MotionTimelineUnavailable;
            if (step.Confidence < m_Settings.MinimumLandingConfidence)
                return CharacterFootPlanBuildDecisionReason.ConfidenceBelowMinimum;
            if (step.ActionStepClock.Phase >= 0.9999f)
                return CharacterFootPlanBuildDecisionReason.StepComplete;
            if (incoming ? !step.IsPreSwing : !(step.IsPreSwing || step.ActionStepClock.IsSwing))
                return CharacterFootPlanBuildDecisionReason.OutsidePlanningWindow;
            return CharacterFootPlanBuildDecisionReason.EligibleButNotAttempted;
        }

        static bool CanPrepareEventSuccessor(CharacterPredictiveFootPlanExecution plan)
        {
            if (plan.State != CharacterPredictiveFootPlanState.Executing)
                return false;
            return IsApproachingContact(plan);
        }

        static bool IsApproachingContact(CharacterPredictiveFootPlanExecution plan)
        {
            plan.EvaluateActionState(
                plan.ActionStepPhase,
                out _,
                out AnimationFootSupportPhase supportPhase,
                out _,
                out _);
            return supportPhase == AnimationFootSupportPhase.ApproachingContact;
        }

        bool IsMotionWithinCommitTolerance(CharacterPredictiveFootPlanExecution plan)
        {
            float error = plan.MotionLandingError;
            return float.IsFinite(error) && error <= ResolveMotionDeviationThreshold(plan);
        }

        bool IsEventSuccessorOriginCompatible(
            CharacterPredictiveFootPlanExecution successor,
            Vector3 committedSole,
            FootPlacementSurface committedSupport,
            Vector3 up)
        {
            FootPlacementSurface support = ResolveSupportAtRoutePoint(
                committedSupport,
                committedSole,
                up);
            if (!support.IsValid)
                return false;
            successor.EvaluateGroundPath(
                0f,
                out Vector3 successorStart,
                out FootPlacementSurface successorStartSupport);
            if (!successorStartSupport.IsValid ||
                successorStartSupport.Identity != support.Identity)
            {
                return false;
            }
            float distance = Vector3.Distance(successorStart, support.Point);
            return float.IsFinite(distance) &&
                   distance <= ResolveMotionDeviationThreshold(successor);
        }

        float ResolveMotionDeviationThreshold(CharacterPredictiveFootPlanExecution plan) =>
            2f * Mathf.Max(
                plan.SoleSupportRadius,
                Mathf.Max(m_Settings.PathSphereRadius, m_Settings.SwingCapsuleRadius));

        static CharacterFootPlanBuildOrigin BuildPlanOrigin(
            in CharacterFootPlanSupportFact source,
            ulong fallbackPlanSequence,
            ulong fallbackLandingEventIdentity,
            Vector3 up)
        {
            if (!source.IsAvailable)
                return default;
            ulong sourcePlanSequence = source.SourcePlanSequence != 0
                ? source.SourcePlanSequence
                : fallbackPlanSequence;
            ulong sourceLandingEventIdentity = source.SourceLandingEventIdentity != 0
                ? source.SourceLandingEventIdentity
                : fallbackLandingEventIdentity;
            return BuildPlanOrigin(
                source.Kind,
                sourcePlanSequence,
                sourceLandingEventIdentity,
                source.Sole,
                source.Support.IsValid ? source.Support.Point : source.Sole,
                source.Support,
                up);
        }

        static CharacterFootPlanBuildOrigin BuildPlanOrigin(
            CharacterFootPlanOriginKind kind,
            ulong sourcePlanSequence,
            ulong sourceLandingEventIdentity,
            Vector3 sole,
            Vector3 fallbackGroundPath,
            FootPlacementSurface sourceSupport,
            Vector3 up)
        {
            FootPlacementSurface support = ResolveSupportAtRoutePoint(
                sourceSupport,
                sole,
                up);
            Vector3 groundPath = support.IsValid ? support.Point : fallbackGroundPath;
            return new CharacterFootPlanBuildOrigin(
                kind,
                sourcePlanSequence,
                sourceLandingEventIdentity,
                sole,
                groundPath,
                support,
                up);
        }

        bool ShouldRequestIntentRevision(
            CharacterFootPlanExecutionState runtime,
            CharacterPredictiveFootPlanExecution plan,
            in AnimationPredictedFootStepSample step,
            in CommittedLocomotionPlanarMotionTimeline motionTimeline,
            double movementPlaybackTime,
            float predictionLeadSeconds,
            float trajectoryCurvatureDegreesPerSecond,
            Vector3 presentedBodyPosition,
            Quaternion rootWorldRotation)
        {
            if (plan.State != CharacterPredictiveFootPlanState.Executing ||
                !step.IsAuthoritative || !motionTimeline.IsValid ||
                step.ActionStepClock.Phase >= 0.9999f)
            {
                return false;
            }
            plan.EvaluateActionState(
                plan.ActionStepPhase,
                out _,
                out AnimationFootSupportPhase supportPhase,
                out _,
                out _);
            if (supportPhase != AnimationFootSupportPhase.Unsupported)
                return false;
            float secondsToApproachContact = Mathf.Max(
                0f,
                (plan.ApproachContactPhase - plan.ActionStepPhase) *
                plan.RootTrajectory.ActionStepDurationSeconds);
            float revisionBlendSeconds = 1f / Mathf.Max(0.0001f, m_TransitionBlendSpeed);
            if (secondsToApproachContact <= revisionBlendSeconds)
                return false;
            float remainingSeconds = Mathf.Max(
                0f,
                (1f - plan.ActionStepPhase) *
                plan.RootTrajectory.ActionStepDurationSeconds);
            float horizonSeconds = predictionLeadSeconds + remainingSeconds;
            if (horizonSeconds <= 0.0001f || m_FutureBodyTrajectorySource == null)
                return false;
            Vector3 up = plan.RootTrajectory.Up;
            Vector3 expectedVelocity = Vector3.ProjectOnPlane(
                plan.RootTrajectory.EvaluatePresentedBodyVelocityAtEventPhase(
                    plan.ActionStepPhase),
                up);
            Vector3 currentVelocity = Vector3.ProjectOnPlane(
                new Vector3(
                    motionTimeline.CurrentVelocityX,
                    0f,
                    motionTimeline.CurrentVelocityZ),
                up);
            float velocityError = (currentVelocity - expectedVelocity).magnitude *
                                  horizonSeconds;
            float curvatureAngle = Mathf.Abs(
                trajectoryCurvatureDegreesPerSecond -
                plan.RootTrajectory.FrozenTrajectoryCurvatureDegreesPerSecond) *
                Mathf.Deg2Rad * horizonSeconds;
            float curvatureLever = Mathf.Max(
                plan.SoleSupportRadius,
                plan.RootTrajectory.EvaluateRemainingPlanarDistance(plan.ActionStepPhase));
            float curvatureError = 2f * curvatureLever * Mathf.Sin(
                Mathf.Min(Mathf.PI, curvatureAngle) * 0.5f);
            float preflightError = Mathf.Max(
                plan.MotionLandingError,
                Mathf.Sqrt(
                    velocityError * velocityError +
                    curvatureError * curvatureError));
            float enterThreshold = ResolveMotionDeviationThreshold(plan);
            if (!float.IsFinite(preflightError) || preflightError <= enterThreshold)
            {
                runtime.ObserveIntentLandingDisplacement(preflightError, enterThreshold);
                return false;
            }
            runtime.ObserveIntentLandingDisplacement(preflightError, enterThreshold);
            if (runtime.HasAttemptedPlanBuildRevision(
                    CharacterFootPlanAttemptKind.IntentRevision,
                    plan.Sequence,
                    motionTimeline.Generation,
                    motionTimeline.AuthorityTick.Value))
                return false;
            float currentSegmentRemainingSeconds = motionTimeline.CurrentSegmentDurationTicks > 0
                ? Mathf.Max(
                    0f,
                    (float)(motionTimeline.CurrentSegmentDurationSeconds - movementPlaybackTime))
                : float.PositiveInfinity;
            var trajectoryRequest = new CharacterFutureBodyTrajectoryRequest(
                m_ActorId,
                horizonSeconds,
                motionTimeline.CurrentVelocityX,
                motionTimeline.CurrentVelocityZ,
                motionTimeline.ContinuationVelocityX,
                motionTimeline.ContinuationVelocityZ,
                currentSegmentRemainingSeconds,
                motionTimeline.HasContinuation,
                trajectoryCurvatureDegreesPerSecond,
                motionTimeline.YawVelocityDegreesPerSecond,
                motionTimeline.MaximumYawVelocityDegreesPerSecond);
            if (!m_FutureBodyTrajectorySource.TryPredict(
                    in trajectoryRequest,
                    out CharacterFutureBodyTrajectory currentTrajectory))
            {
                runtime.ObserveIntentLandingDisplacement(preflightError, enterThreshold);
                return false;
            }
            CharacterFutureBodyTrajectorySample landingSample =
                currentTrajectory.Evaluate(horizonSeconds);
            Vector3 currentLandingPosition = presentedBodyPosition + new Vector3(
                landingSample.RelativePositionX,
                landingSample.RelativePositionY,
                landingSample.RelativePositionZ);
            Vector3 expectedLandingPosition = plan.ProjectedPresentedBodyLanding;
            float linearError = Vector3.ProjectOnPlane(
                    currentLandingPosition - expectedLandingPosition,
                    up)
                .magnitude;
            Quaternion currentLandingRotation = (
                Quaternion.AngleAxis(landingSample.RelativeYawDegrees, up) *
                rootWorldRotation).normalized;
            float angularDifference = Quaternion.Angle(
                plan.ProjectedRootLandingRotation,
                currentLandingRotation) * Mathf.Deg2Rad;
            float angularLever = Mathf.Max(
                plan.SoleSupportRadius,
                plan.RootTrajectory.EvaluateRemainingPlanarDistance(plan.ActionStepPhase));
            float angularError = 2f * angularLever * Mathf.Sin(angularDifference * 0.5f);
            float error = Mathf.Sqrt(
                linearError * linearError +
                angularError * angularError);
            runtime.ObserveIntentLandingDisplacement(error, enterThreshold);
            return float.IsFinite(error) && error > enterThreshold;
        }

        bool CreatePlan(
            CharacterPredictiveFootPlanExecution plan,
            in CharacterFootPlanBuildRequest request,
            out CharacterFootPlanAttemptDiagnostics attempt)
        {
            CharacterFootPlanAttemptKind attemptKind = request.AttemptKind;
            CharacterFootSide side = request.Side;
            AnimationPredictedFootStepSample step = request.Step;
            ulong renderFrame = request.RenderFrame;
            CharacterFootPlanBuildOrigin origin = request.Origin;
            float soleSupportRadius = request.SoleSupportRadius;
            Vector3 rootStart = request.RootStart;
            Quaternion rootStartRotation = request.RootStartRotation;
            Vector3 presentedBodyStartPosition = request.PresentedBodyStartPosition;
            Vector3 committedBodyVelocity = request.CommittedBodyVelocity;
            float trajectoryCurvatureDegreesPerSecond =
                request.TrajectoryCurvatureDegreesPerSecond;
            bool trajectoryCurvatureAvailable = request.TrajectoryCurvatureAvailable;
            CommittedLocomotionPlanarMotionTimeline motionTimeline = request.MotionTimeline;
            double movementPlaybackTime = request.MovementPlaybackTime;
            Vector3 up = request.Up;
            float legLength = request.LegLength;
            if (attemptKind == CharacterFootPlanAttemptKind.None)
                throw new ArgumentOutOfRangeException(nameof(attemptKind));
            if (!origin.IsAvailable)
                throw new ArgumentException("Predictive Foot Plan build origin is unavailable.", nameof(request));
            ulong sequence = AllocatePlanSequence();
            float currentSegmentRemainingSeconds = motionTimeline.CurrentSegmentDurationTicks > 0
                ? Mathf.Max(0f, (float)(motionTimeline.CurrentSegmentDurationSeconds - movementPlaybackTime))
                : float.PositiveInfinity;
            float trajectoryDurationSeconds = Mathf.Max(
                0.0001f,
                Mathf.Max(
                    step.ActionStepClock.TimeToLandingSeconds,
                    step.PredictionLeadSeconds +
                    (1f - step.ActionStepClock.Phase) *
                    step.ActionStepClock.DurationSeconds));
            var trajectoryRequest = new CharacterFutureBodyTrajectoryRequest(
                m_ActorId,
                trajectoryDurationSeconds,
                motionTimeline.CurrentVelocityX,
                motionTimeline.CurrentVelocityZ,
                motionTimeline.ContinuationVelocityX,
                motionTimeline.ContinuationVelocityZ,
                currentSegmentRemainingSeconds,
                motionTimeline.HasContinuation,
                trajectoryCurvatureDegreesPerSecond,
                motionTimeline.YawVelocityDegreesPerSecond,
                motionTimeline.MaximumYawVelocityDegreesPerSecond);
            if (!m_FutureBodyTrajectorySource.TryPredict(
                    in trajectoryRequest,
                    out CharacterFutureBodyTrajectory futureBodyTrajectory))
            {
                attempt = new CharacterFootPlanAttemptDiagnostics(
                    attemptKind,
                    sequence,
                    renderFrame,
                    step.LandingEventIdentity,
                    CharacterPredictiveFootPlanState.Rejected,
                    FootPredictionRejectReason.MotionTimelineUnavailable,
                    FootPlacementGroundEnvelopeRejectReason.None,
                    0,
                    0,
                    0,
                    null,
                    in request);
                return false;
            }
            var rootTrajectory = new CharacterPredictiveFootRootTrajectory(
                rootStart,
                rootStartRotation,
                presentedBodyStartPosition,
                origin.GroundPath,
                origin.Sole,
                committedBodyVelocity,
                trajectoryCurvatureDegreesPerSecond,
                trajectoryCurvatureAvailable,
                in motionTimeline,
                movementPlaybackTime,
                futureBodyTrajectory,
                up,
                in step);
            Vector3 pathStart = origin.GroundPath;
            rootTrajectory.EvaluateEventPhase(1f, out Vector3 rootLanding, out Quaternion rootLandingRotation);
            Vector3 landing = rootTrajectory.EvaluateFootRoute(1f);
            Vector3 predictedHip = rootTrajectory.EvaluateHipRoute(1f);
            FootPredictionRejectReason rejectReason = FootPredictionRejectReason.None;
            CharacterPredictiveFootPlacementQueryResult query = default;
            ResolveVirtualGroundSplitEvent(
                side,
                in step,
                in rootTrajectory,
                out float virtualGroundSplitEventPhase,
                out ulong virtualGroundSplitLandingEventIdentity);
            if (rejectReason == FootPredictionRejectReason.None &&
                !rootTrajectory.CanCoverEventPhase(1f))
            {
                rejectReason = FootPredictionRejectReason.MotionTimelineUnavailable;
            }
            else if (rejectReason == FootPredictionRejectReason.None &&
                (!IsFinite(landing) || !IsFinite(predictedHip) ||
                 !IsFinite(rootLanding) || !IsFinite(rootLandingRotation)))
            {
                rejectReason = FootPredictionRejectReason.NonFinite;
            }
            else if (rejectReason == FootPredictionRejectReason.None)
            {
                query = m_Query.Query(
                    side == CharacterFootSide.Left ? 0 : 1,
                    in step,
                    in rootTrajectory,
                    pathStart,
                    origin.Support,
                    virtualGroundSplitEventPhase,
                    virtualGroundSplitLandingEventIdentity,
                    m_GroundLayerMask,
                    up,
                    soleSupportRadius,
                    legLength * m_Settings.MaximumPredictionReachRatio,
                    out CharacterPredictiveFootRootTrajectory resolvedTrajectory);
                rootTrajectory = resolvedTrajectory;
                rootTrajectory.EvaluateEventPhase(
                    1f,
                    out rootLanding,
                    out rootLandingRotation);
                landing = rootTrajectory.EvaluateFootRoute(1f);
                predictedHip = rootTrajectory.EvaluateHipRoute(1f);
                if (!query.HasFutureLandingSupport)
                    rejectReason = ResolveFutureLandingRejectReason(
                        query.GroundEnvelope.RejectReason);
                else
                {
                    landing = query.FutureLandingSupport.Point;
                    query.BodySupportPath.Evaluate(
                        in rootTrajectory,
                        1f,
                        out _,
                        out predictedHip);
                    if (!CharacterPredictiveFootPlacementPlan.HasValidGroundPathRateRange(
                            in rootTrajectory,
                            in query))
                    {
                        rejectReason = FootPredictionRejectReason.FootRateInvalid;
                    }
                }
            }
            if (rejectReason == FootPredictionRejectReason.None)
            {
                plan.Commit(
                    sequence,
                    renderFrame,
                    in step,
                    pathStart,
                    landing,
                    in rootTrajectory,
                    predictedHip,
                    in query);
            }
            else
            {
                plan.Reject(
                    sequence,
                    renderFrame,
                    in step,
                    pathStart,
                    landing,
                    in rootTrajectory,
                    predictedHip,
                    rejectReason,
                    in query);
            }
            attempt = new CharacterFootPlanAttemptDiagnostics(attemptKind, plan, in request);
            return plan.HasExecutablePath;
        }

        static void ResolveVirtualGroundSplitEvent(
            CharacterFootSide side,
            in AnimationPredictedFootStepSample step,
            in CharacterPredictiveFootRootTrajectory rootTrajectory,
            out float eventPhase,
            out ulong landingEventIdentity)
        {
            eventPhase = 0f;
            landingEventIdentity = 0;
            if (side != CharacterFootSide.Left && side != CharacterFootSide.Right)
                throw new ArgumentOutOfRangeException(nameof(side));
            if (!step.HasOpposingLandingEvent)
                return;
            float leadSeconds = step.PredictionLeadSeconds;
            float ownLandingSeconds = step.ActionStepClock.TimeToLandingSeconds - leadSeconds;
            float opposingLandingSeconds = step.OpposingLandingDelaySeconds - leadSeconds;
            float durationSeconds = step.ActionStepClock.DurationSeconds;
            if (!float.IsFinite(ownLandingSeconds) || !float.IsFinite(opposingLandingSeconds) ||
                !float.IsFinite(durationSeconds) || durationSeconds <= 0f ||
                opposingLandingSeconds <= 0.0001f ||
                opposingLandingSeconds >= ownLandingSeconds - 0.0001f)
            {
                return;
            }
            eventPhase = step.ActionStepClock.Phase + opposingLandingSeconds / durationSeconds;
            if (eventPhase <= rootTrajectory.PathStartPhase + 0.0001f || eventPhase >= 0.9999f)
            {
                eventPhase = 0f;
                return;
            }
            landingEventIdentity = step.OpposingLandingEventIdentity;
        }

        static FixedList512Bytes<CharacterPredictiveFootPathSampleDiagnostics> BuildPathDiagnostics(
            CharacterPredictiveFootPlanExecution plan)
        {
            var result = new FixedList512Bytes<CharacterPredictiveFootPathSampleDiagnostics>();
            if (plan.GroundEnvelopeSegmentCount <= 0)
                return result;
            FootPlacementGroundEnvelopeSegment first = plan.GetPathSegment(0);
            float firstPhase = Mathf.Lerp(
                plan.RootTrajectory.PathStartPhase,
                1f,
                first.StartFraction);
            plan.EvaluateBodyPath(firstPhase, out Vector3 firstRoot, out Vector3 firstHip);
            result.Add(new CharacterPredictiveFootPathSampleDiagnostics(
                first.StartFraction,
                first.EdgeStart,
                first.StartSurface.IsValid ? first.StartSurface.Normal : Vector3.up,
                first.StartSurface.Identity,
                firstRoot,
                firstHip));
            int outputCount = Mathf.Min(plan.GroundEnvelopeSegmentCount, Mathf.Min(7, result.Capacity - 1));
            for (int i = 0; i < outputCount; i++)
            {
                int segmentIndex = Mathf.Min(
                    plan.GroundEnvelopeSegmentCount - 1,
                    Mathf.RoundToInt(
                        (i + 1f) * plan.GroundEnvelopeSegmentCount / outputCount) - 1);
                FootPlacementGroundEnvelopeSegment segment = plan.GetPathSegment(segmentIndex);
                float phase = Mathf.Lerp(
                    plan.RootTrajectory.PathStartPhase,
                    1f,
                    segment.EndFraction);
                plan.EvaluateBodyPath(phase, out Vector3 root, out Vector3 hip);
                result.Add(new CharacterPredictiveFootPathSampleDiagnostics(
                    segment.EndFraction,
                    segment.EdgeEnd,
                    segment.EndSurface.IsValid ? segment.EndSurface.Normal : Vector3.up,
                    segment.EndSurface.Identity,
                    root,
                    hip));
            }
            return result;
        }

        static FixedList128Bytes<Vector3> BuildPlannedFootRouteDiagnostics(
            CharacterPredictiveFootPlanExecution plan)
        {
            var result = new FixedList128Bytes<Vector3>();
            if (!plan.OwnsEvent)
                return result;
            const int diagnosticSampleCount = 7;
            int count = Mathf.Min(diagnosticSampleCount, plan.FrozenWorldFootRoute.Length);
            for (int i = 0; i < count; i++)
            {
                int sourceIndex = count > 1
                    ? Mathf.RoundToInt(i * (plan.FrozenWorldFootRoute.Length - 1f) / (count - 1f))
                    : 0;
                result.Add(plan.GetPlannedFootRouteSample(sourceIndex));
            }
            return result;
        }

        static FootPredictionRejectReason ResolveRejectReason(
            CharacterPredictiveFootPlanExecution plan,
            in AnimationPredictedFootStepSample step,
            bool landingEventIdentityValid,
            bool rewritten,
            float predictionReachRatio,
            bool stanceOwnsFoot)
        {
            if (plan.State == CharacterPredictiveFootPlanState.Rejected)
                return plan.CreationRejectReason;
            if (plan.State == CharacterPredictiveFootPlanState.Planned)
                return FootPredictionRejectReason.PlanWaitingForRelease;
            if (plan.State == CharacterPredictiveFootPlanState.Executing)
            {
                if (!float.IsFinite(predictionReachRatio))
                    return FootPredictionRejectReason.NonFinite;
                if (rewritten)
                    return FootPredictionRejectReason.None;
                return stanceOwnsFoot
                    ? FootPredictionRejectReason.StanceConstraintOwnsFoot
                    : FootPredictionRejectReason.NonFinite;
            }
            if (step.IsAuthoritative && !landingEventIdentityValid)
                return FootPredictionRejectReason.LandingEventIdentityInvalid;
            if (!step.IsAuthoritative)
                return FootPredictionRejectReason.LandingEventUnavailable;
            if (step.Confidence <= 0f)
                return FootPredictionRejectReason.LandingConfidenceInsufficient;
            if (!step.IsPreSwing && !step.ActionStepClock.IsSwing)
                return FootPredictionRejectReason.LandingEventNotPreSwing;
            return FootPredictionRejectReason.NoCommittedPlan;
        }

        static FootPredictionRejectReason ResolveFutureLandingRejectReason(
            FootPlacementGroundEnvelopeRejectReason reason)
        {
            return reason switch
            {
                FootPlacementGroundEnvelopeRejectReason.NoCandidate =>
                    FootPredictionRejectReason.FutureLandingNoCandidate,
                FootPlacementGroundEnvelopeRejectReason.HeightDiscontinuity =>
                    FootPredictionRejectReason.FutureLandingHeightDiscontinuity,
                FootPlacementGroundEnvelopeRejectReason.EdgeGap =>
                    FootPredictionRejectReason.FutureLandingEdgeGap,
                FootPlacementGroundEnvelopeRejectReason.ReachExceeded =>
                    FootPredictionRejectReason.FutureLandingReachExceeded,
                FootPlacementGroundEnvelopeRejectReason.StepExceeded =>
                    FootPredictionRejectReason.FutureLandingStepExceeded,
                FootPlacementGroundEnvelopeRejectReason.UnsupportedCenter =>
                    FootPredictionRejectReason.FutureLandingUnsupportedCenter,
                FootPlacementGroundEnvelopeRejectReason.SlopeExceeded =>
                    FootPredictionRejectReason.FutureLandingSlopeExceeded,
                FootPlacementGroundEnvelopeRejectReason.InvalidCandidate =>
                    FootPredictionRejectReason.FutureLandingInvalidCandidate,
                _ => FootPredictionRejectReason.NoFutureLanding
            };
        }

        static void ResolveCurrentActionState(
            CharacterPredictiveFootPlanExecution plan,
            out AnimationFootConstraintMode constraintMode,
            out AnimationFootSupportPhase supportPhase,
            out AnimationFootOrientationPolicy orientationPolicy,
            out AnimationBodyRotationPivotMode bodyPivotMode)
        {
            plan.EvaluateActionState(
                out constraintMode,
                out supportPhase,
                out orientationPolicy,
                out bodyPivotMode);
        }

        static void RequireAuthoritativeConstraint(
            in AnimationPredictedFootStepSample step,
            AnimationFootConstraintMode constraintMode,
            AnimationFootSupportPhase supportPhase,
            AnimationBodyRotationPivotMode bodyPivotMode)
        {
            CharacterPredictiveFootPlacementPlan.RequireAuthoritativeConstraint(
                step.ActionStepClock.IsSwing,
                step.ActionStepClock.IsPreSwing,
                constraintMode,
                supportPhase,
                bodyPivotMode);
        }

        static bool AllowsStanceHandoff(CharacterPredictiveFootPlanExecution plan)
        {
            if (plan.State != CharacterPredictiveFootPlanState.Executing)
                return true;
            ResolveCurrentActionState(
                plan,
                out _,
                out AnimationFootSupportPhase supportPhase,
                out _,
                out _);
            return supportPhase != AnimationFootSupportPhase.Unsupported;
        }

        static CharacterPredictiveFootPlanEndReason ResolveReplacementEndReason(
            CharacterPredictiveFootPlanExecution plan) =>
            plan.ActionStepPhase >= 0.9999f
                ? CharacterPredictiveFootPlanEndReason.ActionCompleted
                : CharacterPredictiveFootPlanEndReason.EventReplaced;

        ulong AllocatePlanSequence()
        {
            ulong value = m_NextPlanSequence++;
            if (m_NextPlanSequence == 0)
                m_NextPlanSequence = 1;
            return value;
        }

        void RequireValidInput(
            in CharacterFootPlacementFrameInput frame,
            in CharacterFullBodyIkGoalSetHeader ownerHeader,
            CharacterFullBodyIkGoal currentPelvis,
            CharacterFullBodyIkGoal currentLeft,
            CharacterFullBodyIkGoal currentRight,
            in CharacterFootGroundingDiagnostics currentDiagnostics)
        {
            if (frame.ActorId != m_ActorId ||
                 !frame.Body.IsValid ||
                 !ownerHeader.IsValid ||
                 ownerHeader.Availability != CharacterFullBodyIkGoalSetAvailability.Ready ||
                 ownerHeader.FrameSequence != frame.RenderFrame ||
                 ownerHeader.CompletionIdentity != frame.CompletionIdentity ||
                 !ownerHeader.RigId.Equals(m_RigId) ||
                 !ownerHeader.RigRevision.Equals(m_RigRevision) ||
                 ownerHeader.GoalCount != 3 ||
                 !currentDiagnostics.IsCompleted ||
                 currentDiagnostics.FrameSequence != frame.RenderFrame ||
                 currentDiagnostics.CompletionIdentity != frame.CompletionIdentity ||
                 !IsCurrentGoal(currentPelvis, CharacterFullBodyIkEffectorSlot.PelvisPreSolveTranslation, CharacterFullBodyIkGoalApplication.PelvisPreSolveTranslation, 0) ||
                 !IsCurrentGoal(currentLeft, CharacterFullBodyIkEffectorSlot.LeftFoot, CharacterFullBodyIkGoalApplication.FootPlacementEffectorTarget, 1) ||
                 !IsCurrentGoal(currentRight, CharacterFullBodyIkEffectorSlot.RightFoot, CharacterFullBodyIkGoalApplication.FootPlacementEffectorTarget, 2) ||
                 !SameGoal(currentLeft, currentDiagnostics.Left.Goal) ||
                 !SameGoal(currentRight, currentDiagnostics.Right.Goal))
            {
                throw new ArgumentException("Predictive Foot Placement input is invalid.");
            }
        }

        static bool IsCurrentGoal(
            CharacterFullBodyIkGoal goal,
            CharacterFullBodyIkEffectorSlot slot,
            CharacterFullBodyIkGoalApplication application,
            int metadataIndex) =>
            goal.IsValid &&
            goal.Slot == slot &&
            goal.Application == application &&
            goal.SourceKind == CharacterFullBodyIkGoalSourceKind.FootGrounding &&
            goal.DiagnosticMetadataIndex == metadataIndex;

        static bool SameGoal(CharacterFullBodyIkGoal left, CharacterFullBodyIkGoal right) =>
            left.Slot == right.Slot &&
            left.ComponentPosition == right.ComponentPosition &&
            left.ComponentRotation == right.ComponentRotation &&
            left.PositionWeight == right.PositionWeight &&
            left.RotationWeight == right.RotationWeight &&
            left.Application == right.Application &&
            left.SourceKind == right.SourceKind &&
            left.DiagnosticMetadataIndex == right.DiagnosticMetadataIndex;

        static FootPlacementSurface ResolveSupportAtRoutePoint(
            FootPlacementSurface surface,
            Vector3 routePoint,
            Vector3 componentUp)
        {
            if (!surface.IsValid)
                return default;
            Vector3 normal = surface.Normal.normalized;
            Vector3 up = componentUp.normalized;
            float denominator = Vector3.Dot(up, normal);
            if (!float.IsFinite(denominator) || denominator <= 0.0001f)
                return default;
            Vector3 point = routePoint + up * (
                Vector3.Dot(surface.Point - routePoint, normal) / denominator);
            return IsFinite(point)
                ? new FootPlacementSurface(surface.Collider, point, normal)
                : default;
        }

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        static bool IsFinite(Quaternion value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) &&
            float.IsFinite(value.z) && float.IsFinite(value.w) &&
            Quaternion.Dot(value, value) > 0f;

    }
}
