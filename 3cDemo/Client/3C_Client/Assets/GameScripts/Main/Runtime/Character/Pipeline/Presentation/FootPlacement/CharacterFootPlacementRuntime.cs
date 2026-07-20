using System;
using System.Collections.Generic;
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
        readonly AnimationPoseContribution[] m_DiagnosticContributions;

        CharacterFootPlacementFrameSnapshot m_Snapshot;
        ulong m_LastRenderFrame;
        ulong m_ResetSequence;
        float m_PelvisOffset;
        float m_PelvisVelocity;
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
            m_DiagnosticContributions = new AnimationPoseContribution[settings.ProducerCapacity];
            ResetInternal(0, 0, FootConstraintTransitionReason.PresentationReset, false);
        }

        public CharacterFootPlacementFrameSnapshot Snapshot => m_Snapshot;
        public string PoseSourceLayerId => m_Settings.PoseSourceLayerId;

        public void Present(CharacterPosePostProcessFrame frame)
        {
            RequireAlive();
            if (frame.ActorId != m_ActorId)
                throw new InvalidOperationException("Foot Placement frame targets another Actor.");
            if (frame.RenderFrame == m_LastRenderFrame)
                throw new InvalidOperationException($"Foot Placement Actor '{m_ActorId}' received render frame '{frame.RenderFrame}' twice.");
            if (!frame.Body.IsValid || frame.AnimationContributions.Count == 0)
            {
                ResetInternal(
                    frame.RenderFrame,
                    frame.Body.ResetSequence,
                    FootConstraintTransitionReason.MissingAnimationOutput,
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
                    CharacterFootPlacementPolicyWeight weight = ResolvePolicyWeight(frame.AnimationContributions);
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

                    ContactDecision leftContact = ClassifyContact(m_Left, leftKinematics, frame.Body, weight);
                    ContactDecision rightContact = ClassifyContact(m_Right, rightKinematics, frame.Body, weight);
                    PredictedFootprint leftPrediction = Predict(
                        m_Left,
                        pose.Left,
                        leftKinematics,
                        frame.Body,
                        m_Rig.LeftLegLength,
                        weight.Value);
                    PredictedFootprint rightPrediction = Predict(
                        m_Right,
                        pose.Right,
                        rightKinematics,
                        frame.Body,
                        m_Rig.RightLegLength,
                        weight.Value);
                    FootPlacementSupportResult leftSupport;
                    FootPlacementSupportResult rightSupport;
                    using (QueryMarker.Auto())
                    {
                        leftSupport = m_Query.Query(pose.Left, leftPrediction.Position, m_Rig.LeftLegLength);
                        rightSupport = m_Query.Query(pose.Right, rightPrediction.Position, m_Rig.RightLegLength);
                    }
                    ResolvedFoot left = ResolveFoot(
                        m_Left,
                        pose.Left,
                        leftKinematics,
                        leftContact,
                        leftPrediction,
                        leftSupport,
                        frame.Body,
                        weight,
                        m_Rig.LeftLegLength,
                        deltaSeconds);
                    ResolvedFoot right = ResolveFoot(
                        m_Right,
                        pose.Right,
                        rightKinematics,
                        rightContact,
                        rightPrediction,
                        rightSupport,
                        frame.Body,
                        weight,
                        m_Rig.RightLegLength,
                        deltaSeconds);
                    PelvisResolution pelvis = ResolvePelvis(pose, left, right, weight.Value, deltaSeconds);
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

        CharacterFootPlacementPolicyWeight ResolvePolicyWeight(
            IReadOnlyList<AnimationPoseContribution> contributions)
        {
            float visible = 0f;
            float weight = 0f;
            for (int i = 0; i < contributions.Count; i++)
            {
                AnimationPoseContribution contribution = contributions[i];
                if (!contribution.IsValid ||
                    !string.Equals(contribution.LayerId, m_Settings.PoseSourceLayerId, StringComparison.Ordinal))
                    throw new InvalidOperationException("Foot Placement received an invalid pose contribution.");
                if (!m_Settings.TrySample(
                        contribution.ProgramProducerIndex,
                        contribution.VisualSampleTime,
                        contribution.Cycle,
                        out AnimationFootPlacementSample sample))
                    throw new InvalidOperationException($"Foot Placement cannot sample animation curves for producer '{contribution.ProducerId}'.");
                float visualWeight = contribution.Weight;
                visible += visualWeight;
                weight += visualWeight * sample.Weight;
            }
            if (visible <= 0.0001f)
                return default;
            return new CharacterFootPlacementPolicyWeight(weight / visible, visible);
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
            FootRuntimeState state,
            FootKinematics kinematics,
            CharacterBodyPresentationFrame body,
            CharacterFootPlacementPolicyWeight weight)
        {
            if (!kinematics.HasVelocityHistory || !state.HasSupportHistory)
                return default;
            float planar = new Vector2(kinematics.SoleVelocity.x, kinematics.SoleVelocity.z).magnitude;
            float vertical = Mathf.Abs(kinematics.SoleVelocity.y);
            bool canPlant = body.TargetGrounded &&
                            weight.Value >= m_Settings.Contact.MinimumPlacementWeight &&
                            state.LastSupportDistance <= m_Settings.Contact.PlantDistance &&
                            planar <= m_Settings.Contact.PlantPlanarSpeed &&
                            vertical <= m_Settings.Contact.PlantVerticalSpeed &&
                            kinematics.Descending;
            bool shouldRelease = !body.TargetGrounded ||
                                 weight.Value < m_Settings.Contact.MinimumPlacementWeight ||
                                 state.LastSupportDistance > m_Settings.Contact.ReleaseDistance ||
                                 planar > m_Settings.Contact.ReleasePlanarSpeed ||
                                 vertical > m_Settings.Contact.ReleaseVerticalSpeed;
            return new ContactDecision(canPlant, shouldRelease);
        }

        PredictedFootprint Predict(
            FootRuntimeState state,
            CharacterFootPlacementAnimatedFootPose pose,
            FootKinematics kinematics,
            CharacterBodyPresentationFrame body,
            float legLength,
            float predictionWeight)
        {
            Vector3 currentSole = (pose.HeelPosition + pose.ToePosition) * 0.5f;
            if (predictionWeight <= 0f)
                return new PredictedFootprint(currentSole, 0f, false, FootPredictionRejectReason.NoSupportEstimate);
            float estimatedHorizon = m_Settings.Prediction.MaximumLookAheadSeconds;
            if (state.HasSupportHistory && IsFinite(state.LastSupportDistance) && kinematics.SoleVelocity.y < -0.0001f)
                estimatedHorizon = state.LastSupportDistance / -kinematics.SoleVelocity.y;
            float clampedHorizon = Mathf.Clamp(
                estimatedHorizon,
                m_Settings.Prediction.MinimumLookAheadSeconds,
                m_Settings.Prediction.MaximumLookAheadSeconds);
            bool horizonClamped = !Mathf.Approximately(estimatedHorizon, clampedHorizon);
            float horizon = clampedHorizon * predictionWeight;
            if (Mathf.Abs(body.VisibleYawVelocityDegreesPerSecond) >
                m_Settings.Prediction.MaximumYawVelocityDegreesPerSecond)
                return new PredictedFootprint(currentSole, horizon, horizonClamped, FootPredictionRejectReason.AngularVelocityExceeded);

            Vector3 rootPosition = m_Rig.VisualRoot.position + body.VisibleVelocity * horizon;
            Quaternion rootRotation = m_Rig.VisualRoot.rotation *
                                      Quaternion.Euler(0f, body.VisibleYawVelocityDegreesPerSecond * horizon, 0f);
            Vector3 predictedLocal = kinematics.SoleLocal + kinematics.SoleLocalVelocity * horizon;
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
            CharacterBodyPresentationFrame body,
            CharacterFootPlacementPolicyWeight weight,
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
                            UpdateSliding(state, support, kinematics, deltaSeconds);
                    }
                }
            }

            if (state.ReachBlocked && support.HasSupport &&
                IsReachableAtPelvis(pose.HipPosition, support.Surface.Point, legLength))
                state.ReachBlocked = false;
            if (state.ConstraintState == FootConstraintState.Free &&
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
                ? support.Surface
                : state.Surface.Rebuild();
            float desiredClearance = state.ConstraintState == FootConstraintState.Free
                ? support.SwingClearance * weight.Value
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
            float desiredPositionWeight = clearance > 0.0001f ? weight.Value : 0f;
            if (targetSurface.IsValid)
            {
                Vector3 ankleFromSole = pose.AnklePosition - animatedSole;
                targetPosition = targetSurface.Point + ankleFromSole + Vector3.up * clearance;
                if (state.ConstraintState != FootConstraintState.Free)
                    desiredPositionWeight = weight.Value;
            }
            state.SolveWeight = Decay(
                state.SolveWeight,
                desiredPositionWeight,
                desiredPositionWeight > state.SolveWeight
                    ? m_Settings.Smoothing.PlantHalfLifeSeconds
                    : m_Settings.Smoothing.ReleaseHalfLifeSeconds,
                deltaSeconds);
            Quaternion targetRotation = targetSurface.IsValid
                ? ResolveFootRotation(
                    pose.AnkleRotation,
                    pose.SoleForward,
                    targetSurface.Normal,
                    targetSurface.Point.y - animatedSole.y,
                    weight.Value)
                : pose.AnkleRotation;
            state.TargetRotation = state.HasTargetRotation
                ? Quaternion.Slerp(
                    state.TargetRotation,
                    targetRotation,
                    DecayFactor(m_Settings.Smoothing.RotationHalfLifeSeconds, deltaSeconds))
                : targetRotation;
            state.HasTargetRotation = true;
            state.TargetPosition = targetPosition;
            float rotationWeight = targetSurface.IsValid
                ? state.SolveWeight * weight.Value
                : 0f;
            var plan = new FootPlacementFootPlan(
                state.Side,
                targetPosition,
                state.TargetRotation,
                state.SolveWeight,
                rotationWeight,
                state.ConstraintState,
                state.TransitionReason);
            return new ResolvedFoot(
                plan,
                pose,
                kinematics,
                prediction,
                support,
                state.LockError,
                state.ReplantError,
                weight,
                targetSurface.Identity);
        }

        void UpdateSliding(
            FootRuntimeState state,
            FootPlacementSupportResult support,
            FootKinematics kinematics,
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
            float planarSpeed = new Vector2(kinematics.SoleVelocity.x, kinematics.SoleVelocity.z).magnitude;
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
            float pelvisPolicyWeight,
            float deltaSeconds)
        {
            float leftWeight = left.Plan.ConstraintState == FootConstraintState.Free
                ? 0f
                : left.Plan.PositionWeight * pelvisPolicyWeight;
            float rightWeight = right.Plan.ConstraintState == FootConstraintState.Free
                ? 0f
                : right.Plan.PositionWeight * pelvisPolicyWeight;
            float leftDelta = left.Plan.Position.y - pose.Left.AnklePosition.y;
            float rightDelta = right.Plan.Position.y - pose.Right.AnklePosition.y;
            float total = leftWeight + rightWeight;
            float desired = total > 0.0001f
                ? (leftDelta * leftWeight + rightDelta * rightWeight) / total
                : 0f;
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
            FootPlacementSupportFoot supportFoot = ResolveSupportFoot(
                leftWeight,
                rightWeight,
                left.Plan.Position.y,
                right.Plan.Position.y);
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
            desired = Mathf.Clamp(
                desired,
                -m_Settings.Pelvis.MaximumDownOffset,
                m_Settings.Pelvis.MaximumUpOffset);
            float previous = m_PelvisOffset;
            DecayCritical(
                ref m_PelvisOffset,
                ref m_PelvisVelocity,
                desired,
                m_Settings.Pelvis.HalfLifeSeconds,
                deltaSeconds);
            float maximumDelta = m_Settings.Pelvis.MaximumSpeed * deltaSeconds;
            m_PelvisOffset = Mathf.Clamp(m_PelvisOffset, previous - maximumDelta, previous + maximumDelta);
            return new PelvisResolution(desired, m_PelvisOffset, supportFoot, unreachableFoot);
        }

        ResolvedFoot MarkLegUnreachable(FootRuntimeState state, ResolvedFoot foot)
        {
            Release(state, FootConstraintTransitionReason.LegUnreachable);
            var plan = new FootPlacementFootPlan(
                foot.Plan.Side,
                foot.Plan.Position,
                foot.Plan.Rotation,
                foot.Plan.PositionWeight,
                foot.Plan.RotationWeight,
                FootConstraintState.Free,
                FootConstraintTransitionReason.LegUnreachable);
            return new ResolvedFoot(
                plan,
                foot.Pose,
                foot.Kinematics,
                foot.Prediction,
                foot.Support,
                foot.LockError,
                foot.ReplantError,
                foot.Weight,
                foot.SurfaceIdentity);
        }

        Quaternion ResolveFootRotation(
            Quaternion animatedRotation,
            Vector3 animatedForward,
            Vector3 supportNormal,
            float heightDelta,
            float policyWeight)
        {
            Quaternion inverseRoot = Quaternion.Inverse(m_Rig.VisualRoot.rotation);
            Vector3 localNormal = inverseRoot * supportNormal.normalized;
            float pitch = Mathf.Clamp(
                Mathf.Atan2(localNormal.z, Mathf.Max(0.0001f, localNormal.y)) * Mathf.Rad2Deg,
                -m_Settings.Rotation.MaximumPitchDegrees,
                m_Settings.Rotation.MaximumPitchDegrees);
            float roll = Mathf.Clamp(
                -Mathf.Atan2(localNormal.x, Mathf.Max(0.0001f, localNormal.y)) * Mathf.Rad2Deg,
                -m_Settings.Rotation.MaximumRollDegrees,
                m_Settings.Rotation.MaximumRollDegrees);
            Vector3 clampedNormal = m_Rig.VisualRoot.rotation *
                                    (Quaternion.Euler(pitch, 0f, roll) * Vector3.up);
            Vector3 forward = Vector3.ProjectOnPlane(animatedForward, clampedNormal).normalized;
            if (forward.sqrMagnitude <= 0.0001f)
                forward = Vector3.ProjectOnPlane(m_Rig.VisualRoot.forward, clampedNormal).normalized;
            Quaternion surfaceRotation = Quaternion.LookRotation(forward, clampedNormal);
            float alignment = heightDelta >= 0f
                ? m_Settings.Rotation.AscentSurfaceAlignment
                : m_Settings.Rotation.DescentSurfaceAlignment;
            return Quaternion.Slerp(animatedRotation, surfaceRotation, Mathf.Clamp01(alignment * policyWeight));
        }

        void BuildSnapshot(
            CharacterPosePostProcessFrame frame,
            ResolvedFoot left,
            ResolvedFoot right,
            PelvisResolution pelvis,
            CharacterFootPlacementSolverResult solverResult)
        {
            CharacterFootPlacementFrameSnapshot.CopyContributions(
                frame.AnimationContributions,
                m_DiagnosticContributions);
            m_Snapshot = new CharacterFootPlacementFrameSnapshot(
                m_ActorId,
                frame.RenderFrame,
                frame.Body.PreviousTick,
                frame.Body.CurrentTick,
                frame.Body.ResetSequence,
                m_Settings.PoseSourceLayerId,
                m_DiagnosticContributions,
                Mathf.Min(frame.AnimationContributions.Count, m_DiagnosticContributions.Length),
                BuildFootSnapshot(left),
                BuildFootSnapshot(right),
                pelvis.TargetOffset,
                pelvis.CurrentOffset,
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
                foot.Plan.Position,
                foot.Plan.Rotation);
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
                    LayerId = m_Settings.PoseSourceLayerId,
                    Status = $"L:{m_Snapshot.Left.ConstraintState}/R:{m_Snapshot.Right.ConstraintState}",
                    Detail = $"body={m_Snapshot.PreviousBodyTick}->{m_Snapshot.CurrentBodyTick};reset={m_Snapshot.ResetSequence};leftSurface={m_Snapshot.Left.SurfaceIdentity};rightSurface={m_Snapshot.Right.SurfaceIdentity};leftReason={m_Snapshot.Left.TransitionReason};rightReason={m_Snapshot.Right.TransitionReason};leftPrediction={m_Snapshot.Left.PredictionHorizon:0.####}/clamped:{m_Snapshot.Left.PredictionHorizonClamped}/reject:{m_Snapshot.Left.PredictionRejectReason};rightPrediction={m_Snapshot.Right.PredictionHorizon:0.####}/clamped:{m_Snapshot.Right.PredictionHorizonClamped}/reject:{m_Snapshot.Right.PredictionRejectReason};pelvis={m_Snapshot.PelvisCurrentOffset:0.####};support={m_Snapshot.SupportFoot};queries={m_Snapshot.Left.QueryCount + m_Snapshot.Right.QueryCount}",
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
            m_PelvisVelocity = 0f;
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

        static FootPlacementSupportFoot ResolveSupportFoot(float leftWeight, float rightWeight, float leftHeight, float rightHeight)
        {
            if (leftWeight <= 0.0001f && rightWeight <= 0.0001f)
                return FootPlacementSupportFoot.None;
            if (Mathf.Abs(leftWeight - rightWeight) <= 0.05f)
            {
                if (Mathf.Abs(leftHeight - rightHeight) <= 0.02f)
                    return FootPlacementSupportFoot.Both;
                return leftHeight > rightHeight ? FootPlacementSupportFoot.Left : FootPlacementSupportFoot.Right;
            }
            return leftWeight > rightWeight ? FootPlacementSupportFoot.Left : FootPlacementSupportFoot.Right;
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
            public ContactDecision(bool canPlant, bool shouldRelease)
            {
                CanPlant = canPlant;
                ShouldRelease = shouldRelease;
            }
            public bool CanPlant { get; }
            public bool ShouldRelease { get; }
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
            public ResolvedFoot(FootPlacementFootPlan plan, CharacterFootPlacementAnimatedFootPose pose, FootKinematics kinematics, PredictedFootprint prediction, FootPlacementSupportResult support, float lockError, float replantError, CharacterFootPlacementPolicyWeight weight, int surfaceIdentity)
            { Plan = plan; Pose = pose; Kinematics = kinematics; Prediction = prediction; Support = support; LockError = lockError; ReplantError = replantError; Weight = weight; SurfaceIdentity = surfaceIdentity; }
            public FootPlacementFootPlan Plan { get; }
            public CharacterFootPlacementAnimatedFootPose Pose { get; }
            public FootKinematics Kinematics { get; }
            public PredictedFootprint Prediction { get; }
            public FootPlacementSupportResult Support { get; }
            public float LockError { get; }
            public float ReplantError { get; }
            public CharacterFootPlacementPolicyWeight Weight { get; }
            public int SurfaceIdentity { get; }
        }

        readonly struct VerticalInterval
        {
            public VerticalInterval(float minimum, float maximum) { Minimum = minimum; Maximum = maximum; }
            public float Minimum { get; }
            public float Maximum { get; }
        }

        readonly struct PelvisResolution
        {
            public PelvisResolution(float targetOffset, float currentOffset, FootPlacementSupportFoot supportFoot, CharacterFootSide unreachableFoot)
            { TargetOffset = targetOffset; CurrentOffset = currentOffset; SupportFoot = supportFoot; UnreachableFoot = unreachableFoot; }
            public float TargetOffset { get; }
            public float CurrentOffset { get; }
            public FootPlacementSupportFoot SupportFoot { get; }
            public CharacterFootSide UnreachableFoot { get; }
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
