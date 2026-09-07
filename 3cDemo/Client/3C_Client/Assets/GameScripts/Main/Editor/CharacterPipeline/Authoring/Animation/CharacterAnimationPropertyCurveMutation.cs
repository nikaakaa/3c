using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class CharacterAnimationPropertyCurveMutation
    {
        internal static void ValidateClips(CharacterAnimationPropertyImportPlan plan)
        {
            for (int i = 0; i < plan.SourceClosure.Count; i++)
            {
                CharacterAnimationPropertyImportClipTarget target = plan.SourceClosure[i];
                CharacterAnimationClipContentIdentity identity =
                    CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(target.Clip);
                if (!string.Equals(identity.AssetGuid, target.AssetGuid, StringComparison.Ordinal) ||
                    identity.LocalFileId != target.LocalFileId ||
                    !string.Equals(identity.FullDependencyHash, target.DependencyHash, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"AnimationClip '{target.AssetPath}' changed after analyze.");
                }
                Dictionary<string, AnimationCurve> currentCurves =
                    CharacterAnimationPropertyCurveAnalyzer.ReadBlendShapeCurves(
                        target.Clip,
                        plan.AnimationCurvePath);
                if (currentCurves.Count != plan.CurveSet.Count ||
                    !new HashSet<string>(currentCurves.Keys, StringComparer.Ordinal)
                        .SetEquals(plan.CurveSet.Select(value => value.SourceBlendShapeName)))
                {
                    throw new InvalidOperationException(
                        $"AnimationClip '{target.AssetPath}' BlendShape curve set changed after analyze.");
                }
                CharacterAnimationRootCurveClassifier.RequireExact(target.Clip);
                if (!string.Equals(
                        CharacterAnimationRootCurveClassifier.ComputeEvidenceHash(target.Clip),
                        target.RootEvidenceHash,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"AnimationClip '{target.AssetPath}' root evidence changed after analyze.");
                }
                foreach (CharacterAnimationPropertyImportCurveTarget curve in plan.CurveSet)
                {
                    EditorCurveBinding sourceBinding = EditorCurveBinding.FloatCurve(
                        plan.AnimationCurvePath,
                        typeof(SkinnedMeshRenderer),
                        curve.SourcePropertyName);
                    AnimationCurve source = AnimationUtility.GetEditorCurve(target.Clip, sourceBinding);
                    if (source == null || source.length == 0)
                        throw new InvalidOperationException(
                            $"AnimationClip '{target.AssetPath}' is missing '{curve.SourcePropertyName}'.");
                    if (curve.RequiresRename)
                    {
                        EditorCurveBinding targetBinding = EditorCurveBinding.FloatCurve(
                            plan.AnimationCurvePath,
                            typeof(SkinnedMeshRenderer),
                            curve.TargetPropertyName);
                        if (AnimationUtility.GetEditorCurve(target.Clip, targetBinding) != null)
                            throw new InvalidOperationException(
                                $"AnimationClip '{target.AssetPath}' already contains '{curve.TargetPropertyName}'.");
                    }
                }
            }
        }

        internal static void Apply(CharacterAnimationPropertyImportPlan plan)
        {
            for (int clipIndex = 0; clipIndex < plan.SourceClosure.Count; clipIndex++)
            {
                CharacterAnimationPropertyImportClipTarget target = plan.SourceClosure[clipIndex];
                foreach (CharacterAnimationPropertyImportCurveTarget curve in plan.CurveSet)
                {
                    EditorCurveBinding sourceBinding = EditorCurveBinding.FloatCurve(
                        plan.AnimationCurvePath,
                        typeof(SkinnedMeshRenderer),
                        curve.SourcePropertyName);
                    EditorCurveBinding targetBinding = EditorCurveBinding.FloatCurve(
                        plan.AnimationCurvePath,
                        typeof(SkinnedMeshRenderer),
                        curve.TargetPropertyName);
                    if (curve.RequiresRename)
                    {
                        AnimationCurve source = AnimationUtility.GetEditorCurve(
                            target.Clip,
                            sourceBinding);
                        AnimationUtility.SetEditorCurve(
                            target.Clip,
                            targetBinding,
                            CopyCurve(source));
                        if (AnimationUtility.GetEditorCurve(target.Clip, targetBinding) == null)
                            throw new InvalidOperationException(
                                $"AnimationClip '{target.AssetPath}' could not receive '{curve.TargetPropertyName}'.");
                        RequireEquivalent(
                            source,
                            AnimationUtility.GetEditorCurve(target.Clip, targetBinding),
                            target.AssetPath,
                            curve.SourcePropertyName,
                            curve.TargetPropertyName);
                        AnimationUtility.SetEditorCurve(target.Clip, sourceBinding, null);
                    }
                    AnimationCurve current = AnimationUtility.GetEditorCurve(
                        target.Clip,
                        targetBinding);
                    if (current == null)
                        throw new InvalidOperationException(
                            $"AnimationClip '{target.AssetPath}' has no target curve '{curve.TargetPropertyName}'.");
                    for (int keyIndex = 0; keyIndex < current.length; keyIndex++)
                    {
                        AnimationUtility.SetKeyLeftTangentMode(
                            current,
                            keyIndex,
                            AnimationUtility.TangentMode.Linear);
                        AnimationUtility.SetKeyRightTangentMode(
                            current,
                            keyIndex,
                            AnimationUtility.TangentMode.Linear);
                    }
                    AnimationUtility.SetEditorCurve(target.Clip, targetBinding, current);
                }
                VerifyAfterApply(plan, target);
                EditorUtility.SetDirty(target.Clip);
            }
        }

        static AnimationCurve CopyCurve(AnimationCurve source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            return new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
        }

        static void VerifyAfterApply(
            CharacterAnimationPropertyImportPlan plan,
            CharacterAnimationPropertyImportClipTarget clip)
        {
            Dictionary<string, AnimationCurve> curves =
                CharacterAnimationPropertyCurveAnalyzer.ReadBlendShapeCurves(
                    clip.Clip,
                    plan.AnimationCurvePath);
            if (curves.Count != plan.CurveSet.Count)
                throw new InvalidOperationException(
                    $"AnimationClip '{clip.AssetPath}' must retain exactly {plan.CurveSet.Count} target BlendShape curves.");
            foreach (CharacterAnimationPropertyImportCurveTarget curve in plan.CurveSet)
            {
                AnimationCurve target = curves.TryGetValue(
                    curve.TargetBlendShapeName,
                    out AnimationCurve value)
                    ? value
                    : null;
                if (target == null)
                    throw new InvalidOperationException(
                        $"AnimationClip '{clip.AssetPath}' is missing target BlendShape '{curve.TargetBlendShapeName}'.");
                for (int keyIndex = 0; keyIndex < target.length; keyIndex++)
                {
                    if (AnimationUtility.GetKeyLeftTangentMode(target, keyIndex) != AnimationUtility.TangentMode.Linear ||
                        AnimationUtility.GetKeyRightTangentMode(target, keyIndex) != AnimationUtility.TangentMode.Linear)
                    {
                        throw new InvalidOperationException(
                            $"AnimationClip '{clip.AssetPath}' target BlendShape '{curve.TargetBlendShapeName}' is not linear.");
                    }
                }
                if (curve.RequiresRename &&
                    AnimationUtility.GetEditorCurve(
                        clip.Clip,
                        EditorCurveBinding.FloatCurve(
                            plan.AnimationCurvePath,
                            typeof(SkinnedMeshRenderer),
                            curve.SourcePropertyName)) != null)
                {
                    throw new InvalidOperationException(
                        $"AnimationClip '{clip.AssetPath}' retained old BlendShape binding '{curve.SourcePropertyName}'.");
                }
            }
            if (!string.Equals(
                    CharacterAnimationRootCurveClassifier.ComputeEvidenceHash(clip.Clip),
                    clip.RootEvidenceHash,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"AnimationClip '{clip.AssetPath}' root evidence was modified.");
            }
        }

        static void RequireEquivalent(
            AnimationCurve source,
            AnimationCurve target,
            string clipPath,
            string sourceProperty,
            string targetProperty)
        {
            if (source == null || target == null ||
                source.preWrapMode != target.preWrapMode ||
                source.postWrapMode != target.postWrapMode ||
                source.length != target.length)
            {
                throw new InvalidOperationException(
                    $"AnimationClip '{clipPath}' curve '{sourceProperty}' was not copied exactly to '{targetProperty}'.");
            }
            for (int i = 0; i < source.length; i++)
            {
                Keyframe left = source[i];
                Keyframe right = target[i];
                if (!left.time.Equals(right.time) ||
                    !left.value.Equals(right.value) ||
                    !left.inTangent.Equals(right.inTangent) ||
                    !left.outTangent.Equals(right.outTangent) ||
                    !left.inWeight.Equals(right.inWeight) ||
                    !left.outWeight.Equals(right.outWeight) ||
                    left.weightedMode != right.weightedMode ||
                    AnimationUtility.GetKeyLeftTangentMode(source, i) != AnimationUtility.GetKeyLeftTangentMode(target, i) ||
                    AnimationUtility.GetKeyRightTangentMode(source, i) != AnimationUtility.GetKeyRightTangentMode(target, i))
                {
                    throw new InvalidOperationException(
                        $"AnimationClip '{clipPath}' curve '{sourceProperty}' was not copied exactly to '{targetProperty}'.");
                }
            }
        }
    }
}
