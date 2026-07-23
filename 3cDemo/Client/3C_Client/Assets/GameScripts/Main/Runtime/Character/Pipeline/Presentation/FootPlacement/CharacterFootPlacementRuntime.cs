using System;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using Unity.Profiling;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal readonly struct PredictedFootprint
    {
        public PredictedFootprint(
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

        public Vector3 Position { get; }
        public float Horizon { get; }
        public bool HorizonClamped { get; }
        public FootPredictionRejectReason RejectReason { get; }
        public bool IsAccepted => RejectReason == FootPredictionRejectReason.None;
    }

    internal sealed class CharacterFootPlacementRuntime : ICharacterPosePostProcessPass
    {
        const float HalfLifeLambda = 0.69314718056f;

        static readonly ProfilerMarker PlanMarker = new ProfilerMarker("FootPlacement.Plan");
        static readonly ProfilerMarker QueryMarker = new ProfilerMarker("FootPlacement.Query");
        static readonly ProfilerMarker SolveMarker = new ProfilerMarker("FootPlacement.Solve");

        readonly ActorId m_ActorId;
        readonly CharacterFootPlacementRuntimeSettings m_Settings;
        readonly CharacterFootPlacementRigBinding m_Rig;
        readonly ICharacterFootPlacementSolver m_Solver;
        readonly CharacterFootPlacementSupportQuery m_Query;
        readonly RuntimeDiagnosticsContext m_Diagnostics;
        readonly FootRuntimeState m_Left = new FootRuntimeState(CharacterFootSide.Left);
        readonly FootRuntimeState m_Right = new FootRuntimeState(CharacterFootSide.Right);
        readonly AnimationPoseSourceContribution[] m_DiagnosticContributions;

        CharacterFootPlacementFrameSnapshot m_Snapshot;
        ulong m_LastRenderFrame;
        ulong m_ResetSequence;
        float m_PelvisOffset;
        float m_PelvisReachOffset;
        float m_PelvisReachVelocity;
        float m_ActorMovementCompensationOffset;
        float m_ActorMovementCompensationVelocity;
        bool m_Disposed;

        public CharacterFootPlacementRuntime(
            ActorId actorId,
            CharacterFootPlacementRuntimeSettings settings,
            CharacterFootPlacementRigBinding rig,
            ICharacterFootPlacementSolver solver,
            PhysicsScene physicsScene,
            RuntimeDiagnosticsContext diagnostics)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Foot Placement Actor identity is invalid.", nameof(actorId));
            m_ActorId = actorId;
            m_Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            m_Solver = solver ?? throw new ArgumentNullException(nameof(solver));
            m_Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            m_Rig.RequireValid();
            m_Solver.RequireValid(m_Rig);
            m_Solver.Initialize(new CharacterFootPlacementSolverContext(actorId, rig));
            if (!m_Solver.IsInitialized)
                throw new InvalidOperationException($"Foot Placement solver failed to initialize for Actor '{actorId}'.");
            m_Query = new CharacterFootPlacementSupportQuery(physicsScene, rig, settings.Trace);
            m_DiagnosticContributions = new AnimationPoseSourceContribution[settings.ContributionCapacity];
            ResetInternal(0, 0, FootConstraintTransitionReason.PresentationReset, false);
        }

        public CharacterFootPlacementFrameSnapshot Snapshot => m_Snapshot;

        public void Present(CharacterPosePostProcessFrame frame)
        {
            RequireAlive();
            if (frame.ActorId != m_ActorId)
                throw new InvalidOperationException("Foot Placement frame targets another Actor.");
            if (frame.RenderFrame == m_LastRenderFrame)
                throw new InvalidOperationException($"Foot Placement Actor '{m_ActorId}' received render frame '{frame.RenderFrame}' twice.");
            if (!frame.Body.IsValid)
            {
                ResetInternal(
                    frame.RenderFrame,
                    frame.Body.ResetSequence,
                    FootConstraintTransitionReason.MissingAnimationOutput,
                    true);
                return;
            }

            ComposedAnimationPoseFrame animationPose = frame.AnimationPose;
            if (animationPose.Availability != AnimationPoseAvailability.Pose)
            {
                ResetInternal(
                    frame.RenderFrame,
                    frame.Body.ResetSequence,
                    animationPose.Availability == AnimationPoseAvailability.Invalid
                        ? FootConstraintTransitionReason.InvalidPose
                        : FootConstraintTransitionReason.MissingAnimationOutput,
                    true);
                return;
            }

            try
            {
                using (PlanMarker.Auto())
                {
                    if (frame.Body.ResetSequence != m_ResetSequence)
                    {
                        ResetInternal(
                            frame.RenderFrame,
                            frame.Body.ResetSequence,
                            FootConstraintTransitionReason.BodyReset,
                            true);
                    }
                    CharacterFootPlacementFeatureFrame features = ResolveAnimationFeatures(animationPose);
                    CharacterFootPlacementAnimatedPose pose = m_Solver.CaptureAnimatedPose(frame.RenderFrame);
                    float deltaSeconds = frame.PresentationDeltaSeconds;
                    FootKinematics leftKinematics = CaptureKinematics(m_Left, pose.Left, deltaSeconds);
                    FootKinematics rightKinematics = CaptureKinematics(m_Right, pose.Right, deltaSeconds);
                    if (HasIllegalDiscontinuity(leftKinematics) || HasIllegalDiscontinuity(rightKinematics))
                    {
                        ResetInternal(
                            frame.RenderFrame,
                            frame.Body.ResetSequence,
                            FootConstraintTransitionReason.InvalidPose,
                            true);
                        pose = m_Solver.CaptureAnimatedPose(frame.RenderFrame);
                        leftKinematics = CaptureKinematics(m_Left, pose.Left, deltaSeconds);
                        rightKinematics = CaptureKinematics(m_Right, pose.Right, deltaSeconds);
                    }

                    PredictedFootprint leftPrediction = Predict(
                        pose.Left,
                        features.Left,
                        frame.Body,
                        m_Rig.LeftLegLength,
                        features.Value);
                    PredictedFootprint rightPrediction = Predict(
                        pose.Right,
                        features.Right,
                        frame.Body,
                        m_Rig.RightLegLength,
                        features.Value);
                    FootPlacementSupportResult leftSupport;
                    FootPlacementSupportResult rightSupport;
                    using (QueryMarker.Auto())
                    {
                        leftSupport = m_Query.Query(pose.Left, leftPrediction.Position, m_Rig.LeftLegLength);
                        rightSupport = m_Query.Query(pose.Right, rightPrediction.Position, m_Rig.RightLegLength);
                    }
                    ContactDecision leftContact = ClassifyContact(pose.Left, features.Left, frame.Body, features, leftSupport);
                    ContactDecision rightContact = ClassifyContact(pose.Right, features.Right, frame.Body, features, rightSupport);
                    ResolvedFoot left = ResolveFoot(
                        m_Left,
                        pose.Left,
                        leftKinematics,
                        leftContact,
                        leftPrediction,
                        leftSupport,
                        features.Left,
                        frame.Body,
                        features,
                        m_Rig.LeftLegLength,
                        deltaSeconds);
                    ResolvedFoot right = ResolveFoot(
                        m_Right,
                        pose.Right,
                        rightKinematics,
                        rightContact,
                        rightPrediction,
                        rightSupport,
                        features.Right,
                        frame.Body,
                        features,
                        m_Rig.RightLegLength,
                        deltaSeconds);
                    ApplyFootSeparation(ref left, ref right);
                    PelvisResolution pelvis = ResolvePelvis(pose, left, right, frame.Body, deltaSeconds);
                    if (pelvis.UnreachableFoot == CharacterFootSide.Left)
                        left = MarkLegUnreachable(m_Left, left);
                    else if (pelvis.UnreachableFoot == CharacterFootSide.Right)
                        right = MarkLegUnreachable(m_Right, right);
                    var plan = new CharacterFootPlacementPlan(
                        m_ActorId,
                        frame.RenderFrame,
                        frame.Body.ResetSequence,
                        left.Plan,
                        right.Plan,
                        pelvis.CurrentOffset);
                    if (!plan.IsValid)
                        throw new InvalidOperationException("Foot Placement produced a non-finite plan.");
                    CharacterFootPlacementSolverResult solverResult;
                    using (SolveMarker.Auto())
                        solverResult = m_Solver.Apply(plan);
                    if (!solverResult.Applied)
                        throw new InvalidOperationException($"Foot Placement solver rejected frame '{frame.RenderFrame}': {solverResult.Detail}");
                    m_LastRenderFrame = frame.RenderFrame;
                    m_ResetSequence = frame.Body.ResetSequence;
                    BuildSnapshot(frame, left, right, pelvis, solverResult);
                    PublishDiagnostics();
                }
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"Foot Placement failed for Actor '{m_ActorId}' at render frame '{frame.RenderFrame}'.",
                    exception);
            }
        }

        public void Reset(CharacterPosePostProcessReset reset)
        {
            if (m_Disposed)
                return;
            ulong lastPresentedRenderFrame = m_LastRenderFrame;
            FootConstraintTransitionReason reason = reset.Reason == CharacterPosePostProcessResetReason.MissingAnimationOutput
                ? FootConstraintTransitionReason.MissingAnimationOutput
                : reset.Reason == CharacterPosePostProcessResetReason.InvalidPose
                    ? FootConstraintTransitionReason.InvalidPose
                    : reset.Reason == CharacterPosePostProcessResetReason.BodyStreamReset
                        ? FootConstraintTransitionReason.BodyReset
                        : FootConstraintTransitionReason.PresentationReset;
            ResetInternal(reset.RenderFrame, reset.ResetSequence, reason, true);
            m_LastRenderFrame = lastPresentedRenderFrame;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            ResetInternal(m_LastRenderFrame, m_ResetSequence, FootConstraintTransitionReason.PresentationReset, true);
            m_Solver.Dispose();
            m_Disposed = true;
        }

        CharacterFootPlacementFeatureFrame ResolveAnimationFeatures(ComposedAnimationPoseFrame animationPose)
        {
            if (!string.Equals(animationPose.PosePlanHash, m_Settings.PosePlanHash, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Foot Placement Pose Plan mismatch: expected '{m_Settings.PosePlanHash}', received '{animationPose.PosePlanHash}'.");
            if (!animationPose.HasFootFeatures ||
                !animationPose.LeftFootFeatures.IsValid ||
                !animationPose.RightFootFeatures.IsValid)
                throw new InvalidOperationException("Foot Placement final pose has no generated foot features.");
            int parameterIndex = m_Settings.FootPlacementWeightParameterIndex;
            if ((uint)parameterIndex >= (uint)animationPose.PoseParameters.Count)
                throw new InvalidOperationException(
                    $"Foot Placement Pose Parameter '{m_Settings.FootPlacementWeightParameterId}' is outside the final pose parameter buffer.");
            float weight = animationPose.PoseParameters[parameterIndex];
            if (!float.IsFinite(weight) || weight < 0f || weight > 1f)
                throw new InvalidOperationException(
                    $"Foot Placement Pose Parameter '{m_Settings.FootPlacementWeightParameterId}' must be normalized.");
            return new CharacterFootPlacementFeatureFrame(
                weight,
                animationPose.LeftFootFeatures,
                animationPose.RightFootFeatures);
        }

        FootKinematics CaptureKinematics(
            FootRuntimeState state,
            CharacterFootPlacementAnimatedFootPose pose,
            float deltaSeconds)
        {
            Vector3 ankleLocal = m_Rig.VisualRoot.InverseTransformPoint(pose.AnklePosition);
            Vector3 toeLocal = m_Rig.VisualRoot.InverseTransformPoint(pose.ToePosition);
            Vector3 soleLocal = m_Rig.VisualRoot.InverseTransformPoint((pose.HeelPosition + pose.ToePosition) * 0.5f);
            bool hasVelocity = state.HasPoseHistory && deltaSeconds > 0.000001f;
            Vector3 ankleLocalVelocity = hasVelocity
                ? (ankleLocal - state.PreviousAnkleLocal) / deltaSeconds
                : Vector3.zero;
            Vector3 toeLocalVelocity = hasVelocity
                ? (toeLocal - state.PreviousToeLocal) / deltaSeconds
                : Vector3.zero;
            Vector3 soleLocalVelocity = hasVelocity
                ? (soleLocal - state.PreviousSoleLocal) / deltaSeconds
                : Vector3.zero;
            state.PreviousAnkleLocal = ankleLocal;
            state.PreviousToeLocal = toeLocal;
            state.PreviousSoleLocal = soleLocal;
            state.HasPoseHistory = true;
            Vector3 ankleVelocity = m_Rig.VisualRoot.TransformDirection(ankleLocalVelocity);
            Vector3 toeVelocity = m_Rig.VisualRoot.TransformDirection(toeLocalVelocity);
            Vector3 soleVelocity = m_Rig.VisualRoot.TransformDirection(soleLocalVelocity);
            return new FootKinematics(
                ankleLocal,
                toeLocal,
                soleLocal,
                ankleLocalVelocity,
                toeLocalVelocity,
                soleLocalVelocity,
                ankleVelocity,
                toeVelocity,
                soleVelocity,
                hasVelocity,
                soleVelocity.y <= m_Settings.Contact.DescendingTolerance);
        }

        ContactDecision ClassifyContact(
            CharacterFootPlacementAnimatedFootPose pose,
            AnimationFootFeatureSample feature,
            CharacterBodyPresentationFrame body,
            CharacterFootPlacementFeatureFrame frame,
            FootPlacementSupportResult support)
        {
            Vector3 sole = (pose.HeelPosition + pose.ToePosition) * 0.5f;
            Vector3 angularVelocity = Vector3.up *
                                      (body.VisibleYawVelocityDegreesPerSecond * Mathf.Deg2Rad);
            Vector3 worldVelocity = m_Rig.VisualRoot.TransformDirection(feature.SoleLocalVelocity) +
                                    body.VisibleVelocity +
                                    Vector3.Cross(angularVelocity, sole - m_Rig.VisualRoot.position);
            float planar = new Vector2(worldVelocity.x, worldVelocity.z).magnitude;
            float vertical = Mathf.Abs(worldVelocity.y);
            bool canPlant = body.TargetGrounded &&
                            frame.Value >= m_Settings.Contact.MinimumPlacementWeight &&
                            support.HasSupport &&
                            support.SoleDistance <= m_Settings.Contact.PlantDistance &&
                            feature.PlantConfidence >= m_Settings.Contact.PlantConfidenceEnter &&
                            planar <= m_Settings.Contact.PlantPlanarSpeed &&
                            vertical <= m_Settings.Contact.PlantVerticalSpeed &&
                            worldVelocity.y <= m_Settings.Contact.DescendingTolerance;
            bool shouldRelease = !body.TargetGrounded ||
                                 frame.Value < m_Settings.Contact.MinimumPlacementWeight ||
                                 !support.HasSupport ||
                                 support.SoleDistance > m_Settings.Contact.ReleaseDistance ||
                                 feature.PlantConfidence <= m_Settings.Contact.PlantConfidenceExit ||
                                 planar > m_Settings.Contact.ReleasePlanarSpeed ||
                                 vertical > m_Settings.Contact.ReleaseVerticalSpeed;
            return new ContactDecision(canPlant, shouldRelease, worldVelocity);
        }

        PredictedFootprint Predict(
            CharacterFootPlacementAnimatedFootPose pose,
            AnimationFootFeatureSample feature,
            CharacterBodyPresentationFrame body,
            float legLength,
            float predictionWeight)
        {
            Vector3 currentSole = (pose.HeelPosition + pose.ToePosition) * 0.5f;
            if (predictionWeight <= 0f)
                return new PredictedFootprint(currentSole, 0f, false, FootPredictionRejectReason.NoSupportEstimate);
            if (feature.NextLandingConfidence <= 0.0001f)
                return new PredictedFootprint(currentSole, 0f, false, FootPredictionRejectReason.NoFutureLanding);
            float estimatedHorizon = feature.NextLandingDelaySeconds;
            float clampedHorizon = Mathf.Clamp(
                estimatedHorizon,
                m_Settings.Prediction.MinimumLookAheadSeconds,
                m_Settings.Prediction.MaximumLookAheadSeconds);
            bool horizonClamped = !Mathf.Approximately(estimatedHorizon, clampedHorizon);
            float horizon = clampedHorizon;
            if (Mathf.Abs(body.VisibleYawVelocityDegreesPerSecond) >
                m_Settings.Prediction.MaximumYawVelocityDegreesPerSecond)
                return new PredictedFootprint(currentSole, horizon, horizonClamped, FootPredictionRejectReason.AngularVelocityExceeded);

            Vector3 rootPosition = m_Rig.VisualRoot.position + body.VisibleVelocity * horizon;
            Quaternion rootRotation = m_Rig.VisualRoot.rotation *
                                      Quaternion.Euler(0f, body.VisibleYawVelocityDegreesPerSecond * horizon, 0f);
            Vector3 currentLocal = m_Rig.VisualRoot.InverseTransformPoint(currentSole);
            Vector3 predictedLocal = new Vector3(
                feature.NextLandingLocalOffset.x,
                currentLocal.y,
                feature.NextLandingLocalOffset.y);
            Vector3 predicted = rootPosition + rootRotation * predictedLocal;
            if (!IsFinite(predicted))
                return new PredictedFootprint(currentSole, horizon, horizonClamped, FootPredictionRejectReason.NonFinite);
            if (Vector3.Distance(currentSole, predicted) > m_Settings.Prediction.MaximumPredictionDistance)
                return new PredictedFootprint(currentSole, horizon, horizonClamped, FootPredictionRejectReason.DistanceExceeded);
            if (Vector3.Distance(pose.HipPosition, predicted) >
                legLength * m_Settings.Prediction.MaximumReachRatio)
                return new PredictedFootprint(currentSole, horizon, horizonClamped, FootPredictionRejectReason.ReachExceeded);
            return new PredictedFootprint(predicted, horizon, horizonClamped, FootPredictionRejectReason.None);
        }

        ResolvedFoot ResolveFoot(
            FootRuntimeState state,
            CharacterFootPlacementAnimatedFootPose pose,
            FootKinematics kinematics,
            ContactDecision contact,
            PredictedFootprint prediction,
            FootPlacementSupportResult support,
            AnimationFootFeatureSample feature,
            CharacterBodyPresentationFrame body,
            CharacterFootPlacementFeatureFrame weight,
            float legLength,
            float deltaSeconds)
        {
            state.TransitionReason = FootConstraintTransitionReason.None;
            Vector3 animatedSole = (pose.HeelPosition + pose.ToePosition) * 0.5f;
            if (!body.TargetGrounded)
                Release(state, FootConstraintTransitionReason.BodyAirborne);
            else if (weight.Value < m_Settings.Contact.MinimumPlacementWeight)
                Release(state, FootConstraintTransitionReason.PolicyReleased);
            else if (state.ConstraintState != FootConstraintState.Free)
            {
                if (!IsSurfaceValid(state.Surface))
                    Release(state, FootConstraintTransitionReason.SurfaceInvalid);
                else
                {
                    FootPlacementSurface anchor = state.Surface.Rebuild();
                    state.LockError = Vector3.Distance(animatedSole, anchor.Point);
                    state.ReplantError = state.LockError;
                    if (state.LockError > m_Settings.Constraint.ReplantDistance ||
                        Vector3.Angle(anchor.Normal, support.HasSupport ? support.Surface.Normal : anchor.Normal) >
                        m_Settings.Constraint.ReplantAngleDegrees)
                    {
                        Release(state, FootConstraintTransitionReason.ReplantThresholdExceeded);
                    }
                    else if (Vector3.Distance(pose.HipPosition, anchor.Point) >
                             legLength * m_Settings.Constraint.MaximumReachRatio)
                    {
                        Release(state, FootConstraintTransitionReason.LegUnreachable);
                    }
                    else
                    {
                        state.Surface = anchor;
                        if (state.ConstraintState == FootConstraintState.Locked &&
                            state.LockError > m_Settings.Constraint.SlideStartDistance)
                        {
                            state.ConstraintState = FootConstraintState.Sliding;
                            state.TransitionReason = FootConstraintTransitionReason.AnimationDrift;
                        }
                        if (state.ConstraintState == FootConstraintState.Sliding)
                            UpdateSliding(state, support, contact.WorldVelocity, deltaSeconds);
                    }
                }
            }

            if (state.ReachBlocked && support.HasSupport &&
                IsReachableAtPelvis(pose.HipPosition, support.Surface.Point, legLength))
                state.ReachBlocked = false;
            if (state.ConstraintState == FootConstraintState.Free &&
                state.TransitionReason == FootConstraintTransitionReason.None &&
                state.SolveWeight <= 0.001f &&
                !state.ReachBlocked &&
                contact.CanPlant && support.HasSupport &&
                Vector3.Distance(pose.HipPosition, support.Surface.Point) <=
                legLength * m_Settings.Constraint.MaximumReachRatio)
            {
                state.ConstraintState = FootConstraintState.Locked;
                state.TransitionReason = FootConstraintTransitionReason.ContactCommitted;
                state.Surface = support.Surface;
                state.PlantSurfaceLocalPoint = support.Surface.LocalPoint;
            }
            if (contact.ShouldRelease && state.ConstraintState != FootConstraintState.Free)
                Release(state, body.TargetGrounded
                    ? FootConstraintTransitionReason.ContactReleased
                    : FootConstraintTransitionReason.BodyAirborne);

            state.LastSupportDistance = support.SoleDistance;
            state.HasSupportHistory = support.HasSupport;
            FootPlacementSurface targetSurface = state.ConstraintState == FootConstraintState.Free
                ? support.CurrentSupport
                : state.Surface.Rebuild();
            float surfaceHeightDelta = targetSurface.IsValid ? targetSurface.Point.y - animatedSole.y : 0f;
            float heelLiftDistance = CharacterFootPlacementRotationPlanner.ResolveHeelLift(
                pose,
                support,
                state.ConstraintState != FootConstraintState.Free,
                m_Settings.Constraint);
            float desiredClearance = state.ConstraintState == FootConstraintState.Free
                ? support.SwingClearance
                : 0f;
            state.Clearance = state.ConstraintState == FootConstraintState.Free
                ? Decay(
                    state.Clearance,
                    desiredClearance,
                    m_Settings.Smoothing.ClearanceHalfLifeSeconds,
                    deltaSeconds)
                : 0f;
            float clearance = state.Clearance;
            Vector3 targetPosition = pose.AnklePosition + Vector3.up * clearance;
            float positionResponse = m_Settings.Rotation.SamplePositionResponse(body.VisibleVelocity.magnitude);
            float freePositionWeight = clearance > 0.0001f ? weight.Value * positionResponse : 0f;
            if (targetSurface.IsValid && state.ConstraintState != FootConstraintState.Free)
            {
                Vector3 ankleFromSole = pose.AnklePosition - animatedSole;
                targetPosition = targetSurface.Point + ankleFromSole + Vector3.up * (clearance - heelLiftDistance);
            }
            else if (targetSurface.IsValid && state.SolveWeight <= 0.001f)
            {
                float lift = Mathf.Max(0f, targetSurface.Point.y - animatedSole.y);
                targetPosition = pose.AnklePosition + Vector3.up * Mathf.Max(clearance, lift);
                freePositionWeight = targetPosition.y > pose.AnklePosition.y + 0.0001f
                    ? weight.Value * positionResponse
                    : 0f;
            }
            float movementSpeed = body.VisibleVelocity.magnitude;
            float desiredConstraintWeight = state.ConstraintState != FootConstraintState.Free && targetSurface.IsValid
                ? weight.Value * positionResponse
                : 0f;
            state.SolveWeight = Decay(
                state.SolveWeight,
                desiredConstraintWeight,
                desiredConstraintWeight > state.SolveWeight
                    ? m_Settings.Smoothing.PlantHalfLifeSeconds
                    : m_Settings.Smoothing.ReleaseHalfLifeSeconds,
                deltaSeconds);
            float ankleTwistDegrees = 0f;
            Quaternion targetRotation = targetSurface.IsValid
                ? CharacterFootPlacementRotationPlanner.ResolveRotation(
                    m_Rig.VisualRoot,
                    pose,
                    targetSurface.Normal,
                    surfaceHeightDelta,
                    heelLiftDistance,
                    m_Settings.Constraint,
                    m_Settings.Rotation,
                    out ankleTwistDegrees)
                : pose.AnkleRotation;
            state.TargetRotation = state.HasTargetRotation
                ? Quaternion.Slerp(
                    state.TargetRotation,
                    targetRotation,
                    DecayFactor(m_Settings.Smoothing.RotationHalfLifeSeconds, deltaSeconds))
                : targetRotation;
            state.HasTargetRotation = true;
            state.TargetPosition = targetPosition;
            float positionWeight = state.ConstraintState != FootConstraintState.Free
                ? state.SolveWeight
                : Mathf.Max(state.SolveWeight, freePositionWeight);
            float rotationWeight = targetSurface.IsValid && state.ConstraintState != FootConstraintState.Free
                ? state.SolveWeight * m_Settings.Rotation.SampleRotationResponse(movementSpeed)
                : 0f;
            Vector3 poleDirection = state.Side == CharacterFootSide.Left
                ? m_Rig.LeftKneePoleLocalDirection
                : m_Rig.RightKneePoleLocalDirection;
            Vector3 bendGoal = pose.HipPosition +
                               m_Rig.VisualRoot.TransformDirection(poleDirection) * legLength;
            var plan = new FootPlacementFootPlan(
                state.Side,
                targetPosition,
                state.TargetRotation,
                bendGoal,
                positionWeight,
                positionWeight,
                rotationWeight,
                state.ConstraintState,
                state.TransitionReason);
            return new ResolvedFoot(
                plan,
                pose,
                kinematics,
                prediction,
                support,
                feature,
                contact.WorldVelocity,
                state.LockError,
                state.ReplantError,
                weight,
                targetSurface.Identity,
                ankleTwistDegrees,
                heelLiftDistance,
                0f);
        }

        void UpdateSliding(
            FootRuntimeState state,
            FootPlacementSupportResult support,
            Vector3 worldVelocity,
            float deltaSeconds)
        {
            if (support.HasSupport && support.Surface.Identity == state.Surface.Identity)
            {
                Vector3 currentLocal = state.Surface.LocalPoint;
                Vector3 desiredLocal = support.Surface.LocalPoint;
                Vector3 movedLocal = Vector3.MoveTowards(
                    currentLocal,
                    desiredLocal,
                    m_Settings.Constraint.SlideSpeed * deltaSeconds);
                if (Vector3.Distance(state.PlantSurfaceLocalPoint, movedLocal) >
                    m_Settings.Constraint.MaximumSlideDistance)
                {
                    Release(state, FootConstraintTransitionReason.ReplantThresholdExceeded);
                    return;
                }
                Vector3 movedWorld = state.Surface.Transform.TransformPoint(movedLocal);
                state.Surface = new FootPlacementSurface(
                    state.Surface.Collider,
                    movedWorld,
                    support.Surface.Normal);
            }
            float planarSpeed = new Vector2(worldVelocity.x, worldVelocity.z).magnitude;
            if (state.ConstraintState == FootConstraintState.Sliding &&
                state.LockError <= m_Settings.Constraint.SlideStopDistance &&
                planarSpeed <= m_Settings.Contact.PlantPlanarSpeed)
            {
                state.ConstraintState = FootConstraintState.Locked;
                state.TransitionReason = FootConstraintTransitionReason.SlideSettled;
            }
        }

        PelvisResolution ResolvePelvis(
            CharacterFootPlacementAnimatedPose pose,
            ResolvedFoot left,
            ResolvedFoot right,
            CharacterBodyPresentationFrame body,
            float deltaSeconds)
        {
            float leftWeight = left.Plan.ConstraintState == FootConstraintState.Free
                ? 0f
                : left.Plan.PositionWeight;
            float rightWeight = right.Plan.ConstraintState == FootConstraintState.Free
                ? 0f
                : right.Plan.PositionWeight;
            float leftDelta = left.Plan.Position.y - pose.Left.AnklePosition.y;
            float rightDelta = right.Plan.Position.y - pose.Right.AnklePosition.y;
            PelvisHeightResolution height = ResolvePelvisHeight(
                leftWeight,
                rightWeight,
                leftDelta,
                rightDelta,
                left.Plan.Position,
                right.Plan.Position,
                body);
            float desired = height.DesiredOffset * left.Weight.Value;
            VerticalInterval leftInterval = BuildPelvisInterval(
                pose.Left.HipPosition,
                left.Plan.Position,
                Mathf.Max(
                    0.0001f,
                    m_Rig.LeftLegLength * m_Settings.Constraint.MaximumReachRatio -
                    m_Settings.Pelvis.ReachSlack));
            VerticalInterval rightInterval = BuildPelvisInterval(
                pose.Right.HipPosition,
                right.Plan.Position,
                Mathf.Max(
                    0.0001f,
                    m_Rig.RightLegLength * m_Settings.Constraint.MaximumReachRatio -
                    m_Settings.Pelvis.ReachSlack));
            FootPlacementSupportFoot supportFoot = height.SupportFoot;
            CharacterFootSide unreachableFoot = default;
            bool hasLeft = leftWeight > 0.0001f;
            bool hasRight = rightWeight > 0.0001f;
            if (hasLeft && hasRight)
            {
                float minimum = Mathf.Max(leftInterval.Minimum, rightInterval.Minimum);
                float maximum = Mathf.Min(leftInterval.Maximum, rightInterval.Maximum);
                if (minimum <= maximum)
                    desired = Mathf.Clamp(desired, minimum, maximum);
                else
                {
                    unreachableFoot = ResolveUnreachableFoot(
                        supportFoot,
                        desired,
                        leftInterval,
                        rightInterval);
                    if (unreachableFoot == CharacterFootSide.Left)
                    {
                        desired = Mathf.Clamp(rightDelta, rightInterval.Minimum, rightInterval.Maximum);
                        supportFoot = FootPlacementSupportFoot.Right;
                    }
                    else
                    {
                        desired = Mathf.Clamp(leftDelta, leftInterval.Minimum, leftInterval.Maximum);
                        supportFoot = FootPlacementSupportFoot.Left;
                    }
                }
            }
            else if (hasLeft)
                desired = Mathf.Clamp(desired, leftInterval.Minimum, leftInterval.Maximum);
            else if (hasRight)
                desired = Mathf.Clamp(desired, rightInterval.Minimum, rightInterval.Maximum);
            else
                desired = 0f;
            float reachTarget = Mathf.Clamp(
                desired,
                -m_Settings.Pelvis.MaximumDownOffset,
                m_Settings.Pelvis.MaximumUpOffset);
            float previousReach = m_PelvisReachOffset;
            DecayCritical(
                ref m_PelvisReachOffset,
                ref m_PelvisReachVelocity,
                reachTarget,
                m_Settings.Pelvis.HalfLifeSeconds,
                deltaSeconds);
            float maximumReachDelta = m_Settings.Pelvis.MaximumSpeed * deltaSeconds;
            m_PelvisReachOffset = Mathf.Clamp(
                m_PelvisReachOffset,
                previousReach - maximumReachDelta,
                previousReach + maximumReachDelta);
            float compensationTarget = UpdateActorMovementCompensation(body, deltaSeconds);
            float targetOffset = Mathf.Clamp(
                reachTarget + compensationTarget,
                -m_Settings.Pelvis.MaximumDownOffset,
                m_Settings.Pelvis.MaximumUpOffset);
            m_PelvisOffset = Mathf.Clamp(
                m_PelvisReachOffset + m_ActorMovementCompensationOffset,
                -m_Settings.Pelvis.MaximumDownOffset,
                m_Settings.Pelvis.MaximumUpOffset);
            return new PelvisResolution(
                targetOffset,
                m_PelvisOffset,
                reachTarget,
                m_PelvisReachOffset,
                compensationTarget,
                m_ActorMovementCompensationOffset,
                m_ActorMovementCompensationVelocity,
                height.Mode,
                height.Decision,
                height.Reason,
                height.DirectionalSpeed,
                height.FootLeadDistance,
                height.SlopeHeightDifference,
                supportFoot,
                unreachableFoot);
        }

        PelvisHeightResolution ResolvePelvisHeight(
            float leftWeight,
            float rightWeight,
            float leftDelta,
            float rightDelta,
            Vector3 leftPosition,
            Vector3 rightPosition,
            CharacterBodyPresentationFrame body)
        {
            bool hasLeft = leftWeight > 0.0001f;
            bool hasRight = rightWeight > 0.0001f;
            FootPlacementPelvisHeightMode mode = m_Settings.Pelvis.HeightMode;
            Vector3 movement = body.VisibleVelocity;
            movement.y = 0f;
            float directionalSpeed = movement.magnitude;
            Vector3 footDelta = rightPosition - leftPosition;
            float footLeadDistance = directionalSpeed > 0.0001f
                ? Vector3.Dot(footDelta, movement / directionalSpeed)
                : 0f;
            float slopeHeightDifference = rightPosition.y - leftPosition.y;

            if (!hasLeft && !hasRight)
            {
                return new PelvisHeightResolution(
                    0f,
                    FootPlacementSupportFoot.None,
                    mode,
                    FootPlacementPelvisHeightDecision.Unavailable,
                    FootPlacementPelvisHeightReason.NoPlantedFeet,
                    directionalSpeed,
                    footLeadDistance,
                    slopeHeightDifference);
            }
            if (hasLeft && !hasRight)
            {
                return new PelvisHeightResolution(
                    leftDelta,
                    FootPlacementSupportFoot.Left,
                    mode,
                    FootPlacementPelvisHeightDecision.Resolved,
                    FootPlacementPelvisHeightReason.SinglePlantedFoot,
                    directionalSpeed,
                    footLeadDistance,
                    slopeHeightDifference);
            }
            if (!hasLeft)
            {
                return new PelvisHeightResolution(
                    rightDelta,
                    FootPlacementSupportFoot.Right,
                    mode,
                    FootPlacementPelvisHeightDecision.Resolved,
                    FootPlacementPelvisHeightReason.SinglePlantedFoot,
                    directionalSpeed,
                    footLeadDistance,
                    slopeHeightDifference);
            }
            if (mode == FootPlacementPelvisHeightMode.AllPlantedFeet)
            {
                float totalWeight = leftWeight + rightWeight;
                return new PelvisHeightResolution(
                    (leftDelta * leftWeight + rightDelta * rightWeight) / totalWeight,
                    FootPlacementSupportFoot.Both,
                    mode,
                    FootPlacementPelvisHeightDecision.Resolved,
                    FootPlacementPelvisHeightReason.AllPlantedFeet,
                    directionalSpeed,
                    footLeadDistance,
                    slopeHeightDifference);
            }
            if (directionalSpeed < m_Settings.Pelvis.MinimumDirectionalSpeed)
            {
                return new PelvisHeightResolution(
                    0f,
                    FootPlacementSupportFoot.Both,
                    mode,
                    FootPlacementPelvisHeightDecision.Unavailable,
                    FootPlacementPelvisHeightReason.MovementDirectionUnavailable,
                    directionalSpeed,
                    footLeadDistance,
                    slopeHeightDifference);
            }
            if (Mathf.Abs(footLeadDistance) < m_Settings.Pelvis.MinimumFootLeadDistance)
            {
                return new PelvisHeightResolution(
                    0f,
                    FootPlacementSupportFoot.Both,
                    mode,
                    FootPlacementPelvisHeightDecision.Neutral,
                    FootPlacementPelvisHeightReason.FootOrderAmbiguous,
                    directionalSpeed,
                    footLeadDistance,
                    slopeHeightDifference);
            }
            if (Mathf.Abs(slopeHeightDifference) < m_Settings.Pelvis.MinimumSlopeHeightDifference)
            {
                return new PelvisHeightResolution(
                    0f,
                    FootPlacementSupportFoot.Both,
                    mode,
                    FootPlacementPelvisHeightDecision.Neutral,
                    FootPlacementPelvisHeightReason.LevelSupport,
                    directionalSpeed,
                    footLeadDistance,
                    slopeHeightDifference);
            }

            bool rightIsForward = footLeadDistance > 0f;
            bool forwardIsHigher = rightIsForward
                ? slopeHeightDifference > 0f
                : slopeHeightDifference < 0f;
            FootPlacementSupportFoot selectedFoot = rightIsForward
                ? FootPlacementSupportFoot.Right
                : FootPlacementSupportFoot.Left;
            float selectedDelta = rightIsForward ? rightDelta : leftDelta;
            return new PelvisHeightResolution(
                selectedDelta,
                selectedFoot,
                mode,
                FootPlacementPelvisHeightDecision.Resolved,
                forwardIsHigher
                    ? FootPlacementPelvisHeightReason.UphillForwardFoot
                    : FootPlacementPelvisHeightReason.DownhillLowerFoot,
                directionalSpeed,
                footLeadDistance,
                slopeHeightDifference);
        }

        float UpdateActorMovementCompensation(CharacterBodyPresentationFrame body, float deltaSeconds)
        {
            if (!body.GroundedBefore || !body.GroundedAfter)
            {
                ClearActorMovementCompensation();
                return 0f;
            }

            FootPlacementActorMovementCompensationMode mode = m_Settings.Pelvis.ActorMovementCompensationMode;
            if (mode == FootPlacementActorMovementCompensationMode.ComponentSpace)
            {
                ClearActorMovementCompensation();
                return 0f;
            }

            bool shouldCompensate = mode == FootPlacementActorMovementCompensationMode.WorldSpace ||
                                    Mathf.Abs(body.SourceTranslationDelta.y) >= m_Settings.Pelvis.SuddenVerticalThreshold;
            float visibleVerticalDelta = body.VisibleTranslationDelta.y;
            if (shouldCompensate && Mathf.Abs(visibleVerticalDelta) > 0.000001f)
            {
                m_ActorMovementCompensationOffset = Mathf.Clamp(
                    m_ActorMovementCompensationOffset - visibleVerticalDelta,
                    -m_Settings.Pelvis.MaximumActorMovementCompensation,
                    m_Settings.Pelvis.MaximumActorMovementCompensation);
                m_ActorMovementCompensationVelocity = 0f;
                return m_ActorMovementCompensationOffset;
            }

            float target = m_ActorMovementCompensationOffset;
            float previous = m_ActorMovementCompensationOffset;
            DecayCritical(
                ref m_ActorMovementCompensationOffset,
                ref m_ActorMovementCompensationVelocity,
                0f,
                m_Settings.Pelvis.ActorMovementCompensationHalfLifeSeconds,
                deltaSeconds);
            float maximumDelta = m_Settings.Pelvis.ActorMovementCompensationMaximumSpeed * deltaSeconds;
            m_ActorMovementCompensationOffset = Mathf.Clamp(
                m_ActorMovementCompensationOffset,
                previous - maximumDelta,
                previous + maximumDelta);
            return target;
        }

        void ClearActorMovementCompensation()
        {
            m_ActorMovementCompensationOffset = 0f;
            m_ActorMovementCompensationVelocity = 0f;
        }

        ResolvedFoot MarkLegUnreachable(FootRuntimeState state, ResolvedFoot foot)
        {
            Release(state, FootConstraintTransitionReason.LegUnreachable);
            state.SolveWeight = 0f;
            var plan = new FootPlacementFootPlan(
                foot.Plan.Side,
                foot.Pose.AnklePosition,
                foot.Pose.AnkleRotation,
                foot.Pose.KneePosition,
                0f,
                0f,
                0f,
                FootConstraintState.Free,
                FootConstraintTransitionReason.LegUnreachable);
            return new ResolvedFoot(
                plan,
                foot.Pose,
                foot.Kinematics,
                foot.Prediction,
                foot.Support,
                foot.Feature,
                foot.WorldVelocity,
                foot.LockError,
                foot.ReplantError,
                foot.Weight,
                foot.SurfaceIdentity,
                foot.AnkleTwistDegrees,
                foot.HeelLiftDistance,
                foot.SeparationCorrection);
        }

        void ApplyFootSeparation(ref ResolvedFoot left, ref ResolvedFoot right)
        {
            float minimum = m_Settings.Constraint.MinimumFootSeparation;
            Vector3 delta = right.Plan.Position - left.Plan.Position;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance >= minimum)
                return;
            bool leftMovable = left.Plan.ConstraintState == FootConstraintState.Free;
            bool rightMovable = right.Plan.ConstraintState == FootConstraintState.Free;
            if (!leftMovable && !rightMovable)
                return;
            Vector3 direction = distance > 0.0001f ? delta / distance : ResolveSeparationDirection(left, right);
            float correction = minimum - distance;
            if (leftMovable && rightMovable)
            {
                left = WithPlanPosition(left, left.Plan.Position - direction * (correction * 0.5f), correction * 0.5f);
                right = WithPlanPosition(right, right.Plan.Position + direction * (correction * 0.5f), correction * 0.5f);
            }
            else if (leftMovable)
                left = WithPlanPosition(left, left.Plan.Position - direction * correction, correction);
            else
                right = WithPlanPosition(right, right.Plan.Position + direction * correction, correction);
        }

        Vector3 ResolveSeparationDirection(ResolvedFoot left, ResolvedFoot right)
        {
            Vector3 animated = right.Pose.AnklePosition - left.Pose.AnklePosition;
            animated.y = 0f;
            if (animated.sqrMagnitude > 0.0001f)
                return animated.normalized;
            Vector3 rightAxis = m_Rig.VisualRoot.right;
            rightAxis.y = 0f;
            return rightAxis.normalized;
        }

        static ResolvedFoot WithPlanPosition(ResolvedFoot foot, Vector3 position, float separationCorrection)
        {
            var plan = new FootPlacementFootPlan(
                foot.Plan.Side,
                position,
                foot.Plan.Rotation,
                foot.Plan.BendGoalPosition,
                foot.Plan.BendGoalWeight,
                foot.Plan.PositionWeight,
                foot.Plan.RotationWeight,
                foot.Plan.ConstraintState,
                foot.Plan.TransitionReason);
            return new ResolvedFoot(
                plan,
                foot.Pose,
                foot.Kinematics,
                foot.Prediction,
                foot.Support,
                foot.Feature,
                foot.WorldVelocity,
                foot.LockError,
                foot.ReplantError,
                foot.Weight,
                foot.SurfaceIdentity,
                foot.AnkleTwistDegrees,
                foot.HeelLiftDistance,
                foot.SeparationCorrection + separationCorrection);
        }

        void BuildSnapshot(
            CharacterPosePostProcessFrame frame,
            ResolvedFoot left,
            ResolvedFoot right,
            PelvisResolution pelvis,
            CharacterFootPlacementSolverResult solverResult)
        {
            ComposedAnimationPoseFrame animationPose = frame.AnimationPose;
            CharacterFootPlacementFrameSnapshot.CopyContributions(
                animationPose.Contributions,
                m_DiagnosticContributions);
            m_Snapshot = new CharacterFootPlacementFrameSnapshot(
                m_ActorId, frame.RenderFrame,
                frame.Body.PreviousTick, frame.Body.CurrentTick, frame.Body.ResetSequence,
                animationPose.PosePlanHash,
                animationPose.CompletionIdentity,
                animationPose.ContinuityIdentity,
                m_Settings.FootPlacementWeightParameterId.Value,
                m_Settings.FootPlacementWeightParameterIndex,
                animationPose.PoseParameters[m_Settings.FootPlacementWeightParameterIndex],
                m_Settings.FootAnalysis.CalibrationId.Value,
                m_Settings.FootAnalysis.CalibrationRevision, m_Settings.FootAnalysis.AnalysisSourceId, m_Settings.FootAnalysis.AnalysisVersion,
                m_Settings.FootAnalysis.AlgorithmVersion,
                m_DiagnosticContributions, Mathf.Min(animationPose.Contributions.Count, m_DiagnosticContributions.Length),
                BuildFootSnapshot(left),
                BuildFootSnapshot(right),
                m_Settings.Pelvis.ActorMovementCompensationMode,
                frame.Body.SourceTranslationDelta,
                frame.Body.VisibleTranslationDelta,
                frame.Body.GroundedBefore,
                frame.Body.GroundedAfter,
                pelvis.ReachTargetOffset,
                pelvis.ReachCurrentOffset,
                pelvis.ActorMovementCompensationTargetOffset,
                pelvis.ActorMovementCompensationCurrentOffset,
                pelvis.ActorMovementCompensationVelocity,
                pelvis.TargetOffset,
                pelvis.CurrentOffset,
                pelvis.HeightMode,
                pelvis.HeightDecision,
                pelvis.HeightReason,
                pelvis.DirectionalSpeed,
                pelvis.FootLeadDistance,
                pelvis.SlopeHeightDifference,
                pelvis.SupportFoot,
                solverResult);
        }
        FootPlacementFootFrameSnapshot BuildFootSnapshot(ResolvedFoot foot)
        {
            return new FootPlacementFootFrameSnapshot(
                foot.Plan.Side,
                foot.Plan.ConstraintState,
                foot.Plan.TransitionReason,
                foot.Kinematics.AnkleVelocity,
                foot.Kinematics.ToeVelocity,
                foot.Kinematics.Descending,
                foot.Support.SoleDistance,
                foot.Prediction.Position,
                foot.Prediction.Horizon,
                foot.Prediction.HorizonClamped,
                foot.Prediction.RejectReason,
                foot.Support.QueryCount,
                foot.Support.CandidateCount,
                foot.Support.RejectedCount,
                foot.SurfaceIdentity,
                foot.LockError,
                foot.ReplantError,
                foot.Weight.Value,
                foot.Plan.PositionWeight,
                foot.Feature.SoleLocalVelocity, foot.WorldVelocity, foot.Feature.SoleHeight, foot.Feature.PlantConfidence,
                foot.Feature.NextLandingConfidence, foot.Feature.NextLandingDelaySeconds,
                foot.Feature.NextLandingLocalOffset,
                foot.Support.HeelSupport.Identity, foot.Support.ToeSupport.Identity,
                foot.Support.CurrentSupport.Identity, foot.Support.FutureLandingSupport.Identity,
                foot.Support.GroundEnvelope.Count, foot.Support.GroundEnvelope.RejectReason,
                foot.AnkleTwistDegrees, foot.HeelLiftDistance, foot.SeparationCorrection,
                foot.Plan.Position, foot.Plan.Rotation);
        }

        void PublishDiagnostics()
        {
            if (!m_Diagnostics.ShouldPublish(RuntimeTraceChannel.FootPlacement, RuntimeTraceEventKind.FootPlacementSnapshot))
                return;
            m_Diagnostics.Publish(
                RuntimeTraceChannel.FootPlacement,
                RuntimeTraceDomain.Presentation,
                RuntimeTraceEventKind.FootPlacementSnapshot,
                RuntimeSourceElementHandle.Invalid,
                RuntimeInstanceKey.Character(m_Diagnostics.CharacterRuntimeId),
                new RuntimeTracePayload
                {
                    Name = m_ActorId.Value,
                    OwnerId = m_Snapshot.PosePlanHash,
                    RelatedElementId = m_Snapshot.FootPlacementWeightParameterId,
                    Status = $"L:{m_Snapshot.Left.ConstraintState}/R:{m_Snapshot.Right.ConstraintState}/P:{m_Snapshot.PelvisHeightDecision}",
                    Cause = m_Snapshot.PelvisHeightReason.ToString(),
                    Detail = CharacterFootPlacementDiagnosticFormatter.Format(m_Snapshot),
                    Weight = m_Snapshot.Left.SolverWeight,
                    FinalWeight = m_Snapshot.Right.SolverWeight,
                    Time = m_Snapshot.PelvisTargetOffset,
                    SecondaryTime = m_Snapshot.PelvisCurrentOffset,
                    Value = DebugValueSnapshot.Capture(new Vector3(
                        m_Snapshot.Left.SurfaceDistance,
                        m_Snapshot.Right.SurfaceDistance,
                        m_Snapshot.PelvisCurrentOffset))
                });
        }

        void ResetInternal(
            ulong renderFrame,
            ulong resetSequence,
            FootConstraintTransitionReason reason,
            bool notifySolver)
        {
            m_Left.Reset(reason);
            m_Right.Reset(reason);
            m_PelvisOffset = 0f;
            m_PelvisReachOffset = 0f;
            m_PelvisReachVelocity = 0f;
            ClearActorMovementCompensation();
            m_Snapshot = default;
            m_LastRenderFrame = renderFrame;
            m_ResetSequence = resetSequence;
            if (notifySolver && m_Solver.IsInitialized)
                m_Solver.ResetPose(new CharacterFootPlacementSolverReset(renderFrame, resetSequence, reason));
        }

        bool IsSurfaceValid(FootPlacementSurface surface)
        {
            if (!surface.IsValid || !surface.Collider.enabled || !surface.Collider.gameObject.activeInHierarchy)
                return false;
            int layerBit = 1 << surface.Collider.gameObject.layer;
            return (m_Settings.Trace.GroundLayerMask & layerBit) != 0 && !m_Rig.IsSelfCollider(surface.Collider);
        }

        void Release(FootRuntimeState state, FootConstraintTransitionReason reason)
        {
            state.ReachBlocked = reason == FootConstraintTransitionReason.LegUnreachable;
            if (state.ConstraintState == FootConstraintState.Free)
                return;
            state.ConstraintState = FootConstraintState.Free;
            state.TransitionReason = reason;
            state.Surface = default;
            state.PlantSurfaceLocalPoint = Vector3.zero;
        }

        bool HasIllegalDiscontinuity(FootKinematics kinematics)
        {
            if (!kinematics.HasVelocityHistory)
                return false;
            float maximum = m_Settings.Prediction.MaximumPredictionDistance * 2f;
            return kinematics.AnkleLocalVelocity.magnitude > maximum * 120f ||
                   kinematics.ToeLocalVelocity.magnitude > maximum * 120f;
        }

        static VerticalInterval BuildPelvisInterval(Vector3 hip, Vector3 foot, float maximumReach)
        {
            Vector2 horizontal = new Vector2(hip.x - foot.x, hip.z - foot.z);
            float verticalReach = Mathf.Sqrt(Mathf.Max(0f, maximumReach * maximumReach - horizontal.sqrMagnitude));
            return new VerticalInterval(
                foot.y - verticalReach - hip.y,
                foot.y + verticalReach - hip.y);
        }

        bool IsReachableAtPelvis(Vector3 hip, Vector3 foot, float legLength)
        {
            Vector3 resolvedHip = hip + Vector3.up * m_PelvisOffset;
            float maximumReach = Mathf.Max(
                0.0001f,
                legLength * m_Settings.Constraint.MaximumReachRatio - m_Settings.Pelvis.ReachSlack);
            return Vector3.Distance(resolvedHip, foot) <= maximumReach;
        }

        static CharacterFootSide ResolveUnreachableFoot(
            FootPlacementSupportFoot supportFoot,
            float desired,
            VerticalInterval left,
            VerticalInterval right)
        {
            if (supportFoot == FootPlacementSupportFoot.Left)
                return CharacterFootSide.Right;
            if (supportFoot == FootPlacementSupportFoot.Right)
                return CharacterFootSide.Left;
            float leftError = DistanceToInterval(desired, left);
            float rightError = DistanceToInterval(desired, right);
            return leftError <= rightError ? CharacterFootSide.Right : CharacterFootSide.Left;
        }

        static float DistanceToInterval(float value, VerticalInterval interval)
        {
            if (value < interval.Minimum)
                return interval.Minimum - value;
            return value > interval.Maximum ? value - interval.Maximum : 0f;
        }

        static float Decay(float current, float target, float halfLife, float deltaSeconds)
        {
            return Mathf.Lerp(current, target, DecayFactor(halfLife, deltaSeconds));
        }

        static float DecayFactor(float halfLife, float deltaSeconds)
        {
            return deltaSeconds <= 0f ? 0f : 1f - Mathf.Exp(-HalfLifeLambda * deltaSeconds / halfLife);
        }

        static void DecayCritical(
            ref float value,
            ref float velocity,
            float target,
            float halfLife,
            float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
                return;
            float lambda = HalfLifeLambda / halfLife;
            float error = value - target;
            float j = velocity + lambda * error;
            float decay = Mathf.Exp(-lambda * deltaSeconds);
            value = target + (error + j * deltaSeconds) * decay;
            velocity = (velocity - lambda * j * deltaSeconds) * decay;
        }

        static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterFootPlacementRuntime));
        }

        readonly struct ContactDecision
        {
            public ContactDecision(bool canPlant, bool shouldRelease, Vector3 worldVelocity)
            {
                CanPlant = canPlant;
                ShouldRelease = shouldRelease;
                WorldVelocity = worldVelocity;
            }
            public bool CanPlant { get; }
            public bool ShouldRelease { get; }
            public Vector3 WorldVelocity { get; }
        }

        readonly struct FootKinematics
        {
            public FootKinematics(Vector3 ankleLocal, Vector3 toeLocal, Vector3 soleLocal, Vector3 ankleLocalVelocity, Vector3 toeLocalVelocity, Vector3 soleLocalVelocity, Vector3 ankleVelocity, Vector3 toeVelocity, Vector3 soleVelocity, bool hasVelocityHistory, bool descending)
            { AnkleLocal = ankleLocal; ToeLocal = toeLocal; SoleLocal = soleLocal; AnkleLocalVelocity = ankleLocalVelocity; ToeLocalVelocity = toeLocalVelocity; SoleLocalVelocity = soleLocalVelocity; AnkleVelocity = ankleVelocity; ToeVelocity = toeVelocity; SoleVelocity = soleVelocity; HasVelocityHistory = hasVelocityHistory; Descending = descending; }
            public Vector3 AnkleLocal { get; }
            public Vector3 ToeLocal { get; }
            public Vector3 SoleLocal { get; }
            public Vector3 AnkleLocalVelocity { get; }
            public Vector3 ToeLocalVelocity { get; }
            public Vector3 SoleLocalVelocity { get; }
            public Vector3 AnkleVelocity { get; }
            public Vector3 ToeVelocity { get; }
            public Vector3 SoleVelocity { get; }
            public bool HasVelocityHistory { get; }
            public bool Descending { get; }
        }

        readonly struct ResolvedFoot
        {
            public ResolvedFoot(FootPlacementFootPlan plan, CharacterFootPlacementAnimatedFootPose pose, FootKinematics kinematics, PredictedFootprint prediction, FootPlacementSupportResult support, AnimationFootFeatureSample feature, Vector3 worldVelocity, float lockError, float replantError, CharacterFootPlacementFeatureFrame weight, int surfaceIdentity, float ankleTwistDegrees, float heelLiftDistance, float separationCorrection)
            { Plan = plan; Pose = pose; Kinematics = kinematics; Prediction = prediction; Support = support; Feature = feature; WorldVelocity = worldVelocity; LockError = lockError; ReplantError = replantError; Weight = weight; SurfaceIdentity = surfaceIdentity; AnkleTwistDegrees = ankleTwistDegrees; HeelLiftDistance = heelLiftDistance; SeparationCorrection = separationCorrection; }
            public FootPlacementFootPlan Plan { get; }
            public CharacterFootPlacementAnimatedFootPose Pose { get; }
            public FootKinematics Kinematics { get; }
            public PredictedFootprint Prediction { get; }
            public FootPlacementSupportResult Support { get; }
            public AnimationFootFeatureSample Feature { get; }
            public Vector3 WorldVelocity { get; }
            public float LockError { get; }
            public float ReplantError { get; }
            public CharacterFootPlacementFeatureFrame Weight { get; }
            public int SurfaceIdentity { get; }
            public float AnkleTwistDegrees { get; }
            public float HeelLiftDistance { get; }
            public float SeparationCorrection { get; }
        }

        readonly struct VerticalInterval
        {
            public VerticalInterval(float minimum, float maximum) { Minimum = minimum; Maximum = maximum; }
            public float Minimum { get; }
            public float Maximum { get; }
        }

        readonly struct PelvisResolution
        {
            public PelvisResolution(
                float targetOffset,
                float currentOffset,
                float reachTargetOffset,
                float reachCurrentOffset,
                float actorMovementCompensationTargetOffset,
                float actorMovementCompensationCurrentOffset,
                float actorMovementCompensationVelocity,
                FootPlacementPelvisHeightMode heightMode,
                FootPlacementPelvisHeightDecision heightDecision,
                FootPlacementPelvisHeightReason heightReason,
                float directionalSpeed,
                float footLeadDistance,
                float slopeHeightDifference,
                FootPlacementSupportFoot supportFoot,
                CharacterFootSide unreachableFoot)
            {
                TargetOffset = targetOffset;
                CurrentOffset = currentOffset;
                ReachTargetOffset = reachTargetOffset;
                ReachCurrentOffset = reachCurrentOffset;
                ActorMovementCompensationTargetOffset = actorMovementCompensationTargetOffset;
                ActorMovementCompensationCurrentOffset = actorMovementCompensationCurrentOffset;
                ActorMovementCompensationVelocity = actorMovementCompensationVelocity;
                HeightMode = heightMode;
                HeightDecision = heightDecision;
                HeightReason = heightReason;
                DirectionalSpeed = directionalSpeed;
                FootLeadDistance = footLeadDistance;
                SlopeHeightDifference = slopeHeightDifference;
                SupportFoot = supportFoot;
                UnreachableFoot = unreachableFoot;
            }
            public float TargetOffset { get; }
            public float CurrentOffset { get; }
            public float ReachTargetOffset { get; }
            public float ReachCurrentOffset { get; }
            public float ActorMovementCompensationTargetOffset { get; }
            public float ActorMovementCompensationCurrentOffset { get; }
            public float ActorMovementCompensationVelocity { get; }
            public FootPlacementPelvisHeightMode HeightMode { get; }
            public FootPlacementPelvisHeightDecision HeightDecision { get; }
            public FootPlacementPelvisHeightReason HeightReason { get; }
            public float DirectionalSpeed { get; }
            public float FootLeadDistance { get; }
            public float SlopeHeightDifference { get; }
            public FootPlacementSupportFoot SupportFoot { get; }
            public CharacterFootSide UnreachableFoot { get; }
        }

        readonly struct PelvisHeightResolution
        {
            public PelvisHeightResolution(
                float desiredOffset,
                FootPlacementSupportFoot supportFoot,
                FootPlacementPelvisHeightMode mode,
                FootPlacementPelvisHeightDecision decision,
                FootPlacementPelvisHeightReason reason,
                float directionalSpeed,
                float footLeadDistance,
                float slopeHeightDifference)
            {
                DesiredOffset = desiredOffset;
                SupportFoot = supportFoot;
                Mode = mode;
                Decision = decision;
                Reason = reason;
                DirectionalSpeed = directionalSpeed;
                FootLeadDistance = footLeadDistance;
                SlopeHeightDifference = slopeHeightDifference;
            }

            public float DesiredOffset { get; }
            public FootPlacementSupportFoot SupportFoot { get; }
            public FootPlacementPelvisHeightMode Mode { get; }
            public FootPlacementPelvisHeightDecision Decision { get; }
            public FootPlacementPelvisHeightReason Reason { get; }
            public float DirectionalSpeed { get; }
            public float FootLeadDistance { get; }
            public float SlopeHeightDifference { get; }
        }

        sealed class FootRuntimeState
        {
            public FootRuntimeState(CharacterFootSide side) { Side = side; Reset(FootConstraintTransitionReason.PresentationReset); }
            public CharacterFootSide Side { get; }
            public FootConstraintState ConstraintState;
            public FootConstraintTransitionReason TransitionReason;
            public FootPlacementSurface Surface;
            public Vector3 PlantSurfaceLocalPoint;
            public Vector3 PreviousAnkleLocal;
            public Vector3 PreviousToeLocal;
            public Vector3 PreviousSoleLocal;
            public bool HasPoseHistory;
            public bool HasSupportHistory;
            public float LastSupportDistance;
            public float SolveWeight;
            public float LockError;
            public float ReplantError;
            public float Clearance;
            public Vector3 TargetPosition;
            public Quaternion TargetRotation;
            public bool HasTargetRotation;
            public bool ReachBlocked;

            public void Reset(FootConstraintTransitionReason reason)
            {
                ConstraintState = FootConstraintState.Free;
                TransitionReason = reason;
                Surface = default;
                PlantSurfaceLocalPoint = Vector3.zero;
                PreviousAnkleLocal = Vector3.zero;
                PreviousToeLocal = Vector3.zero;
                PreviousSoleLocal = Vector3.zero;
                HasPoseHistory = false;
                HasSupportHistory = false;
                LastSupportDistance = float.PositiveInfinity;
                SolveWeight = 0f;
                LockError = 0f;
                ReplantError = 0f;
                Clearance = 0f;
                TargetPosition = Vector3.zero;
                TargetRotation = Quaternion.identity;
                HasTargetRotation = false;
                ReachBlocked = false;
            }
        }
    }
}
