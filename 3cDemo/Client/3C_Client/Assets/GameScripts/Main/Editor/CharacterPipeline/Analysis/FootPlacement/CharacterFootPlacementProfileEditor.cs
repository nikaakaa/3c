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
            "m_ProfileId",
            "m_Revision",
            "m_LyraCurrentGrounding",
            "m_StanceStabilization",
            "m_PredictiveExtension"
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
