using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal sealed class CharacterPredictiveFootPlacementPlanner
    {
        enum FootPlanTransitionKind : byte
        {
            None = 0,
            IntentRevision = 1,
            EventSuccessor = 2
        }

        sealed class FootPlanRuntime
        {
            internal FootPlanRuntime(CharacterFootSide side, int pathCapacity)
            {
                Active = new CharacterPredictiveFootPlacementPlan(side, pathCapacity);
                Revision = new CharacterPredictiveFootPlacementPlan(side, pathCapacity);
            }

            internal CharacterPredictiveFootPlacementPlan Active { get; private set; }
            internal CharacterPredictiveFootPlacementPlan Revision { get; private set; }
            internal bool HasRevision { get; private set; }
            internal bool HasEventSuccessor =>
                HasRevision && TransitionKind == FootPlanTransitionKind.EventSuccessor;
            internal bool HasIntentRevision =>
                HasRevision && TransitionKind == FootPlanTransitionKind.IntentRevision;
            internal float RevisionBlendWeight { get; private set; }
            internal float SmoothedRevisionBlendWeight =>
                RevisionBlendWeight * RevisionBlendWeight * (3f - 2f * RevisionBlendWeight);
            internal bool IsFadingOut { get; private set; }
            internal float FadeOutWeight { get; private set; }
            internal ulong FadeOutStartedFrame { get; private set; }
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
            internal ulong IntentRevisionAttemptPlanSequence { get; private set; }
            internal ulong IntentRevisionAttemptTrajectoryGeneration { get; private set; }
            internal ulong IntentRevisionAttemptAuthorityTick { get; private set; }
            internal bool HasLastOutputSole { get; private set; }
            internal Vector3 LastOutputSole { get; private set; }
            internal Vector3 LastOutputAnimatedAnklePosition { get; private set; }
            internal Quaternion LastOutputAnimatedAnkleRotation { get; private set; }
            internal Vector3 LastOutputCurrentHip { get; private set; }
            internal Vector3 LastOutputAnklePosition { get; private set; }
            internal Quaternion LastOutputAnkleRotation { get; private set; }
            internal bool HasLastOutputGroundPath { get; private set; }
            internal Vector3 LastOutputGroundPath { get; private set; }
            internal FootPlacementSurface LastOutputGroundSupport { get; private set; }
            internal ulong LastOutputGroundPlanSequence { get; private set; }
            internal Vector3 LastOutputPathRoot { get; private set; }
            internal Vector3 LastOutputPathRootStart { get; private set; }
            internal Vector3 LastOutputPathHip { get; private set; }
            internal bool HasTransitionOrigin { get; private set; }
            internal Vector3 TransitionOriginAnkleOffset { get; private set; }
            internal Quaternion TransitionOriginAnkleRotationOffset { get; private set; }
            internal Vector3 TransitionOriginGroundPath { get; private set; }
            internal FootPlacementSurface TransitionOriginGroundSupport { get; private set; }
            internal Vector3 TransitionOriginPathRootOffset { get; private set; }
            internal Vector3 TransitionOriginPathRootStartOffset { get; private set; }
            internal Vector3 TransitionOriginPathHipOffset { get; private set; }
            internal ulong CommittedAnchorPlanSequence { get; private set; }
            internal ulong CommittedAnchorLandingEventIdentity { get; private set; }
            internal Vector3 CommittedAnchorSole { get; private set; }
            internal FootPlacementSurface CommittedAnchorSupport { get; private set; }
            FootPlanTransitionKind TransitionKind { get; set; }
            ulong m_OutputContinuityPlanSequence;
            ulong m_OutputContinuityStartedFrame;
            float m_OutputContinuityWeight;
            Vector3 m_OutputContinuityPositionOffset;
            Quaternion m_OutputContinuityRotationOffset = Quaternion.identity;

            internal void BeginFrame()
            {
                Active.BeginFrame();
                Revision.BeginFrame();
            }

            internal void BeginIntentRevision()
            {
                if (!Revision.HasExecutablePath)
                    throw new InvalidOperationException("Predictive Foot revision is not executable.");
                CaptureTransitionOrigin();
                HasRevision = true;
                TransitionKind = FootPlanTransitionKind.IntentRevision;
                RevisionBlendWeight = 0f;
                IsFadingOut = false;
                FadeOutWeight = 0f;
                FadeOutStartedFrame = 0;
            }

            internal void BeginEventSuccessor()
            {
                if (!Revision.HasExecutablePath)
                    throw new InvalidOperationException("Predictive Foot successor is not executable.");
                ClearTransitionOrigin();
                HasRevision = true;
                TransitionKind = FootPlanTransitionKind.EventSuccessor;
                RevisionBlendWeight = 0f;
                IsFadingOut = false;
                FadeOutWeight = 0f;
                FadeOutStartedFrame = 0;
            }

            internal void PromoteRevision()
            {
                if (!HasRevision || !Revision.HasExecutablePath)
                    throw new InvalidOperationException("Predictive Foot revision cannot be promoted.");
                Active.Reset(CharacterPredictiveFootPlanEndReason.EventReplaced);
                CharacterPredictiveFootPlacementPlan retired = Active;
                Active = Revision;
                Revision = retired;
                HasRevision = false;
                TransitionKind = FootPlanTransitionKind.None;
                RevisionBlendWeight = 0f;
                ClearTransitionOrigin();
            }

            internal void BeginFadeOut(
                CharacterPredictiveFootPlanEndReason reason,
                ulong renderFrame)
            {
                if (renderFrame == 0)
                    throw new ArgumentOutOfRangeException(nameof(renderFrame));
                if (IsFadingOut)
                {
                    if (Revision.OwnsEvent)
                        Revision.Reset(reason);
                    HasRevision = false;
                    TransitionKind = FootPlanTransitionKind.None;
                    RevisionBlendWeight = 0f;
                    return;
                }
                CancelRevision(reason);
                if (!Active.HasExecutablePath || !HasCompleteOutputForPlan(Active.Sequence))
                {
                    Active.Reset(reason);
                    IsFadingOut = false;
                    FadeOutWeight = 0f;
                    FadeOutStartedFrame = 0;
                    ClearTransitionOrigin();
                    return;
                }
                CaptureTransitionOrigin();
                IsFadingOut = true;
                FadeOutWeight = 0f;
                FadeOutStartedFrame = renderFrame;
            }

            internal void AdvanceTransition(
                ulong renderFrame,
                float deltaSeconds,
                float blendSpeed)
            {
                bool blendRevision = HasIntentRevision;
                if (blendRevision)
                {
                    if (Revision.State != CharacterPredictiveFootPlanState.Executing)
                    {
                        RevisionBlendWeight = 0f;
                        return;
                    }
                    if (renderFrame <= Revision.GeneratedFrame)
                        return;
                    RevisionBlendWeight = Mathf.MoveTowards(
                        RevisionBlendWeight,
                        1f,
                        blendSpeed * deltaSeconds);
                    if (RevisionBlendWeight < 0.999999f)
                        return;
                    PromoteRevision();
                    return;
                }
                if (!IsFadingOut)
                    return;
                if (renderFrame <= FadeOutStartedFrame)
                    return;
                FadeOutWeight = Mathf.MoveTowards(
                    FadeOutWeight,
                    1f,
                    blendSpeed * deltaSeconds);
                if (FadeOutWeight < 0.999999f)
                    return;
                Active.Reset(CharacterPredictiveFootPlanEndReason.EventReplaced);
                IsFadingOut = false;
                FadeOutWeight = 0f;
                FadeOutStartedFrame = 0;
                ClearTransitionOrigin();
            }

            internal void CancelRevision(CharacterPredictiveFootPlanEndReason reason)
            {
                if (Revision.OwnsEvent)
                    Revision.Reset(reason);
                HasRevision = false;
                TransitionKind = FootPlanTransitionKind.None;
                RevisionBlendWeight = 0f;
                ClearTransitionOrigin();
            }

            internal bool TryGetCommittedLanding(
                ulong landingEventIdentity,
                out Vector3 sole,
                out FootPlacementSurface support)
            {
                bool available = landingEventIdentity != 0 &&
                                 CommittedAnchorLandingEventIdentity == landingEventIdentity &&
                                 IsFinite(CommittedAnchorSole) &&
                                 CommittedAnchorSupport.IsValid;
                sole = available ? CommittedAnchorSole : default;
                support = available ? CommittedAnchorSupport.Rebuild() : default;
                return available && support.IsValid;
            }

            internal void ObserveStance(
                bool hasCommittedAnchor,
                ulong anchorPlanSequence,
                ulong anchorLandingEventIdentity,
                Vector3 anchorSole,
                FootPlacementSurface anchorSupport)
            {
                bool valid = hasCommittedAnchor &&
                             anchorPlanSequence != 0 &&
                             anchorLandingEventIdentity != 0 &&
                             IsFinite(anchorSole) &&
                             anchorSupport.IsValid;
                CommittedAnchorPlanSequence = valid ? anchorPlanSequence : 0;
                CommittedAnchorLandingEventIdentity = valid
                    ? anchorLandingEventIdentity
                    : 0;
                CommittedAnchorSole = valid ? anchorSole : default;
                CommittedAnchorSupport = valid ? anchorSupport : default;
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

            internal bool HasAttemptedIntentRevision(
                ulong planSequence,
                ulong trajectoryGeneration,
                ulong authorityTick) =>
                planSequence != 0 &&
                trajectoryGeneration != 0 &&
                authorityTick != 0 &&
                IntentRevisionAttemptPlanSequence == planSequence &&
                IntentRevisionAttemptTrajectoryGeneration == trajectoryGeneration &&
                IntentRevisionAttemptAuthorityTick == authorityTick;

            internal void MarkIntentRevisionAttempt(
                ulong planSequence,
                in CommittedLocomotionPlanarMotionTimeline motionTimeline)
            {
                if (planSequence == 0 || !motionTimeline.IsValid)
                    throw new ArgumentOutOfRangeException(nameof(planSequence));
                IntentRevisionAttemptPlanSequence = planSequence;
                IntentRevisionAttemptTrajectoryGeneration = motionTimeline.Generation;
                IntentRevisionAttemptAuthorityTick = motionTimeline.AuthorityTick.Value;
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
                LastOutputAnimatedAnklePosition = animatedAnklePosition;
                LastOutputAnimatedAnkleRotation = animatedAnkleRotation.normalized;
                LastOutputCurrentHip = currentHip;
                LastOutputAnklePosition = anklePosition;
                LastOutputAnkleRotation = ankleRotation.normalized;
                LastOutputSole = sole;
                HasLastOutputSole = true;
                if (!groundSupport.IsValid || !IsFinite(groundPath) || groundPlanSequence == 0 ||
                    !IsFinite(pathRoot) || !IsFinite(pathRootStart) || !IsFinite(pathHip))
                {
                    HasLastOutputGroundPath = false;
                    LastOutputGroundPath = Vector3.zero;
                    LastOutputGroundSupport = default;
                    LastOutputGroundPlanSequence = 0;
                    LastOutputPathRoot = Vector3.zero;
                    LastOutputPathRootStart = Vector3.zero;
                    LastOutputPathHip = Vector3.zero;
                    return;
                }
                LastOutputGroundPath = groundPath;
                LastOutputGroundSupport = new FootPlacementSurface(
                    groundSupport.Collider,
                    groundPath,
                    groundSupport.Normal.normalized);
                HasLastOutputGroundPath = LastOutputGroundSupport.IsValid;
                LastOutputGroundPlanSequence = HasLastOutputGroundPath
                    ? groundPlanSequence
                    : 0;
                LastOutputPathRoot = pathRoot;
                LastOutputPathRootStart = pathRootStart;
                LastOutputPathHip = pathHip;
            }

            internal bool HasCompleteOutputForPlan(ulong planSequence) =>
                planSequence != 0 &&
                HasLastOutputSole &&
                HasLastOutputGroundPath &&
                LastOutputGroundPlanSequence == planSequence &&
                IsFinite(LastOutputAnimatedAnklePosition) &&
                IsFinite(LastOutputAnimatedAnkleRotation) &&
                IsFinite(LastOutputCurrentHip) &&
                IsFinite(LastOutputAnklePosition) &&
                IsFinite(LastOutputAnkleRotation) &&
                IsFinite(LastOutputPathRoot) &&
                IsFinite(LastOutputPathRootStart) &&
                IsFinite(LastOutputPathHip);

            internal void PromoteUncommittedRevision(CharacterPredictiveFootPlanEndReason reason)
            {
                if (!Revision.HasExecutablePath)
                    throw new InvalidOperationException("Predictive Foot uncommitted revision is not executable.");
                Active.Reset(reason);
                CharacterPredictiveFootPlacementPlan retired = Active;
                Active = Revision;
                Revision = retired;
                HasRevision = false;
                TransitionKind = FootPlanTransitionKind.None;
                RevisionBlendWeight = 0f;
                IsFadingOut = false;
                FadeOutWeight = 0f;
                FadeOutStartedFrame = 0;
                ClearTransitionOrigin();
                ClearIntentObservation();
            }

            internal void ResolveOutputContinuity(
                ulong renderFrame,
                float deltaSeconds,
                float blendSpeed,
                bool baselineOwnsFoot,
                bool targetAvailable,
                ulong targetPlanSequence,
                Vector3 targetPosition,
                Quaternion targetRotation,
                out Vector3 resolvedPosition,
                out Quaternion resolvedRotation)
            {
                resolvedPosition = targetPosition;
                resolvedRotation = targetRotation;
                if (baselineOwnsFoot || !targetAvailable || targetPlanSequence == 0 ||
                    !IsFinite(targetPosition) || !IsFinite(targetRotation))
                {
                    ClearOutputContinuity();
                    return;
                }
                bool outputOwnerChanged = LastOutputGroundPlanSequence != targetPlanSequence;
                if (outputOwnerChanged &&
                    m_OutputContinuityPlanSequence != targetPlanSequence &&
                    HasLastOutputSole && IsFinite(LastOutputAnklePosition) &&
                    IsFinite(LastOutputAnkleRotation))
                {
                    m_OutputContinuityPlanSequence = targetPlanSequence;
                    m_OutputContinuityStartedFrame = renderFrame;
                    m_OutputContinuityWeight = 1f;
                    m_OutputContinuityPositionOffset = LastOutputAnklePosition - targetPosition;
                    m_OutputContinuityRotationOffset = (
                        LastOutputAnkleRotation * Quaternion.Inverse(targetRotation)).normalized;
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
                HasRevision = false;
                TransitionKind = FootPlanTransitionKind.None;
                RevisionBlendWeight = 0f;
                IsFadingOut = false;
                FadeOutWeight = 0f;
                FadeOutStartedFrame = 0;
                ClearIntentObservation();
                HasLastOutputSole = false;
                LastOutputSole = Vector3.zero;
                LastOutputAnimatedAnklePosition = Vector3.zero;
                LastOutputAnimatedAnkleRotation = Quaternion.identity;
                LastOutputCurrentHip = Vector3.zero;
                LastOutputAnklePosition = Vector3.zero;
                LastOutputAnkleRotation = Quaternion.identity;
                HasLastOutputGroundPath = false;
                LastOutputGroundPath = Vector3.zero;
                LastOutputGroundSupport = default;
                LastOutputGroundPlanSequence = 0;
                LastOutputPathRoot = Vector3.zero;
                LastOutputPathRootStart = Vector3.zero;
                LastOutputPathHip = Vector3.zero;
                ClearTransitionOrigin();
                CommittedAnchorPlanSequence = 0;
                CommittedAnchorLandingEventIdentity = 0;
                CommittedAnchorSole = default;
                CommittedAnchorSupport = default;
                IntentRevisionAttemptPlanSequence = 0;
                IntentRevisionAttemptTrajectoryGeneration = 0;
                IntentRevisionAttemptAuthorityTick = 0;
                ClearOutputContinuity();
            }

            static bool IsFinite(Vector3 value) =>
                float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

            static bool IsFinite(Quaternion value) =>
                float.IsFinite(value.x) && float.IsFinite(value.y) &&
                float.IsFinite(value.z) && float.IsFinite(value.w);

            void CaptureTransitionOrigin()
            {
                if (!HasCompleteOutputForPlan(Active.Sequence))
                    throw new InvalidOperationException("Predictive Foot transition origin is unavailable.");
                HasTransitionOrigin = true;
                TransitionOriginAnkleOffset =
                    LastOutputAnklePosition - LastOutputAnimatedAnklePosition;
                TransitionOriginAnkleRotationOffset = (
                    LastOutputAnkleRotation *
                    Quaternion.Inverse(LastOutputAnimatedAnkleRotation)).normalized;
                TransitionOriginGroundPath = LastOutputGroundPath;
                TransitionOriginGroundSupport = LastOutputGroundSupport.Rebuild();
                TransitionOriginPathRootOffset = LastOutputPathRoot - LastOutputCurrentHip;
                TransitionOriginPathRootStartOffset =
                    LastOutputPathRootStart - LastOutputCurrentHip;
                TransitionOriginPathHipOffset = LastOutputPathHip - LastOutputCurrentHip;
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
                anklePosition = animatedAnklePosition + TransitionOriginAnkleOffset;
                ankleRotation = (
                    TransitionOriginAnkleRotationOffset *
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
                pathRoot = currentHip + TransitionOriginPathRootOffset;
                pathRootStart = currentHip + TransitionOriginPathRootStartOffset;
                pathHip = currentHip + TransitionOriginPathHipOffset;
            }

            void ClearTransitionOrigin()
            {
                HasTransitionOrigin = false;
                TransitionOriginAnkleOffset = Vector3.zero;
                TransitionOriginAnkleRotationOffset = Quaternion.identity;
                TransitionOriginGroundPath = Vector3.zero;
                TransitionOriginGroundSupport = default;
                TransitionOriginPathRootOffset = Vector3.zero;
                TransitionOriginPathRootStartOffset = Vector3.zero;
                TransitionOriginPathHipOffset = Vector3.zero;
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
        readonly FootPlanRuntime m_LeftPlan;
        readonly FootPlanRuntime m_RightPlan;
        CharacterPredictiveFootStanceInput m_LeftLandingHandoff;
        CharacterPredictiveFootStanceInput m_RightLandingHandoff;
        ulong m_NextPlanSequence = 1;
        ulong m_PreparedRenderFrame;
        ulong m_PreparedCompletionIdentity;
        float m_TrajectoryCurvatureDegreesPerSecond;
        bool m_TrajectoryCurvatureAvailable;

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
            m_LeftPlan = new FootPlanRuntime(
                CharacterFootSide.Left,
                CharacterPredictiveFootPlacementQuery.MaximumPathPointCapacity);
            m_RightPlan = new FootPlanRuntime(
                CharacterFootSide.Right,
                CharacterPredictiveFootPlacementQuery.MaximumPathPointCapacity);
        }

        internal CharacterPredictiveFootPlacementDiagnostics Diagnostics => m_Diagnostics;

        internal void ObserveStance(
            CharacterFootSide side,
            bool hasCommittedAnchor,
            ulong anchorPlanSequence,
            ulong anchorLandingEventIdentity,
            Vector3 anchorSole,
            FootPlacementSurface anchorSupport)
        {
            FootPlanRuntime runtime = side == CharacterFootSide.Left
                ? m_LeftPlan
                : side == CharacterFootSide.Right
                    ? m_RightPlan
                    : throw new ArgumentOutOfRangeException(nameof(side));
            runtime.ObserveStance(
                hasCommittedAnchor,
                anchorPlanSequence,
                anchorLandingEventIdentity,
                anchorSole,
                anchorSupport);
        }

        internal void Prepare(
            in CharacterFootPlacementPlanningFrame frame,
            in CharacterFootPlacementAnimatedPose pose,
            Vector3 leftGroundProbeStart,
            FootPlacementSurface leftGroundProbeSupport,
            Vector3 rightGroundProbeStart,
            FootPlacementSurface rightGroundProbeSupport)
        {
            if (frame.ActorId != m_ActorId || !frame.Body.IsValid ||
                frame.RenderFrame == 0 || frame.CompletionIdentity == 0 ||
                frame.RenderFrame == m_PreparedRenderFrame)
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
                m_LeftPlan,
                pose.Left,
                leftFeature,
                leftGroundProbeStart,
                leftGroundProbeSupport,
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
                frame.PresentationDeltaSeconds,
                out m_LeftLandingHandoff);
            PrepareFoot(
                CharacterFootSide.Right,
                m_RightPlan,
                pose.Right,
                rightFeature,
                rightGroundProbeStart,
                rightGroundProbeSupport,
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
                frame.PresentationDeltaSeconds,
                out m_RightLandingHandoff);
            m_PreparedRenderFrame = frame.RenderFrame;
            m_PreparedCompletionIdentity = frame.CompletionIdentity;
        }

        internal CharacterPredictiveFootStanceInput GetStanceInput(
            CharacterFootSide side,
            ulong renderFrame,
            ulong completionIdentity,
            AnimationFootFeatureSample feature,
            CharacterFootPlacementAnimatedFootPose pose)
        {
            RequirePrepared(renderFrame, completionIdentity);
            FootPlanRuntime runtime = side == CharacterFootSide.Left
                ? m_LeftPlan
                : side == CharacterFootSide.Right
                    ? m_RightPlan
                    : throw new ArgumentOutOfRangeException(nameof(side));
            CharacterPredictiveFootPlacementPlan plan = runtime.Active;
            CharacterPredictiveFootStanceInput landingHandoff = side == CharacterFootSide.Left
                ? m_LeftLandingHandoff
                : m_RightLandingHandoff;
            AnimationPredictedFootStepSample step = feature.PredictedStep;
            if (!step.IsAuthoritative && !plan.HasExecutablePath)
                return default;
            bool activePlanMatches = step.IsAuthoritative &&
                                     plan.MatchesAuthoritativeEvent(in step);
            CharacterPredictiveFootPlacementPlan revision = runtime.Revision;
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
            bool hasTransitionOrigin = runtime.HasTransitionOrigin &&
                                       (runtime.HasIntentRevision || runtime.IsFadingOut);
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
                float blend = runtime.SmoothedRevisionBlendWeight;
                pathPosition = Vector3.Lerp(pathPosition, revisionPathPosition, blend);
                pathRoot = Vector3.Lerp(pathRoot, revisionPathRoot, blend);
                pathRootStart = Vector3.Lerp(pathRootStart, revisionPathRootStart, blend);
                pathHip = Vector3.Lerp(pathHip, revisionPathHip, blend);
            }
            CharacterPredictiveFootTarget activeTarget = default;
            bool activeTargetAvailable = !runtime.IsFadingOut &&
                                         TryEvaluateFootTarget(
                                             plan,
                                             plan.ActionStepPhase,
                                             pose,
                                             m_Rig.PoseRoot.up.normalized,
                                             pose.HipPosition,
                                             0f,
                                             out activeTarget);
            if (activeTargetAvailable)
                targetAnklePosition = activeTarget.AnklePosition;
            if (hasTransitionOrigin)
                targetAnklePosition = transitionOriginAnklePosition;
            if (revisionContributes &&
                TryEvaluateFootTarget(
                    revision,
                    revision.ActionStepPhase,
                    pose,
                    m_Rig.PoseRoot.up.normalized,
                    pose.HipPosition,
                    0f,
                    out CharacterPredictiveFootTarget revisionTarget))
            {
                targetAnklePosition = activeTargetAvailable || hasTransitionOrigin
                    ? Vector3.Lerp(
                        hasTransitionOrigin
                            ? transitionOriginAnklePosition
                            : activeTarget.AnklePosition,
                        revisionTarget.AnklePosition,
                        runtime.SmoothedRevisionBlendWeight)
                    : revisionTarget.AnklePosition;
            }
            CharacterPredictiveFootPlacementPlan contactPlan =
                runtime.HasIntentRevision && revisionMatches
                    ? revision
                    : activePlanMatches
                        ? plan
                        : null;
            if (landingHandoff.HasContactTarget)
            {
                hasContactTarget = true;
                contactSurface = landingHandoff.ContactSurface;
                contactAnklePosition = landingHandoff.ContactAnklePosition;
                contactAnkleRotation = landingHandoff.ContactAnkleRotation;
                contactPlanSequence = landingHandoff.ContactPlanSequence;
                contactLandingEventIdentity = landingHandoff.ContactLandingEventIdentity;
            }
            else if (contactPlan != null &&
                contactPlan.State == CharacterPredictiveFootPlanState.Executing &&
                IsMotionWithinCommitTolerance(contactPlan) &&
                supportPhase == AnimationFootSupportPhase.ApproachingContact &&
                TryEvaluateFootTarget(
                    contactPlan,
                    contactPlan.ActionStepPhase,
                    pose,
                    m_Rig.PoseRoot.up.normalized,
                    pose.HipPosition,
                    0f,
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
            CharacterPredictiveFootPlacementPlan timingPlan = revisionContributes
                ? revision
                : plan;
            float activePredictiveOutputWeight = plan.State == CharacterPredictiveFootPlanState.Executing
                ? plan.EvaluatePredictiveOutputWeight() *
                  runtime.PredictiveRetentionWeight
                : 0f;
            float revisionPredictiveOutputWeight = revisionContributes
                ? revision.EvaluatePredictiveOutputWeight()
                : 0f;
            float predictiveOutputWeight = revisionContributes
                ? Mathf.Lerp(
                    activePredictiveOutputWeight,
                    revisionPredictiveOutputWeight,
                    runtime.SmoothedRevisionBlendWeight)
                : activePredictiveOutputWeight;
            float remainingSeconds = Mathf.Max(
                0f,
                currentEventOwnsState
                    ? step.ActionStepClock.TimeToLandingSeconds
                    : (1f - timingPlan.ActionStepPhase) * timingPlan.ActionStepDurationSeconds);
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

        internal void Resolve(
            in CharacterFootPlacementPlanningFrame frame,
            in CharacterFullBodyIkGoalSetHeader ownerHeader,
            in CharacterFootGroundingPlan baseline,
            in CharacterFootGroundingDiagnostics baselineDiagnostics,
            out CharacterFootGroundingPlan result)
        {
            RequireValidInput(
                in frame,
                in ownerHeader,
                in baseline,
                in baselineDiagnostics);
            RequirePrepared(frame.RenderFrame, frame.CompletionIdentity);
            CharacterFootPlacementAnimatedPose pose = m_Rig.CaptureAnimatedPose(
                frame.RenderFrame,
                frame.UpstreamPose.DenseComponentPoses);
            CharacterFootPlacementPoseInput upstreamPose = frame.UpstreamPose;
            AnimationPredictedFootStepSample leftStep =
                upstreamPose.LeftFootFeatures.PredictedStep;
            AnimationPredictedFootStepSample rightStep =
                upstreamPose.RightFootFeatures.PredictedStep;
            float leftEventPoseWeight = ResolveCurrentEventFootPoseWeight(
                in upstreamPose,
                CharacterFootSide.Left,
                in leftStep);
            float rightEventPoseWeight = ResolveCurrentEventFootPoseWeight(
                in upstreamPose,
                CharacterFootSide.Right,
                in rightStep);
            CharacterFullBodyIkGoal left = ModifyFoot(
                CharacterFootSide.Left,
                m_LeftPlan,
                pose.Left,
                frame.UpstreamPose.LeftFootFeatures,
                leftEventPoseWeight,
                baseline.LeftFoot,
                baselineDiagnostics.Left,
                frame.RenderFrame,
                frame.PresentationDeltaSeconds,
                m_Rig.LeftLegLength,
                ResolveAppliedHip(pose.Left.HipPosition, baseline.Pelvis),
                out CharacterPredictiveFootPlacementFootDiagnostics leftDiagnostics,
                out CharacterPredictiveFootLegFrameSnapshot leftDebugSnapshot);
            CharacterFullBodyIkGoal right = ModifyFoot(
                CharacterFootSide.Right,
                m_RightPlan,
                pose.Right,
                frame.UpstreamPose.RightFootFeatures,
                rightEventPoseWeight,
                baseline.RightFoot,
                baselineDiagnostics.Right,
                frame.RenderFrame,
                frame.PresentationDeltaSeconds,
                m_Rig.RightLegLength,
                ResolveAppliedHip(pose.Right.HipPosition, baseline.Pelvis),
                out CharacterPredictiveFootPlacementFootDiagnostics rightDiagnostics,
                out CharacterPredictiveFootLegFrameSnapshot rightDebugSnapshot);
            m_Diagnostics = new CharacterPredictiveFootPlacementDiagnostics(
                frame.RenderFrame,
                frame.CompletionIdentity,
                in ownerHeader,
                in leftDiagnostics,
                in rightDiagnostics);
            CharacterPredictiveFootPlacementDebugSnapshotRegistry.Publish(
                new CharacterPredictiveFootFrameSnapshot(
                    m_ActorId,
                    frame.RenderFrame,
                    frame.CompletionIdentity,
                    in leftDebugSnapshot,
                    in rightDebugSnapshot));
            result = new CharacterFootGroundingPlan(
                baseline.Pelvis,
                left,
                right);
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

        internal void Reset()
        {
            m_LeftPlan.Reset(CharacterPredictiveFootPlanEndReason.PresentationReset);
            m_RightPlan.Reset(CharacterPredictiveFootPlanEndReason.PresentationReset);
            m_NextPlanSequence = 1;
            m_LeftLandingHandoff = default;
            m_RightLandingHandoff = default;
            m_PreparedRenderFrame = 0;
            m_PreparedCompletionIdentity = 0;
            m_TrajectoryCurvatureDegreesPerSecond = 0f;
            m_TrajectoryCurvatureAvailable = false;
            m_Diagnostics = default;
            CharacterPredictiveFootPlacementDebugSnapshotRegistry.Remove(m_ActorId);
        }

        CharacterFullBodyIkGoal ModifyFoot(
            CharacterFootSide side,
            FootPlanRuntime runtime,
            CharacterFootPlacementAnimatedFootPose pose,
            AnimationFootFeatureSample feature,
            float currentEventFootPoseWeight,
            CharacterFullBodyIkGoal baseline,
            CharacterFootGroundingFootDiagnostics grounding,
            ulong renderFrame,
            float presentationDeltaSeconds,
            float legLength,
            Vector3 appliedHip,
            out CharacterPredictiveFootPlacementFootDiagnostics diagnostics,
            out CharacterPredictiveFootLegFrameSnapshot debugSnapshot)
        {
            CharacterPredictiveFootPlacementPlan plan = runtime.Active;
            AnimationPredictedFootStepSample step = feature.PredictedStep;
            bool landingEventIdentityValid = step.HasConsistentLandingEventIdentity(side);
            Transform component = m_Rig.PoseRoot;
            Vector3 up = component.up.normalized;
            Vector3 baselineWorldPosition = component.TransformPoint(baseline.ComponentPosition);
            Quaternion baselineWorldRotation = (component.rotation * baseline.ComponentRotation).normalized;
            CharacterFootPlacementSoleContactPose baselineContacts = pose.ResolveSoleContacts(
                baselineWorldPosition,
                baselineWorldRotation);
            CharacterFootPlacementSoleContactPose nativeContacts = pose.ResolveSoleContacts(
                pose.AnklePosition,
                pose.AnkleRotation);
            Vector3 currentSole = (nativeContacts.HeelPosition + nativeContacts.ToePosition) * 0.5f;
            CharacterFullBodyIkGoal result = baseline;
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
            CharacterPredictiveFootPlacementPlan revisionPlan = runtime.Revision;
            bool hasIntentRevision = runtime.HasIntentRevision;
            bool hasTransitionOrigin = runtime.HasTransitionOrigin &&
                                       (hasIntentRevision || runtime.IsFadingOut);
            float predictiveOutputWeight = plan.State == CharacterPredictiveFootPlanState.Executing
                ? plan.EvaluatePredictiveOutputWeight()
                : 0f;
            float revisionPredictiveOutputWeight = hasIntentRevision &&
                                                   revisionPlan.State == CharacterPredictiveFootPlanState.Executing
                ? revisionPlan.EvaluatePredictiveOutputWeight()
                : 0f;
            float revisionTransitionBlend = hasIntentRevision
                ? runtime.SmoothedRevisionBlendWeight
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
            bool stanceOwnsFoot = actionConstraintOwnsFoot || physicalStanceOwnsFoot;
            bool currentSupportOwnsIdle = !step.IsAuthoritative &&
                                          plan.State == CharacterPredictiveFootPlanState.Inactive;
            bool baselineOwnsFoot = physicalStanceOwnsFoot || currentSupportOwnsIdle;
            if (!stanceOwnsFoot && !currentSupportOwnsIdle)
            {
                result = new CharacterFullBodyIkGoal(
                    baseline.Slot,
                    component.InverseTransformPoint(pose.AnklePosition),
                    (Quaternion.Inverse(component.rotation) * pose.AnkleRotation).normalized,
                    baseline.PositionWeight,
                    baseline.RotationWeight,
                    baseline.Application,
                    baseline.SourceKind,
                    baseline.DiagnosticMetadataIndex);
            }
            CharacterPredictiveFootTarget targetData = default;
            FootPredictionRejectReason activeEvaluationRejectReason = FootPredictionRejectReason.None;
            bool activeTargetAvailable = !runtime.IsFadingOut &&
                                         TryEvaluateFootTarget(
                                             plan,
                                             plan.ActionStepPhase,
                                             pose,
                                             up,
                                             appliedHip,
                                             legLength * m_Settings.MaximumPredictionReachRatio,
                                             out targetData,
                                             out activeEvaluationRejectReason);
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
                    renderFrame);
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
            bool revisionTargetAvailable = hasIntentRevision &&
                                           TryEvaluateFootTarget(
                                               revisionPlan,
                                               revisionPlan.ActionStepPhase,
                                               pose,
                                               up,
                                               appliedHip,
                                               legLength * m_Settings.MaximumPredictionReachRatio,
                                               out revisionTargetData,
                                               out revisionEvaluationRejectReason);
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
                Vector3 transitionOriginAnklePosition = default;
                Quaternion transitionOriginAnkleRotation = default;
                Vector3 transitionOriginPathRoot = default;
                Vector3 transitionOriginPathRootStart = default;
                Vector3 transitionOriginPathHip = default;
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
                Vector3 predictiveAnklePosition = revisionTargetAvailable
                    ? Vector3.Lerp(outgoingAnklePosition, revisionTargetData.AnklePosition, revisionBlend)
                    : outgoingAnklePosition;
                clearanceEvaluated = true;
                currentPathPosition = revisionTargetAvailable
                    ? Vector3.Lerp(outgoingPathPosition, revisionTargetData.PathPosition, revisionBlend)
                    : outgoingPathPosition;
                currentPathRoot = revisionTargetAvailable
                    ? Vector3.Lerp(outgoingPathRoot, revisionTargetData.PathRoot, revisionBlend)
                    : outgoingPathRoot;
                currentPathHip = revisionTargetAvailable
                    ? Vector3.Lerp(outgoingPathHip, revisionTargetData.PathHip, revisionBlend)
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
                currentPathSupport = revisionTargetAvailable && revisionBlend >= 0.5f
                    ? revisionTargetData.Support
                    : outgoingPathSupport;
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
                Vector3 pathSurfaceNormal = currentPathSupport.IsValid
                    ? currentPathSupport.Normal.normalized
                    : up;
                Vector3 baselineSole = (baselineContacts.HeelPosition + baselineContacts.ToePosition) * 0.5f;
                Vector3 preEnvelopeNormal = ResolvePathClearanceNormal(
                    baselineSole,
                    currentPathPosition,
                    pathSurfaceNormal,
                    up,
                    plan.SoleSupportRadius);
                preHeelDistance = Vector3.Dot(
                    baselineContacts.HeelPosition - currentPathPosition,
                    preEnvelopeNormal);
                preToeDistance = Vector3.Dot(
                    baselineContacts.ToePosition - currentPathPosition,
                    preEnvelopeNormal);
                requiredLift = Vector3.Dot(predictiveAnklePosition - pose.AnklePosition, up);
                Vector3 activeResolvedAnklePosition = hasTransitionOrigin
                    ? runtime.IsFadingOut
                        ? Vector3.Lerp(
                            grounding.ContactState != CharacterFootContactState.Swing ||
                            grounding.AnchorBlendWeight > 0.0001f
                                ? baselineWorldPosition
                                : pose.AnklePosition,
                            outgoingAnklePosition,
                            activePlanPredictionBlend)
                        : outgoingAnklePosition
                    : Vector3.Lerp(
                        baselineWorldPosition,
                        targetData.AnklePosition,
                        activePlanPredictionBlend);
                Quaternion activeResolvedAnkleRotation = hasTransitionOrigin
                    ? runtime.IsFadingOut
                        ? Quaternion.Slerp(
                            grounding.ContactState != CharacterFootContactState.Swing ||
                            grounding.AnchorBlendWeight > 0.0001f
                                ? baselineWorldRotation
                                : pose.AnkleRotation,
                            outgoingAnkleRotation,
                            activePlanPredictionBlend).normalized
                        : outgoingAnkleRotation
                    : Quaternion.Slerp(
                        baselineWorldRotation,
                        targetData.AnkleRotation,
                        activePlanPredictionBlend).normalized;
                Vector3 resolvedAnklePosition = activeResolvedAnklePosition;
                Quaternion resolvedAnkleRotation = activeResolvedAnkleRotation;
                if (revisionTargetAvailable)
                {
                    Vector3 revisionResolvedAnklePosition = Vector3.Lerp(
                        baselineWorldPosition,
                        revisionTargetData.AnklePosition,
                        revisionPlanPredictionBlend);
                    Quaternion revisionResolvedAnkleRotation = Quaternion.Slerp(
                        baselineWorldRotation,
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
                ulong outputPlanSequence = revisionTargetAvailable && revisionTransitionBlend >= 0.5f
                    ? revisionPlan.Sequence
                    : plan.Sequence;
                runtime.ResolveOutputContinuity(
                    renderFrame,
                    presentationDeltaSeconds,
                    m_TransitionBlendSpeed,
                    baselineOwnsFoot,
                    true,
                    outputPlanSequence,
                    resolvedAnklePosition,
                    resolvedAnkleRotation,
                    out resolvedAnklePosition,
                    out resolvedAnkleRotation);
                CharacterFootPlacementSoleContactPose resolvedContacts = pose.ResolveSoleContacts(
                    resolvedAnklePosition,
                    resolvedAnkleRotation);
                Vector3 resolvedSole = (resolvedContacts.HeelPosition + resolvedContacts.ToePosition) * 0.5f;
                Vector3 envelopeNormal = ResolvePathClearanceNormal(
                    resolvedSole,
                    currentPathPosition,
                    pathSurfaceNormal,
                    up,
                    plan.SoleSupportRadius);
                postHeelDistance = Vector3.Dot(
                    resolvedContacts.HeelPosition - currentPathPosition,
                    envelopeNormal);
                postToeDistance = Vector3.Dot(
                    resolvedContacts.ToePosition - currentPathPosition,
                    envelopeNormal);
                predictiveOwnsSoleClearance = !stanceOwnsFoot &&
                                              !runtime.IsFadingOut &&
                                              authoritativePredictionBlend >= 0.999999f;
                if (!stanceOwnsFoot && !runtime.IsFadingOut)
                {
                    CharacterFootGroundingHitDiagnostics pathClearanceSupport =
                        BuildPathSupportDiagnostics(
                            currentPathSupport,
                            currentPathPosition,
                            envelopeNormal);
                    CharacterFootGroundingHitDiagnostics currentClearanceSupport =
                        grounding.SoleSupport;
                    float clearanceTranslation = Mathf.Max(
                        predictiveOwnsSoleClearance
                            ? ResolveSoleClearanceTranslation(
                                in resolvedContacts,
                                up,
                                in pathClearanceSupport)
                            : 0f,
                        ResolveSoleClearanceTranslation(
                            in resolvedContacts,
                            up,
                            in currentClearanceSupport));
                    if (clearanceTranslation > 0f)
                    {
                        resolvedAnklePosition += up * clearanceTranslation;
                        resolvedContacts = pose.ResolveSoleContacts(
                            resolvedAnklePosition,
                            resolvedAnkleRotation);
                        postHeelDistance = Vector3.Dot(
                            resolvedContacts.HeelPosition - currentPathPosition,
                            envelopeNormal);
                        postToeDistance = Vector3.Dot(
                            resolvedContacts.ToePosition - currentPathPosition,
                            envelopeNormal);
                    }
                }
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
                            envelopeNormal);
                        postToeDistance = Vector3.Dot(
                            resolvedContacts.ToePosition - currentPathPosition,
                            envelopeNormal);
                    }
                }
                predictionReachRatio = Vector3.Distance(appliedHip, resolvedAnklePosition) / legLength;
                if (IsFinite(resolvedAnklePosition) && IsFinite(resolvedAnkleRotation) &&
                    float.IsFinite(predictionReachRatio) &&
                    finalReachValid &&
                    !stanceOwnsFoot)
                {
                    result = new CharacterFullBodyIkGoal(
                        baseline.Slot,
                        component.InverseTransformPoint(resolvedAnklePosition),
                        (Quaternion.Inverse(component.rotation) * resolvedAnkleRotation).normalized,
                        baseline.PositionWeight,
                        baseline.RotationWeight,
                        CharacterFullBodyIkGoalApplication.FootPlacementEffectorTarget,
                        baseline.SourceKind | CharacterFullBodyIkGoalSourceKind.PredictiveExtension,
                        baseline.DiagnosticMetadataIndex);
                    rewritten = true;
                }
            }
            else
            {
                runtime.ResolveOutputContinuity(
                    renderFrame,
                    presentationDeltaSeconds,
                    m_TransitionBlendSpeed,
                    baselineOwnsFoot,
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
            FixedList512Bytes<CharacterPredictiveFootPathSampleDiagnostics> pathSamples =
                BuildPathDiagnostics(plan);
            FixedList128Bytes<Vector3> plannedFootRouteWorld = BuildPlannedFootRouteDiagnostics(plan);
            var currentEventDiagnostics = new CharacterPredictiveFootEventDiagnostics(
                side,
                in feature);
            AnimationPredictedFootStepSample incomingStep = feature.IncomingPredictedStep;
            var incomingEventDiagnostics = new CharacterPredictiveFootEventDiagnostics(
                side,
                feature.IsValid,
                incomingStep);
            var planLifecycleDiagnostics = new CharacterPredictiveFootPlanLifecycleDiagnostics(plan);
            var queryDiagnostics = new CharacterPredictiveFootQueryDiagnostics(plan);
            CharacterFootGroundingHitDiagnostics pathSupportDiagnostics = currentPathSupport.IsValid
                ? new CharacterFootGroundingHitDiagnostics(
                    new FootPlacementSurface(
                        currentPathSupport.Collider,
                        currentPathPosition,
                        currentPathSupport.Normal))
                : default;
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
                runtime.SmoothedRevisionBlendWeight,
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
                baselineWorldPosition,
                component.TransformPoint(result.ComponentPosition),
                baseline,
                result);
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
                revisionTargetAvailable && revisionTransitionBlend >= 0.5f
                    ? revisionPlan.Sequence
                    : targetAvailable
                        ? plan.Sequence
                        : 0,
                currentPathRoot,
                currentPathRootStart,
                currentPathHip);
            debugSnapshot = new CharacterPredictiveFootLegFrameSnapshot(
                side,
                plan.State,
                plan.ActionProgress,
                plan.GroundPathProgress,
                plan.GeometrySnapshot,
                plan.WorldProjectionMatrix,
                runtime.HasRevision
                    ? runtime.Revision.State
                    : CharacterPredictiveFootPlanState.Inactive,
                runtime.HasRevision
                    ? runtime.Revision.GeometrySnapshot
                    : null,
                runtime.HasRevision
                    ? runtime.Revision.WorldProjectionMatrix
                    : Matrix4x4.identity,
                runtime.HasRevision
                    ? runtime.SmoothedRevisionBlendWeight
                    : 0f,
                clearanceEvaluated,
                rewritten,
                requiredLift,
                appliedLift,
                currentPathPosition,
                baselineWorldPosition,
                baselineContacts.HeelPosition,
                baselineContacts.ToePosition,
                finalWorldPosition,
                finalContacts.HeelPosition,
                finalContacts.ToePosition);
            return result;
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

        static CharacterFootGroundingHitDiagnostics BuildPathSupportDiagnostics(
            FootPlacementSurface support,
            Vector3 pathPosition,
            Vector3 clearanceNormal) =>
            support.IsValid
                ? new CharacterFootGroundingHitDiagnostics(
                    new FootPlacementSurface(
                        support.Collider,
                        pathPosition,
                        clearanceNormal.normalized))
                : default;

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

        static float ResolveSoleClearanceTranslation(
            in CharacterFootPlacementSoleContactPose contacts,
            Vector3 up,
            in CharacterFootGroundingHitDiagnostics support)
        {
            if (!support.HasHit)
                return 0f;
            Vector3 normal = support.Normal.normalized;
            float upNormalDot = Vector3.Dot(up, normal);
            if (upNormalDot <= 0.0001f)
                return 0f;
            float heelDistance = Vector3.Dot(contacts.HeelPosition - support.Point, normal);
            float toeDistance = Vector3.Dot(contacts.ToePosition - support.Point, normal);
            float penetration = Mathf.Max(0f, -Mathf.Min(heelDistance, toeDistance));
            return penetration / upNormalDot;
        }

        static bool TryEvaluateFootTarget(
            CharacterPredictiveFootPlacementPlan plan,
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
            CharacterPredictiveFootPlacementPlan plan,
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
            if (!TryClampTransitionClearanceToReach(
                    pose,
                    appliedHip,
                    authoredAnklePosition,
                    up,
                    maximumReach,
                    ref anklePosition,
                    out float transitionClearanceReduction))
            {
                rejectReason = FootPredictionRejectReason.ReachExceeded;
                return false;
            }
            if (transitionClearanceReduction > 0f)
            {
                animationClearanceContinuityContribution = Mathf.Max(
                    0f,
                    animationClearanceContinuityContribution - transitionClearanceReduction);
                compositeAnimationClearance = Mathf.Max(
                    authoredAnimationClearance,
                    compositeAnimationClearance - transitionClearanceReduction);
                contacts = pose.ResolveSoleContacts(anklePosition, ankleRotation);
                heelDistance = Vector3.Dot(contacts.HeelPosition - pathPosition, envelopeNormal);
                toeDistance = Vector3.Dot(contacts.ToePosition - pathPosition, envelopeNormal);
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
                contacts = pose.ResolveSoleContacts(anklePosition, ankleRotation);
                heelDistance = Vector3.Dot(contacts.HeelPosition - pathPosition, envelopeNormal);
                toeDistance = Vector3.Dot(contacts.ToePosition - pathPosition, envelopeNormal);
            }
            if (!IsFinite(anklePosition) || !IsFinite(ankleRotation) ||
                !float.IsFinite(heelDistance) || !float.IsFinite(toeDistance))
            {
                rejectReason = FootPredictionRejectReason.NonFinite;
                return false;
            }
            target = new CharacterPredictiveFootTarget(
                pathPosition,
                pathRoot,
                pathHip,
                support,
                anklePosition,
                ankleRotation,
                contacts,
                heelDistance,
                toeDistance,
                authoredAnimationClearance,
                animationClearanceContinuityOffset,
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

        static bool TryBuildLandingHandoff(
            CharacterPredictiveFootPlacementPlan plan,
            CharacterFootPlacementAnimatedFootPose pose,
            float plantConfidence,
            Vector3 up,
            float presentationDeltaSeconds,
            out CharacterPredictiveFootStanceInput handoff)
        {
            handoff = default;
            if (plan.State != CharacterPredictiveFootPlanState.Executing ||
                !plan.HasExecutablePath ||
                !float.IsFinite(presentationDeltaSeconds) || presentationDeltaSeconds <= 0f)
            {
                return false;
            }
            float remainingSeconds = Mathf.Max(
                0f,
                (1f - plan.ActionStepPhase) * plan.ActionStepDurationSeconds);
            if (remainingSeconds > presentationDeltaSeconds + 0.00001f ||
                !TryEvaluateFootTarget(
                    plan,
                    1f,
                    pose,
                    up,
                    pose.HipPosition,
                    0f,
                    out CharacterPredictiveFootTarget target) ||
                !target.Support.IsValid)
            {
                return false;
            }
            plan.EvaluateActionState(
                1f,
                out AnimationFootConstraintMode constraintMode,
                out AnimationFootSupportPhase supportPhase,
                out _,
                out AnimationBodyRotationPivotMode bodyPivotMode);
            if (supportPhase != AnimationFootSupportPhase.ApproachingContact)
                return false;
            plan.EvaluateBodyPath(
                plan.RootTrajectory.PathStartPhase,
                out Vector3 pathRootStart,
                out _);
            handoff = new CharacterPredictiveFootStanceInput(
                true,
                false,
                false,
                plan.Sequence,
                plan.LandingEventIdentity,
                true,
                plan.Sequence,
                plan.LandingEventIdentity,
                constraintMode,
                supportPhase,
                bodyPivotMode,
                plan.RootTrajectory.EvaluateConstraintWeight(1f),
                plan.RootTrajectory.EvaluateSupportWeight(1f),
                plantConfidence,
                1f,
                0f,
                target.Support,
                target.AnklePosition,
                target.AnkleRotation,
                target.PathPosition,
                target.PathRoot,
                pathRootStart,
                target.PathHip,
                pose.HipPosition,
                target.AnklePosition,
                0f,
                0f,
                0f,
                Vector3.zero,
                Vector3.zero,
                0f);
            return true;
        }

        void PrepareFoot(
            CharacterFootSide side,
            FootPlanRuntime runtime,
            CharacterFootPlacementAnimatedFootPose pose,
            AnimationFootFeatureSample feature,
            Vector3 groundProbeStart,
            FootPlacementSurface groundProbeSupport,
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
            float presentationDeltaSeconds,
            out CharacterPredictiveFootStanceInput landingHandoff)
        {
            landingHandoff = default;
            runtime.BeginFrame();
            runtime.ClearIntentObservation();
            CharacterPredictiveFootPlacementPlan plan = runtime.Active;
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
            if ((currentPlanMatches || activeEventReplaced && step.IsPreSwing) &&
                plan.HasExecutablePath &&
                IsMotionWithinCommitTolerance(plan) &&
                !runtime.IsFadingOut)
            {
                TryBuildLandingHandoff(
                    plan,
                    pose,
                    feature.PlantConfidence,
                    up,
                    presentationDeltaSeconds,
                    out landingHandoff);
            }
            bool outgoingOutputAvailable = runtime.HasLastOutputGroundPath &&
                                           runtime.LastOutputGroundPlanSequence == plan.Sequence;
            Vector3 successorSole = outgoingOutputAvailable && runtime.HasLastOutputSole
                ? runtime.LastOutputSole
                : currentSole;
            Vector3 successorProbeStart = outgoingOutputAvailable
                ? runtime.LastOutputGroundPath
                : groundProbeStart;
            FootPlacementSurface successorProbeSupport = outgoingOutputAvailable
                ? runtime.LastOutputGroundSupport
                : groundProbeSupport;
            if (plan.HasExecutablePath && plan.FutureSupport.IsValid)
            {
                FootPlacementSurface plannedLandingSupport = ResolveSupportAtRoutePoint(
                    plan.ProjectedFutureSupport,
                    plan.ProjectedLanding,
                    up);
                if (plannedLandingSupport.IsValid)
                {
                    successorSole = plan.ProjectedLanding;
                    successorProbeSupport = plannedLandingSupport;
                    successorProbeStart = plannedLandingSupport.Point;
                }
            }
            if (outgoingOutputAvailable && runtime.HasLastOutputSole)
            {
                FootPlacementSurface projectedSupport = ResolveSupportAtRoutePoint(
                    successorProbeSupport,
                    successorSole,
                    up);
                if (projectedSupport.IsValid)
                {
                    successorProbeSupport = projectedSupport;
                    successorProbeStart = projectedSupport.Point;
                }
            }
            if (landingHandoff.HasContactTarget &&
                step.IsAuthoritative &&
                landingHandoff.ContactLandingEventIdentity == step.LandingEventIdentity)
            {
                CharacterFootPlacementSoleContactPose handoffContacts = pose.ResolveSoleContacts(
                    landingHandoff.ContactAnklePosition,
                    landingHandoff.ContactAnkleRotation);
                Vector3 handoffSole =
                    (handoffContacts.HeelPosition + handoffContacts.ToePosition) * 0.5f;
                successorProbeSupport = ResolveSupportAtRoutePoint(
                    landingHandoff.ContactSurface,
                    handoffSole,
                    up);
                successorProbeStart = successorProbeSupport.IsValid
                    ? successorProbeSupport.Point
                    : handoffSole;
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
                CharacterPredictiveFootPlacementPlan revision = runtime.Revision;
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
                        if (runtime.TryGetCommittedLanding(
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
                            FootPlacementSurface currentOriginSupport = ResolveSupportAtRoutePoint(
                                groundProbeSupport,
                                currentSole,
                                up);
                            successorOriginValid = currentOriginSupport.IsValid &&
                                                   IsEventSuccessorOriginCompatible(
                                                       revision,
                                                       currentSole,
                                                       currentOriginSupport,
                                                       up);
                        }
                        if (IsMotionWithinCommitTolerance(revision) && successorOriginValid)
                        {
                            runtime.PromoteRevision();
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
            bool hasSuccessorOrigin = successorProbeSupport.IsValid &&
                                      IsFinite(successorProbeStart) &&
                                      IsFinite(successorSole);
            bool hasCommittedLanding = runtime.TryGetCommittedLanding(
                plan.LandingEventIdentity,
                out Vector3 committedLandingSole,
                out FootPlacementSurface committedLandingSupport);
            if (hasCommittedLanding)
            {
                successorSole = committedLandingSole;
                successorProbeSupport = ResolveSupportAtRoutePoint(
                    committedLandingSupport,
                    committedLandingSole,
                    up);
                successorProbeStart = successorProbeSupport.IsValid
                    ? successorProbeSupport.Point
                    : default;
                hasCommittedLanding = successorProbeSupport.IsValid;
                hasSuccessorOrigin = hasCommittedLanding;
            }
            else if (activeEventReplaced && !hasSuccessorOrigin)
            {
                FootPlacementSurface currentOriginSupport = ResolveSupportAtRoutePoint(
                    groundProbeSupport,
                    currentSole,
                    up);
                if (currentOriginSupport.IsValid)
                {
                    successorSole = currentSole;
                    successorProbeSupport = currentOriginSupport;
                    successorProbeStart = currentOriginSupport.Point;
                    hasSuccessorOrigin = true;
                }
                else
                {
                    hasSuccessorOrigin = false;
                }
            }
            if (currentPlanMatches && plan.HasExecutablePath &&
                !intentRevisionRequested &&
                CanPrepareEventSuccessor(plan) &&
                IsMotionWithinCommitTolerance(plan) &&
                hasSuccessorOrigin &&
                incomingPlanningCandidate &&
                !plan.MatchesAuthoritativeEvent(in incomingStep) &&
                !runtime.HasRevision && !runtime.IsFadingOut &&
                m_FutureBodyTrajectorySource != null)
            {
                bool created = CreatePlan(
                    side,
                    runtime.Revision,
                    in incomingStep,
                    renderFrame,
                    successorProbeStart,
                    successorProbeSupport,
                    successorSole,
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
                if (created)
                    runtime.BeginEventSuccessor();
            }
            if (activeEventReplaced && !runtime.HasRevision)
            {
                ulong sourceSequence = step.LandingEventIdentity;
                bool canAttempt = planningCandidate &&
                                  hasSuccessorOrigin &&
                                  m_FutureBodyTrajectorySource != null &&
                                  !runtime.HasAttemptedIntentRevision(
                                      sourceSequence,
                                      motionTimeline.Generation,
                                      motionTimeline.AuthorityTick.Value);
                bool created = false;
                if (canAttempt)
                {
                    created = CreatePlan(
                        side,
                        runtime.Revision,
                        in step,
                        renderFrame,
                        successorProbeStart,
                        successorProbeSupport,
                        successorSole,
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
                    runtime.MarkIntentRevisionAttempt(sourceSequence, in motionTimeline);
                }
                if (created)
                {
                    runtime.BeginEventSuccessor();
                    runtime.Revision.UpdateWorldProjection(rootWorldPosition, rootWorldRotation);
                    if (runtime.Revision.MatchesAuthoritativeEvent(in step) &&
                        IsMotionWithinCommitTolerance(runtime.Revision))
                    {
                        runtime.PromoteRevision();
                        plan = runtime.Active;
                        currentPlanMatches = true;
                        activeEventReplaced = false;
                        replacementReason = CharacterPredictiveFootPlanEndReason.None;
                    }
                    else
                    {
                        runtime.CancelRevision(
                            CharacterPredictiveFootPlanEndReason.MotionDeviationExceeded);
                        runtime.BeginFadeOut(replacementReason, renderFrame);
                    }
                }
                else if (!runtime.IsFadingOut)
                    runtime.BeginFadeOut(replacementReason, renderFrame);
            }
            runtime.AdvanceTransition(
                renderFrame,
                presentationDeltaSeconds,
                m_TransitionBlendSpeed);
            plan = runtime.Active;
            bool needsInitialPlan = !plan.OwnsEvent ||
                                    plan.State == CharacterPredictiveFootPlanState.Rejected &&
                                    plan.MatchesAuthoritativeEvent(in step);
            ulong initialAttemptIdentity = plan.Sequence != 0
                ? plan.Sequence
                : step.LandingEventIdentity;
            if (planningCandidate && needsInitialPlan && !runtime.HasRevision &&
                !runtime.IsFadingOut && m_FutureBodyTrajectorySource != null &&
                !runtime.HasAttemptedIntentRevision(
                    initialAttemptIdentity,
                    motionTimeline.Generation,
                    motionTimeline.AuthorityTick.Value))
            {
                CreatePlan(
                    side,
                    plan,
                    in step,
                    renderFrame,
                    groundProbeStart,
                    groundProbeSupport,
                    currentSole,
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
                runtime.MarkIntentRevisionAttempt(
                    plan.Sequence != 0 ? plan.Sequence : initialAttemptIdentity,
                    in motionTimeline);
                if (plan.HasExecutablePath)
                    plan.UpdateWorldProjection(rootWorldPosition, rootWorldRotation);
            }
            if (!runtime.HasRevision && !runtime.IsFadingOut && plan.HasExecutablePath &&
                plan.MatchesAuthoritativeEvent(in step) &&
                intentRevisionRequested)
            {
                if (!TryResolveIntentRevisionOrigin(
                        runtime,
                        plan,
                        up,
                        out Vector3 revisionSole,
                        out Vector3 revisionGroundPath,
                        out FootPlacementSurface revisionGroundSupport))
                {
                    revisionSole = currentSole;
                    revisionGroundPath = groundProbeStart;
                    revisionGroundSupport = groundProbeSupport;
                }
                runtime.MarkIntentRevisionAttempt(plan.Sequence, in motionTimeline);
                bool created = CreatePlan(
                    side,
                    runtime.Revision,
                    in step,
                    renderFrame,
                    revisionGroundPath,
                    revisionGroundSupport,
                    revisionSole,
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
                if (created)
                {
                    runtime.Revision.UpdateWorldProjection(rootWorldPosition, rootWorldRotation);
                    if (runtime.HasCompleteOutputForPlan(plan.Sequence))
                        runtime.BeginIntentRevision();
                    else
                        runtime.PromoteUncommittedRevision(
                            CharacterPredictiveFootPlanEndReason.MotionDeviationExceeded);
                }
            }
        }

        static bool CanPrepareEventSuccessor(CharacterPredictiveFootPlacementPlan plan)
        {
            if (plan.State != CharacterPredictiveFootPlanState.Executing)
                return false;
            return IsApproachingContact(plan);
        }

        static bool IsApproachingContact(CharacterPredictiveFootPlacementPlan plan)
        {
            plan.EvaluateActionState(
                plan.ActionStepPhase,
                out _,
                out AnimationFootSupportPhase supportPhase,
                out _,
                out _);
            return supportPhase == AnimationFootSupportPhase.ApproachingContact;
        }

        bool IsMotionWithinCommitTolerance(CharacterPredictiveFootPlacementPlan plan)
        {
            float error = plan.MotionLandingError;
            return float.IsFinite(error) && error <= ResolveMotionDeviationThreshold(plan);
        }

        bool IsEventSuccessorOriginCompatible(
            CharacterPredictiveFootPlacementPlan successor,
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

        float ResolveMotionDeviationThreshold(CharacterPredictiveFootPlacementPlan plan) =>
            2f * Mathf.Max(
                plan.SoleSupportRadius,
                Mathf.Max(m_Settings.PathSphereRadius, m_Settings.SwingCapsuleRadius));

        static bool TryResolveIntentRevisionOrigin(
            FootPlanRuntime runtime,
            CharacterPredictiveFootPlacementPlan plan,
            Vector3 up,
            out Vector3 sole,
            out Vector3 groundPath,
            out FootPlacementSurface support)
        {
            sole = default;
            groundPath = default;
            support = default;
            if (!runtime.HasCompleteOutputForPlan(plan.Sequence))
            {
                return false;
            }
            sole = runtime.LastOutputSole;
            support = ResolveSupportAtRoutePoint(
                runtime.LastOutputGroundSupport,
                sole,
                up);
            groundPath = support.IsValid ? support.Point : default;
            return support.IsValid && IsFinite(sole) && IsFinite(groundPath);
        }

        bool ShouldRequestIntentRevision(
            FootPlanRuntime runtime,
            CharacterPredictiveFootPlacementPlan plan,
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
            if (runtime.HasAttemptedIntentRevision(
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
            CharacterFootSide side,
            CharacterPredictiveFootPlacementPlan plan,
            in AnimationPredictedFootStepSample step,
            ulong renderFrame,
            Vector3 groundProbeStart,
            FootPlacementSurface groundProbeSupport,
            Vector3 animationSoleAtGeneration,
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
            float currentSegmentRemainingSeconds = motionTimeline.CurrentSegmentDurationTicks > 0
                ? Mathf.Max(0f, (float)(motionTimeline.CurrentSegmentDurationSeconds - movementPlaybackTime))
                : float.PositiveInfinity;
            float trajectoryDurationSeconds = Mathf.Max(
                0.0001f,
                step.PredictionLeadSeconds + Mathf.Max(
                    step.ActionStepClock.TimeToLandingSeconds,
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
                return false;
            }
            var rootTrajectory = new CharacterPredictiveFootRootTrajectory(
                rootStart,
                rootStartRotation,
                presentedBodyStartPosition,
                groundProbeStart,
                animationSoleAtGeneration,
                committedBodyVelocity,
                trajectoryCurvatureDegreesPerSecond,
                trajectoryCurvatureAvailable,
                in motionTimeline,
                movementPlaybackTime,
                futureBodyTrajectory,
                up,
                in step);
            Vector3 pathStart = groundProbeStart;
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
                    groundProbeSupport,
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
            ulong sequence = AllocatePlanSequence();
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
            CharacterPredictiveFootPlacementPlan plan)
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
            CharacterPredictiveFootPlacementPlan plan)
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
            CharacterPredictiveFootPlacementPlan plan,
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
            CharacterPredictiveFootPlacementPlan plan,
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

        static bool AllowsStanceHandoff(CharacterPredictiveFootPlacementPlan plan)
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
            CharacterPredictiveFootPlacementPlan plan) =>
            plan.ActionStepPhase >= 0.9999f
                ? CharacterPredictiveFootPlanEndReason.ActionCompleted
                : CharacterPredictiveFootPlanEndReason.EventReplaced;

        void RequirePrepared(ulong renderFrame, ulong completionIdentity)
        {
            if (renderFrame == 0 || completionIdentity == 0 ||
                m_PreparedRenderFrame != renderFrame ||
                m_PreparedCompletionIdentity != completionIdentity)
            {
                throw new InvalidOperationException(
                    "Predictive Foot Modifier requires the same frame planning result from Foot Grounding.");
            }
        }

        ulong AllocatePlanSequence()
        {
            ulong value = m_NextPlanSequence++;
            if (m_NextPlanSequence == 0)
                m_NextPlanSequence = 1;
            return value;
        }

        void RequireValidInput(
            in CharacterFootPlacementPlanningFrame frame,
            in CharacterFullBodyIkGoalSetHeader ownerHeader,
            in CharacterFootGroundingPlan baseline,
            in CharacterFootGroundingDiagnostics baselineDiagnostics)
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
                 !baselineDiagnostics.IsCompleted ||
                 baselineDiagnostics.FrameSequence != frame.RenderFrame ||
                 baselineDiagnostics.CompletionIdentity != frame.CompletionIdentity ||
                 !IsBaselineGoal(baseline.Pelvis, CharacterFullBodyIkEffectorSlot.PelvisPreSolveTranslation, CharacterFullBodyIkGoalApplication.PelvisPreSolveTranslation, 0) ||
                 !IsBaselineGoal(baseline.LeftFoot, CharacterFullBodyIkEffectorSlot.LeftFoot, CharacterFullBodyIkGoalApplication.FootPlacementEffectorTarget, 1) ||
                 !IsBaselineGoal(baseline.RightFoot, CharacterFullBodyIkEffectorSlot.RightFoot, CharacterFullBodyIkGoalApplication.FootPlacementEffectorTarget, 2) ||
                 !SameGoal(baseline.LeftFoot, baselineDiagnostics.Left.Goal) ||
                 !SameGoal(baseline.RightFoot, baselineDiagnostics.Right.Goal))
            {
                throw new ArgumentException("Predictive Foot Placement input is invalid.");
            }
        }

        static bool IsBaselineGoal(
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
