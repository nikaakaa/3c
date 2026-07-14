using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Motion
{
    public enum MotionCorrectionApplicationExtent
    {
        None,
        Partial,
        Full
    }

    public readonly struct MotionCorrectionApplicationResult
    {
        public MotionCorrectionApplicationResult(
            MotionCorrectionApplicationExtent extent,
            ulong inputSequence,
            ulong sourceTick,
            Vector3 beforePosition,
            Quaternion beforeRotation,
            Vector3 targetPosition,
            Quaternion targetRotation,
            Vector3 appliedDelta,
            float appliedYawDegrees,
            bool applied)
        {
            Extent = extent;
            InputSequence = inputSequence;
            SourceTick = sourceTick;
            BeforePosition = beforePosition;
            BeforeRotation = beforeRotation;
            TargetPosition = targetPosition;
            TargetRotation = targetRotation;
            AppliedDelta = appliedDelta;
            AppliedYawDegrees = appliedYawDegrees;
            Applied = applied;
        }

        public MotionCorrectionApplicationExtent Extent { get; }
        public ulong InputSequence { get; }
        public ulong SourceTick { get; }
        public Vector3 BeforePosition { get; }
        public Quaternion BeforeRotation { get; }
        public Vector3 TargetPosition { get; }
        public Quaternion TargetRotation { get; }
        public Vector3 AppliedDelta { get; }
        public float AppliedYawDegrees { get; }
        public bool Applied { get; }
    }

    public readonly struct MotionResult
    {
        public MotionResult(
            Vector3 requestedDisplacement,
            Vector3 appliedDisplacement,
            Vector3 position,
            Quaternion rotation,
            bool grounded,
            bool hasMotion,
            float requestedYawDegrees = 0f,
            float appliedYawDegrees = 0f)
        {
            RequestedDisplacement = requestedDisplacement;
            AppliedDisplacement = appliedDisplacement;
            Position = position;
            Rotation = rotation;
            Grounded = grounded;
            HasMotion = hasMotion;
            RequestedYawDegrees = requestedYawDegrees;
            AppliedYawDegrees = appliedYawDegrees;
        }

        public Vector3 RequestedDisplacement { get; }
        public Vector3 AppliedDisplacement { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public bool Grounded { get; }
        public bool HasMotion { get; }
        public float RequestedYawDegrees { get; }
        public float AppliedYawDegrees { get; }
    }
}
