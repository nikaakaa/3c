#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BTSMTL.Timeline.Editor
{
    sealed class TimelineClipCreationRequest
    {
        public string TrackAuthoringId;
        public string Kind;
        public int StartFrame;
        public int EndFrame;
        public int CurveEndFrame;
        public UnityEngine.Object Resource;
        public string CurveId = "MotionCurve";
        public TimelineMotionContributionSpace Space = TimelineMotionContributionSpace.Local;
        public TimelineMotionChannel Channel = TimelineMotionChannel.Action;
        public TimelineMotionBlendMode BlendMode = TimelineMotionBlendMode.Override;
        public int Priority = 100;
        public bool ConsumeLowerChannels = true;
        public string SourceMotionClipId;
        public string CueId = "Cue";
        public string CueType = "Cue";
    }

    sealed class TimelineClipCreationPopup : PopupWindowContent
    {
        readonly TimelineClipCreationRequest m_Request;
        readonly IReadOnlyList<string> m_MotionClipIds;
        readonly Action<TimelineClipCreationRequest> m_Create;
        string m_Error;

        public TimelineClipCreationPopup(
            TimelineClipCreationRequest request,
            IReadOnlyList<string> motionClipIds,
            Action<TimelineClipCreationRequest> create)
        {
            m_Request = request;
            m_MotionClipIds = motionClipIds ?? Array.Empty<string>();
            m_Create = create;
        }

        public override Vector2 GetWindowSize() => new Vector2(360, 320);

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.LabelField("Add Clip", EditorStyles.boldLabel);
            m_Request.StartFrame = EditorGUILayout.IntField("Start Frame", m_Request.StartFrame);
            m_Request.EndFrame = EditorGUILayout.IntField("End Frame", m_Request.EndFrame);

            if (m_Request.Kind == TimelineContractKinds.AnimationClip)
                m_Request.Resource = EditorGUILayout.ObjectField("Animation Clip", m_Request.Resource, typeof(UnityEngine.AnimationClip), false);
            else if (m_Request.Kind == TimelineContractKinds.TreeClip)
                m_Request.Resource = EditorGUILayout.ObjectField("Graph / Tree", m_Request.Resource, typeof(UnityEngine.Object), false);

            if (m_Request.Kind == TimelineContractKinds.MotionCurveClip)
            {
                m_Request.CurveId = EditorGUILayout.TextField("Curve Id", m_Request.CurveId);
                m_Request.CurveEndFrame = EditorGUILayout.IntField("Curve End Frame", m_Request.CurveEndFrame);
                m_Request.Space = (TimelineMotionContributionSpace)EditorGUILayout.EnumPopup("Space", m_Request.Space);
                m_Request.Channel = (TimelineMotionChannel)EditorGUILayout.EnumPopup("Channel", m_Request.Channel);
                m_Request.BlendMode = (TimelineMotionBlendMode)EditorGUILayout.EnumPopup("Blend Mode", m_Request.BlendMode);
                m_Request.Priority = EditorGUILayout.IntField("Priority", m_Request.Priority);
                m_Request.ConsumeLowerChannels = EditorGUILayout.Toggle("Consume Lower", m_Request.ConsumeLowerChannels);
            }
            else if (m_Request.Kind == TimelineContractKinds.MotionWarpClip)
            {
                var labels = new string[m_MotionClipIds.Count + 1];
                labels[0] = "Select source";
                int selected = 0;
                for (int index = 0; index < m_MotionClipIds.Count; index++)
                {
                    labels[index + 1] = m_MotionClipIds[index];
                    if (string.Equals(m_Request.SourceMotionClipId, m_MotionClipIds[index], StringComparison.Ordinal))
                        selected = index + 1;
                }
                int next = EditorGUILayout.Popup("Source Motion", selected, labels);
                m_Request.SourceMotionClipId = next > 0 ? m_MotionClipIds[next - 1] : string.Empty;
            }
            else if (m_Request.Kind == TimelineContractKinds.ActionCueClip)
            {
                m_Request.CueId = EditorGUILayout.TextField("Cue Id", m_Request.CueId);
                m_Request.CueType = EditorGUILayout.TextField("Cue Type", m_Request.CueType);
            }

            m_Error = Validate();
            if (!string.IsNullOrEmpty(m_Error))
                EditorGUILayout.HelpBox(m_Error, MessageType.Error);
            using (new EditorGUI.DisabledScope(!string.IsNullOrEmpty(m_Error)))
            {
                if (GUILayout.Button("Create"))
                {
                    m_Create(m_Request);
                    editorWindow.Close();
                }
            }
        }

        string Validate()
        {
            if (m_Request.StartFrame < 0 || m_Request.EndFrame <= m_Request.StartFrame)
                return "帧范围必须满足 Start < End。";
            if (m_Request.Kind == TimelineContractKinds.AnimationClip && m_Request.Resource is not UnityEngine.AnimationClip)
                return "必须选择已有 AnimationClip。";
            if (m_Request.Kind == TimelineContractKinds.TreeClip &&
                m_Request.Resource is not ScriptableObject)
                return "必须选择正式 Graph / Tree 来源。";
            if (m_Request.Kind == TimelineContractKinds.MotionCurveClip &&
                (m_Request.CurveEndFrame <= m_Request.StartFrame || m_Request.CurveEndFrame > m_Request.EndFrame))
                return "Curve End Frame 必须位于 Clip 范围内。";
            if (m_Request.Kind == TimelineContractKinds.MotionWarpClip && string.IsNullOrEmpty(m_Request.SourceMotionClipId))
                return "MotionWarp 必须选择已有 MotionCurve 来源。";
            if (m_Request.Kind == TimelineContractKinds.ActionCueClip &&
                (string.IsNullOrWhiteSpace(m_Request.CueId) || string.IsNullOrWhiteSpace(m_Request.CueType)))
                return "Cue Id 和 Cue Type 必须填写。";
            return string.Empty;
        }
    }
}
#endif
