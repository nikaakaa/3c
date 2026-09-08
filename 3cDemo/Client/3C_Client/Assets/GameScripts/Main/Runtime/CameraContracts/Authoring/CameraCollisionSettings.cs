using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public sealed class CameraCollisionSettings
    {
        [SerializeField] bool m_Enabled = true;
        [SerializeField] LayerMask m_LayerMask = Physics.DefaultRaycastLayers;
        [SerializeField, Min(0f)] float m_Radius = 0.2f;
        [SerializeField, Min(0f)] float m_NearClipPlane = 0.05f;
        [SerializeField, Min(0f)] float m_SmoothTime = 0.08f;
        [SerializeField] CameraTimeDomain m_TimeDomain = CameraTimeDomain.PresentationScaled;

        public bool Enabled => m_Enabled;
        public LayerMask LayerMask => m_LayerMask;
        public float Radius => m_Radius;
        public float NearClipPlane => m_NearClipPlane;
        public float SmoothTime => m_SmoothTime;
        public CameraTimeDomain TimeDomain => m_TimeDomain;

        public CameraCollisionSettings() { }

        internal CameraCollisionSettings(CameraCollisionSettings source)
        {
            m_Enabled = source.Enabled;
            m_LayerMask = source.LayerMask;
            m_Radius = source.Radius;
            m_NearClipPlane = source.NearClipPlane;
            m_SmoothTime = source.SmoothTime;
            m_TimeDomain = source.TimeDomain;
        }

        public void RequireValid(string source)
        {
            if (!float.IsFinite(Radius) || Radius < 0f || !float.IsFinite(NearClipPlane) ||
                NearClipPlane < 0f || !float.IsFinite(SmoothTime) || SmoothTime < 0f ||
                !Enum.IsDefined(typeof(CameraTimeDomain), TimeDomain))
                throw new InvalidOperationException($"{source} contains invalid Camera collision settings.");
        }
    }
}
