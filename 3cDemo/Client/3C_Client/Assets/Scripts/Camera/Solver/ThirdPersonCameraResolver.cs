using UnityEngine;

namespace ThirdPersonCamera
{
    public static class ThirdPersonCameraResolver
    {
        public static CameraResolveResult Resolve(
            Vector3 anchorPosition,
            Quaternion targetRotation,
            Vector3 planarForward,
            Vector3 planarRight,
            CameraAimIntent aimIntent)
        {
            Vector3 lookDirection = targetRotation * Vector3.forward;
            Vector3 aimPoint = anchorPosition + aimIntent.Offset;

            return new CameraResolveResult(
                anchorPosition,
                aimPoint,
                planarForward,
                planarRight,
                lookDirection);
        }
    }
}
