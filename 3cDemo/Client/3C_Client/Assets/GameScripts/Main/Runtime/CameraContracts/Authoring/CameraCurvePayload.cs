using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public sealed class CameraCurvePayload
    {
        [SerializeField] string m_CurveId = string.Empty;
        [SerializeField] string m_Revision = string.Empty;
        [SerializeField] CameraCurveKey[] m_Keys = Array.Empty<CameraCurveKey>();
        [SerializeField] CameraTimeDomain m_TimeDomain;
        [SerializeField] float m_MinValue;
        [SerializeField] float m_MaxValue;
        [SerializeField] string m_Unit = string.Empty;
        [SerializeField] WrapMode m_PreWrapMode;
        [SerializeField] WrapMode m_PostWrapMode;

        internal CameraCurvePayload(
            string curveId,
            string revision,
            CameraCurveKey[] keys,
            CameraTimeDomain timeDomain,
            float minValue,
            float maxValue,
            string unit,
            WrapMode preWrapMode,
            WrapMode postWrapMode)
        {
            m_CurveId = curveId ?? string.Empty;
            m_Revision = revision ?? string.Empty;
            m_Keys = keys ?? Array.Empty<CameraCurveKey>();
            m_TimeDomain = timeDomain;
            m_MinValue = minValue;
            m_MaxValue = maxValue;
            m_Unit = unit ?? string.Empty;
            m_PreWrapMode = preWrapMode;
            m_PostWrapMode = postWrapMode;
        }

        public string CurveId => m_CurveId ?? string.Empty;
        public string Revision => m_Revision ?? string.Empty;
        public IReadOnlyList<CameraCurveKey> Keys => m_Keys ?? Array.Empty<CameraCurveKey>();
        public CameraTimeDomain TimeDomain => m_TimeDomain;
        public float MinValue => m_MinValue;
        public float MaxValue => m_MaxValue;
        public string Unit => m_Unit ?? string.Empty;
        public WrapMode PreWrapMode => m_PreWrapMode;
        public WrapMode PostWrapMode => m_PostWrapMode;

        public float Evaluate(float time)
        {
            IReadOnlyList<CameraCurveKey> keys = Keys;
            if (keys.Count == 0)
                throw new InvalidOperationException($"Camera Curve '{CurveId}' has no keys.");
            if (keys.Count == 1)
                return keys[0].Value;
            int last = keys.Count - 1;
            time = time < keys[0].Time
                ? WrapTime(time, keys[0].Time, keys[last].Time, PreWrapMode)
                : time > keys[last].Time
                    ? WrapTime(time, keys[0].Time, keys[last].Time, PostWrapMode)
                    : time;
            if (time <= keys[0].Time)
                return keys[0].Value;
            if (time >= keys[last].Time)
                return keys[last].Value;
            for (int index = 1; index < keys.Count; index++)
            {
                CameraCurveKey right = keys[index];
                if (time > right.Time)
                    continue;
                CameraCurveKey left = keys[index - 1];
                float duration = right.Time - left.Time;
                float t = duration <= 0f ? 1f : Mathf.Clamp01((time - left.Time) / duration);
                float t2 = t * t;
                float t3 = t2 * t;
                float h00 = 2f * t3 - 3f * t2 + 1f;
                float h10 = t3 - 2f * t2 + t;
                float h01 = -2f * t3 + 3f * t2;
                float h11 = t3 - t2;
                return h00 * left.Value +
                       h10 * duration * left.OutTangent +
                       h01 * right.Value +
                       h11 * duration * right.InTangent;
            }
            return keys[last].Value;
        }

        static float WrapTime(float time, float first, float last, WrapMode mode)
        {
            float duration = last - first;
            if (duration <= 0f || mode == WrapMode.Once || mode == WrapMode.Default || mode == WrapMode.ClampForever)
                return time < first ? first : last;
            if (mode == WrapMode.Loop)
                return first + Mathf.Repeat(time - first, duration);
            if (mode == WrapMode.PingPong)
            {
                float cycle = Mathf.Repeat(time - first, duration * 2f);
                return cycle <= duration
                    ? first + cycle
                    : last - (cycle - duration);
            }
            return time < first ? first : last;
        }

        public void RequireValid(string source)
        {
            if (string.IsNullOrWhiteSpace(CurveId) || string.IsNullOrWhiteSpace(Revision) ||
                Keys.Count == 0 || !Enum.IsDefined(typeof(CameraTimeDomain), TimeDomain) ||
                !float.IsFinite(MinValue) || !float.IsFinite(MaxValue) || MinValue > MaxValue ||
                string.IsNullOrWhiteSpace(Unit) ||
                !Enum.IsDefined(typeof(WrapMode), PreWrapMode) ||
                !Enum.IsDefined(typeof(WrapMode), PostWrapMode))
            {
                throw new InvalidOperationException($"{source} contains an invalid Camera Curve payload.");
            }
            float previousTime = float.NegativeInfinity;
            for (int i = 0; i < Keys.Count; i++)
            {
                CameraCurveKey key = Keys[i];
                if (key == null || !float.IsFinite(key.Time) || !float.IsFinite(key.Value) ||
                    !float.IsFinite(key.InTangent) || !float.IsFinite(key.OutTangent) ||
                    key.Time <= previousTime)
                {
                    throw new InvalidOperationException($"{source} Curve key #{i} is invalid or not ordered.");
                }
                previousTime = key.Time;
            }
        }
    }
}
