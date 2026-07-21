using System;
using ThirdPersonGameplay.Lab;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonGameplay.Editor.Lab
{
    [CustomEditor(typeof(GameplayLabBootstrap))]
    internal sealed class GameplayLabBootstrapInspector : UnityEditor.Editor
    {
        SerializedProperty m_Variants;
        SerializedProperty m_StartupVariantIndex;

        void OnEnable()
        {
            m_Variants = serializedObject.FindProperty("m_Variants");
            m_StartupVariantIndex = serializedObject.FindProperty("m_StartupVariantIndex");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                EditorGUILayout.PropertyField(m_Variants, true);
                string[] labels = BuildLabels();
                if (labels.Length == 0)
                {
                    EditorGUILayout.HelpBox("Gameplay Lab requires explicit Session Variants.", MessageType.Error);
                }
                else
                {
                    int current = Mathf.Clamp(m_StartupVariantIndex.intValue, 0, labels.Length - 1);
                    m_StartupVariantIndex.intValue = EditorGUILayout.Popup("Startup Variant", current, labels);
                }
            }
            serializedObject.ApplyModifiedProperties();
        }

        string[] BuildLabels()
        {
            if (m_Variants == null || !m_Variants.isArray)
                return Array.Empty<string>();
            var labels = new string[m_Variants.arraySize];
            for (int i = 0; i < labels.Length; i++)
            {
                GameplayLabSessionVariantDefinition variant =
                    m_Variants.GetArrayElementAtIndex(i).objectReferenceValue as GameplayLabSessionVariantDefinition;
                labels[i] = variant ? variant.VariantId : $"Missing Variant [{i}]";
            }
            return labels;
        }
    }
}
