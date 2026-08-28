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
        Releasing = 3,
        LandingReach = 4
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

        internal CharacterFootPrimarySupportResult Result =>
            new CharacterFootPrimarySupportResult(
                HasValue,
                Side,
                LandingEventIdentity,
                Retained);
    }

    internal readonly struct CharacterFootPrimarySupportResult
    {
        internal CharacterFootPrimarySupportResult(
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

        internal bool HasValue { get; }
        internal CharacterFootSide Side { get; }
        internal ulong LandingEventIdentity { get; }
        internal bool Retained { get; }
    }

    public readonly struct CharacterFootPrimarySupportDiagnostics
    {
        readonly CharacterFootPrimarySupportResult m_Result;

        internal CharacterFootPrimarySupportDiagnostics(
            in CharacterFootPrimarySupportResult result) =>
            m_Result = result;

        public bool HasValue => m_Result.HasValue;
        public CharacterFootSide Side => m_Result.Side;
        public ulong LandingEventIdentity => m_Result.LandingEventIdentity;
        public bool Retained => m_Result.Retained;
    }

    internal readonly struct CharacterFootStrideIntentResult
    {
        internal CharacterFootStrideIntentResult(
            CharacterFootStrideRejectReason rejectReason,
            CharacterFootSide supportSide,
            CharacterFootSide swingSide,
            Vector3 strideStart,
            Vector3 strideEnd,
            float swingTimeToLanding,
            bool releasePelvis)
        {
            RejectReason = rejectReason;
            SupportSide = supportSide;
            SwingSide = swingSide;
            StrideStart = strideStart;
            StrideEnd = strideEnd;
            SwingTimeToLanding = swingTimeToLanding;
            ReleasePelvis = releasePelvis;
            Accepted = rejectReason == CharacterFootStrideRejectReason.None;
        }

        internal bool Accepted { get; }
        internal CharacterFootStrideRejectReason RejectReason { get; }
        internal CharacterFootSide SupportSide { get; }
        internal CharacterFootSide SwingSide { get; }
        internal Vector3 StrideStart { get; }
        internal Vector3 StrideEnd { get; }
        internal float SwingTimeToLanding { get; }
        internal bool ReleasePelvis { get; }
    }

    internal readonly struct CharacterFootPelvisFrame
    {
        internal CharacterFootPelvisFrame(
            Vector3 componentUp,
            Vector3 poseRootPosition,
            Vector3 animatedPelvis,
            Vector3 animatedPelvisComponentPosition,
            in CharacterFootPlacementAnimatedPose pose,
            Vector3 leftCorrectedSole,
            Vector3 rightCorrectedSole,
            float leftLegLength,
            float rightLegLength,
            float footPlacementWeight,
            float deltaSeconds)
        {
            ComponentUp = componentUp;
            PoseRootPosition = poseRootPosition;
            AnimatedPelvis = animatedPelvis;
            AnimatedPelvisComponentPosition = animatedPelvisComponentPosition;
            Pose = pose;
            LeftCorrectedSole = leftCorrectedSole;
            RightCorrectedSole = rightCorrectedSole;
            LeftLegLength = leftLegLength;
            RightLegLength = rightLegLength;
            FootPlacementWeight = footPlacementWeight;
            DeltaSeconds = deltaSeconds;
        }

        internal Vector3 ComponentUp { get; }
        internal Vector3 PoseRootPosition { get; }
        internal Vector3 AnimatedPelvis { get; }
        internal Vector3 AnimatedPelvisComponentPosition { get; }
        internal CharacterFootPlacementAnimatedPose Pose { get; }
        internal Vector3 LeftCorrectedSole { get; }
        internal Vector3 RightCorrectedSole { get; }
        internal float LeftLegLength { get; }
        internal float RightLegLength { get; }
        internal float FootPlacementWeight { get; }
        internal float DeltaSeconds { get; }
    }

    internal readonly struct CharacterFootStrideHipsResult
    {
        internal CharacterFootStrideHipsResult(
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
            State == CharacterFootStrideState.Releasing ||
            State == CharacterFootStrideState.LandingReach;

        internal CharacterFootStrideHipsResult WithLandingReachOutput(
            CharacterFootStrideState state,
            float springTarget,
            float springOutput,
            float springVelocity,
            Vector3 pelvisDelta,
            float positionWeight) =>
            new CharacterFootStrideHipsResult(
                state,
                RejectReason,
                SupportSide,
                SwingSide,
                StrideStart,
                StrideEnd,
                Progress,
                Slope,
                SampledGround,
                PoseRootPosition,
                AnimatedPelvis,
                AnimatedPelvisComponentPosition,
                RawPelvisDelta,
                RootRelativeGroundTargetAlongUp,
                SoleClearanceLiftAlongUp,
                HadPreviousState,
                SupportChanged,
                PreviousSlope,
                SpringHandoffReason,
                SpringVelocityReset,
                PreviousSpringTarget,
                PreviousSpringOutput,
                PreviousSpringVelocity,
                SpringInput,
                SpringInputVelocity,
                SpringFrequency,
                UnclampedSpringTarget,
                SupportReachAvailable,
                SupportLegCompressionReserve,
                SupportReachUsableLegLength,
                SupportReachMinimumAlongUp,
                SupportReachMaximumAlongUp,
                SupportReachTargetClamped,
                SupportReachOutputClamped,
                springTarget,
                springOutput,
                springVelocity,
                pelvisDelta,
                positionWeight);
    }

    public readonly struct CharacterFootStrideHipsDiagnostics
    {
        readonly CharacterFootStrideHipsResult m_Result;

        internal CharacterFootStrideHipsDiagnostics(
            in CharacterFootStrideHipsResult result) =>
            m_Result = result;

        public CharacterFootStrideState State => m_Result.State;
        public CharacterFootStrideRejectReason RejectReason => m_Result.RejectReason;
        public CharacterFootSide SupportSide => m_Result.SupportSide;
        public CharacterFootSide SwingSide => m_Result.SwingSide;
        public Vector3 StrideStart => m_Result.StrideStart;
        public Vector3 StrideEnd => m_Result.StrideEnd;
        public float Progress => m_Result.Progress;
        public CharacterFootStrideSlope Slope => m_Result.Slope;
        public Vector3 SampledGround => m_Result.SampledGround;
        public Vector3 PoseRootPosition => m_Result.PoseRootPosition;
        public Vector3 AnimatedPelvis => m_Result.AnimatedPelvis;
        public Vector3 AnimatedPelvisComponentPosition =>
            m_Result.AnimatedPelvisComponentPosition;
        public Vector3 RawPelvisDelta => m_Result.RawPelvisDelta;
        public float RootRelativeGroundTargetAlongUp =>
            m_Result.RootRelativeGroundTargetAlongUp;
        public float SoleClearanceLiftAlongUp => m_Result.SoleClearanceLiftAlongUp;
        public bool HadPreviousState => m_Result.HadPreviousState;
        public bool SupportChanged => m_Result.SupportChanged;
        public CharacterFootStrideSlope PreviousSlope => m_Result.PreviousSlope;
        public CharacterFootPelvisSpringHandoffReason SpringHandoffReason =>
            m_Result.SpringHandoffReason;
        public bool SpringVelocityReset => m_Result.SpringVelocityReset;
        public float PreviousSpringTarget => m_Result.PreviousSpringTarget;
        public float PreviousSpringOutput => m_Result.PreviousSpringOutput;
        public float PreviousSpringVelocity => m_Result.PreviousSpringVelocity;
        public float SpringInput => m_Result.SpringInput;
        public float SpringInputVelocity => m_Result.SpringInputVelocity;
        public float SpringFrequency => m_Result.SpringFrequency;
        public float UnclampedSpringTarget => m_Result.UnclampedSpringTarget;
        public bool SupportReachAvailable => m_Result.SupportReachAvailable;
        public float SupportLegCompressionReserve =>
            m_Result.SupportLegCompressionReserve;
        public float SupportReachUsableLegLength =>
            m_Result.SupportReachUsableLegLength;
        public float SupportReachMinimumAlongUp => m_Result.SupportReachMinimumAlongUp;
        public float SupportReachMaximumAlongUp => m_Result.SupportReachMaximumAlongUp;
        public bool SupportReachTargetClamped => m_Result.SupportReachTargetClamped;
        public bool SupportReachOutputClamped => m_Result.SupportReachOutputClamped;
        public float SpringTarget => m_Result.SpringTarget;
        public float SpringOutput => m_Result.SpringOutput;
        public float SpringVelocity => m_Result.SpringVelocity;
        public Vector3 PelvisDelta => m_Result.PelvisDelta;
        public float PositionWeight => m_Result.PositionWeight;
        public bool Accepted => m_Result.Accepted;
        public bool ProducesPelvisGoal => m_Result.ProducesPelvisGoal;
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

        internal static void ResolvePrimarySupport(
            in CharacterResolvedFootResult leftMotion,
            in CharacterResolvedFootResult rightMotion,
            ref CharacterFootPrimarySupportFacts primarySupport)
        {
            bool leftRetainable = IsRetainablePrimarySupport(in leftMotion);
            bool rightRetainable = IsRetainablePrimarySupport(in rightMotion);
            bool leftCandidate = IsAcquirablePrimarySupport(in leftMotion);
            bool rightCandidate = IsAcquirablePrimarySupport(in rightMotion);
            if (primarySupport.HasValue)
            {
                bool retained = primarySupport.Side == CharacterFootSide.Left
                    ? leftRetainable &&
                      leftMotion.SupportEventIdentity == primarySupport.LandingEventIdentity &&
                      (!rightCandidate ||
                       leftMotion.SupportWeight >= rightMotion.SupportWeight)
                    : rightRetainable &&
                      rightMotion.SupportEventIdentity == primarySupport.LandingEventIdentity &&
                      (!leftCandidate ||
                       rightMotion.SupportWeight >= leftMotion.SupportWeight);
                if (retained)
                {
                    primarySupport.Retained = true;
                    return;
                }
            }

            if (!leftCandidate && !rightCandidate)
            {
                primarySupport.Clear();
                return;
            }

            bool selectLeft = leftCandidate &&
                (!rightCandidate ||
                 leftMotion.SupportWeight > rightMotion.SupportWeight ||
                 Mathf.Abs(leftMotion.SupportWeight - rightMotion.SupportWeight) <=
                 GeometryEpsilon &&
                 leftMotion.SupportHorizontalError <= rightMotion.SupportHorizontalError);
            primarySupport.HasValue = true;
            primarySupport.Side = selectLeft
                ? CharacterFootSide.Left
                : CharacterFootSide.Right;
            primarySupport.LandingEventIdentity = selectLeft
                ? leftMotion.SupportEventIdentity
                : rightMotion.SupportEventIdentity;
            primarySupport.Retained = false;
        }

        internal static CharacterFootStrideIntentResult ResolveIntent(
            in AnimationFootMotionStep leftSwingStep,
            in AnimationFootMotionStep rightSwingStep,
            bool hasSelectedSwing,
            CharacterFootSide selectedSwingSide,
            bool hasLeftNextSwingLanding,
            in CharacterFootGroundPathLanding leftNextSwingLanding,
            bool hasRightNextSwingLanding,
            in CharacterFootGroundPathLanding rightNextSwingLanding,
            bool leftGroundPathAccepted,
            bool rightGroundPathAccepted,
            bool grounded,
            bool actionOccupied,
            in CharacterResolvedFootPair resolvedPair,
            in CharacterFootPrimarySupportResult primarySupport,
            Vector3 componentUp)
        {
            if (!grounded)
            {
                return new CharacterFootStrideIntentResult(
                    CharacterFootStrideRejectReason.BodyNotGrounded,
                    default,
                    default,
                    default,
                    default,
                    0f,
                    false);
            }
            if (actionOccupied)
            {
                return new CharacterFootStrideIntentResult(
                    CharacterFootStrideRejectReason.ActionOccupied,
                    default,
                    default,
                    default,
                    default,
                    0f,
                    false);
            }
            Vector3 primarySupportContactAnchor = primarySupport.HasValue
                ? primarySupport.Side == CharacterFootSide.Left
                    ? resolvedPair.Left.PelvisReachReference.Point
                    : resolvedPair.Right.PelvisReachReference.Point
                : default;
            if (!TryResolveStride(
                    in leftSwingStep,
                    in rightSwingStep,
                    hasSelectedSwing,
                    selectedSwingSide,
                    primarySupport.HasValue,
                    primarySupport.Side,
                    primarySupport.LandingEventIdentity,
                    primarySupportContactAnchor,
                    hasLeftNextSwingLanding,
                    hasLeftNextSwingLanding
                        ? leftNextSwingLanding.Point
                        : default,
                    hasLeftNextSwingLanding
                        ? leftNextSwingLanding.LandingEventIdentity
                        : 0,
                    hasRightNextSwingLanding,
                    hasRightNextSwingLanding
                        ? rightNextSwingLanding.Point
                        : default,
                    hasRightNextSwingLanding
                        ? rightNextSwingLanding.LandingEventIdentity
                        : 0,
                    componentUp,
                    out CharacterFootSide supportSide,
                    out CharacterFootSide swingSide,
                    out Vector3 strideStart,
                    out Vector3 strideEnd,
                    out CharacterFootStrideRejectReason rejectReason))
            {
                return new CharacterFootStrideIntentResult(
                    rejectReason,
                    default,
                    default,
                    default,
                    default,
                    0f,
                    true);
            }
            bool groundPathAccepted = swingSide == CharacterFootSide.Left
                ? leftGroundPathAccepted
                : rightGroundPathAccepted;
            if (!groundPathAccepted)
            {
                return new CharacterFootStrideIntentResult(
                    CharacterFootStrideRejectReason.GroundPathRejected,
                    default,
                    default,
                    default,
                    default,
                    0f,
                    true);
            }
            float swingTimeToLanding = swingSide == CharacterFootSide.Left
                ? leftSwingStep.TimeToLandingSeconds
                : rightSwingStep.TimeToLandingSeconds;
            return new CharacterFootStrideIntentResult(
                CharacterFootStrideRejectReason.None,
                supportSide,
                swingSide,
                strideStart,
                strideEnd,
                swingTimeToLanding,
                false);
        }

        internal static bool TrySelectSwing(
            in AnimationFootMotionStep leftStep,
            in AnimationFootMotionStep rightStep,
            in CharacterFootSwingMotionResult leftMotion,
            in CharacterFootSwingMotionResult rightMotion,
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
            in AnimationFootMotionStep leftSwingStep,
            in AnimationFootMotionStep rightSwingStep,
            bool hasSelectedSwing,
            CharacterFootSide selectedSwingSide,
            bool hasPrimarySupport,
            CharacterFootSide primarySupportSide,
            ulong primarySupportLandingEventIdentity,
            Vector3 primarySupportContactAnchor,
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
                primarySupportSide == selectedSwingSide ||
                !Finite(primarySupportContactAnchor))
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
                strideStart = primarySupportContactAnchor;
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
                strideStart = primarySupportContactAnchor;
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

        internal static CharacterFootStrideHipsResult BuildRejected(
            CharacterFootStrideRejectReason reason) =>
            new CharacterFootStrideHipsResult(
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

        internal static CharacterFootStrideHipsResult BuildPelvis(
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
            return new CharacterFootStrideHipsResult(
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

        internal static CharacterFootStrideHipsResult ApplyLandingReach(
            in CharacterFootStrideHipsResult source,
            bool leftAvailable,
            Vector3 leftHip,
            Vector3 leftTargetAnkle,
            float leftLegLength,
            bool rightAvailable,
            Vector3 rightHip,
            Vector3 rightTargetAnkle,
            float rightLegLength,
            Vector3 componentUp,
            float footPlacementWeight,
            in CharacterFootMotionSettings settings,
            ref CharacterFootPelvisSpringState spring)
        {
            if (!Finite(componentUp) ||
                componentUp.sqrMagnitude <= GeometryEpsilon ||
                !leftAvailable && !rightAvailable ||
                !float.IsFinite(footPlacementWeight) ||
                footPlacementWeight < 0f ||
                footPlacementWeight > 1f)
            {
                return source;
            }
            Vector3 up = componentUp.normalized;
            float minimum = float.NegativeInfinity;
            float maximum = float.PositiveInfinity;
            if (leftAvailable)
            {
                if (!Finite(leftHip) ||
                    !Finite(leftTargetAnkle) ||
                    !float.IsFinite(leftLegLength) ||
                    leftLegLength <= EndpointTolerance ||
                    !TryResolveSupportReachInterval(
                        leftHip,
                        leftTargetAnkle,
                        up,
                        leftLegLength,
                        settings.MinimumLandingLegCompressionReserve,
                        out _,
                        out float leftMinimum,
                        out float leftMaximum))
                {
                    return source;
                }
                minimum = Mathf.Max(minimum, leftMinimum);
                maximum = Mathf.Min(maximum, leftMaximum);
            }
            if (rightAvailable)
            {
                if (!Finite(rightHip) ||
                    !Finite(rightTargetAnkle) ||
                    !float.IsFinite(rightLegLength) ||
                    rightLegLength <= EndpointTolerance ||
                    !TryResolveSupportReachInterval(
                        rightHip,
                        rightTargetAnkle,
                        up,
                        rightLegLength,
                        settings.MinimumLandingLegCompressionReserve,
                        out _,
                        out float rightMinimum,
                        out float rightMaximum))
                {
                    return source;
                }
                minimum = Mathf.Max(minimum, rightMinimum);
                maximum = Mathf.Min(maximum, rightMaximum);
            }
            if (minimum > maximum)
                return source;
            if (source.SupportReachAvailable)
            {
                minimum = Mathf.Max(
                    minimum,
                    source.SupportReachMinimumAlongUp);
                maximum = Mathf.Min(
                    maximum,
                    source.SupportReachMaximumAlongUp);
                if (minimum > maximum)
                    return source;
            }
            float sourceTarget = source.ProducesPelvisGoal
                ? source.SpringTarget
                : 0f;
            float sourceOutput = source.ProducesPelvisGoal
                ? source.SpringOutput
                : 0f;
            float target = Mathf.Clamp(sourceTarget, minimum, maximum);
            float output = Mathf.Clamp(sourceOutput, minimum, maximum);
            bool targetClamped =
                Mathf.Abs(target - sourceTarget) > GeometryEpsilon;
            bool outputClamped =
                Mathf.Abs(output - sourceOutput) > GeometryEpsilon;
            if (!source.ProducesPelvisGoal && !targetClamped && !outputClamped)
                return source;
            float velocity = source.ProducesPelvisGoal
                ? source.SpringVelocity
                : 0f;
            if (outputClamped &&
                (output <= minimum && velocity < 0f ||
                 output >= maximum && velocity > 0f))
            {
                velocity = 0f;
            }
            spring.HasValue = true;
            spring.TargetAlongUp = target;
            spring.OutputAlongUp = output;
            spring.VelocityAlongUp = velocity;
            CharacterFootStrideState state = source.ProducesPelvisGoal
                ? source.State
                : CharacterFootStrideState.LandingReach;
            Vector3 pelvisDelta = up * output;
            float positionWeight = Mathf.Abs(output) > EndpointTolerance
                ? footPlacementWeight
                : 0f;
            return source.WithLandingReachOutput(
                state,
                target,
                output,
                velocity,
                pelvisDelta,
                positionWeight);
        }

        internal static CharacterFootStrideHipsResult BuildPelvisRelease(
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
            return new CharacterFootStrideHipsResult(
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

        static bool IsAuthoritativeSwing(in AnimationFootMotionStep step) =>
            step.IsValid &&
            step.IsAuthoritative &&
            step.IsSwing &&
            step.HasConsistentLandingEventIdentity;

        static bool IsRetainablePrimarySupport(
            in CharacterResolvedFootResult motion) =>
            motion.Outcome == CharacterFootResolvedOutcome.Ready &&
            motion.PelvisReachReference.IsAvailable &&
            motion.SupportEventIdentity != 0 &&
            motion.SupportWeight > GeometryEpsilon &&
            motion.SupportEligibility != CharacterFootSupportEligibility.None;

        static bool IsAcquirablePrimarySupport(
            in CharacterResolvedFootResult motion) =>
            IsRetainablePrimarySupport(in motion) &&
            motion.SupportEligibility ==
            CharacterFootSupportEligibility.AcquireAndRetain;

        static CharacterFootSwingMotionResult RejectedPlant(
            CharacterFootSwingMotionRejectReason reason,
            ulong landingEventIdentity,
            Vector3 originalSole,
            Vector3 originalAnkle) =>
            new CharacterFootSwingMotionResult(
                CharacterFootSwingMotionState.Rejected,
                reason,
                landingEventIdentity,
                0,
                default,
                originalSole,
                originalAnkle,
                0f,
                0f,
                default,
                default,
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
