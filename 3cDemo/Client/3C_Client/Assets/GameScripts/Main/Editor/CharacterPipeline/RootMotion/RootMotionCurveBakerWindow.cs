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

            try
            {
                if (targetAsset)
                    Undo.RecordObject(asset, "Bake Root Motion Curve");
                RootMotionCurveBakingService.Bake(
                    clip,
                    sampleObject,
                    null,
                    asset,
                    ResolveSampleRate(),
                    evaluationMode);
            }
            catch (System.Exception exception)
            {
                if (!targetAsset && asset)
                    DestroyImmediate(asset);
                EditorUtility.DisplayDialog("Root Motion Baker", exception.Message, "OK");
                return;
            }

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

            if (!sampleObject.GetComponentInChildren<Animator>(true))
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

    }
}
