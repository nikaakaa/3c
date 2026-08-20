using System;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public enum CharacterFootStrideState : byte
    {
        None = 0,
        Rejected = 1,
        Accepted = 2,
        Releasing = 3
    }

    public enum CharacterFootStrideRejectReason : byte
    {
        None = 0,
        DualSwing = 1,
        MissingSupportLanding = 2,
        MissingSwingLanding = 3,
        SwingIdentityMismatch = 4,
        InvalidComponentUp = 5,
        DegenerateStride = 6,
        BodyNotGrounded = 7,
        ActionOccupied = 8,
        GroundPathRejected = 9,
        InvalidInput = 10,
        SupportUnavailable = 11,
        SupportLegUnreachable = 12
    }

    public enum CharacterFootStrideSlope : byte
    {
        Flat = 0,
        Ascending = 1,
        Descending = 2
    }

    [Flags]
    public enum CharacterFootPelvisSpringHandoffReason : byte
    {
        None = 0,
        SupportChanged = 1,
        SlopeChanged = 2,
        TargetCrossedOutput = 4
    }

    public enum CharacterFootSupportLockState : byte
    {
        None = 0,
        Locked = 1,
        Sliding = 2,
        Unlocked = 3,
        SwingHandoff = 4
    }

    internal struct CharacterFootSupportLockFacts
    {
        internal bool HasValue;
        internal ulong LandingEventIdentity;
        internal CharacterFootSupportLockState State;
        internal Vector3 TargetAnkle;
        internal float PositionWeight;
        internal Vector3 UnlockStartCorrection;
        internal float UnlockStartPositionWeight;
        internal float UnlockRemainingSeconds;
        internal Vector3 OutputAnkle;
        internal Vector3 OutputVelocity;
        internal ulong SwingHandoffLandingEventIdentity;
        internal Vector3 SwingHandoffTargetAnkle;

        internal void ClearSupport()
        {
            HasValue = false;
            LandingEventIdentity = 0;
            State = CharacterFootSupportLockState.None;
            TargetAnkle = default;
            PositionWeight = 0f;
            UnlockStartCorrection = default;
            UnlockStartPositionWeight = 0f;
            UnlockRemainingSeconds = 0f;
            OutputAnkle = default;
            OutputVelocity = default;
            SwingHandoffLandingEventIdentity = 0;
            SwingHandoffTargetAnkle = default;
        }

        internal void Clear()
        {
            ClearSupport();
        }
    }

    internal struct CharacterFootLockPreparationFacts
    {
        internal ulong LandingEventIdentity;
        internal float StartTimeToLandingSeconds;
        internal float Weight;

        internal void Clear()
        {
            LandingEventIdentity = 0;
            StartTimeToLandingSeconds = 0f;
            Weight = 0f;
        }
    }

    internal struct CharacterFootPrimarySupportFacts
    {
        internal bool HasValue;
        internal CharacterFootSide Side;
        internal ulong LandingEventIdentity;
        internal bool Retained;

        internal void Clear()
        {
            HasValue = false;
            Side = default;
            LandingEventIdentity = 0;
            Retained = false;
        }

        internal CharacterFootPrimarySupportDiagnostics Diagnostics =>
            new CharacterFootPrimarySupportDiagnostics(
                HasValue,
                Side,
                LandingEventIdentity,
                Retained);
    }

    public readonly struct CharacterFootPrimarySupportDiagnostics
    {
        internal CharacterFootPrimarySupportDiagnostics(
            bool hasValue,
            CharacterFootSide side,
            ulong landingEventIdentity,
            bool retained)
        {
            HasValue = hasValue;
            Side = side;
            LandingEventIdentity = landingEventIdentity;
            Retained = retained;
        }

        public bool HasValue { get; }
        public CharacterFootSide Side { get; }
        public ulong LandingEventIdentity { get; }
        public bool Retained { get; }
    }

    public readonly struct CharacterFootStrideHipsDiagnostics
    {
        internal CharacterFootStrideHipsDiagnostics(
            CharacterFootStrideState state,
            CharacterFootStrideRejectReason rejectReason,
            CharacterFootSide supportSide,
            CharacterFootSide swingSide,
            Vector3 strideStart,
            Vector3 strideEnd,
            float progress,
            CharacterFootStrideSlope slope,
            Vector3 sampledGround,
            Vector3 poseRootPosition,
            Vector3 animatedPelvis,
            Vector3 animatedPelvisComponentPosition,
            Vector3 rawPelvisDelta,
            float rootRelativeGroundTargetAlongUp,
            float soleClearanceLiftAlongUp,
            bool hadPreviousState,
            bool supportChanged,
            CharacterFootStrideSlope previousSlope,
            CharacterFootPelvisSpringHandoffReason springHandoffReason,
            bool springVelocityReset,
            float previousSpringTarget,
            float previousSpringOutput,
            float previousSpringVelocity,
            float springInput,
            float springInputVelocity,
            float springFrequency,
            float unclampedSpringTarget,
            bool supportReachAvailable,
            float supportLegCompressionReserve,
            float supportReachUsableLegLength,
            float supportReachMinimumAlongUp,
            float supportReachMaximumAlongUp,
            bool supportReachTargetClamped,
            bool supportReachOutputClamped,
            float springTarget,
            float springOutput,
            float springVelocity,
            Vector3 pelvisDelta,
            float positionWeight)
        {
            State = state;
            RejectReason = rejectReason;
            SupportSide = supportSide;
            SwingSide = swingSide;
            StrideStart = strideStart;
            StrideEnd = strideEnd;
            Progress = progress;
            Slope = slope;
            SampledGround = sampledGround;
            PoseRootPosition = poseRootPosition;
            AnimatedPelvis = animatedPelvis;
            AnimatedPelvisComponentPosition = animatedPelvisComponentPosition;
            RawPelvisDelta = rawPelvisDelta;
            RootRelativeGroundTargetAlongUp = rootRelativeGroundTargetAlongUp;
            SoleClearanceLiftAlongUp = soleClearanceLiftAlongUp;
            HadPreviousState = hadPreviousState;
            SupportChanged = supportChanged;
            PreviousSlope = previousSlope;
            SpringHandoffReason = springHandoffReason;
            SpringVelocityReset = springVelocityReset;
            PreviousSpringTarget = previousSpringTarget;
            PreviousSpringOutput = previousSpringOutput;
            PreviousSpringVelocity = previousSpringVelocity;
            SpringInput = springInput;
            SpringInputVelocity = springInputVelocity;
            SpringFrequency = springFrequency;
            UnclampedSpringTarget = unclampedSpringTarget;
            SupportReachAvailable = supportReachAvailable;
            SupportLegCompressionReserve = supportLegCompressionReserve;
            SupportReachUsableLegLength = supportReachUsableLegLength;
            SupportReachMinimumAlongUp = supportReachMinimumAlongUp;
            SupportReachMaximumAlongUp = supportReachMaximumAlongUp;
            SupportReachTargetClamped = supportReachTargetClamped;
            SupportReachOutputClamped = supportReachOutputClamped;
            SpringTarget = springTarget;
            SpringOutput = springOutput;
            SpringVelocity = springVelocity;
            PelvisDelta = pelvisDelta;
            PositionWeight = positionWeight;
        }

        public CharacterFootStrideState State { get; }
        public CharacterFootStrideRejectReason RejectReason { get; }
        public CharacterFootSide SupportSide { get; }
        public CharacterFootSide SwingSide { get; }
        public Vector3 StrideStart { get; }
        public Vector3 StrideEnd { get; }
        public float Progress { get; }
        public CharacterFootStrideSlope Slope { get; }
        public Vector3 SampledGround { get; }
        public Vector3 PoseRootPosition { get; }
        public Vector3 AnimatedPelvis { get; }
        public Vector3 AnimatedPelvisComponentPosition { get; }
        public Vector3 RawPelvisDelta { get; }
        public float RootRelativeGroundTargetAlongUp { get; }
        public float SoleClearanceLiftAlongUp { get; }
        public bool HadPreviousState { get; }
        public bool SupportChanged { get; }
        public CharacterFootStrideSlope PreviousSlope { get; }
        public CharacterFootPelvisSpringHandoffReason SpringHandoffReason { get; }
        public bool SpringVelocityReset { get; }
        public float PreviousSpringTarget { get; }
        public float PreviousSpringOutput { get; }
        public float PreviousSpringVelocity { get; }
        public float SpringInput { get; }
        public float SpringInputVelocity { get; }
        public float SpringFrequency { get; }
        public float UnclampedSpringTarget { get; }
        public bool SupportReachAvailable { get; }
        public float SupportLegCompressionReserve { get; }
        public float SupportReachUsableLegLength { get; }
        public float SupportReachMinimumAlongUp { get; }
        public float SupportReachMaximumAlongUp { get; }
        public bool SupportReachTargetClamped { get; }
        public bool SupportReachOutputClamped { get; }
        public float SpringTarget { get; }
        public float SpringOutput { get; }
        public float SpringVelocity { get; }
        public Vector3 PelvisDelta { get; }
        public float PositionWeight { get; }
        public bool Accepted => State == CharacterFootStrideState.Accepted;
        public bool ProducesPelvisGoal =>
            State == CharacterFootStrideState.Accepted ||
            State == CharacterFootStrideState.Releasing;
    }

    internal struct CharacterFootPelvisSpringState
    {
        internal bool HasValue;
        internal CharacterFootSide SupportSide;
        internal ulong SupportLandingEventIdentity;
        internal CharacterFootStrideSlope Slope;
        internal float TargetAlongUp;
        internal float OutputAlongUp;
        internal float VelocityAlongUp;

        internal void Clear()
        {
            HasValue = false;
            SupportSide = default;
            SupportLandingEventIdentity = 0;
            Slope = CharacterFootStrideSlope.Flat;
            TargetAlongUp = 0f;
            OutputAlongUp = 0f;
            VelocityAlongUp = 0f;
        }
    }

    internal static class CharacterFootStrideHipsBuilder
    {
        const float GeometryEpsilon = 0.0001f;
        const float EndpointTolerance = 0.005f;

        internal static float PrepareLandingLock(
            in AnimationBiomechanicalStepHeader step,
            bool hasNextSwingLanding,
            ulong nextSwingLandingEventIdentity,
            ref CharacterFootLockPreparationFacts preparation)
        {
            bool valid = step.IsValid &&
                         step.IsAuthoritative &&
                         step.HasConsistentLandingEventIdentity &&
                         (step.IsPreSwing || step.IsSwing) &&
                         step.TimeToLandingSeconds > GeometryEpsilon &&
                         hasNextSwingLanding &&
                         nextSwingLandingEventIdentity == step.LandingEventIdentity;
            if (!valid)
                return 0f;

            if (preparation.LandingEventIdentity != step.LandingEventIdentity)
            {
                preparation.LandingEventIdentity = step.LandingEventIdentity;
                preparation.StartTimeToLandingSeconds = step.TimeToLandingSeconds;
                preparation.Weight = 0f;
            }

            float start = preparation.StartTimeToLandingSeconds;
            float candidate = start <= GeometryEpsilon
                ? 1f
                : Mathf.Clamp01(1f - step.TimeToLandingSeconds / start);
            preparation.Weight = Mathf.Max(
                preparation.Weight,
                candidate);
            return preparation.Weight;
        }

        internal static void CompleteLandingLockPreparation(
            ulong landingEventIdentity,
            ref CharacterFootLockPreparationFacts preparation)
        {
            if (landingEventIdentity != 0 &&
                preparation.LandingEventIdentity == landingEventIdentity)
            {
                preparation.Weight = 1f;
            }
        }

        internal static void ResolvePrimarySupport(
            in CharacterFootSwingMotionDiagnostics leftMotion,
            in CharacterFootSwingMotionDiagnostics rightMotion,
            ref CharacterFootPrimarySupportFacts primarySupport)
        {
            bool leftRetainable = IsRetainablePrimarySupport(in leftMotion);
            bool rightRetainable = IsRetainablePrimarySupport(in rightMotion);
            if (primarySupport.HasValue)
            {
                bool retained = primarySupport.Side == CharacterFootSide.Left
                    ? leftRetainable &&
                      leftMotion.LandingEventIdentity == primarySupport.LandingEventIdentity
                    : rightRetainable &&
                      rightMotion.LandingEventIdentity == primarySupport.LandingEventIdentity;
                if (retained)
                {
                    primarySupport.Retained = true;
                    return;
                }
            }

            bool leftCandidate = IsAcquirablePrimarySupport(in leftMotion);
            bool rightCandidate = IsAcquirablePrimarySupport(in rightMotion);
            if (!leftCandidate && !rightCandidate)
            {
                primarySupport.Clear();
                return;
            }

            bool selectLeft = leftCandidate &&
                (!rightCandidate ||
                 leftMotion.SupportHorizontalError <= rightMotion.SupportHorizontalError);
            primarySupport.HasValue = true;
            primarySupport.Side = selectLeft
                ? CharacterFootSide.Left
                : CharacterFootSide.Right;
            primarySupport.LandingEventIdentity = selectLeft
                ? leftMotion.LandingEventIdentity
                : rightMotion.LandingEventIdentity;
            primarySupport.Retained = false;
        }

        internal static bool TrySelectSwing(
            in AnimationBiomechanicalStepHeader leftStep,
            in AnimationBiomechanicalStepHeader rightStep,
            in CharacterFootSwingMotionDiagnostics leftMotion,
            in CharacterFootSwingMotionDiagnostics rightMotion,
            out CharacterFootSide swingSide)
        {
            bool leftAuthoritativeSwing = IsAuthoritativeSwing(in leftStep);
            bool rightAuthoritativeSwing = IsAuthoritativeSwing(in rightStep);
            bool leftSwingCandidate = leftAuthoritativeSwing &&
                                 leftMotion.Accepted &&
                                 leftMotion.LandingEventIdentity == leftStep.LandingEventIdentity;
            bool rightSwingCandidate = rightAuthoritativeSwing &&
                                  rightMotion.Accepted &&
                                  rightMotion.LandingEventIdentity == rightStep.LandingEventIdentity;
            if (leftAuthoritativeSwing != rightAuthoritativeSwing)
            {
                swingSide = leftAuthoritativeSwing
                    ? CharacterFootSide.Left
                    : CharacterFootSide.Right;
                return true;
            }
            if (!leftAuthoritativeSwing || !leftSwingCandidate && !rightSwingCandidate)
            {
                swingSide = default;
                return false;
            }
            swingSide = leftSwingCandidate &&
                        (!rightSwingCandidate ||
                         Mathf.Abs(leftMotion.VerticalCorrection) >=
                         Mathf.Abs(rightMotion.VerticalCorrection))
                ? CharacterFootSide.Left
                : CharacterFootSide.Right;
            return true;
        }

        internal static bool TryResolveStride(
            in AnimationBiomechanicalStepHeader leftSwingStep,
            in AnimationBiomechanicalStepHeader rightSwingStep,
            bool hasSelectedSwing,
            CharacterFootSide selectedSwingSide,
            bool hasPrimarySupport,
            CharacterFootSide primarySupportSide,
            ulong primarySupportLandingEventIdentity,
            bool hasLeftLastLanding,
            Vector3 leftLastLanding,
            ulong leftLastLandingEventIdentity,
            bool hasRightLastLanding,
            Vector3 rightLastLanding,
            ulong rightLastLandingEventIdentity,
            bool hasLeftNextSwingLanding,
            Vector3 leftNextSwingLanding,
            ulong leftNextSwingEventIdentity,
            bool hasRightNextSwingLanding,
            Vector3 rightNextSwingLanding,
            ulong rightNextSwingEventIdentity,
            Vector3 componentUp,
            out CharacterFootSide supportSide,
            out CharacterFootSide swingSide,
            out Vector3 strideStart,
            out Vector3 strideEnd,
            out CharacterFootStrideRejectReason rejectReason)
        {
            supportSide = default;
            swingSide = default;
            strideStart = default;
            strideEnd = default;
            if (!hasSelectedSwing)
            {
                rejectReason = CharacterFootStrideRejectReason.MissingSwingLanding;
                return false;
            }
            if (!hasPrimarySupport ||
                primarySupportLandingEventIdentity == 0 ||
                primarySupportSide == selectedSwingSide)
            {
                rejectReason = CharacterFootStrideRejectReason.SupportUnavailable;
                return false;
            }
            if (selectedSwingSide == CharacterFootSide.Left)
            {
                if (!IsAuthoritativeSwing(in leftSwingStep))
                {
                    rejectReason = CharacterFootStrideRejectReason.MissingSwingLanding;
                    return false;
                }
                if (primarySupportSide != CharacterFootSide.Right)
                {
                    rejectReason = CharacterFootStrideRejectReason.SupportUnavailable;
                    return false;
                }
                if (!hasRightLastLanding || rightLastLandingEventIdentity == 0 ||
                    rightLastLandingEventIdentity != primarySupportLandingEventIdentity ||
                    !Finite(rightLastLanding))
                {
                    rejectReason = CharacterFootStrideRejectReason.MissingSupportLanding;
                    return false;
                }
                if (!hasLeftNextSwingLanding ||
                    leftNextSwingEventIdentity != leftSwingStep.LandingEventIdentity ||
                    !Finite(leftNextSwingLanding))
                {
                    rejectReason = leftNextSwingEventIdentity != 0 &&
                                   leftNextSwingEventIdentity != leftSwingStep.LandingEventIdentity
                        ? CharacterFootStrideRejectReason.SwingIdentityMismatch
                        : CharacterFootStrideRejectReason.MissingSwingLanding;
                    return false;
                }
                supportSide = CharacterFootSide.Right;
                swingSide = CharacterFootSide.Left;
                strideStart = rightLastLanding;
                strideEnd = leftNextSwingLanding;
            }
            else
            {
                if (!IsAuthoritativeSwing(in rightSwingStep))
                {
                    rejectReason = CharacterFootStrideRejectReason.MissingSwingLanding;
                    return false;
                }
                if (primarySupportSide != CharacterFootSide.Left)
                {
                    rejectReason = CharacterFootStrideRejectReason.SupportUnavailable;
                    return false;
                }
                if (!hasLeftLastLanding || leftLastLandingEventIdentity == 0 ||
                    leftLastLandingEventIdentity != primarySupportLandingEventIdentity ||
                    !Finite(leftLastLanding))
                {
                    rejectReason = CharacterFootStrideRejectReason.MissingSupportLanding;
                    return false;
                }
                if (!hasRightNextSwingLanding ||
                    rightNextSwingEventIdentity != rightSwingStep.LandingEventIdentity ||
                    !Finite(rightNextSwingLanding))
                {
                    rejectReason = rightNextSwingEventIdentity != 0 &&
                                   rightNextSwingEventIdentity != rightSwingStep.LandingEventIdentity
                        ? CharacterFootStrideRejectReason.SwingIdentityMismatch
                        : CharacterFootStrideRejectReason.MissingSwingLanding;
                    return false;
                }
                supportSide = CharacterFootSide.Left;
                swingSide = CharacterFootSide.Right;
                strideStart = leftLastLanding;
                strideEnd = rightNextSwingLanding;
            }
            if (!Finite(componentUp) || componentUp.sqrMagnitude <= GeometryEpsilon)
            {
                rejectReason = CharacterFootStrideRejectReason.InvalidComponentUp;
                return false;
            }
            rejectReason = CharacterFootStrideRejectReason.None;
            return true;
        }

        internal static CharacterFootSwingMotionDiagnostics BuildStancePlant(
            CharacterFootPlacementAnimatedFootPose animatedFoot,
            in AnimationBiomechanicalStepHeader step,
            float footPlacementWeight,
            Vector3 componentUp,
            bool hasLastLanding,
            Vector3 lastLanding,
            ulong lastLandingEventIdentity,
            float preparationStartTimeToLandingSeconds,
            float preparationWeight,
            in CharacterFootMotionSettings settings,
            float deltaSeconds,
            ref CharacterFootSupportLockFacts lockFacts)
        {
            Vector3 originalSole = (animatedFoot.HeelPosition + animatedFoot.ToePosition) * 0.5f;
            Vector3 originalAnkle = animatedFoot.AnklePosition;
            ulong landingEventIdentity = hasLastLanding ? lastLandingEventIdentity : 0;
            if (!step.IsValid || !step.IsAuthoritative)
                return RejectedPlant(
                    CharacterFootSwingMotionRejectReason.StepUnavailable,
                    landingEventIdentity,
                    originalSole,
                    originalAnkle);
            if (!Finite(componentUp) || componentUp.sqrMagnitude <= GeometryEpsilon)
                return RejectedPlant(
                    CharacterFootSwingMotionRejectReason.InvalidComponentUp,
                    landingEventIdentity,
                    originalSole,
                    originalAnkle);
            if (!float.IsFinite(footPlacementWeight) || footPlacementWeight < 0f || footPlacementWeight > 1f)
                return RejectedPlant(
                    CharacterFootSwingMotionRejectReason.InvalidWeight,
                    landingEventIdentity,
                    originalSole,
                    originalAnkle);
            if (!hasLastLanding || landingEventIdentity == 0 || !Finite(lastLanding))
                return RejectedPlant(
                    CharacterFootSwingMotionRejectReason.StanceLandingUnavailable,
                    landingEventIdentity,
                    originalSole,
                    originalAnkle);
            Vector3 up = componentUp.normalized;
            float plant = Vector3.Dot(lastLanding - originalSole, up);
            if (!float.IsFinite(plant))
                return RejectedPlant(
                    CharacterFootSwingMotionRejectReason.StanceLandingUnavailable,
                    landingEventIdentity,
                    originalSole,
                    originalAnkle);
            plant = Mathf.Max(0f, plant);
            Vector3 horizontalOffset = Vector3.ProjectOnPlane(lastLanding - originalSole, up);
            float horizontalError = horizontalOffset.magnitude;
            if (!float.IsFinite(horizontalError))
                return RejectedPlant(
                    CharacterFootSwingMotionRejectReason.StanceLandingUnavailable,
                    landingEventIdentity,
                    originalSole,
                    originalAnkle);
            bool sameLockEvent = lockFacts.HasValue &&
                                 lockFacts.LandingEventIdentity == landingEventIdentity;
            if (sameLockEvent &&
                lockFacts.State == CharacterFootSupportLockState.Unlocked)
            {
                return ContinueSupportUnlock(
                    landingEventIdentity,
                    originalSole,
                    originalAnkle,
                    up,
                    horizontalError,
                    preparationStartTimeToLandingSeconds,
                    preparationWeight,
                    in settings,
                    deltaSeconds,
                    false,
                    ref lockFacts);
            }
            if (horizontalError > settings.SlideDistance)
            {
                if (!sameLockEvent)
                {
                    lockFacts.ClearSupport();
                    return RejectedPlant(
                        CharacterFootSwingMotionRejectReason.SupportOutsideSlideDistance,
                        landingEventIdentity,
                        originalSole,
                        originalAnkle);
                }
                BeginSupportUnlock(originalAnkle, in settings, ref lockFacts);
                return ContinueSupportUnlock(
                    landingEventIdentity,
                    originalSole,
                    originalAnkle,
                    up,
                    horizontalError,
                    preparationStartTimeToLandingSeconds,
                    preparationWeight,
                    in settings,
                    deltaSeconds,
                    true,
                    ref lockFacts);
            }

            CharacterFootSupportLockState previousState = sameLockEvent
                ? lockFacts.State
                : CharacterFootSupportLockState.None;
            CharacterFootSupportLockState lockState = previousState switch
            {
                CharacterFootSupportLockState.Locked =>
                    horizontalError <= settings.LockDistance
                        ? CharacterFootSupportLockState.Locked
                        : CharacterFootSupportLockState.Sliding,
                CharacterFootSupportLockState.Sliding =>
                    CharacterFootSupportLockState.Sliding,
                _ => horizontalError <= settings.LockDistance
                    ? CharacterFootSupportLockState.Locked
                    : CharacterFootSupportLockState.Sliding
            };
            float slideT = lockState == CharacterFootSupportLockState.Locked
                ? 1f
                : Mathf.InverseLerp(
                    settings.SlideDistance,
                    settings.LockDistance,
                    horizontalError);
            Vector3 targetOffset = horizontalOffset * slideT;
            Vector3 targetAnkle = originalAnkle + targetOffset + up * plant;
            if (sameLockEvent && previousState == CharacterFootSupportLockState.Locked &&
                lockState == CharacterFootSupportLockState.Locked)
            {
                targetAnkle = lockFacts.TargetAnkle;
            }
            else if (sameLockEvent &&
                     (previousState == CharacterFootSupportLockState.Locked ||
                      previousState == CharacterFootSupportLockState.Sliding))
            {
                float lockedHeight = Vector3.Dot(lockFacts.TargetAnkle, up);
                targetAnkle += up * (lockedHeight - Vector3.Dot(targetAnkle, up));
            }
            Vector3 ankleDelta = targetAnkle - originalAnkle;
            Vector3 targetSole = originalSole + ankleDelta;
            float verticalCorrection = Vector3.Dot(ankleDelta, up);
            float positionWeight = footPlacementWeight;
            Vector3 outputVelocity = sameLockEvent &&
                                     deltaSeconds > GeometryEpsilon
                ? (targetAnkle - lockFacts.OutputAnkle) / deltaSeconds
                : default;
            lockFacts.HasValue = true;
            lockFacts.LandingEventIdentity = landingEventIdentity;
            lockFacts.State = lockState;
            lockFacts.TargetAnkle = targetAnkle;
            lockFacts.PositionWeight = positionWeight;
            lockFacts.UnlockStartCorrection = default;
            lockFacts.UnlockStartPositionWeight = 0f;
            lockFacts.UnlockRemainingSeconds = 0f;
            lockFacts.OutputAnkle = targetAnkle;
            lockFacts.OutputVelocity = outputVelocity;
            lockFacts.SwingHandoffLandingEventIdentity = 0;
            lockFacts.SwingHandoffTargetAnkle = default;
            return new CharacterFootSwingMotionDiagnostics(
                CharacterFootSwingMotionState.Accepted,
                CharacterFootSwingMotionRejectReason.None,
                landingEventIdentity,
                0,
                originalSole,
                originalAnkle,
                0f,
                0f,
                lastLanding,
                targetSole,
                verticalCorrection,
                0f,
                1f,
                targetSole,
                targetAnkle,
                positionWeight,
                0f,
                lockState,
                horizontalError,
                0f,
                default,
                preparationStartTimeToLandingSeconds,
                preparationWeight);
        }

        internal static CharacterFootSwingMotionDiagnostics BuildSwingHandoff(
            CharacterFootPlacementAnimatedFootPose animatedFoot,
            in CharacterFootSwingMotionDiagnostics swingMotion,
            ulong handoffLandingEventIdentity,
            Vector3 componentUp,
            in CharacterFootMotionSettings settings,
            float deltaSeconds,
            ref CharacterFootSupportLockFacts lockFacts)
        {
            Vector3 originalSole = (animatedFoot.HeelPosition + animatedFoot.ToePosition) * 0.5f;
            Vector3 originalAnkle = animatedFoot.AnklePosition;
            if (!lockFacts.HasValue || handoffLandingEventIdentity == 0 ||
                !Finite(componentUp) || componentUp.sqrMagnitude <= GeometryEpsilon ||
                !float.IsFinite(deltaSeconds) || deltaSeconds < 0f)
            {
                throw new InvalidOperationException("Foot swing handoff input is invalid.");
            }

            bool beginning = lockFacts.State != CharacterFootSupportLockState.SwingHandoff;
            bool hasPredictedTarget = swingMotion.Accepted &&
                                      swingMotion.LandingEventIdentity ==
                                      handoffLandingEventIdentity;
            Vector3 handoffTargetAnkle = hasPredictedTarget
                ? swingMotion.CorrectedAnkle
                : originalAnkle;
            if (beginning)
            {
                lockFacts.State = CharacterFootSupportLockState.SwingHandoff;
                lockFacts.OutputAnkle = Finite(lockFacts.OutputAnkle)
                    ? lockFacts.OutputAnkle
                    : lockFacts.TargetAnkle;
                lockFacts.OutputVelocity = Finite(lockFacts.OutputVelocity)
                    ? lockFacts.OutputVelocity
                    : default;
                lockFacts.SwingHandoffLandingEventIdentity = handoffLandingEventIdentity;
                lockFacts.SwingHandoffTargetAnkle = handoffTargetAnkle;
                lockFacts.UnlockStartPositionWeight = lockFacts.PositionWeight;
                lockFacts.UnlockRemainingSeconds = settings.UnlockBlendSeconds;
            }
            else
            {
                if (lockFacts.SwingHandoffLandingEventIdentity != handoffLandingEventIdentity)
                {
                    lockFacts.SwingHandoffLandingEventIdentity = handoffLandingEventIdentity;
                    lockFacts.UnlockRemainingSeconds = settings.UnlockBlendSeconds;
                }
                lockFacts.SwingHandoffTargetAnkle = handoffTargetAnkle;
            }

            if (!Finite(lockFacts.OutputAnkle) ||
                !Finite(lockFacts.OutputVelocity) ||
                !Finite(lockFacts.SwingHandoffTargetAnkle))
            {
                throw new InvalidOperationException("Foot swing handoff state is invalid.");
            }

            bool hasCurrentTarget = handoffLandingEventIdentity ==
                                    lockFacts.SwingHandoffLandingEventIdentity;
            if (!beginning && deltaSeconds > 0f &&
                lockFacts.UnlockRemainingSeconds > GeometryEpsilon)
            {
                float duration = lockFacts.UnlockRemainingSeconds;
                float step = Mathf.Min(deltaSeconds, duration);
                AdvanceHermite(
                    lockFacts.OutputAnkle,
                    lockFacts.OutputVelocity,
                    lockFacts.SwingHandoffTargetAnkle,
                    duration,
                    step,
                    out lockFacts.OutputAnkle,
                    out lockFacts.OutputVelocity);
                float targetWeight = hasPredictedTarget
                    ? swingMotion.PositionWeight
                    : lockFacts.PositionWeight;
                lockFacts.PositionWeight = Mathf.Lerp(
                    lockFacts.PositionWeight,
                    targetWeight,
                    step / duration);
                lockFacts.UnlockRemainingSeconds = Mathf.Max(0f, duration - step);
            }

            if (lockFacts.UnlockRemainingSeconds <= GeometryEpsilon && hasCurrentTarget)
            {
                lockFacts.ClearSupport();
                return hasPredictedTarget
                    ? swingMotion
                    : CharacterFootSwingMotionBuilder.SuppressUnselected(in swingMotion);
            }

            Vector3 up = componentUp.normalized;
            Vector3 correction = lockFacts.OutputAnkle - originalAnkle;
            Vector3 correctedSole = originalSole + correction;
            float horizontalError = Vector3.ProjectOnPlane(
                lockFacts.SwingHandoffTargetAnkle - lockFacts.OutputAnkle,
                up).magnitude;
            return new CharacterFootSwingMotionDiagnostics(
                CharacterFootSwingMotionState.Accepted,
                CharacterFootSwingMotionRejectReason.None,
                lockFacts.SwingHandoffLandingEventIdentity,
                hasPredictedTarget ? swingMotion.GroundPathInputIdentity : 0,
                originalSole,
                originalAnkle,
                hasPredictedTarget ? swingMotion.Distance : 0f,
                hasPredictedTarget ? swingMotion.Progress : 0f,
                hasPredictedTarget ? swingMotion.BaselineSample : default,
                hasPredictedTarget ? swingMotion.EnvelopeSample : default,
                Vector3.Dot(correction, up),
                hasPredictedTarget ? swingMotion.LandingPredictionError : 0f,
                hasPredictedTarget ? swingMotion.LandingConstraintWeight : 0f,
                correctedSole,
                lockFacts.OutputAnkle,
                lockFacts.PositionWeight,
                0f,
                CharacterFootSupportLockState.SwingHandoff,
                horizontalError,
                lockFacts.UnlockRemainingSeconds,
                correction,
                hasPredictedTarget
                    ? swingMotion.SupportLockPreparationStartTimeToLandingSeconds
                    : 0f,
                hasPredictedTarget ? swingMotion.SupportLockPreparationWeight : 0f);
        }

        static void AdvanceHermite(
            Vector3 start,
            Vector3 startVelocity,
            Vector3 end,
            float duration,
            float step,
            out Vector3 position,
            out Vector3 velocity)
        {
            float t = Mathf.Clamp01(step / duration);
            float t2 = t * t;
            float t3 = t2 * t;
            float h00 = 2f * t3 - 3f * t2 + 1f;
            float h10 = t3 - 2f * t2 + t;
            float h01 = -2f * t3 + 3f * t2;
            position = h00 * start + h10 * duration * startVelocity + h01 * end;
            float dh00 = 6f * t2 - 6f * t;
            float dh10 = 3f * t2 - 4f * t + 1f;
            float dh01 = -6f * t2 + 6f * t;
            velocity = (dh00 * start +
                        dh10 * duration * startVelocity +
                        dh01 * end) / duration;
        }

        static void BeginSupportUnlock(
            Vector3 originalAnkle,
            in CharacterFootMotionSettings settings,
            ref CharacterFootSupportLockFacts lockFacts)
        {
            lockFacts.State = CharacterFootSupportLockState.Unlocked;
            lockFacts.UnlockStartCorrection = lockFacts.TargetAnkle - originalAnkle;
            lockFacts.UnlockStartPositionWeight = lockFacts.PositionWeight;
            lockFacts.UnlockRemainingSeconds = settings.UnlockBlendSeconds;
            lockFacts.SwingHandoffLandingEventIdentity = 0;
            lockFacts.SwingHandoffTargetAnkle = default;
        }

        static CharacterFootSwingMotionDiagnostics ContinueSupportUnlock(
            ulong landingEventIdentity,
            Vector3 originalSole,
            Vector3 originalAnkle,
            Vector3 up,
            float horizontalError,
            float preparationStartTimeToLandingSeconds,
            float preparationWeight,
            in CharacterFootMotionSettings settings,
            float deltaSeconds,
            bool beganUnlock,
            ref CharacterFootSupportLockFacts lockFacts)
        {
            if (!beganUnlock && deltaSeconds > 0f)
                lockFacts.UnlockRemainingSeconds = Mathf.Max(
                    0f,
                    lockFacts.UnlockRemainingSeconds - deltaSeconds);
            if (lockFacts.UnlockRemainingSeconds <= GeometryEpsilon)
            {
                lockFacts.ClearSupport();
                return RejectedPlant(
                    CharacterFootSwingMotionRejectReason.SupportOwnershipReleased,
                    landingEventIdentity,
                    originalSole,
                    originalAnkle);
            }

            float releaseAlpha = lockFacts.UnlockRemainingSeconds /
                                 settings.UnlockBlendSeconds;
            float releaseWeight = lockFacts.UnlockStartPositionWeight * releaseAlpha;
            Vector3 releasedAnkle = originalAnkle + lockFacts.UnlockStartCorrection;
            Vector3 releasedSole = originalSole + lockFacts.UnlockStartCorrection;
            lockFacts.OutputVelocity = !beganUnlock && deltaSeconds > GeometryEpsilon
                ? (releasedAnkle - lockFacts.OutputAnkle) / deltaSeconds
                : default;
            lockFacts.OutputAnkle = releasedAnkle;
            lockFacts.PositionWeight = releaseWeight;
            return new CharacterFootSwingMotionDiagnostics(
                CharacterFootSwingMotionState.Accepted,
                CharacterFootSwingMotionRejectReason.None,
                landingEventIdentity,
                0,
                originalSole,
                originalAnkle,
                horizontalError,
                0f,
                default,
                default,
                Vector3.Dot(lockFacts.UnlockStartCorrection, up),
                0f,
                1f,
                releasedSole,
                releasedAnkle,
                releaseWeight,
                0f,
                CharacterFootSupportLockState.Unlocked,
                horizontalError,
                lockFacts.UnlockRemainingSeconds,
                lockFacts.UnlockStartCorrection,
                preparationStartTimeToLandingSeconds,
                preparationWeight);
        }

        internal static CharacterFootStrideHipsDiagnostics BuildRejected(
            CharacterFootStrideRejectReason reason) =>
            new CharacterFootStrideHipsDiagnostics(
                CharacterFootStrideState.Rejected,
                reason,
                default,
                default,
                default,
                default,
                0f,
                CharacterFootStrideSlope.Flat,
                default,
                default,
                default,
                default,
                default,
                0f,
                0f,
                false,
                false,
                CharacterFootStrideSlope.Flat,
                CharacterFootPelvisSpringHandoffReason.None,
                false,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                false,
                0f,
                0f,
                0f,
                0f,
                false,
                false,
                0f,
                0f,
                0f,
                default,
                0f);

        internal static CharacterFootStrideHipsDiagnostics BuildPelvis(
            CharacterFootSide supportSide,
            ulong supportLandingEventIdentity,
            CharacterFootSide swingSide,
            Vector3 strideStart,
            Vector3 strideEnd,
            Vector3 poseRootPosition,
            Vector3 componentUp,
            Vector3 animatedPelvis,
            Vector3 animatedPelvisComponentPosition,
            Vector3 supportHip,
            Vector3 supportTargetAnkle,
            float supportLegLength,
            float supportLegCompressionReserve,
            Vector3 leftOriginalSole,
            Vector3 rightOriginalSole,
            Vector3 leftCorrectedSole,
            Vector3 rightCorrectedSole,
            float swingTimeToLandingSeconds,
            float footPlacementWeight,
            float deltaSeconds,
            in CharacterFootMotionSettings settings,
            ref CharacterFootPelvisSpringState spring)
        {
            if (!Finite(componentUp) || componentUp.sqrMagnitude <= GeometryEpsilon ||
                !Finite(strideStart) || !Finite(strideEnd) || !Finite(poseRootPosition) ||
                !Finite(animatedPelvis) || !Finite(animatedPelvisComponentPosition) ||
                !Finite(supportHip) || !Finite(supportTargetAnkle) ||
                !float.IsFinite(supportLegLength) || supportLegLength <= EndpointTolerance ||
                !float.IsFinite(supportLegCompressionReserve) || supportLegCompressionReserve < 0f ||
                !Finite(leftOriginalSole) || !Finite(rightOriginalSole) ||
                !Finite(leftCorrectedSole) || !Finite(rightCorrectedSole) ||
                !float.IsFinite(deltaSeconds) || deltaSeconds < 0f)
                return BuildRejected(CharacterFootStrideRejectReason.InvalidComponentUp);
            if (supportLandingEventIdentity == 0 ||
                !float.IsFinite(footPlacementWeight) ||
                footPlacementWeight < 0f || footPlacementWeight > 1f)
                return BuildRejected(CharacterFootStrideRejectReason.InvalidInput);
            Vector3 up = componentUp.normalized;
            Vector3 horizontal = Vector3.ProjectOnPlane(strideEnd - strideStart, up);
            float pathLength = horizontal.magnitude;
            if (!float.IsFinite(pathLength) || pathLength <= GeometryEpsilon)
                return BuildRejected(CharacterFootStrideRejectReason.DegenerateStride);
            float progress = Mathf.Clamp01(
                Vector3.Dot(
                    Vector3.ProjectOnPlane(poseRootPosition - strideStart, up),
                    horizontal / pathLength) / pathLength);
            Vector3 sampledGround = Vector3.Lerp(strideStart, strideEnd, progress);
            float rise = Vector3.Dot(strideEnd - strideStart, up);
            CharacterFootStrideSlope slope = CharacterFootStrideSlope.Flat;
            float rootRelativeGroundTarget = 0f;
            if (rise > EndpointTolerance)
            {
                slope = CharacterFootStrideSlope.Ascending;
                rootRelativeGroundTarget = Vector3.Dot(
                    sampledGround - poseRootPosition,
                    up);
            }
            else if (rise < -EndpointTolerance)
            {
                slope = CharacterFootStrideSlope.Descending;
                if (float.IsFinite(swingTimeToLandingSeconds) &&
                    swingTimeToLandingSeconds > GeometryEpsilon)
                {
                    rootRelativeGroundTarget = Vector3.Dot(
                        sampledGround - poseRootPosition,
                        up);
                }
            }
            Vector3 rawPelvisDelta = up * rootRelativeGroundTarget;
            float originalLowerSole = Mathf.Min(
                Vector3.Dot(leftOriginalSole, up),
                Vector3.Dot(rightOriginalSole, up));
            float correctedLowerSole = Mathf.Min(
                Vector3.Dot(leftCorrectedSole, up),
                Vector3.Dot(rightCorrectedSole, up));
            float soleClearanceLift = Mathf.Max(
                0f,
                correctedLowerSole - originalLowerSole);
            float unclampedTarget = rootRelativeGroundTarget + soleClearanceLift;
            if (!TryResolveSupportReachInterval(
                    supportHip,
                    supportTargetAnkle,
                    up,
                    supportLegLength,
                    supportLegCompressionReserve,
                    out float supportReachUsableLegLength,
                    out float supportReachMinimum,
                    out float supportReachMaximum))
            {
                return BuildPelvisRelease(
                    CharacterFootStrideRejectReason.SupportLegUnreachable,
                    componentUp,
                    footPlacementWeight,
                    deltaSeconds,
                    in settings,
                    ref spring);
            }
            float target = Mathf.Clamp(
                unclampedTarget,
                supportReachMinimum,
                supportReachMaximum);
            bool supportReachTargetClamped =
                Mathf.Abs(target - unclampedTarget) > GeometryEpsilon;
            bool hadPreviousState = spring.HasValue;
            bool supportChanged = hadPreviousState &&
                (spring.SupportSide != supportSide ||
                 spring.SupportLandingEventIdentity != supportLandingEventIdentity);
            CharacterFootStrideSlope previousSlope = hadPreviousState
                ? spring.Slope
                : CharacterFootStrideSlope.Flat;
            bool slopeChanged = hadPreviousState && previousSlope != slope;
            float previousTarget = hadPreviousState ? spring.TargetAlongUp : 0f;
            float previousOutput = hadPreviousState ? spring.OutputAlongUp : 0f;
            float previousVelocity = hadPreviousState ? spring.VelocityAlongUp : 0f;
            float springInput = previousOutput;
            float previousTargetDirection = previousTarget - previousOutput;
            float nextTargetDirection = target - previousOutput;
            bool targetCrossedOutput = hadPreviousState &&
                Mathf.Abs(previousTargetDirection) > EndpointTolerance &&
                Mathf.Abs(nextTargetDirection) > EndpointTolerance &&
                previousTargetDirection * nextTargetDirection < 0f;
            CharacterFootPelvisSpringHandoffReason handoffReason =
                CharacterFootPelvisSpringHandoffReason.None;
            if (supportChanged)
                handoffReason |= CharacterFootPelvisSpringHandoffReason.SupportChanged;
            if (slopeChanged)
                handoffReason |= CharacterFootPelvisSpringHandoffReason.SlopeChanged;
            if (targetCrossedOutput)
                handoffReason |= CharacterFootPelvisSpringHandoffReason.TargetCrossedOutput;
            bool velocityReset =
                handoffReason != CharacterFootPelvisSpringHandoffReason.None &&
                Mathf.Abs(nextTargetDirection) > GeometryEpsilon &&
                previousVelocity * nextTargetDirection < 0f;
            float springInputVelocity = velocityReset ? 0f : previousVelocity;
            float springOutput = springInput;
            float springVelocity = springInputVelocity;
            if (deltaSeconds > 0f)
            {
                float omega = settings.PelvisSpringFrequency * 2f * Mathf.PI;
                float x0 = springInput - target;
                float j0 = springInputVelocity + omega * x0;
                float decay = Mathf.Exp(-omega * deltaSeconds);
                springOutput = target + (x0 + j0 * deltaSeconds) * decay;
                springVelocity =
                    (springInputVelocity - omega * j0 * deltaSeconds) * decay;
            }
            float unclampedSpringOutput = springOutput;
            springOutput = Mathf.Clamp(
                springOutput,
                supportReachMinimum,
                supportReachMaximum);
            bool supportReachOutputClamped =
                Mathf.Abs(springOutput - unclampedSpringOutput) > GeometryEpsilon;
            if (supportReachOutputClamped &&
                (springOutput <= supportReachMinimum && springVelocity < 0f ||
                 springOutput >= supportReachMaximum && springVelocity > 0f))
            {
                springVelocity = 0f;
            }
            spring.HasValue = true;
            spring.SupportSide = supportSide;
            spring.SupportLandingEventIdentity = supportLandingEventIdentity;
            spring.Slope = slope;
            spring.TargetAlongUp = target;
            spring.OutputAlongUp = springOutput;
            spring.VelocityAlongUp = springVelocity;
            Vector3 pelvisDelta = up * springOutput;
            float positionWeight = Mathf.Abs(springOutput) > EndpointTolerance
                ? footPlacementWeight
                : 0f;
            return new CharacterFootStrideHipsDiagnostics(
                CharacterFootStrideState.Accepted,
                CharacterFootStrideRejectReason.None,
                supportSide,
                swingSide,
                strideStart,
                strideEnd,
                progress,
                slope,
                sampledGround,
                poseRootPosition,
                animatedPelvis,
                animatedPelvisComponentPosition,
                rawPelvisDelta,
                rootRelativeGroundTarget,
                soleClearanceLift,
                hadPreviousState,
                supportChanged,
                previousSlope,
                handoffReason,
                velocityReset,
                previousTarget,
                previousOutput,
                previousVelocity,
                springInput,
                springInputVelocity,
                settings.PelvisSpringFrequency,
                unclampedTarget,
                true,
                supportLegCompressionReserve,
                supportReachUsableLegLength,
                supportReachMinimum,
                supportReachMaximum,
                supportReachTargetClamped,
                supportReachOutputClamped,
                target,
                springOutput,
                springVelocity,
                pelvisDelta,
                positionWeight);
        }

        internal static CharacterFootStrideHipsDiagnostics BuildPelvisRelease(
            CharacterFootStrideRejectReason reason,
            Vector3 componentUp,
            float footPlacementWeight,
            float deltaSeconds,
            in CharacterFootMotionSettings settings,
            ref CharacterFootPelvisSpringState spring)
        {
            if (!spring.HasValue)
                return BuildRejected(reason);
            if (!Finite(componentUp) || componentUp.sqrMagnitude <= GeometryEpsilon ||
                !float.IsFinite(footPlacementWeight) ||
                footPlacementWeight < 0f || footPlacementWeight > 1f ||
                !float.IsFinite(deltaSeconds) || deltaSeconds < 0f)
            {
                spring.Clear();
                return BuildRejected(CharacterFootStrideRejectReason.InvalidInput);
            }

            Vector3 up = componentUp.normalized;
            float previousTarget = spring.TargetAlongUp;
            float previousOutput = spring.OutputAlongUp;
            float previousVelocity = spring.VelocityAlongUp;
            float target = 0f;
            float nextTargetDirection = target - previousOutput;
            CharacterFootPelvisSpringHandoffReason handoffReason =
                CharacterFootPelvisSpringHandoffReason.SupportChanged;
            if (spring.Slope != CharacterFootStrideSlope.Flat)
                handoffReason |= CharacterFootPelvisSpringHandoffReason.SlopeChanged;
            bool velocityReset = Mathf.Abs(nextTargetDirection) > GeometryEpsilon &&
                                 previousVelocity * nextTargetDirection < 0f;
            float springInputVelocity = velocityReset ? 0f : previousVelocity;
            float springOutput = previousOutput;
            float springVelocity = springInputVelocity;
            if (deltaSeconds > 0f)
            {
                float omega = settings.PelvisSpringFrequency * 2f * Mathf.PI;
                float x0 = previousOutput;
                float j0 = springInputVelocity + omega * x0;
                float decay = Mathf.Exp(-omega * deltaSeconds);
                springOutput = (x0 + j0 * deltaSeconds) * decay;
                springVelocity =
                    (springInputVelocity - omega * j0 * deltaSeconds) * decay;
            }
            if (Mathf.Abs(springOutput) <= GeometryEpsilon &&
                Mathf.Abs(springVelocity) <= GeometryEpsilon)
            {
                spring.Clear();
                return BuildRejected(reason);
            }

            CharacterFootStrideSlope previousSlope = spring.Slope;
            spring.HasValue = true;
            spring.SupportSide = default;
            spring.SupportLandingEventIdentity = 0;
            spring.Slope = CharacterFootStrideSlope.Flat;
            spring.TargetAlongUp = 0f;
            spring.OutputAlongUp = springOutput;
            spring.VelocityAlongUp = springVelocity;
            Vector3 pelvisDelta = up * springOutput;
            float positionWeight = Mathf.Abs(springOutput) > EndpointTolerance
                ? footPlacementWeight
                : 0f;
            return new CharacterFootStrideHipsDiagnostics(
                CharacterFootStrideState.Releasing,
                reason,
                default,
                default,
                default,
                default,
                0f,
                CharacterFootStrideSlope.Flat,
                default,
                default,
                default,
                default,
                default,
                0f,
                0f,
                true,
                true,
                previousSlope,
                handoffReason,
                velocityReset,
                previousTarget,
                previousOutput,
                previousVelocity,
                previousOutput,
                springInputVelocity,
                settings.PelvisSpringFrequency,
                target,
                false,
                0f,
                0f,
                0f,
                0f,
                false,
                false,
                target,
                springOutput,
                springVelocity,
                pelvisDelta,
                positionWeight);
        }

        static bool TryResolveSupportReachInterval(
            Vector3 supportHip,
            Vector3 supportTargetAnkle,
            Vector3 up,
            float supportLegLength,
            float supportLegCompressionReserve,
            out float usableLegLength,
            out float minimumAlongUp,
            out float maximumAlongUp)
        {
            Vector3 hipFromTarget = supportHip - supportTargetAnkle;
            Vector3 horizontal = Vector3.ProjectOnPlane(hipFromTarget, up);
            float horizontalSquare = horizontal.sqrMagnitude;
            float maximumUsableLegLength = supportLegLength - EndpointTolerance;
            float maximumLegSquare = maximumUsableLegLength * maximumUsableLegLength;
            if (!float.IsFinite(horizontalSquare) ||
                maximumUsableLegLength <= EndpointTolerance ||
                horizontalSquare >= maximumLegSquare)
            {
                usableLegLength = 0f;
                minimumAlongUp = 0f;
                maximumAlongUp = 0f;
                return false;
            }
            float minimumUsableLegLength = Mathf.Min(
                maximumUsableLegLength,
                Mathf.Sqrt(horizontalSquare + EndpointTolerance * EndpointTolerance));
            usableLegLength = Mathf.Clamp(
                supportLegLength - Mathf.Max(EndpointTolerance, supportLegCompressionReserve),
                minimumUsableLegLength,
                maximumUsableLegLength);
            float legSquare = usableLegLength * usableLegLength;
            if (!float.IsFinite(usableLegLength) ||
                usableLegLength <= EndpointTolerance ||
                horizontalSquare >= legSquare)
            {
                usableLegLength = 0f;
                minimumAlongUp = 0f;
                maximumAlongUp = 0f;
                return false;
            }

            float vertical = Vector3.Dot(hipFromTarget, up);
            float verticalReach = Mathf.Sqrt(legSquare - horizontalSquare);
            minimumAlongUp = -vertical - verticalReach;
            maximumAlongUp = -vertical + verticalReach;
            return float.IsFinite(minimumAlongUp) &&
                   float.IsFinite(maximumAlongUp) &&
                   minimumAlongUp <= maximumAlongUp;
        }

        static bool IsAuthoritativeSwing(in AnimationBiomechanicalStepHeader step) =>
            step.IsValid &&
            step.IsAuthoritative &&
            step.IsSwing &&
            step.HasConsistentLandingEventIdentity;

        static bool IsRetainablePrimarySupport(
            in CharacterFootSwingMotionDiagnostics motion) =>
            motion.Accepted &&
            motion.LandingEventIdentity != 0 &&
            (motion.SupportLockState == CharacterFootSupportLockState.Locked ||
             motion.SupportLockState == CharacterFootSupportLockState.Sliding);

        static bool IsAcquirablePrimarySupport(
            in CharacterFootSwingMotionDiagnostics motion) =>
            motion.Accepted &&
            motion.LandingEventIdentity != 0 &&
            motion.SupportLockState == CharacterFootSupportLockState.Locked;

        static CharacterFootSwingMotionDiagnostics RejectedPlant(
            CharacterFootSwingMotionRejectReason reason,
            ulong landingEventIdentity,
            Vector3 originalSole,
            Vector3 originalAnkle) =>
            new CharacterFootSwingMotionDiagnostics(
                CharacterFootSwingMotionState.Rejected,
                reason,
                landingEventIdentity,
                0,
                originalSole,
                originalAnkle,
                0f,
                0f,
                default,
                default,
                0f,
                0f,
                0f,
                originalSole,
                originalAnkle,
                0f,
                0f);

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
