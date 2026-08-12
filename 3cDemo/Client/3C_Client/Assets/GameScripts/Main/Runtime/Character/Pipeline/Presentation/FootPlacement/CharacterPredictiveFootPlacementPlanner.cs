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
        readonly int m_GroundLayerMask;
        readonly FixedString64Bytes m_RigId;
        readonly FixedString64Bytes m_RigRevision;
        CharacterPredictiveFootPlacementRuntimeSettings m_Settings;
        CharacterPredictiveFootPlacementQuery m_Query;
        CharacterPredictiveFootPlacementDiagnostics m_Diagnostics;
        readonly CharacterPredictiveFootPlacementPlan m_LeftPlan;
        readonly CharacterPredictiveFootPlacementPlan m_RightPlan;
        ulong m_NextPlanSequence = 1;
        ulong m_PreparedRenderFrame;
        ulong m_PreparedCompletionIdentity;

        internal CharacterPredictiveFootPlacementPlanner(
            ActorId actorId,
            CharacterFootPlacementPoseRig rig,
            CharacterFootPlacementRuntimeSettings settings,
            CharacterFootPlacementWorldQueryBackend world)
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
            Vector3 desiredWorldVelocity = new Vector3(
                frame.DesiredPlanarVelocity.x,
                0f,
                frame.DesiredPlanarVelocity.y);
            PrepareFoot(
                CharacterFootSide.Left,
                m_LeftPlan,
                pose.Left,
                leftFeature,
                frame.RenderFrame,
                rootWorldPosition,
                rootWorldRotation,
                desiredWorldVelocity,
                m_Rig.LeftLegLength);
            PrepareFoot(
                CharacterFootSide.Right,
                m_RightPlan,
                pose.Right,
                rightFeature,
                frame.RenderFrame,
                rootWorldPosition,
                rootWorldRotation,
                desiredWorldVelocity,
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
                ResolveAuthoritativeConstraint(
                    in step,
                    ref constraintMode,
                    ref supportPhase,
                    ref bodyPivotMode);
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
                plan.Evaluate(
                    plan.Progress,
                    out pathPosition,
                    out pathRoot,
                    out pathHip,
                    out _);
                pathRootStart = plan.GetPathSegment(0).RootStart;
            }
            if (supportPhase == AnimationFootSupportPhase.ApproachingContact &&
                TryEvaluateFootTarget(
                    plan,
                    pose,
                    m_Rig.PoseRoot.up.normalized,
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
                hasContactTarget,
                constraintMode,
                supportPhase,
                bodyPivotMode,
                feature.PlantConfidence,
                plan.Progress,
                remainingSeconds,
                contactSurface,
                contactAnklePosition,
                contactAnkleRotation,
                pathPosition,
                pathRoot,
                pathRootStart,
                pathHip);
        }

        internal CharacterFootGroundingPlan Resolve(
            in CharacterFootPlacementPlanningFrame frame,
            in CharacterFullBodyIkGoalSetHeader ownerHeader,
            in CharacterFootGroundingPlan baseline)
        {
            RequireValidInput(
                in frame,
                in ownerHeader,
                in baseline);
            RequirePrepared(frame.RenderFrame, frame.CompletionIdentity);
            CharacterFootPlacementAnimatedPose pose = m_Rig.CaptureAnimatedPose(
                frame.RenderFrame,
                frame.UpstreamPose.DenseComponentPoses);
            CharacterFullBodyIkGoal left = ModifyFoot(
                CharacterFootSide.Left,
                m_LeftPlan,
                pose.Left,
                frame.UpstreamPose.LeftFootFeatures,
                baseline.LeftFoot,
                baseline.Diagnostics.Left,
                frame.RenderFrame,
                m_Rig.LeftLegLength,
                out CharacterPredictiveFootPlacementFootDiagnostics leftDiagnostics,
                out CharacterPredictiveFootLegFrameSnapshot leftDebugSnapshot);
            CharacterFullBodyIkGoal right = ModifyFoot(
                CharacterFootSide.Right,
                m_RightPlan,
                pose.Right,
                frame.UpstreamPose.RightFootFeatures,
                baseline.RightFoot,
                baseline.Diagnostics.Right,
                frame.RenderFrame,
                m_Rig.RightLegLength,
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
            return new CharacterFootGroundingPlan(
                baseline.Pelvis,
                left,
                right,
                baseline.Diagnostics);
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
            CharacterFullBodyIkGoal baseline,
            CharacterFootGroundingFootDiagnostics grounding,
            ulong renderFrame,
            float legLength,
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
            bool allowsStanceHandoff = AllowsStanceHandoff(plan);
            bool stanceOwnsFoot = grounding.ContactState != CharacterFootContactState.Swing &&
                                  allowsStanceHandoff;
            float stanceReleaseBlend = plan.State == CharacterPredictiveFootPlanState.Executing &&
                                       !allowsStanceHandoff
                ? Mathf.Clamp01(grounding.AnchorBlendWeight)
                : 0f;
            if (!stanceOwnsFoot)
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
            if (TryEvaluateFootTarget(
                    plan,
                    pose,
                    up,
                    out CharacterPredictiveFootTarget targetData))
            {
                clearanceEvaluated = true;
                currentPathPosition = targetData.PathPosition;
                currentPathRoot = targetData.PathRoot;
                currentPathHip = targetData.PathHip;
                currentPathSupport = targetData.Support;
                Vector3 supportNormal = currentPathSupport.IsValid
                    ? currentPathSupport.Normal.normalized
                    : up;
                preHeelDistance = Vector3.Dot(baselineContacts.HeelPosition - currentPathPosition, supportNormal);
                preToeDistance = Vector3.Dot(baselineContacts.ToePosition - currentPathPosition, supportNormal);
                requiredLift = Vector3.Dot(targetData.AnklePosition - pose.AnklePosition, up);
                float predictionBlend = 1f - stanceReleaseBlend;
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
                float residualPenetration = Mathf.Max(
                    0f,
                    -Mathf.Min(postHeelDistance, postToeDistance));
                float upNormalDot = Vector3.Dot(up, supportNormal);
                if (!stanceOwnsFoot && residualPenetration > 0f && upNormalDot > 0.0001f)
                {
                    resolvedAnklePosition += up * (residualPenetration / upNormalDot);
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
                if (!stanceOwnsFoot)
                    appliedLift = Vector3.Dot(resolvedAnklePosition - pose.AnklePosition, up);
                predictionReachRatio = Vector3.Distance(currentPathHip, resolvedAnklePosition) / legLength;
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
                pathSupportDiagnostics,
                preHeelDistance,
                preToeDistance,
                postHeelDistance,
                postToeDistance,
                clearanceEvaluated,
                Mathf.Max(0f, -Mathf.Min(postHeelDistance, postToeDistance)),
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
                plan.Progress,
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

        static bool TryEvaluateFootTarget(
            CharacterPredictiveFootPlacementPlan plan,
            CharacterFootPlacementAnimatedFootPose pose,
            Vector3 componentUp,
            out CharacterPredictiveFootTarget target)
        {
            target = default;
            if (plan.State != CharacterPredictiveFootPlanState.Executing)
                return false;
            Vector3 up = componentUp.normalized;
            plan.EvaluateClearancePath(
                plan.Progress,
                out Vector3 pathPosition,
                out Vector3 pathRoot,
                out Vector3 pathHip,
                out FootPlacementSurface pathSupport,
                out _);
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
            float targetSoleHeight = Vector3.Dot(pathPosition, up) +
                                     plan.EvaluateCurrentAnimationClearanceHeight();
            Vector3 targetSole = nativeSole + up * (targetSoleHeight - Vector3.Dot(nativeSole, up));
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
                toeDistance);
            return true;
        }

        void PrepareFoot(
            CharacterFootSide side,
            CharacterPredictiveFootPlacementPlan plan,
            CharacterFootPlacementAnimatedFootPose pose,
            AnimationFootFeatureSample feature,
            ulong renderFrame,
            Vector3 rootWorldPosition,
            Quaternion rootWorldRotation,
            Vector3 targetWorldVelocity,
            float legLength)
        {
            plan.BeginFrame();
            AnimationPredictedFootStepSample step = feature.PredictedStep;
            bool landingEventIdentityValid = step.HasConsistentLandingEventIdentity(side);
            if (plan.OwnsEvent &&
                (!landingEventIdentityValid || !plan.MatchesAuthoritativeEvent(in step)))
            {
                plan.Reset(CharacterPredictiveFootPlanEndReason.EventReplaced);
            }
            if (plan.HasExecutablePath)
            {
                if (plan.ShouldInterruptWorldMotion(targetWorldVelocity))
                {
                    plan.InterruptWorldMotion();
                }
                else
                {
                    plan.SynchronizeActionClock(renderFrame, in step);
                }
            }
            Transform component = m_Rig.PoseRoot;
            Vector3 up = component.up.normalized;
            CharacterFootPlacementSoleContactPose contacts = pose.ResolveSoleContacts(
                pose.AnklePosition,
                pose.AnkleRotation);
            Vector3 currentSole = (contacts.HeelPosition + contacts.ToePosition) * 0.5f;
            bool planningCandidate = landingEventIdentityValid &&
                                     step.Confidence >= m_Settings.MinimumLandingConfidence &&
                                     step.ActionStepClock.Phase < 0.9999f;
            if (planningCandidate && !plan.OwnsEvent)
            {
                CreatePlan(
                    side,
                    plan,
                    pose,
                    in step,
                    renderFrame,
                    currentSole,
                    rootWorldPosition,
                    rootWorldRotation,
                    targetWorldVelocity,
                    up,
                    legLength);
            }
        }

        void CreatePlan(
            CharacterFootSide side,
            CharacterPredictiveFootPlacementPlan plan,
            CharacterFootPlacementAnimatedFootPose pose,
            in AnimationPredictedFootStepSample step,
            ulong renderFrame,
            Vector3 currentSole,
            Vector3 rootStart,
            Quaternion rootStartRotation,
            Vector3 frozenWorldVelocity,
            Vector3 up,
            float legLength)
        {
            var rootTrajectory = new CharacterPredictiveFootRootTrajectory(
                rootStart,
                rootStartRotation,
                frozenWorldVelocity,
                currentSole,
                pose.HipPosition,
                pose.AnklePosition,
                up,
                in step);
            rootTrajectory.EvaluateEventPhase(1f, out Vector3 rootLanding, out Quaternion rootLandingRotation);
            Vector3 landing = rootTrajectory.EvaluateFootRoute(1f);
            Vector3 predictedHip = rootTrajectory.EvaluateHipRoute(1f);
            FootPredictionRejectReason rejectReason = FootPredictionRejectReason.None;
            CharacterPredictiveFootPlacementQueryResult query = default;
            ResolveVirtualGroundSplit(
                side,
                in step,
                in rootTrajectory,
                out float virtualGroundSplitFraction,
                out ulong virtualGroundSplitLandingEventIdentity);
            if (rejectReason == FootPredictionRejectReason.None &&
                (!IsFinite(landing) || !IsFinite(predictedHip) ||
                 !IsFinite(frozenWorldVelocity) ||
                 !IsFinite(rootLanding) || !IsFinite(rootLandingRotation)))
            {
                rejectReason = FootPredictionRejectReason.NonFinite;
            }
            else if (rejectReason == FootPredictionRejectReason.None)
            {
                query = m_Query.Query(
                    side == CharacterFootSide.Left ? 0 : 1,
                    currentSole,
                    in step,
                    in rootTrajectory,
                    virtualGroundSplitFraction,
                    virtualGroundSplitLandingEventIdentity,
                    m_GroundLayerMask,
                    up,
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
                    predictedHip = query.GroundEnvelope
                        .GetSegment(query.GroundEnvelope.Count - 1)
                        .HipEnd;
                }
            }
            ulong sequence = AllocatePlanSequence();
            if (rejectReason == FootPredictionRejectReason.None)
            {
                plan.Commit(
                    sequence,
                    renderFrame,
                    in step,
                    currentSole,
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
                    currentSole,
                    landing,
                    in rootTrajectory,
                    predictedHip,
                    rejectReason,
                    in query);
            }
        }

        static void ResolveVirtualGroundSplit(
            CharacterFootSide side,
            in AnimationPredictedFootStepSample step,
            in CharacterPredictiveFootRootTrajectory rootTrajectory,
            out float fraction,
            out ulong landingEventIdentity)
        {
            fraction = 0f;
            landingEventIdentity = 0;
            if (side != CharacterFootSide.Left && side != CharacterFootSide.Right)
                throw new ArgumentOutOfRangeException(nameof(side));
            if (!step.HasOpposingLandingEvent)
                return;
            float ownLandingSeconds = step.ActionStepClock.TimeToLandingSeconds;
            float opposingLandingSeconds = step.OpposingLandingDelaySeconds;
            float durationSeconds = step.ActionStepClock.DurationSeconds;
            if (!float.IsFinite(ownLandingSeconds) || !float.IsFinite(opposingLandingSeconds) ||
                !float.IsFinite(durationSeconds) || durationSeconds <= 0f ||
                opposingLandingSeconds <= 0.0001f ||
                opposingLandingSeconds >= ownLandingSeconds - 0.0001f)
            {
                return;
            }
            float eventPhase = step.ActionStepClock.Phase + opposingLandingSeconds / durationSeconds;
            if (eventPhase <= rootTrajectory.PathStartPhase + 0.0001f || eventPhase >= 0.9999f)
                return;
            fraction = Mathf.Clamp01(
                (eventPhase - rootTrajectory.PathStartPhase) /
                Mathf.Max(0.000001f, 1f - rootTrajectory.PathStartPhase));
            landingEventIdentity = step.OpposingLandingEventIdentity;
        }

        static FixedList512Bytes<CharacterPredictiveFootPathSampleDiagnostics> BuildPathDiagnostics(
            CharacterPredictiveFootPlacementPlan plan)
        {
            var result = new FixedList512Bytes<CharacterPredictiveFootPathSampleDiagnostics>();
            if (plan.GroundEnvelopeSegmentCount <= 0)
                return result;
            FootPlacementGroundEnvelopeSegment first = plan.GetPathSegment(0);
            result.Add(new CharacterPredictiveFootPathSampleDiagnostics(
                first.StartFraction,
                first.EdgeStart,
                first.Surface.IsValid ? first.Surface.Normal : Vector3.up,
                first.Surface.Identity,
                first.RootStart,
                first.HipStart));
            int outputCount = Mathf.Min(plan.GroundEnvelopeSegmentCount, Mathf.Min(7, result.Capacity - 1));
            for (int i = 0; i < outputCount; i++)
            {
                int segmentIndex = Mathf.Min(
                    plan.GroundEnvelopeSegmentCount - 1,
                    Mathf.RoundToInt(
                        (i + 1f) * plan.GroundEnvelopeSegmentCount / outputCount) - 1);
                FootPlacementGroundEnvelopeSegment segment = plan.GetPathSegment(segmentIndex);
                result.Add(new CharacterPredictiveFootPathSampleDiagnostics(
                    segment.EndFraction,
                    segment.EdgeEnd,
                    segment.Surface.IsValid ? segment.Surface.Normal : Vector3.up,
                    segment.Surface.Identity,
                    segment.RootEnd,
                    segment.HipEnd));
            }
            return result;
        }

        static FixedList128Bytes<Vector3> BuildPlannedFootRouteDiagnostics(
            CharacterPredictiveFootPlacementPlan plan)
        {
            var result = new FixedList128Bytes<Vector3>();
            if (!plan.OwnsEvent)
                return result;
            for (int i = 0; i < plan.FrozenWorldFootRoute.Length; i++)
                result.Add(plan.GetPlannedFootRouteSample(i));
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
                if (stanceOwnsFoot)
                    return FootPredictionRejectReason.StanceConstraintOwnsFoot;
                if (!float.IsFinite(predictionReachRatio))
                    return FootPredictionRejectReason.NonFinite;
                return rewritten
                    ? FootPredictionRejectReason.None
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
            ResolveAuthoritativeConstraint(
                plan.ActionStepPhase >= plan.LiftOffPhase && plan.ActionStepPhase < 0.9999f,
                plan.ActionStepPhase < plan.LiftOffPhase,
                ref constraintMode,
                ref supportPhase,
                ref bodyPivotMode);
        }

        static void ResolveAuthoritativeConstraint(
            in AnimationPredictedFootStepSample step,
            ref AnimationFootConstraintMode constraintMode,
            ref AnimationFootSupportPhase supportPhase,
            ref AnimationBodyRotationPivotMode bodyPivotMode)
        {
            ResolveAuthoritativeConstraint(
                step.ActionStepClock.IsSwing,
                step.ActionStepClock.IsPreSwing,
                ref constraintMode,
                ref supportPhase,
                ref bodyPivotMode);
        }

        static void ResolveAuthoritativeConstraint(
            bool isSwing,
            bool isPreSwing,
            ref AnimationFootConstraintMode constraintMode,
            ref AnimationFootSupportPhase supportPhase,
            ref AnimationBodyRotationPivotMode bodyPivotMode)
        {
            if (isSwing)
            {
                constraintMode = AnimationFootConstraintMode.Unlocked;
                supportPhase = supportPhase == AnimationFootSupportPhase.ApproachingContact
                    ? AnimationFootSupportPhase.ApproachingContact
                    : AnimationFootSupportPhase.Unsupported;
                bodyPivotMode = AnimationBodyRotationPivotMode.Pelvis;
                return;
            }
            if (!isPreSwing || constraintMode != AnimationFootConstraintMode.Unlocked)
                return;
            constraintMode = AnimationFootConstraintMode.Sliding;
            supportPhase = supportPhase == AnimationFootSupportPhase.Releasing
                ? AnimationFootSupportPhase.Releasing
                : AnimationFootSupportPhase.Supporting;
            bodyPivotMode = AnimationBodyRotationPivotMode.SupportFoot;
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
            return supportPhase == AnimationFootSupportPhase.ApproachingContact;
        }

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
            in CharacterFootGroundingPlan baseline)
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
                 !baseline.Diagnostics.IsCompleted ||
                 baseline.Diagnostics.FrameSequence != frame.RenderFrame ||
                 baseline.Diagnostics.CompletionIdentity != frame.CompletionIdentity ||
                 !IsBaselineGoal(baseline.Pelvis, CharacterFullBodyIkEffectorSlot.PelvisPreSolveTranslation, CharacterFullBodyIkGoalApplication.PelvisPreSolveTranslation, 0) ||
                 !IsBaselineGoal(baseline.LeftFoot, CharacterFullBodyIkEffectorSlot.LeftFoot, CharacterFullBodyIkGoalApplication.FootPlacementEffectorTarget, 1) ||
                 !IsBaselineGoal(baseline.RightFoot, CharacterFullBodyIkEffectorSlot.RightFoot, CharacterFullBodyIkGoalApplication.FootPlacementEffectorTarget, 2) ||
                 !SameGoal(baseline.LeftFoot, baseline.Diagnostics.Left.Goal) ||
                 !SameGoal(baseline.RightFoot, baseline.Diagnostics.Right.Goal))
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
