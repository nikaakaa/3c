using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Motion
{
    public readonly struct ResolvedCharacterMotionFact
    {
        public ResolvedCharacterMotionFact(
            ulong inputSequence,
            ulong localLogicTick,
            Vector3 appliedDisplacement,
            float appliedYawDegrees,
            Vector3 position,
            Quaternion rotation,
            bool grounded,
            bool hasMotion,
            float horizontalSpeed)
        {
            InputSequence = inputSequence;
            LocalLogicTick = localLogicTick;
            AppliedDisplacement = appliedDisplacement;
            AppliedYawDegrees = appliedYawDegrees;
            Position = position;
            Rotation = rotation;
            Grounded = grounded;
            HasMotion = hasMotion;
            HorizontalSpeed = horizontalSpeed;
        }

        public ulong InputSequence { get; }
        public ulong LocalLogicTick { get; }
        public Vector3 AppliedDisplacement { get; }
        public float AppliedYawDegrees { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public bool Grounded { get; }
        public bool HasMotion { get; }
        public float HorizontalSpeed { get; }
        public bool IsValid => LocalLogicTick != 0;
    }
}
