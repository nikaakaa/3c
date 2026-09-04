using System;
using System.Collections.Generic;
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
        [SerializeField] float m_ScreenY = 0.5f;

        public float Height => m_Height;
        public float Radius => m_Radius;
        public float ScreenY => m_ScreenY;

        public CameraOrbitDescriptor() { }

        public CameraOrbitDescriptor(float height, float radius, float screenY)
        {
            m_Height = height;
            m_Radius = radius;
            m_ScreenY = screenY;
        }

        public void RequireValid(string source)
        {
            if (!float.IsFinite(Height) || !float.IsFinite(Radius) || Radius <= 0f ||
                !float.IsFinite(ScreenY) || ScreenY < 0f || ScreenY > 1f)
                throw new InvalidOperationException($"{source} contains an invalid Camera orbit.");
        }
    }
}
