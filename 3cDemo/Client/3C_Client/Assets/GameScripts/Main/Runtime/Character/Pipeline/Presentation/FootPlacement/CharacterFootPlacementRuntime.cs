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
            CharacterFootLandingPredictionFootDiagnostics selected)
        {
            Current = current;
            Incoming = incoming;
            Selected = selected;
        }

        internal CharacterFootLandingPredictionFootDiagnostics Current { get; }
        internal CharacterFootLandingPredictionFootDiagnostics Incoming { get; }
        internal CharacterFootLandingPredictionFootDiagnostics Selected { get; }
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

        CharacterFutureBodyTranslation m_BodyTrajectory;
        ulong m_BodyTrajectoryTick;
        ulong m_BodyTrajectoryResetSequence;
        ulong m_BodyTrajectoryGeneration;
        ulong m_BodyTrajectoryAuthorityTick;
        float m_BodyTrajectoryRequestedDuration;
        bool m_HasBodyTrajectoryAttempt;
        CharacterFootLandingPredictionDiagnostics m_LastDiagnostics;
        CharacterFootLandingPredictionDiagnostics m_PendingDiagnostics;
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
                settings.GroundDetection.HitCapacity);
            m_RightGroundPath = new CharacterFootGroundPathFootState(
                settings.GroundDetection.HitCapacity);
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
            float currentSegmentRemainingSeconds = timeline.IsValid
                ? ResolveCurrentSegmentRemainingSeconds(timeline, frame.Body)
                : 0f;
            var inputDiagnostics = new CharacterFootLandingPredictionInputDiagnostics(
                frame.PresentationDeltaSeconds,
                frame.Body,
                in timeline,
                currentSegmentRemainingSeconds);
            CharacterFutureBodyTranslation bodyTrajectory = ResolveBodyTrajectory(
                frame.Pose.LeftFootSteps,
                frame.Pose.RightFootSteps,
                in timeline,
                currentSegmentRemainingSeconds,
                frame.Body);

            CharacterFootLandingPredictionPair leftPair = PredictFootPair(
                CharacterFootSide.Left,
                frame.Pose.LeftFootSteps,
                pose.Left,
                leftGoal,
                in timeline,
                currentSegmentRemainingSeconds,
                bodyTrajectory,
                in frame);
            CharacterFootLandingPredictionPair rightPair = PredictFootPair(
                CharacterFootSide.Right,
                frame.Pose.RightFootSteps,
                pose.Right,
                rightGoal,
                in timeline,
                currentSegmentRemainingSeconds,
                bodyTrajectory,
                in frame);
            CharacterFootLandingPredictionFootDiagnostics left = leftPair.Selected;
            CharacterFootLandingPredictionFootDiagnostics right = rightPair.Selected;
            Vector3 componentUp = frame.Body.VisibleRotation * Vector3.up;
            CharacterFootGroundPathDiagnostics leftGroundPath = PrepareGroundPath(
                CharacterFootSide.Left,
                leftPair.Current,
                leftPair.Incoming,
                componentUp,
                inputDiagnostics.TimelineAuthorityTick,
                m_LeftGroundPath);
            CharacterFootGroundPathDiagnostics rightGroundPath = PrepareGroundPath(
                CharacterFootSide.Right,
                rightPair.Current,
                rightPair.Incoming,
                componentUp,
                inputDiagnostics.TimelineAuthorityTick,
                m_RightGroundPath);
            left = left.WithGroundPath(in leftGroundPath);
            right = right.WithGroundPath(in rightGroundPath);

            goalOutput[0] = pelvisGoal;
            goalOutput[1] = leftGoal;
            goalOutput[2] = rightGoal;
            m_PendingDiagnostics = new CharacterFootLandingPredictionDiagnostics(
                frame.RenderFrame,
                frame.Pose.CompletionIdentity,
                m_Rig.VisualRoot.GetInstanceID(),
                inputDiagnostics,
                pelvisGoal,
                left,
                right);
            m_HasPendingFrame = true;

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
            m_LeftGroundPath.Seal();
            m_RightGroundPath.Seal();
            m_PendingDiagnostics = default;
            m_HasPendingFrame = false;
            CharacterFootLandingPredictionDebugRegistry.Publish(in m_LastDiagnostics);
        }

        internal void DiscardPendingFrame()
        {
            RequireAlive();
            m_LeftGroundPath.Discard();
            m_RightGroundPath.Discard();
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
            CharacterFootLandingPredictionFootDiagnostics currentLanding,
            CharacterFootLandingPredictionFootDiagnostics nextLanding,
            Vector3 componentUp,
            ulong authorityTick,
            CharacterFootGroundPathFootState state)
        {
            if (!currentLanding.Accepted)
            {
                CharacterFootGroundPathPage rejectedPage = state.BeginPending();
                rejectedPage.SetRejected(
                    CharacterFootGroundPathRejectReason.CurrentLandingUnavailable,
                    false,
                    0,
                    default);
                return new CharacterFootGroundPathDiagnostics(rejectedPage, false);
            }
            if (!nextLanding.Accepted)
            {
                CharacterFootGroundPathPage rejectedPage = state.BeginPending();
                rejectedPage.SetRejected(
                    CharacterFootGroundPathRejectReason.NextLandingUnavailable,
                    false,
                    0,
                    default);
                return new CharacterFootGroundPathDiagnostics(rejectedPage, false);
            }

            CharacterFootGroundPathRevisionKey key =
                CharacterFootGroundPathRevisionBuilder.BuildKey(
                    side,
                    currentLanding,
                    nextLanding,
                    authorityTick,
                    componentUp,
                    m_Settings.ProfileRevision);
            if (state.HasCommittedRevision && state.CommittedKey.Equals(key) &&
                (state.CommittedAccepted ||
                 state.CommittedAuthorityTick == key.AuthorityTick))
            {
                CharacterFootGroundPathPage committedPage = state.ReuseCommitted();
                return new CharacterFootGroundPathDiagnostics(committedPage, false);
            }

            CharacterFootGroundPathPage pendingPage = state.BeginPending();
            CharacterFootGroundDetectionSettings settings = m_Settings.GroundDetection;
            if (!CharacterFootGroundPathRevisionBuilder.TryBuild(
                    in key,
                    currentLanding.LandingPoint,
                    nextLanding.LandingPoint,
                    currentLanding.LandingNormal,
                    nextLanding.LandingNormal,
                    currentLanding.SurfaceIdentity,
                    nextLanding.SurfaceIdentity,
                    componentUp,
                    in settings,
                    out CharacterFootGroundPathRevision revision))
            {
                pendingPage.SetRejected(
                    CharacterFootGroundPathRejectReason.InvalidRequest,
                    false,
                    0,
                    default);
                return new CharacterFootGroundPathDiagnostics(pendingPage, false);
            }

            CharacterFootGroundPathQueryRequest query = revision.Query;
            CharacterFootGroundPathQueryResult result = m_WorldQuery.Query(
                in query,
                pendingPage.Contacts);
            if (result.Accepted)
            {
                if (CharacterFootGroundEnvelopeBuilder.TryBuild(
                        in revision,
                        pendingPage.Contacts,
                        state.EnvelopeWorkspace,
                        pendingPage.Envelope,
                        out CharacterFootGroundPathRejectReason envelopeRejectReason))
                {
                    pendingPage.SetAccepted(result.SegmentCount, in revision);
                }
                else
                {
                    pendingPage.SetRejected(
                        envelopeRejectReason,
                        true,
                        result.SegmentCount,
                        in revision);
                }
            }
            else
                pendingPage.SetRejected(
                    result.RejectReason,
                    true,
                    result.SegmentCount,
                    in revision);
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
            in CharacterFootPlacementFrameInput frame)
        {
            Vector3 currentSole = (animatedFoot.HeelPosition + animatedFoot.ToePosition) * 0.5f;
            CharacterFootLandingPredictionFootDiagnostics current = PredictStep(
                side,
                steps.CurrentStep,
                CharacterFootLandingStepSource.Current,
                currentSole,
                goal,
                in timeline,
                currentSegmentRemainingSeconds,
                bodyTrajectory,
                in frame);
            CharacterFootLandingPredictionFootDiagnostics incoming = PredictStep(
                side,
                steps.IncomingStep,
                CharacterFootLandingStepSource.Incoming,
                currentSole,
                goal,
                in timeline,
                currentSegmentRemainingSeconds,
                bodyTrajectory,
                in frame);
            CharacterFootLandingPredictionFootDiagnostics selected =
                steps.CurrentStep.IsAuthoritative &&
                steps.CurrentStep.TimeToLandingSeconds > 0.000001f
                    ? current
                    : steps.IncomingStep.IsAuthoritative &&
                      steps.IncomingStep.TimeToLandingSeconds > 0.000001f
                        ? incoming
                        : current;
            return new CharacterFootLandingPredictionPair(current, incoming, selected);
        }

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
                out CharacterFootLandingSupport support);
            return new CharacterFootLandingPredictionFootDiagnostics(
                side,
                accepted
                    ? CharacterFootLandingPredictionState.Accepted
                    : CharacterFootLandingPredictionState.Rejected,
                accepted
                    ? CharacterFootLandingPredictionRejectReason.None
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

        static CharacterFullBodyIkGoal CreatePelvisGoal() =>
            new CharacterFullBodyIkGoal(
                CharacterFullBodyIkEffectorSlot.PelvisPreSolveTranslation,
                Vector3.zero,
                Quaternion.identity,
                0f,
                0f,
                CharacterFullBodyIkGoalApplication.PelvisPreSolveTranslation,
                CharacterFullBodyIkGoalSourceKind.FootPlacement,
                -1);

        CharacterFullBodyIkGoal CreateFootGoal(
            CharacterFootSide side,
            CharacterFootPlacementAnimatedFootPose foot)
        {
            Transform root = m_Rig.PoseRoot;
            return new CharacterFullBodyIkGoal(
                side == CharacterFootSide.Left
                    ? CharacterFullBodyIkEffectorSlot.LeftFoot
                    : CharacterFullBodyIkEffectorSlot.RightFoot,
                root.InverseTransformPoint(foot.AnklePosition),
                (Quaternion.Inverse(root.rotation) * foot.AnkleRotation).normalized,
                0f,
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
        }
    }
}
