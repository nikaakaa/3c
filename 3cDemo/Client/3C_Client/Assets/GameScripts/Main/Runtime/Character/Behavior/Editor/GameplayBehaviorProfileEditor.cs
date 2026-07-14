using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Behavior.Editor
{
    [CustomEditor(typeof(GameplayBehaviorProfile))]
    public sealed class GameplayBehaviorProfileEditor : UnityEditor.Editor
    {
        readonly List<string> m_Errors = new List<string>();

        SerializedProperty m_BehaviorId;
        SerializedProperty m_BehaviorKind;
        SerializedProperty m_DisplayName;
        SerializedProperty m_DebugCategory;
        SerializedProperty m_Tags;

        void OnEnable()
        {
            m_BehaviorId = serializedObject.FindProperty("m_BehaviorId");
            m_BehaviorKind = serializedObject.FindProperty("m_BehaviorKind");
            m_DisplayName = serializedObject.FindProperty("m_DisplayName");
            m_DebugCategory = serializedObject.FindProperty("m_DebugCategory");
            m_Tags = serializedObject.FindProperty("m_Tags");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawHeader("Identity");
            EditorGUILayout.PropertyField(m_BehaviorId);
            EditorGUILayout.PropertyField(m_BehaviorKind);
            EditorGUILayout.PropertyField(m_DisplayName);
            EditorGUILayout.PropertyField(m_DebugCategory);
            EditorGUILayout.PropertyField(m_Tags, true);
            serializedObject.ApplyModifiedProperties();

            DrawHeader("Configuration");
            m_Errors.Clear();
            GameplayBehaviorProfile profile = target as GameplayBehaviorProfile;
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
