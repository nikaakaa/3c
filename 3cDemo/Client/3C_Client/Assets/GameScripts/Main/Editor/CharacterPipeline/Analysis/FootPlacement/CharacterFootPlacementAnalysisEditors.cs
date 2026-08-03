using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [CustomEditor(typeof(CharacterFootPlacementAnalysisSource))]
    public sealed class CharacterFootPlacementAnalysisSourceEditor : UnityEditor.Editor
    {
        SerializedProperty m_SamplingRigGuid;

        void OnEnable()
        {
            m_SamplingRigGuid = serializedObject.FindProperty("m_SamplingRigAssetGuid");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_AnalysisSourceId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_AnalysisVersion"));
            DrawSamplingRig();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RigDefinition"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RigCalibration"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_CalibrationPreviewClip"));
            EditorGUILayout.Slider(
                serializedObject.FindProperty("m_CalibrationPreviewNormalizedTime"),
                0f,
                1f,
                "Calibration Preview Time");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SampleRate"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Thresholds"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Reduction"), true);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.TextField("Algorithm", CharacterFootPlacementAnalysisSource.AlgorithmVersion);
            serializedObject.ApplyModifiedProperties();
            DrawStatus();
            CharacterFootPlacementAnalysisSource source = (CharacterFootPlacementAnalysisSource)target;
            EditorGUILayout.HelpBox(
                CharacterFootPlacementRigCalibrationAuthoringSession.GetLastValidation(source.RigCalibration),
                MessageType.None);
            if (GUILayout.Button("Edit Rig Calibration In Sampling Rig"))
                CharacterFootPlacementRigCalibrationAuthoringSession.Open(source);
        }

        void DrawSamplingRig()
        {
            string guid = m_SamplingRigGuid.stringValue;
            GameObject current = CharacterFootPlacementAnalysisSource.IsAssetGuid(guid)
                ? AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid))
                : null;
            GameObject next = EditorGUILayout.ObjectField("Sampling Rig Prefab", current, typeof(GameObject), false) as GameObject;
            if (next == current)
                return;
            if (next && PrefabUtility.GetPrefabAssetType(next) == PrefabAssetType.NotAPrefab)
            {
                EditorUtility.DisplayDialog("Invalid Sampling Rig", "Sampling Rig must be a persisted Prefab asset.", "OK");
                return;
            }
            m_SamplingRigGuid.stringValue = next
                ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(next))
                : string.Empty;
        }

        void DrawStatus()
        {
            try
            {
                ((CharacterFootPlacementAnalysisSource)target).RequireValid();
                EditorGUILayout.HelpBox("Analysis Source is valid.", MessageType.Info);
            }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox(exception.Message, MessageType.Error);
            }
        }
    }

    [CustomEditor(typeof(CharacterWorldAwarePresentationBinding))]
    public sealed class CharacterWorldAwarePresentationBindingEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_PresentationRoot"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SelfColliderRoot"));
            serializedObject.ApplyModifiedProperties();
            CharacterWorldAwarePresentationBinding binding = (CharacterWorldAwarePresentationBinding)target;
            if (CharacterFootPlacementRigCalibrationAuthoringSession.IsEditing(binding))
            {
                CharacterFootPlacementRigCalibrationAuthoringSession.DrawInspector();
                return;
            }
            try
            {
                binding.RequireValid();
                EditorGUILayout.HelpBox("World-Aware Presentation Binding is valid.", MessageType.Info);
            }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox(exception.Message, MessageType.Error);
            }
            CharacterFootPlacementAnalysisSource source = FindAnalysisSource(binding);
            if (!source)
            {
                EditorGUILayout.HelpBox(
                    "This Prefab Stage has no exact Foot Placement Analysis Source for its asset GUID.",
                    MessageType.Error);
                using (new EditorGUI.DisabledScope(true))
                    GUILayout.Button("Open Foot Placement Calibration");
                return;
            }
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Foot Placement Source", source, typeof(CharacterFootPlacementAnalysisSource), false);
            if (GUILayout.Button("Open Foot Placement Calibration"))
                CharacterFootPlacementRigCalibrationAuthoringSession.Open(source);
        }

        static CharacterFootPlacementAnalysisSource FindAnalysisSource(CharacterWorldAwarePresentationBinding binding)
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null || !stage.prefabContentsRoot ||
                !binding.transform.IsChildOf(stage.prefabContentsRoot.transform))
                return null;
            string samplingRigGuid = AssetDatabase.AssetPathToGUID(stage.assetPath);
            if (string.IsNullOrEmpty(samplingRigGuid))
                return null;
            string[] guids = AssetDatabase.FindAssets("t:CharacterFootPlacementAnalysisSource");
            for (int i = 0; i < guids.Length; i++)
            {
                CharacterFootPlacementAnalysisSource source = AssetDatabase.LoadAssetAtPath<CharacterFootPlacementAnalysisSource>(
                    AssetDatabase.GUIDToAssetPath(guids[i]));
                if (source && string.Equals(source.SamplingRigAssetGuid, samplingRigGuid, StringComparison.Ordinal))
                    return source;
            }
            return null;
        }
    }
}
