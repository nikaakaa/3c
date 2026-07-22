using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.MotionMatching
{
    public sealed class AnimationClipMotionMatchingParameterCurveResolver : IMotionMatchingProjectionParameterCurveResolver
    {
        public static readonly AnimationClipMotionMatchingParameterCurveResolver Instance =
            new AnimationClipMotionMatchingParameterCurveResolver();

        AnimationClipMotionMatchingParameterCurveResolver() { }

        public MotionMatchingPoseParameterCurvePayload ResolveRequired(
            AnimationClip clip,
            PoseParameterId parameterId)
        {
            if (!clip)
                throw new ArgumentNullException(nameof(clip));
            if (!parameterId.IsValid)
                throw new ArgumentException("Pose Parameter identity is invalid.", nameof(parameterId));
            EditorCurveBinding? resolvedBinding = null;
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            for (int i = 0; i < bindings.Length; i++)
            {
                if (!string.Equals(bindings[i].propertyName, parameterId.Value, StringComparison.Ordinal))
                    continue;
                if (resolvedBinding.HasValue)
                    throw new InvalidOperationException($"AnimationClip '{clip.name}' duplicates Motion Matching parameter curve '{parameterId}'.");
                resolvedBinding = bindings[i];
            }
            if (!resolvedBinding.HasValue)
                throw new InvalidOperationException($"AnimationClip '{clip.name}' has no Motion Matching parameter curve '{parameterId}'.");
            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, resolvedBinding.Value);
            if (curve == null || curve.length == 0 || !float.IsFinite(clip.length) || clip.length <= 0f)
                throw new InvalidOperationException($"AnimationClip '{clip.name}' Motion Matching parameter curve '{parameterId}' is invalid.");

            var normalizedTimes = new List<float> { 0f };
            for (int i = 0; i < curve.keys.Length; i++)
            {
                float normalized = Mathf.Clamp01(curve.keys[i].time / clip.length);
                if (normalized > normalizedTimes[normalizedTimes.Count - 1] && normalized < 1f)
                    normalizedTimes.Add(normalized);
            }
            normalizedTimes.Add(1f);
            var values = new float[normalizedTimes.Count];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = curve.Evaluate(normalizedTimes[i] * clip.length);
                if (!float.IsFinite(values[i]) || values[i] < 0f || values[i] > 1f)
                    throw new InvalidOperationException($"AnimationClip '{clip.name}' Motion Matching parameter curve '{parameterId}' contains a value outside [0, 1].");
            }
            return new MotionMatchingPoseParameterCurvePayload(parameterId, normalizedTimes.ToArray(), values);
        }
    }
}
