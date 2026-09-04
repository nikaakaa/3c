using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public sealed class CameraCurveKey
    {
        [SerializeField] float m_Time;
        [SerializeField] float m_Value;
        [SerializeField] float m_InTangent;
        [SerializeField] float m_OutTangent;

        public CameraCurveKey() { }

        public CameraCurveKey(float time, float value, float inTangent, float outTangent)
        {
            m_Time = time;
            m_Value = value;
            m_InTangent = inTangent;
            m_OutTangent = outTangent;
        }

        public float Time => m_Time;
        public float Value => m_Value;
        public float InTangent => m_InTangent;
        public float OutTangent => m_OutTangent;
    }
}
