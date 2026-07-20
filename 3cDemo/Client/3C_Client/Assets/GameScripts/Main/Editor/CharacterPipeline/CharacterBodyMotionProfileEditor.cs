using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [CustomEditor(typeof(CharacterBodyMotionProfile))]
    public sealed class CharacterBodyMotionProfileEditor : UnityEditor.Editor
    {
        SerializedProperty m_GravityAcceleration;
        SerializedProperty m_MaximumFallSpeed;

        void OnEnable()
        {
            m_GravityAcceleration = serializedObject.FindProperty("m_GravityAcceleration");
            m_MaximumFallSpeed = serializedObject.FindProperty("m_MaximumFallSpeed");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_GravityAcceleration, new GUIContent("Gravity Acceleration"));
            EditorGUILayout.PropertyField(m_MaximumFallSpeed, new GUIContent("Maximum Fall Speed"));
            serializedObject.ApplyModifiedProperties();

            var errors = new List<string>();
            if (((CharacterBodyMotionProfile)target).CollectConfigurationErrors(errors))
                return;
            for (int i = 0; i < errors.Count; i++)
                EditorGUILayout.HelpBox(errors[i], MessageType.Error);
        }
    }
}
