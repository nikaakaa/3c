using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class CharacterAnimationRootCurveClassifier
    {
        internal static readonly string[] EvidenceNames =
        {
            "RootT.x",
            "RootT.y",
            "RootT.z",
            "RootQ.x",
            "RootQ.y",
            "RootQ.z",
            "RootQ.w"
        };

        internal static bool IsEvidence(EditorCurveBinding binding) =>
            string.IsNullOrEmpty(binding.path) &&
            binding.type == typeof(Animator) &&
            EvidenceNames.Contains(binding.propertyName, StringComparer.Ordinal);

        internal static string[] AllowNoneOrRequireExact(AnimationClip clip)
        {
            var actual = new HashSet<string>(StringComparer.Ordinal);
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (IsEvidence(binding))
                    actual.Add(binding.propertyName);
            }
            if (actual.Count == 0)
                return Array.Empty<string>();
            if (actual.Count != EvidenceNames.Length ||
                !actual.SetEquals(EvidenceNames))
            {
                throw new InvalidOperationException(
                    $"AnimationClip '{clip.name}' must contain the exact seven Animator root evidence curves.");
            }
            return EvidenceNames.ToArray();
        }

        internal static string[] RequireExact(AnimationClip clip)
        {
            string[] result = AllowNoneOrRequireExact(clip);
            if (result.Length != EvidenceNames.Length)
            {
                throw new InvalidOperationException(
                    $"AnimationClip '{clip.name}' must contain the exact seven Animator root evidence curves.");
            }
            return result;
        }

        internal static string ComputeEvidenceHash(AnimationClip clip)
        {
            AllowNoneOrRequireExact(clip);
            var values = new List<string> { "character-animation-root-evidence/v1" };
            for (int i = 0; i < EvidenceNames.Length; i++)
            {
                EditorCurveBinding binding = EditorCurveBinding.FloatCurve(
                    string.Empty,
                    typeof(Animator),
                    EvidenceNames[i]);
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                values.Add(EvidenceNames[i]);
                if (curve == null)
                {
                    values.Add("missing");
                    continue;
                }
                values.Add(curve.preWrapMode.ToString());
                values.Add(curve.postWrapMode.ToString());
                values.Add(curve.length.ToString(CultureInfo.InvariantCulture));
                for (int keyIndex = 0; keyIndex < curve.length; keyIndex++)
                {
                    Keyframe key = curve[keyIndex];
                    values.Add(key.time.ToString("R", CultureInfo.InvariantCulture));
                    values.Add(key.value.ToString("R", CultureInfo.InvariantCulture));
                    values.Add(key.inTangent.ToString("R", CultureInfo.InvariantCulture));
                    values.Add(key.outTangent.ToString("R", CultureInfo.InvariantCulture));
                    values.Add(key.inWeight.ToString("R", CultureInfo.InvariantCulture));
                    values.Add(key.outWeight.ToString("R", CultureInfo.InvariantCulture));
                    values.Add(key.weightedMode.ToString());
                    values.Add(AnimationUtility.GetKeyLeftTangentMode(curve, keyIndex).ToString());
                    values.Add(AnimationUtility.GetKeyRightTangentMode(curve, keyIndex).ToString());
                }
            }
            return CharacterAclHash.ComputeStrings(values);
        }
    }
}
