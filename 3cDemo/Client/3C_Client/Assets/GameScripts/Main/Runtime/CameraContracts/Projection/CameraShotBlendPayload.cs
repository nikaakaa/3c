using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public sealed class CameraShotBlendPayload
    {
        [SerializeField] float m_Duration;
        [SerializeField] CameraCurvePayload m_Curve;
        [SerializeField] bool m_UseCoreSpace;
        [SerializeField] bool m_UseDelta;

        public CameraShotBlendPayload(CameraShotBlendSettings source)
        {
            m_Duration = source.Duration;
            m_Curve = source.Curve.Compile();
            m_UseCoreSpace = source.UseCoreSpace;
            m_UseDelta = source.UseDelta;
        }

        public CameraShotBlendPayload(float duration, CameraCurvePayload curve, bool useCoreSpace, bool useDelta)
        {
            m_Duration = duration;
            m_Curve = curve;
            m_UseCoreSpace = useCoreSpace;
            m_UseDelta = useDelta;
        }

        public float Duration => m_Duration;
        public CameraCurvePayload Curve => m_Curve;
        public bool UseCoreSpace => m_UseCoreSpace;
        public bool UseDelta => m_UseDelta;
    }
}
