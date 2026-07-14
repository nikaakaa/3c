using System;

namespace ThirdPersonCharacter.Pipeline.Motion
{
    public readonly struct CharacterMotionVector3
    {
        public CharacterMotionVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public bool IsFinite => IsFiniteValue(X) && IsFiniteValue(Y) && IsFiniteValue(Z);
        public float SqrMagnitude => X * X + Y * Y + Z * Z;

        static bool IsFiniteValue(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    public readonly struct CharacterMotionRotation
    {
        public CharacterMotionRotation(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float W { get; }
        public bool IsFinite =>
            IsFiniteValue(X) && IsFiniteValue(Y) && IsFiniteValue(Z) && IsFiniteValue(W);
        public bool IsValid => IsFinite && X * X + Y * Y + Z * Z + W * W > 0.000001f;

        static bool IsFiniteValue(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    public readonly struct CharacterLogicPose
    {
        public CharacterLogicPose(CharacterMotionVector3 position, CharacterMotionRotation rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        public CharacterMotionVector3 Position { get; }
        public CharacterMotionRotation Rotation { get; }
        public bool IsValid => Position.IsFinite && Rotation.IsValid;
    }

    public readonly struct CharacterLogicBodyState
    {
        public CharacterLogicBodyState(
            CharacterLogicPose pose,
            CharacterMotionVector3 velocity,
            bool grounded)
        {
            Pose = pose;
            Velocity = velocity;
            Grounded = grounded;
        }

        public CharacterLogicPose Pose { get; }
        public CharacterMotionVector3 Position => Pose.Position;
        public CharacterMotionRotation Rotation => Pose.Rotation;
        public CharacterMotionVector3 Velocity { get; }
        public bool Grounded { get; }
        public bool IsValid => Pose.IsValid && Velocity.IsFinite;
    }

    public readonly struct CharacterMotionExecutionInput
    {
        public CharacterMotionExecutionInput(
            ulong localLogicTick,
            float deltaSeconds,
            CharacterLogicBodyState currentState,
            CharacterMotionVector3 requestedDisplacement,
            CharacterMotionVector3 requestedVelocity,
            float requestedYawDegrees,
            bool hasMotion)
        {
            LocalLogicTick = localLogicTick;
            DeltaSeconds = deltaSeconds;
            CurrentState = currentState;
            RequestedDisplacement = requestedDisplacement;
            RequestedVelocity = requestedVelocity;
            RequestedYawDegrees = requestedYawDegrees;
            HasMotion = hasMotion;
        }

        public ulong LocalLogicTick { get; }
        public float DeltaSeconds { get; }
        public CharacterLogicBodyState CurrentState { get; }
        public CharacterMotionVector3 RequestedDisplacement { get; }
        public CharacterMotionVector3 RequestedVelocity { get; }
        public float RequestedYawDegrees { get; }
        public bool HasMotion { get; }
        public bool IsValid =>
            LocalLogicTick != 0 &&
            DeltaSeconds > 0f &&
            !float.IsNaN(DeltaSeconds) &&
            !float.IsInfinity(DeltaSeconds) &&
            CurrentState.IsValid &&
            RequestedDisplacement.IsFinite &&
            RequestedVelocity.IsFinite &&
            !float.IsNaN(RequestedYawDegrees) &&
            !float.IsInfinity(RequestedYawDegrees);
    }

    [Flags]
    public enum CharacterMotionCollisionSummary
    {
        None = 0,
        Sides = 1,
        Above = 2,
        Below = 4
    }

    public readonly struct CharacterMotionExecutionResult
    {
        public CharacterMotionExecutionResult(
            CharacterMotionExecutionInput input,
            CharacterLogicBodyState finalState,
            CharacterMotionVector3 appliedDisplacement,
            float appliedYawDegrees,
            CharacterMotionCollisionSummary collisionSummary)
        {
            Input = input;
            FinalState = finalState;
            AppliedDisplacement = appliedDisplacement;
            AppliedYawDegrees = appliedYawDegrees;
            CollisionSummary = collisionSummary;
        }

        public CharacterMotionExecutionInput Input { get; }
        public CharacterLogicBodyState FinalState { get; }
        public CharacterMotionVector3 AppliedDisplacement { get; }
        public float AppliedYawDegrees { get; }
        public CharacterMotionCollisionSummary CollisionSummary { get; }
        public bool IsValid =>
            Input.IsValid &&
            FinalState.IsValid &&
            AppliedDisplacement.IsFinite &&
            !float.IsNaN(AppliedYawDegrees) &&
            !float.IsInfinity(AppliedYawDegrees);
    }

    public interface ICharacterMotionExecutor
    {
        string ImplementationId { get; }

        bool TryExecute(
            CharacterMotionExecutionInput input,
            out CharacterMotionExecutionResult result,
            out string error);
    }

    public interface ICharacterLogicPosePort
    {
        string ImplementationId { get; }

        bool TryReadState(out CharacterLogicBodyState state, out string error);

        bool TryApplyPose(
            CharacterLogicPose pose,
            out CharacterLogicBodyState state,
            out string error);
    }
}
