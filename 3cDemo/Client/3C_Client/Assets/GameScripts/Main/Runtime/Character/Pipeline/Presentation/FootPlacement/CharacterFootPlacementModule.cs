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
        internal CharacterFootGroundPathPage LeftGroundPath;
        internal CharacterFootGroundPathPage RightGroundPath;
        internal CharacterFootLandingObservationPage LeftLandingObservation;
        internal CharacterFootLandingObservationPage RightLandingObservation;
        internal readonly CharacterFutureBodyTranslation BodyTrajectory =
            new CharacterFutureBodyTranslation();
        internal ulong BodyTrajectoryTick;
        internal ulong BodyTrajectoryResetSequence;
        internal ulong BodyTrajectoryGeneration;
        internal ulong BodyTrajectoryAuthorityTick;
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
                LeftGroundPath = null;
                RightGroundPath = null;
                LeftLandingObservation = null;
                RightLandingObservation = null;
                BodyTrajectory.CopyFrom(committed.BodyTrajectory);
                BodyTrajectoryTick = committed.BodyTrajectoryTick;
                BodyTrajectoryResetSequence = committed.BodyTrajectoryResetSequence;
                BodyTrajectoryGeneration = committed.BodyTrajectoryGeneration;
                BodyTrajectoryAuthorityTick = committed.BodyTrajectoryAuthorityTick;
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
            ResolvedFeet = default;
            StrideHips = default;
            PelvisGoal = default;
            LeftGoal = default;
            RightGoal = default;
            Diagnostics.Clear();
            FrameSequence = 0;
            CompletionIdentity = 0;
            RecordDiagnostics = false;
            HasFrame = false;
        }

        internal void Reset()
        {
            LeftFoot = default;
            RightFoot = default;
            PelvisSpring.Clear();
            PrimarySupport.Clear();
            ResolvedFeet = default;
            StrideHips = default;
            PelvisGoal = default;
            LeftGoal = default;
            RightGoal = default;
            LeftGroundPath = null;
            RightGroundPath = null;
            LeftLandingObservation = null;
            RightLandingObservation = null;
            BodyTrajectory.Clear();
            BodyTrajectoryTick = 0;
            BodyTrajectoryResetSequence = 0;
            BodyTrajectoryGeneration = 0;
            BodyTrajectoryAuthorityTick = 0;
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
            AnimationFootMotionRuntimeSample selectedStep,
            CharacterFootLandingStepSource selectedSource)
        {
            Selected = selected;
            SelectedStep = selectedStep;
            SelectedSource = selectedSource;
        }

        internal CharacterFootLandingPredictionResult Selected { get; }
        internal AnimationFootMotionRuntimeSample SelectedStep { get; }
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
        internal bool IsOccupied => ActionInstanceIdentity != 0 && Weight > 0.0001f;
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
            CharacterFutureBodyTranslation bodyTrajectory = ResolveBodyTrajectory(
                bank,
                leftCurrentStep,
                rightCurrentStep,
                in timeline,
                currentSegmentRemainingSeconds,
                frame.Body);
            Vector3 componentUp = frame.Body.VisibleRotation * Vector3.up;

            CharacterFootLandingSnapshot leftLanding =
                CharacterFootLandingRuntime.ProjectBeforePrediction(
                    in bank.LeftFoot,
                    in leftCurrentStep);
            CharacterFootLandingSnapshot rightLanding =
                CharacterFootLandingRuntime.ProjectBeforePrediction(
                    in bank.RightFoot,
                    in rightCurrentStep);
            CharacterFootLandingPredictionPair leftPair = PredictFootPair(
                CharacterFootSide.Left,
                leftCurrentStep,
                pose.Left,
                leftGoal,
                in timeline,
                currentSegmentRemainingSeconds,
                bodyTrajectory,
                in frame,
                leftLanding.LastLandingEventIdentity,
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
                rightLanding.LastLandingEventIdentity,
                m_RightLandingObservation,
                committedBank?.RightLandingObservation,
                out bank.RightLandingObservation);
            CharacterFootLandingPredictionResult left = leftPair.Selected;
            AnimationFootMotionRuntimeSample leftSelectedStep = leftPair.SelectedStep;
            CharacterFootLandingPredictionResult right = rightPair.Selected;
            AnimationFootMotionRuntimeSample rightSelectedStep = rightPair.SelectedStep;
            leftLanding = CharacterFootLandingRuntime.ProjectAfterPrediction(
                in bank.LeftFoot,
                in leftCurrentStep,
                in leftSelectedStep,
                in left,
                m_Settings.FootMotion);
            rightLanding = CharacterFootLandingRuntime.ProjectAfterPrediction(
                in bank.RightFoot,
                in rightCurrentStep,
                in rightSelectedStep,
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
            bool hasLeftContactLanding = leftLanding.HasPromotedLanding;
            CharacterFootGroundPathLanding leftContactLanding =
                hasLeftContactLanding ? leftLanding.PromotedLanding : default;
            if (!hasLeftContactLanding)
            {
                hasLeftContactLanding = leftLanding.TryResolveLanding(
                    leftSelectedStep.LandingEventIdentity,
                    out leftContactLanding);
            }
            bool hasRightContactLanding = rightLanding.HasPromotedLanding;
            CharacterFootGroundPathLanding rightContactLanding =
                hasRightContactLanding ? rightLanding.PromotedLanding : default;
            if (!hasRightContactLanding)
            {
                hasRightContactLanding = rightLanding.TryResolveLanding(
                    rightSelectedStep.LandingEventIdentity,
                    out rightContactLanding);
            }
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
                    leftLanding.NextSwingPredictionError,
                    leftSelectedStep.LockWeight)
                .WithPlantConfidence(
                    leftSelectedStep.LockWeight);
            CharacterFootSwingMotionResult rightSwingMotion =
                CharacterFootSwingMotionBuilder.Build(
                    pose.Right,
                    in rightSelectedStep,
                    footPlacementWeight,
                    componentUp,
                    in rightGroundPath,
                    true,
                    formalFootFrame.Right.FootHeight,
                    rightLanding.NextSwingPredictionError,
                    rightSelectedStep.LockWeight)
                .WithPlantConfidence(
                    rightSelectedStep.LockWeight);
            bool hasSelectedSwing = CharacterFootStrideHipsBuilder.TrySelectSwing(
                in leftSelectedStep,
                in rightSelectedStep,
                in leftSwingMotion,
                in rightSwingMotion,
                out CharacterFootSide selectedSwingSide);
            Transform goalRoot = m_Rig.PoseRoot;
            var leftConstraintFrame = new CharacterFootStateFrame(
                frame.RenderFrame,
                frame.Pose.CompletionIdentity,
                new FixedString64Bytes(m_Rig.Rig.RigId),
                new FixedString64Bytes(m_Rig.Rig.RigRevision),
                pose.Left,
                in leftSwingMotion,
                hasLeftContactLanding,
                in leftContactLanding,
                IsHardFootGoalOwnershipLoss(facts.Grounded, in leftAction),
                footPlacementWeight,
                componentUp,
                frame.PresentationDeltaSeconds,
                m_Settings.FootMotion);
            var rightConstraintFrame = new CharacterFootStateFrame(
                frame.RenderFrame,
                frame.Pose.CompletionIdentity,
                new FixedString64Bytes(m_Rig.Rig.RigId),
                new FixedString64Bytes(m_Rig.Rig.RigRevision),
                pose.Right,
                in rightSwingMotion,
                hasRightContactLanding,
                in rightContactLanding,
                IsHardFootGoalOwnershipLoss(facts.Grounded, in rightAction),
                footPlacementWeight,
                componentUp,
                frame.PresentationDeltaSeconds,
                m_Settings.FootMotion);
            var leftEvaluation = new CharacterFootStateEvaluation(
                CharacterFootSide.Left,
                in leftCurrentStep,
                in leftSelectedStep,
                in left,
                in leftConstraintFrame);
            var rightEvaluation = new CharacterFootStateEvaluation(
                CharacterFootSide.Right,
                in rightCurrentStep,
                in rightSelectedStep,
                in right,
                in rightConstraintFrame);
            CharacterResolvedFootResult leftResolved =
                CharacterFootLifecycle.Evaluate(
                    ref bank.LeftFoot,
                    in leftEvaluation,
                    out CharacterFootSwingMotionResult leftFootMotion);
            CharacterResolvedFootResult rightResolved =
                CharacterFootLifecycle.Evaluate(
                    ref bank.RightFoot,
                    in rightEvaluation,
                    out CharacterFootSwingMotionResult rightFootMotion);
            var resolvedPair = new CharacterResolvedFootPair(
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
                leftAction.IsOccupied || rightAction.IsOccupied,
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
            CharacterFootStrideHipsResult strideHips = ResolveStrideHips(
                in strideIntent,
                in resolvedPair,
                in primarySupport,
                in pelvisFrame,
                ref bank.PelvisSpring);
            if (facts.Grounded &&
                !leftAction.IsOccupied &&
                !rightAction.IsOccupied)
            {
                bool leftLandingReach = IsLandingReachCandidate(
                    in leftSelectedStep,
                    in leftFootMotion,
                    in leftResolved);
                bool rightLandingReach = IsLandingReachCandidate(
                    in rightSelectedStep,
                    in rightFootMotion,
                    in rightResolved);
                if (leftLandingReach || rightLandingReach)
                {
                    strideHips = CharacterFootStrideHipsBuilder.ApplyLandingReach(
                        in strideHips,
                        leftLandingReach,
                        pose.Left.HipPosition,
                        leftResolved.FinalAnkle,
                        m_Rig.LeftLegLength,
                        rightLandingReach,
                        pose.Right.HipPosition,
                        rightResolved.FinalAnkle,
                        m_Rig.RightLegLength,
                        componentUp,
                        footPlacementWeight,
                        m_Settings.FootMotion,
                        ref bank.PelvisSpring);
                }
            }
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
                    in footStepObservation);
                var leftDiagnostics =
                    new CharacterFootLandingPredictionFootDiagnostics(
                        in left,
                        pose.Left,
                        new CharacterFootStepCandidateSelectionDiagnostics(
                            leftCurrentStep,
                            leftLanding.LastLandingEventIdentity,
                            leftPair.SelectedSource,
                            leftSelectedStep.IsValid
                                ? leftSelectedStep.LandingEventIdentity
                                : 0,
                            m_Settings.LandingPrediction
                                .MaximumPredictionTimeSeconds));
                var rightDiagnostics =
                    new CharacterFootLandingPredictionFootDiagnostics(
                        in right,
                        pose.Right,
                        new CharacterFootStepCandidateSelectionDiagnostics(
                            rightCurrentStep,
                            rightLanding.LastLandingEventIdentity,
                            rightPair.SelectedSource,
                            rightSelectedStep.IsValid
                                ? rightSelectedStep.LandingEventIdentity
                                : 0,
                            m_Settings.LandingPrediction
                                .MaximumPredictionTimeSeconds));
                var primarySupportDiagnostics =
                    new CharacterFootPrimarySupportDiagnostics(in primarySupport);
                var strideDiagnostics =
                    new CharacterFootStrideHipsDiagnostics(in strideHips);
                var diagnostics = new CharacterFootLandingPredictionDiagnostics(
                    frame.RenderFrame,
                    frame.Pose.CompletionIdentity,
                    m_Rig.VisualRoot.GetInstanceID(),
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

        CharacterFootLandingPredictionPair PredictFootPair(
            CharacterFootSide side,
            AnimationFootMotionRuntimeSample footMotion,
            CharacterFootPlacementAnimatedFootPose animatedFoot,
            CharacterFullBodyIkGoal goal,
            in CommittedLocomotionPlanarMotionTimeline timeline,
            float currentSegmentRemainingSeconds,
            CharacterFutureBodyTranslation bodyTrajectory,
            in CharacterFootPlacementFrameInput frame,
            ulong lastLandingEventIdentity,
            CharacterFootLandingObservationPagePool observationPool,
            CharacterFootLandingObservationPage committedObservation,
            out CharacterFootLandingObservationPage pendingObservation)
        {
            pendingObservation = committedObservation;
            Vector3 currentSole =
                (animatedFoot.HeelPosition + animatedFoot.ToePosition) * 0.5f;
            bool hasCandidate = IsPredictiveLanding(
                footMotion,
                lastLandingEventIdentity,
                m_Settings.LandingPrediction.MaximumPredictionTimeSeconds);
            CharacterFootLandingPredictionResult selected = hasCandidate
                ? PredictStep(
                    side,
                    footMotion,
                    CharacterFootLandingStepSource.FormalNextLanding,
                    currentSole,
                    goal,
                    in timeline,
                    currentSegmentRemainingSeconds,
                    bodyTrajectory,
                    in frame,
                    observationPool,
                    committedObservation,
                    out pendingObservation)
                : Rejected(
                    side,
                    CharacterFootLandingPredictionRejectReason.StepUnavailable,
                    CharacterFootLandingStepSource.None,
                    footMotion,
                    timeline.Generation,
                    currentSole,
                    default,
                    default,
                    goal);
            return new CharacterFootLandingPredictionPair(
                selected,
                hasCandidate ? footMotion : default,
                hasCandidate
                    ? CharacterFootLandingStepSource.FormalNextLanding
                    : CharacterFootLandingStepSource.None);
        }

        static bool IsPredictiveLanding(
            AnimationFootMotionRuntimeSample step,
            ulong lastLandingEventIdentity,
            float maximumPredictionTimeSeconds) =>
            step.IsAuthoritative &&
            step.HasConsistentLandingEventIdentity &&
            (step.IsPreSwing || step.IsSwing) &&
            step.TimeToLandingSeconds > 0.000001f &&
            step.TimeToLandingSeconds <= maximumPredictionTimeSeconds &&
            step.LandingEventIdentity != 0 &&
            step.LandingEventIdentity != lastLandingEventIdentity;

        CharacterFootLandingPredictionResult PredictStep(
            CharacterFootSide side,
            AnimationFootMotionRuntimeSample step,
            CharacterFootLandingStepSource stepSource,
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
            if (!step.IsAuthoritative)
            {
                return Rejected(
                    side,
                    CharacterFootLandingPredictionRejectReason.StepUnavailable,
                    stepSource,
                    step,
                    0,
                    currentSole,
                    default,
                    default,
                    goal);
            }
            if (!step.HasConsistentLandingEventIdentity)
            {
                return Rejected(
                    side,
                    CharacterFootLandingPredictionRejectReason.StepIdentityMismatch,
                    stepSource,
                    step,
                    timeline.Generation,
                    currentSole,
                    default,
                    default,
                    goal);
            }
            CharacterFootLandingPredictionSettings settings = m_Settings.LandingPrediction;
            if (step.TimeToLandingSeconds < 0f ||
                step.TimeToLandingSeconds > settings.MaximumPredictionTimeSeconds)
            {
                return Rejected(
                    side,
                    CharacterFootLandingPredictionRejectReason.LandingTimeInvalid,
                    stepSource,
                    step,
                    0,
                    currentSole,
                    default,
                    default,
                    goal);
            }
            if (!timeline.IsValid)
            {
                return Rejected(
                    side,
                    CharacterFootLandingPredictionRejectReason.MotionTimelineUnavailable,
                    stepSource,
                    step,
                    0,
                    currentSole,
                    default,
                    default,
                    goal);
            }
            bool requiresFutureBodyTranslation = step.TimeToLandingSeconds > 0.000001f;
            if (requiresFutureBodyTranslation && bodyTrajectory == null)
            {
                return Rejected(
                    side,
                    CharacterFootLandingPredictionRejectReason.FutureBodyTranslationUnavailable,
                    stepSource,
                    step,
                    timeline.Generation,
                    currentSole,
                    default,
                    default,
                    goal);
            }

            if (requiresFutureBodyTranslation &&
                bodyTrajectory.DurationSeconds + 0.0001f < step.TimeToLandingSeconds)
            {
                return Rejected(
                    side,
                    CharacterFootLandingPredictionRejectReason.FutureBodyTranslationRangeInvalid,
                    stepSource,
                    step,
                    timeline.Generation,
                    currentSole,
                    default,
                    default,
                    goal);
            }

            CharacterFutureBodyTranslationSample bodyTranslation =
                bodyTrajectory != null
                    ? bodyTrajectory.Evaluate(step.TimeToLandingSeconds)
                    : new CharacterFutureBodyTranslationSample(
                        0f,
                        0f,
                        0f,
                        0f,
                        0f,
                        0f,
                        0f);
            Vector3 componentUp = frame.Body.VisibleRotation * Vector3.up;
            Vector3 rawLanding = CharacterFootLandingPredictor.ProjectRawLanding(
                frame.Body.VisiblePosition,
                frame.Body.VisibleRotation,
                in bodyTranslation,
                step.RootLocalLanding);
            AnimationFootMotionRuntimeFrame formalFootFrame =
                frame.Pose.FootMotion;
            AnimationFootMotionRuntimeSample formalFoot =
                side == CharacterFootSide.Left
                    ? formalFootFrame.Left
                    : formalFootFrame.Right;
            bool contactAcquisitionRefresh =
                formalFootFrame.IsValid &&
                formalFootFrame.CompletionIdentity == frame.Pose.CompletionIdentity &&
                formalFoot.LockMode ==
                AnimationFootStepObservationLockMode.Sliding;
            CharacterFootLandingObservationResult observation =
                CharacterFootLandingPredictor.ResolveObservation(
                side,
                step.LandingEventIdentity,
                step.SourceSampleIdentity,
                step.SourceSampleCycle,
                rawLanding,
                componentUp,
                contactAcquisitionRefresh,
                m_Settings.ProfileRevision,
                in settings,
                m_WorldQuery,
                observationPool,
                committedObservation,
                out pendingObservation);
            CharacterFootLandingObservationPage observationPage = observation.Page;
            CharacterFootLandingQueryResult queryResult = observationPage.Result;
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
                    : queryRejectReason == CharacterFootLandingQueryRejectReason.CapacityExceeded
                        ? CharacterFootLandingPredictionRejectReason.GroundQueryCapacityExceeded
                        : CharacterFootLandingPredictionRejectReason.GroundQueryMissed,
                stepSource,
                step.LandingEventIdentity,
                timeline.Generation,
                1f,
                step.TimeToLandingSeconds,
                step.RootLocalLanding,
                bodyTrajectory != null,
                bodyTrajectory != null ? bodyTrajectory.SourceIdentity : string.Empty,
                in bodyTranslation,
                currentSole,
                rawLanding,
                observationDiagnostics,
                observationPage.Query,
                support,
                querySelection,
                goal);
        }

        CharacterFutureBodyTranslation ResolveBodyTrajectory(
            CharacterFootPlacementBank bank,
            AnimationFootMotionRuntimeSample leftFootMotion,
            AnimationFootMotionRuntimeSample rightFootMotion,
            in CommittedLocomotionPlanarMotionTimeline timeline,
            float currentSegmentRemainingSeconds,
            CharacterBodyPresentationFrame body)
        {
            float maximum = m_Settings.LandingPrediction.MaximumPredictionTimeSeconds;
            float leftTime = ResolvePredictionTime(leftFootMotion, maximum);
            float rightTime = ResolvePredictionTime(rightFootMotion, maximum);
            float duration = Mathf.Max(leftTime, rightTime);
            if (duration <= 0f || !timeline.IsValid ||
                m_FutureBodyTranslationSource == null)
            {
                return null;
            }

            bool sameCommittedBody = bank.HasBodyTrajectoryAttempt &&
                                     bank.BodyTrajectoryTick == body.CurrentTick &&
                                     bank.BodyTrajectoryResetSequence == body.ResetSequence &&
                                     bank.BodyTrajectoryGeneration == timeline.Generation &&
                                     bank.BodyTrajectoryAuthorityTick == timeline.AuthorityTick.Value;
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
            bank.BodyTrajectoryRequestedDuration = duration;
            bank.BodyTrajectory.Clear();

            var request = new CharacterFutureBodyTranslationRequest(
                m_ActorId,
                duration,
                body.TargetVelocity.x,
                body.TargetVelocity.z,
                timeline.ContinuationVelocityX,
                timeline.ContinuationVelocityZ,
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

        static CharacterFootLandingPredictionResult Rejected(
            CharacterFootSide side,
            CharacterFootLandingPredictionRejectReason reason,
            CharacterFootLandingStepSource stepSource,
            AnimationFootMotionRuntimeSample step,
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
                step.IsValid ? step.LandingEventIdentity : 0,
                trajectoryGeneration,
                step.IsAuthoritative ? 1f : 0f,
                step.IsAuthoritative ? step.TimeToLandingSeconds : 0f,
                step.IsAuthoritative ? step.RootLocalLanding : default,
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

        CharacterFootStrideHipsResult ResolveStrideHips(
            in CharacterFootStrideIntentResult intent,
            in CharacterResolvedFootPair resolvedPair,
            in CharacterFootPrimarySupportResult primarySupport,
            in CharacterFootPelvisFrame frame,
            ref CharacterFootPelvisSpringState pelvisSpring)
        {
            if (!intent.Accepted)
            {
                if (!intent.ReleasePelvis)
                    return CharacterFootStrideHipsBuilder.BuildRejected(
                        intent.RejectReason);
                return ReleaseStride(
                    intent.RejectReason,
                    frame.ComponentUp,
                    frame.FootPlacementWeight,
                    frame.DeltaSeconds,
                    ref pelvisSpring);
            }
            CharacterResolvedFootResult supportMotion =
                intent.SupportSide == CharacterFootSide.Left
                    ? resolvedPair.Left
                    : resolvedPair.Right;
            bool validSupport =
                                supportMotion.Outcome == CharacterFootResolvedOutcome.Ready &&
                                supportMotion.PelvisReachReference.IsAvailable &&
                                supportMotion.SupportWeight >
                                CharacterPoseConstraintMath.Epsilon &&
                                supportMotion.SupportEligibility !=
                                CharacterFootSupportEligibility.None;
            ulong supportLandingEventIdentity = primarySupport.LandingEventIdentity;
            if (!validSupport ||
                supportMotion.SupportEventIdentity != supportLandingEventIdentity)
            {
                return ReleaseStride(
                    CharacterFootStrideRejectReason.SupportUnavailable,
                    frame.ComponentUp,
                    frame.FootPlacementWeight,
                    frame.DeltaSeconds,
                    ref pelvisSpring);
            }
            Vector3 supportHip = intent.SupportSide == CharacterFootSide.Left
                ? frame.Pose.Left.HipPosition
                : frame.Pose.Right.HipPosition;
            float supportLegLength = intent.SupportSide == CharacterFootSide.Left
                ? frame.LeftLegLength
                : frame.RightLegLength;
            Vector3 supportAnimatedAnkle = intent.SupportSide == CharacterFootSide.Left
                ? frame.Pose.Left.AnklePosition
                : frame.Pose.Right.AnklePosition;
            float supportLegCompressionReserve = Mathf.Max(
                0f,
                supportLegLength - Vector3.Distance(supportHip, supportAnimatedAnkle));
            return CharacterFootStrideHipsBuilder.BuildPelvis(
                intent.SupportSide,
                supportLandingEventIdentity,
                intent.SwingSide,
                intent.StrideStart,
                intent.StrideEnd,
                frame.PoseRootPosition,
                frame.ComponentUp,
                frame.AnimatedPelvis,
                frame.AnimatedPelvisComponentPosition,
                supportHip,
                supportMotion.FinalAnkle,
                supportLegLength,
                supportLegCompressionReserve,
                frame.Pose.Left.HeelPosition * 0.5f +
                frame.Pose.Left.ToePosition * 0.5f,
                frame.Pose.Right.HeelPosition * 0.5f +
                frame.Pose.Right.ToePosition * 0.5f,
                frame.LeftCorrectedSole,
                frame.RightCorrectedSole,
                intent.SwingTimeToLanding,
                frame.FootPlacementWeight,
                frame.DeltaSeconds,
                m_Settings.FootMotion,
                ref pelvisSpring);
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
            return originalSole +
                   (targetAnkle - foot.AnklePosition) * goal.PositionWeight;
        }

        static bool IsHardFootGoalOwnershipLoss(
            bool grounded,
            in CharacterFootActionOccupancy action) =>
            !grounded || action.IsOccupied;

        static bool IsLandingReachCandidate(
            in AnimationFootMotionRuntimeSample step,
            in CharacterFootSwingMotionResult motion,
            in CharacterResolvedFootResult resolved) =>
            step.IsAuthoritative &&
            step.HasConsistentLandingEventIdentity &&
            step.IsSwing &&
            motion.Accepted &&
            motion.LandingEventIdentity == step.LandingEventIdentity &&
            resolved.GoalWeight > CharacterPoseConstraintMath.Epsilon;

        CharacterFootStrideHipsResult ReleaseStride(
            CharacterFootStrideRejectReason reason,
            Vector3 componentUp,
            float footPlacementWeight,
            float deltaSeconds,
            ref CharacterFootPelvisSpringState pelvisSpring) =>
            CharacterFootStrideHipsBuilder.BuildPelvisRelease(
                reason,
                componentUp,
                footPlacementWeight,
                deltaSeconds,
                m_Settings.FootMotion,
                ref pelvisSpring);

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
                                      result.GoalWeight >
                                      CharacterPoseConstraintMath.Epsilon;
            Vector3 anklePosition = hasEffectiveOutput
                ? result.FinalAnkle
                : foot.AnklePosition;
            float positionWeight = hasEffectiveOutput
                ? result.GoalWeight
                : 0f;
            return new CharacterFullBodyIkGoal(
                side == CharacterFootSide.Left
                    ? CharacterFullBodyIkEffectorSlot.LeftFoot
                    : CharacterFullBodyIkEffectorSlot.RightFoot,
                root.InverseTransformPoint(anklePosition),
                (Quaternion.Inverse(root.rotation) * foot.AnkleRotation).normalized,
                positionWeight,
                0f,
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
        }
    }
}
