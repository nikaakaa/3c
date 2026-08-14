using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal sealed class CharacterPredictiveFootPlacementPlanner
    {
        readonly ActorId m_ActorId;
        readonly CharacterFootPlacementPoseRig m_Rig;
        readonly CharacterFootPlacementWorldQueryBackend m_World;
        readonly ICharacterFutureBodyTrajectorySource m_FutureBodyTrajectorySource;
        readonly int m_GroundLayerMask;
        readonly FixedString64Bytes m_RigId;
        readonly FixedString64Bytes m_RigRevision;
        CharacterPredictiveFootPlacementRuntimeSettings m_Settings;
        CharacterPredictiveFootPlacementQuery m_Query;
        CharacterPredictiveFootPlacementDiagnostics m_Diagnostics;
        CharacterPredictiveFootPlacementPlan m_LeftPlan;
        CharacterPredictiveFootPlacementPlan m_RightPlan;
        ulong m_NextPlanSequence = 1;
        ulong m_PreparedRenderFrame;
        ulong m_PreparedCompletionIdentity;

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
            m_GroundLayerMask = settings.CurrentGrounding.GroundLayerMask;
            m_RigId = new FixedString64Bytes(rig.Rig.RigId);
            m_RigRevision = new FixedString64Bytes(rig.Rig.RigRevision);
            m_World = world ?? throw new ArgumentNullException(nameof(world));
            m_FutureBodyTrajectorySource = futureBodyTrajectorySource;
            m_Query = new CharacterPredictiveFootPlacementQuery(m_World, m_Settings);
            m_LeftPlan = new CharacterPredictiveFootPlacementPlan(
                CharacterFootSide.Left,
                CharacterPredictiveFootPlacementQuery.MaximumPathPointCapacity);
            m_RightPlan = new CharacterPredictiveFootPlacementPlan(
                CharacterFootSide.Right,
                CharacterPredictiveFootPlacementQuery.MaximumPathPointCapacity);
        }

        internal CharacterPredictiveFootPlacementDiagnostics Diagnostics => m_Diagnostics;

        internal void Prepare(
            in CharacterFootPlacementPlanningFrame frame,
            in CharacterFootPlacementAnimatedPose pose)
        {
            if (frame.ActorId != m_ActorId || !frame.Body.IsValid ||
                frame.RenderFrame == 0 || frame.CompletionIdentity == 0 ||
                frame.RenderFrame == m_PreparedRenderFrame)
            {
                throw new InvalidOperationException("Predictive Foot planning frame is invalid or duplicated.");
            }
            Vector3 rootWorldPosition = m_Rig.PoseRoot.position;
            Quaternion rootWorldRotation = m_Rig.PoseRoot.rotation;
            AnimationFootFeatureSample leftFeature = frame.UpstreamPose.LeftFootFeatures;
            AnimationFootFeatureSample rightFeature = frame.UpstreamPose.RightFootFeatures;
            Vector3 committedBodyVelocity = frame.Body.TargetVelocity;
            CommittedLocomotionPlanarMotionTimeline motionTimeline = frame.LocomotionMotionTimeline;
            float trajectoryCurvatureDegreesPerSecond =
                frame.TrajectoryCurvatureDegreesPerSecond;
            bool trajectoryCurvatureAvailable = frame.TrajectoryCurvatureAvailable;
            PrepareFoot(
                CharacterFootSide.Left,
                m_LeftPlan,
                pose.Left,
                leftFeature,
                frame.RenderFrame,
                rootWorldPosition,
                rootWorldRotation,
                committedBodyVelocity,
                trajectoryCurvatureDegreesPerSecond,
                trajectoryCurvatureAvailable,
                in motionTimeline,
                frame.MovementPlaybackTime,
                m_Rig.LeftLegLength);
            PrepareFoot(
                CharacterFootSide.Right,
                m_RightPlan,
                pose.Right,
                rightFeature,
                frame.RenderFrame,
                rootWorldPosition,
                rootWorldRotation,
                committedBodyVelocity,
                trajectoryCurvatureDegreesPerSecond,
                trajectoryCurvatureAvailable,
                in motionTimeline,
                frame.MovementPlaybackTime,
                m_Rig.RightLegLength);
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
            CharacterPredictiveFootPlacementPlan plan = side == CharacterFootSide.Left
                ? m_LeftPlan
                : side == CharacterFootSide.Right
                    ? m_RightPlan
                    : throw new ArgumentOutOfRangeException(nameof(side));
            AnimationPredictedFootStepSample step = feature.PredictedStep;
            if (!step.IsAuthoritative && !plan.HasExecutablePath)
                return default;
            AnimationFootConstraintMode constraintMode;
            AnimationFootSupportPhase supportPhase;
            AnimationBodyRotationPivotMode bodyPivotMode;
            if (plan.HasExecutablePath)
            {
                ResolveCurrentActionState(
                    plan,
                    out constraintMode,
                    out supportPhase,
                    out _,
                    out bodyPivotMode);
            }
            else
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
            }
            Vector3 pathPosition = default;
            Vector3 pathRoot = default;
            Vector3 pathRootStart = default;
            Vector3 pathHip = default;
            FootPlacementSurface contactSurface = default;
            Vector3 contactAnklePosition = default;
            Quaternion contactAnkleRotation = default;
            bool hasContactTarget = false;
            if (plan.HasExecutablePath)
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
            if (plan.State == CharacterPredictiveFootPlanState.Executing &&
                supportPhase == AnimationFootSupportPhase.ApproachingContact &&
                TryEvaluateFootTarget(
                    plan,
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
                pathPosition = target.PathPosition;
                pathRoot = target.PathRoot;
                pathHip = target.PathHip;
            }
            float remainingSeconds = Mathf.Max(
                0f,
                (1f - plan.ActionStepPhase) * plan.ActionStepDurationSeconds);
            return new CharacterPredictiveFootStanceInput(
                true,
                plan.HasExecutablePath,
                plan.State == CharacterPredictiveFootPlanState.Executing,
                plan.Sequence,
                hasContactTarget,
                constraintMode,
                supportPhase,
                bodyPivotMode,
                feature.PlantConfidence,
                plan.ActionProgress,
                remainingSeconds,
                contactSurface,
                contactAnklePosition,
                contactAnkleRotation,
                pathPosition,
                pathRoot,
                pathRootStart,
                pathHip);
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

        internal void ApplyTuning(CharacterPredictiveFootPlacementRuntimeSettings settings)
        {
            settings.RequireValid();
            m_Settings = settings;
            m_Query = new CharacterPredictiveFootPlacementQuery(m_World, settings);
        }

        internal void Reset()
        {
            m_LeftPlan.Reset(CharacterPredictiveFootPlanEndReason.PresentationReset);
            m_RightPlan.Reset(CharacterPredictiveFootPlanEndReason.PresentationReset);
            m_NextPlanSequence = 1;
            m_PreparedRenderFrame = 0;
            m_PreparedCompletionIdentity = 0;
            m_Diagnostics = default;
            CharacterPredictiveFootPlacementDebugSnapshotRegistry.Remove(m_ActorId);
        }

        CharacterFullBodyIkGoal ModifyFoot(
            CharacterFootSide side,
            CharacterPredictiveFootPlacementPlan plan,
            CharacterFootPlacementAnimatedFootPose pose,
            AnimationFootFeatureSample feature,
            float currentEventFootPoseWeight,
            CharacterFullBodyIkGoal baseline,
            CharacterFootGroundingFootDiagnostics grounding,
            ulong renderFrame,
            float legLength,
            Vector3 appliedHip,
            out CharacterPredictiveFootPlacementFootDiagnostics diagnostics,
            out CharacterPredictiveFootLegFrameSnapshot debugSnapshot)
        {
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
            float predictiveOutputWeight = plan.State == CharacterPredictiveFootPlanState.Executing &&
                                           !step.IsPreSwing
                ? plan.EvaluatePredictiveOutputWeight()
                : 0f;
            bool actionConstraintOwnsFoot = step.IsAuthoritative && predictiveOutputWeight <= 0.000001f;
            bool stanceOwnsFoot = actionConstraintOwnsFoot ||
                                  (grounding.ContactState != CharacterFootContactState.Swing &&
                                   allowsStanceHandoff &&
                                   grounding.AnchorBlendWeight >= 0.999999f);
            bool currentSupportOwnsIdle = !step.IsAuthoritative &&
                                          plan.State == CharacterPredictiveFootPlanState.Inactive;
            float stanceTransitionBlend = plan.State == CharacterPredictiveFootPlanState.Executing
                ? Mathf.Clamp01(grounding.AnchorBlendWeight)
                : 0f;
            float planPredictionBlend = predictiveOutputWeight * (1f - stanceTransitionBlend);
            float poseSynchronizedPredictionBlend = planPredictionBlend;
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
            bool targetAvailable = TryEvaluateFootTarget(
                plan,
                pose,
                up,
                appliedHip,
                legLength * m_Settings.MaximumPredictionReachRatio,
                out CharacterPredictiveFootTarget targetData);
            if (targetAvailable)
            {
                clearanceEvaluated = true;
                currentPathPosition = targetData.PathPosition;
                currentPathRoot = targetData.PathRoot;
                currentPathHip = targetData.PathHip;
                currentPathSupport = targetData.Support;
                authoredAnimationClearance = targetData.AuthoredAnimationClearance;
                animationClearanceContinuityOffset = targetData.AnimationClearanceContinuityOffset;
                animationClearanceContinuityContribution = targetData.AnimationClearanceContinuityContribution;
                reachClearance = targetData.ReachClearance;
                compositeAnimationClearance = targetData.CompositeAnimationClearance;
                Vector3 supportNormal = currentPathSupport.IsValid
                    ? currentPathSupport.Normal.normalized
                    : up;
                preHeelDistance = Vector3.Dot(baselineContacts.HeelPosition - currentPathPosition, supportNormal);
                preToeDistance = Vector3.Dot(baselineContacts.ToePosition - currentPathPosition, supportNormal);
                requiredLift = Vector3.Dot(targetData.AnklePosition - pose.AnklePosition, up);
                float predictionBlend = poseSynchronizedPredictionBlend;
                Vector3 resolvedAnklePosition = Vector3.Lerp(
                    baselineWorldPosition,
                    targetData.AnklePosition,
                    predictionBlend);
                Quaternion resolvedAnkleRotation = Quaternion.Slerp(
                    baselineWorldRotation,
                    targetData.AnkleRotation,
                    predictionBlend).normalized;
                CharacterFootPlacementSoleContactPose resolvedContacts = pose.ResolveSoleContacts(
                    resolvedAnklePosition,
                    resolvedAnkleRotation);
                postHeelDistance = Vector3.Dot(
                    resolvedContacts.HeelPosition - currentPathPosition,
                    supportNormal);
                postToeDistance = Vector3.Dot(
                    resolvedContacts.ToePosition - currentPathPosition,
                    supportNormal);
                predictiveOwnsSoleClearance = !stanceOwnsFoot && predictionBlend >= 0.999999f;
                if (!stanceOwnsFoot)
                {
                    CharacterFootGroundingHitDiagnostics pathClearanceSupport =
                        BuildPathSupportDiagnostics(currentPathSupport);
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
                            supportNormal);
                        postToeDistance = Vector3.Dot(
                            resolvedContacts.ToePosition - currentPathPosition,
                            supportNormal);
                    }
                }
                if (!stanceOwnsFoot)
                    appliedLift = Vector3.Dot(resolvedAnklePosition - pose.AnklePosition, up);
                predictionReachRatio = Vector3.Distance(appliedHip, resolvedAnklePosition) / legLength;
                if (IsFinite(resolvedAnklePosition) && IsFinite(resolvedAnkleRotation) &&
                    float.IsFinite(predictionReachRatio) &&
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
            FootPredictionRejectReason rejectReason = ResolveRejectReason(
                plan,
                in step,
                landingEventIdentityValid,
                rewritten,
                predictionReachRatio,
                plan.OwnsEvent && plan.State == CharacterPredictiveFootPlanState.Executing
                    ? stanceOwnsFoot
                    : false);
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
                new CharacterFootGroundingHitDiagnostics(plan.FutureSupport),
                in queryDiagnostics,
                in currentEventDiagnostics,
                in incomingEventDiagnostics,
                currentEventFootPoseWeight,
                planPredictionBlend,
                poseSynchronizedPredictionBlend,
                plan.LandingDelayAtGeneration,
                plan.OwnsEvent ? Vector3.Distance(plan.Start, plan.Landing) : 0f,
                in planLifecycleDiagnostics,
                currentSole,
                plan.Start,
                plan.Landing,
                currentPathPosition,
                currentPathRoot,
                currentPathHip,
                plan.PredictedHip,
                plan.RootStart,
                plan.RootStartRotation,
                plan.RootLanding,
                plan.RootLandingRotation,
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
            debugSnapshot = new CharacterPredictiveFootLegFrameSnapshot(
                side,
                plan.State,
                plan.ActionProgress,
                plan.GroundPathProgress,
                plan.GeometrySnapshot,
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
            if (!step.IsAuthoritative)
                return 0f;
            float weight = 0f;
            for (int i = 0; i < upstreamPose.ContributionCount; i++)
            {
                AnimationPoseSourceContribution contribution = upstreamPose.Contributions[i];
                if (contribution.ContributionContinuityIdentity !=
                    step.ContributionContinuityIdentity)
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
            FootPlacementSurface support) =>
            support.IsValid
                ? new CharacterFootGroundingHitDiagnostics(support)
                : default;

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
            CharacterFootPlacementAnimatedFootPose pose,
            Vector3 componentUp,
            Vector3 appliedHip,
            float maximumReach,
            out CharacterPredictiveFootTarget target)
        {
            target = default;
            if (plan.State != CharacterPredictiveFootPlanState.Executing)
                return false;
            Vector3 up = componentUp.normalized;
            plan.EvaluateClearancePath(
                plan.ActionStepPhase,
                out Vector3 pathPosition,
                out Vector3 pathRoot,
                out Vector3 pathHip,
                out FootPlacementSurface pathSupport,
                out Vector3 predictedSole);
            ResolveCurrentActionState(
                plan,
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
            plan.EvaluateCurrentAnimationClearance(
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
            CharacterFootPlacementSoleContactPose contacts = pose.ResolveSoleContacts(
                anklePosition,
                ankleRotation);
            float heelDistance = Vector3.Dot(contacts.HeelPosition - pathPosition, supportNormal);
            float toeDistance = Vector3.Dot(contacts.ToePosition - pathPosition, supportNormal);
            float penetration = Mathf.Max(0f, -Mathf.Min(heelDistance, toeDistance));
            float upNormalDot = Vector3.Dot(up, supportNormal);
            if (penetration > 0f && upNormalDot > 0.0001f)
            {
                anklePosition += up * (penetration / upNormalDot);
                contacts = pose.ResolveSoleContacts(anklePosition, ankleRotation);
                heelDistance = Vector3.Dot(contacts.HeelPosition - pathPosition, supportNormal);
                toeDistance = Vector3.Dot(contacts.ToePosition - pathPosition, supportNormal);
            }
            if (!TryResolveAppliedReachClearance(
                    pose,
                    appliedHip,
                    anklePosition,
                    up,
                    maximumReach,
                    out float reachClearance))
            {
                return false;
            }
            if (reachClearance > 0f)
            {
                anklePosition += up * reachClearance;
                contacts = pose.ResolveSoleContacts(anklePosition, ankleRotation);
                heelDistance = Vector3.Dot(contacts.HeelPosition - pathPosition, supportNormal);
                toeDistance = Vector3.Dot(contacts.ToePosition - pathPosition, supportNormal);
            }
            if (!IsFinite(anklePosition) || !IsFinite(ankleRotation) ||
                !float.IsFinite(heelDistance) || !float.IsFinite(toeDistance))
                return false;
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
            CharacterPredictiveFootPlacementPlan plan,
            CharacterFootPlacementAnimatedFootPose pose,
            AnimationFootFeatureSample feature,
            ulong renderFrame,
            Vector3 rootWorldPosition,
            Quaternion rootWorldRotation,
            Vector3 committedBodyVelocity,
            float trajectoryCurvatureDegreesPerSecond,
            bool trajectoryCurvatureAvailable,
            in CommittedLocomotionPlanarMotionTimeline motionTimeline,
            double movementPlaybackTime,
            float legLength)
        {
            plan.BeginFrame();
            AnimationPredictedFootStepSample step = feature.PredictedStep;
            bool landingEventIdentityValid = step.HasConsistentLandingEventIdentity(side);
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
            if (plan.OwnsEvent && !currentPlanMatches)
            {
                plan.Reset(ResolveReplacementEndReason(plan));
            }
            if (plan.HasExecutablePath)
            {
                if (!step.IsAuthoritative ||
                    !plan.MatchesAuthoritativeEvent(in step))
                {
                    plan.Reset(ResolveReplacementEndReason(plan));
                }
                else
                {
                    plan.SynchronizeActionClock(renderFrame, in step);
                    if (plan.HasExecutablePath &&
                        plan.ShouldInterruptWorldMotion(
                            trajectoryCurvatureDegreesPerSecond,
                            trajectoryCurvatureAvailable,
                            in motionTimeline,
                            Mathf.Max(m_Settings.PathSphereRadius, m_Settings.SwingCapsuleRadius)))
                    {
                        plan.InterruptWorldMotion();
                    }
                }
            }
            bool planningCandidate = landingEventIdentityValid &&
                                     step.IsPreSwing &&
                                     trajectoryCurvatureAvailable &&
                                     step.Confidence >= m_Settings.MinimumLandingConfidence &&
                                     step.ActionStepClock.Phase < 0.9999f;
            if (planningCandidate && !plan.OwnsEvent && m_FutureBodyTrajectorySource != null)
            {
                CreatePlan(
                    side,
                    plan,
                    in step,
                    renderFrame,
                    currentSole,
                    soleSupportRadius,
                    pose.HipPosition,
                    pose.AnklePosition,
                    rootWorldPosition,
                    rootWorldRotation,
                    committedBodyVelocity,
                    trajectoryCurvatureDegreesPerSecond,
                    in motionTimeline,
                    movementPlaybackTime,
                    up,
                    legLength);
            }
        }

        void CreatePlan(
            CharacterFootSide side,
            CharacterPredictiveFootPlacementPlan plan,
            in AnimationPredictedFootStepSample step,
            ulong renderFrame,
            Vector3 currentSole,
            float soleSupportRadius,
            Vector3 currentHip,
            Vector3 currentAnkle,
            Vector3 rootStart,
            Quaternion rootStartRotation,
            Vector3 committedBodyVelocity,
            float trajectoryCurvatureDegreesPerSecond,
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
                trajectoryCurvatureDegreesPerSecond);
            if (!m_FutureBodyTrajectorySource.TryPredict(
                    in trajectoryRequest,
                    out CharacterFutureBodyTrajectory futureBodyTrajectory))
            {
                return;
            }
            var rootTrajectory = new CharacterPredictiveFootRootTrajectory(
                rootStart,
                rootStartRotation,
                committedBodyVelocity,
                trajectoryCurvatureDegreesPerSecond,
                in motionTimeline,
                movementPlaybackTime,
                futureBodyTrajectory,
                currentSole,
                currentHip,
                currentAnkle,
                up,
                in step);
            Vector3 pathStart = rootTrajectory.EvaluateFootRoute(rootTrajectory.PathStartPhase);
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
                first.Surface.IsValid ? first.Surface.Normal : Vector3.up,
                first.Surface.Identity,
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
                    segment.Surface.IsValid ? segment.Surface.Normal : Vector3.up,
                    segment.Surface.Identity,
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
            if (!step.IsPreSwing)
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

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        static bool IsFinite(Quaternion value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) &&
            float.IsFinite(value.z) && float.IsFinite(value.w) &&
            Quaternion.Dot(value, value) > 0f;

    }
}
