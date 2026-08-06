using System;
using RootMotion.FinalIK;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal readonly struct PredictedFootprint
    {
        internal PredictedFootprint(
            Vector3 position,
            float horizon,
            bool horizonClamped,
            FootPredictionRejectReason rejectReason)
        {
            Position = position;
            Horizon = horizon;
            HorizonClamped = horizonClamped;
            RejectReason = rejectReason;
        }

        internal Vector3 Position { get; }
        internal float Horizon { get; }
        internal bool HorizonClamped { get; }
        internal FootPredictionRejectReason RejectReason { get; }
        internal bool IsAccepted => RejectReason == FootPredictionRejectReason.None;
    }

    internal readonly struct CharacterFootPlacementPlan
    {
        internal CharacterFootPlacementPlan(
            CharacterFullBodyIkGoal pelvis,
            CharacterFullBodyIkGoal leftFoot,
            CharacterFullBodyIkGoal rightFoot,
            CharacterPredictiveFootPlacementDiagnostics diagnostics)
        {
            Pelvis = pelvis;
            LeftFoot = leftFoot;
            RightFoot = rightFoot;
            Diagnostics = diagnostics;
            if (!Pelvis.IsValid || !LeftFoot.IsValid || !RightFoot.IsValid || !Diagnostics.IsCompleted)
                throw new ArgumentException("Foot Placement Plan is invalid.");
        }

        internal CharacterFullBodyIkGoal Pelvis { get; }
        internal CharacterFullBodyIkGoal LeftFoot { get; }
        internal CharacterFullBodyIkGoal RightFoot { get; }
        internal CharacterPredictiveFootPlacementDiagnostics Diagnostics { get; }

        internal void WriteGoals(NativeSlice<CharacterFullBodyIkGoal> output)
        {
            if (output.Length != 3)
                throw new ArgumentException("Foot Placement Plan requires exactly three Goal slots.", nameof(output));
            output[0] = Pelvis;
            output[1] = LeftFoot;
            output[2] = RightFoot;
        }
    }

    internal sealed class CharacterPredictiveFootPlacementGoalSource : IDisposable
    {
        readonly CharacterFootPlacementPoseRig m_Rig;
        readonly CharacterFootPlacementPlanner m_Planner;

        internal CharacterPredictiveFootPlacementGoalSource(
            ActorId actorId,
            CharacterFootPlacementRuntimeSettings settings,
            CharacterFootPlacementPoseRig rig,
            PhysicsScene physicsScene)
        {
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            m_Planner = new CharacterFootPlacementPlanner(actorId, settings, rig, physicsScene);
        }

        internal CharacterPredictiveFootPlacementDiagnostics Diagnostics => m_Planner.Diagnostics;

        internal string ApplyTuning(
            CharacterFinalIkGroundingSettings grounding,
            CharacterPredictiveFootPlacementRuntimeSettings predictive,
            bool resetOwnerState)
        {
            try
            {
                m_Planner.ApplyTuning(grounding, predictive, resetOwnerState);
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
                    m_Planner.Settings.Grounding,
                    m_Planner.Settings.Predictive,
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
            CharacterFootPlacementPlan plan = m_Planner.Plan(in frame, weightParameterIndex);
            plan.WriteGoals(goalOutput);
            return new CharacterFullBodyIkGoalSetHeader(
                frame.RenderFrame,
                frame.CompletionIdentity,
                m_Rig.Rig.RigId,
                m_Rig.Rig.RigRevision,
                producerOperationIndex,
                producerCallSiteIndex,
                goalWorkspaceOffset,
                3,
                CharacterFullBodyIkGoalSetAvailability.Ready);
        }

        internal void Reset(CharacterFootPlacementReset reset) => m_Planner.Reset(reset);

        public void Dispose() => m_Planner.Dispose();
    }

    internal sealed class CharacterFootPlacementPlanner : IDisposable
    {
        sealed class FootState
        {
            internal FootState(CharacterFootSide side) => Side = side;
            internal CharacterFootSide Side { get; }
            internal FootConstraintState ConstraintState = FootConstraintState.Free;
            internal FootConstraintTransitionReason TransitionReason;
            internal FootPlacementSurface LockedSurface;
            internal Vector3 LockedLocalPosition;
            internal Vector3 LockedLocalPlantPosition;
            internal Quaternion LockedLocalRotation = Quaternion.identity;
            internal bool HasAnchor;

            internal void CaptureAnchor(
                FootPlacementSurface surface,
                Vector3 worldPosition,
                Quaternion worldRotation,
                Vector3 worldPlantPosition)
            {
                if (!surface.IsValid)
                    throw new InvalidOperationException("Foot Placement lock requires a valid surface.");
                LockedSurface = surface;
                LockedLocalPosition = surface.Transform.InverseTransformPoint(worldPosition);
                LockedLocalPlantPosition = surface.Transform.InverseTransformPoint(worldPlantPosition);
                LockedLocalRotation =
                    (Quaternion.Inverse(surface.Transform.rotation) * worldRotation).normalized;
                HasAnchor = true;
                ConstraintState = FootConstraintState.Locked;
                TransitionReason = FootConstraintTransitionReason.ContactCommitted;
            }

            internal void UpdateAnchor(
                Vector3 worldPosition,
                Quaternion worldRotation,
                Vector3 worldPlantPosition)
            {
                if (!HasAnchor || !LockedSurface.IsValid)
                    throw new InvalidOperationException("Foot Placement anchor is unavailable.");
                LockedLocalPosition = LockedSurface.Transform.InverseTransformPoint(worldPosition);
                LockedLocalPlantPosition = LockedSurface.Transform.InverseTransformPoint(worldPlantPosition);
                LockedLocalRotation =
                    (Quaternion.Inverse(LockedSurface.Transform.rotation) * worldRotation).normalized;
            }

            internal bool TryResolveAnchor(
                CharacterPredictiveFootPlacementRuntimeSettings settings,
                int groundLayerMask,
                out Vector3 worldPosition,
                out Quaternion worldRotation,
                out Vector3 worldPlantPosition,
                out FootPlacementSurface surface)
            {
                surface = HasAnchor ? LockedSurface.Rebuild() : default;
                if (!surface.IsValid ||
                    !surface.Collider.enabled ||
                    !surface.Transform.gameObject.activeInHierarchy ||
                    (groundLayerMask & (1 << surface.Transform.gameObject.layer)) == 0 ||
                    Vector3.Angle(Vector3.up, surface.Normal) > settings.MaximumSlopeDegrees)
                {
                    worldPosition = Vector3.zero;
                    worldRotation = Quaternion.identity;
                    worldPlantPosition = Vector3.zero;
                    return false;
                }
                worldPosition = surface.Transform.TransformPoint(LockedLocalPosition);
                worldRotation = (surface.Transform.rotation * LockedLocalRotation).normalized;
                worldPlantPosition = surface.Transform.TransformPoint(LockedLocalPlantPosition);
                return IsFinite(worldPosition) && IsFinite(worldPlantPosition) && IsUnit(worldRotation);
            }

            internal void Release(FootConstraintTransitionReason reason, bool retainAnchor)
            {
                ConstraintState = FootConstraintState.Free;
                TransitionReason = reason;
                if (!retainAnchor)
                    ClearAnchor();
            }

            internal void ClearAnchor()
            {
                LockedSurface = default;
                LockedLocalPosition = Vector3.zero;
                LockedLocalPlantPosition = Vector3.zero;
                LockedLocalRotation = Quaternion.identity;
                HasAnchor = false;
            }

            internal void Reset(FootConstraintTransitionReason reason)
            {
                ConstraintState = FootConstraintState.Free;
                TransitionReason = reason;
                ClearAnchor();
            }

            static bool IsFinite(Vector3 value) =>
                float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

            static bool IsUnit(Quaternion value)
            {
                float squareMagnitude = value.x * value.x + value.y * value.y +
                                        value.z * value.z + value.w * value.w;
                return float.IsFinite(squareMagnitude) &&
                       Mathf.Abs(squareMagnitude - 1f) <= 0.01f;
            }
        }

        readonly ActorId m_ActorId;
        readonly CharacterFootPlacementRuntimeSettings m_Settings;
        readonly CharacterFootPlacementPoseRig m_Rig;
        readonly CharacterFootPlacementWorldQueryBackend m_World;
        readonly CharacterFinalIkGroundingAdapter m_Grounding;
        readonly CharacterFootPlacementPelvisPlanner m_Pelvis = new CharacterFootPlacementPelvisPlanner();
        readonly FootState m_Left = new FootState(CharacterFootSide.Left);
        readonly FootState m_Right = new FootState(CharacterFootSide.Right);
        ulong m_LastRenderFrame;
        ulong m_ResetSequence;
        float m_GroundingTime;
        CharacterPredictiveFootPlacementDiagnostics m_Diagnostics;
        bool m_Disposed;

        internal CharacterFootPlacementPlanner(
            ActorId actorId,
            CharacterFootPlacementRuntimeSettings settings,
            CharacterFootPlacementPoseRig rig,
            PhysicsScene physicsScene)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Predictive Foot Placement Actor identity is invalid.", nameof(actorId));
            m_ActorId = actorId;
            m_Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            m_Rig.RequireValid();
            m_World = new CharacterFootPlacementWorldQueryBackend(
                physicsScene,
                rig,
                settings.Predictive.HitCapacity,
                settings.Predictive.MaximumSlopeDegrees);
            m_Grounding = new CharacterFinalIkGroundingAdapter(settings.Grounding);
            ResetInternal(0, FootConstraintTransitionReason.PresentationReset);
        }

        internal CharacterPredictiveFootPlacementDiagnostics Diagnostics => m_Diagnostics;

        internal CharacterFootPlacementRuntimeSettings Settings => m_Settings;

        internal void ApplyTuning(
            CharacterFinalIkGroundingSettings grounding,
            CharacterPredictiveFootPlacementRuntimeSettings predictive,
            bool resetOwnerState)
        {
            m_Settings.ApplyTuning(grounding, predictive);
            m_Grounding.ApplyTuning(grounding);
            m_World.ApplyMaximumSlope(predictive.MaximumSlopeDegrees);
            if (resetOwnerState)
                ResetInternal(m_ResetSequence, FootConstraintTransitionReason.PresentationReset);
        }

        internal CharacterFootPlacementPlan Plan(
            in CharacterFootPlacementPlanningFrame frame,
            int weightParameterIndex)
        {
            RequireAlive();
            if (frame.ActorId != m_ActorId || frame.RenderFrame == m_LastRenderFrame)
                throw new InvalidOperationException("Predictive Foot Placement frame identity is invalid or duplicated.");
            if (!frame.Body.IsValid || frame.PresentationDeltaSeconds <= 0f)
                throw new InvalidOperationException("Predictive Foot Placement requires valid body and delta inputs.");
            if (frame.Body.ResetSequence != m_ResetSequence)
                ResetInternal(frame.Body.ResetSequence, FootConstraintTransitionReason.BodyReset);

            CharacterFootPlacementPoseInput animationPose = frame.UpstreamPose;
            CharacterFootPlacementFeatureFrame features = ResolveFeatures(
                animationPose,
                weightParameterIndex);
            CharacterFootPlacementAnimatedPose pose = m_Rig.CaptureAnimatedPose(
                frame.RenderFrame,
                animationPose.DenseComponentPoses);
            GroundingFrameInput groundingFrame = BuildGroundingFrame(
                frame,
                animationPose,
                pose,
                features);
            m_World.BeginGroundingFrameDiagnostics();
            CharacterFinalIkGroundingResult grounding = m_Grounding.Evaluate(
                in groundingFrame,
                m_World,
                new GroundingComponentTransform(m_Rig.PoseRoot.position, m_Rig.PoseRoot.rotation));
            CharacterGroundingQueryDiagnostics leftHeelRequest = QueryDiagnostics(
                m_World.TryGetLastRayRequest(0, out GroundingQueryRequest leftHeel),
                in leftHeel);
            CharacterGroundingQueryDiagnostics leftToeRequest = QueryDiagnostics(
                m_World.TryGetLastToeRequest(0, out GroundingQueryRequest leftToe),
                in leftToe);
            CharacterGroundingQueryDiagnostics leftFootCenterRequest = QueryDiagnostics(
                m_World.TryGetLastFootCenterRequest(0, out GroundingQueryRequest leftFootCenter),
                in leftFootCenter);
            CharacterGroundingQueryDiagnostics rightHeelRequest = QueryDiagnostics(
                m_World.TryGetLastRayRequest(1, out GroundingQueryRequest rightHeel),
                in rightHeel);
            CharacterGroundingQueryDiagnostics rightToeRequest = QueryDiagnostics(
                m_World.TryGetLastToeRequest(1, out GroundingQueryRequest rightToe),
                in rightToe);
            CharacterGroundingQueryDiagnostics rightFootCenterRequest = QueryDiagnostics(
                m_World.TryGetLastFootCenterRequest(1, out GroundingQueryRequest rightFootCenter),
                in rightFootCenter);

            FootGoal left = ResolveFoot(
                m_Left,
                pose.Left,
                features.Left,
                grounding.LeftFoot,
                frame.Body,
                frame.PresentationDeltaSeconds,
                m_Rig.LeftLegLength,
                features.Value);
            FootGoal right = ResolveFoot(
                m_Right,
                pose.Right,
                features.Right,
                grounding.RightFoot,
                frame.Body,
                frame.PresentationDeltaSeconds,
                m_Rig.RightLegLength,
                features.Value);
            ApplyFootSeparation(ref left, ref right, poseRoot: m_Rig.PoseRoot);

            CharacterFootPlacementPelvisPlan pelvisPlan = m_Pelvis.Plan(
                BuildPelvisLegInput(CharacterFootSide.Left, pose.Left, left, m_Rig.LeftLegLength),
                BuildPelvisLegInput(CharacterFootSide.Right, pose.Right, right, m_Rig.RightLegLength),
                m_Rig.PoseRoot.up,
                m_Rig.PoseRoot.forward,
                frame.Body.VisibleVelocity,
                m_Rig.PoseRoot.position,
                frame.PresentationDeltaSeconds,
                m_Settings.Predictive);
            if (pelvisPlan.RejectLeftGoal)
            {
                m_Left.Release(FootConstraintTransitionReason.PelvisRangeConflictReleased, false);
                left = left.ReleaseForPelvisConflict();
            }
            if (pelvisPlan.RejectRightGoal)
            {
                m_Right.Release(FootConstraintTransitionReason.PelvisRangeConflictReleased, false);
                right = right.ReleaseForPelvisConflict();
            }
            left = left.ApplyPelvis(pose.Left, pelvisPlan.ComponentTranslation, m_Rig.PoseRoot);
            right = right.ApplyPelvis(pose.Right, pelvisPlan.ComponentTranslation, m_Rig.PoseRoot);

            var pelvis = new CharacterFullBodyIkGoal(
                CharacterFullBodyIkEffectorSlot.PelvisPreSolveTranslation,
                pelvisPlan.ComponentTranslation,
                Quaternion.identity,
                1f,
                0f,
                CharacterFullBodyIkGoalApplication.PelvisPreSolveTranslation,
                CharacterFullBodyIkGoalSourceKind.FinalIkGrounding,
                CharacterFullBodyIkPlantPivotMode.None,
                Vector3.zero,
                0f,
                0);
            m_Diagnostics = new CharacterPredictiveFootPlacementDiagnostics(
                frame.RenderFrame,
                frame.CompletionIdentity,
                frame.Body.ResetSequence,
                CharacterFinalIkGroundingAdapter.BackendIdentity,
                pelvisPlan,
                new CharacterGroundingHitDiagnostics(grounding.RootHit),
                grounding.Grounded,
                BuildFootDiagnostics(
                    m_Left,
                    leftHeelRequest,
                    leftToeRequest,
                    leftFootCenterRequest,
                    grounding.LeftFoot,
                    features.Left,
                    left),
                BuildFootDiagnostics(
                    m_Right,
                    rightHeelRequest,
                    rightToeRequest,
                    rightFootCenterRequest,
                    grounding.RightFoot,
                    features.Right,
                    right));
            m_LastRenderFrame = frame.RenderFrame;
            m_ResetSequence = frame.Body.ResetSequence;
            return new CharacterFootPlacementPlan(
                pelvis,
                left.Goal,
                right.Goal,
                m_Diagnostics);
        }

        internal void Reset(CharacterFootPlacementReset reset)
        {
            if (m_Disposed)
                return;
            ResetInternal(reset.ResetSequence, ToTransitionReason(reset.Reason));
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            ResetInternal(m_ResetSequence, FootConstraintTransitionReason.PresentationReset);
            m_Disposed = true;
        }

        CharacterFootPlacementFeatureFrame ResolveFeatures(
            CharacterFootPlacementPoseInput animationPose,
            int weightParameterIndex)
        {
            if (!string.Equals(animationPose.PosePlanHash, m_Settings.PosePlanHash, StringComparison.Ordinal))
                throw new InvalidOperationException("Predictive Foot Placement Pose Plan identity is stale.");
            float weight = 1f;
            if (weightParameterIndex >= 0)
            {
                if ((uint)weightParameterIndex >= (uint)animationPose.PoseParameters.Length ||
                    animationPose.PoseParameterAvailability[weightParameterIndex] != 1)
                {
                    throw new InvalidOperationException("Predictive Foot Placement Weight is unavailable.");
                }
                weight = animationPose.PoseParameters[weightParameterIndex];
            }
            if (!float.IsFinite(weight) || weight < 0f || weight > 1f)
                throw new InvalidOperationException("Predictive Foot Placement Weight is invalid.");
            return new CharacterFootPlacementFeatureFrame(
                weight,
                animationPose.LeftFootFeatures,
                animationPose.RightFootFeatures);
        }

        GroundingFrameInput BuildGroundingFrame(
            in CharacterFootPlacementPlanningFrame frame,
            in CharacterFootPlacementPoseInput animationPose,
            in CharacterFootPlacementAnimatedPose pose,
            in CharacterFootPlacementFeatureFrame features)
        {
            Transform poseRoot = m_Rig.PoseRoot;
            AnimationLocalBonePose rootPose = animationPose.DenseComponentPoses[m_Rig.Rig.RootPhysicalBoneIndex];
            AnimationLocalBonePose pelvisPose = animationPose.DenseComponentPoses[m_Rig.Rig.PelvisPhysicalBoneIndex];
            m_GroundingTime += frame.PresentationDeltaSeconds;
            return new GroundingFrameInput(
                m_GroundingTime,
                frame.PresentationDeltaSeconds,
                m_World.PhysicsScene,
                m_Settings.Grounding.GroundLayerMask,
                new GroundingComponentTransform(
                    poseRoot.TransformPoint(rootPose.Position),
                    poseRoot.rotation * rootPose.Rotation),
                new GroundingComponentTransform(
                    poseRoot.TransformPoint(pose.PelvisLocalPosition),
                    poseRoot.rotation * pelvisPose.Rotation),
                BuildGroundingFoot(0, pose.Left, ResolvePlantWeight(frame.Body, features.Value, features.Left)),
                BuildGroundingFoot(1, pose.Right, ResolvePlantWeight(frame.Body, features.Value, features.Right)),
                2);
        }

        static GroundingFootInput BuildGroundingFoot(
            int footIndex,
            CharacterFootPlacementAnimatedFootPose pose,
            float plantWeight)
        {
            Vector3 center = (pose.HeelPosition + pose.ToePosition) * 0.5f;
            return new GroundingFootInput(
                footIndex,
                new GroundingComponentTransform(pose.AnklePosition, pose.AnkleRotation),
                new GroundingComponentTransform(pose.HeelPosition, pose.SemanticRotation),
                new GroundingComponentTransform(pose.ToePosition, pose.ToeRotation),
                new GroundingComponentTransform(center, pose.SemanticRotation),
                plantWeight);
        }

        static float ResolvePlantWeight(
            CharacterBodyPresentationFrame body,
            float policyWeight,
            AnimationFootFeatureSample feature)
        {
            if (!ResolveBodyGrounded(body))
                return 0f;
            float plantedWeight = Mathf.InverseLerp(0.5f, 1f, feature.PlantConfidence);
            return Mathf.Clamp01(policyWeight * plantedWeight);
        }

        static float ResolvePlacementWeight(
            CharacterBodyPresentationFrame body,
            float policyWeight,
            bool hasGroundingSurface)
        {
            if (!hasGroundingSurface || !ResolveBodyGrounded(body))
                return 0f;
            return Mathf.Clamp01(policyWeight);
        }

        static bool ResolveBodyGrounded(CharacterBodyPresentationFrame body) =>
            body.TargetGrounded || body.GroundedBefore || body.GroundedAfter;

        FootGoal ResolveFoot(
            FootState state,
            CharacterFootPlacementAnimatedFootPose pose,
            AnimationFootFeatureSample feature,
            CharacterFinalIkGroundingFootResult grounding,
            CharacterBodyPresentationFrame body,
            float deltaSeconds,
            float legLength,
            float policyWeight)
        {
            state.TransitionReason = FootConstraintTransitionReason.None;
            CharacterPredictiveFootPlacementRuntimeSettings settings = m_Settings.Predictive;
            Transform poseRoot = m_Rig.PoseRoot;
            Vector3 currentSole = (pose.HeelPosition + pose.ToePosition) * 0.5f;
            FootPlacementSurface support = BuildSupport(grounding.CurrentGroundingHit);
            FootPlacementSurface toeSupport = BuildSupport(grounding.ToeHit);
            Vector3 groundingWorldPosition = poseRoot.TransformPoint(grounding.ComponentPosition);
            Quaternion groundingWorldRotation =
                (poseRoot.rotation * grounding.ComponentRotation).normalized;
            bool usesToePivot = settings.LockType == CharacterFootPlantLockType.PivotAroundToe;
            FootPlacementSurface plantSurface = usesToePivot ? toeSupport : support;
            Vector3 currentPlantPosition = usesToePivot ? pose.ToePosition : currentSole;
            Vector3 groundingPlantPosition = usesToePivot && toeSupport.IsValid
                ? grounding.ToeHit.Point
                : usesToePivot
                    ? ResolveToePosition(pose, groundingWorldPosition, groundingWorldRotation)
                    : groundingWorldPosition;
            Vector3 soleVelocity = ResolveContactVelocity(
                feature.SoleLocalVelocity,
                currentSole,
                body,
                poseRoot);
            float planarSpeed = new Vector2(soleVelocity.x, soleVelocity.z).magnitude;
            float verticalSpeed = soleVelocity.y >= 0f
                ? soleVelocity.y
                : Mathf.Max(0f, -soleVelocity.y - settings.DescendingTolerance);
            bool bodyGrounded = ResolveBodyGrounded(body);
            bool policyActive = policyWeight >= settings.MinimumSourceContribution;

            bool hasAnchor = state.TryResolveAnchor(
                settings,
                m_Settings.Grounding.GroundLayerMask,
                out Vector3 anchorWorldPosition,
                out Quaternion anchorWorldRotation,
                out Vector3 anchorWorldPlantPosition,
                out FootPlacementSurface anchorSurface);
            if (state.HasAnchor && !hasAnchor)
            {
                state.Release(FootConstraintTransitionReason.SurfaceInvalid, false);
                hasAnchor = false;
            }
            if (!policyActive)
            {
                state.Release(FootConstraintTransitionReason.PolicyReleased, false);
                hasAnchor = false;
            }
            else if (settings.LockType == CharacterFootPlantLockType.Unlocked)
            {
                state.Release(FootConstraintTransitionReason.PolicyReleased, false);
                hasAnchor = false;
            }
            else if (!bodyGrounded)
            {
                state.Release(FootConstraintTransitionReason.BodyAirborne, false);
                hasAnchor = false;
            }

            FootPlacementSurface contactSurface = hasAnchor ? anchorSurface : plantSurface;
            float surfaceDistance = contactSurface.IsValid
                ? Mathf.Abs(Vector3.Dot(
                    currentPlantPosition - contactSurface.Point,
                    contactSurface.Normal))
                : float.PositiveInfinity;
            float contactWeight = ResolveContactWeight(
                feature.PlantConfidence,
                policyWeight,
                planarSpeed,
                verticalSpeed,
                surfaceDistance,
                contactSurface.IsValid,
                settings);
            if (!bodyGrounded)
                contactWeight = 0f;
            bool hasGroundingSurface = grounding.Grounded && grounding.CurrentGroundingHit.HasHit;
            float plantWeight = policyActive && hasGroundingSurface
                ? ResolvePlantWeight(body, policyWeight, feature)
                : 0f;
            float placementWeight = policyActive
                ? ResolvePlacementWeight(body, policyWeight, hasGroundingSurface)
                : 0f;

            if (state.ConstraintState == FootConstraintState.Free && state.HasAnchor)
            {
                if (contactWeight <= 0.0001f)
                {
                    state.ClearAnchor();
                    hasAnchor = false;
                }
            }

            if (state.ConstraintState == FootConstraintState.Free && !state.HasAnchor)
            {
                bool canPlant = policyActive &&
                                bodyGrounded &&
                                grounding.Grounded &&
                                plantSurface.IsValid &&
                                settings.LockType != CharacterFootPlantLockType.Unlocked &&
                                feature.PlantConfidence >= settings.PlantConfidenceEnter &&
                                planarSpeed <= settings.PlantPlanarSpeed &&
                                verticalSpeed <= settings.PlantVerticalSpeed &&
                                surfaceDistance <= settings.PlantDistance;
                if (canPlant)
                {
                    state.CaptureAnchor(
                        plantSurface,
                        groundingWorldPosition,
                        groundingWorldRotation,
                        groundingPlantPosition);
                    hasAnchor = state.TryResolveAnchor(
                        settings,
                        m_Settings.Grounding.GroundLayerMask,
                        out anchorWorldPosition,
                        out anchorWorldRotation,
                        out anchorWorldPlantPosition,
                        out anchorSurface);
                }
            }

            Vector3 targetWorldPosition = groundingWorldPosition;
            Quaternion targetWorldRotation = groundingWorldRotation;
            Vector3 targetPlantPosition = groundingPlantPosition;
            bool hasPlantPivot = false;
            if (hasAnchor)
            {
                ResolveAnchoredTargetForLock(
                    settings.LockType,
                    pose,
                    anchorWorldPosition,
                    anchorWorldRotation,
                    anchorWorldPlantPosition,
                    groundingWorldRotation,
                    out targetWorldPosition,
                    out targetWorldRotation,
                    out targetPlantPosition,
                    out hasPlantPivot);
            }
            Vector3 plannedHipPosition = pose.HipPosition;
            float extensionRatio = Vector3.Distance(plannedHipPosition, targetWorldPosition) / legLength;
            float ankleTwistDegrees = hasAnchor && plantSurface.IsValid
                ? ResolveAnkleTwistDegrees(
                    pose,
                    anchorWorldRotation,
                    groundingWorldRotation,
                    plantSurface.Normal)
                : 0f;

            if (state.ConstraintState != FootConstraintState.Free)
            {
                float drift = Vector3.Distance(
                    usesToePivot ? anchorWorldPlantPosition : anchorWorldPosition,
                    usesToePivot ? groundingPlantPosition : groundingWorldPosition);
                float supportAngle = plantSurface.IsValid
                    ? Vector3.Angle(anchorSurface.Normal, plantSurface.Normal)
                    : 0f;
                bool release = feature.PlantConfidence <= settings.PlantConfidenceExit ||
                               planarSpeed >= settings.ReleasePlanarSpeed ||
                               verticalSpeed >= settings.ReleaseVerticalSpeed ||
                               surfaceDistance >= settings.ReleaseDistance;
                if (release)
                {
                    state.Release(
                        FootConstraintTransitionReason.ContactReleased,
                        contactWeight > 0.0001f);
                }
                else if (drift >= settings.ReplantDistance ||
                         drift >= settings.MaximumSlideDistance ||
                         supportAngle > settings.ReplantAngleDegrees)
                {
                    state.Release(
                        FootConstraintTransitionReason.ReplantThresholdExceeded,
                        contactWeight > 0.0001f);
                }
                else if (ankleTwistDegrees > settings.MaximumAnkleTwistDegrees)
                {
                    state.Release(
                        FootConstraintTransitionReason.AnkleTwistExceeded,
                        contactWeight > 0.0001f);
                }
                else if (plantSurface.IsValid &&
                         plantSurface.Identity == anchorSurface.Identity &&
                         drift >= settings.SlideStartDistance)
                {
                    state.ConstraintState = FootConstraintState.Sliding;
                    state.TransitionReason = FootConstraintTransitionReason.AnimationDrift;
                }
            }

            if (state.ConstraintState == FootConstraintState.Sliding && state.HasAnchor)
            {
                float maximumMove = settings.SlideSpeed * deltaSeconds;
                Vector3 slidPosition = Vector3.MoveTowards(
                    anchorWorldPosition,
                    groundingWorldPosition,
                    maximumMove);
                Vector3 slidPlantPosition = Vector3.MoveTowards(
                    anchorWorldPlantPosition,
                    groundingPlantPosition,
                    maximumMove);
                float distance = Vector3.Distance(
                    usesToePivot ? anchorWorldPlantPosition : anchorWorldPosition,
                    usesToePivot ? groundingPlantPosition : groundingWorldPosition);
                float rotationT = distance > 0.0001f
                    ? Mathf.Clamp01(maximumMove / distance)
                    : 1f;
                Quaternion slidRotation = Quaternion.Slerp(
                    anchorWorldRotation,
                    groundingWorldRotation,
                    rotationT).normalized;
                state.UpdateAnchor(slidPosition, slidRotation, slidPlantPosition);
                hasAnchor = state.TryResolveAnchor(
                    settings,
                    m_Settings.Grounding.GroundLayerMask,
                    out anchorWorldPosition,
                    out anchorWorldRotation,
                    out anchorWorldPlantPosition,
                    out anchorSurface);
                ResolveAnchoredTargetForLock(
                    settings.LockType,
                    pose,
                    anchorWorldPosition,
                    anchorWorldRotation,
                    anchorWorldPlantPosition,
                    groundingWorldRotation,
                    out targetWorldPosition,
                    out targetWorldRotation,
                    out targetPlantPosition,
                    out hasPlantPivot);
                if (Vector3.Distance(
                        usesToePivot ? targetPlantPosition : targetWorldPosition,
                        usesToePivot ? groundingPlantPosition : groundingWorldPosition) <=
                    settings.SlideStopDistance)
                {
                    state.ConstraintState = FootConstraintState.Locked;
                    state.TransitionReason = FootConstraintTransitionReason.SlideSettled;
                }
            }
            else if (state.HasAnchor && hasAnchor)
            {
                ResolveAnchoredTargetForLock(
                    settings.LockType,
                    pose,
                    anchorWorldPosition,
                    anchorWorldRotation,
                    anchorWorldPlantPosition,
                    groundingWorldRotation,
                    out targetWorldPosition,
                    out targetWorldRotation,
                    out targetPlantPosition,
                    out hasPlantPivot);
            }
            else
            {
                targetWorldPosition = groundingWorldPosition;
                targetWorldRotation = groundingWorldRotation;
                targetPlantPosition = groundingPlantPosition;
                hasPlantPivot = false;
            }
            extensionRatio = Vector3.Distance(plannedHipPosition, targetWorldPosition) / legLength;

            Vector3 componentPosition = ToComponentPoint(poseRoot, targetWorldPosition);
            Quaternion componentRotation =
                (Quaternion.Inverse(poseRoot.rotation) * targetWorldRotation).normalized;
            Vector3 componentPlantPivot = hasPlantPivot
                ? ToComponentPoint(poseRoot, targetPlantPosition)
                : Vector3.zero;
            float positionGoalWeight = hasAnchor ? contactWeight : plantWeight;
            float rotationGoalWeight = hasAnchor ? contactWeight : plantWeight;
            var goal = new CharacterFullBodyIkGoal(
                state.Side == CharacterFootSide.Left
                    ? CharacterFullBodyIkEffectorSlot.LeftFoot
                    : CharacterFullBodyIkEffectorSlot.RightFoot,
                componentPosition,
                componentRotation,
                positionGoalWeight,
                rotationGoalWeight,
                CharacterFullBodyIkGoalApplication.GroundingEffectorTarget,
                hasAnchor
                    ? CharacterFullBodyIkGoalSourceKind.FinalIkGrounding |
                      CharacterFullBodyIkGoalSourceKind.PredictiveExtension
                    : CharacterFullBodyIkGoalSourceKind.FinalIkGrounding,
                hasPlantPivot
                    ? CharacterFullBodyIkPlantPivotMode.Toe
                    : CharacterFullBodyIkPlantPivotMode.None,
                componentPlantPivot,
                hasPlantPivot ? settings.HeelLiftRatio : 0f,
                state.Side == CharacterFootSide.Left ? 1 : 2);
            var groundingGoal = new CharacterFullBodyIkGoal(
                state.Side == CharacterFootSide.Left
                    ? CharacterFullBodyIkEffectorSlot.LeftFoot
                    : CharacterFullBodyIkEffectorSlot.RightFoot,
                ToComponentPoint(poseRoot, groundingWorldPosition),
                grounding.ComponentRotation,
                plantWeight,
                plantWeight,
                CharacterFullBodyIkGoalApplication.GroundingEffectorTarget,
                CharacterFullBodyIkGoalSourceKind.FinalIkGrounding,
                CharacterFullBodyIkPlantPivotMode.None,
                Vector3.zero,
                0f,
                state.Side == CharacterFootSide.Left ? 1 : 2);
            return new FootGoal(
                goal,
                groundingGoal,
                plannedHipPosition,
                legLength,
                extensionRatio,
                ankleTwistDegrees,
                0f,
                placementWeight,
                plantWeight,
                contactWeight,
                default,
                soleVelocity,
                new PredictedFootprint(
                    currentSole,
                    0f,
                    false,
                    FootPredictionRejectReason.NoFutureLanding),
                plantSurface);
        }

        void ApplyFootSeparation(
            ref FootGoal left,
            ref FootGoal right,
            Transform poseRoot)
        {
            float minimum = m_Settings.Predictive.MinimumFootSeparation;
            if (minimum <= 0f ||
                left.ContactWeight <= 0.0001f ||
                right.ContactWeight <= 0.0001f)
                return;

            Vector3 leftWorld = poseRoot.TransformPoint(left.Goal.ComponentPosition);
            Vector3 rightWorld = poseRoot.TransformPoint(right.Goal.ComponentPosition);
            Vector3 delta = Vector3.ProjectOnPlane(rightWorld - leftWorld, poseRoot.up);
            float distance = delta.magnitude;
            if (distance >= minimum)
                return;

            bool leftMovable = m_Left.ConstraintState == FootConstraintState.Free;
            bool rightMovable = m_Right.ConstraintState == FootConstraintState.Free;
            if (!leftMovable && !rightMovable)
            {
                if (left.Goal.PositionWeight < right.Goal.PositionWeight)
                {
                    m_Left.Release(FootConstraintTransitionReason.FootSeparationReleased, false);
                    left = left.UseGrounding(poseRoot);
                    leftMovable = true;
                }
                else
                {
                    m_Right.Release(FootConstraintTransitionReason.FootSeparationReleased, false);
                    right = right.UseGrounding(poseRoot);
                    rightMovable = true;
                }
                leftWorld = poseRoot.TransformPoint(left.Goal.ComponentPosition);
                rightWorld = poseRoot.TransformPoint(right.Goal.ComponentPosition);
                delta = Vector3.ProjectOnPlane(rightWorld - leftWorld, poseRoot.up);
                distance = delta.magnitude;
                if (distance >= minimum)
                    return;
            }

            Vector3 direction = distance > 0.0001f
                ? delta / distance
                : Vector3.ProjectOnPlane(poseRoot.right, poseRoot.up).normalized;
            if (direction.sqrMagnitude <= 0.0001f)
                return;
            float correction = minimum - distance;
            if (leftMovable && rightMovable)
            {
                FootGoal leftCandidate = left;
                FootGoal rightCandidate = right;
                if (TryShiftFoot(
                        ref leftCandidate,
                        -direction * (correction * 0.5f),
                        poseRoot) &&
                    TryShiftFoot(
                        ref rightCandidate,
                        direction * (correction * 0.5f),
                        poseRoot))
                {
                    left = leftCandidate;
                    right = rightCandidate;
                    return;
                }
            }
            if (leftMovable && TryShiftFoot(ref left, -direction * correction, poseRoot))
                return;
            if (rightMovable)
                _ = TryShiftFoot(ref right, direction * correction, poseRoot);
        }

        bool TryShiftFoot(
            ref FootGoal foot,
            Vector3 worldOffset,
            Transform poseRoot)
        {
            Vector3 worldPosition = poseRoot.TransformPoint(foot.Goal.ComponentPosition) + worldOffset;
            float extensionRatio = Vector3.Distance(
                foot.PlannedHipWorldPosition,
                worldPosition) / foot.LegLength;
            CharacterPredictiveFootPlacementRuntimeSettings settings = m_Settings.Predictive;
            if (!float.IsFinite(extensionRatio) ||
                extensionRatio < settings.MinimumLegExtensionRatio ||
                extensionRatio > settings.MaximumLegExtensionRatio)
                return false;
            CharacterFullBodyIkGoal source = foot.Goal;
            Vector3 componentPosition = ToComponentPoint(poseRoot, worldPosition);
            Vector3 componentPlantPivot = source.PlantPivotMode == CharacterFullBodyIkPlantPivotMode.Toe
                ? source.ComponentPlantPivot + componentPosition - source.ComponentPosition
                : Vector3.zero;
            var goal = new CharacterFullBodyIkGoal(
                source.Slot,
                componentPosition,
                source.ComponentRotation,
                source.PositionWeight,
                source.RotationWeight,
                source.Application,
                source.SourceKind,
                source.PlantPivotMode,
                componentPlantPivot,
                source.PlantPivotWeight,
                source.DiagnosticMetadataIndex);
            foot = foot.WithGoal(
                goal,
                extensionRatio,
                foot.SeparationCorrection + worldOffset.magnitude);
            return true;
        }

        static void ResolveAnchoredTargetForLock(
            CharacterFootPlantLockType lockType,
            CharacterFootPlacementAnimatedFootPose pose,
            Vector3 anchoredAnklePosition,
            Quaternion anchoredRotation,
            Vector3 anchoredPlantPosition,
            Quaternion groundingRotation,
            out Vector3 targetPosition,
            out Quaternion targetRotation,
            out Vector3 targetPlantPosition,
            out bool hasPlantPivot)
        {
            targetPlantPosition = anchoredPlantPosition;
            switch (lockType)
            {
                case CharacterFootPlantLockType.PivotAroundToe:
                    targetRotation = groundingRotation;
                    targetPosition = ResolveToePivotAnklePositionForLock(
                        pose,
                        targetRotation,
                        anchoredPlantPosition);
                    hasPlantPivot = true;
                    return;
                case CharacterFootPlantLockType.PivotAroundAnkle:
                    targetPosition = anchoredAnklePosition;
                    targetRotation = groundingRotation;
                    hasPlantPivot = false;
                    return;
                case CharacterFootPlantLockType.LockRotation:
                    targetPosition = anchoredAnklePosition;
                    targetRotation = anchoredRotation;
                    hasPlantPivot = false;
                    return;
                default:
                    throw new InvalidOperationException("Unlocked Foot Placement cannot resolve a planted anchor.");
            }
        }

        static Vector3 ResolveToePivotAnklePositionForLock(
            CharacterFootPlacementAnimatedFootPose pose,
            Quaternion targetRotation,
            Vector3 targetPlantPosition)
        {
            Vector3 ankleToToe = Quaternion.Inverse(pose.AnkleRotation) *
                                 (pose.ToePosition - pose.AnklePosition);
            return targetPlantPosition - targetRotation * ankleToToe;
        }

        static Vector3 ResolveToePosition(
            CharacterFootPlacementAnimatedFootPose pose,
            Vector3 anklePosition,
            Quaternion ankleRotation)
        {
            Vector3 ankleToToe = Quaternion.Inverse(pose.AnkleRotation) *
                                 (pose.ToePosition - pose.AnklePosition);
            return anklePosition + ankleRotation * ankleToToe;
        }

        static float ResolveAnkleTwistDegrees(
            CharacterFootPlacementAnimatedFootPose pose,
            Quaternion anchorWorldRotation,
            Quaternion groundingWorldRotation,
            Vector3 supportNormal)
        {
            if (supportNormal.sqrMagnitude <= 0.0001f)
                return 0f;
            Vector3 normal = supportNormal.normalized;
            Vector3 anchorForward = Vector3.ProjectOnPlane(
                anchorWorldRotation * Quaternion.Inverse(pose.AnkleRotation) * pose.SoleForward,
                normal);
            Vector3 groundingForward = Vector3.ProjectOnPlane(
                groundingWorldRotation * Quaternion.Inverse(pose.AnkleRotation) * pose.SoleForward,
                normal);
            if (anchorForward.sqrMagnitude <= 0.0001f ||
                groundingForward.sqrMagnitude <= 0.0001f)
                return 0f;
            return Mathf.Abs(Vector3.SignedAngle(
                groundingForward.normalized,
                anchorForward.normalized,
                normal));
        }

        static Vector3 ResolveContactVelocity(
            Vector3 localSoleVelocity,
            Vector3 currentSole,
            CharacterBodyPresentationFrame body,
            Transform poseRoot)
        {
            Vector3 angularVelocity = Vector3.up *
                                      (body.VisibleYawVelocityDegreesPerSecond * Mathf.Deg2Rad);
            Vector3 bodyPointVelocity = body.VisibleVelocity +
                                        Vector3.Cross(
                                            angularVelocity,
                                            currentSole - poseRoot.position);
            return poseRoot.rotation * localSoleVelocity + bodyPointVelocity;
        }

        static float ResolveContactWeight(
            float plantConfidence,
            float policyWeight,
            float planarSpeed,
            float verticalSpeed,
            float surfaceDistance,
            bool hasSurface,
            CharacterPredictiveFootPlacementRuntimeSettings settings)
        {
            if (!hasSurface || policyWeight < settings.MinimumSourceContribution)
                return 0f;
            float planarWeight = 1f - Mathf.InverseLerp(
                settings.PlantPlanarSpeed,
                settings.ReleasePlanarSpeed,
                planarSpeed);
            float verticalWeight = 1f - Mathf.InverseLerp(
                settings.PlantVerticalSpeed,
                settings.ReleaseVerticalSpeed,
                verticalSpeed);
            float distanceWeight = 1f - Mathf.InverseLerp(
                settings.PlantDistance,
                settings.ReleaseDistance,
                surfaceDistance);
            return Mathf.Clamp01(
                policyWeight * plantConfidence *
                Mathf.Min(planarWeight, verticalWeight, distanceWeight));
        }

        static Vector3 ToComponentPoint(Transform poseRoot, Vector3 worldPoint) =>
            Quaternion.Inverse(poseRoot.rotation) * (worldPoint - poseRoot.position);

        CharacterFootPlacementPelvisLegInput BuildPelvisLegInput(
            CharacterFootSide side,
            CharacterFootPlacementAnimatedFootPose pose,
            in FootGoal goal,
            float legLength)
        {
            return new CharacterFootPlacementPelvisLegInput(
                side,
                pose.HipPosition,
                pose.AnklePosition,
                m_Rig.PoseRoot.TransformPoint(goal.Goal.ComponentPosition),
                goal.Goal.PositionWeight,
                goal.PlantWeight,
                goal.ContactWeight,
                legLength,
                goal.CurrentSupport);
        }

        static FootPlacementSurface BuildSupport(GroundingQueryHit hit) =>
            hit.HasHit && hit.PhysicsHit.collider
                ? new FootPlacementSurface(hit.PhysicsHit.collider, hit.Point, hit.Normal.normalized)
                : default;

        CharacterPredictiveFootDiagnostics BuildFootDiagnostics(
            FootState state,
            CharacterGroundingQueryDiagnostics heelRequest,
            CharacterGroundingQueryDiagnostics toeRequest,
            CharacterGroundingQueryDiagnostics footCenterRequest,
            in CharacterFinalIkGroundingFootResult grounding,
            AnimationFootFeatureSample feature,
            in FootGoal resolved) =>
            new CharacterPredictiveFootDiagnostics(
                state.Side,
                heelRequest,
                toeRequest,
                footCenterRequest,
                in grounding,
                feature,
                resolved.CurrentSupport,
                resolved.Predictive.FutureLandingSupport,
                resolved.Predictive.GroundEnvelope,
                state.ConstraintState,
                state.TransitionReason,
                state.HasAnchor ? state.LockedLocalPosition : Vector3.zero,
                state.HasAnchor ? state.LockedLocalPlantPosition : Vector3.zero,
                state.HasAnchor ? state.LockedLocalRotation : Quaternion.identity,
                m_Settings.Predictive.LockType,
                m_Settings.Predictive.AdjustHeelBeforePlanting,
                resolved.Prediction.RejectReason,
                resolved.Prediction.Horizon,
                resolved.Prediction.HorizonClamped,
                grounding.Velocity * m_Settings.Grounding.VelocityPrediction,
                resolved.SoleVelocity,
                resolved.ExtensionRatio,
                resolved.AnkleTwistDegrees,
                resolved.SeparationCorrection,
                resolved.PlacementWeight,
                resolved.PlantWeight,
                resolved.ContactWeight,
                resolved.Predictive.SwingClearance,
                resolved.Predictive.QueryCount,
                resolved.Predictive.RejectedCount,
                resolved.Goal);

        static CharacterGroundingQueryDiagnostics QueryDiagnostics(
            bool available,
            in GroundingQueryRequest request) =>
            available ? new CharacterGroundingQueryDiagnostics(in request) : default;

        void ResetInternal(ulong resetSequence, FootConstraintTransitionReason reason)
        {
            m_Grounding.Reset();
            m_Pelvis.Reset();
            m_Left.Reset(reason);
            m_Right.Reset(reason);
            m_ResetSequence = resetSequence;
            m_LastRenderFrame = 0;
            m_GroundingTime = 0f;
            m_Diagnostics = default;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterFootPlacementPlanner));
        }

        static FootConstraintTransitionReason ToTransitionReason(CharacterFootPlacementResetReason reason) => reason switch
        {
            CharacterFootPlacementResetReason.BodyStreamReset => FootConstraintTransitionReason.BodyReset,
            CharacterFootPlacementResetReason.MissingAnimationOutput => FootConstraintTransitionReason.MissingAnimationOutput,
            CharacterFootPlacementResetReason.InvalidPose => FootConstraintTransitionReason.InvalidPose,
            _ => FootConstraintTransitionReason.PresentationReset
        };

        readonly struct FootGoal
        {
            internal FootGoal(
                CharacterFullBodyIkGoal goal,
                CharacterFullBodyIkGoal groundingGoal,
                Vector3 plannedHipWorldPosition,
                float legLength,
                float extensionRatio,
                float ankleTwistDegrees,
                float separationCorrection,
                float placementWeight,
                float plantWeight,
                float contactWeight,
                CharacterPredictiveFootPlacementQueryResult predictive,
                Vector3 soleVelocity,
                PredictedFootprint prediction,
                FootPlacementSurface currentSupport)
            {
                Goal = goal;
                GroundingGoal = groundingGoal;
                PlannedHipWorldPosition = plannedHipWorldPosition;
                LegLength = legLength;
                ExtensionRatio = extensionRatio;
                AnkleTwistDegrees = ankleTwistDegrees;
                SeparationCorrection = separationCorrection;
                PlacementWeight = placementWeight;
                PlantWeight = plantWeight;
                ContactWeight = contactWeight;
                Predictive = predictive;
                SoleVelocity = soleVelocity;
                Prediction = prediction;
                CurrentSupport = currentSupport;
            }

            internal CharacterFullBodyIkGoal Goal { get; }
            internal CharacterFullBodyIkGoal GroundingGoal { get; }
            internal Vector3 PlannedHipWorldPosition { get; }
            internal float LegLength { get; }
            internal float ExtensionRatio { get; }
            internal float AnkleTwistDegrees { get; }
            internal float SeparationCorrection { get; }
            internal float PlacementWeight { get; }
            internal float PlantWeight { get; }
            internal float ContactWeight { get; }
            internal CharacterPredictiveFootPlacementQueryResult Predictive { get; }
            internal Vector3 SoleVelocity { get; }
            internal PredictedFootprint Prediction { get; }
            internal FootPlacementSurface CurrentSupport { get; }

            internal FootGoal UseGrounding(Transform poseRoot)
            {
                Vector3 worldPosition = poseRoot.TransformPoint(GroundingGoal.ComponentPosition);
                float extensionRatio = Vector3.Distance(
                    PlannedHipWorldPosition,
                    worldPosition) / LegLength;
                return WithGoal(GroundingGoal, extensionRatio, SeparationCorrection);
            }

            internal FootGoal WithGoal(
                CharacterFullBodyIkGoal goal,
                float extensionRatio,
                float separationCorrection) =>
                new FootGoal(
                    goal,
                    GroundingGoal,
                    PlannedHipWorldPosition,
                    LegLength,
                    extensionRatio,
                    AnkleTwistDegrees,
                    separationCorrection,
                    PlacementWeight,
                    PlantWeight,
                    ContactWeight,
                    Predictive,
                    SoleVelocity,
                    Prediction,
                    CurrentSupport);

            internal FootGoal ReleaseForPelvisConflict()
            {
                CharacterFullBodyIkGoal source = Goal;
                var goal = new CharacterFullBodyIkGoal(
                    source.Slot,
                    source.ComponentPosition,
                    source.ComponentRotation,
                    0f,
                    0f,
                    source.Application,
                    source.SourceKind,
                    CharacterFullBodyIkPlantPivotMode.None,
                    Vector3.zero,
                    0f,
                    source.DiagnosticMetadataIndex);
                return new FootGoal(
                    goal,
                    GroundingGoal,
                    PlannedHipWorldPosition,
                    LegLength,
                    ExtensionRatio,
                    AnkleTwistDegrees,
                    SeparationCorrection,
                    0f,
                    0f,
                    0f,
                    Predictive,
                    SoleVelocity,
                    Prediction,
                    CurrentSupport);
            }

            internal FootGoal ApplyPelvis(
                CharacterFootPlacementAnimatedFootPose pose,
                Vector3 componentTranslation,
                Transform poseRoot)
            {
                Vector3 worldTranslation = poseRoot.rotation * componentTranslation;
                Vector3 plannedHip = pose.HipPosition + worldTranslation;
                Vector3 movedAnkle = pose.AnklePosition + worldTranslation;
                Vector3 target = poseRoot.TransformPoint(Goal.ComponentPosition);
                Vector3 effectiveAnkle = Vector3.Lerp(movedAnkle, target, Goal.PositionWeight);
                float extensionRatio = Vector3.Distance(plannedHip, effectiveAnkle) / LegLength;
                return new FootGoal(
                    Goal,
                    GroundingGoal,
                    plannedHip,
                    LegLength,
                    extensionRatio,
                    AnkleTwistDegrees,
                    SeparationCorrection,
                    PlacementWeight,
                    PlantWeight,
                    ContactWeight,
                    Predictive,
                    SoleVelocity,
                    Prediction,
                    CurrentSupport);
            }
        }
    }
}
