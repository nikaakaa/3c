using UnityEngine;

namespace ThirdPersonCamera
{
    public readonly struct CameraAimIntent
    {
        public static CameraAimIntent Free => new CameraAimIntent(Vector3.zero);

        public CameraAimIntent(Vector3 offset)
        {
            Offset = offset;
        }

        public Vector3 Offset { get; }
    }
}
