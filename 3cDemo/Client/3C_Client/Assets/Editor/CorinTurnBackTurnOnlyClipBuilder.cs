using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonAnimation.EditorTools
{
    public sealed class CorinTurnBackTurnOnlyClipBuilderWindow : EditorWindow
    {
        const string DefaultSourcePath = "Assets/Art/Animation/MyDemoNeed/Corin/WithWeaponRootmotion/Corin_TurnBack_WithWeaponRootmotion.anim";
        const string DefaultOutputPath = "Assets/Art/Animation/MyDemoNeed/Corin/WithWeaponRootmotion/Corin_TurnBack_TurnOnly_WithWeaponRootmotion.anim";
        const float DefaultEndTime = 0.47f;

        AnimationClip sourceClip;
        string outputPath = DefaultOutputPath;
        float endTime = DefaultEndTime;

        [MenuItem("Tools/3C/Corin/TurnBack TurnOnly Clip Builder")]
        public static void Open()
        {
            CorinTurnBackTurnOnlyClipBuilderWindow window = GetWindow<CorinTurnBackTurnOnlyClipBuilderWindow>("TurnBack TurnOnly");
            window.minSize = new Vector2(520f, 150f);
            window.ResolveDefaults();
            window.Show();
        }

        [MenuItem("Tools/3C/Corin/Build Default TurnBack TurnOnly Clip")]
        public static void BuildDefault()
        {
            AnimationClip source = AssetDatabase.LoadAssetAtPath<AnimationClip>(DefaultSourcePath);
            AnimationClip output = CorinTurnBackTurnOnlyClipBuilder.CreateOrUpdateClipAsset(
                source,
                DefaultOutputPath,
                DefaultEndTime);
            Selection.activeObject = output;
            EditorGUIUtility.PingObject(output);
            Debug.Log($"[CorinTurnBackTurnOnlyClipBuilder] Built {DefaultOutputPath} from {DefaultSourcePath} endTime={DefaultEndTime:F3}s.");
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
            endTime = EditorGUILayout.FloatField("End Time", Mathf.Max(0.01f, endTime));

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("RootT.x/y/z will be zeroed. Root-level Transform planar positions are zeroed. RootQ and skeleton curves are clipped to End Time.", MessageType.Info);

            using (new EditorGUI.DisabledScope(sourceClip == null || string.IsNullOrWhiteSpace(outputPath)))
            {
                if (GUILayout.Button("Build TurnOnly Clip"))
                {
                    AnimationClip output = CorinTurnBackTurnOnlyClipBuilder.CreateOrUpdateClipAsset(sourceClip, outputPath, endTime);
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
            if (endTime <= 0f)
                endTime = DefaultEndTime;
        }
    }

    public static class CorinTurnBackTurnOnlyClipBuilder
    {
        public static AnimationClip CreateOrUpdateClipAsset(AnimationClip source, string outputPath, float endTime)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            string normalizedPath = NormalizeAssetPath(outputPath);
            EnsureAssetFolder(normalizedPath);
            AnimationClip generated = BuildTurnOnlyClip(source, endTime, Path.GetFileNameWithoutExtension(normalizedPath));
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

        public static AnimationClip BuildTurnOnlyClip(AnimationClip source, float endTime, string clipName)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            float duration = Mathf.Clamp(endTime, 0.01f, Mathf.Max(0.01f, source.length));
            var output = new AnimationClip
            {
                name = string.IsNullOrWhiteSpace(clipName) ? source.name + "_TurnOnly" : clipName,
                frameRate = source.frameRate,
                wrapMode = source.wrapMode,
                legacy = source.legacy,
            };

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(source);
            settings.startTime = 0f;
            settings.stopTime = duration;
            settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(output, settings);
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(source))
            {
                AnimationCurve sourceCurve = AnimationUtility.GetEditorCurve(source, binding);
                AnimationCurve outputCurve = IsAnimatorRootT(binding)
                    ? AnimationCurve.Constant(0f, duration, 0f)
                    : IsRootTransformPlanarLocalPosition(binding)
                        ? AnimationCurve.Constant(0f, duration, 0f)
                        : ClipCurve(sourceCurve, duration);
                AnimationUtility.SetEditorCurve(output, binding, outputCurve);
            }

            foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
            {
                ObjectReferenceKeyframe[] sourceCurve = AnimationUtility.GetObjectReferenceCurve(source, binding);
                ObjectReferenceKeyframe[] outputCurve = ClipObjectReferenceCurve(sourceCurve, duration);
                if (outputCurve.Length > 0)
                    AnimationUtility.SetObjectReferenceCurve(output, binding, outputCurve);
            }

            AnimationUtility.SetAnimationEvents(output, ClipEvents(AnimationUtility.GetAnimationEvents(source), duration));
            output.EnsureQuaternionContinuity();
            return output;
        }

        static AnimationCurve ClipCurve(AnimationCurve source, float duration, float valueOffset = 0f)
        {
            if (source == null)
                return AnimationCurve.Constant(0f, duration, 0f);

            var output = new AnimationCurve();
            output.preWrapMode = source.preWrapMode;
            output.postWrapMode = source.postWrapMode;
            AddKey(output, 0f, source.Evaluate(0f) + valueOffset);

            Keyframe[] keys = source.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                if (key.time <= 0f || key.time >= duration)
                    continue;

                key.value += valueOffset;
                output.AddKey(key);
            }

            AddKey(output, duration, source.Evaluate(duration) + valueOffset);
            return output;
        }

        static void AddKey(AnimationCurve curve, float time, float value)
        {
            int index = curve.AddKey(new Keyframe(time, value));
            if (index >= 0)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.Linear);
            }
        }

        static ObjectReferenceKeyframe[] ClipObjectReferenceCurve(ObjectReferenceKeyframe[] source, float duration)
        {
            if (source == null || source.Length == 0)
                return Array.Empty<ObjectReferenceKeyframe>();

            var output = new System.Collections.Generic.List<ObjectReferenceKeyframe>(source.Length);
            for (int i = 0; i < source.Length; i++)
            {
                ObjectReferenceKeyframe key = source[i];
                if (key.time < 0f || key.time > duration)
                    continue;

                output.Add(key);
            }

            return output.ToArray();
        }

        static AnimationEvent[] ClipEvents(AnimationEvent[] source, float duration)
        {
            if (source == null || source.Length == 0)
                return Array.Empty<AnimationEvent>();

            var output = new System.Collections.Generic.List<AnimationEvent>(source.Length);
            for (int i = 0; i < source.Length; i++)
            {
                AnimationEvent animationEvent = source[i];
                if (animationEvent.time < 0f || animationEvent.time > duration)
                    continue;

                output.Add(animationEvent);
            }

            return output.ToArray();
        }

        static bool IsAnimatorRootT(EditorCurveBinding binding)
        {
            return binding.path == string.Empty &&
                   binding.type == typeof(Animator) &&
                   (binding.propertyName == "RootT.x" ||
                    binding.propertyName == "RootT.y" ||
                    binding.propertyName == "RootT.z");
        }

        static bool IsRootTransformPlanarLocalPosition(EditorCurveBinding binding)
        {
            return binding.type == typeof(Transform) &&
                   (binding.propertyName == "m_LocalPosition.x" ||
                    binding.propertyName == "m_LocalPosition.z") &&
                   (string.IsNullOrEmpty(binding.path) || binding.path.IndexOf('/') < 0);
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
