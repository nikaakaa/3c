using UnityEngine;

namespace ThirdPersonMovement
{
    public readonly struct LocomotionSpatialFacts
    {
        const float DirectionSqrEpsilon = 0.000001f;

        public LocomotionSpatialFacts(
            Vector3 worldMoveDirection,
            Vector3 facingForward,
            Vector3 cameraPlanarForward,
            Vector3 cameraPlanarRight)
        {
            WorldMoveDirection = NormalizePlanarOrZero(worldMoveDirection);
            FacingForward = NormalizePlanarOrZero(facingForward);
            CameraPlanarForward = NormalizePlanarOrZero(cameraPlanarForward);
            CameraPlanarRight = NormalizePlanarOrZero(cameraPlanarRight);
        }

        public Vector3 WorldMoveDirection { get; }
        public Vector3 FacingForward { get; }
        public Vector3 CameraPlanarForward { get; }
        public Vector3 CameraPlanarRight { get; }
        public bool HasWorldMoveDirection => WorldMoveDirection.sqrMagnitude > DirectionSqrEpsilon;
        public bool HasFacingForward => FacingForward.sqrMagnitude > DirectionSqrEpsilon;

        public static LocomotionSpatialFacts Empty => default;

        static Vector3 NormalizePlanarOrZero(Vector3 value)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            return sqrMagnitude > DirectionSqrEpsilon ? value / Mathf.Sqrt(sqrMagnitude) : Vector3.zero;
        }
    }
}
