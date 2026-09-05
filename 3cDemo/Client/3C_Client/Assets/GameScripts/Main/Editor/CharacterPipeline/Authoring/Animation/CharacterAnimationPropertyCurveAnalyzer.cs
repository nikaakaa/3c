using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterAnimationPropertyCurveAnalysis
    {
        internal CharacterAnimationPropertyCurveAnalysis(
            IReadOnlyList<CharacterAnimationPropertyImportClipTarget> clips,
            IReadOnlyList<CharacterAnimationPropertyImportCurveTarget> curves)
        {
            Clips = clips ?? throw new ArgumentNullException(nameof(clips));
            Curves = curves ?? throw new ArgumentNullException(nameof(curves));
        }

        internal IReadOnlyList<CharacterAnimationPropertyImportClipTarget> Clips { get; }
        internal IReadOnlyList<CharacterAnimationPropertyImportCurveTarget> Curves { get; }
    }

    internal static class CharacterAnimationPropertyCurveAnalyzer
    {
        internal static CharacterAnimationPropertyCurveAnalysis Analyze(
            IReadOnlyList<CharacterAnimationResourceClosureEntry> closure,
            string animationCurvePath,
            Mesh mesh)
        {
            Dictionary<string, AnimationCurve> first = null;
            var analyzedClips = new List<CharacterAnimationPropertyImportClipTarget>(closure.Count);
            for (int i = 0; i < closure.Count; i++)
            {
                CharacterAnimationResourceClosureEntry entry = closure[i];
                Dictionary<string, AnimationCurve> current =
                    ReadBlendShapeCurves(entry.Clip, animationCurvePath);
                if (first == null)
                {
                    first = current;
                }
                else if (!new HashSet<string>(first.Keys, StringComparer.Ordinal)
                             .SetEquals(current.Keys))
                {
                    throw new InvalidOperationException(
                        $"AnimationClip '{entry.Identity.AssetPath}' does not have the exact formal BlendShape curve set.");
                }
                string[] rootEvidence = CharacterAnimationRootCurveClassifier.AllowNoneOrRequireExact(entry.Clip);
                analyzedClips.Add(new CharacterAnimationPropertyImportClipTarget(
                    entry.Clip,
                    entry.Identity,
                    entry.SourceCategory,
                    entry.CurrentBackend,
                    ComputeBlendShapeCurveHash(current),
                    CountLinearizationRequired(current),
                    rootEvidence,
                    CharacterAnimationRootCurveClassifier.ComputeEvidenceHash(entry.Clip)));
            }
            if (first == null || first.Count == 0)
                throw new InvalidOperationException("Formal Animation source closure has no BlendShape curves.");

            var curves = new List<CharacterAnimationPropertyImportCurveTarget>(first.Count);
            var parameterIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (string sourceName in first.Keys.OrderBy(value => value, StringComparer.Ordinal))
            {
                string targetName = sourceName;
                int blendShapeIndex = mesh.GetBlendShapeIndex(sourceName);
                if (blendShapeIndex < 0)
                {
                    var matches = new List<int>();
                    for (int index = 0; index < mesh.blendShapeCount; index++)
                    {
                        if (string.Equals(
                                mesh.GetBlendShapeName(index),
                                sourceName,
                                StringComparison.OrdinalIgnoreCase))
                            matches.Add(index);
                    }
                    if (matches.Count != 1)
                        throw new InvalidOperationException(
                            $"BlendShape '{sourceName}' does not resolve uniquely on Mesh '{mesh.name}'.");
                    blendShapeIndex = matches[0];
                    targetName = mesh.GetBlendShapeName(blendShapeIndex);
                }
                string parameterId = CharacterAnimationPropertyImportIdentity.ParameterId(targetName);
                if (!parameterIds.Add(parameterId))
                    throw new InvalidOperationException(
                        $"BlendShape parameter identity '{parameterId}' is duplicated after Unicode encoding.");
                curves.Add(new CharacterAnimationPropertyImportCurveTarget(
                    sourceName,
                    targetName,
                    blendShapeIndex,
                    parameterId,
                    !string.Equals(sourceName, targetName, StringComparison.Ordinal)));
            }
            return new CharacterAnimationPropertyCurveAnalysis(
                analyzedClips.ToArray(),
                curves.ToArray());
        }

        internal static Dictionary<string, AnimationCurve> ReadBlendShapeCurves(
            AnimationClip clip,
            string animationCurvePath)
        {
            var result = new Dictionary<string, AnimationCurve>(StringComparer.Ordinal);
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.type != typeof(SkinnedMeshRenderer) ||
                    !binding.propertyName.StartsWith("blendShape.", StringComparison.Ordinal))
                    continue;
                if (!string.Equals(binding.path, animationCurvePath, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        $"AnimationClip '{clip.name}' contains BlendShape curve '{binding.path}:{binding.propertyName}' outside the explicit path '{animationCurvePath}'.");
                string name = binding.propertyName.Substring("blendShape.".Length);
                if (!result.TryAdd(name, AnimationUtility.GetEditorCurve(clip, binding)))
                    throw new InvalidOperationException(
                        $"AnimationClip '{clip.name}' has duplicate BlendShape curve '{name}'.");
                if (result[name] == null || result[name].length == 0)
                    throw new InvalidOperationException(
                        $"AnimationClip '{clip.name}' BlendShape curve '{name}' is empty.");
            }
            if (result.Count == 0)
                throw new InvalidOperationException(
                    $"AnimationClip '{clip.name}' has no BlendShape curve at '{animationCurvePath}'.");
            return result;
        }

        static int CountLinearizationRequired(
            IReadOnlyDictionary<string, AnimationCurve> curves)
        {
            int count = 0;
            foreach (AnimationCurve curve in curves.Values)
            {
                bool linear = true;
                for (int i = 0; i < curve.length; i++)
                {
                    if (AnimationUtility.GetKeyLeftTangentMode(curve, i) != AnimationUtility.TangentMode.Linear ||
                        AnimationUtility.GetKeyRightTangentMode(curve, i) != AnimationUtility.TangentMode.Linear)
                    {
                        linear = false;
                        break;
                    }
                }
                if (!linear)
                    count++;
            }
            return count;
        }

        static string ComputeBlendShapeCurveHash(
            IReadOnlyDictionary<string, AnimationCurve> curves)
        {
            var values = new List<string> { "character-animation-blendshape-curves/v1" };
            foreach (KeyValuePair<string, AnimationCurve> pair in curves.OrderBy(value => value.Key, StringComparer.Ordinal))
            {
                AnimationCurve curve = pair.Value;
                values.Add(pair.Key);
                values.Add(curve.preWrapMode.ToString());
                values.Add(curve.postWrapMode.ToString());
                values.Add(curve.length.ToString(CultureInfo.InvariantCulture));
                for (int i = 0; i < curve.length; i++)
                {
                    Keyframe key = curve[i];
                    values.Add(key.time.ToString("R", CultureInfo.InvariantCulture));
                    values.Add(key.value.ToString("R", CultureInfo.InvariantCulture));
                    values.Add(key.inTangent.ToString("R", CultureInfo.InvariantCulture));
                    values.Add(key.outTangent.ToString("R", CultureInfo.InvariantCulture));
                    values.Add(key.inWeight.ToString("R", CultureInfo.InvariantCulture));
                    values.Add(key.outWeight.ToString("R", CultureInfo.InvariantCulture));
                    values.Add(key.weightedMode.ToString());
                    values.Add(AnimationUtility.GetKeyLeftTangentMode(curve, i).ToString());
                    values.Add(AnimationUtility.GetKeyRightTangentMode(curve, i).ToString());
                }
            }
            return CharacterAclHash.ComputeStrings(values);
        }
    }
}
