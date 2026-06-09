using UnityEngine;

namespace ThirdPersonCamera
{
    public readonly struct CameraInfluenceRequest
    {
        public CameraInfluenceRequest(bool active, int priority, float weight, CameraAimIntent aimIntent)
        {
            Active = active;
            Priority = priority;
            Weight = Mathf.Clamp01(weight);
            AimIntent = aimIntent;
        }

        public bool Active { get; }
        public int Priority { get; }
        public float Weight { get; }
        public CameraAimIntent AimIntent { get; }

        public static CameraInfluenceRequest FreeDefault => new CameraInfluenceRequest(true, 0, 1f, CameraAimIntent.Free);
    }
}
