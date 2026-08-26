using System;
using ThirdPersonCharacter.Pipeline.Animation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public enum CharacterFootConstraintState : byte
    {
        Swing = 0,
        Landing = 1,
        Locked = 2,
        Releasing = 3,
        UnlockedSupport = 4
    }

    public enum CharacterFootLockResponse : byte
    {
        None = 0,
        FullAnchor = 1,
        Sliding = 2
    }

    public enum CharacterFootSupportEligibility : byte
    {
        None = 0,
        RetainOnly = 1,
        AcquireAndRetain = 2
    }

    internal enum CharacterFootResolvedOutcome : byte
    {
        Ready = 1
    }

    internal readonly struct CharacterFootContactReference
    {
        internal CharacterFootContactReference(
            ulong eventIdentity,
            Vector3 point)
        {
            EventIdentity = eventIdentity;
            Point = point;
            IsAvailable = eventIdentity != 0;
        }

        internal bool IsAvailable { get; }
        internal ulong EventIdentity { get; }
        internal Vector3 Point { get; }
    }

    internal readonly struct CharacterFootPelvisReachReference
    {
        internal CharacterFootPelvisReachReference(
            ulong eventIdentity,
            Vector3 point)
        {
            EventIdentity = eventIdentity;
            Point = point;
            IsAvailable = eventIdentity != 0;
        }

        internal bool IsAvailable { get; }
        internal ulong EventIdentity { get; }
        internal Vector3 Point { get; }
    }

    internal readonly struct CharacterResolvedFootResult
    {
        internal CharacterResolvedFootResult(
            ulong frameSequence,
            ulong completionIdentity,
            FixedString64Bytes rigId,
            FixedString64Bytes rigRevision,
            CharacterFootSide side,
            Vector3 finalSole,
            Vector3 finalAnkle,
            Vector3 effectiveCorrection,
            float goalWeight,
            in CharacterFootContactReference contactReference,
            float contactOwnership,
            CharacterFootSupportEligibility supportEligibility,
            float supportWeight,
            float supportIntentWeight,
            float supportHorizontalError,
            ulong supportEventIdentity,
            in CharacterFootPelvisReachReference pelvisReachReference)
        {
            FrameSequence = frameSequence;
            CompletionIdentity = completionIdentity;
            RigId = rigId;
            RigRevision = rigRevision;
            Side = side;
            FinalSole = finalSole;
            FinalAnkle = finalAnkle;
            EffectiveCorrection = effectiveCorrection;
            GoalWeight = goalWeight;
            ContactReference = contactReference;
            ContactOwnership = contactOwnership;
            SupportEligibility = supportEligibility;
            SupportWeight = supportWeight;
            SupportIntentWeight = supportIntentWeight;
            SupportHorizontalError = supportHorizontalError;
            SupportEventIdentity = supportEventIdentity;
            PelvisReachReference = pelvisReachReference;
            Outcome = CharacterFootResolvedOutcome.Ready;
        }

        internal ulong FrameSequence { get; }
        internal ulong CompletionIdentity { get; }
        internal FixedString64Bytes RigId { get; }
        internal FixedString64Bytes RigRevision { get; }
        internal CharacterFootSide Side { get; }
        internal Vector3 FinalSole { get; }
        internal Vector3 FinalAnkle { get; }
        internal Vector3 EffectiveCorrection { get; }
        internal float GoalWeight { get; }
        internal CharacterFootContactReference ContactReference { get; }
        internal float ContactOwnership { get; }
        internal CharacterFootSupportEligibility SupportEligibility { get; }
        internal float SupportWeight { get; }
        internal float SupportIntentWeight { get; }
        internal float SupportHorizontalError { get; }
        internal ulong SupportEventIdentity { get; }
        internal CharacterFootPelvisReachReference PelvisReachReference { get; }
        internal CharacterFootResolvedOutcome Outcome { get; }
    }

    internal readonly struct CharacterResolvedFootPair
    {
        internal CharacterResolvedFootPair(
            in CharacterResolvedFootResult left,
            in CharacterResolvedFootResult right)
        {
            if (left.Outcome != CharacterFootResolvedOutcome.Ready ||
                right.Outcome != CharacterFootResolvedOutcome.Ready ||
                left.FrameSequence == 0 ||
                left.FrameSequence != right.FrameSequence ||
                left.CompletionIdentity == 0 ||
                left.CompletionIdentity != right.CompletionIdentity ||
                !left.RigId.Equals(right.RigId) ||
                !left.RigRevision.Equals(right.RigRevision))
            {
                throw new InvalidOperationException(
                    "Resolved Foot Pair lineage is inconsistent.");
            }
            FrameSequence = left.FrameSequence;
            CompletionIdentity = left.CompletionIdentity;
            RigId = left.RigId;
            RigRevision = left.RigRevision;
            Left = left;
            Right = right;
        }

        internal ulong FrameSequence { get; }
        internal ulong CompletionIdentity { get; }
        internal FixedString64Bytes RigId { get; }
        internal FixedString64Bytes RigRevision { get; }
        internal CharacterResolvedFootResult Left { get; }
        internal CharacterResolvedFootResult Right { get; }
    }

    public enum CharacterFootSwingMotionState : byte
    {
        None = 0,
        Rejected = 1,
        Accepted = 2
    }

    public enum CharacterFootSwingMotionRejectReason : byte
    {
        None = 0,
        StepUnavailable = 1,
        StepNotSwing = 2,
        InvalidComponentUp = 3,
        InvalidWeight = 4,
        GroundPathRejected = 5,
        UnreachableEdge = 6,
        LandingEventMismatch = 7,
        InvalidEnvelope = 8,
        EnvelopeEndpointMismatch = 9,
        EnvelopeUnordered = 10,
        DegeneratePath = 11,
        EnvelopeSampleUnavailable = 12,
        NegativeVerticalCorrection = 13,
        InvalidSwingPhase = 14,
        UnselectedSwing = 15
    }

    internal readonly struct CharacterFootSwingPathReference
    {
        internal CharacterFootSwingPathReference(
            ulong landingEventIdentity,
            Vector3 landingPoint)
        {
            if (landingEventIdentity == 0 ||
                !float.IsFinite(landingPoint.x) ||
                !float.IsFinite(landingPoint.y) ||
                !float.IsFinite(landingPoint.z))
            {
                throw new ArgumentException(
                    "Swing Path reference is invalid.");
            }
            LandingEventIdentity = landingEventIdentity;
            LandingPoint = landingPoint;
            IsAvailable = true;
        }

        internal bool IsAvailable { get; }
        internal ulong LandingEventIdentity { get; }
        internal Vector3 LandingPoint { get; }
    }

    internal readonly struct CharacterFootSwingMotionResult
    {
        internal CharacterFootSwingMotionResult(
            CharacterFootSwingMotionState state,
            CharacterFootSwingMotionRejectReason rejectReason,
            ulong landingEventIdentity,
            ulong groundPathInputIdentity,
            CharacterFootSwingPathReference swingPathReference,
            Vector3 originalSole,
            Vector3 originalAnkle,
            float distance,
            float progress,
            Vector3 baselineSample,
            Vector3 envelopeSample,
            float verticalCorrection,
            float landingPredictionError,
            float landingConstraintWeight,
            Vector3 correctedSole,
            Vector3 correctedAnkle,
            float positionWeight,
            float rotationWeight,
            CharacterFootConstraintState constraintState = CharacterFootConstraintState.Swing,
            CharacterFootLockResponse lockResponse = CharacterFootLockResponse.None,
            float supportHorizontalError = 0f,
            float contactOwnership = 0f,
            float supportWeight = 0f,
            Vector3 supportContactAnchor = default,
            float plantConfidence = 0f,
            Vector3 desiredCorrection = default,
            bool contactPlaneAvailable = false,
            int contactSurfaceIdentity = 0,
            Vector3 contactPlaneNormal = default,
            CharacterFootPathContinuityFact pathContinuity = default)
        {
            State = state;
            RejectReason = rejectReason;
            LandingEventIdentity = landingEventIdentity;
            GroundPathInputIdentity = groundPathInputIdentity;
            SwingPathReference = swingPathReference;
            OriginalSole = originalSole;
            OriginalAnkle = originalAnkle;
            Distance = distance;
            Progress = progress;
            BaselineSample = baselineSample;
            EnvelopeSample = envelopeSample;
            VerticalCorrection = verticalCorrection;
            LandingPredictionError = landingPredictionError;
            LandingConstraintWeight = landingConstraintWeight;
            CorrectedSole = correctedSole;
            CorrectedAnkle = correctedAnkle;
            PositionWeight = positionWeight;
            RotationWeight = rotationWeight;
            ConstraintState = constraintState;
            LockResponse = lockResponse;
            SupportHorizontalError = supportHorizontalError;
            ContactOwnership = contactOwnership;
            SupportWeight = supportWeight;
            SupportContactAnchor = supportContactAnchor;
            PlantConfidence = plantConfidence;
            DesiredCorrection = desiredCorrection;
            ContactPlaneAvailable = contactPlaneAvailable;
            ContactSurfaceIdentity = contactSurfaceIdentity;
            ContactPlaneNormal = contactPlaneNormal;
            PathContinuity = pathContinuity;
        }

        public CharacterFootSwingMotionState State { get; }
        public CharacterFootSwingMotionRejectReason RejectReason { get; }
        public ulong LandingEventIdentity { get; }
        public ulong GroundPathInputIdentity { get; }
        internal CharacterFootSwingPathReference SwingPathReference { get; }
        public Vector3 OriginalSole { get; }
        public Vector3 OriginalAnkle { get; }
        public float Distance { get; }
        public float Progress { get; }
        public Vector3 BaselineSample { get; }
        public Vector3 EnvelopeSample { get; }
        public float VerticalCorrection { get; }
        public float LandingPredictionError { get; }
        public float LandingConstraintWeight { get; }
        public Vector3 CorrectedSole { get; }
        public Vector3 CorrectedAnkle { get; }
        public float PositionWeight { get; }
        public float RotationWeight { get; }
        public CharacterFootConstraintState ConstraintState { get; }
        public CharacterFootLockResponse LockResponse { get; }
        public float SupportHorizontalError { get; }
        public float ContactOwnership { get; }
        public float SupportWeight { get; }
        public Vector3 SupportContactAnchor { get; }
        public float PlantConfidence { get; }
        public Vector3 DesiredCorrection { get; }
        public bool ContactPlaneAvailable { get; }
        public int ContactSurfaceIdentity { get; }
        public Vector3 ContactPlaneNormal { get; }
        internal CharacterFootPathContinuityFact PathContinuity { get; }
        public bool Accepted => State == CharacterFootSwingMotionState.Accepted;

        internal CharacterFootSwingMotionResult WithPlantConfidence(
            float plantConfidence) =>
            new CharacterFootSwingMotionResult(
                State,
                RejectReason,
                LandingEventIdentity,
                GroundPathInputIdentity,
                SwingPathReference,
                OriginalSole,
                OriginalAnkle,
                Distance,
                Progress,
                BaselineSample,
                EnvelopeSample,
                VerticalCorrection,
                LandingPredictionError,
                LandingConstraintWeight,
                CorrectedSole,
                CorrectedAnkle,
                PositionWeight,
                RotationWeight,
                ConstraintState,
                LockResponse,
                SupportHorizontalError,
                ContactOwnership,
                SupportWeight,
                SupportContactAnchor,
                plantConfidence,
                DesiredCorrection,
                ContactPlaneAvailable,
                ContactSurfaceIdentity,
                ContactPlaneNormal,
                PathContinuity);
    }

    public readonly struct CharacterFootSwingMotionDiagnostics
    {
        internal CharacterFootSwingMotionDiagnostics(
            in CharacterFootSwingMotionResult result)
        {
            State = result.State;
            RejectReason = result.RejectReason;
            LandingEventIdentity = result.LandingEventIdentity;
            GroundPathInputIdentity = result.GroundPathInputIdentity;
            OriginalSole = result.OriginalSole;
            OriginalAnkle = result.OriginalAnkle;
            Distance = result.Distance;
            Progress = result.Progress;
            BaselineSample = result.BaselineSample;
            EnvelopeSample = result.EnvelopeSample;
            VerticalCorrection = result.VerticalCorrection;
            LandingPredictionError = result.LandingPredictionError;
            LandingConstraintWeight = result.LandingConstraintWeight;
            CorrectedSole = result.CorrectedSole;
            CorrectedAnkle = result.CorrectedAnkle;
            PositionWeight = result.PositionWeight;
            RotationWeight = result.RotationWeight;
            ConstraintState = result.ConstraintState;
            LockResponse = result.LockResponse;
            SupportHorizontalError = result.SupportHorizontalError;
            ContactOwnership = result.ContactOwnership;
            SupportWeight = result.SupportWeight;
            SupportContactAnchor = result.SupportContactAnchor;
            PlantConfidence = result.PlantConfidence;
            DesiredCorrection = result.DesiredCorrection;
            ContactPlaneAvailable = result.ContactPlaneAvailable;
            ContactSurfaceIdentity = result.ContactSurfaceIdentity;
            ContactPlaneNormal = result.ContactPlaneNormal;
            CharacterFootPathContinuityFact path = result.PathContinuity;
            PathContinuityEvaluated = path.Evaluated;
            PathRevisionReason = path.RevisionReason.ToString();
            PathResidualRebuilt = path.ResidualRebuilt;
            PathAvailableBefore = path.PathAvailableBefore;
            PathAvailableAfter = path.PathAvailableAfter;
            PathPreviousLandingEventIdentity = path.PreviousLandingEventIdentity;
            PathCurrentLandingEventIdentity = path.CurrentLandingEventIdentity;
            PathPreviousTargetCorrection = path.PreviousTargetCorrection;
            PathCurrentTargetCorrection = path.CurrentTargetCorrection;
            PathLandingPointDelta = path.LandingPointDelta;
            PathTargetDelta = path.TargetDelta;
            SwingResidualBeforeRevision = path.ResidualBeforeRevision;
            SwingResidualBeforeDecay = path.ResidualBeforeDecay;
            SwingResidualAfterDecay = path.ResidualAfterDecay;
            ResidualOutputCorrection = path.ResidualOutputCorrection;
            LandingUpdateDistance = path.LandingUpdateDistance;
            ResidualTimeToLandingSeconds = path.TimeToLandingSeconds;
            ResidualBaseHalfLifeSeconds = path.BaseHalfLifeSeconds;
            ResidualDeadlineHalfLifeAvailable = path.DeadlineHalfLifeAvailable;
            ResidualDeadlineHalfLifeSeconds = path.DeadlineHalfLifeSeconds;
            ResidualAppliedHalfLifeSeconds = path.AppliedHalfLifeSeconds;
            ConstraintStateBefore = path.StateBefore;
            LockResponseBefore = path.LockResponseBefore;
            OutputStagesAvailable = path.OutputStagesAvailable;
            ReleasingCompletedToSwing = path.ReleasingCompletedToSwing;
            EnvelopeAvailable = path.EnvelopeAvailable;
            CorrectionBeforeSafetyFloor = path.CorrectionBeforeSafetyFloor;
            GroundEnvelopeSafetyCorrection =
                path.GroundEnvelopeSafetyCorrection;
            SafetyFloorOutputCorrection = path.SafetyFloorOutputCorrection;
            FinalEffectiveCorrection = path.FinalEffectiveCorrection;
            SafetyFloorClamped = path.SafetyFloorClamped;
            SafetyFloorClampMeters = path.SafetyFloorClampMeters;
            EnvelopeClearanceBeforeMeters = path.EnvelopeClearanceBeforeMeters;
            EnvelopeClearanceAfterMeters = path.EnvelopeClearanceAfterMeters;
        }

        public CharacterFootSwingMotionState State { get; }
        public CharacterFootSwingMotionRejectReason RejectReason { get; }
        public ulong LandingEventIdentity { get; }
        public ulong GroundPathInputIdentity { get; }
        public Vector3 OriginalSole { get; }
        public Vector3 OriginalAnkle { get; }
        public float Distance { get; }
        public float Progress { get; }
        public Vector3 BaselineSample { get; }
        public Vector3 EnvelopeSample { get; }
        public float VerticalCorrection { get; }
        public float LandingPredictionError { get; }
        public float LandingConstraintWeight { get; }
        public Vector3 CorrectedSole { get; }
        public Vector3 CorrectedAnkle { get; }
        public float PositionWeight { get; }
        public float RotationWeight { get; }
        public CharacterFootConstraintState ConstraintState { get; }
        public CharacterFootLockResponse LockResponse { get; }
        public float SupportHorizontalError { get; }
        public float ContactOwnership { get; }
        public float SupportWeight { get; }
        public Vector3 SupportContactAnchor { get; }
        public float PlantConfidence { get; }
        public Vector3 DesiredCorrection { get; }
        public bool ContactPlaneAvailable { get; }
        public int ContactSurfaceIdentity { get; }
        public Vector3 ContactPlaneNormal { get; }
        public bool PathContinuityEvaluated { get; }
        public string PathRevisionReason { get; }
        public bool PathResidualRebuilt { get; }
        public bool PathAvailableBefore { get; }
        public bool PathAvailableAfter { get; }
        public ulong PathPreviousLandingEventIdentity { get; }
        public ulong PathCurrentLandingEventIdentity { get; }
        public Vector3 PathPreviousTargetCorrection { get; }
        public Vector3 PathCurrentTargetCorrection { get; }
        public float PathLandingPointDelta { get; }
        public float PathTargetDelta { get; }
        public Vector3 SwingResidualBeforeRevision { get; }
        public Vector3 SwingResidualBeforeDecay { get; }
        public Vector3 SwingResidualAfterDecay { get; }
        public Vector3 ResidualOutputCorrection { get; }
        public float LandingUpdateDistance { get; }
        public float ResidualTimeToLandingSeconds { get; }
        public float ResidualBaseHalfLifeSeconds { get; }
        public bool ResidualDeadlineHalfLifeAvailable { get; }
        public float ResidualDeadlineHalfLifeSeconds { get; }
        public float ResidualAppliedHalfLifeSeconds { get; }
        public CharacterFootConstraintState ConstraintStateBefore { get; }
        public CharacterFootLockResponse LockResponseBefore { get; }
        public bool OutputStagesAvailable { get; }
        public bool ReleasingCompletedToSwing { get; }
        public bool EnvelopeAvailable { get; }
        public Vector3 CorrectionBeforeSafetyFloor { get; }
        public Vector3 GroundEnvelopeSafetyCorrection { get; }
        public Vector3 SafetyFloorOutputCorrection { get; }
        public Vector3 FinalEffectiveCorrection { get; }
        public bool SafetyFloorClamped { get; }
        public float SafetyFloorClampMeters { get; }
        public float EnvelopeClearanceBeforeMeters { get; }
        public float EnvelopeClearanceAfterMeters { get; }
        public bool Accepted => State == CharacterFootSwingMotionState.Accepted;
    }

    internal static class CharacterFootSwingMotionBuilder
    {
        const float GeometryEpsilon = 0.0001f;
        const float EndpointTolerance = 0.005f;

        internal static CharacterFootSwingMotionResult Build(
            CharacterFootPlacementAnimatedFootPose animatedFoot,
            in AnimationBiomechanicalStepHeader step,
            float footPlacementWeight,
            Vector3 componentUp,
            in CharacterFootGroundPathResult groundPath,
            float landingPredictionError,
            float landingConstraintWeight)
        {
            Vector3 originalSole = (animatedFoot.HeelPosition + animatedFoot.ToePosition) * 0.5f;
            Vector3 originalAnkle = animatedFoot.AnklePosition;
            ulong landingEventIdentity = step.IsValid ? step.LandingEventIdentity : 0;
            if (!step.IsValid || !step.IsAuthoritative)
                return Rejected(
                    CharacterFootSwingMotionRejectReason.StepUnavailable,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            if (!step.IsSwing)
                return Rejected(
                    CharacterFootSwingMotionRejectReason.StepNotSwing,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            if (!step.HasConsistentLandingEventIdentity)
                return Rejected(
                    CharacterFootSwingMotionRejectReason.LandingEventMismatch,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            return BuildForSwing(
                animatedFoot,
                in step,
                landingEventIdentity,
                footPlacementWeight,
                componentUp,
                in groundPath,
                landingPredictionError,
                landingConstraintWeight);
        }

        internal static CharacterFootSwingMotionResult BuildForSwing(
            CharacterFootPlacementAnimatedFootPose animatedFoot,
            in AnimationBiomechanicalStepHeader step,
            ulong landingEventIdentity,
            float footPlacementWeight,
            Vector3 componentUp,
            in CharacterFootGroundPathResult groundPath,
            float landingPredictionError,
            float landingConstraintWeight)
        {
            Vector3 originalSole = (animatedFoot.HeelPosition + animatedFoot.ToePosition) * 0.5f;
            Vector3 originalAnkle = animatedFoot.AnklePosition;
            if (!Finite(componentUp) || componentUp.sqrMagnitude <= GeometryEpsilon)
                return Rejected(
                    CharacterFootSwingMotionRejectReason.InvalidComponentUp,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            if (!float.IsFinite(footPlacementWeight) || footPlacementWeight < 0f || footPlacementWeight > 1f)
                return Rejected(
                    CharacterFootSwingMotionRejectReason.InvalidWeight,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            if (!float.IsFinite(landingPredictionError) || landingPredictionError < 0f ||
                !float.IsFinite(landingConstraintWeight) ||
                landingConstraintWeight < 0f || landingConstraintWeight > 1f)
                return Rejected(
                    CharacterFootSwingMotionRejectReason.InvalidWeight,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            if (!TryResolveSwingPhaseWeight(in step, out float trajectoryProgress))
                return Rejected(
                    CharacterFootSwingMotionRejectReason.InvalidSwingPhase,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            if (!groundPath.Accepted)
                return Rejected(
                    groundPath.RejectReason == CharacterFootGroundPathRejectReason.UnreachableEdge
                        ? CharacterFootSwingMotionRejectReason.UnreachableEdge
                        : CharacterFootSwingMotionRejectReason.GroundPathRejected,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            if (groundPath.NextSwingLandingEventIdentity != landingEventIdentity)
                return Rejected(
                    CharacterFootSwingMotionRejectReason.LandingEventMismatch,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            if (groundPath.EnvelopeVertexCount < 2 ||
                !Finite(groundPath.LastLanding) ||
                !Finite(groundPath.NextSwingLanding))
                return Rejected(
                    CharacterFootSwingMotionRejectReason.InvalidEnvelope,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);

            Vector3 up = componentUp.normalized;
            Vector3 horizontal = Vector3.ProjectOnPlane(
                groundPath.NextSwingLanding - groundPath.LastLanding,
                up);
            float pathLength = horizontal.magnitude;
            if (!float.IsFinite(pathLength) || pathLength <= GeometryEpsilon)
                return Rejected(
                    CharacterFootSwingMotionRejectReason.DegeneratePath,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            if (Vector3.Distance(
                    groundPath.EnvelopeVertexAt(0).Position,
                    groundPath.LastLanding) > EndpointTolerance ||
                Vector3.Distance(
                    groundPath.EnvelopeVertexAt(groundPath.EnvelopeVertexCount - 1).Position,
                    groundPath.NextSwingLanding) > EndpointTolerance)
                return Rejected(
                    CharacterFootSwingMotionRejectReason.EnvelopeEndpointMismatch,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);

            float progress = trajectoryProgress;
            float distance = pathLength * progress;
            Vector3 baselineSample = Vector3.Lerp(
                groundPath.LastLanding,
                groundPath.NextSwingLanding,
                progress);
            if (!TrySampleEnvelope(
                    groundPath,
                    progress,
                    out Vector3 envelopeSample,
                    out CharacterFootSwingMotionRejectReason sampleRejectReason))
                return Rejected(
                    sampleRejectReason,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle,
                    distance,
                    progress,
                    baselineSample);

            float envelopeFloorLift = Vector3.Dot(
                envelopeSample - baselineSample,
                up);
            if (!float.IsFinite(envelopeFloorLift))
                return Rejected(
                    CharacterFootSwingMotionRejectReason.NegativeVerticalCorrection,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle,
                    distance,
                    progress,
                    baselineSample,
                    envelopeSample);
            float baselineHeightError = Vector3.Dot(
                baselineSample - originalSole,
                up);
            if (!float.IsFinite(baselineHeightError))
                return Rejected(
                    CharacterFootSwingMotionRejectReason.NegativeVerticalCorrection,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle,
                    distance,
                    progress,
                    baselineSample,
                    envelopeSample);
            float verticalCorrection =
                Mathf.Max(0f, envelopeFloorLift) +
                landingConstraintWeight * baselineHeightError;
            Vector3 correctedSole = originalSole + up * verticalCorrection;
            Vector3 correctedAnkle = originalAnkle + up * verticalCorrection;
            float positionWeight = footPlacementWeight;
            return new CharacterFootSwingMotionResult(
                CharacterFootSwingMotionState.Accepted,
                CharacterFootSwingMotionRejectReason.None,
                landingEventIdentity,
                groundPath.InputIdentity,
                new CharacterFootSwingPathReference(
                    landingEventIdentity,
                    groundPath.NextSwingLanding),
                originalSole,
                originalAnkle,
                distance,
                progress,
                baselineSample,
                envelopeSample,
                verticalCorrection,
                landingPredictionError,
                landingConstraintWeight,
                correctedSole,
                correctedAnkle,
                positionWeight,
                0f);
        }

        internal static CharacterFootSwingMotionResult SuppressUnselected(
            in CharacterFootSwingMotionResult motion)
        {
            if (!motion.Accepted)
                return motion;
            return new CharacterFootSwingMotionResult(
                CharacterFootSwingMotionState.Rejected,
                CharacterFootSwingMotionRejectReason.UnselectedSwing,
                motion.LandingEventIdentity,
                motion.GroundPathInputIdentity,
                default,
                motion.OriginalSole,
                motion.OriginalAnkle,
                motion.Distance,
                motion.Progress,
                motion.BaselineSample,
                motion.EnvelopeSample,
                motion.VerticalCorrection,
                motion.LandingPredictionError,
                motion.LandingConstraintWeight,
                motion.OriginalSole,
                motion.OriginalAnkle,
                0f,
                0f,
                plantConfidence: motion.PlantConfidence);
        }

        static bool TryResolveSwingPhaseWeight(
            in AnimationBiomechanicalStepHeader step,
            out float weight)
        {
            weight = 0f;
            if (!float.IsFinite(step.EventPhase) ||
                !float.IsFinite(step.LiftOffPhase) ||
                !float.IsFinite(step.LandingPhase) ||
                step.LandingPhase <= step.LiftOffPhase)
                return false;
            float phase = Mathf.InverseLerp(
                step.LiftOffPhase,
                step.LandingPhase,
                step.EventPhase);
            weight = Mathf.SmoothStep(0f, 1f, phase);
            return float.IsFinite(weight);
        }

        static bool TrySampleEnvelope(
            in CharacterFootGroundPathResult groundPath,
            float progress,
            out Vector3 sample,
            out CharacterFootSwingMotionRejectReason rejectReason)
        {
            Vector3 previous = groundPath.EnvelopeVertexAt(0).Position;
            if (!Finite(previous))
            {
                sample = default;
                rejectReason = CharacterFootSwingMotionRejectReason.InvalidEnvelope;
                return false;
            }
            float totalLength = 0f;
            for (int i = 1; i < groundPath.EnvelopeVertexCount; i++)
            {
                Vector3 current = groundPath.EnvelopeVertexAt(i).Position;
                if (!Finite(current))
                {
                    sample = default;
                    rejectReason = CharacterFootSwingMotionRejectReason.InvalidEnvelope;
                    return false;
                }
                float segmentLength = Vector3.Distance(previous, current);
                if (!float.IsFinite(segmentLength))
                {
                    sample = default;
                    rejectReason = CharacterFootSwingMotionRejectReason.InvalidEnvelope;
                    return false;
                }
                totalLength += segmentLength;
                previous = current;
            }
            if (!float.IsFinite(totalLength) || totalLength <= GeometryEpsilon)
            {
                sample = default;
                rejectReason = CharacterFootSwingMotionRejectReason.DegeneratePath;
                return false;
            }

            float targetDistance = Mathf.Clamp01(progress) * totalLength;
            if (targetDistance >= totalLength - GeometryEpsilon)
            {
                sample = groundPath.EnvelopeVertexAt(
                    groundPath.EnvelopeVertexCount - 1).Position;
                rejectReason = CharacterFootSwingMotionRejectReason.None;
                return true;
            }

            float accumulatedLength = 0f;
            previous = groundPath.EnvelopeVertexAt(0).Position;
            for (int i = 1; i < groundPath.EnvelopeVertexCount; i++)
            {
                Vector3 current = groundPath.EnvelopeVertexAt(i).Position;
                float segmentLength = Vector3.Distance(previous, current);
                if (segmentLength <= GeometryEpsilon)
                {
                    previous = current;
                    continue;
                }
                if (targetDistance <= accumulatedLength + segmentLength)
                {
                    float t = Mathf.Clamp01(
                        (targetDistance - accumulatedLength) / segmentLength);
                    sample = Vector3.Lerp(previous, current, t);
                    rejectReason = Finite(sample)
                        ? CharacterFootSwingMotionRejectReason.None
                        : CharacterFootSwingMotionRejectReason.InvalidEnvelope;
                    return rejectReason == CharacterFootSwingMotionRejectReason.None;
                }
                accumulatedLength += segmentLength;
                previous = current;
            }
            sample = groundPath.EnvelopeVertexAt(
                groundPath.EnvelopeVertexCount - 1).Position;
            rejectReason = CharacterFootSwingMotionRejectReason.None;
            return true;
        }

        static CharacterFootSwingMotionResult Rejected(
            CharacterFootSwingMotionRejectReason reason,
            ulong landingEventIdentity,
            ulong groundPathInputIdentity,
            Vector3 originalSole,
            Vector3 originalAnkle,
            float distance = 0f,
            float progress = 0f,
            Vector3 baselineSample = default,
            Vector3 envelopeSample = default,
            float verticalCorrection = 0f,
            float landingPredictionError = 0f,
            float landingConstraintWeight = 0f) =>
            new CharacterFootSwingMotionResult(
                CharacterFootSwingMotionState.Rejected,
                reason,
                landingEventIdentity,
                groundPathInputIdentity,
                default,
                originalSole,
                originalAnkle,
                distance,
                progress,
                baselineSample,
                envelopeSample,
                verticalCorrection,
                landingPredictionError,
                landingConstraintWeight,
                originalSole,
                originalAnkle,
                0f,
                0f);

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
