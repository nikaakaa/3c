using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.RootMotion
{
    public sealed class RootMotionCurveBakerWindow : EditorWindow
    {
        enum SampleRateMode
        {
            FromClip,
            Fps60,
            Fps120
        }

        enum BakeMode
        {
            Unspecified = 0,
            FullLocalDelta = 1,
            ForwardDistanceYaw = 2
        }

        AnimationClip clip;
        GameObject sampleObject;
        DefaultAsset outputFolder;
        RootMotionCurveAsset targetAsset;
        SampleRateMode sampleRateMode = SampleRateMode.FromClip;
        BakeMode bakeMode;

        [MenuItem("Tools/3C/Animation/Root Motion Curve Baker")]
        public static void Open()
        {
            GetWindow<RootMotionCurveBakerWindow>("Root Motion Baker");
        }

        void OnGUI()
        {
            clip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", clip, typeof(AnimationClip), false);
            sampleObject = (GameObject)EditorGUILayout.ObjectField("Sample Object", sampleObject, typeof(GameObject), true);
            targetAsset = (RootMotionCurveAsset)EditorGUILayout.ObjectField("Target Asset", targetAsset, typeof(RootMotionCurveAsset), false);
            outputFolder = (DefaultAsset)EditorGUILayout.ObjectField("Output Folder", outputFolder, typeof(DefaultAsset), false);
            sampleRateMode = (SampleRateMode)EditorGUILayout.EnumPopup("Sample Rate", sampleRateMode);
            bakeMode = (BakeMode)EditorGUILayout.EnumPopup("Bake Mode", bakeMode);

            if (!TryGetEvaluationMode(out _, out string modeError))
                EditorGUILayout.HelpBox(modeError, MessageType.Error);

            using (new EditorGUI.DisabledScope(!CanBake()))
            {
                if (GUILayout.Button("Bake Root Motion Curve", GUILayout.Height(32)))
                    Bake();
            }
        }

        bool CanBake()
        {
            return clip != null &&
                sampleObject != null &&
                (targetAsset != null || outputFolder != null) &&
                TryGetEvaluationMode(out _, out _);
        }

        void Bake()
        {
            if (!ValidateInput(out string error))
            {
                EditorUtility.DisplayDialog("Root Motion Baker", error, "OK");
                return;
            }

            if (!TryGetEvaluationMode(out RootMotionCurveEvaluationMode evaluationMode, out error))
            {
                EditorUtility.DisplayDialog("Root Motion Baker", error, "OK");
                return;
            }

            RootMotionCurveAsset asset = targetAsset;
            string assetPath = targetAsset ? AssetDatabase.GetAssetPath(targetAsset) : BuildOutputAssetPath();
            if (!targetAsset)
            {
                if (AssetDatabase.LoadAssetAtPath<RootMotionCurveAsset>(assetPath))
                {
                    EditorUtility.DisplayDialog("Root Motion Baker", "目标资产已存在，请显式选择 Target Asset 覆盖。", "OK");
                    return;
                }

                asset = CreateInstance<RootMotionCurveAsset>();
            }

            if (!TryBakeCurves(
                    evaluationMode,
                    out AnimationCurve x,
                    out AnimationCurve y,
                    out AnimationCurve z,
                    out AnimationCurve forwardDistance,
                    out AnimationCurve yaw,
                    out Vector3 totalPosition,
                    out float totalForwardDistance,
                    out float totalYaw,
                    out float sampleRate,
                    out error))
            {
                if (!targetAsset && asset)
                    DestroyImmediate(asset);

                EditorUtility.DisplayDialog("Root Motion Baker", error, "OK");
                return;
            }

            Undo.RecordObject(asset, "Bake Root Motion Curve");
            asset.SetBakedData(
                clip,
                Mathf.Max(0f, clip.length),
                sampleRate,
                evaluationMode,
                x,
                y,
                z,
                forwardDistance,
                yaw,
                totalPosition,
                totalForwardDistance,
                totalYaw);

            if (!targetAsset)
                AssetDatabase.CreateAsset(asset, assetPath);

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = asset;
        }

        bool ValidateInput(out string error)
        {
            error = string.Empty;
            if (!clip)
            {
                error = "缺少 Animation Clip。";
                return false;
            }

            if (!sampleObject)
            {
                error = "缺少 Sample Object。";
                return false;
            }

            if (!sampleObject.GetComponent<Animator>())
            {
                error = "Sample Object 必须包含 Animator。";
                return false;
            }

            if (!targetAsset && !IsValidOutputFolder())
            {
                error = "请选择有效 Output Folder，或选择 Target Asset 覆盖。";
                return false;
            }

            if (!TryGetEvaluationMode(out _, out error))
                return false;

            return true;
        }

        bool IsValidOutputFolder()
        {
            if (!outputFolder)
                return false;

            string path = AssetDatabase.GetAssetPath(outputFolder);
            return AssetDatabase.IsValidFolder(path);
        }

        string BuildOutputAssetPath()
        {
            string folderPath = AssetDatabase.GetAssetPath(outputFolder);
            string safeName = string.IsNullOrEmpty(clip.name) ? "RootMotionCurve" : clip.name;
            return $"{folderPath}/{safeName}_RootMotionCurve.asset";
        }

        float ResolveSampleRate()
        {
            switch (sampleRateMode)
            {
                case SampleRateMode.Fps60:
                    return 60f;
                case SampleRateMode.Fps120:
                    return 120f;
                default:
                    return clip.frameRate > 0f ? clip.frameRate : 30f;
            }
        }

        bool TryBakeCurves(
            RootMotionCurveEvaluationMode evaluationMode,
            out AnimationCurve x,
            out AnimationCurve y,
            out AnimationCurve z,
            out AnimationCurve forwardDistance,
            out AnimationCurve yaw,
            out Vector3 totalPosition,
            out float totalForwardDistance,
            out float totalYaw,
            out float sampleRate,
            out string error)
        {
            x = new AnimationCurve();
            y = new AnimationCurve();
            z = new AnimationCurve();
            forwardDistance = new AnimationCurve();
            yaw = new AnimationCurve();
            totalPosition = Vector3.zero;
            totalForwardDistance = 0f;
            totalYaw = 0f;
            sampleRate = ResolveSampleRate();
            error = string.Empty;

            GameObject instance = null;
            try
            {
                instance = Instantiate(sampleObject, Vector3.zero, Quaternion.identity);
                instance.hideFlags = HideFlags.HideAndDontSave;

                Animator animator = instance.GetComponent<Animator>();
                if (!animator)
                {
                    error = "Sample Prefab 实例缺少 Animator。";
                    return false;
                }

                if (!animator.runtimeAnimatorController)
                {
                    error = "Animator 必须有 RuntimeAnimatorController。";
                    return false;
                }

                AnimatorOverrideController overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
                AnimationClip[] controllerClips = overrideController.animationClips;
                if (controllerClips == null || controllerClips.Length == 0)
                {
                    error = "RuntimeAnimatorController 中没有可替换动画。";
                    return false;
                }

                var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(controllerClips.Length);
                for (int i = 0; i < controllerClips.Length; i++)
                    overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(controllerClips[i], clip));

                overrideController.ApplyOverrides(overrides);
                animator.runtimeAnimatorController = overrideController;
                animator.applyRootMotion = true;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Update(0f);

                float duration = Mathf.Max(0f, clip.length);
                int frameCount = Mathf.Max(1, Mathf.CeilToInt(duration * sampleRate));
                float defaultDeltaTime = 1f / Mathf.Max(1f, sampleRate);

                AddKey(x, y, z, forwardDistance, yaw, 0f, totalPosition, totalForwardDistance, totalYaw);
                for (int i = 1; i <= frameCount; i++)
                {
                    float previousTime = Mathf.Min((i - 1) * defaultDeltaTime, duration);
                    float currentTime = Mathf.Min(i * defaultDeltaTime, duration);
                    float deltaTime = Mathf.Max(0f, currentTime - previousTime);
                    if (deltaTime <= 0f)
                        continue;

                    Quaternion previousRotation = instance.transform.rotation;
                    animator.Update(deltaTime);

                    Vector3 worldDelta = animator.deltaPosition;
                    Quaternion worldDeltaRotation = animator.deltaRotation;
                    Vector3 localDelta = Quaternion.Inverse(previousRotation) * worldDelta;
                    float deltaYaw = ExtractYaw(worldDeltaRotation);

                    if (evaluationMode == RootMotionCurveEvaluationMode.ForwardDistanceYaw)
                    {
                        totalForwardDistance += new Vector2(localDelta.x, localDelta.z).magnitude;
                        totalPosition = Vector3.forward * totalForwardDistance;
                    }
                    else
                    {
                        totalPosition += localDelta;
                    }

                    totalYaw += deltaYaw;
                    AddKey(x, y, z, forwardDistance, yaw, currentTime, totalPosition, totalForwardDistance, totalYaw);
                }

                return true;
            }
            finally
            {
                if (instance)
                    DestroyImmediate(instance);
            }
        }

        bool TryGetEvaluationMode(out RootMotionCurveEvaluationMode evaluationMode, out string error)
        {
            switch (bakeMode)
            {
                case BakeMode.FullLocalDelta:
                    evaluationMode = RootMotionCurveEvaluationMode.FullLocalDelta;
                    error = string.Empty;
                    return true;
                case BakeMode.ForwardDistanceYaw:
                    evaluationMode = RootMotionCurveEvaluationMode.ForwardDistanceYaw;
                    error = string.Empty;
                    return true;
                case BakeMode.Unspecified:
                    evaluationMode = RootMotionCurveEvaluationMode.Unspecified;
                    error = "请选择 Root Motion 曲线求值模式。";
                    return false;
                default:
                    evaluationMode = RootMotionCurveEvaluationMode.Unspecified;
                    error = $"Root Motion Baker 包含未知 Bake Mode 值：{(int)bakeMode}。";
                    return false;
            }
        }

        static void AddKey(
            AnimationCurve x,
            AnimationCurve y,
            AnimationCurve z,
            AnimationCurve forwardDistance,
            AnimationCurve yaw,
            float time,
            Vector3 position,
            float distance,
            float localYaw)
        {
            x.AddKey(time, position.x);
            y.AddKey(time, position.y);
            z.AddKey(time, position.z);
            forwardDistance.AddKey(time, distance);
            yaw.AddKey(time, localYaw);
        }

        static float ExtractYaw(Quaternion rotation)
        {
            Vector3 forward = rotation * Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0000001f)
                return 0f;

            return Vector3.SignedAngle(Vector3.forward, forward.normalized, Vector3.up);
        }
    }
}
