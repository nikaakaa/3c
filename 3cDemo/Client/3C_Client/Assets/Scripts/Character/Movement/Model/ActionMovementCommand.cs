using UnityEngine;

namespace ThirdPersonMovement
{
    public readonly struct ActionMovementCommand
    {
        public ActionMovementCommand(Vector3 worldDirection, float planarDistance, float deltaTime, bool rotateToDirection)
        {
            WorldDirection = NormalizePlanarOrZero(worldDirection);
            PlanarDistance = Mathf.Max(0f, planarDistance);
            DeltaTime = Mathf.Max(0f, deltaTime);
            RotateToDirection = rotateToDirection;
        }

        public Vector3 WorldDirection { get; }
        public float PlanarDistance { get; }
        public float DeltaTime { get; }
        public bool RotateToDirection { get; }
        public Vector3 PlanarDisplacement => WorldDirection * PlanarDistance;
        public bool HasMovement => WorldDirection.sqrMagnitude > 0.000001f && PlanarDistance > 0f;

        static Vector3 NormalizePlanarOrZero(Vector3 value)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            return sqrMagnitude > 0.000001f ? value / Mathf.Sqrt(sqrMagnitude) : Vector3.zero;
        }
    }
}
