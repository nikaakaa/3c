using UnityEngine;

namespace ThirdPersonMovement
{
    public readonly struct MovementCommand
    {
        public MovementCommand(Vector3 worldDirection, float planarSpeed, float rotationSpeed, float deltaTime, BasicMovementPhase phase)
        {
            WorldDirection = worldDirection.sqrMagnitude > 0.000001f ? worldDirection.normalized : Vector3.zero;
            DesiredFacing = WorldDirection;
            PlanarSpeed = Mathf.Max(0f, planarSpeed);
            RotationSpeed = Mathf.Max(0f, rotationSpeed);
            DeltaTime = Mathf.Max(0f, deltaTime);
            Phase = phase;
        }

        public Vector3 WorldDirection { get; }
        public Vector3 DesiredFacing { get; }
        public float PlanarSpeed { get; }
        public float RotationSpeed { get; }
        public float DeltaTime { get; }
        public BasicMovementPhase Phase { get; }
        public bool HasMovement => WorldDirection.sqrMagnitude > 0.000001f && PlanarSpeed > 0f;
    }
}
