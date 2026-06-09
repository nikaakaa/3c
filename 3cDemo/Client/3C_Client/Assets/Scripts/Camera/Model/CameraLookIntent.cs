using UnityEngine;

namespace ThirdPersonCamera
{
    public readonly struct CameraLookIntent
    {
        public CameraLookIntent(Vector2 delta) { Delta = delta; }
        public Vector2 Delta { get; }
    }
}
