using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [CreateAssetMenu(fileName = "CharacterAnimationBlendCurve", menuName = "3C/Character/Animation Blend Curve")]
    public sealed class CharacterAnimationBlendCurveAsset : ScriptableObject
    {
        public const string SchemaVersion = "character-animation-blend-curve/v1";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_CurveId = Guid.NewGuid().ToString("N");
        [SerializeField] AnimationCurve m_Curve = new AnimationCurve(
            new Keyframe(0f, 0f, 1f, 1f),
            new Keyframe(1f, 1f, 1f, 1f));

        public string Schema => m_Schema ?? string.Empty;
        public string CurveId => m_CurveId ?? string.Empty;
        public string Revision => ComputeRevision(ToCanonicalCurve(m_Curve));
        public string DependencyIdentity => $"{CurveId}@{Revision}";
        public AnimationCurve Curve => Copy(m_Curve);

        public void Configure(string curveId, AnimationCurve curve)
        {
            m_Schema = SchemaVersion;
            m_CurveId = PoseNodeId.Require(curveId, nameof(curveId));
            SetCurve(curve);
        }

        public void SetCurve(AnimationCurve curve)
        {
            CharacterAnimationBlendCurve canonical = ToCanonicalCurve(curve);
            canonical.RequireValid();
            m_Curve = Copy(curve);
            m_Curve.preWrapMode = WrapMode.Clamp;
            m_Curve.postWrapMode = WrapMode.Clamp;
        }

        public void RegenerateIdentity() => m_CurveId = Guid.NewGuid().ToString("N");

        public AnimationBlendCurvePayload Compile()
        {
            RequireValid();
            return ToCanonicalCurve(m_Curve).Compile();
        }

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(CurveId) || m_Curve == null)
            {
                throw new InvalidOperationException($"Animation Blend Curve Asset '{name}' is incomplete.");
            }
            ToCanonicalCurve(m_Curve).RequireValid();
        }

        public static CharacterAnimationBlendCurve ToCanonicalCurve(AnimationCurve curve)
        {
            if (curve == null || curve.length < 2)
                throw new InvalidOperationException("Animation Blend Curve requires at least two keys.");
            Keyframe[] source = curve.keys;
            var keys = new CharacterAnimationBlendCurveKey[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                Keyframe key = source[i];
                if (key.weightedMode != WeightedMode.None)
                    throw new InvalidOperationException($"Animation Blend Curve key #{i} uses weighted tangents.");
                keys[i] = new CharacterAnimationBlendCurveKey(
                    key.time,
                    key.value,
                    key.inTangent,
                    key.outTangent);
            }
            return new CharacterAnimationBlendCurve(keys);
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

        static string ComputeRevision(CharacterAnimationBlendCurve curve)
        {
            var value = new StringBuilder(SchemaVersion);
            for (int i = 0; i < curve.Keys.Count; i++)
            {
                CharacterAnimationBlendCurveKey key = curve.Keys[i];
                value.Append('|').Append(key.Time.ToString("R", CultureInfo.InvariantCulture));
                value.Append('|').Append(key.Value.ToString("R", CultureInfo.InvariantCulture));
                value.Append('|').Append(key.InTangent.ToString("R", CultureInfo.InvariantCulture));
                value.Append('|').Append(key.OutTangent.ToString("R", CultureInfo.InvariantCulture));
            }
            using SHA256 algorithm = SHA256.Create();
            byte[] hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(value.ToString()));
            var result = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
                result.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
            return result.ToString();
        }
    }
}
