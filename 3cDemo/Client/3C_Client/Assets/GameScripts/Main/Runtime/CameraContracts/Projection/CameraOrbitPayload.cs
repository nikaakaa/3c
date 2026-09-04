using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public sealed class CameraOrbitPayload
    {
        [SerializeField] float m_Height;
        [SerializeField] float m_Radius;
        [SerializeField] float m_ScreenY;

        public CameraOrbitPayload(float height, float radius, float screenY)
        {
            m_Height = height;
            m_Radius = radius;
            m_ScreenY = screenY;
        }

        public float Height => m_Height;
        public float Radius => m_Radius;
        public float ScreenY => m_ScreenY;

        public void RequireValid(string source)
        {
            if (!float.IsFinite(Height) || !float.IsFinite(Radius) || Radius <= 0f ||
                !float.IsFinite(ScreenY) || ScreenY < 0f || ScreenY > 1f)
                throw new InvalidOperationException($"{source} contains an invalid Camera orbit.");
        }
    }
}
