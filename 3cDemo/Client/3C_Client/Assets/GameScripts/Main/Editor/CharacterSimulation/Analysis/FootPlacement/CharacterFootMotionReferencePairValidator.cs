using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public static class CharacterFootMotionReferencePairValidator
    {
        const float TimeTolerance = 0.00001f;
        const float RootTolerance = 0.00001f;
        const float CurveTolerance = 0.0001f;
        const float SettledTranslationTailTolerance = 0.002f;
        const float SettledRotationTailTolerance = 0.01f;
        const float MaximumSettledTailIntervals = 2f;

        public static float RequireCompatible(
            in CharacterFootMotionReference reference,
            CharacterAnimationRigDefinition rig,
            AnimationBoneId motionRootBoneId)
        {
            if (!rig)
                throw new ArgumentNullException(nameof(rig));
            rig.RequireValid();
            _ = rig.RequirePhysicalBoneIndex(motionRootBoneId);
            AnimationClip target = reference.Target;
            AnimationClip motion = reference.MotionReference;
            float targetDuration = CharacterAnimationClipRegisteredCurveCatalog.ResolveSourceDurationSeconds(target);
            float motionDuration = CharacterAnimationClipRegisteredCurveCatalog.ResolveSourceDurationSeconds(motion);
            if (Mathf.Abs(targetDuration - motionDuration) > TimeTolerance ||
                target.isLooping != motion.isLooping ||
                !target.frameRate.Equals(motion.frameRate) ||
                target.humanMotion != motion.humanMotion)
                throw new InvalidOperationException($"Foot Motion pair '{target.name}'/'{motion.name}' timing, Loop, Sample Rate or animation type differs.");
            string rootPath = ResolveRootPath(motionRootBoneId);
            HashSet<string> analysisPaths = AnalysisPaths(rig, rootPath);
            EditorCurveBinding[] targetBindings = SourceBindings(target, analysisPaths);
            EditorCurveBinding[] motionBindings = SourceBindings(motion, analysisPaths);
            if (targetBindings.Length != motionBindings.Length)
            {
                string targetOnly = string.Join(",", targetBindings
                    .Where(left => !motionBindings.Any(right => CharacterAnimationClipRegisteredCurveCatalog.SameBinding(left, right)))
                    .Take(8)
                    .Select(BindingText));
                string motionOnly = string.Join(",", motionBindings
                    .Where(left => !targetBindings.Any(right => CharacterAnimationClipRegisteredCurveCatalog.SameBinding(left, right)))
                    .Take(8)
                    .Select(BindingText));
                throw new InvalidOperationException(
                    $"Foot Motion pair '{target.name}'/'{motion.name}' source binding counts differ {targetBindings.Length}/{motionBindings.Length}; TargetOnly={targetOnly}; MotionOnly={motionOnly}.");
            }
            for (int i = 0; i < targetBindings.Length; i++)
            {
                EditorCurveBinding targetBinding = targetBindings[i];
                EditorCurveBinding motionBinding = motionBindings[i];
                if (!CharacterAnimationClipRegisteredCurveCatalog.SameBinding(targetBinding, motionBinding))
                    throw new InvalidOperationException($"Foot Motion pair '{target.name}'/'{motion.name}' source binding #{i} differs.");
                AnimationCurve targetCurve = AnimationUtility.GetEditorCurve(target, targetBinding);
                AnimationCurve motionCurve = AnimationUtility.GetEditorCurve(motion, motionBinding);
                if (AllowedRootMotion(targetBinding, rootPath))
                {
                    if (RootTranslation(targetBinding, rootPath))
                        RequireInPlaceTargetRoot(target, targetBinding, targetCurve);
                    RequireCoverage(motion, motionBinding, motionCurve, motionDuration);
                    continue;
                }
                RequireSameCurve(target, motion, targetBinding, targetCurve, motionCurve, targetDuration);
            }
            RequireSameObjectCurves(target, motion);
            float displacement = ResolveHorizontalRootDisplacement(motion, motionBindings, rootPath, motionDuration);
            return displacement;
        }

        static EditorCurveBinding[] SourceBindings(
            AnimationClip clip,
            HashSet<string> analysisPaths) =>
            AnimationUtility.GetCurveBindings(clip)
                .Where(binding =>
                    !CharacterAnimationClipRegisteredCurveCatalog.IsRegistered(binding) &&
                    analysisPaths.Contains(binding.path ?? string.Empty))
                .OrderBy(binding => binding.path, StringComparer.Ordinal)
                .ThenBy(binding => binding.type?.AssemblyQualifiedName, StringComparer.Ordinal)
                .ThenBy(binding => binding.propertyName, StringComparer.Ordinal)
                .ToArray();

        static HashSet<string> AnalysisPaths(
            CharacterAnimationRigDefinition rig,
            string rootPath)
        {
            var result = new HashSet<string>(StringComparer.Ordinal)
            {
                string.Empty,
                "Root",
                rootPath,
                ResolveRootPath(rig.PelvisBoneId),
                ResolveRootPath(rig.LeftLeg.HipBoneId),
                ResolveRootPath(rig.LeftLeg.KneeBoneId),
                ResolveRootPath(rig.LeftLeg.AnkleBoneId),
                ResolveRootPath(rig.LeftLeg.ToeBoneId),
                ResolveRootPath(rig.RightLeg.HipBoneId),
                ResolveRootPath(rig.RightLeg.KneeBoneId),
                ResolveRootPath(rig.RightLeg.AnkleBoneId),
                ResolveRootPath(rig.RightLeg.ToeBoneId)
            };
            return result;
        }

        static bool AllowedRootMotion(EditorCurveBinding binding, string rootPath) =>
            string.Equals(binding.path, rootPath, StringComparison.Ordinal) &&
            (string.Equals(binding.propertyName, "m_LocalPosition.x", StringComparison.Ordinal) ||
             string.Equals(binding.propertyName, "m_LocalPosition.z", StringComparison.Ordinal) ||
             RootRotationProperty(binding.propertyName)) ||
            string.Equals(binding.path, "Root", StringComparison.Ordinal) &&
            (binding.propertyName.StartsWith("m_LocalPosition.", StringComparison.Ordinal) ||
             RootRotationProperty(binding.propertyName)) ||
            string.IsNullOrEmpty(binding.path) &&
            (string.Equals(binding.propertyName, "RootT.x", StringComparison.Ordinal) ||
             string.Equals(binding.propertyName, "RootT.y", StringComparison.Ordinal) ||
             string.Equals(binding.propertyName, "RootT.z", StringComparison.Ordinal) ||
             binding.propertyName.StartsWith("RootQ.", StringComparison.Ordinal));

        static bool RootRotationProperty(string property) =>
            property.StartsWith("m_LocalRotation.", StringComparison.Ordinal);

        static bool RootTranslation(EditorCurveBinding binding, string rootPath) =>
            string.Equals(binding.path, rootPath, StringComparison.Ordinal) &&
            (string.Equals(binding.propertyName, "m_LocalPosition.x", StringComparison.Ordinal) ||
             string.Equals(binding.propertyName, "m_LocalPosition.z", StringComparison.Ordinal)) ||
            string.IsNullOrEmpty(binding.path) &&
            (string.Equals(binding.propertyName, "RootT.x", StringComparison.Ordinal) ||
             string.Equals(binding.propertyName, "RootT.z", StringComparison.Ordinal));

        static void RequireInPlaceTargetRoot(
            AnimationClip target,
            EditorCurveBinding binding,
            AnimationCurve curve)
        {
            if (curve == null || curve.length < 1)
                throw new InvalidOperationException($"Target '{target.name}' Root Curve '{binding.propertyName}' is missing.");
            Keyframe[] keys = curve.keys;
            if (Mathf.Abs(keys[keys.Length - 1].value - keys[0].value) > RootTolerance)
                throw new InvalidOperationException($"Target '{target.name}' Root Curve '{binding.propertyName}' has non-zero net motion.");
        }

        static void RequireCoverage(
            AnimationClip clip,
            EditorCurveBinding binding,
            AnimationCurve curve,
            float duration)
        {
            if (curve == null || curve.length < 1 || clip.frameRate <= 0f)
                throw new InvalidOperationException($"Motion Reference '{clip.name}' Root Curve '{binding.propertyName}' does not cover the Clip.");
            Keyframe first = curve.keys[0];
            Keyframe last = curve.keys[curve.length - 1];
            if (Mathf.Abs(first.time) > TimeTolerance ||
                last.time > duration + TimeTolerance)
                throw new InvalidOperationException($"Motion Reference '{clip.name}' Root Curve '{binding.propertyName}' does not cover the Clip.");
            float tailDuration = duration - last.time;
            float sampleInterval = 1f / clip.frameRate;
            if (tailDuration <= sampleInterval + TimeTolerance)
                return;
            if (tailDuration > MaximumSettledTailIntervals * sampleInterval + TimeTolerance ||
                !float.IsFinite(last.outTangent) ||
                Mathf.Abs(last.outTangent) * tailDuration > SettledTailTolerance(binding))
                throw new InvalidOperationException($"Motion Reference '{clip.name}' Root Curve '{binding.propertyName}' does not cover the Clip.");
        }

        static float SettledTailTolerance(EditorCurveBinding binding) =>
            RootRotationProperty(binding.propertyName) ||
            binding.propertyName.StartsWith("RootQ.", StringComparison.Ordinal)
                ? SettledRotationTailTolerance
                : SettledTranslationTailTolerance;

        static void RequireSameCurve(
            AnimationClip target,
            AnimationClip motion,
            EditorCurveBinding binding,
            AnimationCurve left,
            AnimationCurve right,
            float duration)
        {
            if (left == null || right == null ||
                left.preWrapMode != right.preWrapMode || left.postWrapMode != right.postWrapMode)
                throw new InvalidOperationException($"Foot Motion pair '{target.name}'/'{motion.name}' Curve '{binding.path}/{binding.propertyName}' differs.");
            var times = new HashSet<float>();
            for (int i = 0; i < left.length; i++)
                times.Add(left.keys[i].time);
            for (int i = 0; i < right.length; i++)
                times.Add(right.keys[i].time);
            int intervals = Mathf.Max(2, Mathf.CeilToInt(duration * Mathf.Max(target.frameRate, motion.frameRate)));
            for (int i = 0; i <= intervals; i++)
                times.Add(duration * i / intervals);
            foreach (float time in times)
            {
                float difference = Mathf.Abs(left.Evaluate(time) - right.Evaluate(time));
                if (!float.IsFinite(difference) || difference > CurveTolerance)
                    throw new InvalidOperationException(
                        $"Foot Motion pair '{target.name}'/'{motion.name}' Curve '{binding.path}/{binding.propertyName}' differs by {difference:R} at {time:R}s.");
            }
        }

        static void RequireSameObjectCurves(AnimationClip target, AnimationClip motion)
        {
            EditorCurveBinding[] left = AnimationUtility.GetObjectReferenceCurveBindings(target)
                .OrderBy(binding => binding.path, StringComparer.Ordinal)
                .ThenBy(binding => binding.propertyName, StringComparer.Ordinal)
                .ToArray();
            EditorCurveBinding[] right = AnimationUtility.GetObjectReferenceCurveBindings(motion)
                .OrderBy(binding => binding.path, StringComparer.Ordinal)
                .ThenBy(binding => binding.propertyName, StringComparer.Ordinal)
                .ToArray();
            if (left.Length != right.Length)
                throw new InvalidOperationException($"Foot Motion pair '{target.name}'/'{motion.name}' object binding counts differ.");
            for (int i = 0; i < left.Length; i++)
            {
                if (!CharacterAnimationClipRegisteredCurveCatalog.SameBinding(left[i], right[i]))
                    throw new InvalidOperationException($"Foot Motion pair '{target.name}'/'{motion.name}' object binding #{i} differs.");
                ObjectReferenceKeyframe[] leftKeys = AnimationUtility.GetObjectReferenceCurve(target, left[i]);
                ObjectReferenceKeyframe[] rightKeys = AnimationUtility.GetObjectReferenceCurve(motion, right[i]);
                if (leftKeys.Length != rightKeys.Length)
                    throw new InvalidOperationException($"Foot Motion pair '{target.name}'/'{motion.name}' object Curve '{left[i].propertyName}' differs.");
                for (int keyIndex = 0; keyIndex < leftKeys.Length; keyIndex++)
                {
                    if (!Same(leftKeys[keyIndex].time, rightKeys[keyIndex].time) ||
                        leftKeys[keyIndex].value != rightKeys[keyIndex].value)
                        throw new InvalidOperationException($"Foot Motion pair '{target.name}'/'{motion.name}' object Curve '{left[i].propertyName}' key #{keyIndex} differs.");
                }
            }
        }

        static float ResolveHorizontalRootDisplacement(
            AnimationClip motion,
            IReadOnlyList<EditorCurveBinding> bindings,
            string rootPath,
            float duration)
        {
            float x = Delta(motion, bindings, rootPath, "m_LocalPosition.x", duration);
            float z = Delta(motion, bindings, rootPath, "m_LocalPosition.z", duration);
            return new Vector2(x, z).magnitude;
        }

        static float Delta(
            AnimationClip clip,
            IReadOnlyList<EditorCurveBinding> bindings,
            string rootPath,
            string property,
            float duration)
        {
            EditorCurveBinding binding = bindings.First(value =>
                string.Equals(value.path, rootPath, StringComparison.Ordinal) &&
                string.Equals(value.propertyName, property, StringComparison.Ordinal));
            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
            return curve.Evaluate(duration) - curve.Evaluate(0f);
        }

        static string ResolveRootPath(AnimationBoneId rootBoneId)
        {
            const string prefix = "animation-bone/";
            string value = rootBoneId.Value;
            if (string.IsNullOrEmpty(value) || !value.StartsWith(prefix, StringComparison.Ordinal) || value.Length <= prefix.Length)
                throw new InvalidOperationException("Foot Motion Rig Root BoneId cannot resolve an AnimationClip path.");
            return value.Substring(prefix.Length);
        }

        static string BindingText(EditorCurveBinding binding) =>
            $"{binding.path}/{binding.type?.Name}/{binding.propertyName}";

        static bool Same(float left, float right) =>
            BitConverter.SingleToInt32Bits(left) == BitConverter.SingleToInt32Bits(right);
    }
}
