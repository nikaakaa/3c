using UnityEngine;

namespace ThirdPersonCamera
{
    public static class TransformFollowAnchorSource
    {
        public static CameraFollowAnchor Read(Transform source, Vector3 fallbackPosition)
        {
            return new CameraFollowAnchor(source != null ? source.position : fallbackPosition);
        }
    }
}
