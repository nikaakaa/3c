using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.RootMotion
{
    public static class AnimationClipNormalizationAuthoringService
    {
        const string RootPath = "Bip001";

        public static AnimationClip CreateRootOffsetNormalizedCopy(
            AnimationClip source,
            string assetPath,
            AnimationClip rootRotationSource)
        {
            if (!source)
                throw new ArgumentNullException(nameof(source));
            if (!rootRotationSource)
                throw new ArgumentNullException(nameof(rootRotationSource));
            if (string.IsNullOrWhiteSpace(assetPath))
                throw new ArgumentException("Animation output path is required.", nameof(assetPath));
            if (AssetDatabase.LoadMainAssetAtPath(assetPath))
                throw new InvalidOperationException($"Animation asset already exists: {assetPath}");

            AnimationClip destination = UnityEngine.Object.Instantiate(source);
            destination.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(destination, assetPath);

            Normalize(source, destination, rootRotationSource);
            return destination;
        }

        public static void ReplaceRootOffsetNormalizedCopy(
            AnimationClip source,
            AnimationClip destination,
            AnimationClip rootRotationSource)
        {
            if (!source || !destination || !rootRotationSource || source == destination)
                throw new ArgumentException("Animation replacement requires distinct source and destination clips.");
            string destinationName = destination.name;
            Undo.RecordObject(destination, "Replace normalized animation source");
            EditorUtility.CopySerialized(source, destination);
            destination.name = destinationName;
            Normalize(source, destination, rootRotationSource);
        }

        static void Normalize(AnimationClip source, AnimationClip destination, AnimationClip rootRotationSource)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(source);
            for (int i = 0; i < bindings.Length; i++)
            {
                EditorCurveBinding binding = bindings[i];
                AnimationCurve curve = AnimationUtility.GetEditorCurve(source, binding);
                if (binding.path == RootPath &&
                    (binding.propertyName == "m_LocalPosition.x" || binding.propertyName == "m_LocalPosition.z"))
                {
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
                }

                AnimationUtility.SetEditorCurve(destination, binding, curve);
            }

            if (rootRotationSource != source)
                CopyRootRotationCurves(rootRotationSource, destination);
            EditorUtility.SetDirty(destination);
        }

        public static void CopyRootRotationCurves(AnimationClip source, AnimationClip destination)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(source);
            for (int i = 0; i < bindings.Length; i++)
            {
                EditorCurveBinding binding = bindings[i];
                if (binding.path != RootPath ||
                    !binding.propertyName.StartsWith("m_LocalRotation.", StringComparison.Ordinal))
                    continue;
                AnimationUtility.SetEditorCurve(
                    destination,
                    binding,
                    AnimationUtility.GetEditorCurve(source, binding));
            }
            EditorUtility.SetDirty(destination);
        }
    }
}
