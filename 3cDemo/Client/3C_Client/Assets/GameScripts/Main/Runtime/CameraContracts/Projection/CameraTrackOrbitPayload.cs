using System;
using UnityEngine;

namespace ThirdPersonCamera
{
    [Serializable]
    public sealed class CameraTrackOrbitPayload
    {
        [SerializeField] float m_Height;
        [SerializeField] float m_Radius;

        public CameraTrackOrbitPayload(float height, float radius)
        {
            m_Height = height;
            m_Radius = radius;
        }

        public float Height => m_Height;
        public float Radius => m_Radius;

        public void RequireValid(string source)
        {
            if (!float.IsFinite(Height) || !float.IsFinite(Radius) || Radius <= 0f)
                throw new InvalidOperationException($"{source} contains an invalid camera track orbit.");
        }
    }
}
