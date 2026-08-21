using System;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public enum CharacterFootSwingMotionState : byte
    {
        None = 0,
        Rejected = 1,
        Accepted = 2
    }

    public enum CharacterFootSwingMotionRejectReason : byte
    {
        None = 0,
        StepUnavailable = 1,
        StepNotSwing = 2,
        InvalidComponentUp = 3,
        InvalidWeight = 4,
        GroundPathRejected = 5,
        UnreachableEdge = 6,
        LandingEventMismatch = 7,
        InvalidEnvelope = 8,
        EnvelopeEndpointMismatch = 9,
        EnvelopeUnordered = 10,
        DegeneratePath = 11,
        EnvelopeSampleUnavailable = 12,
        NegativeVerticalCorrection = 13,
        InvalidSwingPhase = 14,
        UnselectedSwing = 15
    }

    public readonly struct CharacterFootSwingMotionDiagnostics
    {
        internal CharacterFootSwingMotionDiagnostics(
            CharacterFootSwingMotionState state,
            CharacterFootSwingMotionRejectReason rejectReason,
            ulong landingEventIdentity,
            ulong groundPathInputIdentity,
            Vector3 originalSole,
            Vector3 originalAnkle,
            float distance,
            float progress,
            Vector3 baselineSample,
            Vector3 envelopeSample,
            float verticalCorrection,
            float landingPredictionError,
            float landingConstraintWeight,
            Vector3 correctedSole,
            Vector3 correctedAnkle,
            float positionWeight,
            float rotationWeight,
            CharacterFootSupportLockState supportLockState = CharacterFootSupportLockState.None,
            float supportHorizontalError = 0f,
            float supportLockPreparationStartTimeToLandingSeconds = 0f,
            float supportLockPreparationWeight = 0f,
            float supportConstraintWeight = 0f,
            float supportWeight = 0f,
            Vector3 supportContactAnchor = default)
        {
            State = state;
            RejectReason = rejectReason;
            LandingEventIdentity = landingEventIdentity;
            GroundPathInputIdentity = groundPathInputIdentity;
            OriginalSole = originalSole;
            OriginalAnkle = originalAnkle;
            Distance = distance;
            Progress = progress;
            BaselineSample = baselineSample;
            EnvelopeSample = envelopeSample;
            VerticalCorrection = verticalCorrection;
            LandingPredictionError = landingPredictionError;
            LandingConstraintWeight = landingConstraintWeight;
            CorrectedSole = correctedSole;
            CorrectedAnkle = correctedAnkle;
            PositionWeight = positionWeight;
            RotationWeight = rotationWeight;
            SupportLockState = supportLockState;
            SupportHorizontalError = supportHorizontalError;
            SupportLockPreparationStartTimeToLandingSeconds =
                supportLockPreparationStartTimeToLandingSeconds;
            SupportLockPreparationWeight = supportLockPreparationWeight;
            SupportConstraintWeight = supportConstraintWeight;
            SupportWeight = supportWeight;
            SupportContactAnchor = supportContactAnchor;
        }

        public CharacterFootSwingMotionState State { get; }
        public CharacterFootSwingMotionRejectReason RejectReason { get; }
        public ulong LandingEventIdentity { get; }
        public ulong GroundPathInputIdentity { get; }
        public Vector3 OriginalSole { get; }
        public Vector3 OriginalAnkle { get; }
        public float Distance { get; }
        public float Progress { get; }
        public Vector3 BaselineSample { get; }
        public Vector3 EnvelopeSample { get; }
        public float VerticalCorrection { get; }
        public float LandingPredictionError { get; }
        public float LandingConstraintWeight { get; }
        public Vector3 CorrectedSole { get; }
        public Vector3 CorrectedAnkle { get; }
        public float PositionWeight { get; }
        public float RotationWeight { get; }
        public CharacterFootSupportLockState SupportLockState { get; }
        public float SupportHorizontalError { get; }
        public float SupportLockPreparationStartTimeToLandingSeconds { get; }
        public float SupportLockPreparationWeight { get; }
        public float SupportConstraintWeight { get; }
        public float SupportWeight { get; }
        public Vector3 SupportContactAnchor { get; }
        public bool Accepted => State == CharacterFootSwingMotionState.Accepted;

        internal CharacterFootSwingMotionDiagnostics WithResolvedOutput(
            Vector3 correctedSole,
            Vector3 correctedAnkle,
            Vector3 componentUp,
            float positionWeight,
            float rotationWeight) =>
            new CharacterFootSwingMotionDiagnostics(
                State,
                RejectReason,
                LandingEventIdentity,
                GroundPathInputIdentity,
                OriginalSole,
                OriginalAnkle,
                Distance,
                Progress,
                BaselineSample,
                EnvelopeSample,
                Vector3.Dot(
                    correctedAnkle - OriginalAnkle,
                    componentUp.normalized),
                LandingPredictionError,
                LandingConstraintWeight,
                correctedSole,
                correctedAnkle,
                positionWeight,
                rotationWeight,
                SupportLockState,
                SupportHorizontalError,
                SupportLockPreparationStartTimeToLandingSeconds,
                SupportLockPreparationWeight,
                SupportConstraintWeight,
                SupportWeight,
                SupportContactAnchor);
    }

    public enum CharacterFootSupportLockState : byte
    {
        None = 0,
        Acquiring = 1,
        Locked = 2,
        Sliding = 3,
        Releasing = 4
    }

    internal struct CharacterFootSupportLockFacts
    {
        internal bool HasValue;
        internal ulong LandingEventIdentity;
        internal CharacterFootSupportLockState State;
        internal Vector3 ContactAnchor;
        internal Vector3 OutputAnkle;
        internal Vector3 TransitionStartCorrection;
        internal float TransitionElapsedSeconds;
        internal float TransitionDurationSeconds;
        internal float PositionWeight;
        internal float ContactWeight;
        internal float SupportWeight;
        internal float TransitionStartContactWeight;
        internal float TransitionStartSupportWeight;

        internal void Clear()
        {
            HasValue = false;
            LandingEventIdentity = 0;
            State = CharacterFootSupportLockState.None;
            ContactAnchor = default;
            OutputAnkle = default;
            TransitionStartCorrection = default;
            TransitionElapsedSeconds = 0f;
            TransitionDurationSeconds = 0f;
            PositionWeight = 0f;
            ContactWeight = 0f;
            SupportWeight = 0f;
            TransitionStartContactWeight = 0f;
            TransitionStartSupportWeight = 0f;
        }
    }

    internal readonly struct CharacterFootContactFrame
    {
        internal CharacterFootContactFrame(
            CharacterFootPlacementAnimatedFootPose animatedFoot,
            in AnimationBiomechanicalStepHeader currentStep,
            in CharacterFootSwingMotionDiagnostics stableSwingMotion,
            bool hasLastLanding,
            in CharacterFootGroundPathLanding lastLanding,
            bool hardOwnershipLoss,
            float footPlacementWeight,
            Vector3 componentUp,
            float deltaSeconds,
            float landingPreparationStartTimeToLandingSeconds,
            float landingPreparationWeight,
            in CharacterFootMotionSettings settings)
        {
            AnimatedFoot = animatedFoot;
            CurrentStep = currentStep;
            StableSwingMotion = stableSwingMotion;
            HasLastLanding = hasLastLanding;
            LastLanding = lastLanding;
            HardOwnershipLoss = hardOwnershipLoss;
            FootPlacementWeight = footPlacementWeight;
            ComponentUp = componentUp;
            DeltaSeconds = deltaSeconds;
            LandingPreparationStartTimeToLandingSeconds =
                landingPreparationStartTimeToLandingSeconds;
            LandingPreparationWeight = landingPreparationWeight;
            Settings = settings;
        }

        internal CharacterFootPlacementAnimatedFootPose AnimatedFoot { get; }
        internal AnimationBiomechanicalStepHeader CurrentStep { get; }
        internal CharacterFootSwingMotionDiagnostics StableSwingMotion { get; }
        internal bool HasLastLanding { get; }
        internal CharacterFootGroundPathLanding LastLanding { get; }
        internal bool HardOwnershipLoss { get; }
        internal float FootPlacementWeight { get; }
        internal Vector3 ComponentUp { get; }
        internal float DeltaSeconds { get; }
        internal float LandingPreparationStartTimeToLandingSeconds { get; }
        internal float LandingPreparationWeight { get; }
        internal CharacterFootMotionSettings Settings { get; }
    }

    internal static class CharacterFootContactStateMachine
    {
        const float GeometryEpsilon = 0.0001f;

        internal static CharacterFootSwingMotionDiagnostics Resolve(
            in CharacterFootContactFrame frame,
            ref CharacterFootSupportLockFacts facts)
        {
            RequireValid(in frame);
            if (frame.HardOwnershipLoss)
            {
                facts.Clear();
                CharacterFootSwingMotionDiagnostics stableSwingMotion =
                    frame.StableSwingMotion;
                return CharacterFootSwingMotionBuilder.SuppressUnselected(
                    in stableSwingMotion);
            }
            if (!facts.HasValue)
            {
                return CanAcquire(in frame)
                    ? BeginAcquire(in frame, ref facts)
                    : frame.StableSwingMotion;
            }
            return facts.State switch
            {
                CharacterFootSupportLockState.Acquiring =>
                    ResolveAcquire(in frame, ref facts),
                CharacterFootSupportLockState.Locked =>
                    ResolveContact(in frame, false, ref facts),
                CharacterFootSupportLockState.Sliding =>
                    ResolveContact(in frame, true, ref facts),
                CharacterFootSupportLockState.Releasing =>
                    ResolveRelease(in frame, ref facts),
                _ => throw new InvalidOperationException("Foot Contact state is invalid.")
            };
        }

        static CharacterFootSwingMotionDiagnostics BeginAcquire(
            in CharacterFootContactFrame frame,
            ref CharacterFootSupportLockFacts facts)
        {
            Vector3 up = frame.ComponentUp.normalized;
            Vector3 originalSole = ResolveOriginalSole(frame.AnimatedFoot);
            Vector3 contactTarget = ResolveContactTarget(
                frame.AnimatedFoot,
                frame.LastLanding.Point);
            Vector3 initialOutput = ClampAboveContact(
                frame.StableSwingMotion.CorrectedAnkle,
                frame.AnimatedFoot,
                frame.LastLanding.Point,
                up);
            float horizontalError = Vector3.ProjectOnPlane(
                frame.LastLanding.Point - originalSole,
                up).magnitude;
            if (horizontalError > frame.Settings.SlideDistance)
                return frame.StableSwingMotion;
            facts.HasValue = true;
            facts.LandingEventIdentity = frame.LastLanding.LandingEventIdentity;
            facts.State = CharacterFootSupportLockState.Acquiring;
            facts.ContactAnchor = frame.LastLanding.Point;
            facts.OutputAnkle = initialOutput;
            facts.TransitionStartCorrection = initialOutput - contactTarget;
            facts.TransitionElapsedSeconds = 0f;
            facts.TransitionDurationSeconds = frame.Settings.ContactTransitionSeconds;
            facts.PositionWeight = frame.FootPlacementWeight;
            facts.ContactWeight = 0f;
            facts.SupportWeight = 0f;
            return CreateContactMotion(
                in frame,
                in facts,
                initialOutput,
                CharacterFootSupportLockState.Acquiring,
                horizontalError,
                0f,
                0f);
        }

        static CharacterFootSwingMotionDiagnostics ResolveAcquire(
            in CharacterFootContactFrame frame,
            ref CharacterFootSupportLockFacts facts)
        {
            if (ShouldRelease(in frame))
                return BeginRelease(in frame, ref facts);
            Vector3 up = frame.ComponentUp.normalized;
            Vector3 originalSole = ResolveOriginalSole(frame.AnimatedFoot);
            Vector3 contactTarget = ResolveContactTarget(
                frame.AnimatedFoot,
                facts.ContactAnchor);
            float horizontalError = Vector3.ProjectOnPlane(
                facts.ContactAnchor - originalSole,
                up).magnitude;
            if (horizontalError > frame.Settings.SlideDistance)
                return BeginRelease(in frame, ref facts);
            float progress = AdvanceTransition(in frame, ref facts);
            float blend = Smooth(progress);
            Vector3 output = contactTarget +
                             facts.TransitionStartCorrection * (1f - blend);
            output = ClampAboveContact(
                output,
                frame.AnimatedFoot,
                facts.ContactAnchor,
                up);
            facts.OutputAnkle = output;
            if (progress >= 1f - GeometryEpsilon ||
                Vector3.Distance(
                    ResolveOutputSole(frame.AnimatedFoot, output),
                    facts.ContactAnchor) <= frame.Settings.LandingUpdateDistance)
            {
                facts.State = CharacterFootSupportLockState.Locked;
                facts.OutputAnkle = contactTarget;
                facts.TransitionStartCorrection = default;
                facts.TransitionElapsedSeconds = 0f;
                facts.TransitionDurationSeconds = 0f;
                facts.ContactWeight = 1f;
                facts.SupportWeight = 1f;
                return CreateContactMotion(
                    in frame,
                    in facts,
                    contactTarget,
                    CharacterFootSupportLockState.Locked,
                    horizontalError,
                    1f,
                    1f);
            }
            facts.ContactWeight = blend;
            facts.SupportWeight = 0f;
            return CreateContactMotion(
                in frame,
                in facts,
                output,
                CharacterFootSupportLockState.Acquiring,
                horizontalError,
                blend,
                0f);
        }

        static CharacterFootSwingMotionDiagnostics ResolveContact(
            in CharacterFootContactFrame frame,
            bool wasSliding,
            ref CharacterFootSupportLockFacts facts)
        {
            if (ShouldRelease(in frame))
                return BeginRelease(in frame, ref facts);
            Vector3 up = frame.ComponentUp.normalized;
            Vector3 originalSole = ResolveOriginalSole(frame.AnimatedFoot);
            Vector3 correction = facts.ContactAnchor - originalSole;
            Vector3 horizontal = Vector3.ProjectOnPlane(correction, up);
            float horizontalError = horizontal.magnitude;
            if (horizontalError > frame.Settings.SlideDistance)
                return BeginRelease(in frame, ref facts);
            bool sliding = horizontalError > frame.Settings.LockDistance;
            if (wasSliding && horizontalError <= frame.Settings.LockDistance)
                sliding = false;
            float horizontalWeight = sliding
                ? Mathf.InverseLerp(
                    frame.Settings.SlideDistance,
                    frame.Settings.LockDistance,
                    horizontalError)
                : 1f;
            Vector3 target = frame.AnimatedFoot.AnklePosition +
                             horizontal * horizontalWeight +
                             up * Vector3.Dot(correction, up);
            facts.State = sliding
                ? CharacterFootSupportLockState.Sliding
                : CharacterFootSupportLockState.Locked;
            facts.OutputAnkle = target;
            facts.ContactWeight = 1f;
            facts.SupportWeight = 1f;
            return CreateContactMotion(
                in frame,
                in facts,
                target,
                facts.State,
                horizontalError,
                1f,
                1f);
        }

        static CharacterFootSwingMotionDiagnostics BeginRelease(
            in CharacterFootContactFrame frame,
            ref CharacterFootSupportLockFacts facts)
        {
            Vector3 stableTarget = frame.StableSwingMotion.CorrectedAnkle;
            float startContactWeight = facts.ContactWeight;
            float startSupportWeight = facts.SupportWeight;
            facts.State = CharacterFootSupportLockState.Releasing;
            facts.TransitionStartCorrection = facts.OutputAnkle - stableTarget;
            facts.TransitionElapsedSeconds = 0f;
            facts.TransitionDurationSeconds = frame.Settings.ContactTransitionSeconds;
            facts.TransitionStartContactWeight = startContactWeight;
            facts.TransitionStartSupportWeight = startSupportWeight;
            return CreateContactMotion(
                in frame,
                in facts,
                facts.OutputAnkle,
                CharacterFootSupportLockState.Releasing,
                ResolveHorizontalError(in frame, in facts),
                startContactWeight,
                startSupportWeight);
        }

        static CharacterFootSwingMotionDiagnostics ResolveRelease(
            in CharacterFootContactFrame frame,
            ref CharacterFootSupportLockFacts facts)
        {
            float progress = AdvanceTransition(in frame, ref facts);
            float blend = Smooth(progress);
            Vector3 stableTarget = frame.StableSwingMotion.CorrectedAnkle;
            Vector3 output = stableTarget +
                             facts.TransitionStartCorrection * (1f - blend);
            float horizontalError = ResolveHorizontalError(in frame, in facts);
            facts.OutputAnkle = output;
            if (progress >= 1f - GeometryEpsilon)
            {
                facts.Clear();
                return frame.StableSwingMotion;
            }
            facts.ContactWeight = facts.TransitionStartContactWeight * (1f - blend);
            facts.SupportWeight = facts.TransitionStartSupportWeight * (1f - blend);
            return CreateContactMotion(
                in frame,
                in facts,
                output,
                CharacterFootSupportLockState.Releasing,
                horizontalError,
                facts.ContactWeight,
                facts.SupportWeight);
        }

        static CharacterFootSwingMotionDiagnostics CreateContactMotion(
            in CharacterFootContactFrame frame,
            in CharacterFootSupportLockFacts facts,
            Vector3 outputAnkle,
            CharacterFootSupportLockState state,
            float horizontalError,
            float constraintWeight,
            float supportWeight)
        {
            Vector3 originalSole = ResolveOriginalSole(frame.AnimatedFoot);
            Vector3 originalAnkle = frame.AnimatedFoot.AnklePosition;
            Vector3 outputSole = ResolveOutputSole(frame.AnimatedFoot, outputAnkle);
            return new CharacterFootSwingMotionDiagnostics(
                CharacterFootSwingMotionState.Accepted,
                CharacterFootSwingMotionRejectReason.None,
                facts.LandingEventIdentity,
                frame.StableSwingMotion.GroundPathInputIdentity,
                originalSole,
                originalAnkle,
                frame.StableSwingMotion.Distance,
                frame.StableSwingMotion.Progress,
                frame.StableSwingMotion.BaselineSample,
                frame.StableSwingMotion.EnvelopeSample,
                Vector3.Dot(outputAnkle - originalAnkle, frame.ComponentUp.normalized),
                frame.StableSwingMotion.LandingPredictionError,
                frame.StableSwingMotion.LandingConstraintWeight,
                outputSole,
                outputAnkle,
                state == CharacterFootSupportLockState.Releasing
                    ? Mathf.Lerp(
                        frame.StableSwingMotion.PositionWeight,
                        facts.PositionWeight,
                        constraintWeight)
                    : facts.PositionWeight,
                0f,
                state,
                horizontalError,
                frame.LandingPreparationStartTimeToLandingSeconds,
                frame.LandingPreparationWeight,
                constraintWeight,
                supportWeight,
                facts.ContactAnchor);
        }

        static bool CanAcquire(in CharacterFootContactFrame frame) =>
            frame.HasLastLanding &&
            frame.LastLanding.LandingEventIdentity != 0 &&
            frame.CurrentStep.IsValid &&
            frame.CurrentStep.IsAuthoritative &&
            frame.CurrentStep.HasConsistentLandingEventIdentity &&
            frame.CurrentStep.ConstraintWeight > CharacterPoseConstraintMath.Epsilon &&
            frame.CurrentStep.SupportWeight > CharacterPoseConstraintMath.Epsilon &&
            frame.CurrentStep.LandingEventIdentity ==
            frame.LastLanding.LandingEventIdentity &&
            !IsExplicitRelease(frame.CurrentStep);

        static bool ShouldRelease(in CharacterFootContactFrame frame) =>
            IsExplicitRelease(frame.CurrentStep);

        static bool IsExplicitRelease(AnimationBiomechanicalStepHeader step) =>
            step.IsValid &&
            step.IsAuthoritative &&
            step.HasConsistentLandingEventIdentity &&
            step.ConstraintWeight < 1f - CharacterPoseConstraintMath.Epsilon &&
            (step.IsSwing ||
             step.IsPreSwing && step.EventPhase >= step.ReleasePhase);

        static float AdvanceTransition(
            in CharacterFootContactFrame frame,
            ref CharacterFootSupportLockFacts facts)
        {
            facts.TransitionElapsedSeconds = Mathf.Min(
                facts.TransitionDurationSeconds,
                facts.TransitionElapsedSeconds + frame.DeltaSeconds);
            return facts.TransitionDurationSeconds > GeometryEpsilon
                ? Mathf.Clamp01(
                    facts.TransitionElapsedSeconds /
                    facts.TransitionDurationSeconds)
                : 1f;
        }

        static float ResolveHorizontalError(
            in CharacterFootContactFrame frame,
            in CharacterFootSupportLockFacts facts) =>
            Vector3.ProjectOnPlane(
                facts.ContactAnchor - ResolveOriginalSole(frame.AnimatedFoot),
                frame.ComponentUp.normalized).magnitude;

        static Vector3 ResolveContactTarget(
            CharacterFootPlacementAnimatedFootPose foot,
            Vector3 contactAnchor) =>
            foot.AnklePosition + contactAnchor - ResolveOriginalSole(foot);

        static Vector3 ClampAboveContact(
            Vector3 ankle,
            CharacterFootPlacementAnimatedFootPose foot,
            Vector3 contactAnchor,
            Vector3 up)
        {
            Vector3 sole = ResolveOutputSole(foot, ankle);
            float penetration = Vector3.Dot(contactAnchor - sole, up);
            return penetration > 0f ? ankle + up * penetration : ankle;
        }

        static Vector3 ResolveOriginalSole(CharacterFootPlacementAnimatedFootPose foot) =>
            (foot.HeelPosition + foot.ToePosition) * 0.5f;

        static Vector3 ResolveOutputSole(
            CharacterFootPlacementAnimatedFootPose foot,
            Vector3 outputAnkle) =>
            ResolveOriginalSole(foot) + outputAnkle - foot.AnklePosition;

        static float Smooth(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }

        static void RequireValid(in CharacterFootContactFrame frame)
        {
            if (!Finite(frame.ComponentUp) ||
                frame.ComponentUp.sqrMagnitude <= GeometryEpsilon ||
                !float.IsFinite(frame.FootPlacementWeight) ||
                frame.FootPlacementWeight < 0f || frame.FootPlacementWeight > 1f ||
                !float.IsFinite(frame.DeltaSeconds) || frame.DeltaSeconds < 0f)
            {
                throw new InvalidOperationException("Foot Contact frame is invalid.");
            }
        }

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) &&
            float.IsFinite(value.y) &&
            float.IsFinite(value.z);
    }

    internal static class CharacterFootSwingMotionBuilder
    {
        const float GeometryEpsilon = 0.0001f;
        const float EndpointTolerance = 0.005f;

        internal static CharacterFootSwingMotionDiagnostics Build(
            CharacterFootPlacementAnimatedFootPose animatedFoot,
            in AnimationBiomechanicalStepHeader step,
            float footPlacementWeight,
            Vector3 componentUp,
            in CharacterFootGroundPathDiagnostics groundPath,
            float landingPredictionError,
            float landingConstraintWeight,
            float landingPreparationStartTimeToLandingSeconds,
            float landingPreparationWeight)
        {
            Vector3 originalSole = (animatedFoot.HeelPosition + animatedFoot.ToePosition) * 0.5f;
            Vector3 originalAnkle = animatedFoot.AnklePosition;
            ulong landingEventIdentity = step.IsValid ? step.LandingEventIdentity : 0;
            if (!step.IsValid || !step.IsAuthoritative)
                return Rejected(
                    CharacterFootSwingMotionRejectReason.StepUnavailable,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            if (!step.IsSwing)
                return Rejected(
                    CharacterFootSwingMotionRejectReason.StepNotSwing,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            if (!step.HasConsistentLandingEventIdentity)
                return Rejected(
                    CharacterFootSwingMotionRejectReason.LandingEventMismatch,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            return BuildForSwing(
                animatedFoot,
                in step,
                landingEventIdentity,
                footPlacementWeight,
                componentUp,
                in groundPath,
                landingPredictionError,
                landingConstraintWeight,
                landingPreparationStartTimeToLandingSeconds,
                landingPreparationWeight);
        }

        internal static CharacterFootSwingMotionDiagnostics BuildForSwing(
            CharacterFootPlacementAnimatedFootPose animatedFoot,
            in AnimationBiomechanicalStepHeader step,
            ulong landingEventIdentity,
            float footPlacementWeight,
            Vector3 componentUp,
            in CharacterFootGroundPathDiagnostics groundPath,
            float landingPredictionError,
            float landingConstraintWeight,
            float landingPreparationStartTimeToLandingSeconds,
            float landingPreparationWeight)
        {
            Vector3 originalSole = (animatedFoot.HeelPosition + animatedFoot.ToePosition) * 0.5f;
            Vector3 originalAnkle = animatedFoot.AnklePosition;
            if (!Finite(componentUp) || componentUp.sqrMagnitude <= GeometryEpsilon)
                return Rejected(
                    CharacterFootSwingMotionRejectReason.InvalidComponentUp,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            if (!float.IsFinite(footPlacementWeight) || footPlacementWeight < 0f || footPlacementWeight > 1f)
                return Rejected(
                    CharacterFootSwingMotionRejectReason.InvalidWeight,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            if (!float.IsFinite(landingPredictionError) || landingPredictionError < 0f ||
                !float.IsFinite(landingConstraintWeight) ||
                landingConstraintWeight < 0f || landingConstraintWeight > 1f ||
                !float.IsFinite(landingPreparationStartTimeToLandingSeconds) ||
                landingPreparationStartTimeToLandingSeconds < 0f ||
                !float.IsFinite(landingPreparationWeight) ||
                landingPreparationWeight < 0f || landingPreparationWeight > 1f)
                return Rejected(
                    CharacterFootSwingMotionRejectReason.InvalidWeight,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            if (!TryResolveSwingPhaseWeight(in step, out float trajectoryProgress))
                return Rejected(
                    CharacterFootSwingMotionRejectReason.InvalidSwingPhase,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            if (!groundPath.Accepted)
                return Rejected(
                    groundPath.RejectReason == CharacterFootGroundPathRejectReason.UnreachableEdge
                        ? CharacterFootSwingMotionRejectReason.UnreachableEdge
                        : CharacterFootSwingMotionRejectReason.GroundPathRejected,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            if (groundPath.NextSwingLandingEventIdentity != landingEventIdentity)
                return Rejected(
                    CharacterFootSwingMotionRejectReason.LandingEventMismatch,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            if (groundPath.EnvelopeVertexCount < 2 ||
                !Finite(groundPath.LastLanding) ||
                !Finite(groundPath.NextSwingLanding))
                return Rejected(
                    CharacterFootSwingMotionRejectReason.InvalidEnvelope,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);

            Vector3 up = componentUp.normalized;
            Vector3 horizontal = Vector3.ProjectOnPlane(
                groundPath.NextSwingLanding - groundPath.LastLanding,
                up);
            float pathLength = horizontal.magnitude;
            if (!float.IsFinite(pathLength) || pathLength <= GeometryEpsilon)
                return Rejected(
                    CharacterFootSwingMotionRejectReason.DegeneratePath,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);
            if (Vector3.Distance(
                    groundPath.EnvelopeVertexAt(0).Position,
                    groundPath.LastLanding) > EndpointTolerance ||
                Vector3.Distance(
                    groundPath.EnvelopeVertexAt(groundPath.EnvelopeVertexCount - 1).Position,
                    groundPath.NextSwingLanding) > EndpointTolerance)
                return Rejected(
                    CharacterFootSwingMotionRejectReason.EnvelopeEndpointMismatch,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle);

            float progress = trajectoryProgress;
            float distance = pathLength * progress;
            Vector3 baselineSample = Vector3.Lerp(
                groundPath.LastLanding,
                groundPath.NextSwingLanding,
                progress);
            if (!TrySampleEnvelope(
                    groundPath,
                    progress,
                    out Vector3 envelopeSample,
                    out CharacterFootSwingMotionRejectReason sampleRejectReason))
                return Rejected(
                    sampleRejectReason,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle,
                    distance,
                    progress,
                    baselineSample);

            float envelopeFloorLift = Vector3.Dot(
                envelopeSample - originalSole,
                up);
            if (!float.IsFinite(envelopeFloorLift))
                return Rejected(
                    CharacterFootSwingMotionRejectReason.NegativeVerticalCorrection,
                    landingEventIdentity,
                    groundPath.InputIdentity,
                    originalSole,
                    originalAnkle,
                    distance,
                    progress,
                    baselineSample,
                    envelopeSample);
            float verticalCorrection = Mathf.Max(0f, envelopeFloorLift);
            float landingPlantHeight = Mathf.Max(
                0f,
                Vector3.Dot(groundPath.NextSwingLanding - originalSole, up));
            float preparedLandingCorrection =
                landingPlantHeight * landingPreparationWeight;
            verticalCorrection = Mathf.Max(
                verticalCorrection,
                preparedLandingCorrection);
            Vector3 correctedSole = originalSole + up * verticalCorrection;
            Vector3 correctedAnkle = originalAnkle + up * verticalCorrection;
            float positionWeight = footPlacementWeight;
            return new CharacterFootSwingMotionDiagnostics(
                CharacterFootSwingMotionState.Accepted,
                CharacterFootSwingMotionRejectReason.None,
                landingEventIdentity,
                groundPath.InputIdentity,
                originalSole,
                originalAnkle,
                distance,
                progress,
                baselineSample,
                envelopeSample,
                verticalCorrection,
                landingPredictionError,
                landingConstraintWeight,
                correctedSole,
                correctedAnkle,
                positionWeight,
                0f,
                supportLockPreparationStartTimeToLandingSeconds:
                    landingPreparationStartTimeToLandingSeconds,
                supportLockPreparationWeight: landingPreparationWeight);
        }

        internal static CharacterFootSwingMotionDiagnostics SuppressUnselected(
            in CharacterFootSwingMotionDiagnostics motion)
        {
            if (!motion.Accepted)
                return motion;
            return new CharacterFootSwingMotionDiagnostics(
                CharacterFootSwingMotionState.Rejected,
                CharacterFootSwingMotionRejectReason.UnselectedSwing,
                motion.LandingEventIdentity,
                motion.GroundPathInputIdentity,
                motion.OriginalSole,
                motion.OriginalAnkle,
                motion.Distance,
                motion.Progress,
                motion.BaselineSample,
                motion.EnvelopeSample,
                motion.VerticalCorrection,
                motion.LandingPredictionError,
                motion.LandingConstraintWeight,
                motion.OriginalSole,
                motion.OriginalAnkle,
                0f,
                0f);
        }

        static bool TryResolveSwingPhaseWeight(
            in AnimationBiomechanicalStepHeader step,
            out float weight)
        {
            weight = 0f;
            if (!float.IsFinite(step.EventPhase) ||
                !float.IsFinite(step.LiftOffPhase) ||
                !float.IsFinite(step.LandingPhase) ||
                step.LandingPhase <= step.LiftOffPhase)
                return false;
            float phase = Mathf.InverseLerp(
                step.LiftOffPhase,
                step.LandingPhase,
                step.EventPhase);
            weight = Mathf.SmoothStep(0f, 1f, phase);
            return float.IsFinite(weight);
        }

        static bool TrySampleEnvelope(
            in CharacterFootGroundPathDiagnostics groundPath,
            float progress,
            out Vector3 sample,
            out CharacterFootSwingMotionRejectReason rejectReason)
        {
            Vector3 previous = groundPath.EnvelopeVertexAt(0).Position;
            if (!Finite(previous))
            {
                sample = default;
                rejectReason = CharacterFootSwingMotionRejectReason.InvalidEnvelope;
                return false;
            }
            float totalLength = 0f;
            for (int i = 1; i < groundPath.EnvelopeVertexCount; i++)
            {
                Vector3 current = groundPath.EnvelopeVertexAt(i).Position;
                if (!Finite(current))
                {
                    sample = default;
                    rejectReason = CharacterFootSwingMotionRejectReason.InvalidEnvelope;
                    return false;
                }
                float segmentLength = Vector3.Distance(previous, current);
                if (!float.IsFinite(segmentLength))
                {
                    sample = default;
                    rejectReason = CharacterFootSwingMotionRejectReason.InvalidEnvelope;
                    return false;
                }
                totalLength += segmentLength;
                previous = current;
            }
            if (!float.IsFinite(totalLength) || totalLength <= GeometryEpsilon)
            {
                sample = default;
                rejectReason = CharacterFootSwingMotionRejectReason.DegeneratePath;
                return false;
            }

            float targetDistance = Mathf.Clamp01(progress) * totalLength;
            if (targetDistance >= totalLength - GeometryEpsilon)
            {
                sample = groundPath.EnvelopeVertexAt(
                    groundPath.EnvelopeVertexCount - 1).Position;
                rejectReason = CharacterFootSwingMotionRejectReason.None;
                return true;
            }

            float accumulatedLength = 0f;
            previous = groundPath.EnvelopeVertexAt(0).Position;
            for (int i = 1; i < groundPath.EnvelopeVertexCount; i++)
            {
                Vector3 current = groundPath.EnvelopeVertexAt(i).Position;
                float segmentLength = Vector3.Distance(previous, current);
                if (segmentLength <= GeometryEpsilon)
                {
                    previous = current;
                    continue;
                }
                if (targetDistance <= accumulatedLength + segmentLength)
                {
                    float t = Mathf.Clamp01(
                        (targetDistance - accumulatedLength) / segmentLength);
                    sample = Vector3.Lerp(previous, current, t);
                    rejectReason = Finite(sample)
                        ? CharacterFootSwingMotionRejectReason.None
                        : CharacterFootSwingMotionRejectReason.InvalidEnvelope;
                    return rejectReason == CharacterFootSwingMotionRejectReason.None;
                }
                accumulatedLength += segmentLength;
                previous = current;
            }
            sample = groundPath.EnvelopeVertexAt(
                groundPath.EnvelopeVertexCount - 1).Position;
            rejectReason = CharacterFootSwingMotionRejectReason.None;
            return true;
        }

        static CharacterFootSwingMotionDiagnostics Rejected(
            CharacterFootSwingMotionRejectReason reason,
            ulong landingEventIdentity,
            ulong groundPathInputIdentity,
            Vector3 originalSole,
            Vector3 originalAnkle,
            float distance = 0f,
            float progress = 0f,
            Vector3 baselineSample = default,
            Vector3 envelopeSample = default,
            float verticalCorrection = 0f,
            float landingPredictionError = 0f,
            float landingConstraintWeight = 0f) =>
            new CharacterFootSwingMotionDiagnostics(
                CharacterFootSwingMotionState.Rejected,
                reason,
                landingEventIdentity,
                groundPathInputIdentity,
                originalSole,
                originalAnkle,
                distance,
                progress,
                baselineSample,
                envelopeSample,
                verticalCorrection,
                landingPredictionError,
                landingConstraintWeight,
                originalSole,
                originalAnkle,
                0f,
                0f);

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
