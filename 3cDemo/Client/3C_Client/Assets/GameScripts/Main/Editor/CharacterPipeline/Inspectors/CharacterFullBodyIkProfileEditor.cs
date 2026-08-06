using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [CustomEditor(typeof(CharacterFullBodyIkProfile))]
    public sealed class CharacterFullBodyIkProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ProfileId"));
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Schema"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Revision"));
                EditorGUILayout.TextField("Solver Backend", CharacterFullBodyIkProfile.SolverBackendIdentity);
                EditorGUILayout.TextField("Vendor Source", CharacterFullBodyIkProfile.AuditedVendorSourceRevision);
            }
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Solver", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Iterations"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FabrikPass"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SpineStiffness"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_PullBodyVertical"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_PullBodyHorizontal"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_NodeWeight"));
            EditorGUILayout.Space(6f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_LeftArm"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RightArm"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_LeftLeg"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RightLeg"), true);
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.HelpBox(
                "Editing solver settings changes the Profile revision immediately. Run explicit Character Build to publish the new Pose Plan.",
                MessageType.Info);
        }
    }
}
