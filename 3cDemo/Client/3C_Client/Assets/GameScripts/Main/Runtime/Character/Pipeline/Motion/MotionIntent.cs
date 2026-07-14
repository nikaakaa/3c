using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Motion
{
    public readonly struct MotionIntent
    {
        public MotionIntent(Vector3 displacement, Vector3 velocity, float yawDegrees = 0f)
        {
            Displacement = displacement;
            Velocity = velocity;
            YawDegrees = yawDegrees;
            HasMotion = true;
        }

        public Vector3 Displacement { get; }
        public Vector3 Velocity { get; }
        public float YawDegrees { get; }
        public bool HasMotion { get; }
    }
}
