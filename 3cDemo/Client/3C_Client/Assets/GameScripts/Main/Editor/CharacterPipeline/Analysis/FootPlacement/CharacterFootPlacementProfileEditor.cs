using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [CustomEditor(typeof(CharacterFootPlacementProfile))]
    public sealed class CharacterFootPlacementProfileEditor : UnityEditor.Editor
    {
        static readonly string[] SectionFields =
        {
            "m_Trace",
            "m_Contact",
            "m_Prediction",
            "m_Constraint",
            "m_Limb",
            "m_Pelvis",
            "m_Rotation",
            "m_Smoothing"
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            for (int i = 0; i < SectionFields.Length; i++)
            {
                SerializedProperty property = serializedObject.FindProperty(SectionFields[i]);
                EditorGUILayout.PropertyField(property, true);
                if (i < SectionFields.Length - 1)
                    EditorGUILayout.Space(3f);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
