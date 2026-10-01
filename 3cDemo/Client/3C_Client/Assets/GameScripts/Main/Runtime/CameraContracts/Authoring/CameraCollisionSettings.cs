using System;
using UnityEngine;

namespace ThirdPersonCamera
{
    [Serializable]
    public sealed class CameraCollisionSettings
    {
        [SerializeField] bool m_Enabled = true;
        [SerializeField] LayerMask m_LayerMask = Physics.DefaultRaycastLayers;
        [SerializeField, Min(0f)] float m_CameraRadius = 0.02f;
        [SerializeField, Min(0f)] float m_MinimumDistance = 0.1f;
        [SerializeField, Min(0f)] float m_DistanceLimit;
        [SerializeField, Min(0f)] float m_Damping = 1.5f;
        [SerializeField] CameraTimeDomain m_TimeDomain = CameraTimeDomain.PresentationScaled;
        [SerializeField] CameraCollisionTriggerMode m_TriggerMode = CameraCollisionTriggerMode.Ignore;

        public bool Enabled => m_Enabled;
        public LayerMask LayerMask => m_LayerMask;
        public float CameraRadius => m_CameraRadius;
        public float MinimumDistance => m_MinimumDistance;
        public float DistanceLimit => m_DistanceLimit;
        public float Damping => m_Damping;
        public CameraTimeDomain TimeDomain => m_TimeDomain;
        public CameraCollisionTriggerMode TriggerMode => m_TriggerMode;

        public CameraCollisionSettings() { }

        internal CameraCollisionSettings(CameraCollisionSettings source)
        {
            m_Enabled = source.Enabled;
            m_LayerMask = source.LayerMask;
            m_CameraRadius = source.CameraRadius;
            m_MinimumDistance = source.MinimumDistance;
            m_DistanceLimit = source.DistanceLimit;
            m_Damping = source.Damping;
            m_TimeDomain = source.TimeDomain;
            m_TriggerMode = source.TriggerMode;
        }

        public void Configure(bool enabled, float cameraRadius, float minimumDistance, float distanceLimit, float damping)
        {
            m_Enabled = enabled;
            m_CameraRadius = cameraRadius;
            m_MinimumDistance = minimumDistance;
            m_DistanceLimit = distanceLimit;
            m_Damping = damping;
        }

        public void RequireValid(string source)
        {
            if (!float.IsFinite(CameraRadius) || CameraRadius < 0f ||
                !float.IsFinite(MinimumDistance) || MinimumDistance <= 0f ||
                !float.IsFinite(DistanceLimit) || DistanceLimit < 0f ||
                !float.IsFinite(Damping) || Damping < 0f ||
                !Enum.IsDefined(typeof(CameraTimeDomain), TimeDomain) ||
                !Enum.IsDefined(typeof(CameraCollisionTriggerMode), TriggerMode))
                throw new InvalidOperationException($"{source} contains invalid Camera collision settings.");
        }
    }
}
