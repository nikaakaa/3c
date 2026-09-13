using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
    internal static class CameraAssetRevision
    {
        public static string Compute(UnityEngine.Object value)
        {
            if (!value)
                return string.Empty;
            string json = value.GetType().FullName + "|" + JsonUtility.ToJson(value);
            using SHA256 algorithm = SHA256.Create();
            byte[] hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(json));
            var result = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
                result.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
            return result.ToString();
        }
    }

[CreateAssetMenu(fileName = "CameraCurve", menuName = "3C/Character/Camera/Curve")]
    public sealed class CameraCurveAsset : ScriptableObject
    {
        public const string SchemaVersion = "character-camera-curve/v1";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_CurveId = string.Empty;
        [SerializeField] AnimationCurve m_Curve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(1f, 1f));
        [SerializeField] CameraTimeDomain m_TimeDomain = CameraTimeDomain.PresentationScaled;
        [SerializeField] float m_MinValue;
        [SerializeField] float m_MaxValue = 1f;
        [SerializeField] string m_Unit = "normalized";
        [SerializeField] WrapMode m_PreWrapMode = WrapMode.ClampForever;
        [SerializeField] WrapMode m_PostWrapMode = WrapMode.ClampForever;

        public string Schema => m_Schema ?? string.Empty;
        public string CurveId => m_CurveId ?? string.Empty;
        public AnimationCurve Curve => Copy(m_Curve);
        public CameraTimeDomain TimeDomain => m_TimeDomain;
        public float MinValue => m_MinValue;
        public float MaxValue => m_MaxValue;
        public string Unit => m_Unit ?? string.Empty;
        public WrapMode PreWrapMode => m_PreWrapMode;
        public WrapMode PostWrapMode => m_PostWrapMode;
        public string Revision => ComputeRevision();
        public string DependencyIdentity => $"{CurveId}@{Revision}";

        public void Configure(
            string curveId,
            AnimationCurve curve,
            CameraTimeDomain timeDomain,
            float minValue,
            float maxValue,
            string unit,
            WrapMode preWrapMode = WrapMode.ClampForever,
            WrapMode postWrapMode = WrapMode.ClampForever)
        {
            m_Schema = SchemaVersion;
            m_CurveId = RequireIdentity(curveId, nameof(curveId));
            m_Curve = Copy(curve);
            m_TimeDomain = timeDomain;
            m_MinValue = minValue;
            m_MaxValue = maxValue;
            m_Unit = RequireIdentity(unit, nameof(unit));
            m_PreWrapMode = preWrapMode;
            m_PostWrapMode = postWrapMode;
            RequireValid();
        }

        public CameraCurvePayload Compile()
        {
            RequireValid();
            Keyframe[] keys = m_Curve.keys;
            var payload = new CameraCurveKey[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                if (key.weightedMode != WeightedMode.None)
                    throw new InvalidOperationException($"Camera Curve '{name}' does not support weighted tangents.");
                payload[i] = new CameraCurveKey(key.time, key.value, key.inTangent, key.outTangent);
            }
            return new CameraCurvePayload(
                CurveId,
                Revision,
                payload,
                TimeDomain,
                MinValue,
                MaxValue,
                Unit,
                PreWrapMode,
                PostWrapMode);
        }

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(CurveId) || m_Curve == null || m_Curve.length == 0 ||
                !Enum.IsDefined(typeof(CameraTimeDomain), TimeDomain) ||
                !float.IsFinite(MinValue) || !float.IsFinite(MaxValue) || MinValue > MaxValue ||
                string.IsNullOrWhiteSpace(Unit) ||
                !Enum.IsDefined(typeof(WrapMode), PreWrapMode) ||
                !Enum.IsDefined(typeof(WrapMode), PostWrapMode))
            {
                throw new InvalidOperationException($"Camera Curve Asset '{name}' is incomplete.");
            }
            Keyframe[] keys = m_Curve.keys;
            float previousTime = float.NegativeInfinity;
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                if (key.weightedMode != WeightedMode.None || !float.IsFinite(key.time) ||
                    !float.IsFinite(key.value) || !float.IsFinite(key.inTangent) ||
                    !float.IsFinite(key.outTangent) || key.time <= previousTime)
                {
                    throw new InvalidOperationException($"Camera Curve Asset '{name}' key #{i} is invalid.");
                }
                previousTime = key.time;
            }
        }

        public void RegenerateIdentity() => m_CurveId = Guid.NewGuid().ToString("N");

        string ComputeRevision()
        {
            var value = new StringBuilder(SchemaVersion);
            value.Append('|').Append(CurveId);
            value.Append('|').Append((byte)TimeDomain);
            value.Append('|').Append(MinValue.ToString("R", CultureInfo.InvariantCulture));
            value.Append('|').Append(MaxValue.ToString("R", CultureInfo.InvariantCulture));
            value.Append('|').Append(Unit);
            value.Append('|').Append((int)PreWrapMode);
            value.Append('|').Append((int)PostWrapMode);
            if (m_Curve != null)
            {
                Keyframe[] keys = m_Curve.keys;
                for (int i = 0; i < keys.Length; i++)
                {
                    Keyframe key = keys[i];
                    value.Append('|').Append(key.time.ToString("R", CultureInfo.InvariantCulture));
                    value.Append('|').Append(key.value.ToString("R", CultureInfo.InvariantCulture));
                    value.Append('|').Append(key.inTangent.ToString("R", CultureInfo.InvariantCulture));
                    value.Append('|').Append(key.outTangent.ToString("R", CultureInfo.InvariantCulture));
                }
            }
            using SHA256 algorithm = SHA256.Create();
            byte[] hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(value.ToString()));
            var result = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
                result.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
            return result.ToString();
        }

        static AnimationCurve Copy(AnimationCurve source)
        {
            if (source == null)
                return null;
            return new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
        }

        static string RequireIdentity(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Camera identity is missing or contains surrounding whitespace.", parameterName);
            return value.Trim();
        }
    }
}
