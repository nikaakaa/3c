using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    readonly struct CharacterFootLandingPredictionPair
    {
        internal CharacterFootLandingPredictionPair(
            CharacterFootLandingPredictionFootDiagnostics current,
            CharacterFootLandingPredictionFootDiagnostics incoming,
            CharacterFootLandingPredictionFootDiagnostics selected,
            AnimationBiomechanicalStepHeader selectedStep)
        {
            Current = current;
            Incoming = incoming;
            Selected = selected;
            SelectedStep = selectedStep;
        }

        internal CharacterFootLandingPredictionFootDiagnostics Current { get; }
        internal CharacterFootLandingPredictionFootDiagnostics Incoming { get; }
        internal CharacterFootLandingPredictionFootDiagnostics Selected { get; }
        internal AnimationBiomechanicalStepHeader SelectedStep { get; }
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

    internal sealed class CharacterFootPlacementRuntime : IDisposable
    {
        readonly ActorId m_ActorId;
        readonly CharacterFootPlacementRuntimeSettings m_Settings;
        readonly CharacterFootPlacementPoseRig m_Rig;
        readonly ICharacterFutureBodyTranslationSource m_FutureBodyTranslationSource;
        readonly ICharacterFootPlacementWorldQuery m_WorldQuery;
        readonly CharacterFootGroundPathFootState m_LeftGroundPath;
        readonly CharacterFootGroundPathFootState m_RightGroundPath;

        readonly CharacterFootLandingLifecycle m_LeftLandingLifecycle;
        readonly CharacterFootLandingLifecycle m_RightLandingLifecycle;
        readonly CharacterFootGoalTransition m_LeftGoalTransition;
        readonly CharacterFootGoalTransition m_RightGoalTransition;

        CharacterFutureBodyTranslation m_BodyTrajectory;
        ulong m_BodyTrajectoryTick;
        ulong m_BodyTrajectoryResetSequence;
        ulong m_BodyTrajectoryGeneration;
        ulong m_BodyTrajectoryAuthorityTick;
        float m_BodyTrajectoryRequestedDuration;
        bool m_HasBodyTrajectoryAttempt;
        CharacterFootLandingPredictionDiagnostics m_LastDiagnostics;
        CharacterFootLandingPredictionDiagnostics m_PendingDiagnostics;
        CharacterFootPelvisSpringState m_CommittedPelvisSpring;
        CharacterFootPelvisSpringState m_PendingPelvisSpring;
        CharacterFootSupportLockFacts m_CommittedLeftSupportLock;
        CharacterFootSupportLockFacts m_CommittedRightSupportLock;
        CharacterFootSupportLockFacts m_PendingLeftSupportLock;
        CharacterFootSupportLockFacts m_PendingRightSupportLock;
        bool m_HasPendingFrame;
        bool m_Disposed;

        internal CharacterFootPlacementRuntime(
            ActorId actorId,
            CharacterFootPlacementRuntimeSettings settings,
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
            m_LeftGroundPath = new CharacterFootGroundPathFootState(
                settings.GroundDetection.ContactCapacity);
            m_RightGroundPath = new CharacterFootGroundPathFootState(
                settings.GroundDetection.ContactCapacity);
            m_LeftLandingLifecycle = new CharacterFootLandingLifecycle();
            m_RightLandingLifecycle = new CharacterFootLandingLifecycle();
            m_LeftGoalTransition = new CharacterFootGoalTransition();
            m_RightGoalTransition = new CharacterFootGoalTransition();
        }

        internal bool HasPendingFrame => m_HasPendingFrame;
        internal CharacterFootLandingPredictionDiagnostics LandingPredictionDiagnostics =>
            m_HasPendingFrame ? m_PendingDiagnostics : m_LastDiagnostics;

        internal CharacterFullBodyIkGoalSetHeader EvaluateFrame(
            in CharacterFootPlacementFrameInput frame,
            NativeSlice<CharacterFullBodyIkGoal> goalOutput,
            int goalOffset,
            int producerOperationIndex,
            int producerCallSiteIndex,
            int parameterIndex)
        {
            RequireAlive();
            if (m_HasPendingFrame)
                throw new InvalidOperationException("Foot Placement already has a pending frame.");
            if (frame.ActorId != m_ActorId ||
                !string.Equals(
                    frame.Pose.PosePlanHash,
                    m_Settings.PosePlanHash,
                    StringComparison.Ordinal) ||
                goalOutput.Length != CharacterPresentationFootPlacementDescriptor.GoalCount ||
                goalOffset < 0 || producerOperationIndex < 0 ||
                producerCallSiteIndex < 0 || parameterIndex < 0 ||
                parameterIndex >= frame.Pose.PoseParameters.Length ||
                frame.Pose.PoseParameterAvailability[parameterIndex] == 0 ||
                !float.IsFinite(frame.Pose.PoseParameters[parameterIndex]))
            {
                throw new ArgumentException("Foot Placement frame contract is inconsistent.");
            }
            m_HasPendingFrame = true;

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
            var inputDiagnostics = new CharacterFootLandingPredictionInputDiagnostics(
                frame.PresentationDeltaSeconds,
                frame.Body,
                facts.Grounded,
                facts.HorizontalSpeed,
                in leftAction,
                in rightAction,
                in timeline,
                currentSegmentRemainingSeconds);
            CharacterFutureBodyTranslation bodyTrajectory = ResolveBodyTrajectory(
                frame.Pose.LeftFootSteps,
                frame.Pose.RightFootSteps,
                in timeline,
                currentSegmentRemainingSeconds,
                frame.Body);
            Vector3 componentUp = frame.Body.VisibleRotation * Vector3.up;

            m_LeftLandingLifecycle.BeginPending();
            m_RightLandingLifecycle.BeginPending();
            m_LeftGoalTransition.BeginPending();
            m_RightGoalTransition.BeginPending();
            m_PendingPelvisSpring = m_CommittedPelvisSpring;
            m_PendingLeftSupportLock = m_CommittedLeftSupportLock;
            m_PendingRightSupportLock = m_CommittedRightSupportLock;

            m_LeftLandingLifecycle.PromoteLanded(frame.Pose.LeftFootSteps.CurrentStep);
            m_RightLandingLifecycle.PromoteLanded(frame.Pose.RightFootSteps.CurrentStep);

            CharacterFootLandingSnapshot leftLanding = m_LeftLandingLifecycle.Pending;
            CharacterFootLandingSnapshot rightLanding = m_RightLandingLifecycle.Pending;

            CharacterFootLandingPredictionPair leftPair = PredictFootPair(
                CharacterFootSide.Left,
                frame.Pose.LeftFootSteps,
                pose.Left,
                leftGoal,
                in timeline,
                currentSegmentRemainingSeconds,
                bodyTrajectory,
                in frame,
                leftLanding.LastLandingEventIdentity);
            CharacterFootLandingPredictionPair rightPair = PredictFootPair(
                CharacterFootSide.Right,
                frame.Pose.RightFootSteps,
                pose.Right,
                rightGoal,
                in timeline,
                currentSegmentRemainingSeconds,
                bodyTrajectory,
                in frame,
                rightLanding.LastLandingEventIdentity);
            CharacterFootLandingPredictionFootDiagnostics leftCurrent = leftPair.Current;
            CharacterFootLandingPredictionFootDiagnostics leftIncoming = leftPair.Incoming;
            CharacterFootLandingPredictionFootDiagnostics rightCurrent = rightPair.Current;
            CharacterFootLandingPredictionFootDiagnostics rightIncoming = rightPair.Incoming;
            AnimationBiomechanicalStepHeader leftCurrentStep = frame.Pose.LeftFootSteps.CurrentStep;
            AnimationBiomechanicalStepHeader rightCurrentStep = frame.Pose.RightFootSteps.CurrentStep;
            CharacterFootLandingPredictionFootDiagnostics left = leftPair.Selected;
            AnimationBiomechanicalStepHeader leftSelectedStep = leftPair.SelectedStep;
            CharacterFootLandingPredictionFootDiagnostics right = rightPair.Selected;
            AnimationBiomechanicalStepHeader rightSelectedStep = rightPair.SelectedStep;
            m_LeftLandingLifecycle.CaptureNextSwing(
                in leftSelectedStep,
                in left,
                m_Settings.FootMotion);
            m_RightLandingLifecycle.CaptureNextSwing(
                in rightSelectedStep,
                in right,
                m_Settings.FootMotion);
            leftLanding = m_LeftLandingLifecycle.Pending;
            rightLanding = m_RightLandingLifecycle.Pending;
            bool hasLeftLastLanding = leftLanding.HasLastLanding;
            bool hasLeftNextSwingLanding = leftLanding.HasNextSwingLanding;
            bool hasRightLastLanding = rightLanding.HasLastLanding;
            bool hasRightNextSwingLanding = rightLanding.HasNextSwingLanding;
            CharacterFootGroundPathLanding leftLastLanding = leftLanding.LastLanding;
            CharacterFootGroundPathLanding leftNextSwingLanding = leftLanding.NextSwingLanding;
            CharacterFootGroundPathLanding rightLastLanding = rightLanding.LastLanding;
            CharacterFootGroundPathLanding rightNextSwingLanding = rightLanding.NextSwingLanding;
            CharacterFootGroundPathDiagnostics leftGroundPath = PrepareGroundPath(
                CharacterFootSide.Left,
                hasLeftLastLanding,
                leftLastLanding,
                hasLeftNextSwingLanding,
                leftNextSwingLanding,
                componentUp,
                inputDiagnostics.TimelineAuthorityTick,
                m_LeftGroundPath);
            CharacterFootGroundPathDiagnostics rightGroundPath = PrepareGroundPath(
                CharacterFootSide.Right,
                hasRightLastLanding,
                rightLastLanding,
                hasRightNextSwingLanding,
                rightNextSwingLanding,
                componentUp,
                inputDiagnostics.TimelineAuthorityTick,
                m_RightGroundPath);
            left = left.WithGroundPath(in leftGroundPath);
            right = right.WithGroundPath(in rightGroundPath);

            float footPlacementWeight = frame.Pose.PoseParameters[parameterIndex];
            CharacterFootSwingMotionDiagnostics leftSwingMotion =
                CharacterFootSwingMotionBuilder.Build(
                    pose.Left,
                    in leftCurrentStep,
                    footPlacementWeight,
                    componentUp,
                    in leftGroundPath,
                    leftLanding.NextSwingPredictionError,
                    leftLanding.NextSwingConstraintWeight);
            CharacterFootSwingMotionDiagnostics rightSwingMotion =
                CharacterFootSwingMotionBuilder.Build(
                    pose.Right,
                    in rightCurrentStep,
                    footPlacementWeight,
                    componentUp,
                    in rightGroundPath,
                    rightLanding.NextSwingPredictionError,
                    rightLanding.NextSwingConstraintWeight);
            bool hasSelectedSwing = CharacterFootStrideHipsBuilder.TrySelectSwing(
                in leftCurrentStep,
                in rightCurrentStep,
                in leftSwingMotion,
                in rightSwingMotion,
                out CharacterFootSide selectedSwingSide);
            CharacterFootSwingMotionDiagnostics leftFootMotion =
                hasSelectedSwing && selectedSwingSide == CharacterFootSide.Left
                    ? leftSwingMotion
                    : default;
            CharacterFootSwingMotionDiagnostics rightFootMotion =
                hasSelectedSwing && selectedSwingSide == CharacterFootSide.Right
                    ? rightSwingMotion
                    : default;
            m_PendingLeftSupportLock.Clear();
            m_PendingRightSupportLock.Clear();
            m_PendingPelvisSpring.Clear();
            leftGoal = CreateFootGoal(
                CharacterFootSide.Left,
                pose.Left,
                in leftFootMotion);
            rightGoal = CreateFootGoal(
                CharacterFootSide.Right,
                pose.Right,
                in rightFootMotion);
            Transform goalRoot = m_Rig.PoseRoot;
            Vector3 leftOriginalComponentPosition = goalRoot.InverseTransformPoint(
                pose.Left.AnklePosition);
            Quaternion leftOriginalComponentRotation =
                (Quaternion.Inverse(goalRoot.rotation) * pose.Left.AnkleRotation).normalized;
            Vector3 rightOriginalComponentPosition = goalRoot.InverseTransformPoint(
                pose.Right.AnklePosition);
            Quaternion rightOriginalComponentRotation =
                (Quaternion.Inverse(goalRoot.rotation) * pose.Right.AnkleRotation).normalized;
            leftGoal = m_LeftGoalTransition.Resolve(
                in leftGoal,
                leftOriginalComponentPosition,
                leftOriginalComponentRotation,
                leftGroundPath.Accepted ? leftGroundPath.InputIdentity : 0,
                frame.PresentationDeltaSeconds,
                m_Settings.FootMotion.GoalTransitionHalfLifeSeconds,
                !facts.Grounded || leftAction.IsOccupied);
            rightGoal = m_RightGoalTransition.Resolve(
                in rightGoal,
                rightOriginalComponentPosition,
                rightOriginalComponentRotation,
                rightGroundPath.Accepted ? rightGroundPath.InputIdentity : 0,
                frame.PresentationDeltaSeconds,
                m_Settings.FootMotion.GoalTransitionHalfLifeSeconds,
                !facts.Grounded || rightAction.IsOccupied);
            CharacterFootStrideHipsDiagnostics strideHips = RejectStride(
                CharacterFootStrideRejectReason.SwingOnlyStage);
            CharacterFootGoalTransitionDiagnostics leftGoalTransition =
                m_LeftGoalTransition.CaptureDiagnostics(
                    m_Settings.FootMotion.GoalTransitionHalfLifeSeconds);
            CharacterFootGoalTransitionDiagnostics rightGoalTransition =
                m_RightGoalTransition.CaptureDiagnostics(
                    m_Settings.FootMotion.GoalTransitionHalfLifeSeconds);
            left = left.WithFootMotion(
                in leftFootMotion,
                in leftGoalTransition,
                leftGoal);
            right = right.WithFootMotion(
                in rightFootMotion,
                in rightGoalTransition,
                rightGoal);

            goalOutput[0] = pelvisGoal;
            goalOutput[1] = leftGoal;
            goalOutput[2] = rightGoal;
            m_PendingDiagnostics = new CharacterFootLandingPredictionDiagnostics(
                frame.RenderFrame,
                frame.Pose.CompletionIdentity,
                m_Rig.VisualRoot.GetInstanceID(),
                inputDiagnostics,
                pelvisGoal,
                in strideHips,
                left,
                right);
            return new CharacterFullBodyIkGoalSetHeader(
                frame.RenderFrame,
                frame.Pose.CompletionIdentity,
                m_Rig.Rig.RigId,
                m_Rig.Rig.RigRevision,
                producerOperationIndex,
                producerCallSiteIndex,
                goalOffset,
                CharacterPresentationFootPlacementDescriptor.GoalCount,
                CharacterFullBodyIkGoalSetAvailability.Ready);
        }

        internal void SealFrame(ulong renderFrame, ulong completionIdentity)
        {
            RequireAlive();
            if (!m_HasPendingFrame ||
                m_PendingDiagnostics.FrameSequence != renderFrame ||
                m_PendingDiagnostics.CompletionIdentity != completionIdentity)
            {
                throw new InvalidOperationException(
                    "Foot Placement pending completion identity is inconsistent.");
            }
            m_LastDiagnostics = m_PendingDiagnostics;
            m_CommittedPelvisSpring = m_PendingPelvisSpring;
            m_CommittedLeftSupportLock = m_PendingLeftSupportLock;
            m_CommittedRightSupportLock = m_PendingRightSupportLock;
            m_LeftGroundPath.Seal();
            m_RightGroundPath.Seal();
            m_LeftLandingLifecycle.Seal();
            m_RightLandingLifecycle.Seal();
            m_LeftGoalTransition.Seal();
            m_RightGoalTransition.Seal();
            m_PendingPelvisSpring.Clear();
            m_PendingLeftSupportLock.Clear();
            m_PendingRightSupportLock.Clear();
            m_PendingDiagnostics = default;
            m_HasPendingFrame = false;
            CharacterFootLandingPredictionDebugRegistry.Publish(in m_LastDiagnostics);
        }

        internal void DiscardPendingFrame()
        {
            RequireAlive();
            m_LeftGroundPath.Discard();
            m_RightGroundPath.Discard();
            m_LeftLandingLifecycle.Discard();
            m_RightLandingLifecycle.Discard();
            m_LeftGoalTransition.Discard();
            m_RightGoalTransition.Discard();
            m_PendingPelvisSpring.Clear();
            m_PendingLeftSupportLock.Clear();
            m_PendingRightSupportLock.Clear();
            m_PendingDiagnostics = default;
            m_HasPendingFrame = false;
        }

        internal void Reset(in CharacterFootPlacementReset reset)
        {
            RequireAlive();
            if (reset.ActorId != m_ActorId)
                throw new ArgumentException("Foot Placement reset Actor identity is invalid.");
            m_PendingDiagnostics = default;
            m_LastDiagnostics = default;
            m_HasPendingFrame = false;
            ClearBodyTrajectory();
            m_LeftGroundPath.Reset();
            m_RightGroundPath.Reset();
            ResetLandingState();
            CharacterFootLandingPredictionDebugRegistry.Remove(
                m_Rig.VisualRoot.GetInstanceID());
        }

        internal void RetargetBodyBranch(ulong resetSequence)
        {
            RequireAlive();
            if (resetSequence == 0)
                throw new ArgumentOutOfRangeException(nameof(resetSequence));
            m_PendingDiagnostics = default;
            m_LastDiagnostics = default;
            m_HasPendingFrame = false;
            ClearBodyTrajectory();
            m_LeftGroundPath.Reset();
            m_RightGroundPath.Reset();
            ResetLandingState();
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

        CharacterFootGroundPathDiagnostics PrepareGroundPath(
            CharacterFootSide side,
            bool hasLastLanding,
            CharacterFootGroundPathLanding lastLanding,
            bool hasNextSwingLanding,
            CharacterFootGroundPathLanding nextSwingLanding,
            Vector3 componentUp,
            ulong authorityTick,
            CharacterFootGroundPathFootState state)
        {
            if (!hasLastLanding)
            {
                CharacterFootGroundPathPage rejectedPage = state.BeginPending();
                rejectedPage.SetRejected(
                    CharacterFootGroundPathRejectReason.CurrentLandingUnavailable,
                    false,
                    0,
                    default,
                    default);
                return new CharacterFootGroundPathDiagnostics(rejectedPage, false);
            }
            if (!hasNextSwingLanding)
            {
                CharacterFootGroundPathPage rejectedPage = state.BeginPending();
                rejectedPage.SetRejected(
                    CharacterFootGroundPathRejectReason.NextLandingUnavailable,
                    false, 0, default, default);
                return new CharacterFootGroundPathDiagnostics(rejectedPage, false);
            }

            CharacterFootGroundPathInputKey key =
                CharacterFootGroundPathInputBuilder.BuildKey(
                    side,
                    in lastLanding,
                    in nextSwingLanding,
                    authorityTick,
                    componentUp,
                    m_Settings.ProfileRevision);
            if (state.HasCommittedInput && state.CommittedAccepted &&
                state.CommittedKey.Equals(key))
            {
                CharacterFootGroundPathPage committedPage = state.ReuseCommitted();
                return new CharacterFootGroundPathDiagnostics(committedPage, false);
            }

            CharacterFootGroundPathPage pendingPage = state.BeginPending();
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
                return new CharacterFootGroundPathDiagnostics(pendingPage, false);
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
                        state.EnvelopeWorkspace,
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
            return new CharacterFootGroundPathDiagnostics(pendingPage, true);
        }

        CharacterFootLandingPredictionPair PredictFootPair(
            CharacterFootSide side,
            AnimationBiomechanicalStepReadPage steps,
            CharacterFootPlacementAnimatedFootPose animatedFoot,
            CharacterFullBodyIkGoal goal,
            in CommittedLocomotionPlanarMotionTimeline timeline,
            float currentSegmentRemainingSeconds,
            CharacterFutureBodyTranslation bodyTrajectory,
            in CharacterFootPlacementFrameInput frame,
            ulong lastLandingEventIdentity)
        {
            Vector3 currentSole =
                (animatedFoot.HeelPosition + animatedFoot.ToePosition) * 0.5f;
            bool currentCandidate = IsNextSwingHeader(
                steps.CurrentStep,
                lastLandingEventIdentity,
                m_Settings.LandingPrediction.MaximumPredictionTimeSeconds);
            bool incomingCandidate = IsNextSwingHeader(
                steps.IncomingStep,
                lastLandingEventIdentity,
                m_Settings.LandingPrediction.MaximumPredictionTimeSeconds);
            bool selectCurrent = currentCandidate &&
                (!incomingCandidate ||
                 steps.CurrentStep.TimeToLandingSeconds <= steps.IncomingStep.TimeToLandingSeconds);
            bool selectIncoming = incomingCandidate && !selectCurrent;
            CharacterFootLandingPredictionFootDiagnostics current = selectCurrent
                ? PredictStep(
                    side,
                    steps.CurrentStep,
                    CharacterFootLandingStepSource.Current,
                    currentSole,
                    goal,
                    in timeline,
                    currentSegmentRemainingSeconds,
                    bodyTrajectory,
                    in frame)
                : Rejected(
                    side,
                    CharacterFootLandingPredictionRejectReason.StepUnavailable,
                    CharacterFootLandingStepSource.Current,
                    steps.CurrentStep,
                    timeline.Generation,
                    currentSole,
                    default,
                    default,
                    goal);
            CharacterFootLandingPredictionFootDiagnostics incoming = selectIncoming
                ? PredictStep(
                    side,
                    steps.IncomingStep,
                    CharacterFootLandingStepSource.Incoming,
                    currentSole,
                    goal,
                    in timeline,
                    currentSegmentRemainingSeconds,
                    bodyTrajectory,
                    in frame)
                : Rejected(
                    side,
                    CharacterFootLandingPredictionRejectReason.StepUnavailable,
                    CharacterFootLandingStepSource.Incoming,
                    steps.IncomingStep,
                    timeline.Generation,
                    currentSole,
                    default,
                    default,
                    goal);
            CharacterFootLandingPredictionFootDiagnostics selected = selectCurrent
                ? current
                : selectIncoming ? incoming : current;
            AnimationBiomechanicalStepHeader selectedStep = selectCurrent
                ? steps.CurrentStep
                : selectIncoming ? steps.IncomingStep : default;
            return new CharacterFootLandingPredictionPair(
                current, incoming, selected, selectedStep);
        }

        static bool IsNextSwingHeader(
            AnimationBiomechanicalStepHeader step,
            ulong lastLandingEventIdentity,
            float maximumPredictionTimeSeconds) =>
            step.IsAuthoritative &&
            step.HasConsistentLandingEventIdentity &&
            (step.IsPreSwing || step.IsSwing) &&
            step.TimeToLandingSeconds > 0.000001f &&
            step.TimeToLandingSeconds <= maximumPredictionTimeSeconds &&
            step.LandingEventIdentity != lastLandingEventIdentity;

        CharacterFootLandingPredictionFootDiagnostics PredictStep(
            CharacterFootSide side,
            AnimationBiomechanicalStepHeader step,
            CharacterFootLandingStepSource stepSource,
            Vector3 currentSole,
            CharacterFullBodyIkGoal goal,
            in CommittedLocomotionPlanarMotionTimeline timeline,
            float currentSegmentRemainingSeconds,
            CharacterFutureBodyTranslation bodyTrajectory,
            in CharacterFootPlacementFrameInput frame)
        {
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
            bool accepted = CharacterFootLandingPredictor.TryResolve(
                side,
                rawLanding,
                componentUp,
                in settings,
                m_WorldQuery,
                out CharacterFootPlacementQueryRequest query,
                out CharacterFootLandingSupport support,
                out CharacterFootLandingQueryRejectReason queryRejectReason);
            return new CharacterFootLandingPredictionFootDiagnostics(
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
                step.Confidence,
                step.TimeToLandingSeconds,
                step.RootLocalLanding,
                bodyTrajectory != null,
                bodyTrajectory != null ? bodyTrajectory.SourceIdentity : string.Empty,
                in bodyTranslation,
                currentSole,
                rawLanding,
                query,
                support,
                goal);
        }

        CharacterFutureBodyTranslation ResolveBodyTrajectory(
            AnimationBiomechanicalStepReadPage leftSteps,
            AnimationBiomechanicalStepReadPage rightSteps,
            in CommittedLocomotionPlanarMotionTimeline timeline,
            float currentSegmentRemainingSeconds,
            CharacterBodyPresentationFrame body)
        {
            float maximum = m_Settings.LandingPrediction.MaximumPredictionTimeSeconds;
            float leftCurrentTime = ResolvePredictionTime(
                leftSteps.CurrentStep,
                maximum);
            float leftIncomingTime = ResolvePredictionTime(
                leftSteps.IncomingStep,
                maximum);
            float rightCurrentTime = ResolvePredictionTime(
                rightSteps.CurrentStep,
                maximum);
            float rightIncomingTime = ResolvePredictionTime(
                rightSteps.IncomingStep,
                maximum);
            float duration = Mathf.Max(
                Mathf.Max(leftCurrentTime, leftIncomingTime),
                Mathf.Max(rightCurrentTime, rightIncomingTime));
            if (duration <= 0f || !timeline.IsValid ||
                m_FutureBodyTranslationSource == null)
            {
                return null;
            }

            bool sameCommittedBody = m_HasBodyTrajectoryAttempt &&
                                     m_BodyTrajectoryTick == body.CurrentTick &&
                                     m_BodyTrajectoryResetSequence == body.ResetSequence &&
                                     m_BodyTrajectoryGeneration == timeline.Generation &&
                                     m_BodyTrajectoryAuthorityTick == timeline.AuthorityTick.Value;
            if (sameCommittedBody &&
                duration <= m_BodyTrajectoryRequestedDuration + 0.0001f)
            {
                return m_BodyTrajectory;
            }

            m_HasBodyTrajectoryAttempt = true;
            m_BodyTrajectoryTick = body.CurrentTick;
            m_BodyTrajectoryResetSequence = body.ResetSequence;
            m_BodyTrajectoryGeneration = timeline.Generation;
            m_BodyTrajectoryAuthorityTick = timeline.AuthorityTick.Value;
            m_BodyTrajectoryRequestedDuration = duration;
            m_BodyTrajectory = null;

            var request = new CharacterFutureBodyTranslationRequest(
                m_ActorId,
                duration,
                body.TargetVelocity.x,
                body.TargetVelocity.z,
                timeline.ContinuationVelocityX,
                timeline.ContinuationVelocityZ,
                currentSegmentRemainingSeconds,
                timeline.HasContinuation,
                leftCurrentTime,
                leftIncomingTime,
                rightCurrentTime,
                rightIncomingTime);
            if (m_FutureBodyTranslationSource.TryPredict(
                    in request,
                    out CharacterFutureBodyTranslation trajectory))
            {
                m_BodyTrajectory = trajectory;
            }
            return m_BodyTrajectory;
        }

        static float ResolvePredictionTime(
            AnimationBiomechanicalStepHeader step,
            float maximum) =>
            step.IsAuthoritative && step.HasConsistentLandingEventIdentity &&
            step.TimeToLandingSeconds > 0.000001f &&
            step.TimeToLandingSeconds <= maximum
                ? step.TimeToLandingSeconds
                : 0f;

        void ResetLandingState()
        {
            m_LeftLandingLifecycle.Reset();
            m_RightLandingLifecycle.Reset();
            m_LeftGoalTransition.Reset();
            m_RightGoalTransition.Reset();
            m_CommittedPelvisSpring.Clear();
            m_PendingPelvisSpring.Clear();
            m_CommittedLeftSupportLock.Clear();
            m_CommittedRightSupportLock.Clear();
            m_PendingLeftSupportLock.Clear();
            m_PendingRightSupportLock.Clear();
        }

        void ClearBodyTrajectory()
        {
            m_BodyTrajectory = null;
            m_BodyTrajectoryTick = 0;
            m_BodyTrajectoryResetSequence = 0;
            m_BodyTrajectoryGeneration = 0;
            m_BodyTrajectoryAuthorityTick = 0;
            m_BodyTrajectoryRequestedDuration = 0f;
            m_HasBodyTrajectoryAttempt = false;
        }

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

        static CharacterFootLandingPredictionFootDiagnostics Rejected(
            CharacterFootSide side,
            CharacterFootLandingPredictionRejectReason reason,
            CharacterFootLandingStepSource stepSource,
            AnimationBiomechanicalStepHeader step,
            ulong trajectoryGeneration,
            Vector3 currentSole,
            Vector3 rawLanding,
            CharacterFootPlacementQueryRequest query,
            CharacterFullBodyIkGoal goal) =>
            new CharacterFootLandingPredictionFootDiagnostics(
                side,
                CharacterFootLandingPredictionState.Rejected,
                reason,
                stepSource,
                step.IsValid ? step.LandingEventIdentity : 0,
                trajectoryGeneration,
                step.IsValid ? step.Confidence : 0f,
                step.IsValid ? step.TimeToLandingSeconds : 0f,
                step.IsValid ? step.RootLocalLanding : default,
                false,
                string.Empty,
                default,
                currentSole,
                rawLanding,
                query,
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

        CharacterFootStrideHipsDiagnostics ResolveStrideHips(
            in AnimationBiomechanicalStepHeader leftStep,
            in AnimationBiomechanicalStepHeader rightStep,
            bool hasSelectedSwing,
            CharacterFootSide selectedSwingSide,
            bool hasLeftLastLanding,
            CharacterFootGroundPathLanding leftLastLanding,
            bool hasRightLastLanding,
            CharacterFootGroundPathLanding rightLastLanding,
            bool hasLeftNextSwingLanding,
            CharacterFootGroundPathLanding leftNextSwingLanding,
            bool hasRightNextSwingLanding,
            CharacterFootGroundPathLanding rightNextSwingLanding,
            bool leftGroundPathAccepted,
            bool rightGroundPathAccepted,
            bool grounded,
            in CharacterFootActionOccupancy leftAction,
            in CharacterFootActionOccupancy rightAction,
            Vector3 componentUp,
            Vector3 poseRootPosition,
            Vector3 animatedPelvis,
            Vector3 animatedPelvisComponentPosition,
            in CharacterFootPlacementAnimatedPose pose,
            in CharacterFootSwingMotionDiagnostics leftFootMotion,
            in CharacterFootSwingMotionDiagnostics rightFootMotion,
            float footPlacementWeight,
            float deltaSeconds)
        {
            if (!grounded)
                return RejectStride(CharacterFootStrideRejectReason.BodyNotGrounded);
            if (leftAction.IsOccupied || rightAction.IsOccupied)
                return RejectStride(CharacterFootStrideRejectReason.ActionOccupied);
            if (!CharacterFootStrideHipsBuilder.TryResolveStride(
                    in leftStep,
                    in rightStep,
                    hasSelectedSwing,
                    selectedSwingSide,
                    hasLeftLastLanding,
                    hasLeftLastLanding ? leftLastLanding.Point : default,
                    hasRightLastLanding,
                    hasRightLastLanding ? rightLastLanding.Point : default,
                    hasLeftNextSwingLanding,
                    hasLeftNextSwingLanding ? leftNextSwingLanding.Point : default,
                    hasLeftNextSwingLanding ? leftNextSwingLanding.LandingEventIdentity : 0,
                    hasRightNextSwingLanding,
                    hasRightNextSwingLanding ? rightNextSwingLanding.Point : default,
                    hasRightNextSwingLanding ? rightNextSwingLanding.LandingEventIdentity : 0,
                    componentUp,
                    out CharacterFootSide supportSide,
                    out CharacterFootSide swingSide,
                    out Vector3 strideStart,
                    out Vector3 strideEnd,
                    out CharacterFootStrideRejectReason rejectReason))
            {
                return RejectStride(rejectReason);
            }
            bool groundPathAccepted = swingSide == CharacterFootSide.Left
                ? leftGroundPathAccepted
                : rightGroundPathAccepted;
            if (!groundPathAccepted)
                return RejectStride(CharacterFootStrideRejectReason.GroundPathRejected);
            float swingTimeToLanding = swingSide == CharacterFootSide.Left
                ? leftStep.TimeToLandingSeconds
                : rightStep.TimeToLandingSeconds;
            Vector3 leftCorrectedSole = leftFootMotion.Accepted
                ? leftFootMotion.CorrectedSole
                : pose.Left.HeelPosition * 0.5f + pose.Left.ToePosition * 0.5f;
            Vector3 rightCorrectedSole = rightFootMotion.Accepted
                ? rightFootMotion.CorrectedSole
                : pose.Right.HeelPosition * 0.5f + pose.Right.ToePosition * 0.5f;
            return CharacterFootStrideHipsBuilder.BuildPelvis(
                supportSide,
                swingSide,
                strideStart,
                strideEnd,
                poseRootPosition,
                componentUp,
                animatedPelvis,
                animatedPelvisComponentPosition,
                pose.Left.HeelPosition * 0.5f + pose.Left.ToePosition * 0.5f,
                pose.Right.HeelPosition * 0.5f + pose.Right.ToePosition * 0.5f,
                leftCorrectedSole,
                rightCorrectedSole,
                swingTimeToLanding,
                footPlacementWeight,
                deltaSeconds,
                m_Settings.FootMotion,
                ref m_PendingPelvisSpring);
        }

        CharacterFootStrideHipsDiagnostics RejectStride(
            CharacterFootStrideRejectReason reason)
        {
            return CharacterFootStrideHipsBuilder.BuildRejected(reason);
        }

        static CharacterFullBodyIkGoal CreatePelvisGoal() =>
            CreatePelvisGoal(default, null);

        static CharacterFullBodyIkGoal CreatePelvisGoal(
            in CharacterFootStrideHipsDiagnostics strideHips,
            Transform poseRoot)
        {
            Vector3 translation = default;
            float weight = 0f;
            if (strideHips.Accepted && poseRoot != null)
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
            CharacterFootSwingMotionDiagnostics motion = default;
            return CreateFootGoal(side, foot, in motion);
        }

        CharacterFullBodyIkGoal CreateFootGoal(
            CharacterFootSide side,
            CharacterFootPlacementAnimatedFootPose foot,
            in CharacterFootSwingMotionDiagnostics motion)
        {
            Transform root = m_Rig.PoseRoot;
            Vector3 anklePosition = motion.Accepted
                ? motion.CorrectedAnkle
                : foot.AnklePosition;
            float positionWeight = motion.Accepted
                ? motion.PositionWeight
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
                throw new ObjectDisposedException(nameof(CharacterFootPlacementRuntime));
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            CharacterFootLandingPredictionDebugRegistry.Remove(
                m_Rig.VisualRoot.GetInstanceID());
            m_LastDiagnostics = default;
            m_PendingDiagnostics = default;
            m_HasPendingFrame = false;
            ClearBodyTrajectory();
            m_LeftGroundPath.Reset();
            m_RightGroundPath.Reset();
            ResetLandingState();
        }
    }
}
