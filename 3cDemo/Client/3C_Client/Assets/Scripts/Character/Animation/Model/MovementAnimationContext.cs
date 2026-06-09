using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAnimation
{
    public readonly struct MovementAnimationContext
    {
        public MovementAnimationContext(
            BasicMovementPhase phase,
            bool hasMoveIntent,
            float inputStrength,
            Vector3 worldDirection,
            float planarSpeed)
        {
            Phase = phase;
            HasMoveIntent = hasMoveIntent;
            InputStrength = Mathf.Clamp01(inputStrength);
            WorldDirection = worldDirection.sqrMagnitude > 0.000001f ? worldDirection.normalized : Vector3.zero;
            PlanarSpeed = Mathf.Max(0f, planarSpeed);
        }

        public BasicMovementPhase Phase { get; }
        public bool HasMoveIntent { get; }
        public float InputStrength { get; }
        public Vector3 WorldDirection { get; }
        public float PlanarSpeed { get; }
    }
}
