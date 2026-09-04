using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public sealed class CameraShotBlendSettings
    {
        [SerializeField] float m_Duration;
        [SerializeField] CameraCurveAsset m_Curve;
        [SerializeField] bool m_UseCoreSpace;
        [SerializeField] bool m_UseDelta;

        public float Duration => m_Duration;
        public CameraCurveAsset Curve => m_Curve;
        public bool UseCoreSpace => m_UseCoreSpace;
        public bool UseDelta => m_UseDelta;

        public CameraShotBlendSettings() { }

        internal CameraShotBlendSettings(float duration, CameraCurveAsset curve, bool useCoreSpace, bool useDelta)
        {
            m_Duration = duration;
            m_Curve = curve;
            m_UseCoreSpace = useCoreSpace;
            m_UseDelta = useDelta;
        }

        public void RequireValid(string source)
        {
            if (!float.IsFinite(Duration) || Duration < 0f || !Curve)
                throw new InvalidOperationException($"{source} is incomplete.");
            Curve.RequireValid();
        }
    }
}
