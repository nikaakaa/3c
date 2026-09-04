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
    }
}
