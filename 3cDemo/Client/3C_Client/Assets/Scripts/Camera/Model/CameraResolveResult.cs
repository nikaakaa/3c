using UnityEngine;

namespace ThirdPersonCamera
{
    public readonly struct CameraResolveResult
    {
        public CameraResolveResult(
            Vector3 anchorPosition,
            Vector3 aimPoint,
            Vector3 cameraPlanarForward,
            Vector3 cameraPlanarRight,
            Vector3 lookDirection)
        {
            AnchorPosition = anchorPosition;
            AimPoint = aimPoint;
            CameraPlanarForward = cameraPlanarForward;
            CameraPlanarRight = cameraPlanarRight;
            LookDirection = lookDirection;
        }

        public Vector3 AnchorPosition { get; }
        public Vector3 AimPoint { get; }
        public Vector3 CameraPlanarForward { get; }
        public Vector3 CameraPlanarRight { get; }
        public Vector3 LookDirection { get; }
    }
}
