using System;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using UnityEditor;
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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RigCalibration"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SampleRate"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Thresholds"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Reduction"), true);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.TextField("Algorithm", CharacterFootPlacementAnalysisSource.AlgorithmVersion);
            serializedObject.ApplyModifiedProperties();
            DrawStatus();
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

    [CustomEditor(typeof(CharacterFootPlacementRig))]
    public sealed class CharacterFootPlacementRigEditor : UnityEditor.Editor
    {
        static readonly string[] BindingFields =
        {
            "m_VisualRoot", "m_Pelvis",
            "m_LeftHip", "m_LeftKnee", "m_LeftAnkle", "m_LeftToe",
            "m_RightHip", "m_RightKnee", "m_RightAnkle", "m_RightToe",
            "m_SelfColliderRoot"
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Calibration"));
            for (int i = 0; i < BindingFields.Length; i++)
                EditorGUILayout.PropertyField(serializedObject.FindProperty(BindingFields[i]));
            serializedObject.ApplyModifiedProperties();
            try
            {
                CharacterFootPlacementRig rig = (CharacterFootPlacementRig)target;
                CharacterFootPlacementRigBinding binding = rig.BuildBinding();
                EditorGUILayout.HelpBox(
                    $"Calibration {binding.CalibrationId} / {binding.CalibrationRevision}",
                    MessageType.Info);
            }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox(exception.Message, MessageType.Error);
            }
        }
    }
}
