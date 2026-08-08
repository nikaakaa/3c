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
            CharacterFullBodyIkGoal rightFoot,
            CharacterFootGroundingDiagnostics diagnostics)
        {
            Pelvis = pelvis;
            LeftFoot = leftFoot;
            RightFoot = rightFoot;
            Diagnostics = diagnostics;
            if (!pelvis.IsValid || !leftFoot.IsValid || !rightFoot.IsValid || !diagnostics.IsCompleted)
                throw new ArgumentException("Foot Grounding plan is invalid.");
        }

        internal CharacterFullBodyIkGoal Pelvis { get; }
        internal CharacterFullBodyIkGoal LeftFoot { get; }
        internal CharacterFullBodyIkGoal RightFoot { get; }
        internal CharacterFootGroundingDiagnostics Diagnostics { get; }

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
            PhysicsScene physicsScene)
        {
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            m_Planner = new CharacterFootGroundingPlanner(actorId, settings, rig, physicsScene);
        }

        internal CharacterFootGroundingDiagnostics Diagnostics => m_Planner.Diagnostics;
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
            CharacterFootGroundingPlan plan = m_Planner.Plan(in frame, weightParameterIndex);
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

        internal void RetargetBodyBranch(ulong resetSequence) =>
            m_Planner.RetargetBodyBranch(resetSequence);

        public void Dispose() => m_Planner.Dispose();
    }

    internal sealed class CharacterFootGroundingPlanner : IDisposable
    {
        sealed class FootState
        {
            internal FootState(CharacterFootSide side) => Side = side;

            internal CharacterFootSide Side { get; }
            internal bool PlantContact;
            internal FootConstraintTransitionReason TransitionReason;
            internal FootPlacementSurface AnchorSurface;
            internal Vector3 AnchorLocalPosition;
            internal Quaternion AnchorLocalRotation = Quaternion.identity;
            internal float AnchorBlendWeight;
            internal bool HasAnchor;
            int m_PreviousSoleSurfaceIdentity;
            Vector3 m_PreviousSoleHeelPosition;
            Vector3 m_PreviousSoleToePosition;
            bool m_HasPreviousSoleSample;

            internal CharacterFootContactState ContactState =>
                HasAnchor || AnchorBlendWeight > 0.0001f
                    ? CharacterFootContactState.Anchored
                    : PlantContact
                        ? CharacterFootContactState.Contact
                        : CharacterFootContactState.Swing;

            internal void UpdateContact(
                AnimationFootFeatureSample feature,
                float surfaceDistance,
                bool surfaceValid,
                CharacterStanceStabilizationSettings settings)
            {
                float speed = feature.SoleLocalVelocity.magnitude;
                if (!surfaceValid ||
                    surfaceDistance > settings.MaximumContactSurfaceDistance ||
                    speed >= settings.UnalignmentSpeedThreshold)
                {
                    if (PlantContact)
                        TransitionReason = FootConstraintTransitionReason.ContactReleased;
                    PlantContact = false;
                    return;
                }
                if (PlantContact)
                {
                    if (feature.PlantConfidence <= settings.PlantConfidenceExit)
                    {
                        PlantContact = false;
                        TransitionReason = FootConstraintTransitionReason.ContactReleased;
                    }
                    return;
                }
                if (speed <= settings.PlantSpeedThreshold &&
                    feature.PlantConfidence >= settings.PlantConfidenceEnter)
                {
                    PlantContact = true;
                    TransitionReason = FootConstraintTransitionReason.ContactEntered;
                }
            }

            internal SoleContinuityPlan ResolveSoleContinuity(
                FootPlacementSurface support,
                in SoleClearancePlan clearance)
            {
                if (!m_HasPreviousSoleSample || !support.IsValid)
                {
                    return new SoleContinuityPlan(
                        m_HasPreviousSoleSample,
                        m_PreviousSoleSurfaceIdentity,
                        0f,
                        0f,
                        false);
                }
                Vector3 normal = support.Normal.normalized;
                float previousHeelDistance = Vector3.Dot(
                    m_PreviousSoleHeelPosition - support.Point,
                    normal);
                float previousToeDistance = Vector3.Dot(
                    m_PreviousSoleToePosition - support.Point,
                    normal);
                bool crossedCurrentSurface =
                    support.Identity == m_PreviousSoleSurfaceIdentity &&
                    clearance.Penetration > 0f &&
                    Mathf.Min(previousHeelDistance, previousToeDistance) >= -0.0001f;
                return new SoleContinuityPlan(
                    true,
                    m_PreviousSoleSurfaceIdentity,
                    previousHeelDistance,
                    previousToeDistance,
                    crossedCurrentSurface);
            }

            internal void CommitSoleSample(
                FootPlacementSurface support,
                in SoleClearancePlan clearance,
                Vector3 constraintTranslation)
            {
                if (!support.IsValid)
                {
                    ClearSoleSample();
                    return;
                }
                m_PreviousSoleSurfaceIdentity = support.Identity;
                m_PreviousSoleHeelPosition = clearance.Contacts.HeelPosition + constraintTranslation;
                m_PreviousSoleToePosition = clearance.Contacts.ToePosition + constraintTranslation;
                m_HasPreviousSoleSample = true;
            }

            internal void Capture(
                FootPlacementSurface surface,
                Vector3 worldPosition,
                Quaternion worldRotation)
            {
                AnchorSurface = surface;
                AnchorLocalPosition = surface.Transform.InverseTransformPoint(worldPosition);
                AnchorLocalRotation =
                    (Quaternion.Inverse(surface.Transform.rotation) * worldRotation).normalized;
                HasAnchor = true;
                TransitionReason = FootConstraintTransitionReason.AnchorCaptured;
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

            internal void Release(FootConstraintTransitionReason reason)
            {
                PlantContact = false;
                TransitionReason = reason;
            }

            internal void ClearAnchor()
            {
                AnchorSurface = default;
                AnchorLocalPosition = Vector3.zero;
                AnchorLocalRotation = Quaternion.identity;
                HasAnchor = false;
                AnchorBlendWeight = 0f;
            }

            internal void Reset(FootConstraintTransitionReason reason)
            {
                PlantContact = false;
                TransitionReason = reason;
                ClearAnchor();
                ClearSoleSample();
            }

            void ClearSoleSample()
            {
                m_PreviousSoleSurfaceIdentity = 0;
                m_PreviousSoleHeelPosition = Vector3.zero;
                m_PreviousSoleToePosition = Vector3.zero;
                m_HasPreviousSoleSample = false;
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
        readonly CharacterFootPlacementPelvisPlanner m_Pelvis = new CharacterFootPlacementPelvisPlanner();
        readonly FootState m_Left = new FootState(CharacterFootSide.Left);
        readonly FootState m_Right = new FootState(CharacterFootSide.Right);
        ulong m_LastRenderFrame;
        ulong m_ResetSequence;
        Vector3 m_PreviousPoseRootPosition;
        bool m_HasPreviousPoseRootPosition;
        CharacterFootGroundingDiagnostics m_Diagnostics;
        bool m_Disposed;

        internal CharacterFootGroundingPlanner(
            ActorId actorId,
            CharacterFootPlacementRuntimeSettings settings,
            CharacterFootPlacementPoseRig rig,
            PhysicsScene physicsScene)
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
            ResetInternal(0, FootConstraintTransitionReason.PresentationReset);
        }

        internal CharacterFootGroundingDiagnostics Diagnostics => m_Diagnostics;
        internal CharacterFootPlacementRuntimeSettings Settings => m_Settings;

        internal void ApplyTuning(
            CharacterLyraCurrentGroundingSettings currentGrounding,
            CharacterStanceStabilizationSettings stanceStabilization,
            CharacterPredictiveFootPlacementRuntimeSettings predictiveExtension,
            bool resetOwnerState)
        {
            m_Settings.ApplyTuning(currentGrounding, stanceStabilization, predictiveExtension);
            m_CurrentGrounding.ApplyTuning(currentGrounding);
            if (resetOwnerState)
                ResetInternal(m_ResetSequence, FootConstraintTransitionReason.PresentationReset);
        }

        internal CharacterFootGroundingPlan Plan(
            in CharacterFootPlacementPlanningFrame frame,
            int weightParameterIndex)
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
                trace.PelvisTargetOffset,
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
                frame.PresentationDeltaSeconds);
            rightPrepared = UpdateStance(
                m_Right,
                pose.Right,
                trace.Right,
                lyra.Right,
                rightPrepared,
                features.Right,
                frame.PresentationDeltaSeconds);
            SoleContinuityPlan leftContinuity = m_Left.ResolveSoleContinuity(
                leftPrepared.Surface,
                leftPrepared.CurrentClearance);
            SoleContinuityPlan rightContinuity = m_Right.ResolveSoleContinuity(
                rightPrepared.Surface,
                rightPrepared.CurrentClearance);
            float leftSoleConstraintOffset = m_Left.PlantContact || leftContinuity.CrossedCurrentSurface
                ? ResolveCurrentSoleConstraintOffset(
                    leftPrepared.CurrentClearance,
                    m_Rig.PoseRoot.up)
                : 0f;
            float rightSoleConstraintOffset = m_Right.PlantContact || rightContinuity.CrossedCurrentSurface
                ? ResolveCurrentSoleConstraintOffset(
                    rightPrepared.CurrentClearance,
                    m_Rig.PoseRoot.up)
                : 0f;
            lyra = m_CurrentGrounding.ApplySoleConstraints(
                lyra,
                leftSoleConstraintOffset,
                rightSoleConstraintOffset);
            Vector3 componentUp = m_Rig.PoseRoot.up.normalized;
            m_Left.CommitSoleSample(
                leftPrepared.Surface,
                leftPrepared.CurrentClearance,
                componentUp * leftSoleConstraintOffset);
            m_Right.CommitSoleSample(
                rightPrepared.Surface,
                rightPrepared.CurrentClearance,
                componentUp * rightSoleConstraintOffset);
            ResolvedFoot left = StabilizeFoot(
                m_Left,
                pose.Left,
                lyra.Left,
                leftPrepared,
                features.Left,
                features.Value,
                m_Rig.LeftLegLength);
            ResolvedFoot right = StabilizeFoot(
                m_Right,
                pose.Right,
                lyra.Right,
                rightPrepared,
                features.Right,
                features.Value,
                m_Rig.RightLegLength);
            CharacterFootPlacementPelvisPlan pelvisPlan;
            try
            {
                pelvisPlan = m_Pelvis.Plan(
                    trace.PelvisTargetOffset,
                    lyra.CurrentPelvisOffset,
                    BuildPelvisInput(CharacterFootSide.Left, pose.Left, left, features.Value, m_Rig.LeftLegLength),
                    BuildPelvisInput(CharacterFootSide.Right, pose.Right, right, features.Value, m_Rig.RightLegLength),
                    m_Rig.PoseRoot.up,
                    m_Settings.StanceStabilization);
            }
            catch (CharacterFootPlacementPelvisReachException exception)
            {
                throw new InvalidOperationException(
                    FormattableString.Invariant(
                        $"Foot Grounding render frame {frame.RenderFrame} pelvis reach failure. {exception.Message}"),
                    exception);
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
                in lyra,
                pelvis,
                m_Settings,
                m_Rig,
                m_World.PhysicsScene.GetHashCode(),
                m_Rig.SelfColliderRoot.GetInstanceID(),
                BuildDiagnostics(m_Left, pose.Left, lyra.Left, features.Left, leftContinuity, left),
                BuildDiagnostics(m_Right, pose.Right, lyra.Right, features.Right, rightContinuity, right));
            m_LastRenderFrame = frame.RenderFrame;
            m_ResetSequence = frame.Body.ResetSequence;
            m_PreviousPoseRootPosition = m_Rig.PoseRoot.position;
            m_HasPreviousPoseRootPosition = true;
            return new CharacterFootGroundingPlan(pelvis, left.Goal, right.Goal, m_Diagnostics);
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
            float deltaSeconds)
        {
            CharacterStanceStabilizationSettings settings = m_Settings.StanceStabilization;
            Transform root = m_Rig.PoseRoot;
            Vector3 up = root.up.normalized;
            SoleClearancePlan currentClearance = MeasureSoleClearance(
                animated,
                root.TransformPoint(lyra.ComponentPosition),
                (root.rotation * lyra.ComponentRotation).normalized,
                prepared.Surface,
                root.up);
            float surfaceDistance = prepared.SurfaceValid
                ? Mathf.Max(
                    Mathf.Abs(currentClearance.HeelPlaneDistance),
                    Mathf.Abs(currentClearance.ToePlaneDistance))
                : float.PositiveInfinity;
            state.UpdateContact(feature, surfaceDistance, prepared.SurfaceValid, settings);
            Vector3 targetWorldPosition = animated.AnklePosition +
                                          up * (trace.TargetOffset + lyra.SoleClearanceTarget);
            bool hasResolvedAnchor = state.TryResolve(
                m_Settings.CurrentGrounding.GroundLayerMask,
                root.up,
                settings.MaximumSurfaceSlopeDegrees,
                out Vector3 anchorWorldPosition,
                out _,
                out _);
            if (state.HasAnchor && !hasResolvedAnchor)
            {
                state.Release(FootConstraintTransitionReason.SurfaceInvalid);
                state.ClearAnchor();
            }
            if (state.PlantContact && hasResolvedAnchor &&
                Vector3.Distance(anchorWorldPosition, targetWorldPosition) > settings.MaximumAnchorDistance)
            {
                state.Release(FootConstraintTransitionReason.AnchorDistanceExceeded);
            }
            float targetBlend = state.PlantContact && hasResolvedAnchor ? 1f : 0f;
            state.AnchorBlendWeight = Mathf.MoveTowards(
                state.AnchorBlendWeight,
                targetBlend,
                settings.AnchorBlendSpeed * deltaSeconds);
            if (state.AnchorBlendWeight <= 0.0001f && !state.PlantContact)
            {
                state.ClearAnchor();
                hasResolvedAnchor = false;
            }
            return new PreparedFoot(
                prepared.Surface,
                prepared.SurfaceValid,
                surfaceDistance,
                currentClearance);
        }

        ResolvedFoot StabilizeFoot(
            FootState state,
            CharacterFootPlacementAnimatedFootPose animated,
            CharacterLyraCurrentGroundingFootResult lyra,
            PreparedFoot prepared,
            AnimationFootFeatureSample feature,
            float alpha,
            float legLength)
        {
            CharacterStanceStabilizationSettings settings = m_Settings.StanceStabilization;
            Transform root = m_Rig.PoseRoot;
            Vector3 lyraWorldPosition = root.TransformPoint(lyra.ComponentPosition);
            Quaternion lyraWorldRotation = (root.rotation * lyra.ComponentRotation).normalized;
            if (state.PlantContact && !state.HasAnchor && prepared.SurfaceValid)
            {
                SoleClearancePlan captureClearance = MeasureSoleClearance(
                    animated,
                    lyraWorldPosition,
                    lyraWorldRotation,
                    prepared.Surface,
                    root.up);
                state.Capture(
                    prepared.Surface,
                    captureClearance.SafeAnklePosition,
                    lyraWorldRotation);
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
                state.Release(FootConstraintTransitionReason.SurfaceInvalid);
                state.ClearAnchor();
            }
            Vector3 finalWorldPosition = hasResolvedAnchor
                ? Vector3.Lerp(lyraWorldPosition, anchorWorldPosition, state.AnchorBlendWeight)
                : lyraWorldPosition;
            Quaternion finalWorldRotation = hasResolvedAnchor
                ? Quaternion.Slerp(lyraWorldRotation, anchorWorldRotation, state.AnchorBlendWeight).normalized
                : lyraWorldRotation;
            SoleClearancePlan soleClearance = MeasureSoleClearance(
                animated,
                finalWorldPosition,
                finalWorldRotation,
                state.PlantContact && hasResolvedAnchor ? anchorSurface : prepared.Surface,
                root.up);
            if (hasResolvedAnchor && alpha > 0.0001f)
            {
                var reachInput = new CharacterFootPlacementPelvisLegInput(
                    state.Side,
                    animated.HipPosition,
                    finalWorldPosition,
                    Mathf.Clamp01(alpha),
                    legLength);
                if (!m_Pelvis.HasReachableOffset(
                        in reachInput,
                        root.up,
                        settings))
                {
                    state.Release(FootConstraintTransitionReason.LegUnreachable);
                    state.ClearAnchor();
                    hasResolvedAnchor = false;
                    finalWorldPosition = lyraWorldPosition;
                    finalWorldRotation = lyraWorldRotation;
                    soleClearance = MeasureSoleClearance(
                        animated,
                        finalWorldPosition,
                        finalWorldRotation,
                        prepared.Surface,
                        root.up);
                }
            }
            float placementWeight = Mathf.Clamp01(alpha);
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
            SoleContinuityPlan continuity,
            ResolvedFoot resolved) =>
            new CharacterFootGroundingFootDiagnostics(
                state.Side,
                lyra,
                feature,
                state.ContactState,
                state.TransitionReason,
                resolved.Surface,
                resolved.SurfaceLocalAnchor,
                resolved.SurfaceLocalRotation,
                resolved.AnchorWorldPosition,
                resolved.AnchorWorldRotation,
                state.HasAnchor,
                state.AnchorBlendWeight,
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
                m_Rig.PoseRoot.up.normalized * lyra.SoleClearanceTarget,
                m_Rig.PoseRoot.InverseTransformPoint(animated.AnklePosition).y,
                continuity.HasPreviousSample,
                continuity.PreviousSurfaceIdentity,
                continuity.PreviousHeelPlaneDistance,
                continuity.PreviousToePlaneDistance,
                continuity.CrossedCurrentSurface,
                resolved.BaselineComponentPosition,
                resolved.BaselineComponentRotation,
                resolved.Goal);

        CharacterFootPlacementPelvisLegInput BuildPelvisInput(
            CharacterFootSide side,
            CharacterFootPlacementAnimatedFootPose pose,
            ResolvedFoot resolved,
            float alpha,
            float legLength)
        {
            float goalWeight = Mathf.Clamp01(alpha);
            return new CharacterFootPlacementPelvisLegInput(
                side,
                pose.HipPosition,
                m_Rig.PoseRoot.TransformPoint(resolved.BaselineComponentPosition),
                goalWeight,
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
            m_Left.Reset(reason);
            m_Right.Reset(reason);
            m_ResetSequence = resetSequence;
            m_LastRenderFrame = 0;
            m_PreviousPoseRootPosition = Vector3.zero;
            m_HasPreviousPoseRootPosition = false;
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

        static float ResolveCurrentSoleConstraintOffset(
            in SoleClearancePlan clearance,
            Vector3 componentUp)
        {
            return Vector3.Dot(
                clearance.SafeAnklePosition - clearance.AnklePosition,
                componentUp.normalized);
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
                SoleClearancePlan currentClearance)
            {
                Surface = surface;
                SurfaceValid = surfaceValid;
                SurfaceDistance = surfaceDistance;
                CurrentClearance = currentClearance;
            }

            internal FootPlacementSurface Surface { get; }
            internal bool SurfaceValid { get; }
            internal float SurfaceDistance { get; }
            internal SoleClearancePlan CurrentClearance { get; }
        }

        readonly struct SoleContinuityPlan
        {
            internal SoleContinuityPlan(
                bool hasPreviousSample,
                int previousSurfaceIdentity,
                float previousHeelPlaneDistance,
                float previousToePlaneDistance,
                bool crossedCurrentSurface)
            {
                HasPreviousSample = hasPreviousSample;
                PreviousSurfaceIdentity = previousSurfaceIdentity;
                PreviousHeelPlaneDistance = previousHeelPlaneDistance;
                PreviousToePlaneDistance = previousToePlaneDistance;
                CrossedCurrentSurface = crossedCurrentSurface;
            }

            internal bool HasPreviousSample { get; }
            internal int PreviousSurfaceIdentity { get; }
            internal float PreviousHeelPlaneDistance { get; }
            internal float PreviousToePlaneDistance { get; }
            internal bool CrossedCurrentSurface { get; }
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
