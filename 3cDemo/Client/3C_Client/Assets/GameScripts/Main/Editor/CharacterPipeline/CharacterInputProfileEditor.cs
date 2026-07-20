using ThirdPersonCharacter.Pipeline.Input;
using System.Collections.Generic;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [CustomEditor(typeof(CharacterInputProfile))]
    public sealed class CharacterInputProfileEditor : UnityEditor.Editor
    {
        readonly List<string> m_Errors = new List<string>();

        SerializedProperty m_SourceAsset;
        SerializedProperty m_InputValues;
        SerializedProperty m_ActionRequests;

        void OnEnable()
        {
            m_SourceAsset = serializedObject.FindProperty("m_SourceAsset");
            m_InputValues = serializedObject.FindProperty("m_InputValues");
            m_ActionRequests = serializedObject.FindProperty("m_ActionRequests");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_SourceAsset);
            EditorGUILayout.PropertyField(m_InputValues, true);
            EditorGUILayout.PropertyField(m_ActionRequests, true);
            serializedObject.ApplyModifiedProperties();
            DrawConfigurationErrors();
        }

        void DrawConfigurationErrors()
        {
            m_Errors.Clear();
            CharacterInputProfile profile = target as CharacterInputProfile;
            if (profile != null && profile.CollectConfigurationErrors(m_Errors))
            {
                EditorGUILayout.HelpBox("Configuration is valid.", MessageType.Info);
                return;
            }

            for (int i = 0; i < m_Errors.Count; i++)
                EditorGUILayout.HelpBox(m_Errors[i], MessageType.Error);
        }
    }
}
