using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public sealed class CameraOrbitDescriptor
    {
        [SerializeField] float m_Height;
        [SerializeField, Min(0f)] float m_Radius = 3f;

        public float Height => m_Height;
        public float Radius => m_Radius;

        public CameraOrbitDescriptor() { }

        public CameraOrbitDescriptor(float height, float radius)
        {
            m_Height = height;
            m_Radius = radius;
        }

        public void RequireValid(string source)
        {
            if (!float.IsFinite(Height) || !float.IsFinite(Radius) || Radius <= 0f)
                throw new InvalidOperationException($"{source} contains an invalid Camera orbit.");
        }
    }
}
