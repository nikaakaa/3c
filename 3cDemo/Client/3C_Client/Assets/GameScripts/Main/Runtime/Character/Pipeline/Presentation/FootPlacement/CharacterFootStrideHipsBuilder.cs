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
        GroundPathRejected = 8,
        InvalidInput = 9,
        SupportUnavailable = 10
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
            bool releasePelvis)
        {
            RejectReason = rejectReason;
            SupportSide = supportSide;
            SwingSide = swingSide;
            StrideStart = strideStart;
            StrideEnd = strideEnd;
            ReleasePelvis = releasePelvis;
            Accepted = rejectReason == CharacterFootStrideRejectReason.None;
        }

        internal bool Accepted { get; }
        internal CharacterFootStrideRejectReason RejectReason { get; }
        internal CharacterFootSide SupportSide { get; }
        internal CharacterFootSide SwingSide { get; }
        internal Vector3 StrideStart { get; }
        internal Vector3 StrideEnd { get; }
        internal bool ReleasePelvis { get; }
    }

    internal readonly struct CharacterFootPelvisHeightTarget
    {
        internal CharacterFootPelvisHeightTarget(
            Vector3 componentUp,
            Vector3 leftAnimatedSole,
            Vector3 rightAnimatedSole,
            Vector3 leftTargetSole,
            Vector3 rightTargetSole)
        {
            if (!CharacterFootConstraintMath.Finite(componentUp) ||
                Mathf.Abs(componentUp.sqrMagnitude - 1f) >
                CharacterFootConstraintMath.GeometryEpsilon ||
                !CharacterFootConstraintMath.Finite(leftAnimatedSole) ||
                !CharacterFootConstraintMath.Finite(rightAnimatedSole) ||
                !CharacterFootConstraintMath.Finite(leftTargetSole) ||
                !CharacterFootConstraintMath.Finite(rightTargetSole))
            {
                throw new System.ArgumentException("Pelvis height target input is invalid.");
            }
            ComponentUp = componentUp;
            LeftAnimatedSole = leftAnimatedSole;
            RightAnimatedSole = rightAnimatedSole;
            LeftTargetSole = leftTargetSole;
            RightTargetSole = rightTargetSole;
            AnimatedMinimumAlongUp = Mathf.Min(
                Vector3.Dot(leftAnimatedSole, componentUp),
                Vector3.Dot(rightAnimatedSole, componentUp));
            TargetMinimumAlongUp = Mathf.Min(
                Vector3.Dot(leftTargetSole, componentUp),
                Vector3.Dot(rightTargetSole, componentUp));
            OffsetAlongUp = TargetMinimumAlongUp - AnimatedMinimumAlongUp;
            if (!float.IsFinite(AnimatedMinimumAlongUp) ||
                !float.IsFinite(TargetMinimumAlongUp) ||
                !float.IsFinite(OffsetAlongUp))
            {
                throw new System.ArgumentException("Pelvis height target result is invalid.");
            }
            Available = true;
        }

        internal bool Available { get; }
        internal Vector3 ComponentUp { get; }
        internal Vector3 LeftAnimatedSole { get; }
        internal Vector3 RightAnimatedSole { get; }
        internal Vector3 LeftTargetSole { get; }
        internal Vector3 RightTargetSole { get; }
        internal float AnimatedMinimumAlongUp { get; }
        internal float TargetMinimumAlongUp { get; }
        internal float OffsetAlongUp { get; }
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

    public enum CharacterFootPelvisLegReachStatus : byte
    {
        NotRequested = 0,
        Available = 1,
        HorizontalUnreachable = 2
    }

    [Flags]
    public enum CharacterFootPelvisLegReachRole : byte
    {
        None = 0,
        FootTarget = 1,
        PrimarySupport = 2
    }

    public enum CharacterFootPelvisReachStatus : byte
    {
        NotRequested = 0,
        Available = 1,
        LegUnreachable = 2,
        NoCommonInterval = 3
    }

    public enum CharacterFootPelvisBoundarySelection : byte
    {
        None = 0,
        PrimarySupport = 1
    }

    internal readonly struct CharacterFootPelvisReachInput
    {
        internal CharacterFootPelvisReachInput(
            bool leftRequested,
            in CharacterFootLandingReachRequest left,
            bool rightRequested,
            in CharacterFootLandingReachRequest right)
        {
            if (leftRequested && !left.IsAvailable ||
                rightRequested && !right.IsAvailable)
                throw new ArgumentException("Pelvis Reach requires a formal foot request.");
            LeftRequested = leftRequested;
            Left = leftRequested ? left : default;
            RightRequested = rightRequested;
            Right = rightRequested ? right : default;
        }

        internal bool LeftRequested { get; }
        internal CharacterFootLandingReachRequest Left { get; }
        internal bool RightRequested { get; }
        internal CharacterFootLandingReachRequest Right { get; }
    }

    internal readonly struct CharacterFootPelvisLegReach
    {
        internal CharacterFootPelvisLegReach(
            in CharacterFootLandingReachRequest request,
            CharacterFootPelvisLegReachRole role,
            Vector3 up)
        {
            if (!request.IsAvailable || role == CharacterFootPelvisLegReachRole.None)
                throw new ArgumentException("Pelvis leg reach input is unavailable.");
            Role = role;
            EventIdentity = request.EventIdentity;
            Hip = request.Hip;
            TargetAnkle = request.TargetAnkle;
            LegLength = request.LegLength;
            MinimumCompressionReserve = request.MinimumCompressionReserve;
            UsableLegLength = LegLength - MinimumCompressionReserve;
            Vector3 hipFromTarget = Hip - TargetAnkle;
            float vertical = Vector3.Dot(hipFromTarget, up);
            float horizontalSquare = (hipFromTarget - up * vertical).sqrMagnitude;
            float verticalReachSquare =
                UsableLegLength * UsableLegLength - horizontalSquare;
            if (!float.IsFinite(verticalReachSquare) || !float.IsFinite(vertical))
                throw new ArgumentException("Pelvis leg reach geometry is non-finite.");
            if (verticalReachSquare < 0f)
            {
                Status = CharacterFootPelvisLegReachStatus.HorizontalUnreachable;
                MinimumAlongUp = 0f;
                MaximumAlongUp = 0f;
                return;
            }
            float verticalReach = Mathf.Sqrt(verticalReachSquare);
            MinimumAlongUp = -vertical - verticalReach;
            MaximumAlongUp = -vertical + verticalReach;
            if (!float.IsFinite(MinimumAlongUp) || !float.IsFinite(MaximumAlongUp))
                throw new ArgumentException("Pelvis leg reach interval is non-finite.");
            Status = CharacterFootPelvisLegReachStatus.Available;
        }

        internal CharacterFootPelvisLegReachRole Role { get; }
        internal CharacterFootPelvisLegReachStatus Status { get; }
        internal ulong EventIdentity { get; }
        internal Vector3 Hip { get; }
        internal Vector3 TargetAnkle { get; }
        internal float LegLength { get; }
        internal float MinimumCompressionReserve { get; }
        internal float UsableLegLength { get; }
        internal float MinimumAlongUp { get; }
        internal float MaximumAlongUp { get; }
        internal bool Requested => Role != CharacterFootPelvisLegReachRole.None;
        internal bool Available => Status == CharacterFootPelvisLegReachStatus.Available;
        internal bool LandingRequested => (Role & CharacterFootPelvisLegReachRole.FootTarget) != 0;
        internal bool PrimarySupport => (Role & CharacterFootPelvisLegReachRole.PrimarySupport) != 0;

        internal bool Contains(float output, float tolerance) =>
            Available &&
            output >= MinimumAlongUp - tolerance &&
            output <= MaximumAlongUp + tolerance;
    }

    internal readonly struct CharacterFootPelvisReachBoundary
    {
        internal CharacterFootPelvisReachBoundary(
            Vector3 componentUp,
            in CharacterFootPelvisLegReach left,
            in CharacterFootPelvisLegReach right)
        {
            ComponentUp = componentUp;
            Left = left;
            Right = right;
            Status = CharacterFootPelvisReachStatus.NotRequested;
            Selection = CharacterFootPelvisBoundarySelection.None;
            IntersectionEvaluated = false;
            IntersectionMinimumAlongUp = 0f;
            IntersectionMaximumAlongUp = 0f;
            MinimumAlongUp = 0f;
            MaximumAlongUp = 0f;
            if (!left.Requested && !right.Requested)
                return;
            if (left.Requested && !left.Available ||
                right.Requested && !right.Available)
            {
                Status = CharacterFootPelvisReachStatus.LegUnreachable;
            }
            else
            {
                float minimum = left.Requested
                    ? left.MinimumAlongUp
                    : right.MinimumAlongUp;
                float maximum = left.Requested
                    ? left.MaximumAlongUp
                    : right.MaximumAlongUp;
                if (left.Requested && right.Requested)
                {
                    minimum = Mathf.Max(minimum, right.MinimumAlongUp);
                    maximum = Mathf.Min(maximum, right.MaximumAlongUp);
                }
                IntersectionEvaluated = true;
                IntersectionMinimumAlongUp = minimum;
                IntersectionMaximumAlongUp = maximum;
                Status = minimum <= maximum
                    ? CharacterFootPelvisReachStatus.Available
                    : CharacterFootPelvisReachStatus.NoCommonInterval;
            }
            CharacterFootPelvisLegReach primary = left.PrimarySupport
                ? left
                : right.PrimarySupport ? right : default;
            if (primary.Available)
            {
                Selection = CharacterFootPelvisBoundarySelection.PrimarySupport;
                MinimumAlongUp = primary.MinimumAlongUp;
                MaximumAlongUp = primary.MaximumAlongUp;
            }
        }

        internal Vector3 ComponentUp { get; }
        internal CharacterFootPelvisLegReach Left { get; }
        internal CharacterFootPelvisLegReach Right { get; }
        internal CharacterFootPelvisReachStatus Status { get; }
        internal CharacterFootPelvisBoundarySelection Selection { get; }
        internal bool IntersectionEvaluated { get; }
        internal float IntersectionMinimumAlongUp { get; }
        internal float IntersectionMaximumAlongUp { get; }
        internal float MinimumAlongUp { get; }
        internal float MaximumAlongUp { get; }
        internal bool Available => Selection != CharacterFootPelvisBoundarySelection.None;
        internal bool HasLandingRequests => Left.LandingRequested || Right.LandingRequested;

        internal bool Contains(float output, float tolerance) =>
            Available &&
            output >= MinimumAlongUp - tolerance &&
            output <= MaximumAlongUp + tolerance;
    }

    public readonly struct CharacterFootPelvisLegReachDiagnostics
    {
        readonly CharacterFootPelvisLegReach m_Result;

        internal CharacterFootPelvisLegReachDiagnostics(
            in CharacterFootPelvisLegReach result) => m_Result = result;

        public CharacterFootPelvisLegReachRole Role => m_Result.Role;
        public CharacterFootPelvisLegReachStatus Status => m_Result.Status;
        public ulong EventIdentity => m_Result.EventIdentity;
        public Vector3 Hip => m_Result.Hip;
        public Vector3 TargetAnkle => m_Result.TargetAnkle;
        public float LegLength => m_Result.LegLength;
        public float MinimumCompressionReserve => m_Result.MinimumCompressionReserve;
        public float UsableLegLength => m_Result.UsableLegLength;
        public float MinimumAlongUp => m_Result.MinimumAlongUp;
        public float MaximumAlongUp => m_Result.MaximumAlongUp;
        public bool Requested => m_Result.Requested;
        public bool Available => m_Result.Available;
    }

    public readonly struct CharacterFootPelvisReachDiagnostics
    {
        readonly CharacterFootPelvisReachBoundary m_Result;

        internal CharacterFootPelvisReachDiagnostics(
            in CharacterFootPelvisReachBoundary result) => m_Result = result;

        public Vector3 ComponentUp => m_Result.ComponentUp;
        public CharacterFootPelvisLegReachDiagnostics Left => new(m_Result.Left);
        public CharacterFootPelvisLegReachDiagnostics Right => new(m_Result.Right);
        public CharacterFootPelvisReachStatus Status => m_Result.Status;
        public CharacterFootPelvisBoundarySelection Selection => m_Result.Selection;
        public bool IntersectionEvaluated => m_Result.IntersectionEvaluated;
        public float IntersectionMinimumAlongUp => m_Result.IntersectionMinimumAlongUp;
        public float IntersectionMaximumAlongUp => m_Result.IntersectionMaximumAlongUp;
        public bool Available => m_Result.Available;
        public float MinimumAlongUp => m_Result.MinimumAlongUp;
        public float MaximumAlongUp => m_Result.MaximumAlongUp;
    }

    internal readonly struct CharacterFootPelvisPosturePreference
    {
        internal CharacterFootPelvisPosturePreference(
            bool available,
            Vector3 hip,
            Vector3 animatedAnkle,
            Vector3 targetAnkle,
            float legLength,
            float compressionReserve,
            float usableLegLength,
            float minimumAlongUp,
            float maximumAlongUp,
            float offsetAlongUp,
            bool targetAdjusted)
        {
            Evaluated = true;
            Available = available;
            Hip = hip;
            AnimatedAnkle = animatedAnkle;
            TargetAnkle = targetAnkle;
            LegLength = legLength;
            CompressionReserve = compressionReserve;
            UsableLegLength = usableLegLength;
            MinimumAlongUp = minimumAlongUp;
            MaximumAlongUp = maximumAlongUp;
            OffsetAlongUp = offsetAlongUp;
            TargetAdjusted = targetAdjusted;
        }

        internal bool Evaluated { get; }
        internal bool Available { get; }
        internal Vector3 Hip { get; }
        internal Vector3 AnimatedAnkle { get; }
        internal Vector3 TargetAnkle { get; }
        internal float LegLength { get; }
        internal float CompressionReserve { get; }
        internal float UsableLegLength { get; }
        internal float MinimumAlongUp { get; }
        internal float MaximumAlongUp { get; }
        internal float OffsetAlongUp { get; }
        internal bool TargetAdjusted { get; }

    }

    internal readonly struct CharacterFootPelvisSpringStep
    {
        internal CharacterFootPelvisSpringStep(
            bool evaluated,
            bool completed,
            bool hadPreviousState,
            bool supportChanged,
            CharacterFootStrideSlope previousSlope,
            CharacterFootPelvisSpringHandoffReason handoffReason,
            bool velocityReset,
            float previousTarget,
            float previousOutput,
            float previousVelocity,
            float input,
            float inputVelocity,
            float frequency,
            float unconstrainedOutput,
            bool targetClamped,
            bool outputClamped,
            bool boundaryVelocityCleared,
            float target,
            float output,
            float velocity,
            float positionWeight)
        {
            Evaluated = evaluated;
            Completed = completed;
            HadPreviousState = hadPreviousState;
            SupportChanged = supportChanged;
            PreviousSlope = previousSlope;
            HandoffReason = handoffReason;
            VelocityReset = velocityReset;
            PreviousTarget = previousTarget;
            PreviousOutput = previousOutput;
            PreviousVelocity = previousVelocity;
            Input = input;
            InputVelocity = inputVelocity;
            Frequency = frequency;
            UnconstrainedOutput = unconstrainedOutput;
            TargetClamped = targetClamped;
            OutputClamped = outputClamped;
            BoundaryVelocityCleared = boundaryVelocityCleared;
            Target = target;
            Output = output;
            Velocity = velocity;
            PositionWeight = positionWeight;
        }

        internal bool Evaluated { get; }
        internal bool Completed { get; }
        internal bool HadPreviousState { get; }
        internal bool SupportChanged { get; }
        internal CharacterFootStrideSlope PreviousSlope { get; }
        internal CharacterFootPelvisSpringHandoffReason HandoffReason { get; }
        internal bool VelocityReset { get; }
        internal float PreviousTarget { get; }
        internal float PreviousOutput { get; }
        internal float PreviousVelocity { get; }
        internal float Input { get; }
        internal float InputVelocity { get; }
        internal float Frequency { get; }
        internal float UnconstrainedOutput { get; }
        internal bool TargetClamped { get; }
        internal bool OutputClamped { get; }
        internal bool BoundaryVelocityCleared { get; }
        internal float Target { get; }
        internal float Output { get; }
        internal float Velocity { get; }
        internal float PositionWeight { get; }

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
            bool poseInputAvailable,
            Vector3 poseRootPosition,
            Vector3 animatedPelvis,
            Vector3 animatedPelvisComponentPosition,
            CharacterFootPelvisHeightTarget heightTarget,
            CharacterFootPelvisPosturePreference posturePreference,
            CharacterFootPelvisReachBoundary reach,
            CharacterFootPelvisSpringStep response)
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
            PoseInputAvailable = poseInputAvailable;
            PoseRootPosition = poseRootPosition;
            AnimatedPelvis = animatedPelvis;
            AnimatedPelvisComponentPosition = animatedPelvisComponentPosition;
            HeightTarget = heightTarget;
            PosturePreference = posturePreference;
            Reach = reach;
            Response = response;
        }

        internal CharacterFootStrideState State { get; }
        internal CharacterFootStrideRejectReason RejectReason { get; }
        internal CharacterFootSide SupportSide { get; }
        internal CharacterFootSide SwingSide { get; }
        internal Vector3 StrideStart { get; }
        internal Vector3 StrideEnd { get; }
        internal float Progress { get; }
        internal CharacterFootStrideSlope Slope { get; }
        internal Vector3 SampledGround { get; }
        internal bool PoseInputAvailable { get; }
        internal Vector3 PoseRootPosition { get; }
        internal Vector3 AnimatedPelvis { get; }
        internal Vector3 AnimatedPelvisComponentPosition { get; }
        internal CharacterFootPelvisHeightTarget HeightTarget { get; }
        internal CharacterFootPelvisPosturePreference PosturePreference { get; }
        internal CharacterFootPelvisReachBoundary Reach { get; }
        internal CharacterFootPelvisSpringStep Response { get; }

        internal bool Accepted => State == CharacterFootStrideState.Accepted;
        internal bool ProducesPelvisGoal =>
            State == CharacterFootStrideState.Accepted ||
            State == CharacterFootStrideState.Releasing;
        internal float SpringTarget => Response.Target;
        internal float SpringOutput => Response.Output;
        internal float SpringVelocity => Response.Velocity;
        internal Vector3 PelvisDelta => Reach.ComponentUp * Response.Output;
        internal float PositionWeight => Response.PositionWeight;
        internal bool LeftLandingReachAvailable =>
            Reach.Left.LandingRequested &&
            Reach.Left.Contains(Response.Output * Response.PositionWeight,
                CharacterFootConstraintMath.GeometryEpsilon);
        internal bool RightLandingReachAvailable =>
            Reach.Right.LandingRequested &&
            Reach.Right.Contains(Response.Output * Response.PositionWeight,
                CharacterFootConstraintMath.GeometryEpsilon);
        internal bool LeftLandingReachEnforced => Reach.Left.PrimarySupport;
        internal bool RightLandingReachEnforced => Reach.Right.PrimarySupport;

    }

    public readonly struct CharacterFootStrideHipsDiagnostics
    {
        readonly CharacterFootStrideHipsResult m_Result;

        internal CharacterFootStrideHipsDiagnostics(
            in CharacterFootStrideHipsResult result) => m_Result = result;

        public CharacterFootStrideState State => m_Result.State;
        public CharacterFootStrideRejectReason RejectReason => m_Result.RejectReason;
        public CharacterFootSide SupportSide => m_Result.SupportSide;
        public CharacterFootSide SwingSide => m_Result.SwingSide;
        public Vector3 StrideStart => m_Result.StrideStart;
        public Vector3 StrideEnd => m_Result.StrideEnd;
        public float Progress => m_Result.Progress;
        public CharacterFootStrideSlope Slope => m_Result.Slope;
        public Vector3 SampledGround => m_Result.SampledGround;
        public bool PoseInputAvailable => m_Result.PoseInputAvailable;
        public Vector3 PoseRootPosition => m_Result.PoseRootPosition;
        public Vector3 AnimatedPelvis => m_Result.AnimatedPelvis;
        public Vector3 AnimatedPelvisComponentPosition => m_Result.AnimatedPelvisComponentPosition;
        public float SpringTarget => m_Result.SpringTarget;
        public float SpringOutput => m_Result.SpringOutput;
        public float SpringVelocity => m_Result.SpringVelocity;
        public Vector3 PelvisDelta => m_Result.PelvisDelta;
        public float PositionWeight => m_Result.PositionWeight;
        public bool Accepted => m_Result.Accepted;
        public bool ProducesPelvisGoal => m_Result.ProducesPelvisGoal;
        public bool HeightTargetAvailable => m_Result.HeightTarget.Available;
        public Vector3 HeightTargetComponentUp => m_Result.HeightTarget.ComponentUp;
        public Vector3 HeightTargetLeftAnimatedSole => m_Result.HeightTarget.LeftAnimatedSole;
        public Vector3 HeightTargetRightAnimatedSole => m_Result.HeightTarget.RightAnimatedSole;
        public Vector3 HeightTargetLeftTargetSole => m_Result.HeightTarget.LeftTargetSole;
        public Vector3 HeightTargetRightTargetSole => m_Result.HeightTarget.RightTargetSole;
        public float HeightTargetAnimatedMinimumAlongUp => m_Result.HeightTarget.AnimatedMinimumAlongUp;
        public float HeightTargetMinimumAlongUp => m_Result.HeightTarget.TargetMinimumAlongUp;
        public float RequestedOffsetAlongUp => m_Result.HeightTarget.OffsetAlongUp;
        public CharacterFootPelvisReachDiagnostics Reach => new(m_Result.Reach);
        public bool PosturePreferenceEvaluated => m_Result.PosturePreference.Evaluated;
        public bool PosturePreferenceAvailable => m_Result.PosturePreference.Available;
        public Vector3 PosturePreferenceHip => m_Result.PosturePreference.Hip;
        public Vector3 PosturePreferenceAnimatedAnkle => m_Result.PosturePreference.AnimatedAnkle;
        public Vector3 PosturePreferenceTargetAnkle => m_Result.PosturePreference.TargetAnkle;
        public float PosturePreferenceLegLength => m_Result.PosturePreference.LegLength;
        public float PosturePreferenceCompressionReserve => m_Result.PosturePreference.CompressionReserve;
        public float PosturePreferenceUsableLegLength => m_Result.PosturePreference.UsableLegLength;
        public float PosturePreferenceMinimumAlongUp => m_Result.PosturePreference.MinimumAlongUp;
        public float PosturePreferenceMaximumAlongUp => m_Result.PosturePreference.MaximumAlongUp;
        public float PosturePreferenceOffsetAlongUp => m_Result.PosturePreference.OffsetAlongUp;
        public bool PosturePreferenceTargetAdjusted => m_Result.PosturePreference.TargetAdjusted;
        public bool ResponseEvaluated => m_Result.Response.Evaluated;
        public bool SpringCompleted => m_Result.Response.Completed;
        public bool HadPreviousState => m_Result.Response.HadPreviousState;
        public bool SupportChanged => m_Result.Response.SupportChanged;
        public CharacterFootStrideSlope PreviousSlope => m_Result.Response.PreviousSlope;
        public CharacterFootPelvisSpringHandoffReason SpringHandoffReason => m_Result.Response.HandoffReason;
        public bool SpringVelocityReset => m_Result.Response.VelocityReset;
        public float PreviousSpringTarget => m_Result.Response.PreviousTarget;
        public float PreviousSpringOutput => m_Result.Response.PreviousOutput;
        public float PreviousSpringVelocity => m_Result.Response.PreviousVelocity;
        public float SpringInput => m_Result.Response.Input;
        public float SpringInputVelocity => m_Result.Response.InputVelocity;
        public float SpringFrequency => m_Result.Response.Frequency;
        public float SpringUnconstrainedOutput => m_Result.Response.UnconstrainedOutput;
        public bool HardReachTargetClamped => m_Result.Response.TargetClamped;
        public bool HardReachOutputClamped => m_Result.Response.OutputClamped;
        public bool HardReachVelocityCleared => m_Result.Response.BoundaryVelocityCleared;
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
            in AnimationFootMotionRuntimeSample leftSwingStep,
            in AnimationFootMotionRuntimeSample rightSwingStep,
            bool hasSelectedSwing,
            CharacterFootSide selectedSwingSide,
            bool hasLeftNextSwingLanding,
            in CharacterFootGroundPathLanding leftNextSwingLanding,
            bool hasRightNextSwingLanding,
            in CharacterFootGroundPathLanding rightNextSwingLanding,
            bool leftGroundPathAccepted,
            bool rightGroundPathAccepted,
            bool grounded,
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
                    true);
            }
            return new CharacterFootStrideIntentResult(
                CharacterFootStrideRejectReason.None,
                supportSide,
                swingSide,
                strideStart,
                strideEnd,
                false);
        }

        internal static bool TrySelectSwing(
            in AnimationFootMotionRuntimeSample leftStep,
            in AnimationFootMotionRuntimeSample rightStep,
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
            in AnimationFootMotionRuntimeSample leftSwingStep,
            in AnimationFootMotionRuntimeSample rightSwingStep,
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

        internal static CharacterFootStrideHipsResult ResolvePelvis(
            in CharacterFootStrideIntentResult intent,
            in CharacterResolvedFootPair resolvedPair,
            in CharacterFootPrimarySupportResult primarySupport,
            in CharacterFootPelvisFrame frame,
            in CharacterFootPelvisReachInput reachInput,
            in CharacterFootMotionSettings settings,
            ref CharacterFootPelvisSpringState spring)
        {
            if (!intent.Accepted)
            {
                if (!intent.ReleasePelvis)
                {
                    spring.Clear();
                    return BuildRejected(intent.RejectReason);
                }
                return ResolvePelvisRelease(
                    intent.RejectReason, in frame, in reachInput, in settings, ref spring);
            }
            CharacterResolvedFootResult supportMotion = intent.SupportSide == CharacterFootSide.Left
                ? resolvedPair.Left
                : resolvedPair.Right;
            if (supportMotion.Outcome != CharacterFootResolvedOutcome.Ready ||
                !supportMotion.PelvisReachReference.IsAvailable ||
                supportMotion.SupportWeight <= CharacterPoseConstraintMath.Epsilon ||
                supportMotion.SupportEligibility == CharacterFootSupportEligibility.None ||
                supportMotion.SupportEventIdentity != primarySupport.LandingEventIdentity)
            {
                return ResolvePelvisRelease(
                    CharacterFootStrideRejectReason.SupportUnavailable,
                    in frame, in reachInput, in settings, ref spring);
            }
            ValidatePelvisFrame(in frame);
            Vector3 up = frame.ComponentUp.normalized;
            Vector3 horizontal = Vector3.ProjectOnPlane(intent.StrideEnd - intent.StrideStart, up);
            float pathLength = horizontal.magnitude;
            if (!Finite(intent.StrideStart) || !Finite(intent.StrideEnd) ||
                !float.IsFinite(pathLength) || pathLength <= GeometryEpsilon)
            {
                return ResolvePelvisRelease(
                    CharacterFootStrideRejectReason.DegenerateStride,
                    in frame, in reachInput, in settings, ref spring);
            }
            float progress = Mathf.Clamp01(
                Vector3.Dot(
                    Vector3.ProjectOnPlane(frame.PoseRootPosition - intent.StrideStart, up),
                    horizontal / pathLength) / pathLength);
            Vector3 sampledGround = Vector3.Lerp(intent.StrideStart, intent.StrideEnd, progress);
            float rise = Vector3.Dot(intent.StrideEnd - intent.StrideStart, up);
            CharacterFootStrideSlope slope = rise > EndpointTolerance
                ? CharacterFootStrideSlope.Ascending
                : rise < -EndpointTolerance
                    ? CharacterFootStrideSlope.Descending
                    : CharacterFootStrideSlope.Flat;
            var heightTarget = new CharacterFootPelvisHeightTarget(
                up,
                frame.Pose.Left.HeelPosition * 0.5f + frame.Pose.Left.ToePosition * 0.5f,
                frame.Pose.Right.HeelPosition * 0.5f + frame.Pose.Right.ToePosition * 0.5f,
                frame.LeftCorrectedSole,
                frame.RightCorrectedSole);
            CharacterFootPlacementAnimatedFootPose supportPose =
                intent.SupportSide == CharacterFootSide.Left ? frame.Pose.Left : frame.Pose.Right;
            float supportLegLength = intent.SupportSide == CharacterFootSide.Left
                ? frame.LeftLegLength : frame.RightLegLength;
            if (!float.IsFinite(supportLegLength) || supportLegLength <= EndpointTolerance ||
                !Finite(supportPose.HipPosition) || !Finite(supportPose.AnklePosition) ||
                !Finite(supportMotion.EffectiveAnkle))
                throw new ArgumentException("Pelvis posture input is invalid.");
            float postureReserve = Mathf.Max(
                0f, supportLegLength -
                Vector3.Distance(supportPose.HipPosition, supportPose.AnklePosition));
            bool postureAvailable = TryResolvePostureInterval(
                supportPose.HipPosition, supportMotion.EffectiveAnkle, up,
                supportLegLength, postureReserve,
                out float postureUsableLength,
                out float postureMinimum, out float postureMaximum);
            float requestedTarget = heightTarget.OffsetAlongUp;
            float postureTarget = postureAvailable
                ? Mathf.Clamp(requestedTarget, postureMinimum, postureMaximum)
                : requestedTarget;
            float preferredTarget = Mathf.Clamp(
                postureTarget, Mathf.Min(0f, requestedTarget), Mathf.Max(0f, requestedTarget));
            var posture = new CharacterFootPelvisPosturePreference(
                postureAvailable,
                supportPose.HipPosition,
                supportPose.AnklePosition,
                supportMotion.EffectiveAnkle,
                supportLegLength,
                postureReserve,
                postureUsableLength,
                postureMinimum,
                postureMaximum,
                preferredTarget,
                Mathf.Abs(preferredTarget - heightTarget.OffsetAlongUp) > GeometryEpsilon);
            bool primaryRequired = supportMotion.GoalWeight > GeometryEpsilon;
            CharacterFootLandingReachRequest primaryRequest = primaryRequired
                ? new CharacterFootLandingReachRequest(
                    primarySupport.LandingEventIdentity,
                    supportPose.HipPosition,
                    supportMotion.EffectiveAnkle,
                    supportLegLength,
                    settings.MinimumLandingLegCompressionReserve)
                : default;
            CharacterFootPelvisReachBoundary reach = ResolveReachBoundary(
                up, in reachInput, primaryRequired, intent.SupportSide, in primaryRequest);
            CharacterFootPelvisSpringStep response = AdvancePelvisResponse(
                preferredTarget, false, intent.SupportSide,
                primarySupport.LandingEventIdentity, slope, in frame, in reach,
                in settings, ref spring);
            return new CharacterFootStrideHipsResult(
                CharacterFootStrideState.Accepted,
                CharacterFootStrideRejectReason.None,
                intent.SupportSide, intent.SwingSide, intent.StrideStart, intent.StrideEnd,
                progress, slope, sampledGround, true,
                frame.PoseRootPosition, frame.AnimatedPelvis, frame.AnimatedPelvisComponentPosition,
                heightTarget, posture, reach, response);
        }

        static CharacterFootStrideHipsResult BuildRejected(
            CharacterFootStrideRejectReason reason,
            CharacterFootPelvisReachBoundary reach = default,
            CharacterFootPelvisSpringStep response = default) =>
            new CharacterFootStrideHipsResult(
                CharacterFootStrideState.Rejected, reason,
                default, default, default, default, 0f,
                CharacterFootStrideSlope.Flat, default, false, default, default, default,
                default, default, reach, response);

        static CharacterFootStrideHipsResult ResolvePelvisRelease(
            CharacterFootStrideRejectReason reason,
            in CharacterFootPelvisFrame frame,
            in CharacterFootPelvisReachInput reachInput,
            in CharacterFootMotionSettings settings,
            ref CharacterFootPelvisSpringState spring)
        {
            ValidatePelvisFrame(in frame);
            Vector3 up = frame.ComponentUp.normalized;
            CharacterFootLandingReachRequest noPrimary = default;
            CharacterFootPelvisReachBoundary reach = ResolveReachBoundary(
                up, in reachInput, false, default, in noPrimary);
            if (!spring.HasValue)
                return BuildRejected(reason, reach);
            CharacterFootPelvisSpringStep response = AdvancePelvisResponse(
                0f, true, default, 0, CharacterFootStrideSlope.Flat,
                in frame, in reach, in settings, ref spring);
            if (response.Completed)
                return BuildRejected(reason, reach, response);
            return new CharacterFootStrideHipsResult(
                CharacterFootStrideState.Releasing,
                reason, default, default, default, default, 0f,
                CharacterFootStrideSlope.Flat, default, true,
                frame.PoseRootPosition, frame.AnimatedPelvis, frame.AnimatedPelvisComponentPosition,
                default, default, reach, response);
        }

        static void ValidatePelvisFrame(in CharacterFootPelvisFrame frame)
        {
            if (!Finite(frame.ComponentUp) ||
                frame.ComponentUp.sqrMagnitude <= GeometryEpsilon ||
                !Finite(frame.PoseRootPosition) ||
                !Finite(frame.AnimatedPelvis) ||
                !Finite(frame.AnimatedPelvisComponentPosition) ||
                !float.IsFinite(frame.FootPlacementWeight) ||
                frame.FootPlacementWeight < 0f || frame.FootPlacementWeight > 1f ||
                !float.IsFinite(frame.DeltaSeconds) || frame.DeltaSeconds < 0f)
                throw new ArgumentException("Pelvis response frame is invalid.");
        }

        static CharacterFootPelvisReachBoundary ResolveReachBoundary(
            Vector3 up,
            in CharacterFootPelvisReachInput input,
            bool hasPrimary,
            CharacterFootSide primarySide,
            in CharacterFootLandingReachRequest primary)
        {
            bool leftPrimary = hasPrimary && primarySide == CharacterFootSide.Left;
            bool rightPrimary = hasPrimary && primarySide == CharacterFootSide.Right;
            CharacterFootPelvisLegReach left = default;
            CharacterFootPelvisLegReach right = default;
            if (input.LeftRequested)
            {
                left = new CharacterFootPelvisLegReach(
                    input.Left,
                    CharacterFootPelvisLegReachRole.FootTarget |
                    (leftPrimary ? CharacterFootPelvisLegReachRole.PrimarySupport : 0),
                    up);
            }
            else if (leftPrimary)
            {
                left = new CharacterFootPelvisLegReach(
                    in primary, CharacterFootPelvisLegReachRole.PrimarySupport, up);
            }
            if (input.RightRequested)
            {
                right = new CharacterFootPelvisLegReach(
                    input.Right,
                    CharacterFootPelvisLegReachRole.FootTarget |
                    (rightPrimary ? CharacterFootPelvisLegReachRole.PrimarySupport : 0),
                    up);
            }
            else if (rightPrimary)
            {
                right = new CharacterFootPelvisLegReach(
                    in primary, CharacterFootPelvisLegReachRole.PrimarySupport, up);
            }
            return new CharacterFootPelvisReachBoundary(up, in left, in right);
        }

        static CharacterFootPelvisSpringStep AdvancePelvisResponse(
            float preferredTarget,
            bool releasing,
            CharacterFootSide supportSide,
            ulong supportEventIdentity,
            CharacterFootStrideSlope slope,
            in CharacterFootPelvisFrame frame,
            in CharacterFootPelvisReachBoundary reach,
            in CharacterFootMotionSettings settings,
            ref CharacterFootPelvisSpringState spring)
        {
            float target = preferredTarget;
            if (!float.IsFinite(target) || !float.IsFinite(settings.PelvisSpringFrequency) ||
                settings.PelvisSpringFrequency <= 0f)
                throw new ArgumentException("Pelvis spring target or frequency is invalid.");
            float minimum = 0f;
            float maximum = 0f;
            if (reach.Available)
            {
                if (frame.FootPlacementWeight <= 0f)
                    throw new InvalidOperationException("Pelvis hard reach has no active position weight.");
                minimum = reach.MinimumAlongUp / frame.FootPlacementWeight;
                maximum = reach.MaximumAlongUp / frame.FootPlacementWeight;
                if (!float.IsFinite(minimum) || !float.IsFinite(maximum))
                    throw new InvalidOperationException("Pelvis weighted reach interval is non-finite.");
                target = Mathf.Clamp(target, minimum, maximum);
            }
            bool targetClamped = Mathf.Abs(target - preferredTarget) > GeometryEpsilon;
            bool hadPreviousState = spring.HasValue;
            bool supportChanged = hadPreviousState &&
                (releasing || spring.SupportSide != supportSide ||
                 spring.SupportLandingEventIdentity != supportEventIdentity);
            CharacterFootStrideSlope previousSlope = hadPreviousState
                ? spring.Slope : CharacterFootStrideSlope.Flat;
            bool slopeChanged = hadPreviousState && previousSlope != slope;
            float previousTarget = hadPreviousState ? spring.TargetAlongUp : 0f;
            float previousOutput = hadPreviousState ? spring.OutputAlongUp : 0f;
            float previousVelocity = hadPreviousState ? spring.VelocityAlongUp : 0f;
            float previousTargetDirection = previousTarget - previousOutput;
            float nextTargetDirection = target - previousOutput;
            bool targetCrossedOutput = !releasing && hadPreviousState &&
                Mathf.Abs(previousTargetDirection) > EndpointTolerance &&
                Mathf.Abs(nextTargetDirection) > EndpointTolerance &&
                previousTargetDirection * nextTargetDirection < 0f;
            CharacterFootPelvisSpringHandoffReason handoff =
                CharacterFootPelvisSpringHandoffReason.None;
            if (supportChanged)
                handoff |= CharacterFootPelvisSpringHandoffReason.SupportChanged;
            if (slopeChanged)
                handoff |= CharacterFootPelvisSpringHandoffReason.SlopeChanged;
            if (targetCrossedOutput)
                handoff |= CharacterFootPelvisSpringHandoffReason.TargetCrossedOutput;
            bool velocityReset =
                (handoff != CharacterFootPelvisSpringHandoffReason.None || previousVelocity > 0f) &&
                Mathf.Abs(nextTargetDirection) > GeometryEpsilon &&
                previousVelocity * nextTargetDirection < 0f;
            float inputVelocity = velocityReset ? 0f : previousVelocity;
            float output = previousOutput;
            float velocity = inputVelocity;
            if (frame.DeltaSeconds > 0f)
            {
                float omega = settings.PelvisSpringFrequency * 2f * Mathf.PI;
                float x0 = previousOutput - target;
                float j0 = inputVelocity + omega * x0;
                float decay = Mathf.Exp(-omega * frame.DeltaSeconds);
                output = target + (x0 + j0 * frame.DeltaSeconds) * decay;
                velocity = (inputVelocity - omega * j0 * frame.DeltaSeconds) * decay;
            }
            float unconstrainedOutput = output;
            if (!float.IsFinite(output) || !float.IsFinite(velocity))
                throw new InvalidOperationException("Pelvis spring response is non-finite.");
            if (reach.Available)
                output = Mathf.Clamp(output, minimum, maximum);
            bool outputClamped = Mathf.Abs(output - unconstrainedOutput) > GeometryEpsilon;
            bool boundaryVelocityCleared = reach.Available &&
                (output <= minimum && velocity < 0f ||
                 output >= maximum && velocity > 0f);
            if (boundaryVelocityCleared)
                velocity = 0f;
            bool completed = releasing &&
                Mathf.Abs(output) <= GeometryEpsilon &&
                Mathf.Abs(velocity) <= GeometryEpsilon &&
                (!reach.Available || reach.Contains(0f, 0f));
            if (completed)
            {
                output = 0f;
                velocity = 0f;
                spring.Clear();
            }
            else
            {
                spring.HasValue = true;
                spring.SupportSide = supportSide;
                spring.SupportLandingEventIdentity = supportEventIdentity;
                spring.Slope = slope;
                spring.TargetAlongUp = target;
                spring.OutputAlongUp = output;
                spring.VelocityAlongUp = velocity;
            }
            bool hardTranslationRequired = reach.Available && !reach.Contains(0f, 0f);
            float visibleTolerance = reach.HasLandingRequests ? GeometryEpsilon : EndpointTolerance;
            float positionWeight = !completed &&
                (hardTranslationRequired || Mathf.Abs(output) > visibleTolerance)
                ? frame.FootPlacementWeight
                : 0f;
            return new CharacterFootPelvisSpringStep(
                true, completed, hadPreviousState, supportChanged, previousSlope,
                handoff, velocityReset, previousTarget, previousOutput, previousVelocity,
                previousOutput, inputVelocity, settings.PelvisSpringFrequency,
                unconstrainedOutput, targetClamped, outputClamped, boundaryVelocityCleared,
                target, output, velocity, positionWeight);
        }

        static bool TryResolvePostureInterval(
            Vector3 supportHip,
            Vector3 supportTargetAnkle,
            Vector3 up,
            float supportLegLength,
            float postureCompressionReserve,
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
                supportLegLength - Mathf.Max(EndpointTolerance, postureCompressionReserve),
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
                float.IsFinite(maximumAlongUp) && minimumAlongUp <= maximumAlongUp;
        }

        static bool IsAuthoritativeSwing(in AnimationFootMotionRuntimeSample step) =>
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
                0f,
                originalSole,
                originalAnkle,
                0f,
                0f);

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
