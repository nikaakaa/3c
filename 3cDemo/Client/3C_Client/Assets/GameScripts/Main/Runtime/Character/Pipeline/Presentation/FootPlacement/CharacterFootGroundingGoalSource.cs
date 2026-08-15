using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal readonly struct CharacterFootGroundingPlan
    {
        internal CharacterFootGroundingPlan(
            CharacterFullBodyIkGoal pelvis,
            CharacterFullBodyIkGoal leftFoot,
            CharacterFullBodyIkGoal rightFoot)
        {
            Pelvis = pelvis;
            LeftFoot = leftFoot;
            RightFoot = rightFoot;
            if (!pelvis.IsValid || !leftFoot.IsValid || !rightFoot.IsValid)
                throw new ArgumentException("Foot Grounding plan is invalid.");
        }

        internal CharacterFullBodyIkGoal Pelvis { get; }
        internal CharacterFullBodyIkGoal LeftFoot { get; }
        internal CharacterFullBodyIkGoal RightFoot { get; }

        internal void WriteGoals(NativeSlice<CharacterFullBodyIkGoal> output)
        {
            if (output.Length != 3)
                throw new ArgumentException("Foot Grounding requires exactly three Goal slots.", nameof(output));
            output[0] = Pelvis;
            output[1] = LeftFoot;
            output[2] = RightFoot;
        }
    }

    internal sealed class CharacterFootGroundingGoalSource : IDisposable
    {
        readonly CharacterFootPlacementPoseRig m_Rig;
        readonly CharacterFootGroundingPlanner m_Planner;

        internal CharacterFootGroundingGoalSource(
            ActorId actorId,
            CharacterFootPlacementRuntimeSettings settings,
            CharacterFootPlacementPoseRig rig,
            PhysicsScene physicsScene,
            ICharacterFutureBodyTrajectorySource futureBodyTrajectorySource)
        {
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            m_Planner = new CharacterFootGroundingPlanner(
                actorId,
                settings,
                rig,
                physicsScene,
                futureBodyTrajectorySource);
        }

        internal CharacterFootGroundingDiagnostics Diagnostics => m_Planner.Diagnostics;
        internal CharacterPredictiveFootPlacementDiagnostics PredictionDiagnostics =>
            m_Planner.PredictionDiagnostics;
        internal CharacterFootPlacementRuntimeSettings Settings => m_Planner.Settings;

        internal string ApplyTuning(
            CharacterLyraCurrentGroundingSettings currentGrounding,
            CharacterStanceStabilizationSettings stanceStabilization,
            CharacterPredictiveFootPlacementRuntimeSettings predictiveExtension,
            bool resetOwnerState)
        {
            try
            {
                m_Planner.ApplyTuning(
                    currentGrounding,
                    stanceStabilization,
                    predictiveExtension,
                    resetOwnerState);
                return string.Empty;
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
        }

        internal string ApplyTuning(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock block,
            bool resetOwnerState)
        {
            try
            {
                string error = CharacterFootPlacementTuningDecoder.Apply(
                    m_Planner.Settings,
                    layout,
                    block);
                if (!string.IsNullOrEmpty(error))
                    return error;
                m_Planner.ApplyTuning(
                    m_Planner.Settings.CurrentGrounding,
                    m_Planner.Settings.StanceStabilization,
                    m_Planner.Settings.PredictiveExtension,
                    resetOwnerState);
                return string.Empty;
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
        }

        internal CharacterFullBodyIkGoalSetHeader Produce(
            in CharacterFootPlacementPlanningFrame frame,
            NativeSlice<CharacterFullBodyIkGoal> goalOutput,
            int goalWorkspaceOffset,
            int producerOperationIndex,
            int producerCallSiteIndex,
            int weightParameterIndex)
        {
            var header = new CharacterFullBodyIkGoalSetHeader(
                frame.RenderFrame,
                frame.CompletionIdentity,
                m_Rig.Rig.RigId,
                m_Rig.Rig.RigRevision,
                producerOperationIndex,
                producerCallSiteIndex,
                goalWorkspaceOffset,
                3,
                CharacterFullBodyIkGoalSetAvailability.Ready);
            m_Planner.Plan(
                in frame,
                in header,
                weightParameterIndex,
                out CharacterFootGroundingPlan plan);
            plan.WriteGoals(goalOutput);
            return header;
        }

        internal void Reset(CharacterFootPlacementReset reset) => m_Planner.Reset(reset);

        internal void RetargetBodyBranch(ulong resetSequence) =>
            m_Planner.RetargetBodyBranch(resetSequence);

        public void Dispose() => m_Planner.Dispose();
    }

    internal sealed class CharacterFootGroundingPlanner : IDisposable
    {
        enum FootOwnershipState : byte
        {
            Locked = 1,
            Releasing = 2,
            Swing = 3,
            Landing = 4
        }

        sealed class FootState
        {
            internal FootState(CharacterFootSide side) => Side = side;

            internal CharacterFootSide Side { get; }
            internal bool PlantContact;
            internal FootConstraintTransitionReason TransitionReason;
            internal FootPlacementSurface AnchorSurface;
            internal Vector3 AnchorLocalPosition;
            internal Quaternion AnchorLocalRotation = Quaternion.identity;
            internal Vector3 AnchorAnimationReferenceLocalPosition;
            internal Quaternion AnchorAnimationReferenceLocalRotation = Quaternion.identity;
            internal bool HasAnchorAnimationReference;
            internal float AnchorBlendWeight;
            internal bool HasAnchor;
            internal CharacterFootContactDecision ContactDecision;
            internal bool ContactSurfaceValid;
            internal bool ContactSurfaceDistanceAccepted;
            internal bool ContactCaptureSpeedAccepted;
            internal bool ContactRetentionSpeedAccepted;
            internal bool ContactConfidenceAccepted;
            internal float AnchorDistance = float.PositiveInfinity;
            internal bool AnchorDistanceAccepted;
            internal bool HasAnimationConstraint;
            internal ulong AnimationConstraintEventIdentity;
            internal AnimationFootConstraintMode AnimationConstraintMode = AnimationFootConstraintMode.Locked;
            internal AnimationFootSupportPhase AnimationSupportPhase = AnimationFootSupportPhase.Supporting;
            internal float AnimationConstraintWeight = 1f;
            internal float AnimationSupportWeight = 1f;
            internal float PelvisSupportWeight;
            internal bool IdleCurrentSupport;
            internal bool IdleAnchor;
            internal bool IdleAnchorCaptureArmed = true;
            internal FootOwnershipState OwnershipState = FootOwnershipState.Locked;

            internal bool AllowsAnchor =>
                OwnershipState == FootOwnershipState.Locked ||
                OwnershipState == FootOwnershipState.Landing;

            internal CharacterFootContactState ContactState =>
                PlantContact && AllowsAnchor && HasAnchor && AnchorBlendWeight >= 0.999999f
                    ? CharacterFootContactState.Anchored
                    : PlantContact || HasAnchor || AnchorBlendWeight > 0.0001f
                        ? CharacterFootContactState.Contact
                        : CharacterFootContactState.Swing;

            internal void PrepareActionEvent(AnimationPredictedFootStepSample step)
            {
                if (!step.IsAuthoritative)
                {
                    AnimationConstraintEventIdentity = 0;
                    return;
                }
                AnimationConstraintEventIdentity = step.LandingEventIdentity;
            }

            internal void UpdateContact(
                AnimationFootFeatureSample feature,
                float surfaceDistance,
                bool surfaceValid,
                CharacterPredictiveFootStanceInput predictive,
                CharacterPresentationMotionPhase motionPhase,
                CharacterStanceStabilizationSettings settings)
            {
                float speed = feature.SoleLocalVelocity.magnitude;
                bool stationaryGroundContact =
                    motionPhase == CharacterPresentationMotionPhase.GroundedStationary &&
                    !predictive.HasActionConstraint;
                bool wasIdleCurrentSupport = IdleCurrentSupport;
                IdleCurrentSupport = stationaryGroundContact;
                if (!IdleCurrentSupport)
                    IdleAnchorCaptureArmed = false;
                HasAnimationConstraint = predictive.HasActionConstraint;
                AnimationConstraintEventIdentity = predictive.HasActionConstraint
                    ? predictive.LandingEventIdentity
                    : 0;
                AnimationConstraintMode = predictive.HasActionConstraint
                    ? predictive.ConstraintMode
                    : AnimationFootConstraintMode.Locked;
                AnimationSupportPhase = predictive.HasActionConstraint
                    ? predictive.SupportPhase
                    : AnimationFootSupportPhase.Supporting;
                AnimationConstraintWeight = predictive.HasActionConstraint
                    ? predictive.ConstraintWeight
                    : 1f;
                AnimationSupportWeight = predictive.HasActionConstraint
                    ? predictive.SupportWeight
                    : 1f;
                OwnershipState = ResolveOwnershipState(
                    stationaryGroundContact,
                    HasAnimationConstraint,
                    AnimationConstraintMode,
                    AnimationSupportPhase,
                    predictive.HasContactTarget,
                    HasAnchor,
                    AnchorBlendWeight,
                    IdleAnchor);
                ContactSurfaceValid = surfaceValid;
                ContactSurfaceDistanceAccepted = surfaceValid &&
                                                   float.IsFinite(surfaceDistance) &&
                                                   surfaceDistance <= settings.MaximumContactSurfaceDistance;
                ContactCaptureSpeedAccepted = stationaryGroundContact ||
                                              speed <= settings.PlantSpeedThreshold;
                ContactRetentionSpeedAccepted = stationaryGroundContact ||
                                                speed < settings.UnalignmentSpeedThreshold;
                ContactConfidenceAccepted = HasAnimationConstraint
                    ? AnimationConstraintMode != AnimationFootConstraintMode.Unlocked ||
                      AnimationSupportPhase == AnimationFootSupportPhase.ApproachingContact
                    : feature.PlantConfidence >= settings.PlantConfidenceEnter;
                if (HasAnimationConstraint)
                {
                    if (AnimationConstraintMode == AnimationFootConstraintMode.Unlocked &&
                        AnimationSupportPhase != AnimationFootSupportPhase.ApproachingContact)
                    {
                        if (PlantContact || HasAnchor)
                        {
                            TransitionReason = FootConstraintTransitionReason.PolicyReleased;
                            ContactDecision = CharacterFootContactDecision.ContactReleasedAnimationConstraint;
                        }
                        else
                        {
                            ContactDecision = CharacterFootContactDecision.WaitingForPlantConfidence;
                        }
                        PlantContact = false;
                        return;
                    }
                    if (!surfaceValid)
                    {
                        if (PlantContact)
                        {
                            TransitionReason = FootConstraintTransitionReason.ContactReleased;
                            ContactDecision = CharacterFootContactDecision.ContactReleasedSurfaceInvalid;
                        }
                        else
                        {
                            ContactDecision = CharacterFootContactDecision.WaitingForSurface;
                        }
                        PlantContact = false;
                        return;
                    }
                    if (!ContactSurfaceDistanceAccepted)
                    {
                        if (PlantContact)
                        {
                            TransitionReason = FootConstraintTransitionReason.ContactReleased;
                            ContactDecision = CharacterFootContactDecision.ContactReleasedSurfaceDistance;
                        }
                        else
                        {
                            ContactDecision = CharacterFootContactDecision.WaitingForDistance;
                        }
                        PlantContact = false;
                        return;
                    }
                    if (!PlantContact && HasAnchor)
                    {
                        ContactDecision = CharacterFootContactDecision.AnchorFading;
                        return;
                    }
                    if (PlantContact)
                    {
                        ContactDecision = CharacterFootContactDecision.ContactRetained;
                        return;
                    }
                    PlantContact = true;
                    TransitionReason = FootConstraintTransitionReason.ContactEntered;
                    ContactDecision = CharacterFootContactDecision.ContactEntered;
                    return;
                }
                ContactConfidenceAccepted = stationaryGroundContact ||
                                            feature.PlantConfidence >= settings.PlantConfidenceEnter;
                if (!surfaceValid)
                {
                    if (PlantContact)
                    {
                        TransitionReason = FootConstraintTransitionReason.ContactReleased;
                        ContactDecision = CharacterFootContactDecision.ContactReleasedSurfaceInvalid;
                    }
                    else
                    {
                        ContactDecision = CharacterFootContactDecision.WaitingForSurface;
                    }
                    PlantContact = false;
                    return;
                }
                if (!ContactSurfaceDistanceAccepted)
                {
                    if (PlantContact)
                    {
                        TransitionReason = FootConstraintTransitionReason.ContactReleased;
                        ContactDecision = CharacterFootContactDecision.ContactReleasedSurfaceDistance;
                    }
                    else
                    {
                        ContactDecision = CharacterFootContactDecision.WaitingForDistance;
                    }
                    PlantContact = false;
                    return;
                }
                if (stationaryGroundContact)
                {
                    bool enteredContact = !PlantContact;
                    PlantContact = true;
                    if (HasAnchor && !IdleAnchor)
                    {
                        if (!wasIdleCurrentSupport)
                            TransitionReason = FootConstraintTransitionReason.IdleCurrentSupportStarted;
                        ContactDecision = CharacterFootContactDecision.AnchorFading;
                    }
                    else if (HasAnchor)
                    {
                        ContactDecision = CharacterFootContactDecision.ContactRetained;
                    }
                    else if (enteredContact)
                    {
                        TransitionReason = FootConstraintTransitionReason.ContactEntered;
                        ContactDecision = CharacterFootContactDecision.ContactEntered;
                    }
                    else
                    {
                        ContactDecision = CharacterFootContactDecision.ContactRetained;
                    }
                    return;
                }
                if (!ContactRetentionSpeedAccepted)
                {
                    if (PlantContact)
                    {
                        TransitionReason = FootConstraintTransitionReason.ContactReleased;
                        ContactDecision = CharacterFootContactDecision.ContactReleasedAnimationSpeed;
                    }
                    else
                    {
                        ContactDecision = CharacterFootContactDecision.WaitingForCaptureSpeed;
                    }
                    PlantContact = false;
                    return;
                }
                if (!PlantContact && HasAnchor)
                {
                    ContactDecision = CharacterFootContactDecision.AnchorFading;
                    return;
                }
                if (PlantContact)
                {
                    ContactDecision = CharacterFootContactDecision.ContactRetained;
                    return;
                }
                if (!ContactCaptureSpeedAccepted)
                {
                    ContactDecision = CharacterFootContactDecision.WaitingForCaptureSpeed;
                    return;
                }
                if (!ContactConfidenceAccepted)
                {
                    ContactDecision = CharacterFootContactDecision.WaitingForPlantConfidence;
                    return;
                }
                if (ContactCaptureSpeedAccepted && ContactConfidenceAccepted)
                {
                    PlantContact = true;
                    TransitionReason = FootConstraintTransitionReason.ContactEntered;
                    ContactDecision = CharacterFootContactDecision.ContactEntered;
                }
            }

            internal void Capture(
                FootPlacementSurface surface,
                Vector3 worldPosition,
                Quaternion worldRotation,
                Vector3 animatedWorldPosition,
                Quaternion animatedWorldRotation)
            {
                AnchorSurface = surface;
                AnchorLocalPosition = surface.Transform.InverseTransformPoint(worldPosition);
                AnchorLocalRotation =
                    (Quaternion.Inverse(surface.Transform.rotation) * worldRotation).normalized;
                UpdateAnimationReference(animatedWorldPosition, animatedWorldRotation, surface);
                HasAnchor = true;
                IdleAnchor = IdleCurrentSupport;
                AnchorBlendWeight = 0f;
                TransitionReason = FootConstraintTransitionReason.AnchorCaptured;
            }

            internal void UpdateAnimationReference(
                Vector3 animatedWorldPosition,
                Quaternion animatedWorldRotation,
                FootPlacementSurface surface)
            {
                AnchorAnimationReferenceLocalPosition =
                    surface.Transform.InverseTransformPoint(animatedWorldPosition);
                AnchorAnimationReferenceLocalRotation = (
                    Quaternion.Inverse(surface.Transform.rotation) * animatedWorldRotation).normalized;
                HasAnchorAnimationReference = true;
            }

            internal bool TryResolveFadeTarget(
                Vector3 animatedWorldPosition,
                Quaternion animatedWorldRotation,
                FootPlacementSurface surface,
                out Vector3 worldPosition,
                out Quaternion worldRotation)
            {
                if (!HasAnchorAnimationReference || !surface.IsValid)
                {
                    worldPosition = Vector3.zero;
                    worldRotation = Quaternion.identity;
                    return false;
                }
                Vector3 currentAnimationLocalPosition =
                    surface.Transform.InverseTransformPoint(animatedWorldPosition);
                Quaternion currentAnimationLocalRotation = (
                    Quaternion.Inverse(surface.Transform.rotation) * animatedWorldRotation).normalized;
                Vector3 fadeLocalPosition = AnchorLocalPosition +
                                            currentAnimationLocalPosition -
                                            AnchorAnimationReferenceLocalPosition;
                Quaternion animationDelta = (
                    currentAnimationLocalRotation *
                    Quaternion.Inverse(AnchorAnimationReferenceLocalRotation)).normalized;
                worldPosition = surface.Transform.TransformPoint(fadeLocalPosition);
                worldRotation = (
                    surface.Transform.rotation * animationDelta * AnchorLocalRotation).normalized;
                return IsFinite(worldPosition) && IsUnit(worldRotation);
            }

            internal bool TryResolve(
                int layerMask,
                Vector3 componentUp,
                float maximumSlopeDegrees,
                out Vector3 worldPosition,
                out Quaternion worldRotation,
                out FootPlacementSurface surface)
            {
                Vector3 up = componentUp.normalized;
                surface = HasAnchor ? AnchorSurface.Rebuild() : default;
                if (!IsFinite(up) || up.sqrMagnitude <= 0.000001f ||
                    !surface.IsValid ||
                    !surface.Collider.enabled ||
                    !surface.Transform.gameObject.activeInHierarchy ||
                    (layerMask & (1 << surface.Transform.gameObject.layer)) == 0 ||
                    Vector3.Angle(up, surface.Normal) > maximumSlopeDegrees)
                {
                    worldPosition = Vector3.zero;
                    worldRotation = Quaternion.identity;
                    return false;
                }
                worldPosition = surface.Transform.TransformPoint(AnchorLocalPosition);
                worldRotation = (surface.Transform.rotation * AnchorLocalRotation).normalized;
                return IsFinite(worldPosition) && IsUnit(worldRotation);
            }

            internal void Release(
                FootConstraintTransitionReason reason,
                CharacterFootContactDecision decision = CharacterFootContactDecision.None)
            {
                PlantContact = false;
                TransitionReason = reason;
                if (decision != CharacterFootContactDecision.None)
                    ContactDecision = decision;
            }

            internal void ClearAnchor()
            {
                AnchorSurface = default;
                AnchorLocalPosition = Vector3.zero;
                AnchorLocalRotation = Quaternion.identity;
                AnchorAnimationReferenceLocalPosition = Vector3.zero;
                AnchorAnimationReferenceLocalRotation = Quaternion.identity;
                HasAnchorAnimationReference = false;
                HasAnchor = false;
                IdleAnchor = false;
                AnchorBlendWeight = 0f;
            }

            internal void Reset(FootConstraintTransitionReason reason)
            {
                PlantContact = false;
                TransitionReason = reason;
                ContactDecision = CharacterFootContactDecision.Reset;
                ContactSurfaceValid = false;
                ContactSurfaceDistanceAccepted = false;
                ContactCaptureSpeedAccepted = false;
                ContactRetentionSpeedAccepted = false;
                ContactConfidenceAccepted = false;
                AnchorDistance = float.PositiveInfinity;
                AnchorDistanceAccepted = false;
                HasAnimationConstraint = false;
                AnimationConstraintEventIdentity = 0;
                AnimationConstraintMode = AnimationFootConstraintMode.Locked;
                AnimationSupportPhase = AnimationFootSupportPhase.Supporting;
                AnimationConstraintWeight = 1f;
                AnimationSupportWeight = 1f;
                PelvisSupportWeight = 0f;
                IdleCurrentSupport = false;
                IdleAnchorCaptureArmed = true;
                OwnershipState = FootOwnershipState.Locked;
                ClearAnchor();
            }

            static FootOwnershipState ResolveOwnershipState(
                bool stationaryGroundContact,
                bool hasAnimationConstraint,
                AnimationFootConstraintMode constraintMode,
                AnimationFootSupportPhase supportPhase,
                bool hasLandingTarget,
                bool hasAnchor,
                float anchorBlendWeight,
                bool idleAnchor)
            {
                if (stationaryGroundContact)
                    return hasAnchor && !idleAnchor
                        ? FootOwnershipState.Releasing
                        : FootOwnershipState.Locked;
                if (!hasAnimationConstraint)
                    return FootOwnershipState.Locked;
                if (hasLandingTarget)
                    return FootOwnershipState.Landing;
                if (supportPhase == AnimationFootSupportPhase.ApproachingContact)
                    return FootOwnershipState.Landing;
                if (supportPhase == AnimationFootSupportPhase.Unsupported)
                {
                    return hasAnchor || anchorBlendWeight > 0.0001f
                        ? FootOwnershipState.Releasing
                        : FootOwnershipState.Swing;
                }
                if (supportPhase == AnimationFootSupportPhase.Releasing ||
                    constraintMode == AnimationFootConstraintMode.Sliding)
                {
                    return FootOwnershipState.Releasing;
                }
                return FootOwnershipState.Locked;
            }

            static bool IsFinite(Vector3 value) =>
                float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

            static bool IsUnit(Quaternion value)
            {
                float magnitude = value.x * value.x + value.y * value.y +
                                  value.z * value.z + value.w * value.w;
                return float.IsFinite(magnitude) && Mathf.Abs(magnitude - 1f) <= 0.01f;
            }
        }

        readonly ActorId m_ActorId;
        readonly CharacterFootPlacementRuntimeSettings m_Settings;
        readonly CharacterFootPlacementPoseRig m_Rig;
        readonly CharacterFootPlacementWorldQueryBackend m_World;
        readonly CharacterLyraCurrentGroundingSolver m_CurrentGrounding;
        readonly CharacterPredictiveFootPlacementPlanner m_SwingPrediction;
        readonly CharacterFootPlacementPelvisPlanner m_Pelvis = new CharacterFootPlacementPelvisPlanner();
        readonly FootState m_Left = new FootState(CharacterFootSide.Left);
        readonly FootState m_Right = new FootState(CharacterFootSide.Right);
        ulong m_LastRenderFrame;
        ulong m_ResetSequence;
        Vector3 m_PreviousPoseRootPosition;
        bool m_HasPreviousPoseRootPosition;
        CharacterFootSide m_PelvisSupportSide;
        ulong m_PelvisSupportPlanSequence;
        bool m_HasPelvisSupportSide;
        CharacterFootGroundingDiagnostics m_Diagnostics;
        bool m_Disposed;

        internal CharacterFootGroundingPlanner(
            ActorId actorId,
            CharacterFootPlacementRuntimeSettings settings,
            CharacterFootPlacementPoseRig rig,
            PhysicsScene physicsScene,
            ICharacterFutureBodyTrajectorySource futureBodyTrajectorySource)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Foot Grounding Actor identity is invalid.", nameof(actorId));
            m_ActorId = actorId;
            m_Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            m_Rig.RequireValid();
            m_World = new CharacterFootPlacementWorldQueryBackend(
                physicsScene,
                rig,
                settings.CurrentGrounding.HitCapacity);
            m_CurrentGrounding = new CharacterLyraCurrentGroundingSolver(
                rig,
                m_World,
                settings.CurrentGrounding);
            m_SwingPrediction = new CharacterPredictiveFootPlacementPlanner(
                actorId,
                rig,
                settings,
                m_World,
                futureBodyTrajectorySource);
            ResetInternal(0, FootConstraintTransitionReason.PresentationReset);
        }

        internal CharacterFootGroundingDiagnostics Diagnostics => m_Diagnostics;
        internal CharacterPredictiveFootPlacementDiagnostics PredictionDiagnostics =>
            m_SwingPrediction.Diagnostics;
        internal CharacterFootPlacementRuntimeSettings Settings => m_Settings;

        internal void ApplyTuning(
            CharacterLyraCurrentGroundingSettings currentGrounding,
            CharacterStanceStabilizationSettings stanceStabilization,
            CharacterPredictiveFootPlacementRuntimeSettings predictiveExtension,
            bool resetOwnerState)
        {
            m_Settings.ApplyTuning(currentGrounding, stanceStabilization, predictiveExtension);
            m_CurrentGrounding.ApplyTuning(currentGrounding);
            m_SwingPrediction.ApplyTuning(
                predictiveExtension,
                stanceStabilization.AnchorBlendSpeed);
            if (resetOwnerState)
                ResetInternal(m_ResetSequence, FootConstraintTransitionReason.PresentationReset);
        }

        internal void Plan(
            in CharacterFootPlacementPlanningFrame frame,
            in CharacterFullBodyIkGoalSetHeader ownerHeader,
            int weightParameterIndex,
            out CharacterFootGroundingPlan result)
        {
            RequireAlive();
            if (frame.ActorId != m_ActorId || frame.RenderFrame == m_LastRenderFrame)
                throw new InvalidOperationException("Foot Grounding frame identity is invalid or duplicated.");
            if (!frame.Body.IsValid || frame.PresentationDeltaSeconds <= 0f)
                throw new InvalidOperationException("Foot Grounding requires valid body and delta inputs.");
            if (frame.Body.ResetSequence != m_ResetSequence)
                ResetInternal(frame.Body.ResetSequence, FootConstraintTransitionReason.BodyReset);

            CharacterFootPlacementPoseInput animationPose = frame.UpstreamPose;
            CharacterFootPlacementFeatureFrame features = ResolveFeatures(animationPose, weightParameterIndex);
            CharacterFootPlacementAnimatedPose pose = m_Rig.CaptureAnimatedPose(
                frame.RenderFrame,
                animationPose.DenseComponentPoses);
            m_Left.PrepareActionEvent(animationPose.LeftFootFeatures.PredictedStep);
            m_Right.PrepareActionEvent(animationPose.RightFootFeatures.PredictedStep);
            Vector3 poseRootUp = m_Rig.PoseRoot.up.normalized;
            float poseRootVerticalDelta = m_HasPreviousPoseRootPosition
                ? Vector3.Dot(
                    m_Rig.PoseRoot.position - m_PreviousPoseRootPosition,
                    poseRootUp)
                : 0f;
            bool bodyGrounded = ResolveBodyGrounded(frame.Body);
            float minimumGroundNormalDot = Mathf.Cos(
                m_Settings.StanceStabilization.MaximumSurfaceSlopeDegrees * Mathf.Deg2Rad);
            CharacterLyraCurrentGroundingTrace trace = m_CurrentGrounding.Trace(
                pose,
                minimumGroundNormalDot);
            CharacterPredictiveFootStanceInput leftPredictive = default;
            CharacterPredictiveFootStanceInput rightPredictive = default;
            if (m_SwingPrediction != null)
            {
                Vector3 leftGroundProbeStart = ResolvePredictiveGroundProbeStart(
                    m_Left,
                    pose.Left);
                Vector3 rightGroundProbeStart = ResolvePredictiveGroundProbeStart(
                    m_Right,
                    pose.Right);
                m_SwingPrediction.Prepare(
                    in frame,
                    in pose,
                    leftGroundProbeStart,
                    rightGroundProbeStart);
                leftPredictive = m_SwingPrediction.GetStanceInput(
                    CharacterFootSide.Left,
                    frame.RenderFrame,
                    frame.CompletionIdentity,
                    frame.UpstreamPose.LeftFootFeatures,
                    pose.Left);
                rightPredictive = m_SwingPrediction.GetStanceInput(
                    CharacterFootSide.Right,
                    frame.RenderFrame,
                    frame.CompletionIdentity,
                    frame.UpstreamPose.RightFootFeatures,
                    pose.Right);
            }
            float currentPelvisTargetOffset = ResolveCurrentPelvisTargetOffset(
                trace,
                m_Left,
                m_Right,
                leftPredictive,
                rightPredictive,
                frame.MotionPhase);
            float pelvisTargetOffset = ResolvePelvisTargetOffset(
                currentPelvisTargetOffset,
                leftPredictive,
                rightPredictive,
                m_Rig.PoseRoot.position,
                poseRootUp,
                out CharacterFootPlacementPelvisSupportDiagnostics pelvisSupportDiagnostics);
            pelvisTargetOffset = Mathf.Clamp(
                pelvisTargetOffset,
                -m_Settings.StanceStabilization.MaximumPelvisLowering,
                m_Settings.StanceStabilization.MaximumPelvisRaising);
            PreparedFoot leftPrepared = PrepareFoot(
                m_Left,
                trace.Left);
            PreparedFoot rightPrepared = PrepareFoot(
                m_Right,
                trace.Right);
            float leftSoleClearanceTarget = ResolveSoleClearanceTarget(
                pose.Left,
                trace.Left,
                leftPrepared.Surface,
                m_Rig.PoseRoot.up);
            float rightSoleClearanceTarget = ResolveSoleClearanceTarget(
                pose.Right,
                trace.Right,
                rightPrepared.Surface,
                m_Rig.PoseRoot.up);
            CharacterLyraCurrentGroundingResult lyra = m_CurrentGrounding.Resolve(
                trace,
                pose,
                pelvisTargetOffset,
                leftSoleClearanceTarget,
                rightSoleClearanceTarget,
                frame.PresentationDeltaSeconds);
            leftPrepared = UpdateStance(
                m_Left,
                pose.Left,
                trace.Left,
                lyra.Left,
                leftPrepared,
                features.Left,
                leftPredictive,
                frame.MotionPhase,
                frame.PresentationDeltaSeconds);
            rightPrepared = UpdateStance(
                m_Right,
                pose.Right,
                trace.Right,
                lyra.Right,
                rightPrepared,
                features.Right,
                rightPredictive,
                frame.MotionPhase,
                frame.PresentationDeltaSeconds);
            float leftSoleConstraintOffset = m_Left.PlantContact
                ? ResolveStanceSoleConstraintOffset(
                    leftPrepared.CurrentClearance,
                    m_Rig.PoseRoot.up)
                : 0f;
            float rightSoleConstraintOffset = m_Right.PlantContact
                ? ResolveStanceSoleConstraintOffset(
                    rightPrepared.CurrentClearance,
                    m_Rig.PoseRoot.up)
                : 0f;
            lyra = m_CurrentGrounding.ApplySoleConstraints(
                lyra,
                leftSoleConstraintOffset,
                rightSoleConstraintOffset);
            ResolvedFoot left = StabilizeFoot(
                m_Left,
                pose.Left,
                lyra.Left,
                leftPrepared,
                features.Left,
                features.Value);
            ResolvedFoot right = StabilizeFoot(
                m_Right,
                pose.Right,
                lyra.Right,
                rightPrepared,
                features.Right,
                features.Value);
            CharacterFootPlacementPelvisPlan pelvisPlan = m_Pelvis.Plan(
                pelvisTargetOffset,
                lyra.CurrentPelvisOffset,
                BuildPelvisInput(
                    m_Left,
                    CharacterFootSide.Left,
                    pose.Left,
                    left,
                    features.Value,
                    m_Rig.LeftLegLength),
                BuildPelvisInput(
                    m_Right,
                    CharacterFootSide.Right,
                    pose.Right,
                    right,
                    features.Value,
                    m_Rig.RightLegLength),
                m_Rig.PoseRoot.up,
                m_Settings.StanceStabilization);
            if (pelvisPlan.RejectLeftGoal)
            {
                m_Left.Release(
                    FootConstraintTransitionReason.PelvisRangeConflictReleased,
                    CharacterFootContactDecision.ContactReleasedPelvisConflict);
            }
            if (pelvisPlan.RejectRightGoal)
            {
                m_Right.Release(
                    FootConstraintTransitionReason.PelvisRangeConflictReleased,
                    CharacterFootContactDecision.ContactReleasedPelvisConflict);
            }
            var pelvis = new CharacterFullBodyIkGoal(
                CharacterFullBodyIkEffectorSlot.PelvisPreSolveTranslation,
                Vector3.up * pelvisPlan.ResolvedOffset,
                Quaternion.identity,
                features.Value,
                0f,
                CharacterFullBodyIkGoalApplication.PelvisPreSolveTranslation,
                CharacterFullBodyIkGoalSourceKind.FootGrounding,
                0);
            m_Diagnostics = new CharacterFootGroundingDiagnostics(
                frame.RenderFrame,
                frame.CompletionIdentity,
                frame.Body.ResetSequence,
                frame.PresentationDeltaSeconds,
                poseRootVerticalDelta,
                bodyGrounded,
                features.Value,
                pelvisPlan,
                pelvisSupportDiagnostics,
                in lyra,
                pelvis,
                m_Settings,
                m_Rig,
                m_World.PhysicsScene.GetHashCode(),
                m_Rig.SelfColliderRoot.GetInstanceID(),
                BuildDiagnostics(m_Left, pose.Left, lyra.Left, features.Left, left),
                BuildDiagnostics(m_Right, pose.Right, lyra.Right, features.Right, right));
            m_LastRenderFrame = frame.RenderFrame;
            m_ResetSequence = frame.Body.ResetSequence;
            m_PreviousPoseRootPosition = m_Rig.PoseRoot.position;
            m_HasPreviousPoseRootPosition = true;
            var baseline = new CharacterFootGroundingPlan(
                pelvis,
                left.Goal,
                right.Goal);
            m_SwingPrediction.Resolve(
                in frame,
                in ownerHeader,
                in baseline,
                in m_Diagnostics,
                out result);
        }

        Vector3 ResolvePredictiveGroundProbeStart(
            FootState state,
            CharacterFootPlacementAnimatedFootPose animated)
        {
            CharacterStanceStabilizationSettings settings = m_Settings.StanceStabilization;
            if (state.TryResolve(
                    m_Settings.CurrentGrounding.GroundLayerMask,
                    m_Rig.PoseRoot.up,
                    settings.MaximumSurfaceSlopeDegrees,
                    out Vector3 anchorPosition,
                    out Quaternion anchorRotation,
                    out _))
            {
                CharacterFootPlacementSoleContactPose anchorContacts = animated.ResolveSoleContacts(
                    anchorPosition,
                    anchorRotation);
                return (anchorContacts.HeelPosition + anchorContacts.ToePosition) * 0.5f;
            }
            CharacterFootPlacementSoleContactPose contacts = animated.ResolveSoleContacts(
                animated.AnklePosition,
                animated.AnkleRotation);
            return (contacts.HeelPosition + contacts.ToePosition) * 0.5f;
        }

        internal void Reset(CharacterFootPlacementReset reset)
        {
            if (!m_Disposed)
                ResetInternal(reset.ResetSequence, ToTransitionReason(reset.Reason));
        }

        internal void RetargetBodyBranch(ulong resetSequence)
        {
            RequireAlive();
            if (resetSequence == 0)
                throw new ArgumentOutOfRangeException(nameof(resetSequence));
            ResetInternal(resetSequence, FootConstraintTransitionReason.BodyReset);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            ResetInternal(m_ResetSequence, FootConstraintTransitionReason.PresentationReset);
            m_Disposed = true;
        }

        PreparedFoot PrepareFoot(
            FootState state,
            CharacterLyraFootTraceResult trace)
        {
            state.TransitionReason = FootConstraintTransitionReason.None;
            CharacterStanceStabilizationSettings settings = m_Settings.StanceStabilization;
            Transform root = m_Rig.PoseRoot;
            FootPlacementSurface surface = BuildSurface(trace.Hit);
            bool surfaceValid = surface.IsValid &&
                                Vector3.Angle(root.up, surface.Normal) <= settings.MaximumSurfaceSlopeDegrees;
            return new PreparedFoot(
                surface,
                surfaceValid,
                float.PositiveInfinity,
                default);
        }

        PreparedFoot UpdateStance(
            FootState state,
            CharacterFootPlacementAnimatedFootPose animated,
            CharacterLyraFootTraceResult trace,
            CharacterLyraCurrentGroundingFootResult lyra,
            PreparedFoot prepared,
            AnimationFootFeatureSample feature,
            CharacterPredictiveFootStanceInput predictive,
            CharacterPresentationMotionPhase motionPhase,
            float deltaSeconds)
        {
            CharacterStanceStabilizationSettings settings = m_Settings.StanceStabilization;
            Transform root = m_Rig.PoseRoot;
            Vector3 up = root.up.normalized;
            bool hasResolvedAnchor = state.TryResolve(
                m_Settings.CurrentGrounding.GroundLayerMask,
                root.up,
                settings.MaximumSurfaceSlopeDegrees,
                out Vector3 anchorWorldPosition,
                out Quaternion anchorWorldRotation,
                out FootPlacementSurface anchorSurface);
            if (state.HasAnchor && !hasResolvedAnchor)
            {
                state.Release(
                    FootConstraintTransitionReason.SurfaceInvalid,
                    CharacterFootContactDecision.ContactReleasedAnchorSurface);
                state.ClearAnchor();
            }
            SoleClearancePlan currentClearance = MeasureSoleClearance(
                animated,
                root.TransformPoint(lyra.ComponentPosition),
                (root.rotation * lyra.ComponentRotation).normalized,
                prepared.Surface,
                root.up);
            FootPlacementSurface contactSurface = prepared.Surface;
            bool contactSurfaceValid = prepared.SurfaceValid;
            SoleClearancePlan contactClearance = currentClearance;
            bool hasPredictiveContactTarget = predictive.HasContactTarget;
            if (hasPredictiveContactTarget)
            {
                contactSurface = predictive.ContactSurface.Rebuild();
                contactSurfaceValid = IsSurfaceUsable(
                    contactSurface,
                    root.up,
                    settings.MaximumSurfaceSlopeDegrees);
                if (contactSurfaceValid)
                {
                    contactClearance = MeasureSoleClearance(
                        animated,
                        animated.AnklePosition,
                        animated.AnkleRotation,
                        contactSurface,
                        root.up);
                }
            }
            bool usePredictiveContactTarget = hasPredictiveContactTarget && contactSurfaceValid;
            bool idleCurrentSupport = motionPhase == CharacterPresentationMotionPhase.GroundedStationary &&
                                      !predictive.HasActionConstraint;
            bool lockedAnchorOwnsContact = hasResolvedAnchor && state.PlantContact &&
                                           (!idleCurrentSupport || state.IdleAnchor) &&
                                           (!predictive.HasActionConstraint ||
                                            predictive.ConstraintMode == AnimationFootConstraintMode.Locked);
            if (!usePredictiveContactTarget && lockedAnchorOwnsContact)
            {
                contactSurface = anchorSurface;
                contactSurfaceValid = true;
                contactClearance = MeasureSoleClearance(
                    animated,
                    anchorWorldPosition,
                    anchorWorldRotation,
                    anchorSurface,
                    root.up);
            }
            bool requiresFrozenLandingTarget = predictive.HasContactTarget ||
                                               predictive.HasActionConstraint &&
                                               predictive.SupportPhase == AnimationFootSupportPhase.ApproachingContact;
            if (requiresFrozenLandingTarget && !usePredictiveContactTarget && !lockedAnchorOwnsContact)
                contactSurfaceValid = false;
            float surfaceDistance = contactSurfaceValid
                ? Mathf.Max(
                    Mathf.Abs(contactClearance.HeelPlaneDistance),
                    Mathf.Abs(contactClearance.ToePlaneDistance))
                : float.PositiveInfinity;
            state.UpdateContact(
                feature,
                surfaceDistance,
                contactSurfaceValid,
                predictive,
                motionPhase,
                settings);
            Vector3 targetWorldPosition = lockedAnchorOwnsContact
                ? anchorWorldPosition
                : usePredictiveContactTarget
                    ? predictive.ContactAnklePosition
                    : animated.AnklePosition + up * (trace.TargetOffset + lyra.SoleClearanceTarget);
            bool hadAnchor = state.HasAnchor;
            if (state.PlantContact && hasResolvedAnchor && state.AllowsAnchor)
            {
                state.UpdateAnimationReference(
                    animated.AnklePosition,
                    animated.AnkleRotation,
                    anchorSurface);
            }
            state.AnchorDistance = hasResolvedAnchor
                ? Vector3.Distance(anchorWorldPosition, targetWorldPosition)
                : float.PositiveInfinity;
            state.AnchorDistanceAccepted = !hadAnchor ||
                                           state.IdleCurrentSupport ||
                                           lockedAnchorOwnsContact ||
                                           hasResolvedAnchor &&
                                           state.AnchorDistance <= settings.MaximumAnchorDistance;
            if (state.PlantContact && hasResolvedAnchor &&
                !state.IdleCurrentSupport &&
                !lockedAnchorOwnsContact && !state.AnchorDistanceAccepted)
            {
                state.Release(
                    FootConstraintTransitionReason.AnchorDistanceExceeded,
                    CharacterFootContactDecision.ContactReleasedAnchorDistance);
            }
            float targetBlend = state.PlantContact && hasResolvedAnchor && state.AllowsAnchor
                ? state.AnimationConstraintWeight
                : 0f;
            state.AnchorBlendWeight = Mathf.MoveTowards(
                state.AnchorBlendWeight,
                targetBlend,
                settings.AnchorBlendSpeed * deltaSeconds);
            float pelvisSupportTarget = state.PlantContact && hasResolvedAnchor && state.AllowsAnchor
                ? state.AnimationSupportWeight
                : 0f;
            state.PelvisSupportWeight = Mathf.MoveTowards(
                state.PelvisSupportWeight,
                pelvisSupportTarget,
                settings.AnchorBlendSpeed * deltaSeconds);
            if (state.AnchorBlendWeight <= 0.0001f &&
                (!state.PlantContact || !state.AllowsAnchor))
            {
                state.ClearAnchor();
                hasResolvedAnchor = false;
            }
            return new PreparedFoot(
                contactSurface,
                contactSurfaceValid,
                surfaceDistance,
                usePredictiveContactTarget ? contactClearance : currentClearance,
                usePredictiveContactTarget,
                predictive.ContactAnklePosition,
                predictive.ContactAnkleRotation);
        }

        ResolvedFoot StabilizeFoot(
            FootState state,
            CharacterFootPlacementAnimatedFootPose animated,
            CharacterLyraCurrentGroundingFootResult lyra,
            PreparedFoot prepared,
            AnimationFootFeatureSample feature,
            float alpha)
        {
            CharacterStanceStabilizationSettings settings = m_Settings.StanceStabilization;
            Transform root = m_Rig.PoseRoot;
            Vector3 baselineWorldPosition = root.TransformPoint(lyra.ComponentPosition);
            Quaternion baselineWorldRotation = (root.rotation * lyra.ComponentRotation).normalized;
            bool resolvesIdleBaseline = state.IdleCurrentSupport &&
                                        !state.IdleAnchor &&
                                        !prepared.HasContactTarget &&
                                        prepared.SurfaceValid;
            if (resolvesIdleBaseline)
            {
                baselineWorldRotation = (
                    Quaternion.FromToRotation(root.up.normalized, prepared.Surface.Normal.normalized) *
                    animated.AnkleRotation).normalized;
                baselineWorldPosition = ResolveSoleContactAnklePosition(
                    animated,
                    animated.AnklePosition,
                    baselineWorldRotation,
                    prepared.Surface,
                    root.up);
            }
            float placementWeight = Mathf.Clamp01(alpha);
            bool fullPlacementWeight = placementWeight >= 0.999999f;
            if (state.IdleCurrentSupport && !fullPlacementWeight)
                state.IdleAnchorCaptureArmed = true;
            bool allowsIdleCapture = !state.IdleCurrentSupport ||
                                     state.IdleAnchorCaptureArmed && fullPlacementWeight;
            if (state.PlantContact && state.AllowsAnchor && allowsIdleCapture &&
                !state.HasAnchor && prepared.SurfaceValid)
            {
                Quaternion captureRotation = prepared.HasContactTarget
                    ? prepared.ContactAnkleRotation
                    : baselineWorldRotation;
                Vector3 capturePosition = prepared.HasContactTarget
                    ? ResolveSoleContactAnklePosition(
                        animated,
                        prepared.ContactAnklePosition,
                        captureRotation,
                        prepared.Surface,
                        root.up)
                    : MeasureSoleClearance(
                        animated,
                        baselineWorldPosition,
                        baselineWorldRotation,
                        prepared.Surface,
                        root.up).SafeAnklePosition;
                state.Capture(
                    prepared.Surface,
                    capturePosition,
                    captureRotation,
                    animated.AnklePosition,
                    animated.AnkleRotation);
                if (state.IdleAnchor)
                    state.IdleAnchorCaptureArmed = false;
            }
            bool hasResolvedAnchor = state.TryResolve(
                m_Settings.CurrentGrounding.GroundLayerMask,
                root.up,
                settings.MaximumSurfaceSlopeDegrees,
                out Vector3 anchorWorldPosition,
                out Quaternion anchorWorldRotation,
                out FootPlacementSurface anchorSurface);
            if (state.HasAnchor && !hasResolvedAnchor)
            {
                state.Release(
                    FootConstraintTransitionReason.SurfaceInvalid,
                    CharacterFootContactDecision.ContactReleasedAnchorSurface);
                state.ClearAnchor();
            }
            if (hasResolvedAnchor && !state.PlantContact &&
                !state.TryResolveFadeTarget(
                    animated.AnklePosition,
                    animated.AnkleRotation,
                    anchorSurface,
                    out anchorWorldPosition,
                    out anchorWorldRotation))
            {
                throw new InvalidOperationException("Stance Anchor fade reference is invalid.");
            }
            float anchorBlendWeight = ResolveTransitionBlend(state.AnchorBlendWeight);
            Vector3 finalWorldPosition = hasResolvedAnchor
                ? Vector3.Lerp(baselineWorldPosition, anchorWorldPosition, anchorBlendWeight)
                : baselineWorldPosition;
            Quaternion finalWorldRotation = hasResolvedAnchor
                ? Quaternion.Slerp(baselineWorldRotation, anchorWorldRotation, anchorBlendWeight).normalized
                : baselineWorldRotation;
            SoleClearancePlan soleClearance = MeasureSoleClearance(
                animated,
                finalWorldPosition,
                finalWorldRotation,
                state.PlantContact && state.AllowsAnchor && hasResolvedAnchor
                    ? anchorSurface
                    : prepared.Surface,
                root.up);
            finalWorldPosition = soleClearance.SafeAnklePosition;
            Vector3 componentPosition = Quaternion.Inverse(root.rotation) * (finalWorldPosition - root.position);
            Quaternion componentRotation = (Quaternion.Inverse(root.rotation) * finalWorldRotation).normalized;
            var goal = new CharacterFullBodyIkGoal(
                state.Side == CharacterFootSide.Left
                    ? CharacterFullBodyIkEffectorSlot.LeftFoot
                    : CharacterFullBodyIkEffectorSlot.RightFoot,
                componentPosition,
                componentRotation,
                placementWeight,
                placementWeight,
                CharacterFullBodyIkGoalApplication.FootPlacementEffectorTarget,
                CharacterFullBodyIkGoalSourceKind.FootGrounding,
                state.Side == CharacterFootSide.Left ? 1 : 2);
            return new ResolvedFoot(
                goal,
                prepared.Surface,
                state.HasAnchor ? state.AnchorLocalPosition : Vector3.zero,
                state.HasAnchor ? state.AnchorLocalRotation : Quaternion.identity,
                hasResolvedAnchor ? anchorWorldPosition : Vector3.zero,
                hasResolvedAnchor ? anchorWorldRotation : Quaternion.identity,
                componentPosition,
                componentRotation,
                placementWeight,
                feature.SoleLocalVelocity.magnitude,
                prepared.SurfaceDistance,
                soleClearance);
        }

        CharacterFootGroundingFootDiagnostics BuildDiagnostics(
            FootState state,
            CharacterFootPlacementAnimatedFootPose animated,
            CharacterLyraCurrentGroundingFootResult lyra,
            AnimationFootFeatureSample feature,
            ResolvedFoot resolved) =>
            new CharacterFootGroundingFootDiagnostics(
                state.Side,
                lyra,
                feature,
                state.ContactState,
                state.TransitionReason,
                state.ContactDecision,
                state.ContactSurfaceValid,
                state.ContactSurfaceDistanceAccepted,
                state.ContactCaptureSpeedAccepted,
                state.ContactRetentionSpeedAccepted,
                state.ContactConfidenceAccepted,
                m_Settings.StanceStabilization.MaximumContactSurfaceDistance,
                m_Settings.StanceStabilization.PlantSpeedThreshold,
                m_Settings.StanceStabilization.UnalignmentSpeedThreshold,
                m_Settings.StanceStabilization.PlantConfidenceEnter,
                m_Settings.StanceStabilization.PlantConfidenceExit,
                state.AnchorDistance,
                state.AnchorDistanceAccepted,
                m_Settings.StanceStabilization.MaximumAnchorDistance,
                m_Settings.StanceStabilization.AnchorBlendSpeed,
                resolved.Surface,
                resolved.SurfaceLocalAnchor,
                resolved.SurfaceLocalRotation,
                resolved.AnchorWorldPosition,
                resolved.AnchorWorldRotation,
                state.HasAnchor,
                ResolveTransitionBlend(state.AnchorBlendWeight),
                resolved.PlacementWeight,
                state.PlantContact,
                resolved.AnimationFootSpeed,
                resolved.SurfaceDistance,
                resolved.SoleClearance.Support,
                resolved.SoleClearance.AnklePosition,
                resolved.SoleClearance.Contacts,
                resolved.SoleClearance.HeelPlaneDistance,
                resolved.SoleClearance.ToePlaneDistance,
                resolved.SoleClearance.Penetration,
                resolved.SoleClearance.SafeAnklePosition - resolved.SoleClearance.AnklePosition,
                m_Rig.PoseRoot.InverseTransformPoint(animated.AnklePosition).y,
                resolved.BaselineComponentPosition,
                resolved.BaselineComponentRotation,
                resolved.Goal);

        static float ResolveTransitionBlend(float progress)
        {
            float value = Mathf.Clamp01(progress);
            return value * value * (3f - 2f * value);
        }

        float ResolvePelvisTargetOffset(
            float currentTarget,
            CharacterPredictiveFootStanceInput left,
            CharacterPredictiveFootStanceInput right,
            Vector3 currentRoot,
            Vector3 componentUp,
            out CharacterFootPlacementPelvisSupportDiagnostics diagnostics)
        {
            Vector3 up = componentUp.normalized;
            bool leftValid = TryResolvePredictivePelvisDisplacement(
                left,
                currentRoot,
                up,
                out float leftDisplacement);
            bool rightValid = TryResolvePredictivePelvisDisplacement(
                right,
                currentRoot,
                up,
                out float rightDisplacement);
            bool hadSupport = m_HasPelvisSupportSide;
            CharacterFootSide previousSide = m_PelvisSupportSide;
            ulong previousPlanSequence = m_PelvisSupportPlanSequence;
            m_HasPelvisSupportSide = leftValid || rightValid;
            if (leftValid && rightValid)
            {
                m_PelvisSupportSide = SelectPredictivePelvisBodyPath(
                    left,
                    right,
                    hadSupport,
                    previousSide,
                    previousPlanSequence);
                m_PelvisSupportPlanSequence = m_PelvisSupportSide == CharacterFootSide.Left
                    ? left.PlanSequence
                    : right.PlanSequence;
            }
            else if (leftValid)
            {
                m_PelvisSupportSide = CharacterFootSide.Left;
                m_PelvisSupportPlanSequence = left.PlanSequence;
            }
            else if (rightValid)
            {
                m_PelvisSupportSide = CharacterFootSide.Right;
                m_PelvisSupportPlanSequence = right.PlanSequence;
            }
            else
            {
                m_PelvisSupportPlanSequence = 0;
            }
            bool supportSwitched = hadSupport && m_HasPelvisSupportSide &&
                                   (previousSide != m_PelvisSupportSide ||
                                    previousPlanSequence != m_PelvisSupportPlanSequence);
            float resolvedTarget = m_HasPelvisSupportSide
                ? m_PelvisSupportSide == CharacterFootSide.Left
                    ? leftDisplacement
                    : rightDisplacement
                : currentTarget;
            diagnostics = new CharacterFootPlacementPelvisSupportDiagnostics(
                m_HasPelvisSupportSide,
                m_PelvisSupportSide,
                supportSwitched,
                m_PelvisSupportPlanSequence,
                currentTarget,
                resolvedTarget,
                left.HasActionConstraint,
                left.ConstraintMode,
                left.SupportPhase,
                left.BodyPivotMode,
                leftValid,
                left.PlanSequence,
                leftDisplacement,
                right.HasActionConstraint,
                right.ConstraintMode,
                right.SupportPhase,
                right.BodyPivotMode,
                rightValid,
                right.PlanSequence,
                rightDisplacement);
            return resolvedTarget;
        }

        static CharacterFootSide SelectPredictivePelvisBodyPath(
            CharacterPredictiveFootStanceInput left,
            CharacterPredictiveFootStanceInput right,
            bool hadSelection,
            CharacterFootSide previousSide,
            ulong previousPlanSequence)
        {
            const float landingTimeTolerance = 0.0001f;
            float difference = left.RemainingSeconds - right.RemainingSeconds;
            if (Mathf.Abs(difference) <= landingTimeTolerance && hadSelection)
            {
                if (previousSide == CharacterFootSide.Left && left.PlanSequence == previousPlanSequence)
                    return CharacterFootSide.Left;
                if (previousSide == CharacterFootSide.Right && right.PlanSequence == previousPlanSequence)
                    return CharacterFootSide.Right;
            }
            return difference <= 0f
                ? CharacterFootSide.Left
                : CharacterFootSide.Right;
        }

        float ResolveCurrentPelvisTargetOffset(
            in CharacterLyraCurrentGroundingTrace trace,
            FootState leftState,
            FootState rightState,
            CharacterPredictiveFootStanceInput leftPredictive,
            CharacterPredictiveFootStanceInput rightPredictive,
            CharacterPresentationMotionPhase motionPhase)
        {
            bool leftValid = TryResolveCurrentPelvisSupport(
                leftState,
                trace.Left,
                leftPredictive,
                motionPhase,
                out float leftTarget);
            bool rightValid = TryResolveCurrentPelvisSupport(
                rightState,
                trace.Right,
                rightPredictive,
                motionPhase,
                out float rightTarget);
            if (leftValid && rightValid)
                return Mathf.Min(leftTarget, rightTarget);
            if (leftValid)
                return leftTarget;
            return rightValid ? rightTarget : 0f;
        }

        bool TryResolveCurrentPelvisSupport(
            FootState state,
            CharacterLyraFootTraceResult trace,
            CharacterPredictiveFootStanceInput predictive,
            CharacterPresentationMotionPhase motionPhase,
            out float target)
        {
            target = 0f;
            bool authoritativeSupport = predictive.HasActionConstraint &&
                                        predictive.ConstraintMode != AnimationFootConstraintMode.Unlocked &&
                                        (predictive.SupportPhase == AnimationFootSupportPhase.Supporting ||
                                         predictive.SupportPhase == AnimationFootSupportPhase.Releasing);
            if (predictive.HasActionConstraint ? !authoritativeSupport : !state.PlantContact)
                return false;
            bool idleCurrentSupport = motionPhase == CharacterPresentationMotionPhase.GroundedStationary &&
                                      !predictive.HasActionConstraint;
            if (state.HasAnchor && (!idleCurrentSupport || state.IdleAnchor))
            {
                CharacterStanceStabilizationSettings settings = m_Settings.StanceStabilization;
                if (!state.TryResolve(
                        m_Settings.CurrentGrounding.GroundLayerMask,
                        m_Rig.PoseRoot.up,
                        settings.MaximumSurfaceSlopeDegrees,
                        out _,
                        out _,
                        out FootPlacementSurface anchorSurface))
                {
                    return false;
                }
                target = m_Rig.PoseRoot.InverseTransformPoint(anchorSurface.Point).y;
                return float.IsFinite(target);
            }
            if (!trace.DidTraceHit)
                return false;
            target = trace.TargetOffset;
            return float.IsFinite(target);
        }

        static bool TryResolvePredictivePelvisDisplacement(
            CharacterPredictiveFootStanceInput input,
            Vector3 currentRoot,
            Vector3 up,
            out float displacement)
        {
            displacement = 0f;
            if (!input.HasExecutablePlan || !input.IsExecuting || input.PlanSequence == 0 ||
                !float.IsFinite(input.RemainingSeconds) ||
                !IsFiniteVector(input.PathRoot) || !IsFiniteVector(currentRoot))
                return false;
            displacement = Vector3.Dot(input.PathRoot - currentRoot, up);
            return float.IsFinite(displacement);
        }

        static bool IsFiniteVector(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        bool IsSurfaceUsable(
            FootPlacementSurface surface,
            Vector3 componentUp,
            float maximumSlopeDegrees) =>
            surface.IsValid &&
            surface.Collider.enabled &&
            surface.Transform.gameObject.activeInHierarchy &&
            (m_Settings.CurrentGrounding.GroundLayerMask &
             (1 << surface.Transform.gameObject.layer)) != 0 &&
            Vector3.Angle(componentUp, surface.Normal) <= maximumSlopeDegrees;

        CharacterFootPlacementPelvisLegInput BuildPelvisInput(
            FootState state,
            CharacterFootSide side,
            CharacterFootPlacementAnimatedFootPose pose,
            ResolvedFoot resolved,
            float alpha,
            float legLength)
        {
            float goalWeight = Mathf.Clamp01(alpha);
            bool hasLockedSupport = state.PlantContact && state.AllowsAnchor && state.HasAnchor;
            float supportWeight = hasLockedSupport
                ? Mathf.Clamp01(state.PelvisSupportWeight) * goalWeight
                : 0f;
            Vector3 targetAnklePosition = hasLockedSupport
                ? resolved.AnchorWorldPosition
                : m_Rig.PoseRoot.TransformPoint(resolved.BaselineComponentPosition);
            return new CharacterFootPlacementPelvisLegInput(
                side,
                pose.HipPosition,
                targetAnklePosition,
                goalWeight,
                supportWeight,
                legLength);
        }

        CharacterFootPlacementFeatureFrame ResolveFeatures(
            CharacterFootPlacementPoseInput animationPose,
            int weightParameterIndex)
        {
            if (!string.Equals(animationPose.PosePlanHash, m_Settings.PosePlanHash, StringComparison.Ordinal))
                throw new InvalidOperationException("Foot Grounding Pose Plan identity is stale.");
            float weight = 1f;
            if (weightParameterIndex >= 0)
            {
                if ((uint)weightParameterIndex >= (uint)animationPose.PoseParameters.Length ||
                    animationPose.PoseParameterAvailability[weightParameterIndex] != 1)
                {
                    throw new InvalidOperationException("Foot Grounding Weight is unavailable.");
                }
                weight = animationPose.PoseParameters[weightParameterIndex];
            }
            if (!float.IsFinite(weight) || weight < 0f || weight > 1f)
                throw new InvalidOperationException("Foot Grounding Weight is invalid.");
            return new CharacterFootPlacementFeatureFrame(
                weight,
                animationPose.LeftFootFeatures,
                animationPose.RightFootFeatures);
        }

        void ResetInternal(ulong resetSequence, FootConstraintTransitionReason reason)
        {
            m_CurrentGrounding.Reset();
            m_Pelvis.Reset();
            m_SwingPrediction.Reset();
            m_Left.Reset(reason);
            m_Right.Reset(reason);
            m_ResetSequence = resetSequence;
            m_LastRenderFrame = 0;
            m_PreviousPoseRootPosition = Vector3.zero;
            m_HasPreviousPoseRootPosition = false;
            m_PelvisSupportSide = default;
            m_PelvisSupportPlanSequence = 0;
            m_HasPelvisSupportSide = false;
            m_Diagnostics = default;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterFootGroundingPlanner));
        }

        static bool ResolveBodyGrounded(CharacterBodyPresentationFrame body) =>
            body.TargetGrounded || body.GroundedBefore || body.GroundedAfter;

        static FootPlacementSurface BuildSurface(CharacterFootPlacementQueryHit hit) =>
            hit.HasHit && hit.PhysicsHit.collider
                ? new FootPlacementSurface(hit.PhysicsHit.collider, hit.Point, hit.Normal.normalized)
                : default;

        static SoleClearancePlan MeasureSoleClearance(
            CharacterFootPlacementAnimatedFootPose animated,
            Vector3 anklePosition,
            Quaternion ankleRotation,
            FootPlacementSurface support,
            Vector3 componentUp)
        {
            CharacterFootPlacementSoleContactPose contacts = animated.ResolveSoleContacts(
                anklePosition,
                ankleRotation);
            if (!support.IsValid)
            {
                return new SoleClearancePlan(
                    support,
                    anklePosition,
                    anklePosition,
                    contacts,
                    0f,
                    0f,
                    0f);
            }
            Vector3 up = componentUp.normalized;
            Vector3 normal = support.Normal.normalized;
            float upNormalDot = Vector3.Dot(up, normal);
            if (!float.IsFinite(upNormalDot) || upNormalDot <= 0.0001f)
                throw new InvalidOperationException("Foot Grounding sole support is not reachable along Component Up.");
            float beforeHeelDistance = Vector3.Dot(
                contacts.HeelPosition - support.Point,
                normal);
            float beforeToeDistance = Vector3.Dot(
                contacts.ToePosition - support.Point,
                normal);
            float penetration = Mathf.Max(
                0f,
                -Mathf.Min(beforeHeelDistance, beforeToeDistance));
            Vector3 translation = up * (penetration / upNormalDot);
            return new SoleClearancePlan(
                support,
                anklePosition,
                anklePosition + translation,
                contacts,
                beforeHeelDistance,
                beforeToeDistance,
                penetration);
        }

        static Vector3 ResolveSoleContactAnklePosition(
            CharacterFootPlacementAnimatedFootPose animated,
            Vector3 anklePosition,
            Quaternion ankleRotation,
            FootPlacementSurface support,
            Vector3 componentUp)
        {
            CharacterFootPlacementSoleContactPose contacts = animated.ResolveSoleContacts(
                anklePosition,
                ankleRotation);
            Vector3 up = componentUp.normalized;
            Vector3 normal = support.Normal.normalized;
            float upNormalDot = Vector3.Dot(up, normal);
            if (!support.IsValid || !float.IsFinite(upNormalDot) || upNormalDot <= 0.0001f)
                throw new InvalidOperationException("Foot Grounding contact support is invalid.");
            float heelDistance = Vector3.Dot(contacts.HeelPosition - support.Point, normal);
            float toeDistance = Vector3.Dot(contacts.ToePosition - support.Point, normal);
            float translation = -Mathf.Min(heelDistance, toeDistance) / upNormalDot;
            Vector3 result = anklePosition + up * translation;
            if (!float.IsFinite(result.x) || !float.IsFinite(result.y) || !float.IsFinite(result.z))
                throw new InvalidOperationException("Foot Grounding contact position is not finite.");
            return result;
        }

        static float ResolveSoleClearanceTarget(
            CharacterFootPlacementAnimatedFootPose animated,
            CharacterLyraFootTraceResult trace,
            FootPlacementSurface support,
            Vector3 componentUp)
        {
            if (!trace.DidTraceHit || !support.IsValid)
                return 0f;
            Vector3 up = componentUp.normalized;
            Vector3 normal = support.Normal.normalized;
            float upNormalDot = Vector3.Dot(up, normal);
            if (!float.IsFinite(upNormalDot) || upNormalDot <= 0.0001f)
                throw new InvalidOperationException("Foot Grounding sole target is not reachable along Component Up.");
            Vector3 targetAnklePosition = animated.AnklePosition + up * trace.TargetOffset;
            Quaternion targetAnkleRotation = (
                Quaternion.FromToRotation(up, normal) * animated.AnkleRotation).normalized;
            CharacterFootPlacementSoleContactPose contacts = animated.ResolveSoleContacts(
                targetAnklePosition,
                targetAnkleRotation);
            float heelDistance = Vector3.Dot(contacts.HeelPosition - support.Point, normal);
            float toeDistance = Vector3.Dot(contacts.ToePosition - support.Point, normal);
            float penetration = Mathf.Max(0f, -Mathf.Min(heelDistance, toeDistance));
            return penetration / upNormalDot;
        }

        static float ResolveStanceSoleConstraintOffset(
            in SoleClearancePlan clearance,
            Vector3 componentUp)
        {
            Vector3 up = componentUp.normalized;
            Vector3 normal = clearance.Support.Normal.normalized;
            float upNormalDot = Vector3.Dot(up, normal);
            if (!clearance.Support.IsValid || !float.IsFinite(upNormalDot) || upNormalDot <= 0.0001f)
                throw new InvalidOperationException("Stance sole support is invalid.");
            return -Mathf.Min(
                clearance.HeelPlaneDistance,
                clearance.ToePlaneDistance) / upNormalDot;
        }

        static FootConstraintTransitionReason ToTransitionReason(CharacterFootPlacementResetReason reason) => reason switch
        {
            CharacterFootPlacementResetReason.BodyStreamReset => FootConstraintTransitionReason.BodyReset,
            CharacterFootPlacementResetReason.MissingAnimationOutput => FootConstraintTransitionReason.MissingAnimationOutput,
            CharacterFootPlacementResetReason.InvalidPose => FootConstraintTransitionReason.InvalidPose,
            _ => FootConstraintTransitionReason.PresentationReset
        };

        readonly struct PreparedFoot
        {
            internal PreparedFoot(
                FootPlacementSurface surface,
                bool surfaceValid,
                float surfaceDistance,
                SoleClearancePlan currentClearance,
                bool hasContactTarget = false,
                Vector3 contactAnklePosition = default,
                Quaternion contactAnkleRotation = default)
            {
                Surface = surface;
                SurfaceValid = surfaceValid;
                SurfaceDistance = surfaceDistance;
                CurrentClearance = currentClearance;
                HasContactTarget = hasContactTarget;
                ContactAnklePosition = contactAnklePosition;
                ContactAnkleRotation = contactAnkleRotation;
            }

            internal FootPlacementSurface Surface { get; }
            internal bool SurfaceValid { get; }
            internal float SurfaceDistance { get; }
            internal SoleClearancePlan CurrentClearance { get; }
            internal bool HasContactTarget { get; }
            internal Vector3 ContactAnklePosition { get; }
            internal Quaternion ContactAnkleRotation { get; }
        }

        readonly struct ResolvedFoot
        {
            internal ResolvedFoot(
                CharacterFullBodyIkGoal goal,
                FootPlacementSurface surface,
                Vector3 surfaceLocalAnchor,
                Quaternion surfaceLocalRotation,
                Vector3 anchorWorldPosition,
                Quaternion anchorWorldRotation,
                Vector3 baselineComponentPosition,
                Quaternion baselineComponentRotation,
                float placementWeight,
                float animationFootSpeed,
                float surfaceDistance,
                SoleClearancePlan soleClearance)
            {
                Goal = goal;
                Surface = surface;
                SurfaceLocalAnchor = surfaceLocalAnchor;
                SurfaceLocalRotation = surfaceLocalRotation;
                AnchorWorldPosition = anchorWorldPosition;
                AnchorWorldRotation = anchorWorldRotation;
                BaselineComponentPosition = baselineComponentPosition;
                BaselineComponentRotation = baselineComponentRotation;
                PlacementWeight = placementWeight;
                AnimationFootSpeed = animationFootSpeed;
                SurfaceDistance = surfaceDistance;
                SoleClearance = soleClearance;
            }

            internal CharacterFullBodyIkGoal Goal { get; }
            internal FootPlacementSurface Surface { get; }
            internal Vector3 SurfaceLocalAnchor { get; }
            internal Quaternion SurfaceLocalRotation { get; }
            internal Vector3 AnchorWorldPosition { get; }
            internal Quaternion AnchorWorldRotation { get; }
            internal Vector3 BaselineComponentPosition { get; }
            internal Quaternion BaselineComponentRotation { get; }
            internal float PlacementWeight { get; }
            internal float AnimationFootSpeed { get; }
            internal float SurfaceDistance { get; }
            internal SoleClearancePlan SoleClearance { get; }
        }

        readonly struct SoleClearancePlan
        {
            internal SoleClearancePlan(
                FootPlacementSurface support,
                Vector3 anklePosition,
                Vector3 safeAnklePosition,
                CharacterFootPlacementSoleContactPose contacts,
                float heelPlaneDistance,
                float toePlaneDistance,
                float penetration)
            {
                Support = support;
                AnklePosition = anklePosition;
                SafeAnklePosition = safeAnklePosition;
                Contacts = contacts;
                HeelPlaneDistance = heelPlaneDistance;
                ToePlaneDistance = toePlaneDistance;
                Penetration = penetration;
            }

            internal FootPlacementSurface Support { get; }
            internal Vector3 AnklePosition { get; }
            internal Vector3 SafeAnklePosition { get; }
            internal CharacterFootPlacementSoleContactPose Contacts { get; }
            internal float HeelPlaneDistance { get; }
            internal float ToePlaneDistance { get; }
            internal float Penetration { get; }
        }
    }
}
