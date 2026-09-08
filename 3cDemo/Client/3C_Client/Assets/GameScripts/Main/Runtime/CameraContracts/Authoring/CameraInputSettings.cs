using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public sealed class CameraInputSettings
    {
        [SerializeField] Vector2 m_Sensitivity = new Vector2(0.12f, 0.0025f);
        [SerializeField] Vector2 m_PitchLimit = new Vector2(-70f, 70f);
        [SerializeField] float m_DefaultResponseWeight = 1f;
        [SerializeField] float m_PitchResponseWeight = 1f;
        [SerializeField] float m_YawResponseWeight = 1f;

        public Vector2 Sensitivity => m_Sensitivity;
        public Vector2 PitchLimit => m_PitchLimit;
        public float DefaultResponseWeight => m_DefaultResponseWeight;
        public float PitchResponseWeight => m_PitchResponseWeight;
        public float YawResponseWeight => m_YawResponseWeight;

        public CameraInputSettings() { }

        internal CameraInputSettings(CameraInputSettings source)
        {
            m_Sensitivity = source.Sensitivity;
            m_PitchLimit = source.PitchLimit;
            m_DefaultResponseWeight = source.DefaultResponseWeight;
            m_PitchResponseWeight = source.PitchResponseWeight;
            m_YawResponseWeight = source.YawResponseWeight;
        }

        public void RequireValid(string source)
        {
            if (!float.IsFinite(Sensitivity.x) || !float.IsFinite(Sensitivity.y) ||
                !float.IsFinite(PitchLimit.x) || !float.IsFinite(PitchLimit.y) ||
                PitchLimit.x >= PitchLimit.y || !float.IsFinite(DefaultResponseWeight) ||
                !float.IsFinite(PitchResponseWeight) || !float.IsFinite(YawResponseWeight) ||
                DefaultResponseWeight < 0f || DefaultResponseWeight > 1f ||
                PitchResponseWeight < 0f || PitchResponseWeight > 1f ||
                YawResponseWeight < 0f || YawResponseWeight > 1f)
                throw new InvalidOperationException($"{source} contains invalid Camera input settings.");
        }
    }
}
