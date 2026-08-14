using System;
using BTSMTL.Timeline.Editor;
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
            if (m_Binding is CharacterSequencePoseSourceBinding sequence)
            {
                EditorGUILayout.LabelField("Duration", sequence.Clip ? $"{sequence.Clip.length:0.###} s" : "Unavailable");
                EditorGUILayout.LabelField("Loop", sequence.Loop ? "Yes" : "No");
                EditorGUILayout.LabelField("Sync Group", string.IsNullOrEmpty(sequence.Sequence.SyncGroupId) ? "None" : sequence.Sequence.SyncGroupId);
                EditorGUILayout.LabelField("Sync Role", sequence.Sequence.SyncRole.ToString());
                EditorGUILayout.LabelField("Time Mapping", sequence.Sequence.TimeMapping.ToString());
                EditorGUILayout.LabelField("Markers", sequence.Sequence.SyncMarkers.Count.ToString());
                EditorGUILayout.LabelField("Material Curves", sequence.Sequence.CurveChannels.Count.ToString());
            }
            EditorGUILayout.Space(6f);
            if (GUILayout.Button("Open Sequence", GUILayout.Height(28f)))
                OpenEditor();
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
            if (!string.IsNullOrEmpty(m_Error))
                EditorGUILayout.HelpBox(m_Error, MessageType.Error);
            EditorGUILayout.HelpBox("素材 Marker、Curve、Notify 与 Analysis 只在主 Timeline Editor 的 Sequence 文档中编辑。", MessageType.Info);
        }

        void OpenEditor()
        {
            try
            {
                if (m_Binding is not CharacterSequencePoseSourceBinding sequence || !sequence.Sequence)
                    throw new InvalidOperationException("Pose Source Binding does not reference a Sequence document.");
                TimelineEditorWindow.Open(sequence.Sequence);
                m_Error = string.Empty;
            }
            catch (Exception exception)
            {
                m_Error = exception.Message;
            }
        }
    }
}
