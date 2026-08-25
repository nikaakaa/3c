using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Simulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public readonly struct CharacterAnimationClipOpenRequest
    {
        public CharacterAnimationClipOpenRequest(
            CharacterPipelineDefinition definition,
            CharacterAnimationPresentationProfile profile,
            AnimationClip clip,
            GameObject previewTarget)
        {
            Definition = definition;
            Profile = profile;
            Clip = clip;
            PreviewTarget = previewTarget;
        }

        public CharacterPipelineDefinition Definition { get; }
        public CharacterAnimationPresentationProfile Profile { get; }
        public AnimationClip Clip { get; }
        public GameObject PreviewTarget { get; }

        public void RequireValid()
        {
            if (!Definition || !Profile || !Clip || !PreviewTarget ||
                Definition.AnimationPresentationProfile != Profile ||
                EditorUtility.IsPersistent(PreviewTarget))
            {
                throw new InvalidOperationException(
                    "AnimationClip open request requires one exact Definition, Profile, Clip and scene Preview Target.");
            }
            _ = CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(Clip);
        }
    }

    public static class CharacterAnimationClipAuthoringService
    {
        public static void Open(CharacterAnimationClipOpenRequest request)
        {
            request.RequireValid();
            ClipCurves[] receivers = request.PreviewTarget.GetComponents<ClipCurves>();
            if (receivers.Length == 0)
                Undo.AddComponent<ClipCurves>(request.PreviewTarget);
            else if (receivers.Length != 1)
                throw new InvalidOperationException("AnimationClip Preview Target contains duplicate authoring Curve receivers.");
            Selection.activeGameObject = request.PreviewTarget;
            AnimationWindow window = EditorWindow.GetWindow<AnimationWindow>();
            window.animationClip = request.Clip;
            window.Show();
            window.Focus();
        }

        public static void ReplaceRegisteredCurve(
            AnimationClip clip,
            string channelId,
            AnimationCurve curve,
            string undoName)
        {
            if (string.IsNullOrWhiteSpace(undoName))
                throw new ArgumentException("AnimationClip Curve mutation Undo name is required.", nameof(undoName));
            Undo.RecordObject(clip, undoName);
            CharacterAnimationClipRegisteredCurveCatalog.Replace(clip, channelId, curve);
        }

        public static void ReplaceSource(
            AnimationClip source,
            AnimationClip target,
            bool normalizeRootTranslation)
        {
            if (!source || !target || source == target)
                throw new ArgumentException("Animation source replacement requires different source and target Clips.");
            string sourcePath = AssetDatabase.GetAssetPath(source);
            string targetPath = AssetDatabase.GetAssetPath(target);
            if (AssetDatabase.LoadMainAssetAtPath(sourcePath) != source ||
                AssetDatabase.LoadMainAssetAtPath(targetPath) != target ||
                !sourcePath.EndsWith(".anim", StringComparison.OrdinalIgnoreCase) ||
                !targetPath.EndsWith(".anim", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Animation source replacement requires persisted native AnimationClips.");
            float sourceDuration = CharacterAnimationClipRegisteredCurveCatalog.ResolveSourceDurationSeconds(source);
            float targetDuration = CharacterAnimationClipRegisteredCurveCatalog.ResolveSourceDurationSeconds(target);
            if (Mathf.Abs(sourceDuration - targetDuration) > 0.00001f ||
                !source.frameRate.Equals(target.frameRate) ||
                source.isLooping != target.isLooping ||
                source.humanMotion != target.humanMotion)
                throw new InvalidOperationException("Animation source and target timing, Loop, Sample Rate or animation type differs.");

            var registered = new Dictionary<string, AnimationCurve>(StringComparer.Ordinal);
            for (int i = 0; i < CharacterAnimationClipRegisteredCurveCatalog.Channels.Count; i++)
            {
                CharacterAnimationClipRegisteredCurveDescriptor descriptor =
                    CharacterAnimationClipRegisteredCurveCatalog.Channels[i];
                if (CharacterAnimationClipRegisteredCurveCatalog.TryRead(
                        target,
                        descriptor.ChannelId,
                        out AnimationCurve curve))
                    registered.Add(descriptor.ChannelId, curve);
            }

            AnimationClip backup = UnityEngine.Object.Instantiate(target);
            backup.hideFlags = HideFlags.HideAndDontSave;
            string targetName = target.name;
            try
            {
                Undo.RecordObject(target, "Replace Animation Source");
                EditorUtility.CopySerialized(source, target);
                target.name = targetName;
                if (normalizeRootTranslation)
                    NormalizeRootTranslation(target);
                foreach (KeyValuePair<string, AnimationCurve> pair in registered)
                    CharacterAnimationClipRegisteredCurveCatalog.Replace(target, pair.Key, pair.Value);
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssetIfDirty(target);
            }
            catch
            {
                EditorUtility.CopySerialized(backup, target);
                target.name = targetName;
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssetIfDirty(target);
                throw;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(backup);
            }
        }

        static void NormalizeRootTranslation(AnimationClip target)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(target);
            for (int i = 0; i < bindings.Length; i++)
            {
                EditorCurveBinding binding = bindings[i];
                bool rootBone = string.Equals(binding.path, "Bip001", StringComparison.Ordinal) &&
                                (string.Equals(binding.propertyName, "m_LocalPosition.x", StringComparison.Ordinal) ||
                                 string.Equals(binding.propertyName, "m_LocalPosition.z", StringComparison.Ordinal));
                bool animatorRoot = string.IsNullOrEmpty(binding.path) &&
                                    (string.Equals(binding.propertyName, "RootT.x", StringComparison.Ordinal) ||
                                     string.Equals(binding.propertyName, "RootT.z", StringComparison.Ordinal));
                if (!rootBone && !animatorRoot)
                    continue;
                AnimationCurve curve = AnimationUtility.GetEditorCurve(target, binding);
                Keyframe[] keys = curve.keys;
                for (int keyIndex = 0; keyIndex < keys.Length; keyIndex++)
                {
                    Keyframe key = keys[keyIndex];
                    key.value = 0f;
                    key.inTangent = 0f;
                    key.outTangent = 0f;
                    keys[keyIndex] = key;
                }
                curve.keys = keys;
                AnimationUtility.SetEditorCurve(target, binding, curve);
            }
        }
    }
}
