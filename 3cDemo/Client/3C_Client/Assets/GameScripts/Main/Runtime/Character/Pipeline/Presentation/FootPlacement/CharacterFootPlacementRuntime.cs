using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    struct CharacterFootLandingFact
    {
        internal bool HasValue;
        internal ulong LandingEventIdentity;
        internal ulong TrajectoryGeneration;
        internal string FutureBodyTranslationSourceIdentity;
        internal int SurfaceIdentity;
        internal Vector3 WorldPoint;
        internal Vector3 WorldNormal;

        internal CharacterFootGroundPathLanding Resolve() =>
            new CharacterFootGroundPathLanding(
                LandingEventIdentity,
                TrajectoryGeneration,
                FutureBodyTranslationSourceIdentity,
                SurfaceIdentity,
                WorldPoint,
                WorldNormal);

        internal void Clear()
        {
            HasValue = false;
            LandingEventIdentity = 0;
            TrajectoryGeneration = 0;
            FutureBodyTranslationSourceIdentity = string.Empty;
            SurfaceIdentity = 0;
            WorldPoint = default;
            WorldNormal = default;
        }
    }

    struct CharacterFootLandingFacts
    {
        CharacterFootLandingFact m_LastLanding;
        CharacterFootLandingFact m_NextSwingLanding;
        CharacterFootLandingFact m_PendingLastLanding;
        CharacterFootLandingFact m_PendingNextSwingLanding;
        Vector3 m_NextSwingReferencePoint;
        Vector3 m_PendingNextSwingReferencePoint;
        float m_NextSwingPredictionError;
        float m_PendingNextSwingPredictionError;
        float m_NextSwingConstraintWeight;
        float m_PendingNextSwingConstraintWeight;
        bool m_HasLastLanding;
        bool m_HasNextSwingLanding;
        bool m_HasPendingLastLanding;
        bool m_HasPendingNextSwingLanding;

        internal bool HasPendingLastLanding => m_HasPendingLastLanding;
        internal bool HasPendingNextSwingLanding => m_HasPendingNextSwingLanding;
        internal ulong PendingLastLandingEventIdentity =>
            m_HasPendingLastLanding ? m_PendingLastLanding.LandingEventIdentity : 0;
        internal float PendingNextSwingPredictionError =>
            m_HasPendingNextSwingLanding ? m_PendingNextSwingPredictionError : 0f;
        internal float PendingNextSwingConstraintWeight =>
            m_HasPendingNextSwingLanding ? m_PendingNextSwingConstraintWeight : 0f;
        internal CharacterFootGroundPathLanding ResolvePendingLastLanding() =>
            m_PendingLastLanding.Resolve();
        internal CharacterFootGroundPathLanding ResolvePendingNextSwingLanding() =>
            m_PendingNextSwingLanding.Resolve();

        internal void BeginPending()
        {
            m_PendingLastLanding = m_LastLanding;
            m_PendingNextSwingLanding = m_NextSwingLanding;
            m_PendingNextSwingReferencePoint = m_NextSwingReferencePoint;
            m_PendingNextSwingPredictionError = m_NextSwingPredictionError;
            m_PendingNextSwingConstraintWeight = m_NextSwingConstraintWeight;
            m_HasPendingLastLanding = m_HasLastLanding;
            m_HasPendingNextSwingLanding = m_HasNextSwingLanding;
        }

        internal void CaptureNextSwing(
            in AnimationBiomechanicalStepHeader step,
            in CharacterFootLandingPredictionFootDiagnostics diagnostics,
            Vector3 componentUp,
            in CharacterFootMotionSettings settings)
        {
            if (!diagnostics.Accepted || !step.IsAuthoritative ||
                !step.HasConsistentLandingEventIdentity ||
                !(step.IsPreSwing || step.IsSwing) ||
                step.TimeToLandingSeconds <= 0.000001f ||
                step.LandingEventIdentity == PendingLastLandingEventIdentity)
            {
                ClearPendingNextSwing();
                return;
            }
            if (m_HasPendingNextSwingLanding &&
                m_PendingNextSwingLanding.LandingEventIdentity == step.LandingEventIdentity)
            {
                Vector3 landingPoint = diagnostics.LandingPoint;
                Vector3 up = componentUp.sqrMagnitude > 0.000001f
                    ? componentUp.normalized
                    : Vector3.up;
                bool sameSurface =
                    m_PendingNextSwingLanding.SurfaceIdentity == diagnostics.SurfaceIdentity;
                bool sameHeight = Mathf.Abs(Vector3.Dot(
                    landingPoint - m_PendingNextSwingLanding.WorldPoint,
                    up)) <= settings.MaximumSameEventVerticalJump;
                if (!sameSurface || !sameHeight)
                    return;
                m_PendingNextSwingPredictionError = Vector3.Distance(
                    m_PendingNextSwingReferencePoint,
                    landingPoint);
                m_PendingNextSwingConstraintWeight = 1f;
                if (Vector3.Distance(
                        m_PendingNextSwingLanding.WorldPoint,
                        landingPoint) >= settings.LandingUpdateDistance ||
                    m_PendingNextSwingLanding.SurfaceIdentity != diagnostics.SurfaceIdentity)
                {
                    m_PendingNextSwingLanding = CreateFact(step, diagnostics);
                }
                return;
            }
            m_PendingNextSwingLanding = CreateFact(step, diagnostics);
            m_PendingNextSwingReferencePoint = diagnostics.LandingPoint;
            m_PendingNextSwingPredictionError = 0f;
            m_PendingNextSwingConstraintWeight = 1f;
            m_HasPendingNextSwingLanding = true;
        }

        internal void PromoteLanded(in AnimationBiomechanicalStepHeader step)
        {
            if (!step.IsAuthoritative ||
                step.TimeToLandingSeconds > 0.000001f ||
                !m_HasPendingNextSwingLanding ||
                m_PendingNextSwingLanding.LandingEventIdentity != step.LandingEventIdentity)
                return;
            m_PendingLastLanding = m_PendingNextSwingLanding;
            m_HasPendingLastLanding = true;
            ClearPendingNextSwing();
        }

        void ClearPendingNextSwing()
        {
            m_PendingNextSwingLanding.Clear();
            m_PendingNextSwingReferencePoint = default;
            m_PendingNextSwingPredictionError = 0f;
            m_PendingNextSwingConstraintWeight = 0f;
            m_HasPendingNextSwingLanding = false;
        }

        static CharacterFootLandingFact CreateFact(
            in AnimationBiomechanicalStepHeader step,
            in CharacterFootLandingPredictionFootDiagnostics diagnostics) =>
            new CharacterFootLandingFact
            {
                HasValue = true,
                LandingEventIdentity = step.LandingEventIdentity,
                TrajectoryGeneration = diagnostics.TrajectoryGeneration,
                FutureBodyTranslationSourceIdentity = diagnostics.FutureBodyTranslationSourceIdentity,
                SurfaceIdentity = diagnostics.SurfaceIdentity,
                WorldPoint = diagnostics.LandingPoint,
                WorldNormal = diagnostics.LandingNormal
            };

        internal void Seal()
        {
            m_LastLanding = m_PendingLastLanding;
            m_NextSwingLanding = m_PendingNextSwingLanding;
            m_NextSwingReferencePoint = m_PendingNextSwingReferencePoint;
            m_NextSwingPredictionError = m_PendingNextSwingPredictionError;
            m_NextSwingConstraintWeight = m_PendingNextSwingConstraintWeight;
            m_HasLastLanding = m_HasPendingLastLanding;
            m_HasNextSwingLanding = m_HasPendingNextSwingLanding;
            m_PendingLastLanding.Clear();
            m_PendingNextSwingLanding.Clear();
            m_PendingNextSwingReferencePoint = default;
            m_PendingNextSwingPredictionError = 0f;
            m_PendingNextSwingConstraintWeight = 0f;
            m_HasPendingLastLanding = false;
            m_HasPendingNextSwingLanding = false;
        }

        internal void Discard()
        {
            m_PendingLastLanding.Clear();
            m_PendingNextSwingLanding.Clear();
            m_PendingNextSwingReferencePoint = default;
            m_PendingNextSwingPredictionError = 0f;
            m_PendingNextSwingConstraintWeight = 0f;
            m_HasPendingLastLanding = false;
            m_HasPendingNextSwingLanding = false;
        }

        internal void Reset()
        {
            m_LastLanding.Clear();
            m_NextSwingLanding.Clear();
            m_PendingLastLanding.Clear();
            m_PendingNextSwingLanding.Clear();
            m_NextSwingReferencePoint = default;
            m_PendingNextSwingReferencePoint = default;
            m_NextSwingPredictionError = 0f;
            m_PendingNextSwingPredictionError = 0f;
            m_NextSwingConstraintWeight = 0f;
            m_PendingNextSwingConstraintWeight = 0f;
            m_HasLastLanding = false;
            m_HasNextSwingLanding = false;
            m_HasPendingLastLanding = false;
            m_HasPendingNextSwingLanding = false;
        }
    }

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

        CharacterFootLandingFacts m_LeftLandingFacts;
        CharacterFootLandingFacts m_RightLandingFacts;

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

            m_LeftLandingFacts.BeginPending();
            m_RightLandingFacts.BeginPending();
            m_PendingPelvisSpring = m_CommittedPelvisSpring;
            m_PendingLeftSupportLock = m_CommittedLeftSupportLock;
            m_PendingRightSupportLock = m_CommittedRightSupportLock;

            m_LeftLandingFacts.PromoteLanded(frame.Pose.LeftFootSteps.CurrentStep);
            m_RightLandingFacts.PromoteLanded(frame.Pose.RightFootSteps.CurrentStep);

            CharacterFootLandingPredictionPair leftPair = PredictFootPair(
                CharacterFootSide.Left,
                frame.Pose.LeftFootSteps,
                pose.Left,
                leftGoal,
                in timeline,
                currentSegmentRemainingSeconds,
                bodyTrajectory,
                in frame,
                m_LeftLandingFacts.PendingLastLandingEventIdentity);
            CharacterFootLandingPredictionPair rightPair = PredictFootPair(
                CharacterFootSide.Right,
                frame.Pose.RightFootSteps,
                pose.Right,
                rightGoal,
                in timeline,
                currentSegmentRemainingSeconds,
                bodyTrajectory,
                in frame,
                m_RightLandingFacts.PendingLastLandingEventIdentity);
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
            m_LeftLandingFacts.CaptureNextSwing(
                in leftSelectedStep,
                in left,
                componentUp,
                m_Settings.FootMotion);
            m_RightLandingFacts.CaptureNextSwing(
                in rightSelectedStep,
                in right,
                componentUp,
                m_Settings.FootMotion);
            bool hasLeftLastLanding = m_LeftLandingFacts.HasPendingLastLanding;
            bool hasLeftNextSwingLanding = m_LeftLandingFacts.HasPendingNextSwingLanding;
            bool hasRightLastLanding = m_RightLandingFacts.HasPendingLastLanding;
            bool hasRightNextSwingLanding = m_RightLandingFacts.HasPendingNextSwingLanding;
            CharacterFootGroundPathLanding leftLastLanding = hasLeftLastLanding
                ? m_LeftLandingFacts.ResolvePendingLastLanding()
                : default;
            CharacterFootGroundPathLanding leftNextSwingLanding = hasLeftNextSwingLanding
                ? m_LeftLandingFacts.ResolvePendingNextSwingLanding()
                : default;
            CharacterFootGroundPathLanding rightLastLanding = hasRightLastLanding
                ? m_RightLandingFacts.ResolvePendingLastLanding()
                : default;
            CharacterFootGroundPathLanding rightNextSwingLanding = hasRightNextSwingLanding
                ? m_RightLandingFacts.ResolvePendingNextSwingLanding()
                : default;
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
                    m_LeftLandingFacts.PendingNextSwingPredictionError,
                    m_LeftLandingFacts.PendingNextSwingConstraintWeight);
            CharacterFootSwingMotionDiagnostics rightSwingMotion =
                CharacterFootSwingMotionBuilder.Build(
                    pose.Right,
                    in rightCurrentStep,
                    footPlacementWeight,
                    componentUp,
                    in rightGroundPath,
                    m_RightLandingFacts.PendingNextSwingPredictionError,
                    m_RightLandingFacts.PendingNextSwingConstraintWeight);
            CharacterFootSwingMotionDiagnostics leftFootMotion = leftSwingMotion;
            CharacterFootSwingMotionDiagnostics rightFootMotion = rightSwingMotion;
            if (facts.Grounded && !leftAction.IsOccupied && !leftCurrentStep.IsSwing)
            {
                leftFootMotion = CharacterFootStrideHipsBuilder.BuildStancePlant(
                    pose.Left,
                    in leftCurrentStep,
                    footPlacementWeight,
                    componentUp,
                    hasLeftLastLanding,
                    hasLeftLastLanding ? leftLastLanding.Point : default,
                    m_Settings.FootMotion,
                    frame.PresentationDeltaSeconds,
                    ref m_PendingLeftSupportLock);
            }
            else
            {
                m_PendingLeftSupportLock.Clear();
            }
            if (facts.Grounded && !rightAction.IsOccupied && !rightCurrentStep.IsSwing)
            {
                rightFootMotion = CharacterFootStrideHipsBuilder.BuildStancePlant(
                    pose.Right,
                    in rightCurrentStep,
                    footPlacementWeight,
                    componentUp,
                    hasRightLastLanding,
                    hasRightLastLanding ? rightLastLanding.Point : default,
                    m_Settings.FootMotion,
                    frame.PresentationDeltaSeconds,
                    ref m_PendingRightSupportLock);
            }
            else
            {
                m_PendingRightSupportLock.Clear();
            }
            if (leftFootMotion.Accepted &&
                leftFootMotion.PositionWeight > 0f &&
                rightFootMotion.Accepted &&
                rightFootMotion.PositionWeight > 0f)
            {
                if (Mathf.Abs(leftFootMotion.VerticalCorrection) >=
                    Mathf.Abs(rightFootMotion.VerticalCorrection))
                {
                    rightFootMotion = default;
                    m_PendingRightSupportLock.Clear();
                }
                else
                {
                    leftFootMotion = default;
                    m_PendingLeftSupportLock.Clear();
                }
            }
            leftGoal = CreateFootGoal(
                CharacterFootSide.Left,
                pose.Left,
                in leftFootMotion);
            rightGoal = CreateFootGoal(
                CharacterFootSide.Right,
                pose.Right,
                in rightFootMotion);
            CharacterFootStrideHipsDiagnostics strideHips = ResolveStrideHips(
                in leftCurrentStep,
                in rightCurrentStep,
                hasLeftLastLanding,
                leftLastLanding,
                hasRightLastLanding,
                rightLastLanding,
                hasLeftNextSwingLanding,
                leftNextSwingLanding,
                hasRightNextSwingLanding,
                rightNextSwingLanding,
                leftGroundPath.Accepted,
                rightGroundPath.Accepted,
                facts.Grounded,
                in leftAction,
                in rightAction,
                componentUp,
                m_Rig.PoseRoot.position,
                m_Rig.PoseRoot.TransformPoint(pose.PelvisLocalPosition),
                pose.PelvisLocalPosition,
                in pose,
                in leftFootMotion,
                in rightFootMotion,
                footPlacementWeight,
                frame.PresentationDeltaSeconds);
            pelvisGoal = CreatePelvisGoal(in strideHips, m_Rig.PoseRoot);
            left = left.WithFootMotion(in leftFootMotion, leftGoal);
            right = right.WithFootMotion(in rightFootMotion, rightGoal);

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
            m_CommittedPelvisSpring = m_PendingPelvisSpring;
            m_CommittedLeftSupportLock = m_PendingLeftSupportLock;
            m_CommittedRightSupportLock = m_PendingRightSupportLock;
            m_LeftGroundPath.Seal();
            m_RightGroundPath.Seal();
            m_LeftLandingFacts.Seal();
            m_RightLandingFacts.Seal();
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
            m_LeftLandingFacts.Discard();
            m_RightLandingFacts.Discard();
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
            if (state.HasCommittedInput && state.CommittedKey.Equals(key) &&
                (state.CommittedAccepted ||
                 state.CommittedAuthorityTick == key.AuthorityTick))
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

        void ResetLandingState()
        {
            m_LeftLandingFacts.Reset();
            m_RightLandingFacts.Reset();
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
