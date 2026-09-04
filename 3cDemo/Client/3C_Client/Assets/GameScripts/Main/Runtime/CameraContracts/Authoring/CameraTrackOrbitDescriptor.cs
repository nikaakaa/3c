using System;
using UnityEngine;

namespace ThirdPersonCamera
{
    [Serializable]
    public sealed class CameraTrackOrbitDescriptor
    {
        [SerializeField] float m_Height;
        [SerializeField, Min(0f)] float m_Radius = 3f;

        public float Height => m_Height;
        public float Radius => m_Radius;

        public CameraTrackOrbitDescriptor() { }

        public CameraTrackOrbitDescriptor(float height, float radius)
        {
            m_Height = height;
            m_Radius = radius;
        }

        public void RequireValid(string source)
        {
            if (!float.IsFinite(Height) || !float.IsFinite(Radius) || Radius <= 0f)
                throw new InvalidOperationException($"{source} contains an invalid camera track orbit.");
        }
    }
}
