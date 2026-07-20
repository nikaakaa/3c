using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.ActionSystem.Editor
{
    [CustomEditor(typeof(ActionProfile))]
    public sealed class ActionProfileEditor : UnityEditor.Editor
    {
        readonly List<string> m_Errors = new List<string>();

        SerializedProperty m_ActionId;
        SerializedProperty m_DisplayName;
        SerializedProperty m_DebugCategory;
        SerializedProperty m_Tags;
        SerializedProperty m_BlockTags;
        SerializedProperty m_CancelTags;
        SerializedProperty m_TargetRequirement;

        void OnEnable()
        {
            m_ActionId = serializedObject.FindProperty("m_ActionId");
            m_DisplayName = serializedObject.FindProperty("m_DisplayName");
            m_DebugCategory = serializedObject.FindProperty("m_DebugCategory");
            m_Tags = serializedObject.FindProperty("m_Tags");
            m_BlockTags = serializedObject.FindProperty("m_BlockTags");
            m_CancelTags = serializedObject.FindProperty("m_CancelTags");
            m_TargetRequirement = serializedObject.FindProperty("m_TargetRequirement");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawHeader("Identity");
            EditorGUILayout.PropertyField(m_ActionId);
            EditorGUILayout.PropertyField(m_DisplayName);
            EditorGUILayout.PropertyField(m_DebugCategory);
            EditorGUILayout.PropertyField(m_TargetRequirement);

            DrawHeader("Gameplay Tags");
            EditorGUILayout.PropertyField(m_Tags, true);
            EditorGUILayout.PropertyField(m_BlockTags, true);
            EditorGUILayout.PropertyField(m_CancelTags, true);
            serializedObject.ApplyModifiedProperties();

            DrawHeader("Configuration");
            m_Errors.Clear();
            ActionProfile profile = target as ActionProfile;
            if (profile && profile.CollectConfigurationErrors(m_Errors))
            {
                EditorGUILayout.HelpBox("Configuration is valid.", MessageType.Info);
                return;
            }

            for (int i = 0; i < m_Errors.Count; i++)
                EditorGUILayout.HelpBox(m_Errors[i], MessageType.Error);
        }

        static void DrawHeader(string label)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        }
    }
}
