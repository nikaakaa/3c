using System;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [CustomEditor(typeof(CharacterPresentationPoseSourceBinding), true)]
    public sealed class CharacterPresentationPoseSourceBindingEditor : UnityEditor.Editor
    {
        CharacterAnimationPresentationProfile m_Profile;
        CharacterPresentationPoseSourceBinding m_Binding;
        string m_Error = string.Empty;

        void OnEnable()
        {
            m_Binding = target as CharacterPresentationPoseSourceBinding;
            string path = m_Binding ? AssetDatabase.GetAssetPath(m_Binding) : string.Empty;
            m_Profile = string.IsNullOrEmpty(path)
                ? null
                : AssetDatabase.LoadMainAssetAtPath(path) as CharacterAnimationPresentationProfile;
        }

        public override void OnInspectorGUI()
        {
            if (!m_Binding || !m_Profile || !m_Binding.Slot)
            {
                EditorGUILayout.HelpBox("Pose Source binding owner is unavailable.", MessageType.Error);
                return;
            }
            EditorGUILayout.LabelField("Pose Source Binding", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Profile", m_Profile, typeof(CharacterAnimationPresentationProfile), false);
                EditorGUILayout.ObjectField("Source Slot", m_Binding.Slot, m_Binding.Slot.GetType(), false);
                EditorGUILayout.EnumPopup("Source Kind", m_Binding.SourceKind);
                EditorGUILayout.ObjectField("Resource", m_Binding.SourceAsset, typeof(UnityEngine.Object), false);
                EditorGUILayout.ObjectField("Rig", m_Binding.Rig, typeof(CharacterAnimationRigDefinition), false);
            }
            if (m_Binding is CharacterClipPoseSourceBinding clipBinding)
            {
                EditorGUILayout.LabelField("Duration", clipBinding.Clip ? $"{clipBinding.Clip.length:0.###} s" : "Unavailable");
                EditorGUILayout.LabelField("Loop", clipBinding.Clip && clipBinding.Clip.isLooping ? "Yes" : "No");
            }
            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!m_Binding.SourceAsset))
            {
                if (GUILayout.Button("Ping Resource"))
                    EditorGUIUtility.PingObject(m_Binding.SourceAsset);
            }
            if (GUILayout.Button("Open Profile"))
            {
                Selection.activeObject = m_Profile;
                EditorGUIUtility.PingObject(m_Profile);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("在Profile或Pose Graph提供精确Definition与场景Preview Target后，用Open Animation Clip进入Unity Animation Window。", MessageType.Info);
        }
    }
}
