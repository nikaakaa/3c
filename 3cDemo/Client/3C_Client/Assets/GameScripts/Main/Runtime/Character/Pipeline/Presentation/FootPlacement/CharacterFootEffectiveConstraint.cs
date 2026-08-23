using System;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal readonly struct CharacterFootEffectiveConstraintFrame
    {
        internal CharacterFootEffectiveConstraintFrame(
            CharacterFootPlacementAnimatedFootPose animatedFoot,
            in CharacterFootSwingMotionDiagnostics swingMotion,
            bool hasLanding,
            in CharacterFootGroundPathLanding landing,
            bool hardOwnershipLoss,
            float footPlacementWeight,
            Vector3 componentUp,
            float deltaSeconds,
            in CharacterFootMotionSettings settings)
        {
            AnimatedFoot = animatedFoot;
            SwingMotion = swingMotion;
            HasLanding = hasLanding;
            Landing = landing;
            HardOwnershipLoss = hardOwnershipLoss;
            FootPlacementWeight = footPlacementWeight;
            ComponentUp = componentUp;
            DeltaSeconds = deltaSeconds;
            Settings = settings;
        }

        internal CharacterFootPlacementAnimatedFootPose AnimatedFoot { get; }
        internal CharacterFootSwingMotionDiagnostics SwingMotion { get; }
        internal bool HasLanding { get; }
        internal CharacterFootGroundPathLanding Landing { get; }
        internal bool HardOwnershipLoss { get; }
        internal float FootPlacementWeight { get; }
        internal Vector3 ComponentUp { get; }
        internal float DeltaSeconds { get; }
        internal CharacterFootMotionSettings Settings { get; }
    }

    internal sealed class CharacterFootEffectiveConstraint
    {
        const float GeometryEpsilon = 0.0001f;

        CharacterFootEffectiveConstraintState m_Committed;
        CharacterFootEffectiveConstraintState m_Pending;
        bool m_HasPending;

        internal void BeginPending()
        {
            m_Pending = m_Committed;
            m_HasPending = true;
        }

        internal CharacterFootSwingMotionDiagnostics Resolve(
            in CharacterFootEffectiveConstraintFrame frame)
        {
            RequirePending();
            RequireValid(in frame);
            CharacterFootSwingMotionDiagnostics swing = frame.SwingMotion;
            float plantConfidence = swing.PlantConfidence;
            Vector3 swingCorrection = ResolveSwingCorrection(
                frame.AnimatedFoot,
                in swing);

            if (frame.HardOwnershipLoss)
            {
                m_Pending.Clear(
                    plantConfidence >=
                    AnimationFootConstraintFacts.GroundedMinimumConfidence);
                return CharacterFootSwingMotionBuilder.SuppressUnselected(in swing);
            }

            if (!m_Pending.HasOutput)
            {
                m_Pending.HasOutput = true;
                m_Pending.OutputCorrection = swingCorrection;
            }

            bool preserveOutput = false;
            Vector3 desiredCorrection = swingCorrection;
            switch (m_Pending.State)
            {
                case CharacterFootSupportLockState.None:
                    ResolveSwingOutput(
                        in frame,
                        in swing,
                        swingCorrection);
                    preserveOutput = true;
                    ResolveNone(
                        in frame,
                        ref desiredCorrection);
                    break;
                case CharacterFootSupportLockState.Acquiring:
                    ResolveAcquiringIntent(
                        in frame,
                        swingCorrection,
                        ref desiredCorrection,
                        ref preserveOutput);
                    break;
                case CharacterFootSupportLockState.Locked:
                case CharacterFootSupportLockState.Sliding:
                    ResolveContactIntent(
                        in frame,
                        swingCorrection,
                        ref desiredCorrection,
                        ref preserveOutput);
                    break;
                case CharacterFootSupportLockState.Releasing:
                    desiredCorrection = swingCorrection;
                    ResolveReleaseOutput(
                        in frame,
                        swingCorrection);
                    preserveOutput = true;
                    break;
                default:
                    throw new InvalidOperationException("Foot effective constraint state is invalid.");
            }

            if (!preserveOutput)
            {
                m_Pending.OutputCorrection =
                    m_Pending.State == CharacterFootSupportLockState.Locked
                        ? desiredCorrection
                        : Advance(
                            m_Pending.OutputCorrection,
                            desiredCorrection,
                            frame.DeltaSeconds,
                            frame.Settings.EffectiveCorrectionHalfLifeSeconds);
            }

            ResolveGroundFloor(
                in frame,
                in swing,
                swingCorrection);

            ResolveOutputState(
                in frame,
                swingCorrection);
            return BuildOutput(
                in frame,
                in swing,
                desiredCorrection);
        }

        void ResolveSwingOutput(
            in CharacterFootEffectiveConstraintFrame frame,
            in CharacterFootSwingMotionDiagnostics swing,
            Vector3 swingCorrection)
        {
            bool hasPath = swing.Accepted &&
                           frame.HasLanding &&
                           frame.Landing.LandingEventIdentity ==
                           swing.LandingEventIdentity;
            bool revised = hasPath != m_Pending.HasSwingPath ||
                           hasPath &&
                           (m_Pending.SwingLandingEventIdentity !=
                                swing.LandingEventIdentity ||
                            Vector3.Distance(
                                m_Pending.SwingLandingPoint,
                                frame.Landing.Point) >
                            frame.Settings.LandingUpdateDistance);
            if (revised)
            {
                m_Pending.SwingResidual =
                    m_Pending.OutputCorrection - swingCorrection;
            }

            m_Pending.HasSwingPath = hasPath;
            m_Pending.SwingLandingEventIdentity = hasPath
                ? swing.LandingEventIdentity
                : 0;
            m_Pending.SwingLandingPoint = hasPath
                ? frame.Landing.Point
                : default;
            m_Pending.SwingResidual = Advance(
                m_Pending.SwingResidual,
                default,
                frame.DeltaSeconds,
                frame.Settings.EffectiveCorrectionHalfLifeSeconds);
            m_Pending.OutputCorrection =
                swingCorrection + m_Pending.SwingResidual;
            if (swing.Accepted)
            {
                m_Pending.OutputCorrection = RaiseToFloor(
                    m_Pending.OutputCorrection,
                    swingCorrection,
                    frame.ComponentUp);
            }
            m_Pending.SwingResidual =
                m_Pending.OutputCorrection - swingCorrection;
        }

        void ResolveNone(
            in CharacterFootEffectiveConstraintFrame frame,
            ref Vector3 desiredCorrection)
        {
            float plantConfidence = frame.SwingMotion.PlantConfidence;
            if (plantConfidence <
                AnimationFootConstraintFacts.GroundedMinimumConfidence)
            {
                m_Pending.PlantCycleConsumed = false;
                return;
            }
            if (m_Pending.PlantCycleConsumed)
                return;

            m_Pending.PlantCycleConsumed = true;
            if (!CanAcquire(in frame))
                return;

            Vector3 contactCorrection = ResolveContactCorrection(
                frame.AnimatedFoot,
                frame.Landing.Point);
            float horizontalError = ResolveHorizontalError(
                contactCorrection,
                frame.ComponentUp);
            if (horizontalError > frame.Settings.LockDistance)
                return;

            m_Pending.HasContact = true;
            m_Pending.LandingEventIdentity = frame.Landing.LandingEventIdentity;
            m_Pending.ContactAnchor = frame.Landing.Point;
            m_Pending.State = CharacterFootSupportLockState.Acquiring;
            m_Pending.OutputCorrection = RaiseToFloor(
                m_Pending.OutputCorrection,
                contactCorrection,
                frame.ComponentUp);
            m_Pending.AcquireResidual =
                m_Pending.OutputCorrection - contactCorrection;
            m_Pending.ContactProgress = 0f;
            m_Pending.ReleaseStartResidual = 0f;
            desiredCorrection = contactCorrection;
        }

        void ResolveAcquiringIntent(
            in CharacterFootEffectiveConstraintFrame frame,
            Vector3 swingCorrection,
            ref Vector3 desiredCorrection,
            ref bool preserveOutput)
        {
            Vector3 contactCorrection = ResolveContactCorrection(
                frame.AnimatedFoot,
                m_Pending.ContactAnchor);
            float horizontalError = ResolveHorizontalError(
                contactCorrection,
                frame.ComponentUp);
            if (frame.SwingMotion.PlantConfidence <
                    AnimationFootConstraintFacts.GroundedMinimumConfidence ||
                horizontalError > frame.Settings.SlideDistance)
            {
                BeginRelease(swingCorrection);
                desiredCorrection = swingCorrection;
                preserveOutput = true;
                return;
            }

            m_Pending.ContactProgress = Mathf.Max(
                m_Pending.ContactProgress,
                ResolvePlantOwnership(frame.SwingMotion.PlantConfidence));
            desiredCorrection = contactCorrection;
            m_Pending.OutputCorrection = contactCorrection +
                                         m_Pending.AcquireResidual *
                                         (1f - m_Pending.ContactProgress);
            preserveOutput = true;
            if (m_Pending.ContactProgress >= 1f - GeometryEpsilon)
            {
                m_Pending.State = CharacterFootSupportLockState.Locked;
                m_Pending.OutputCorrection = contactCorrection;
            }
        }

        void ResolveContactIntent(
            in CharacterFootEffectiveConstraintFrame frame,
            Vector3 swingCorrection,
            ref Vector3 desiredCorrection,
            ref bool preserveOutput)
        {
            Vector3 fullCorrection = ResolveContactCorrection(
                frame.AnimatedFoot,
                m_Pending.ContactAnchor);
            float horizontalError = ResolveHorizontalError(
                fullCorrection,
                frame.ComponentUp);
            if (frame.SwingMotion.PlantConfidence <
                    AnimationFootConstraintFacts.LockedMinimumConfidence ||
                horizontalError > frame.Settings.SlideDistance)
            {
                BeginRelease(swingCorrection);
                desiredCorrection = swingCorrection;
                preserveOutput = true;
                return;
            }

            if (horizontalError > frame.Settings.LockDistance)
            {
                bool enteringSliding =
                    m_Pending.State != CharacterFootSupportLockState.Sliding;
                m_Pending.State = CharacterFootSupportLockState.Sliding;
                desiredCorrection = ResolveSlidingCorrection(
                    fullCorrection,
                    frame.ComponentUp,
                    horizontalError,
                    frame.Settings);
                preserveOutput = enteringSliding;
            }
            else
            {
                m_Pending.State = CharacterFootSupportLockState.Locked;
                desiredCorrection = fullCorrection;
                preserveOutput = false;
            }
        }

        void ResolveOutputState(
            in CharacterFootEffectiveConstraintFrame frame,
            Vector3 swingCorrection)
        {
            if (m_Pending.State != CharacterFootSupportLockState.Releasing)
                return;
            if (frame.SwingMotion.PlantConfidence >=
                    AnimationFootConstraintFacts.GroundedMinimumConfidence ||
                Vector3.Distance(
                    m_Pending.OutputCorrection,
                    swingCorrection) > frame.Settings.LandingUpdateDistance)
            {
                return;
            }
            Vector3 outputCorrection = m_Pending.OutputCorrection;
            m_Pending.Clear(false);
            m_Pending.HasOutput = true;
            m_Pending.OutputCorrection = outputCorrection;
        }

        void BeginRelease(Vector3 swingCorrection)
        {
            m_Pending.State = CharacterFootSupportLockState.Releasing;
            m_Pending.ReleaseTargetCorrection = swingCorrection;
            m_Pending.ReleaseResidual =
                m_Pending.OutputCorrection - swingCorrection;
            m_Pending.ReleaseStartResidual =
                m_Pending.ReleaseResidual.magnitude;
        }

        void ResolveReleaseOutput(
            in CharacterFootEffectiveConstraintFrame frame,
            Vector3 swingCorrection)
        {
            m_Pending.ReleaseResidual +=
                m_Pending.ReleaseTargetCorrection - swingCorrection;
            m_Pending.ReleaseTargetCorrection = swingCorrection;
            m_Pending.ReleaseResidual = Advance(
                m_Pending.ReleaseResidual,
                default,
                frame.DeltaSeconds,
                frame.Settings.EffectiveCorrectionHalfLifeSeconds);
            m_Pending.OutputCorrection =
                swingCorrection + m_Pending.ReleaseResidual;
        }

        CharacterFootSwingMotionDiagnostics BuildOutput(
            in CharacterFootEffectiveConstraintFrame frame,
            in CharacterFootSwingMotionDiagnostics swing,
            Vector3 desiredCorrection)
        {
            CharacterFootSupportLockState state = m_Pending.State;
            bool hasContact = m_Pending.HasContact;
            Vector3 outputCorrection = m_Pending.OutputCorrection;
            Vector3 originalSole = ResolveOriginalSole(frame.AnimatedFoot);
            Vector3 originalAnkle = frame.AnimatedFoot.AnklePosition;
            float horizontalError = hasContact
                ? Vector3.ProjectOnPlane(
                    m_Pending.ContactAnchor - originalSole,
                    frame.ComponentUp.normalized).magnitude
                : 0f;
            float contactOwnership = ResolveContactOwnership(state);
            float supportWeight = state switch
            {
                CharacterFootSupportLockState.Locked => 1f,
                CharacterFootSupportLockState.Sliding => 1f,
                CharacterFootSupportLockState.Releasing => contactOwnership,
                _ => 0f
            };
            float positionWeight = outputCorrection.sqrMagnitude >
                                   GeometryEpsilon * GeometryEpsilon
                ? frame.FootPlacementWeight
                : 0f;
            CharacterFootSwingMotionState outputState = hasContact
                ? CharacterFootSwingMotionState.Accepted
                : swing.State;
            CharacterFootSwingMotionRejectReason rejectReason = hasContact
                ? CharacterFootSwingMotionRejectReason.None
                : swing.RejectReason;
            ulong landingEventIdentity = hasContact
                ? m_Pending.LandingEventIdentity
                : swing.LandingEventIdentity;
            return new CharacterFootSwingMotionDiagnostics(
                outputState,
                rejectReason,
                landingEventIdentity,
                swing.GroundPathInputIdentity,
                originalSole,
                originalAnkle,
                swing.Distance,
                swing.Progress,
                swing.BaselineSample,
                swing.EnvelopeSample,
                Vector3.Dot(outputCorrection, frame.ComponentUp.normalized),
                swing.LandingPredictionError,
                swing.LandingConstraintWeight,
                originalSole + outputCorrection,
                originalAnkle + outputCorrection,
                positionWeight,
                0f,
                state,
                horizontalError,
                contactOwnership,
                supportWeight,
                hasContact ? m_Pending.ContactAnchor : default,
                swing.PlantConfidence,
                desiredCorrection);
        }

        float ResolveContactOwnership(CharacterFootSupportLockState state)
        {
            switch (state)
            {
                case CharacterFootSupportLockState.Acquiring:
                    return m_Pending.ContactProgress;
                case CharacterFootSupportLockState.Locked:
                case CharacterFootSupportLockState.Sliding:
                    return 1f;
                case CharacterFootSupportLockState.Releasing:
                {
                    if (m_Pending.ReleaseStartResidual <= GeometryEpsilon)
                        return 0f;
                    float remaining = m_Pending.ReleaseResidual.magnitude;
                    return Mathf.Clamp01(
                        remaining / m_Pending.ReleaseStartResidual);
                }
                default:
                    return 0f;
            }
        }

        void ResolveGroundFloor(
            in CharacterFootEffectiveConstraintFrame frame,
            in CharacterFootSwingMotionDiagnostics swing,
            Vector3 swingCorrection)
        {
            Vector3 floorCorrection;
            switch (m_Pending.State)
            {
                case CharacterFootSupportLockState.None when swing.Accepted:
                    floorCorrection = swingCorrection;
                    break;
                case CharacterFootSupportLockState.Acquiring:
                case CharacterFootSupportLockState.Locked:
                case CharacterFootSupportLockState.Sliding:
                    floorCorrection = ResolveContactCorrection(
                        frame.AnimatedFoot,
                        m_Pending.ContactAnchor);
                    break;
                default:
                    return;
            }

            m_Pending.OutputCorrection = RaiseToFloor(
                m_Pending.OutputCorrection,
                floorCorrection,
                frame.ComponentUp);
        }

        static Vector3 RaiseToFloor(
            Vector3 outputCorrection,
            Vector3 floorCorrection,
            Vector3 componentUp)
        {
            Vector3 up = componentUp.normalized;
            float missing = Vector3.Dot(
                floorCorrection - outputCorrection,
                up);
            return missing > 0f
                ? outputCorrection + up * missing
                : outputCorrection;
        }

        static float ResolvePlantOwnership(float plantConfidence) =>
            Mathf.InverseLerp(
                AnimationFootConstraintFacts.GroundedMinimumConfidence,
                AnimationFootConstraintFacts.LockedMinimumConfidence,
                plantConfidence);

        static bool CanAcquire(in CharacterFootEffectiveConstraintFrame frame) =>
            frame.HasLanding &&
            frame.Landing.LandingEventIdentity != 0;

        static Vector3 ResolveSwingCorrection(
            CharacterFootPlacementAnimatedFootPose foot,
            in CharacterFootSwingMotionDiagnostics swing) =>
            swing.Accepted
                ? swing.CorrectedAnkle - foot.AnklePosition
                : default;

        static Vector3 ResolveContactCorrection(
            CharacterFootPlacementAnimatedFootPose foot,
            Vector3 contactAnchor) =>
            contactAnchor - ResolveOriginalSole(foot);

        static Vector3 ResolveSlidingCorrection(
            Vector3 fullCorrection,
            Vector3 componentUp,
            float horizontalError,
            CharacterFootMotionSettings settings)
        {
            Vector3 up = componentUp.normalized;
            Vector3 horizontal = Vector3.ProjectOnPlane(fullCorrection, up);
            float horizontalWeight = Mathf.InverseLerp(
                settings.SlideDistance,
                settings.LockDistance,
                horizontalError);
            return horizontal * horizontalWeight +
                   up * Vector3.Dot(fullCorrection, up);
        }

        static float ResolveHorizontalError(
            Vector3 correction,
            Vector3 componentUp) =>
            Vector3.ProjectOnPlane(
                correction,
                componentUp.normalized).magnitude;

        static Vector3 ResolveOriginalSole(
            CharacterFootPlacementAnimatedFootPose foot) =>
            (foot.HeelPosition + foot.ToePosition) * 0.5f;

        static Vector3 Advance(
            Vector3 current,
            Vector3 target,
            float deltaSeconds,
            float halfLifeSeconds)
        {
            if (deltaSeconds <= 0f)
                return current;
            float alpha = 1f - Mathf.Pow(
                0.5f,
                deltaSeconds / halfLifeSeconds);
            return Vector3.LerpUnclamped(current, target, alpha);
        }

        internal void Seal()
        {
            RequirePending();
            m_Committed = m_Pending;
            ClearPending();
        }

        internal void Discard()
        {
            ClearPending();
        }

        internal void Reset()
        {
            m_Committed = default;
            ClearPending();
        }

        void ClearPending()
        {
            m_Pending = default;
            m_HasPending = false;
        }

        void RequirePending()
        {
            if (!m_HasPending)
                throw new InvalidOperationException(
                    "Foot effective constraint has no pending frame.");
        }

        static void RequireValid(in CharacterFootEffectiveConstraintFrame frame)
        {
            if (!Finite(frame.ComponentUp) ||
                frame.ComponentUp.sqrMagnitude <= GeometryEpsilon ||
                !float.IsFinite(frame.FootPlacementWeight) ||
                frame.FootPlacementWeight < 0f ||
                frame.FootPlacementWeight > 1f ||
                !float.IsFinite(frame.DeltaSeconds) ||
                frame.DeltaSeconds < 0f ||
                !float.IsFinite(frame.SwingMotion.PlantConfidence) ||
                frame.SwingMotion.PlantConfidence < 0f ||
                frame.SwingMotion.PlantConfidence > 1f)
            {
                throw new InvalidOperationException(
                    "Foot effective constraint frame is invalid.");
            }
        }

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) &&
            float.IsFinite(value.y) &&
            float.IsFinite(value.z);

        struct CharacterFootEffectiveConstraintState
        {
            internal bool HasOutput;
            internal bool HasSwingPath;
            internal bool PlantCycleConsumed;
            internal bool HasContact;
            internal ulong SwingLandingEventIdentity;
            internal ulong LandingEventIdentity;
            internal CharacterFootSupportLockState State;
            internal Vector3 SwingLandingPoint;
            internal Vector3 SwingResidual;
            internal Vector3 ContactAnchor;
            internal Vector3 OutputCorrection;
            internal Vector3 AcquireResidual;
            internal Vector3 ReleaseTargetCorrection;
            internal Vector3 ReleaseResidual;
            internal float ContactProgress;
            internal float ReleaseStartResidual;

            internal void Clear(bool plantCycleConsumed)
            {
                this = default;
                PlantCycleConsumed = plantCycleConsumed;
            }
        }
    }
}
