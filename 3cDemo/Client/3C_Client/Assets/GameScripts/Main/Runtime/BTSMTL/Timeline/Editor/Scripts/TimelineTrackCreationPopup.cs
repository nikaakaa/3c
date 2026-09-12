#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace BTSMTL.Timeline.Editor
{
    sealed class TimelineTrackCreationPopup : PopupWindowContent
    {
        readonly string m_Kind;
        readonly bool m_RequiresFields;
        readonly Action<string, string, string> m_Create;
        string m_Name;
        string m_ChannelId;
        string m_SlotId;
        string m_Error;

        public TimelineTrackCreationPopup(
            string kind,
            string displayName,
            bool requiresFields,
            Action<string, string, string> create)
        {
            m_Kind = kind;
            m_RequiresFields = requiresFields;
            m_Create = create;
            m_Name = displayName;
        }

        public override Vector2 GetWindowSize() => new Vector2(340, m_RequiresFields ? 156 : 96);

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.LabelField("Add Track", EditorStyles.boldLabel);
            m_Name = EditorGUILayout.TextField("Name", m_Name);
            if (m_RequiresFields && m_Kind == TimelineContractKinds.AnimationTrack)
            {
                m_ChannelId = EditorGUILayout.TextField("Animation Channel", m_ChannelId);
                m_SlotId = EditorGUILayout.TextField("Animation Slot", m_SlotId);
                m_Error = TimelineAuthoringTrackBinding.Validate(m_Kind, m_ChannelId, m_SlotId).Count > 0
                    ? "Animation Channel 和 Animation Slot 必须填写正式 identity."
                    : string.Empty;
            }
            if (!string.IsNullOrEmpty(m_Error))
                EditorGUILayout.HelpBox(m_Error, MessageType.Error);
            using (new EditorGUI.DisabledScope(!string.IsNullOrEmpty(m_Error)))
            {
                if (GUILayout.Button("Create"))
                {
                    m_Create(m_Name, m_ChannelId, m_SlotId);
                    editorWindow.Close();
                }
            }
        }
    }
}
#endif
