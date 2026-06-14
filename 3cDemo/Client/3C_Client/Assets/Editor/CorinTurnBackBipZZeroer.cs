using System;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonAnimation.EditorTools
{
    public static class CorinTurnBackBipZZeroer
    {
        const string DefaultClipPath = "Assets/Art/Animation/MyDemoNeed/Corin/WithWeaponInplace/Corin_TurnBack_NoRootTurn_WithWeaponInplace_MyTest.anim";
        const string DefaultRootPath = "Bip001";
        const string LocalPositionZ = "m_LocalPosition.z";

        [MenuItem("Tools/3C/Corin/Zero MyTest TurnBack Bip001 Z")]
        public static void ZeroDefaultClipBipZ()
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DefaultClipPath);
            if (clip == null)
            {
                Debug.LogError($"TurnBack clip not found: {DefaultClipPath}");
                return;
            }

            ZeroLocalPositionZ(clip, DefaultRootPath);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = clip;
            EditorGUIUtility.PingObject(clip);
            Debug.Log($"Zeroed {DefaultRootPath}.{LocalPositionZ}: {DefaultClipPath}");
        }

        public static void ZeroLocalPositionZ(AnimationClip clip, string rootPath)
        {
            if (clip == null)
                throw new ArgumentNullException(nameof(clip));

            string normalizedRootPath = NormalizePath(rootPath);
            bool changed = false;
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (!IsTargetBinding(binding, normalizedRootPath))
                    continue;

                AnimationCurve source = AnimationUtility.GetEditorCurve(clip, binding);
                AnimationUtility.SetEditorCurve(clip, binding, BuildZeroCurve(source, clip.length));
                changed = true;
            }

            if (!changed)
            {
                EditorCurveBinding binding = EditorCurveBinding.FloatCurve(normalizedRootPath, typeof(Transform), LocalPositionZ);
                AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Constant(0f, Mathf.Max(clip.length, 0.0001f), 0f));
            }
        }

        static bool IsTargetBinding(EditorCurveBinding binding, string rootPath)
        {
            return binding.type == typeof(Transform) &&
                   string.Equals(NormalizePath(binding.path), rootPath, StringComparison.Ordinal) &&
                   string.Equals(binding.propertyName, LocalPositionZ, StringComparison.Ordinal);
        }

        static AnimationCurve BuildZeroCurve(AnimationCurve source, float clipLength)
        {
            if (source == null || source.length <= 0)
                return AnimationCurve.Constant(0f, Mathf.Max(clipLength, 0.0001f), 0f);

            Keyframe[] keys = source.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                key.value = 0f;
                key.inTangent = 0f;
                key.outTangent = 0f;
                key.weightedMode = WeightedMode.None;
                keys[i] = key;
            }

            var curve = new AnimationCurve(keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
            return curve;
        }

        static string NormalizePath(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/').Trim('/');
        }
    }
}
