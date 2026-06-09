using UnityEngine;

namespace ThirdPersonCamera
{
    public struct YawPitchState
    {
        public float Yaw { get; private set; }
        public float Pitch { get; private set; }

        public void Apply(CameraLookIntent intent, Vector2 sensitivity, Vector2 pitchLimits)
        {
            Yaw = Mathf.Repeat(Yaw + intent.Delta.x * sensitivity.x, 360f);
            Pitch = Mathf.Clamp(Pitch - intent.Delta.y * sensitivity.y, pitchLimits.x, pitchLimits.y);
        }

        public void Reset(float yaw, float pitch, Vector2 pitchLimits)
        {
            Yaw = Mathf.Repeat(yaw, 360f);
            Pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);
        }
    }
}
