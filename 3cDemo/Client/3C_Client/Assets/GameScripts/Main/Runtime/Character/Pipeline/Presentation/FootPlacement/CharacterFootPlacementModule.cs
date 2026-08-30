using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal sealed class CharacterFootPlacementDiagnosticsPage
    {
        CharacterFootLandingPredictionDiagnostics m_Value;

        internal bool HasValue => m_Value.FrameSequence != 0;
        internal ref readonly CharacterFootLandingPredictionDiagnostics Value =>
            ref m_Value;

        internal void Set(in CharacterFootLandingPredictionDiagnostics value) =>
            m_Value = value;

        internal void Clear() => m_Value = default;
    }

    internal sealed class CharacterFootPlacementBank
    {
        internal CharacterFootLifecycleContext LeftFoot;
        internal CharacterFootLifecycleContext RightFoot;
        internal CharacterFootPelvisSpringState PelvisSpring;
        internal CharacterFootPrimarySupportFacts PrimarySupport;
        internal CharacterResolvedFootPair ResolvedFeet;
        internal CharacterFootStrideHipsResult StrideHips;
        internal CharacterFullBodyIkGoal PelvisGoal;
        internal CharacterFullBodyIkGoal LeftGoal;
        internal CharacterFullBodyIkGoal RightGoal;
        internal bool HasVisibleFootOutputs;
        internal Vector3 LeftVisibleSole;
        internal Vector3 RightVisibleSole;
        internal CharacterFootGroundPathPage LeftGroundPath;
        internal CharacterFootGroundPathPage RightGroundPath;
        internal CharacterFootLandingObservationPage LeftLandingObservation;
        internal CharacterFootLandingObservationPage RightLandingObservation;
        internal CharacterFootCurrentSupportObservationPage LeftCurrentSupport;
        internal CharacterFootCurrentSupportObservationPage RightCurrentSupport;
        internal CharacterFootPredictionMotionState PredictionMotion;
        internal CharacterFootPredictionMotionResult PredictionMotionResult;
        internal readonly CharacterFutureBodyTranslation BodyTrajectory =
            new CharacterFutureBodyTranslation();
        internal ulong BodyTrajectoryTick;
        internal ulong BodyTrajectoryResetSequence;
        internal ulong BodyTrajectoryGeneration;
        internal ulong BodyTrajectoryAuthorityTick;
        internal ulong BodyTrajectoryPredictionMotionRevision;
        internal float BodyTrajectoryRequestedDuration;
        internal bool HasBodyTrajectoryAttempt;
        internal readonly CharacterFootPlacementDiagnosticsPage Diagnostics =
            new CharacterFootPlacementDiagnosticsPage();
        internal ulong FrameSequence;
        internal ulong CompletionIdentity;
        internal bool RecordDiagnostics;
        internal bool HasFrame;

        internal void Begin(
            CharacterFootPlacementBank committed,
            bool recordDiagnostics)
        {
            if (committed == null)
            {
                Reset();
            }
            else
            {
                LeftFoot = committed.LeftFoot;
                RightFoot = committed.RightFoot;
                PelvisSpring = committed.PelvisSpring;
                PrimarySupport = committed.PrimarySupport;
                ResolvedFeet = default;
                StrideHips = default;
                PelvisGoal = default;
                LeftGoal = default;
                RightGoal = default;
                HasVisibleFootOutputs = false;
                LeftVisibleSole = default;
                RightVisibleSole = default;
                LeftGroundPath = null;
                RightGroundPath = null;
                LeftLandingObservation = null;
                RightLandingObservation = null;
                LeftCurrentSupport = null;
                RightCurrentSupport = null;
                PredictionMotion = committed.PredictionMotion;
                PredictionMotionResult = default;
                BodyTrajectory.CopyFrom(committed.BodyTrajectory);
                BodyTrajectoryTick = committed.BodyTrajectoryTick;
                BodyTrajectoryResetSequence = committed.BodyTrajectoryResetSequence;
                BodyTrajectoryGeneration = committed.BodyTrajectoryGeneration;
                BodyTrajectoryAuthorityTick = committed.BodyTrajectoryAuthorityTick;
                BodyTrajectoryPredictionMotionRevision =
                    committed.BodyTrajectoryPredictionMotionRevision;
                BodyTrajectoryRequestedDuration = committed.BodyTrajectoryRequestedDuration;
                HasBodyTrajectoryAttempt = committed.HasBodyTrajectoryAttempt;
                Diagnostics.Clear();
            }
            FrameSequence = 0;
            CompletionIdentity = 0;
            RecordDiagnostics = recordDiagnostics;
            HasFrame = true;
        }

        internal void ClearPending()
        {
            LeftGroundPath = null;
            RightGroundPath = null;
            LeftLandingObservation = null;
            RightLandingObservation = null;
            LeftCurrentSupport = null;
            RightCurrentSupport = null;
            PredictionMotionResult = default;
            ResolvedFeet = default;
            StrideHips = default;
            PelvisGoal = default;
            LeftGoal = default;
            RightGoal = default;
            HasVisibleFootOutputs = false;
            LeftVisibleSole = default;
            RightVisibleSole = default;
            Diagnostics.Clear();
            FrameSequence = 0;
            CompletionIdentity = 0;
            RecordDiagnostics = false;
            HasFrame = false;
        }

        internal void Reset()
        {
            CharacterFootCorrectionResponseInitializationReason leftReason =
                LeftFoot.Interpolation
                    .PendingCorrectionResponseInitializationReason;
            CharacterFootCorrectionResponseInitializationReason rightReason =
                RightFoot.Interpolation
                    .PendingCorrectionResponseInitializationReason;
            if (leftReason !=
                    CharacterFootCorrectionResponseInitializationReason.None &&
                rightReason !=
                    CharacterFootCorrectionResponseInitializationReason.None &&
                leftReason != rightReason)
            {
                throw new InvalidOperationException(
                    "Foot correction response reset reasons are inconsistent.");
            }
            Reset(leftReason !=
                  CharacterFootCorrectionResponseInitializationReason.None
                ? leftReason
                : rightReason !=
                  CharacterFootCorrectionResponseInitializationReason.None
                    ? rightReason
                    : CharacterFootCorrectionResponseInitializationReason
                        .FirstLegalInput);
        }

        internal void Reset(
            CharacterFootCorrectionResponseInitializationReason reason)
        {
            if (reason ==
                CharacterFootCorrectionResponseInitializationReason.None)
            {
                throw new ArgumentOutOfRangeException(nameof(reason));
            }
            LeftFoot = default;
            RightFoot = default;
            LeftFoot.Interpolation
                .PendingCorrectionResponseInitializationReason = reason;
            RightFoot.Interpolation
                .PendingCorrectionResponseInitializationReason = reason;
            PelvisSpring.Clear();
            PrimarySupport.Clear();
            ResolvedFeet = default;
            StrideHips = default;
            PelvisGoal = default;
            LeftGoal = default;
            RightGoal = default;
            HasVisibleFootOutputs = false;
            LeftVisibleSole = default;
            RightVisibleSole = default;
            LeftGroundPath = null;
            RightGroundPath = null;
            LeftLandingObservation = null;
            RightLandingObservation = null;
            LeftCurrentSupport = null;
            RightCurrentSupport = null;
            PredictionMotion.Clear();
            PredictionMotionResult = default;
            BodyTrajectory.Clear();
            BodyTrajectoryTick = 0;
            BodyTrajectoryResetSequence = 0;
            BodyTrajectoryGeneration = 0;
            BodyTrajectoryAuthorityTick = 0;
            BodyTrajectoryPredictionMotionRevision = 0;
            BodyTrajectoryRequestedDuration = 0f;
            HasBodyTrajectoryAttempt = false;
            Diagnostics.Clear();
            FrameSequence = 0;
            CompletionIdentity = 0;
            RecordDiagnostics = false;
            HasFrame = false;
        }
    }

    readonly struct CharacterFootLandingPredictionPair
    {
        internal CharacterFootLandingPredictionPair(
            CharacterFootLandingPredictionResult selected,
            CharacterFootLandingStepSource selectedSource)
        {
            Selected = selected;
            SelectedSource = selectedSource;
        }

        internal CharacterFootLandingPredictionResult Selected { get; }
        internal CharacterFootLandingStepSource SelectedSource { get; }
    }

    readonly struct CharacterFootActionOccupancy
    {
        internal CharacterFootActionOccupancy(ulong actionInstanceIdentity, float weight)
        {
            ActionInstanceIdentity = actionInstanceIdentity;
            Weight = weight;
        }

        internal ulong ActionInstanceIdentity { get; }
        internal float Weight { get; }
    }

    internal sealed class CharacterFootPlacementModule : IDisposable
    {
        readonly ActorId m_ActorId;
        readonly CharacterFootPlacementModuleSettings m_Settings;
        readonly CharacterFootPlacementPoseRig m_Rig;
        readonly ICharacterFutureBodyTranslationSource m_FutureBodyTranslationSource;
        readonly ICharacterFootPlacementWorldQuery m_WorldQuery;
        readonly CharacterFootGroundPathPagePool m_LeftGroundPath;
        readonly CharacterFootGroundPathPagePool m_RightGroundPath;
        readonly CharacterFootLandingObservationPagePool m_LeftLandingObservation;
        readonly CharacterFootLandingObservationPagePool m_RightLandingObservation;
        readonly CharacterFootCurrentSupportObservationPagePool m_LeftCurrentSupport;
        readonly CharacterFootCurrentSupportObservationPagePool m_RightCurrentSupport;

        bool m_Disposed;

        internal CharacterFootPlacementModule(
            ActorId actorId,
            CharacterFootPlacementModuleSettings settings,
            CharacterFootPlacementPoseRig rig,
            ICharacterFutureBodyTranslationSource futureBodyTranslationSource,
            ICharacterFootPlacementWorldQuery worldQuery)
        {
            if (!actorId.IsValid || settings == null || rig == null || worldQuery == null)
            {
                throw new ArgumentException("Foot Placement Runtime input is invalid.");
            }
            m_ActorId = actorId;
            m_Settings = settings;
            m_Rig = rig;
            m_FutureBodyTranslationSource = futureBodyTranslationSource;
            m_WorldQuery = worldQuery;
            m_LeftGroundPath = new CharacterFootGroundPathPagePool(
                settings.GroundDetection.ContactCapacity);
            m_RightGroundPath = new CharacterFootGroundPathPagePool(
                settings.GroundDetection.ContactCapacity);
            m_LeftLandingObservation = new CharacterFootLandingObservationPagePool();
            m_RightLandingObservation = new CharacterFootLandingObservationPagePool();
            m_LeftCurrentSupport =
                new CharacterFootCurrentSupportObservationPagePool();
            m_RightCurrentSupport =
                new CharacterFootCurrentSupportObservationPagePool();
        }

        internal CharacterFootPlacementBank CreateBank() =>
            new CharacterFootPlacementBank();

        internal CharacterFootPlacementResult EvaluateFrame(
            in CharacterFootPlacementFrameInput frame,
            CharacterFootPlacementBank committedBank,
            CharacterFootPlacementBank bank)
        {
            RequireAlive();
            if (bank == null || !bank.HasFrame)
                throw new InvalidOperationException("Foot Placement has no open bank.");
            if (bank.FrameSequence != 0)
                throw new InvalidOperationException("Foot Placement already evaluated the open bank.");
            if (frame.ActorId != m_ActorId ||
                !string.Equals(
                    frame.Pose.PosePlanHash,
                    m_Settings.PosePlanHash,
                    StringComparison.Ordinal) ||
                frame.FootPlacementWeight < 0f ||
                frame.FootPlacementWeight > 1f)
            {
                throw new ArgumentException("Foot Placement frame contract is inconsistent.");
            }
            CharacterFootPlacementAnimatedPose pose = m_Rig.CaptureAnimatedPose(
                frame.RenderFrame,
                frame.Pose.DenseComponentPoses);
            CharacterFullBodyIkGoal pelvisGoal = CreatePelvisGoal();
            CharacterFullBodyIkGoal leftGoal = CreateFootGoal(
                CharacterFootSide.Left,
                pose.Left);
            CharacterFullBodyIkGoal rightGoal = CreateFootGoal(
                CharacterFootSide.Right,
                pose.Right);

            CharacterPresentationFactFrame facts = frame.Facts;
            CommittedLocomotionPlanarMotionTimeline timeline =
                facts.LocomotionMotionTimeline;
            CharacterFootActionOccupancy leftAction = ResolveActionOccupancy(
                frame.Pose,
                CharacterFootSide.Left);
            CharacterFootActionOccupancy rightAction = ResolveActionOccupancy(
                frame.Pose,
                CharacterFootSide.Right);
            float currentSegmentRemainingSeconds = timeline.IsValid
                ? ResolveCurrentSegmentRemainingSeconds(timeline, frame.Body)
                : 0f;
            AnimationFootMotionRuntimeFrame formalFootFrame =
                frame.Pose.FootMotion;
            AnimationFootMotionRuntimeSample leftCurrentStep =
                formalFootFrame.Left;
            AnimationFootMotionRuntimeSample rightCurrentStep =
                formalFootFrame.Right;
            var leftLockRequest = new CharacterFootLockRequest(in leftCurrentStep);
            var rightLockRequest = new CharacterFootLockRequest(in rightCurrentStep);
            CharacterFutureBodyTranslation bodyTrajectory = ResolveBodyTrajectory(
                bank,
                leftCurrentStep,
                rightCurrentStep,
                in timeline,
                currentSegmentRemainingSeconds,
                frame.PresentationDeltaSeconds,
                frame.Body);
            Vector3 componentUp = frame.Body.VisibleRotation * Vector3.up;
            bank.LeftCurrentSupport = PrepareCurrentSupport(
                CharacterFootSide.Left,
                pose.Left,
                componentUp,
                facts.Grounded,
                frame.RenderFrame,
                frame.Pose.CompletionIdentity,
                m_LeftCurrentSupport,
                committedBank?.LeftCurrentSupport);
            bank.RightCurrentSupport = PrepareCurrentSupport(
                CharacterFootSide.Right,
                pose.Right,
                componentUp,
                facts.Grounded,
                frame.RenderFrame,
                frame.Pose.CompletionIdentity,
                m_RightCurrentSupport,
                committedBank?.RightCurrentSupport);
            CharacterFootCurrentSupportObservation leftCurrentSupport =
                bank.LeftCurrentSupport.Observation;
            CharacterFootCurrentSupportObservation rightCurrentSupport =
                bank.RightCurrentSupport.Observation;

            CharacterFootLandingSnapshot leftLanding =
                CharacterFootLandingRuntime.ProjectBeforePrediction(
                    in bank.LeftFoot,
                    in leftCurrentStep);
            CharacterFootLandingSnapshot rightLanding =
                CharacterFootLandingRuntime.ProjectBeforePrediction(
                    in bank.RightFoot,
                    in rightCurrentStep);
            bool leftPlantVerificationRequired =
                CharacterFootTransitionResolver.RequiresPlantVerification(
                    in bank.LeftFoot,
                    in leftLockRequest);
            bool rightPlantVerificationRequired =
                CharacterFootTransitionResolver.RequiresPlantVerification(
                    in bank.RightFoot,
                    in rightLockRequest);
            CharacterFootLandingPredictionPair leftPair = PredictFootPair(
                CharacterFootSide.Left,
                leftCurrentStep,
                pose.Left,
                leftGoal,
                in timeline,
                currentSegmentRemainingSeconds,
                bodyTrajectory,
                in frame,
                in leftLanding,
                leftPlantVerificationRequired,
                m_LeftLandingObservation,
                committedBank?.LeftLandingObservation,
                out bank.LeftLandingObservation);
            CharacterFootLandingPredictionPair rightPair = PredictFootPair(
                CharacterFootSide.Right,
                rightCurrentStep,
                pose.Right,
                rightGoal,
                in timeline,
                currentSegmentRemainingSeconds,
                bodyTrajectory,
                in frame,
                in rightLanding,
                rightPlantVerificationRequired,
                m_RightLandingObservation,
                committedBank?.RightLandingObservation,
                out bank.RightLandingObservation);
            CharacterFootLandingPredictionResult left = leftPair.Selected;
            AnimationFootMotionRuntimeSample leftSelectedStep = leftCurrentStep;
            CharacterFootLandingPredictionResult right = rightPair.Selected;
            AnimationFootMotionRuntimeSample rightSelectedStep = rightCurrentStep;
            leftLanding = CharacterFootLandingRuntime.ProjectAfterPrediction(
                in bank.LeftFoot,
                in leftCurrentStep,
                in left,
                m_Settings.FootMotion);
            rightLanding = CharacterFootLandingRuntime.ProjectAfterPrediction(
                in bank.RightFoot,
                in rightCurrentStep,
                in right,
                m_Settings.FootMotion);
            bool hasLeftLastLanding = leftLanding.HasLastLanding;
            bool hasLeftNextSwingLanding = leftLanding.HasNextSwingLanding;
            bool hasRightLastLanding = rightLanding.HasLastLanding;
            bool hasRightNextSwingLanding = rightLanding.HasNextSwingLanding;
            CharacterFootGroundPathLanding leftLastLanding = leftLanding.LastLanding;
            CharacterFootGroundPathLanding leftNextSwingLanding = leftLanding.NextSwingLanding;
            CharacterFootGroundPathLanding rightLastLanding = rightLanding.LastLanding;
            CharacterFootGroundPathLanding rightNextSwingLanding = rightLanding.NextSwingLanding;
            CharacterFootGroundPathLanding leftContactLanding = default;
            bool hasLeftContactLanding = leftLockRequest.RequestsLock &&
                (leftLanding.TryResolveVerifiedLanding(
                     leftLockRequest.EventIdentity,
                     out leftContactLanding) ||
                 CharacterFootLandingRuntime.TryResolveCurrentContactCandidate(
                     in leftCurrentStep,
                     in left,
                     out leftContactLanding));
            CharacterFootGroundPathLanding rightContactLanding = default;
            bool hasRightContactLanding = rightLockRequest.RequestsLock &&
                (rightLanding.TryResolveVerifiedLanding(
                     rightLockRequest.EventIdentity,
                     out rightContactLanding) ||
                 CharacterFootLandingRuntime.TryResolveCurrentContactCandidate(
                     in rightCurrentStep,
                     in right,
                     out rightContactLanding));
            bool leftPreparedPlantActive = IsPreparedPlantTargetActive(
                in leftCurrentStep,
                in leftLanding);
            bool rightPreparedPlantActive = IsPreparedPlantTargetActive(
                in rightCurrentStep,
                in rightLanding);
            CharacterFootGroundPathLanding leftPreparedPlantTarget =
                leftPreparedPlantActive ? leftLanding.PlantTarget : default;
            CharacterFootGroundPathLanding rightPreparedPlantTarget =
                rightPreparedPlantActive ? rightLanding.PlantTarget : default;
            CharacterFootGroundPathResult leftGroundPath = PrepareGroundPath(
                CharacterFootSide.Left,
                hasLeftLastLanding,
                leftLastLanding,
                hasLeftNextSwingLanding,
                leftNextSwingLanding,
                componentUp,
                timeline.IsValid ? timeline.AuthorityTick.Value : 0,
                m_LeftGroundPath,
                committedBank?.LeftGroundPath,
                out bank.LeftGroundPath);
            CharacterFootGroundPathResult rightGroundPath = PrepareGroundPath(
                CharacterFootSide.Right,
                hasRightLastLanding,
                rightLastLanding,
                hasRightNextSwingLanding,
                rightNextSwingLanding,
                componentUp,
                timeline.IsValid ? timeline.AuthorityTick.Value : 0,
                m_RightGroundPath,
                committedBank?.RightGroundPath,
                out bank.RightGroundPath);
            left = left.WithGroundPath(in leftGroundPath);
            right = right.WithGroundPath(in rightGroundPath);

            float footPlacementWeight = frame.FootPlacementWeight;
            CharacterFootSwingMotionResult leftSwingMotion =
                CharacterFootSwingMotionBuilder.Build(
                    pose.Left,
                    in leftSelectedStep,
                    footPlacementWeight,
                    componentUp,
                    in leftGroundPath,
                    true,
                    formalFootFrame.Left.FootHeight,
                    leftLanding.NextSwingPredictionError);
            CharacterFootSwingMotionResult rightSwingMotion =
                CharacterFootSwingMotionBuilder.Build(
                    pose.Right,
                    in rightSelectedStep,
                    footPlacementWeight,
                    componentUp,
                    in rightGroundPath,
                    true,
                    formalFootFrame.Right.FootHeight,
                    rightLanding.NextSwingPredictionError);
            bool hasSelectedSwing = CharacterFootStrideHipsBuilder.TrySelectSwing(
                in leftSelectedStep,
                in rightSelectedStep,
                in leftSwingMotion,
                in rightSwingMotion,
                out CharacterFootSide selectedSwingSide);
            Transform goalRoot = m_Rig.PoseRoot;
            var sourceLineage = new FixedString128Bytes(
                frame.Pose.PosePlanHash);
            var profileRevision = new FixedString128Bytes(
                m_Settings.ProfileRevision);
            ulong worldRevision = m_WorldQuery.WorldRevision;
            bool leftPreviousVisibleOutputAvailable =
                TryResolvePreviousVisibleOutput(
                    committedBank,
                    CharacterFootSide.Left,
                    out Vector3 leftPreviousVisibleOutputPoint);
            bool rightPreviousVisibleOutputAvailable =
                TryResolvePreviousVisibleOutput(
                    committedBank,
                    CharacterFootSide.Right,
                    out Vector3 rightPreviousVisibleOutputPoint);
            var leftConstraintFrame = new CharacterFootStateFrame(
                frame.RenderFrame,
                frame.Pose.CompletionIdentity,
                new FixedString64Bytes(m_Rig.Rig.RigId),
                new FixedString64Bytes(m_Rig.Rig.RigRevision),
                CharacterFootSide.Left,
                pose.Left,
                pose.Left.HipPosition,
                m_Rig.LeftLegLength,
                in leftSwingMotion,
                hasLeftContactLanding,
                in leftContactLanding,
                leftPreparedPlantActive,
                in leftPreparedPlantTarget,
                in leftCurrentSupport,
                leftPreviousVisibleOutputAvailable,
                leftPreviousVisibleOutputPoint,
                in leftLockRequest,
                leftCurrentStep.Support,
                leftLockRequest.EventIdentity,
                ResolveFootGoalOwnershipLoss(
                    facts.Grounded,
                    leftCurrentStep.IsAuthoritative),
                footPlacementWeight,
                componentUp,
                frame.PresentationDeltaSeconds,
                sourceLineage,
                profileRevision,
                worldRevision,
                m_Settings.FootMotion);
            var rightConstraintFrame = new CharacterFootStateFrame(
                frame.RenderFrame,
                frame.Pose.CompletionIdentity,
                new FixedString64Bytes(m_Rig.Rig.RigId),
                new FixedString64Bytes(m_Rig.Rig.RigRevision),
                CharacterFootSide.Right,
                pose.Right,
                pose.Right.HipPosition,
                m_Rig.RightLegLength,
                in rightSwingMotion,
                hasRightContactLanding,
                in rightContactLanding,
                rightPreparedPlantActive,
                in rightPreparedPlantTarget,
                in rightCurrentSupport,
                rightPreviousVisibleOutputAvailable,
                rightPreviousVisibleOutputPoint,
                in rightLockRequest,
                rightCurrentStep.Support,
                rightLockRequest.EventIdentity,
                ResolveFootGoalOwnershipLoss(
                    facts.Grounded,
                    rightCurrentStep.IsAuthoritative),
                footPlacementWeight,
                componentUp,
                frame.PresentationDeltaSeconds,
                sourceLineage,
                profileRevision,
                worldRevision,
                m_Settings.FootMotion);
            var leftEvaluation = new CharacterFootStateEvaluation(
                CharacterFootSide.Left,
                in leftCurrentStep,
                in left,
                in leftConstraintFrame);
            var rightEvaluation = new CharacterFootStateEvaluation(
                CharacterFootSide.Right,
                in rightCurrentStep,
                in right,
                in rightConstraintFrame);
            CharacterResolvedFootResult leftResolved =
                CharacterFootLifecycle.Evaluate(
                    ref bank.LeftFoot,
                    in leftEvaluation,
                    out CharacterFootSwingMotionResult leftFootMotion,
                    out CharacterFootLifecycleEvaluationReceipt
                        leftLifecycleReceipt);
            CharacterResolvedFootResult rightResolved =
                CharacterFootLifecycle.Evaluate(
                    ref bank.RightFoot,
                    in rightEvaluation,
                    out CharacterFootSwingMotionResult rightFootMotion,
                    out CharacterFootLifecycleEvaluationReceipt
                        rightLifecycleReceipt);
            var resolvedPair = new CharacterResolvedFootPair(
                in leftResolved,
                in rightResolved);
            CharacterFootLandingReachRequest leftReachRequest =
                leftResolved.LandingReachRequest;
            CharacterFootLandingReachRequest rightReachRequest =
                rightResolved.LandingReachRequest;
            leftGoal = CreateFootGoal(
                CharacterFootSide.Left,
                pose.Left,
                in leftResolved);
            rightGoal = CreateFootGoal(
                CharacterFootSide.Right,
                pose.Right,
                in rightResolved);
            CharacterFootStrideHipsBuilder.ResolvePrimarySupport(
                in leftResolved,
                in rightResolved,
                ref bank.PrimarySupport);
            CharacterFootPrimarySupportResult primarySupport =
                bank.PrimarySupport.Result;
            Vector3 leftCorrectedSole = ResolveWeightedGoalSole(
                pose.Left,
                in leftGoal,
                goalRoot);
            Vector3 rightCorrectedSole = ResolveWeightedGoalSole(
                pose.Right,
                in rightGoal,
                goalRoot);
            CharacterFootStrideIntentResult strideIntent =
                CharacterFootStrideHipsBuilder.ResolveIntent(
                in leftSelectedStep,
                in rightSelectedStep,
                hasSelectedSwing,
                selectedSwingSide,
                hasLeftNextSwingLanding,
                leftNextSwingLanding,
                hasRightNextSwingLanding,
                rightNextSwingLanding,
                leftGroundPath.Accepted,
                rightGroundPath.Accepted,
                facts.Grounded,
                in resolvedPair,
                in primarySupport,
                componentUp);
            var pelvisFrame = new CharacterFootPelvisFrame(
                componentUp,
                m_Rig.PoseRoot.position,
                m_Rig.PoseRoot.TransformPoint(pose.PelvisLocalPosition),
                pose.PelvisLocalPosition,
                in pose,
                leftCorrectedSole,
                rightCorrectedSole,
                m_Rig.LeftLegLength,
                m_Rig.RightLegLength,
                footPlacementWeight,
                frame.PresentationDeltaSeconds);
            bool leftLandingReach = facts.Grounded && IsLandingReachCandidate(
                in leftSelectedStep, in leftFootMotion, in leftResolved);
            bool rightLandingReach = facts.Grounded && IsLandingReachCandidate(
                in rightSelectedStep, in rightFootMotion, in rightResolved);
            var pelvisReachInput = new CharacterFootPelvisReachInput(
                leftLandingReach, in leftReachRequest,
                rightLandingReach, in rightReachRequest);
            CharacterFootStrideHipsResult strideHips = CharacterFootStrideHipsBuilder.ResolvePelvis(
                in strideIntent,
                in resolvedPair,
                in primarySupport,
                in pelvisFrame,
                in pelvisReachInput,
                m_Settings.FootMotion,
                ref bank.PelvisSpring);
            bool leftReachAvailable = strideHips.LeftLandingReachAvailable;
            bool rightReachAvailable = strideHips.RightLandingReachAvailable;
            leftResolved = CharacterFootLifecycle.FinalizeLanding(
                ref bank.LeftFoot,
                in leftLifecycleReceipt,
                !leftLifecycleReceipt.LandingCompletionPending ||
                leftReachAvailable,
                out leftFootMotion);
            rightResolved = CharacterFootLifecycle.FinalizeLanding(
                ref bank.RightFoot,
                in rightLifecycleReceipt,
                !rightLifecycleReceipt.LandingCompletionPending ||
                rightReachAvailable,
                out rightFootMotion);
            resolvedPair = new CharacterResolvedFootPair(
                in leftResolved,
                in rightResolved);
            bank.ResolvedFeet = resolvedPair;
            leftGoal = CreateFootGoal(
                CharacterFootSide.Left,
                pose.Left,
                in leftResolved);
            rightGoal = CreateFootGoal(
                CharacterFootSide.Right,
                pose.Right,
                in rightResolved);
            float leftReachClampDistance = 0f;
            float rightReachClampDistance = 0f;
            if (leftLandingReach && !leftReachAvailable)
            {
                leftGoal = ClampFootGoalToReach(
                    in leftGoal,
                    leftReachRequest.Hip +
                    strideHips.PelvisDelta * strideHips.PositionWeight,
                    pose.Left.AnklePosition,
                    leftReachRequest.LegLength,
                    leftReachRequest.MinimumCompressionReserve,
                    goalRoot,
                    out leftReachClampDistance);
            }
            if (rightLandingReach && !rightReachAvailable)
            {
                rightGoal = ClampFootGoalToReach(
                    in rightGoal,
                    rightReachRequest.Hip +
                    strideHips.PelvisDelta * strideHips.PositionWeight,
                    pose.Right.AnklePosition,
                    rightReachRequest.LegLength,
                    rightReachRequest.MinimumCompressionReserve,
                    goalRoot,
                    out rightReachClampDistance);
            }
            leftFootMotion = CharacterFootSwingMotionBuilder.WithLandingReach(
                in leftFootMotion,
                leftLandingReach,
                leftReachAvailable,
                leftReachClampDistance >
                CharacterPoseConstraintMath.Epsilon,
                leftReachClampDistance);
            rightFootMotion = CharacterFootSwingMotionBuilder.WithLandingReach(
                in rightFootMotion,
                rightLandingReach,
                rightReachAvailable,
                rightReachClampDistance >
                CharacterPoseConstraintMath.Epsilon,
                rightReachClampDistance);
            pelvisGoal = CreatePelvisGoal(in strideHips, m_Rig.PoseRoot);
            bank.StrideHips = strideHips;
            if (!strideHips.ProducesPelvisGoal)
                bank.PelvisSpring.Clear();
            left = left.WithFootMotion(
                in leftFootMotion,
                leftGoal);
            right = right.WithFootMotion(
                in rightFootMotion,
                rightGoal);

            bank.PelvisGoal = pelvisGoal;
            bank.LeftGoal = leftGoal;
            bank.RightGoal = rightGoal;
            bank.LeftVisibleSole = ResolveWeightedGoalSole(
                pose.Left,
                in leftGoal,
                goalRoot);
            bank.RightVisibleSole = ResolveWeightedGoalSole(
                pose.Right,
                in rightGoal,
                goalRoot);
            bank.HasVisibleFootOutputs = true;
            bank.FrameSequence = frame.RenderFrame;
            bank.CompletionIdentity = frame.Pose.CompletionIdentity;
            if (bank.RecordDiagnostics)
            {
                AnimationFootMotionRuntimeFrame footStepObservation =
                    frame.Pose.FootMotion;
                var inputDiagnostics = new CharacterFootLandingPredictionInputDiagnostics(
                    frame.PresentationDeltaSeconds,
                    frame.Body,
                    facts.Grounded,
                    facts.HorizontalSpeed,
                    in leftAction,
                    in rightAction,
                    in timeline,
                    currentSegmentRemainingSeconds,
                    in bank.PredictionMotionResult,
                    in footStepObservation);
                var leftDiagnostics =
                    new CharacterFootLandingPredictionFootDiagnostics(
                        in left,
                        pose.Left,
                        new CharacterFootStepCandidateSelectionDiagnostics(
                            leftCurrentStep,
                            leftLanding.LastLandingEventIdentity,
                            leftPair.SelectedSource,
                            left.LandingEventIdentity,
                            m_Settings.LandingPrediction
                                .MaximumPredictionTimeSeconds),
                        bank.LeftFoot.LandingSnapshot,
                        leftPreparedPlantActive,
                        in leftCurrentSupport,
                        in leftResolved);
                var rightDiagnostics =
                    new CharacterFootLandingPredictionFootDiagnostics(
                        in right,
                        pose.Right,
                        new CharacterFootStepCandidateSelectionDiagnostics(
                            rightCurrentStep,
                            rightLanding.LastLandingEventIdentity,
                            rightPair.SelectedSource,
                            right.LandingEventIdentity,
                            m_Settings.LandingPrediction
                                .MaximumPredictionTimeSeconds),
                        bank.RightFoot.LandingSnapshot,
                        rightPreparedPlantActive,
                        in rightCurrentSupport,
                        in rightResolved);
                var primarySupportDiagnostics =
                    new CharacterFootPrimarySupportDiagnostics(in primarySupport);
                var strideDiagnostics =
                    new CharacterFootStrideHipsDiagnostics(in strideHips);
                var diagnostics = new CharacterFootLandingPredictionDiagnostics(
                    frame.RenderFrame,
                    frame.Pose.CompletionIdentity,
                    m_Rig.VisualRoot.GetInstanceID(),
                    m_Settings.ProfileId,
                    m_Settings.ProfileRevision,
                    inputDiagnostics,
                    in primarySupportDiagnostics,
                    pelvisGoal,
                    in strideDiagnostics,
                    leftDiagnostics,
                    rightDiagnostics);
                bank.Diagnostics.Set(in diagnostics);
            }
            return new CharacterFootPlacementResult(
                frame.RenderFrame,
                frame.Pose.CompletionIdentity,
                new FixedString64Bytes(m_Rig.Rig.RigId),
                new FixedString64Bytes(m_Rig.Rig.RigRevision),
                in resolvedPair,
                in primarySupport,
                in strideHips,
                pelvisGoal,
                leftGoal,
                rightGoal);
        }

        internal void ValidateFrame(
            CharacterFootPlacementBank bank,
            ulong renderFrame,
            ulong completionIdentity)
        {
            RequireAlive();
            if (bank == null || !bank.HasFrame ||
                bank.FrameSequence != renderFrame ||
                bank.CompletionIdentity != completionIdentity)
            {
                throw new InvalidOperationException(
                    "Foot Placement pending completion identity is inconsistent.");
            }
        }

        internal void PublishCommittedDiagnostics(CharacterFootPlacementBank bank)
        {
            if (bank == null || !bank.Diagnostics.HasValue)
                return;
            try
            {
                CharacterFootLandingPredictionDebugRegistry.Publish(
                    in bank.Diagnostics.Value);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        internal void ReleasePendingPages(
            CharacterFootPlacementBank committed,
            CharacterFootPlacementBank pending)
        {
            RequireAlive();
            CharacterFootGroundPathPagePool.Discard(
                pending?.LeftGroundPath,
                committed?.LeftGroundPath);
            CharacterFootGroundPathPagePool.Discard(
                pending?.RightGroundPath,
                committed?.RightGroundPath);
            CharacterFootLandingObservationPagePool.Discard(
                pending?.LeftLandingObservation,
                committed?.LeftLandingObservation);
            CharacterFootLandingObservationPagePool.Discard(
                pending?.RightLandingObservation,
                committed?.RightLandingObservation);
            CharacterFootCurrentSupportObservationPagePool.Discard(
                pending?.LeftCurrentSupport,
                committed?.LeftCurrentSupport);
            CharacterFootCurrentSupportObservationPagePool.Discard(
                pending?.RightCurrentSupport,
                committed?.RightCurrentSupport);
        }

        internal void ResetShared(in CharacterFootPlacementReset reset)
        {
            RequireAlive();
            if (reset.ActorId != m_ActorId)
                throw new ArgumentException("Foot Placement reset Actor identity is invalid.");
            m_LeftGroundPath.Reset();
            m_RightGroundPath.Reset();
            m_LeftLandingObservation.Reset();
            m_RightLandingObservation.Reset();
            m_LeftCurrentSupport.Reset();
            m_RightCurrentSupport.Reset();
            CharacterFootLandingPredictionDebugRegistry.Remove(
                m_Rig.VisualRoot.GetInstanceID());
        }

        internal void RetargetShared(ulong resetSequence)
        {
            RequireAlive();
            if (resetSequence == 0)
                throw new ArgumentOutOfRangeException(nameof(resetSequence));
            m_LeftGroundPath.Reset();
            m_RightGroundPath.Reset();
            m_LeftLandingObservation.Reset();
            m_RightLandingObservation.Reset();
            m_LeftCurrentSupport.Reset();
            m_RightCurrentSupport.Reset();
            CharacterFootLandingPredictionDebugRegistry.Remove(
                m_Rig.VisualRoot.GetInstanceID());
        }

        internal string ApplyTuning(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock block,
            bool resetOwnerState)
        {
            if (layout == null || block == null)
                return "Foot Placement tuning payload is missing.";
            return string.Empty;
        }

        CharacterFootCurrentSupportObservationPage PrepareCurrentSupport(
            CharacterFootSide side,
            CharacterFootPlacementAnimatedFootPose foot,
            Vector3 componentUp,
            bool grounded,
            ulong frameSequence,
            ulong completionIdentity,
            CharacterFootCurrentSupportObservationPagePool pool,
            CharacterFootCurrentSupportObservationPage committed)
        {
            CharacterFootCurrentSupportObservationPage pending =
                pool.AcquireWritable(committed);
            CharacterFootLandingPredictionSettings settings =
                m_Settings.LandingPrediction;
            var heelRequest = new CharacterFootCurrentSupportProbeRequest(
                side,
                CharacterFootCurrentSupportProbeKind.Heel,
                foot.HeelPosition,
                componentUp,
                settings.CastAbove,
                settings.CastBelow,
                settings.SphereRadius,
                settings.GroundLayerMask,
                settings.MinimumGroundNormalDot,
                settings.HitCapacity);
            var toeRequest = new CharacterFootCurrentSupportProbeRequest(
                side,
                CharacterFootCurrentSupportProbeKind.Toe,
                foot.ToePosition,
                componentUp,
                settings.CastAbove,
                settings.CastBelow,
                settings.SphereRadius,
                settings.GroundLayerMask,
                settings.MinimumGroundNormalDot,
                settings.HitCapacity);
            CharacterFootCurrentSupportObservation observation;
            if (!grounded)
            {
                observation = CharacterFootCurrentSupportObservation.Unavailable(
                    frameSequence,
                    completionIdentity,
                    m_WorldQuery.WorldRevision,
                    in heelRequest,
                    in toeRequest,
                    CharacterFootCurrentSupportRejectReason.NotGrounded);
                pending.Set(in observation);
                return pending;
            }
            CharacterFootCurrentSupportProbeResult heel =
                m_WorldQuery.Query(in heelRequest);
            CharacterFootCurrentSupportProbeResult toe =
                m_WorldQuery.Query(in toeRequest);
            observation = CharacterFootCurrentSupportObservation.Resolve(
                frameSequence,
                completionIdentity,
                m_WorldQuery.WorldRevision,
                in heelRequest,
                in toeRequest,
                in heel,
                in toe);
            pending.Set(in observation);
            return pending;
        }

        CharacterFootGroundPathResult PrepareGroundPath(
            CharacterFootSide side,
            bool hasLastLanding,
            CharacterFootGroundPathLanding lastLanding,
            bool hasNextSwingLanding,
            CharacterFootGroundPathLanding nextSwingLanding,
            Vector3 componentUp,
            ulong authorityTick,
            CharacterFootGroundPathPagePool pool,
            CharacterFootGroundPathPage committedPage,
            out CharacterFootGroundPathPage pendingPage)
        {
            if (!hasLastLanding)
            {
                pendingPage = pool.AcquireWritable(committedPage);
                pendingPage.SetRejected(
                    CharacterFootGroundPathRejectReason.CurrentLandingUnavailable,
                    false,
                    0,
                    default,
                    default);
                return new CharacterFootGroundPathResult(pendingPage, false);
            }
            if (!hasNextSwingLanding)
            {
                pendingPage = pool.AcquireWritable(committedPage);
                pendingPage.SetRejected(
                    CharacterFootGroundPathRejectReason.NextLandingUnavailable,
                    false, 0, default, default);
                return new CharacterFootGroundPathResult(pendingPage, false);
            }

            CharacterFootGroundPathInputKey key =
                CharacterFootGroundPathInputBuilder.BuildKey(
                    side,
                    in lastLanding,
                    in nextSwingLanding,
                    authorityTick,
                    componentUp,
                    m_Settings.ProfileRevision);
            if (committedPage != null &&
                committedPage.HasInput &&
                committedPage.State == CharacterFootGroundPathState.Accepted &&
                committedPage.Input.Key.Equals(key))
            {
                pendingPage = CharacterFootGroundPathPagePool.ReuseCommitted(committedPage);
                return new CharacterFootGroundPathResult(pendingPage, false);
            }

            pendingPage = pool.AcquireWritable(committedPage);
            CharacterFootGroundDetectionSettings settings = m_Settings.GroundDetection;
            if (!CharacterFootGroundPathInputBuilder.TryBuild(
                    in key,
                    lastLanding.Point,
                    nextSwingLanding.Point,
                    lastLanding.Normal,
                    nextSwingLanding.Normal,
                    lastLanding.SurfaceIdentity,
                    nextSwingLanding.SurfaceIdentity,
                    componentUp,
                    in settings,
                    out CharacterFootGroundPathInput input))
            {
                pendingPage.SetRejected(
                    CharacterFootGroundPathRejectReason.InvalidRequest,
                    false, 0, default, default);
                return new CharacterFootGroundPathResult(pendingPage, false);
            }

            CharacterFootGroundPathQueryRequest query = input.Query;
            CharacterFootGroundPathQueryResult result = m_WorldQuery.Query(
                in query,
                pendingPage.Contacts);
            if (result.Accepted)
            {
                if (CharacterFootGroundEnvelopeBuilder.TryBuild(
                        in input,
                        pendingPage.Contacts,
                        pool.EnvelopeWorkspace,
                        pendingPage.Edges,
                        pendingPage.Envelope,
                        out CharacterFootGroundPathRejectReason envelopeRejectReason,
                        out CharacterFootGroundInvalidSegment invalidSegment))
                {
                    pendingPage.SetAccepted(result.SegmentCount, in input);
                }
                else
                {
                    pendingPage.SetRejected(
                        envelopeRejectReason, true, result.SegmentCount,
                        in input, in invalidSegment);
                }
            }
            else
                pendingPage.SetRejected(
                    result.RejectReason, true, result.SegmentCount,
                    in input, default);
            return new CharacterFootGroundPathResult(pendingPage, true);
        }

        static bool IsPreparedPlantTargetActive(
            in AnimationFootMotionRuntimeSample footMotion,
            in CharacterFootLandingSnapshot landing)
        {
            if (landing.PlantTargetState !=
                    CharacterFootPlantTargetState.Tracking ||
                !landing.HasPlantTarget)
            {
                return false;
            }
            ulong eventIdentity = landing.PlantTarget.LandingEventIdentity;
            AnimationFootMotionEventFrame events = footMotion.Events;
            bool approachMatches = events.InApproachContactToLanding &&
                                   events.NextLanding.IsBound &&
                                   events.NextLanding.Identity == eventIdentity;
            bool currentMatches = events.CurrentContact.IsBound &&
                                  events.CurrentContact.Identity == eventIdentity;
            return approachMatches || currentMatches;
        }

        CharacterFootLandingPredictionPair PredictFootPair(
            CharacterFootSide side,
            AnimationFootMotionRuntimeSample footMotion,
            CharacterFootPlacementAnimatedFootPose animatedFoot,
            CharacterFullBodyIkGoal goal,
            in CommittedLocomotionPlanarMotionTimeline timeline,
            float currentSegmentRemainingSeconds,
            CharacterFutureBodyTranslation bodyTrajectory,
            in CharacterFootPlacementFrameInput frame,
            in CharacterFootLandingSnapshot landingSnapshot,
            bool plantVerificationRequired,
            CharacterFootLandingObservationPagePool observationPool,
            CharacterFootLandingObservationPage committedObservation,
            out CharacterFootLandingObservationPage pendingObservation)
        {
            pendingObservation = committedObservation;
            Vector3 currentSole =
                (animatedFoot.HeelPosition + animatedFoot.ToePosition) * 0.5f;
            AnimationFootMotionEventFrame events = footMotion.Events;
            AnimationFootMotionEventOccurrence current = events.CurrentContact;
            AnimationFootMotionEventOccurrence next = events.NextLanding;
            bool hasNextCandidate = IsPredictiveLanding(
                in footMotion,
                in next,
                landingSnapshot.LastLandingEventIdentity,
                m_Settings.LandingPrediction.MaximumPredictionTimeSeconds);
            CharacterFootLandingStepSource selectedSource;
            CharacterFootLandingPredictionResult selected;
            if (plantVerificationRequired)
            {
                selectedSource = CharacterFootLandingStepSource.FormalCurrentContact;
                selected = PredictEvent(
                    side,
                    in footMotion,
                    in current,
                    selectedSource,
                    0f,
                    true,
                    currentSole,
                    goal,
                    in timeline,
                    currentSegmentRemainingSeconds,
                    bodyTrajectory,
                    in frame,
                    observationPool,
                    committedObservation,
                    out pendingObservation);
            }
            else if (hasNextCandidate)
            {
                selectedSource = CharacterFootLandingStepSource.FormalNextLanding;
                selected = PredictEvent(
                    side,
                    in footMotion,
                    in next,
                    selectedSource,
                    events.TimeToLandingSeconds,
                    false,
                    currentSole,
                    goal,
                    in timeline,
                    currentSegmentRemainingSeconds,
                    bodyTrajectory,
                    in frame,
                    observationPool,
                    committedObservation,
                    out pendingObservation);
            }
            else
            {
                selectedSource = CharacterFootLandingStepSource.None;
                selected = RejectedEvent(
                    side,
                    CharacterFootLandingPredictionRejectReason.StepUnavailable,
                    selectedSource,
                    in next,
                    events.TimeToLandingSeconds,
                    timeline.IsValid ? timeline.Generation : 0,
                    currentSole,
                    default,
                    default,
                    goal);
            }
            return new CharacterFootLandingPredictionPair(
                selected,
                selectedSource);
        }

        static bool IsPredictiveLanding(
            in AnimationFootMotionRuntimeSample footMotion,
            in AnimationFootMotionEventOccurrence next,
            ulong lastLandingEventIdentity,
            float maximumPredictionTimeSeconds) =>
            footMotion.IsValid &&
            footMotion.HasPredictiveLanding &&
            next.IsBound &&
            footMotion.TimeToLandingSeconds > 0.000001f &&
            footMotion.TimeToLandingSeconds <= maximumPredictionTimeSeconds &&
            next.Identity != lastLandingEventIdentity;

        CharacterFootLandingPredictionResult PredictEvent(
            CharacterFootSide side,
            in AnimationFootMotionRuntimeSample footMotion,
            in AnimationFootMotionEventOccurrence landingEvent,
            CharacterFootLandingStepSource stepSource,
            float timeToLandingSeconds,
            bool currentContact,
            Vector3 currentSole,
            CharacterFullBodyIkGoal goal,
            in CommittedLocomotionPlanarMotionTimeline timeline,
            float currentSegmentRemainingSeconds,
            CharacterFutureBodyTranslation bodyTrajectory,
            in CharacterFootPlacementFrameInput frame,
            CharacterFootLandingObservationPagePool observationPool,
            CharacterFootLandingObservationPage committedObservation,
            out CharacterFootLandingObservationPage pendingObservation)
        {
            pendingObservation = committedObservation;
            ulong trajectoryGeneration = timeline.IsValid ? timeline.Generation : 0;
            if (!footMotion.IsValid)
            {
                return RejectedEvent(
                    side,
                    CharacterFootLandingPredictionRejectReason.StepUnavailable,
                    stepSource,
                    in landingEvent,
                    timeToLandingSeconds,
                    trajectoryGeneration,
                    currentSole,
                    default,
                    default,
                    goal);
            }
            if (!landingEvent.IsBound)
            {
                return RejectedEvent(
                    side,
                    CharacterFootLandingPredictionRejectReason.StepIdentityMismatch,
                    stepSource,
                    in landingEvent,
                    timeToLandingSeconds,
                    trajectoryGeneration,
                    currentSole,
                    default,
                    default,
                    goal);
            }
            CharacterFootLandingPredictionSettings settings =
                m_Settings.LandingPrediction;
            if (!float.IsFinite(timeToLandingSeconds) ||
                timeToLandingSeconds < 0f ||
                timeToLandingSeconds > settings.MaximumPredictionTimeSeconds)
            {
                return RejectedEvent(
                    side,
                    CharacterFootLandingPredictionRejectReason.LandingTimeInvalid,
                    stepSource,
                    in landingEvent,
                    timeToLandingSeconds,
                    trajectoryGeneration,
                    currentSole,
                    default,
                    default,
                    goal);
            }
            if (!currentContact && !timeline.IsValid)
            {
                return RejectedEvent(
                    side,
                    CharacterFootLandingPredictionRejectReason.MotionTimelineUnavailable,
                    stepSource,
                    in landingEvent,
                    timeToLandingSeconds,
                    0,
                    currentSole,
                    default,
                    default,
                    goal);
            }
            bool requiresFutureBodyTranslation =
                !currentContact && timeToLandingSeconds > 0.000001f;
            if (requiresFutureBodyTranslation && bodyTrajectory == null)
            {
                return RejectedEvent(
                    side,
                    CharacterFootLandingPredictionRejectReason.FutureBodyTranslationUnavailable,
                    stepSource,
                    in landingEvent,
                    timeToLandingSeconds,
                    trajectoryGeneration,
                    currentSole,
                    default,
                    default,
                    goal);
            }
            if (requiresFutureBodyTranslation &&
                bodyTrajectory.DurationSeconds + 0.0001f < timeToLandingSeconds)
            {
                return RejectedEvent(
                    side,
                    CharacterFootLandingPredictionRejectReason.FutureBodyTranslationRangeInvalid,
                    stepSource,
                    in landingEvent,
                    timeToLandingSeconds,
                    trajectoryGeneration,
                    currentSole,
                    default,
                    default,
                    goal);
            }

            CharacterFutureBodyTranslationSample bodyTranslation =
                bodyTrajectory != null
                    ? bodyTrajectory.Evaluate(timeToLandingSeconds)
                    : default;
            Vector3 rawLanding = currentContact
                ? currentSole
                : CharacterFootLandingPredictor.ProjectRawLanding(
                    frame.Body.VisiblePosition,
                    frame.Body.VisibleRotation,
                    in bodyTranslation,
                    landingEvent.RootLocalLanding);
            Vector3 componentUp = frame.Body.VisibleRotation * Vector3.up;
            CharacterFootLandingObservationRefreshMode refreshMode =
                currentContact
                    ? CharacterFootLandingObservationRefreshMode
                        .ForcedPlantVerification
                    : footMotion.LockMode ==
                      AnimationFootStepObservationLockMode.Sliding
                        ? CharacterFootLandingObservationRefreshMode
                            .ChangedSlidingAdmissionInput
                        : CharacterFootLandingObservationRefreshMode.Thresholded;
            CharacterFootLandingObservationResult observation =
                CharacterFootLandingPredictor.ResolveObservation(
                    side,
                    landingEvent.Identity,
                    landingEvent.SourceSampleIdentity,
                    landingEvent.LandingCycle,
                    rawLanding,
                    componentUp,
                    refreshMode,
                    m_Settings.ProfileRevision,
                    in settings,
                    m_WorldQuery,
                    observationPool,
                    committedObservation,
                    out pendingObservation);
            CharacterFootLandingObservationPage observationPage =
                observation.Page;
            CharacterFootLandingQueryResult queryResult =
                observationPage.Result;
            bool accepted = queryResult.Accepted;
            CharacterFootLandingQueryRejectReason queryRejectReason =
                queryResult.RejectReason;
            CharacterFootLandingSupport support = queryResult.Support;
            CharacterFootLandingQuerySelectionDiagnostics querySelection =
                queryResult.SelectionDiagnostics;
            var observationDiagnostics =
                new CharacterFootLandingObservationDiagnostics(in observation);
            return new CharacterFootLandingPredictionResult(
                side,
                accepted
                    ? CharacterFootLandingPredictionState.Accepted
                    : CharacterFootLandingPredictionState.Rejected,
                accepted
                    ? CharacterFootLandingPredictionRejectReason.None
                    : queryRejectReason ==
                      CharacterFootLandingQueryRejectReason.CapacityExceeded
                        ? CharacterFootLandingPredictionRejectReason
                            .GroundQueryCapacityExceeded
                        : CharacterFootLandingPredictionRejectReason
                            .GroundQueryMissed,
                stepSource,
                landingEvent.Identity,
                trajectoryGeneration,
                1f,
                timeToLandingSeconds,
                landingEvent.RootLocalLanding,
                bodyTrajectory != null,
                bodyTrajectory != null
                    ? bodyTrajectory.SourceIdentity
                    : string.Empty,
                in bodyTranslation,
                currentSole,
                rawLanding,
                observationDiagnostics,
                observationPage.Query,
                support,
                querySelection,
                goal);
        }

        static CharacterFootLandingPredictionResult RejectedEvent(
            CharacterFootSide side,
            CharacterFootLandingPredictionRejectReason reason,
            CharacterFootLandingStepSource stepSource,
            in AnimationFootMotionEventOccurrence landingEvent,
            float timeToLandingSeconds,
            ulong trajectoryGeneration,
            Vector3 currentSole,
            Vector3 rawLanding,
            CharacterFootPlacementQueryRequest query,
            CharacterFullBodyIkGoal goal) =>
            new CharacterFootLandingPredictionResult(
                side,
                CharacterFootLandingPredictionState.Rejected,
                reason,
                stepSource,
                landingEvent.IsBound ? landingEvent.Identity : 0,
                trajectoryGeneration,
                landingEvent.IsBound ? 1f : 0f,
                landingEvent.IsBound && float.IsFinite(timeToLandingSeconds)
                    ? Mathf.Max(0f, timeToLandingSeconds)
                    : 0f,
                landingEvent.IsBound
                    ? landingEvent.RootLocalLanding
                    : default,
                false,
                string.Empty,
                default,
                currentSole,
                rawLanding,
                default,
                query,
                default,
                default,
                goal);

        CharacterFutureBodyTranslation ResolveBodyTrajectory(
            CharacterFootPlacementBank bank,
            AnimationFootMotionRuntimeSample leftFootMotion,
            AnimationFootMotionRuntimeSample rightFootMotion,
            in CommittedLocomotionPlanarMotionTimeline timeline,
            float currentSegmentRemainingSeconds,
            float presentationDeltaSeconds,
            CharacterBodyPresentationFrame body)
        {
            CharacterFootLandingPredictionSettings settings =
                m_Settings.LandingPrediction;
            Vector2 rawCurrentVelocity = new Vector2(
                body.TargetVelocity.x,
                body.TargetVelocity.z);
            Vector2 rawContinuationVelocity = timeline.IsValid
                ? new Vector2(
                    timeline.ContinuationVelocityX,
                    timeline.ContinuationVelocityZ)
                : default;
            string predictionSourceIdentity =
                m_FutureBodyTranslationSource?.PredictionSourceIdentity ??
                string.Empty;
            CharacterFootPredictionMotionResult predictionMotion =
                CharacterFootPredictionMotionRuntime.Evaluate(
                    ref bank.PredictionMotion,
                    timeline.IsValid,
                    timeline.IsValid ? timeline.Generation : 0,
                    body.ResetSequence,
                    predictionSourceIdentity,
                    rawCurrentVelocity,
                    rawContinuationVelocity,
                    presentationDeltaSeconds,
                    in settings);
            bank.PredictionMotionResult = predictionMotion;
            float maximum = m_Settings.LandingPrediction.MaximumPredictionTimeSeconds;
            float leftTime = ResolvePredictionTime(leftFootMotion, maximum);
            float rightTime = ResolvePredictionTime(rightFootMotion, maximum);
            float duration = Mathf.Max(leftTime, rightTime);
            if (!predictionMotion.IsValid ||
                m_FutureBodyTranslationSource == null)
            {
                bank.BodyTrajectory.Clear();
                bank.HasBodyTrajectoryAttempt = false;
                bank.BodyTrajectoryPredictionMotionRevision = 0;
                return null;
            }
            if (duration <= 0f)
            {
                return null;
            }

            bool sameCommittedBody = bank.HasBodyTrajectoryAttempt &&
                                     bank.BodyTrajectoryTick == body.CurrentTick &&
                                     bank.BodyTrajectoryResetSequence == body.ResetSequence &&
                                     bank.BodyTrajectoryGeneration == timeline.Generation &&
                                     bank.BodyTrajectoryAuthorityTick == timeline.AuthorityTick.Value &&
                                     bank.BodyTrajectoryPredictionMotionRevision ==
                                     predictionMotion.Revision;
            if (sameCommittedBody &&
                duration <= bank.BodyTrajectoryRequestedDuration + 0.0001f)
            {
                return bank.BodyTrajectory.IsAvailable
                    ? bank.BodyTrajectory
                    : null;
            }

            bank.HasBodyTrajectoryAttempt = true;
            bank.BodyTrajectoryTick = body.CurrentTick;
            bank.BodyTrajectoryResetSequence = body.ResetSequence;
            bank.BodyTrajectoryGeneration = timeline.Generation;
            bank.BodyTrajectoryAuthorityTick = timeline.AuthorityTick.Value;
            bank.BodyTrajectoryPredictionMotionRevision =
                predictionMotion.Revision;
            bank.BodyTrajectoryRequestedDuration = duration;
            bank.BodyTrajectory.Clear();

            var request = new CharacterFutureBodyTranslationRequest(
                m_ActorId,
                duration,
                predictionMotion.StableCurrentVelocity.x,
                predictionMotion.StableCurrentVelocity.y,
                predictionMotion.StableContinuationVelocity.x,
                predictionMotion.StableContinuationVelocity.y,
                currentSegmentRemainingSeconds,
                timeline.HasContinuation,
                leftTime,
                0f,
                rightTime,
                0f);
            if (m_FutureBodyTranslationSource.TryPredict(
                    in request,
                    bank.BodyTrajectory))
                return bank.BodyTrajectory;
            return null;
        }

        static float ResolvePredictionTime(
            AnimationFootMotionRuntimeSample step,
            float maximum) =>
            step.IsAuthoritative && step.HasConsistentLandingEventIdentity &&
            step.TimeToLandingSeconds > 0.000001f &&
            step.TimeToLandingSeconds <= maximum
                ? step.TimeToLandingSeconds
                : 0f;

        static float ResolveCurrentSegmentRemainingSeconds(
            CommittedLocomotionPlanarMotionTimeline timeline,
            CharacterBodyPresentationFrame body)
        {
            if (timeline.CurrentSegmentDurationTicks == 0)
                return float.PositiveInfinity;
            ulong elapsedWholeTicks = body.CurrentTick > timeline.AuthorityTick.Value
                ? body.CurrentTick - timeline.AuthorityTick.Value
                : 0;
            double elapsedTicks = elapsedWholeTicks + body.SampleAlpha;
            double remainingTicks = Math.Max(
                0d,
                timeline.CurrentSegmentDurationTicks - elapsedTicks);
            return (float)(remainingTicks / timeline.TickRate);
        }

        static CharacterFootActionOccupancy ResolveActionOccupancy(
            in CharacterFootPlacementPoseInput pose,
            CharacterFootSide side)
        {
            ulong actionInstanceIdentity = 0;
            float selectedWeight = 0f;
            for (int i = 0; i < pose.ContributionCount; i++)
            {
                AnimationPoseSourceContribution contribution = pose.Contributions[i];
                if (contribution.Kind != AnimationPoseContributionKind.Live ||
                    contribution.SourceId.SourceActionInstanceId == 0)
                {
                    continue;
                }
                float weight = side == CharacterFootSide.Left
                    ? contribution.LeftFootWeight
                    : contribution.RightFootWeight;
                ulong candidateIdentity = contribution.SourceId.SourceActionInstanceId;
                if (weight <= 0.0001f ||
                    weight < selectedWeight ||
                    Mathf.Abs(weight - selectedWeight) <= 0.0001f &&
                    actionInstanceIdentity != 0 &&
                    candidateIdentity >= actionInstanceIdentity)
                {
                    continue;
                }
                actionInstanceIdentity = candidateIdentity;
                selectedWeight = weight;
            }
            return new CharacterFootActionOccupancy(
                actionInstanceIdentity,
                selectedWeight);
        }

        static Vector3 ResolveWeightedGoalSole(
            CharacterFootPlacementAnimatedFootPose foot,
            in CharacterFullBodyIkGoal goal,
            Transform poseRoot)
        {
            Vector3 originalSole = foot.HeelPosition * 0.5f + foot.ToePosition * 0.5f;
            if (poseRoot == null || goal.PositionWeight <= 0f)
                return originalSole;
            Vector3 targetAnkle = poseRoot.TransformPoint(goal.ComponentPosition);
            Vector3 effectiveAnkle = Vector3.LerpUnclamped(
                foot.AnklePosition,
                targetAnkle,
                goal.PositionWeight);
            Quaternion targetRotation =
                (poseRoot.rotation * goal.ComponentRotation).normalized;
            Quaternion effectiveRotation = Quaternion.Slerp(
                foot.AnkleRotation,
                targetRotation,
                goal.RotationWeight).normalized;
            CharacterFootPlacementSoleContactPose contacts =
                foot.ResolveSoleContacts(
                    effectiveAnkle,
                    effectiveRotation);
            return (contacts.HeelPosition + contacts.ToePosition) * 0.5f;
        }

        static CharacterFootGoalOwnershipLossReason ResolveFootGoalOwnershipLoss(
            bool grounded,
            bool authoritative)
        {
            CharacterFootGoalOwnershipLossReason reason =
                CharacterFootGoalOwnershipLossReason.None;
            if (!grounded)
                reason |= CharacterFootGoalOwnershipLossReason.Ungrounded;
            if (!authoritative)
            {
                reason |= CharacterFootGoalOwnershipLossReason
                    .SourceLineageInvalidated;
            }
            return reason;
        }

        static bool TryResolvePreviousVisibleOutput(
            CharacterFootPlacementBank committed,
            CharacterFootSide side,
            out Vector3 point)
        {
            point = default;
            if (committed == null || !committed.HasFrame ||
                !committed.HasVisibleFootOutputs)
                return false;
            point = side == CharacterFootSide.Left
                ? committed.LeftVisibleSole
                : committed.RightVisibleSole;
            if (!CharacterPoseConstraintMath.IsFinite(point))
            {
                point = default;
                return false;
            }
            return true;
        }

        static bool IsLandingReachCandidate(
            in AnimationFootMotionRuntimeSample step,
            in CharacterFootSwingMotionResult motion,
            in CharacterResolvedFootResult resolved)
        {
            if (!resolved.LandingReachRequest.IsAvailable ||
                resolved.GoalWeight <= CharacterPoseConstraintMath.Epsilon ||
                motion.LandingEventIdentity !=
                resolved.LandingReachRequest.EventIdentity)
            {
                return false;
            }
            if (motion.ConstraintState == CharacterFootConstraintState.Landing ||
                motion.ConstraintState == CharacterFootConstraintState.Locked ||
                motion.ConstraintState == CharacterFootConstraintState.Releasing)
                return resolved.ContactReference.IsAvailable;
            return step.IsAuthoritative &&
                   step.HasConsistentLandingEventIdentity &&
                   step.HasPredictiveLanding &&
                   motion.Accepted &&
                   motion.LandingEventIdentity == step.LandingEventIdentity;
        }

        static CharacterFullBodyIkGoal ClampFootGoalToReach(
            in CharacterFullBodyIkGoal goal,
            Vector3 hipPosition,
            Vector3 originalAnklePosition,
            float legLength,
            float compressionReserve,
            Transform root,
            out float clampDistance)
        {
            clampDistance = 0f;
            if (goal.PositionWeight <= CharacterPoseConstraintMath.Epsilon)
                return goal;
            float usableLength = legLength - compressionReserve;
            if (root == null || usableLength <=
                CharacterPoseConstraintMath.Epsilon)
            {
                throw new InvalidOperationException(
                    "Landing Reach clamp input is invalid.");
            }
            Vector3 target = root.TransformPoint(goal.ComponentPosition);
            Vector3 effectiveTarget = Vector3.LerpUnclamped(
                originalAnklePosition,
                target,
                goal.PositionWeight);
            Vector3 hipToTarget = effectiveTarget - hipPosition;
            float distance = hipToTarget.magnitude;
            if (distance <= usableLength)
                return goal;
            Vector3 clampedEffectiveTarget = hipPosition +
                                             hipToTarget / distance *
                                             usableLength;
            Vector3 clampedTarget = originalAnklePosition +
                                    (clampedEffectiveTarget -
                                     originalAnklePosition) /
                                    goal.PositionWeight;
            clampDistance = Vector3.Distance(
                effectiveTarget,
                clampedEffectiveTarget);
            return new CharacterFullBodyIkGoal(
                goal.Slot,
                root.InverseTransformPoint(clampedTarget),
                goal.ComponentRotation,
                goal.PositionWeight,
                goal.RotationWeight,
                goal.Application,
                goal.SourceKind,
                goal.DiagnosticMetadataIndex);
        }

        static CharacterFullBodyIkGoal CreatePelvisGoal() =>
            CreatePelvisGoal(default, null);

        static CharacterFullBodyIkGoal CreatePelvisGoal(
            in CharacterFootStrideHipsResult strideHips,
            Transform poseRoot)
        {
            Vector3 translation = default;
            float weight = 0f;
            if (strideHips.ProducesPelvisGoal && poseRoot != null)
            {
                translation = poseRoot.InverseTransformVector(strideHips.PelvisDelta);
                weight = strideHips.PositionWeight;
            }
            return new CharacterFullBodyIkGoal(
                CharacterFullBodyIkEffectorSlot.PelvisPreSolveTranslation,
                translation,
                Quaternion.identity,
                weight,
                0f,
                CharacterFullBodyIkGoalApplication.PelvisPreSolveTranslation,
                CharacterFullBodyIkGoalSourceKind.FootPlacement,
                -1);
        }

        CharacterFullBodyIkGoal CreateFootGoal(
            CharacterFootSide side,
            CharacterFootPlacementAnimatedFootPose foot)
        {
            CharacterResolvedFootResult result = default;
            return CreateFootGoal(side, foot, in result);
        }

        CharacterFullBodyIkGoal CreateFootGoal(
            CharacterFootSide side,
            CharacterFootPlacementAnimatedFootPose foot,
            in CharacterResolvedFootResult result)
        {
            Transform root = m_Rig.PoseRoot;
            bool hasEffectiveOutput =
                                      result.Outcome == CharacterFootResolvedOutcome.Ready &&
                                      (result.GoalWeight >
                                           CharacterPoseConstraintMath.Epsilon ||
                                       result.RotationWeight >
                                           CharacterPoseConstraintMath.Epsilon);
            Vector3 anklePosition = hasEffectiveOutput
                ? result.FinalAnkle
                : foot.AnklePosition;
            float positionWeight = hasEffectiveOutput
                ? result.GoalWeight
                : 0f;
            Quaternion ankleRotation = hasEffectiveOutput
                ? result.FinalRotation
                : foot.AnkleRotation;
            float rotationWeight = hasEffectiveOutput
                ? result.RotationWeight
                : 0f;
            return new CharacterFullBodyIkGoal(
                side == CharacterFootSide.Left
                    ? CharacterFullBodyIkEffectorSlot.LeftFoot
                    : CharacterFullBodyIkEffectorSlot.RightFoot,
                root.InverseTransformPoint(anklePosition),
                (Quaternion.Inverse(root.rotation) * ankleRotation).normalized,
                positionWeight,
                rotationWeight,
                CharacterFullBodyIkGoalApplication.FootPlacementEffectorTarget,
                CharacterFullBodyIkGoalSourceKind.FootPlacement,
                -1);
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterFootPlacementModule));
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            CharacterFootLandingPredictionDebugRegistry.Remove(
                m_Rig.VisualRoot.GetInstanceID());
            m_LeftGroundPath.Reset();
            m_RightGroundPath.Reset();
            m_LeftLandingObservation.Reset();
            m_RightLandingObservation.Reset();
            m_LeftCurrentSupport.Reset();
            m_RightCurrentSupport.Reset();
        }
    }
}
