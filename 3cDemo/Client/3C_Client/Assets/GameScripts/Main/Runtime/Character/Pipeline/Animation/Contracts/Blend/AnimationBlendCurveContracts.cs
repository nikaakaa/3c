using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum CharacterAnimationBlendMode : byte
    {
        Linear = 1,
        EaseIn = 2,
        EaseOut = 3,
        EaseInOut = 4,
        Custom = 5
    }

    [Serializable]
    public sealed class CharacterAnimationBlendCurveKey
    {
        [SerializeField] float m_Time;
        [SerializeField] float m_Value;
        [SerializeField] float m_InTangent;
        [SerializeField] float m_OutTangent;

        public float Time => m_Time;
        public float Value => m_Value;
        public float InTangent => m_InTangent;
        public float OutTangent => m_OutTangent;

        public CharacterAnimationBlendCurveKey() { }

        public CharacterAnimationBlendCurveKey(float time, float value, float inTangent, float outTangent)
        {
            if (!float.IsFinite(time) || !float.IsFinite(value) ||
                !float.IsFinite(inTangent) || !float.IsFinite(outTangent))
            {
                throw new ArgumentException("Animation Blend Curve key is non-finite.");
            }
            m_Time = time;
            m_Value = value;
            m_InTangent = inTangent;
            m_OutTangent = outTangent;
        }
    }

    [Serializable]
    public sealed class CharacterAnimationBlendCurve
    {
        [SerializeField] CharacterAnimationBlendCurveKey[] m_Keys =
        {
            new CharacterAnimationBlendCurveKey(0f, 0f, 0f, 0f),
            new CharacterAnimationBlendCurveKey(1f, 1f, 0f, 0f)
        };

        public IReadOnlyList<CharacterAnimationBlendCurveKey> Keys => m_Keys ?? Array.Empty<CharacterAnimationBlendCurveKey>();

        public CharacterAnimationBlendCurve() { }

        public CharacterAnimationBlendCurve(CharacterAnimationBlendCurveKey[] keys)
        {
            m_Keys = keys ?? throw new ArgumentNullException(nameof(keys));
            Compile();
        }

        public AnimationBlendCurvePayload Compile()
        {
            RequireValid();
            var segments = new AnimationBlendCurveSegment[Keys.Count - 1];
            for (int i = 0; i < segments.Length; i++)
            {
                CharacterAnimationBlendCurveKey from = Keys[i];
                CharacterAnimationBlendCurveKey to = Keys[i + 1];
                float duration = to.Time - from.Time;
                float fromTangent = from.OutTangent * duration;
                float toTangent = to.InTangent * duration;
                segments[i] = new AnimationBlendCurveSegment(
                    from.Time,
                    to.Time,
                    2f * from.Value - 2f * to.Value + fromTangent + toTangent,
                    -3f * from.Value + 3f * to.Value - 2f * fromTangent - toTangent,
                    fromTangent,
                    from.Value);
            }
            return new AnimationBlendCurvePayload(segments);
        }

        public void RequireValid()
        {
            if (Keys.Count < 2)
                throw new InvalidOperationException("Animation Blend Curve requires at least two keys.");
            CharacterAnimationBlendCurveKey first = Keys[0];
            CharacterAnimationBlendCurveKey last = Keys[Keys.Count - 1];
            if (first == null || last == null || first.Time != 0f || first.Value != 0f ||
                last.Time != 1f || last.Value != 1f)
            {
                throw new InvalidOperationException("Animation Blend Curve must begin at 0/0 and end at 1/1.");
            }

            for (int i = 0; i < Keys.Count; i++)
            {
                CharacterAnimationBlendCurveKey key = Keys[i];
                if (key == null || !float.IsFinite(key.Time) || !float.IsFinite(key.Value) ||
                    !float.IsFinite(key.InTangent) || !float.IsFinite(key.OutTangent) ||
                    key.Time < 0f || key.Time > 1f || key.Value < 0f || key.Value > 1f ||
                    i > 0 && (key.Time <= Keys[i - 1].Time || key.Value < Keys[i - 1].Value))
                {
                    throw new InvalidOperationException($"Animation Blend Curve key #{i} is invalid.");
                }
                if (i + 1 < Keys.Count)
                    RequireMonotoneSegment(key, Keys[i + 1], i);
            }
        }

        static void RequireMonotoneSegment(
            CharacterAnimationBlendCurveKey from,
            CharacterAnimationBlendCurveKey to,
            int segmentIndex)
        {
            float slope = (to.Value - from.Value) / (to.Time - from.Time);
            if (slope == 0f)
            {
                if (from.OutTangent != 0f || to.InTangent != 0f)
                    throw new InvalidOperationException($"Animation Blend Curve flat segment #{segmentIndex} requires zero tangents.");
                return;
            }

            float alpha = from.OutTangent / slope;
            float beta = to.InTangent / slope;
            if (alpha < 0f || beta < 0f || alpha * alpha + beta * beta > 9f)
                throw new InvalidOperationException($"Animation Blend Curve segment #{segmentIndex} tangents are not monotone.");
        }
    }

    public static class CharacterAnimationBlendCurveCompiler
    {
        static readonly CharacterAnimationBlendCurve s_Linear = Curve(1f, 1f);
        static readonly CharacterAnimationBlendCurve s_EaseIn = Curve(0f, 2f);
        static readonly CharacterAnimationBlendCurve s_EaseOut = Curve(2f, 0f);
        static readonly CharacterAnimationBlendCurve s_EaseInOut = Curve(0f, 0f);

        public static AnimationBlendCurvePayload Compile(
            CharacterAnimationBlendMode mode,
            CharacterAnimationBlendCurveAsset customCurve)
        {
            RequireConfiguration(mode, customCurve);
            return mode switch
            {
                CharacterAnimationBlendMode.Linear => s_Linear.Compile(),
                CharacterAnimationBlendMode.EaseIn => s_EaseIn.Compile(),
                CharacterAnimationBlendMode.EaseOut => s_EaseOut.Compile(),
                CharacterAnimationBlendMode.EaseInOut => s_EaseInOut.Compile(),
                CharacterAnimationBlendMode.Custom => customCurve.Compile(),
                _ => throw new InvalidOperationException($"Animation Blend Mode '{mode}' is invalid.")
            };
        }

        public static void RequireConfiguration(
            CharacterAnimationBlendMode mode,
            CharacterAnimationBlendCurveAsset customCurve)
        {
            if (!Enum.IsDefined(typeof(CharacterAnimationBlendMode), mode))
                throw new InvalidOperationException($"Animation Blend Mode '{mode}' is invalid.");
            if (mode == CharacterAnimationBlendMode.Custom)
            {
                if (!customCurve)
                    throw new InvalidOperationException("Custom Animation Blend Mode requires a Curve Asset.");
                customCurve.RequireValid();
                return;
            }
            if (customCurve)
                throw new InvalidOperationException($"Animation Blend Mode '{mode}' cannot retain a Custom Curve Asset.");
        }

        static CharacterAnimationBlendCurve Curve(float startTangent, float endTangent) =>
            new CharacterAnimationBlendCurve(new[]
            {
                new CharacterAnimationBlendCurveKey(0f, 0f, startTangent, startTangent),
                new CharacterAnimationBlendCurveKey(1f, 1f, endTangent, endTangent)
            });
    }

    [Serializable]
    public struct AnimationBlendCurveSegment
    {
        [SerializeField] float m_StartTime;
        [SerializeField] float m_EndTime;
        [SerializeField] float m_A;
        [SerializeField] float m_B;
        [SerializeField] float m_C;
        [SerializeField] float m_D;

        public AnimationBlendCurveSegment(float startTime, float endTime, float a, float b, float c, float d)
        {
            m_StartTime = startTime;
            m_EndTime = endTime;
            m_A = a;
            m_B = b;
            m_C = c;
            m_D = d;
        }

        public float StartTime => m_StartTime;
        public float EndTime => m_EndTime;
        public float A => m_A;
        public float B => m_B;
        public float C => m_C;
        public float D => m_D;
    }

    [Serializable]
    public sealed class AnimationBlendCurvePayload
    {
        [SerializeField] AnimationBlendCurveSegment[] m_Segments = Array.Empty<AnimationBlendCurveSegment>();

        public IReadOnlyList<AnimationBlendCurveSegment> Segments => m_Segments ?? Array.Empty<AnimationBlendCurveSegment>();

        internal AnimationBlendCurvePayload(AnimationBlendCurveSegment[] segments)
        {
            m_Segments = segments ?? throw new ArgumentNullException(nameof(segments));
            RequireValid();
        }

        public void RequireValid()
        {
            if (Segments.Count == 0 || Segments[0].StartTime != 0f || Segments[Segments.Count - 1].EndTime != 1f)
                throw new InvalidOperationException("Compiled Animation Blend Curve is incomplete.");
            for (int i = 0; i < Segments.Count; i++)
            {
                AnimationBlendCurveSegment segment = Segments[i];
                if (!float.IsFinite(segment.StartTime) || !float.IsFinite(segment.EndTime) ||
                    !float.IsFinite(segment.A) || !float.IsFinite(segment.B) ||
                    !float.IsFinite(segment.C) || !float.IsFinite(segment.D) ||
                    segment.EndTime <= segment.StartTime ||
                    i > 0 && segment.StartTime != Segments[i - 1].EndTime)
                {
                    throw new InvalidOperationException($"Compiled Animation Blend Curve segment #{i} is invalid.");
                }
            }
        }
    }

    public static class AnimationBlendCurveEvaluator
    {
        public static float Evaluate(AnimationBlendCurvePayload curve, float normalizedTime)
        {
            ResolveSegment(curve, normalizedTime, out AnimationBlendCurveSegment segment, out float u);
            return Mathf.Clamp01(((segment.A * u + segment.B) * u + segment.C) * u + segment.D);
        }

        public static float EvaluateDerivative(AnimationBlendCurvePayload curve, float normalizedTime)
        {
            ResolveSegment(curve, normalizedTime, out AnimationBlendCurveSegment segment, out float u);
            return ((3f * segment.A * u + 2f * segment.B) * u + segment.C) /
                   (segment.EndTime - segment.StartTime);
        }

        static void ResolveSegment(
            AnimationBlendCurvePayload curve,
            float normalizedTime,
            out AnimationBlendCurveSegment segment,
            out float u)
        {
            if (curve == null)
                throw new ArgumentNullException(nameof(curve));
            if (!float.IsFinite(normalizedTime))
                throw new ArgumentOutOfRangeException(nameof(normalizedTime));
            IReadOnlyList<AnimationBlendCurveSegment> segments = curve.Segments;
            float time = Mathf.Clamp01(normalizedTime);
            int index = segments.Count - 1;
            for (int i = 0; i < segments.Count; i++)
            {
                if (time <= segments[i].EndTime)
                {
                    index = i;
                    break;
                }
            }
            segment = segments[index];
            u = (time - segment.StartTime) / (segment.EndTime - segment.StartTime);
        }
    }
}
