using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal sealed class CharacterFootPlacementRuntime : IDisposable
    {
        readonly ActorId m_ActorId;
        readonly CharacterFootPlacementRuntimeSettings m_Settings;
        readonly CharacterFootPlacementPoseRig m_Rig;
        readonly ICharacterFutureBodyTranslationSource m_FutureBodyTranslationSource;
        readonly CharacterFootPlacementWorldQueryBackend m_WorldBackend;
        readonly ICharacterFootLandingWorldQuery m_LandingWorld;

        CharacterFootLandingPredictionDiagnostics m_LastDiagnostics;
        CharacterFootLandingPredictionDiagnostics m_PendingDiagnostics;
        bool m_HasPendingFrame;
        bool m_Disposed;

        internal CharacterFootPlacementRuntime(
            ActorId actorId,
            CharacterFootPlacementRuntimeSettings settings,
            CharacterFootPlacementPoseRig rig,
            PhysicsScene physicsScene,
            ICharacterFutureBodyTranslationSource futureBodyTranslationSource)
        {
            if (!actorId.IsValid || settings == null || rig == null ||
                !physicsScene.IsValid())
            {
                throw new ArgumentException("Foot Placement Runtime input is invalid.");
            }
            m_ActorId = actorId;
            m_Settings = settings;
            m_Rig = rig;
            m_FutureBodyTranslationSource = futureBodyTranslationSource;
            m_WorldBackend = new CharacterFootPlacementWorldQueryBackend(
                physicsScene,
                rig,
                settings.LandingPrediction.HitCapacity);
            m_LandingWorld = new CharacterFootLandingWorldQuery(m_WorldBackend);
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

            CharacterFootLandingPredictionFootDiagnostics left = PredictFoot(
                CharacterFootSide.Left,
                frame.Pose.LeftFootSteps,
                pose.Left,
                leftGoal,
                in timeline,
                currentSegmentRemainingSeconds,
                in frame);
            CharacterFootLandingPredictionFootDiagnostics right = PredictFoot(
                CharacterFootSide.Right,
                frame.Pose.RightFootSteps,
                pose.Right,
                rightGoal,
                in timeline,
                currentSegmentRemainingSeconds,
                in frame);

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
            m_PendingDiagnostics = default;
            m_HasPendingFrame = false;
            CharacterFootLandingPredictionDebugRegistry.Publish(in m_LastDiagnostics);
        }

        internal void DiscardPendingFrame()
        {
            RequireAlive();
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

        CharacterFootLandingPredictionFootDiagnostics PredictFoot(
            CharacterFootSide side,
            AnimationBiomechanicalStepReadPage steps,
            CharacterFootPlacementAnimatedFootPose animatedFoot,
            CharacterFullBodyIkGoal goal,
            in CommittedLocomotionPlanarMotionTimeline timeline,
            float currentSegmentRemainingSeconds,
            in CharacterFootPlacementFrameInput frame)
        {
            Vector3 currentSole = (animatedFoot.HeelPosition + animatedFoot.ToePosition) * 0.5f;
            if (!TrySelectStep(steps, out AnimationBiomechanicalStepHeader step,
                    out CharacterFootLandingStepSource stepSource,
                    out CharacterFootLandingPredictionRejectReason stepFailure))
            {
                return Rejected(
                    side,
                    stepFailure,
                    stepSource,
                    default,
                    0,
                    currentSole,
                    default,
                    default,
                    goal);
            }
            CharacterFootLandingPredictionSettings settings = m_Settings.LandingPrediction;
            if (step.TimeToLandingSeconds <= 0.000001f ||
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
            if (m_FutureBodyTranslationSource == null)
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

            var translationRequest = new CharacterFutureBodyTranslationRequest(
                m_ActorId,
                step.TimeToLandingSeconds,
                frame.Body.TargetVelocity.x,
                frame.Body.TargetVelocity.z,
                timeline.ContinuationVelocityX,
                timeline.ContinuationVelocityZ,
                currentSegmentRemainingSeconds,
                timeline.HasContinuation);
            if (!m_FutureBodyTranslationSource.TryPredict(
                    in translationRequest,
                    out CharacterFutureBodyTranslation translation) ||
                translation == null)
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
            if (translation.DurationSeconds + 0.0001f < step.TimeToLandingSeconds)
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
                translation.Evaluate(step.TimeToLandingSeconds);
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
                m_LandingWorld,
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
                true,
                in bodyTranslation,
                currentSole,
                rawLanding,
                query,
                support,
                goal);
        }

        static bool TrySelectStep(
            AnimationBiomechanicalStepReadPage steps,
            out AnimationBiomechanicalStepHeader selected,
            out CharacterFootLandingStepSource source,
            out CharacterFootLandingPredictionRejectReason failure)
        {
            AnimationBiomechanicalStepHeader current = steps.CurrentStep;
            AnimationBiomechanicalStepHeader incoming = steps.IncomingStep;
            if (current.IsAuthoritative && current.TimeToLandingSeconds > 0.000001f)
            {
                selected = current;
                source = CharacterFootLandingStepSource.Current;
            }
            else if (incoming.IsAuthoritative && incoming.TimeToLandingSeconds > 0.000001f)
            {
                selected = incoming;
                source = CharacterFootLandingStepSource.Incoming;
            }
            else
            {
                selected = default;
                source = CharacterFootLandingStepSource.None;
                bool identityMismatch =
                    current.HasLandingEvent && !current.HasConsistentLandingEventIdentity ||
                    incoming.HasLandingEvent && !incoming.HasConsistentLandingEventIdentity;
                failure = identityMismatch
                    ? CharacterFootLandingPredictionRejectReason.StepIdentityMismatch
                    : CharacterFootLandingPredictionRejectReason.StepUnavailable;
                return false;
            }
            if (!selected.HasConsistentLandingEventIdentity)
            {
                failure = CharacterFootLandingPredictionRejectReason.StepIdentityMismatch;
                return false;
            }
            failure = CharacterFootLandingPredictionRejectReason.None;
            return true;
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
        }
    }
}
