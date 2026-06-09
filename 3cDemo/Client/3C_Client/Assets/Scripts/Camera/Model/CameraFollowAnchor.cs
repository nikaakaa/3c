using UnityEngine;

namespace ThirdPersonCamera
{
    public readonly struct CameraFollowAnchor
    {
        public CameraFollowAnchor(Vector3 position) { Position = position; }
        public Vector3 Position { get; }
    }
}
