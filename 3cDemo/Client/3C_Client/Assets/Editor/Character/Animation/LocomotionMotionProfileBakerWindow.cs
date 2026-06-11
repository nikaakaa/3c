using ThirdPersonAnimation;
using ThirdPersonMovement;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonAnimation.EditorTools
{
    public sealed class LocomotionMotionProfileBakerWindow : EditorWindow
    {
        const string DefaultOutputPath = "Assets/Configs/3C/Locomotion/DefaultRunEndMotionProfile.asset";

        GameObject targetPrefab;
        AnimationClip animationClip;
        BasicMovementPhase phase = BasicMovementPhase.MoveStop;
        BasicMovementGait gait = BasicMovementGait.Run;
        string aliasKey = "RunEnd";
        string motionRootPath = "Bip001";
        int sampleRate = 60;
        string outputPath = DefaultOutputPath;
        LocomotionMotionProfileSO outputProfile;

        [MenuItem("Tools/3C/Animation/Locomotion Motion Profile Baker")]
        public static void Open()
        {
            GetWindow<LocomotionMotionProfileBakerWindow>("Motion Profile Baker");
        }

        void OnGUI()
        {
            targetPrefab = (GameObject)EditorGUILayout.ObjectField("Target Prefab", targetPrefab, typeof(GameObject), false);
            animationClip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", animationClip, typeof(AnimationClip), false);
            phase = (BasicMovementPhase)EditorGUILayout.EnumPopup("Phase", phase);
            gait = (BasicMovementGait)EditorGUILayout.EnumPopup("Gait", gait);
            aliasKey = EditorGUILayout.TextField("Alias Key", aliasKey);
            motionRootPath = EditorGUILayout.TextField("Motion Root Path", motionRootPath);
            sampleRate = EditorGUILayout.IntSlider("Sample Rate", sampleRate, 15, 120);
            outputProfile = (LocomotionMotionProfileSO)EditorGUILayout.ObjectField("Existing Profile", outputProfile, typeof(LocomotionMotionProfileSO), false);

            using (new EditorGUI.DisabledScope(outputProfile != null))
            {
                outputPath = EditorGUILayout.TextField("Output Path", outputPath);
            }

            using (new EditorGUI.DisabledScope(!CanBake()))
            {
                if (GUILayout.Button("Bake Profile"))
                    Bake();
            }
        }

        bool CanBake()
        {
            return targetPrefab != null &&
                   animationClip != null &&
                   !string.IsNullOrWhiteSpace(aliasKey) &&
                   (outputProfile != null || !string.IsNullOrWhiteSpace(outputPath));
        }

        void Bake()
        {
            LocomotionMotionProfileBakeRequest request = new LocomotionMotionProfileBakeRequest(
                targetPrefab,
                animationClip,
                phase,
                gait,
                aliasKey,
                motionRootPath,
                sampleRate);

            if (outputProfile != null)
            {
                LocomotionMotionProfileBakeUtility.BakeIntoProfile(outputProfile, in request);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Selection.activeObject = outputProfile;
                return;
            }

            outputProfile = LocomotionMotionProfileBakeUtility.CreateOrUpdateProfileAsset(outputPath, in request);
            Selection.activeObject = outputProfile;
        }
    }
}
