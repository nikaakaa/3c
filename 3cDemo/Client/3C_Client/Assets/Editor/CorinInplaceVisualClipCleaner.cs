using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonAnimation.EditorTools
{
    public sealed class CorinInplaceVisualClipCleanerWindow : EditorWindow
    {
        const string DefaultSourcePath = "Assets/Art/Animation/MyDemoNeed/Corin/WithWeaponRootmotion/Corin_TurnBack_WithWeaponRootmotion.anim";
        const string DefaultOutputPath = "Assets/Art/Animation/MyDemoNeed/Corin/WithWeaponInplace/Corin_TurnBack_NoRootTurn_WithWeaponInplace.anim";
        const string DefaultRootPath = "Bip001";

        AnimationClip sourceClip;
        string outputPath = DefaultOutputPath;
        string rootPath = DefaultRootPath;

        [MenuItem("Tools/3C/Corin/Inplace Visual Clip Cleaner")]
        public static void Open()
        {
            CorinInplaceVisualClipCleanerWindow window = GetWindow<CorinInplaceVisualClipCleanerWindow>("Inplace Visual Cleaner");
            window.minSize = new Vector2(560f, 170f);
            window.ResolveDefaults();
            window.Show();
        }

        [MenuItem("Tools/3C/Corin/Build Default TurnBack NoRootTurn Inplace Clip")]
        public static void BuildDefault()
        {
            CorinTurnBackProfileCompensatedInplaceBaker.BuildDefaultTurnBack();
        }

        void OnEnable()
        {
            ResolveDefaults();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
            sourceClip = (AnimationClip)EditorGUILayout.ObjectField("Clip", sourceClip, typeof(AnimationClip), false);
            outputPath = EditorGUILayout.TextField("Output Path", outputPath);
            rootPath = EditorGUILayout.TextField("Root Path", rootPath);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Legacy curve cleaner. For TurnBack profile-driven motion, use Build TurnBack Profile Compensated Inplace Clip so visual root pose is compensated against the baked profile.", MessageType.Info);

            using (new EditorGUI.DisabledScope(sourceClip == null || string.IsNullOrWhiteSpace(outputPath)))
            {
                if (GUILayout.Button("Build Clean Inplace Clip"))
                {
                    AnimationClip output = CorinInplaceVisualClipCleaner.CreateOrUpdateClipAsset(sourceClip, outputPath, rootPath);
                    Selection.activeObject = output;
                    EditorGUIUtility.PingObject(output);
                }
            }
        }

        void ResolveDefaults()
        {
            if (sourceClip == null)
                sourceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DefaultSourcePath);
            if (string.IsNullOrWhiteSpace(outputPath))
                outputPath = DefaultOutputPath;
            if (string.IsNullOrWhiteSpace(rootPath))
                rootPath = DefaultRootPath;
        }
    }

    public static class CorinInplaceVisualClipCleaner
    {
        public static AnimationClip CreateOrUpdateClipAsset(AnimationClip source, string outputPath, string rootPath)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            string normalizedPath = NormalizeAssetPath(outputPath);
            EnsureAssetFolder(normalizedPath);
            AnimationClip generated = BuildCleanClip(source, rootPath, Path.GetFileNameWithoutExtension(normalizedPath));
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(normalizedPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, normalizedPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return AssetDatabase.LoadAssetAtPath<AnimationClip>(normalizedPath);
            }

            EditorUtility.CopySerialized(generated, existing);
            existing.name = generated.name;
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return existing;
        }

        public static AnimationClip BuildCleanClip(AnimationClip source, string rootPath, string clipName)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var output = new AnimationClip
            {
                name = string.IsNullOrWhiteSpace(clipName) ? source.name + "_NoRootTurn" : clipName,
                frameRate = source.frameRate,
                wrapMode = source.wrapMode,
                legacy = source.legacy,
            };

            AnimationUtility.SetAnimationClipSettings(output, AnimationUtility.GetAnimationClipSettings(source));
            float duration = Mathf.Max(source.length, 1f / Mathf.Max(1f, source.frameRate));
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(source))
            {
                AnimationCurve sourceCurve = AnimationUtility.GetEditorCurve(source, binding);
                AnimationCurve outputCurve = ResolveOutputCurve(binding, sourceCurve, duration, rootPath);
                AnimationUtility.SetEditorCurve(output, binding, outputCurve);
            }

            foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
            {
                ObjectReferenceKeyframe[] curve = AnimationUtility.GetObjectReferenceCurve(source, binding);
                AnimationUtility.SetObjectReferenceCurve(output, binding, curve);
            }

            AnimationUtility.SetAnimationEvents(output, AnimationUtility.GetAnimationEvents(source));
            output.EnsureQuaternionContinuity();
            return output;
        }

        static AnimationCurve ResolveOutputCurve(EditorCurveBinding binding, AnimationCurve sourceCurve, float duration, string rootPath)
        {
            if (IsAnimatorPlanarRootT(binding))
                return ConstantCurve(duration, 0f);

            if (IsAnimatorRootQ(binding, out float identityValue))
                return ConstantCurve(duration, FirstValueOrDefault(sourceCurve, identityValue));

            if (IsRootTransformPlanarPosition(binding, rootPath))
                return ConstantCurve(duration, 0f);

            if (IsRootTransformRotation(binding, rootPath, out identityValue))
                return ConstantCurve(duration, FirstValueOrDefault(sourceCurve, identityValue));

            return CopyCurve(sourceCurve);
        }

        static bool IsAnimatorPlanarRootT(EditorCurveBinding binding)
        {
            return binding.path == string.Empty &&
                   binding.type == typeof(Animator) &&
                   (binding.propertyName == "RootT.x" || binding.propertyName == "RootT.z");
        }

        static bool IsAnimatorRootQ(EditorCurveBinding binding, out float identityValue)
        {
            identityValue = 0f;
            if (binding.path != string.Empty || binding.type != typeof(Animator))
                return false;

            if (binding.propertyName == "RootQ.w")
            {
                identityValue = 1f;
                return true;
            }

            return binding.propertyName == "RootQ.x" ||
                   binding.propertyName == "RootQ.y" ||
                   binding.propertyName == "RootQ.z";
        }

        static bool IsRootTransformPlanarPosition(EditorCurveBinding binding, string rootPath)
        {
            if (binding.type != typeof(Transform) || !IsRootPath(binding.path, rootPath))
                return false;

            string propertyName = binding.propertyName ?? string.Empty;
            return IsPositionProperty(propertyName) &&
                   (propertyName.EndsWith(".x", StringComparison.Ordinal) ||
                    propertyName.EndsWith(".z", StringComparison.Ordinal));
        }

        static bool IsRootTransformRotation(EditorCurveBinding binding, string rootPath, out float identityValue)
        {
            identityValue = 0f;
            if (binding.type != typeof(Transform) || !IsRootPath(binding.path, rootPath))
                return false;

            string propertyName = binding.propertyName ?? string.Empty;
            if (propertyName == "m_LocalRotation.w")
            {
                identityValue = 1f;
                return true;
            }

            if (propertyName == "m_LocalRotation.x" ||
                propertyName == "m_LocalRotation.y" ||
                propertyName == "m_LocalRotation.z")
                return true;

            return propertyName.IndexOf("EulerAngles", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static bool IsRootPath(string path, string rootPath)
        {
            string normalizedPath = NormalizePath(path);
            string normalizedRootPath = NormalizePath(rootPath);
            if (normalizedPath.Length == 0 || normalizedPath == "Root")
                return true;

            if (normalizedRootPath.Length == 0)
                return normalizedPath.IndexOf('/') < 0;

            if (string.Equals(normalizedPath, normalizedRootPath, StringComparison.Ordinal))
                return true;

            string rootLeaf = GetLeafName(normalizedRootPath);
            return normalizedPath.EndsWith("/" + rootLeaf, StringComparison.Ordinal) ||
                   string.Equals(normalizedPath, rootLeaf, StringComparison.Ordinal);
        }

        static bool IsPositionProperty(string propertyName)
        {
            return propertyName.IndexOf("Position", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static AnimationCurve ConstantCurve(float duration, float value)
        {
            return AnimationCurve.Constant(0f, duration, value);
        }

        static float FirstValueOrDefault(AnimationCurve source, float fallback)
        {
            return source != null && source.length > 0 ? source.Evaluate(0f) : fallback;
        }

        static AnimationCurve CopyCurve(AnimationCurve source)
        {
            if (source == null)
                return null;

            var output = new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode,
            };
            return output;
        }

        static string NormalizePath(string path)
        {
            return (path ?? string.Empty).Replace('\\', '/').Trim('/');
        }

        static string GetLeafName(string path)
        {
            int separator = path.LastIndexOf('/');
            return separator >= 0 ? path.Substring(separator + 1) : path;
        }

        static string NormalizeAssetPath(string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Output path is missing.", nameof(outputPath));

            string normalized = outputPath.Replace('\\', '/');
            if (!normalized.StartsWith("Assets/", StringComparison.Ordinal))
                throw new ArgumentException("Output path must be under Assets/.", nameof(outputPath));

            return normalized.EndsWith(".anim", StringComparison.OrdinalIgnoreCase)
                ? normalized
                : normalized + ".anim";
        }

        static void EnsureAssetFolder(string assetPath)
        {
            string folder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(folder) || AssetDatabase.IsValidFolder(folder))
                return;

            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);

                current = next;
            }
        }
    }
}
