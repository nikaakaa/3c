using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BTSMTL.Timeline.Editor 
{
    public abstract class TimelineClipInspectorView : VisualElement
    {
        protected TimelineEditorView EditorView { get; private set; }

        public void Initialize(TimelineEditorView editorView)
        {
            EditorView = editorView;
        }
    }

    public sealed class MotionWarpClipInspectorView : TimelineClipInspectorView
    {
        readonly MotionWarpClip m_Warp;
        readonly List<MotionCurveClip> m_Sources = new List<MotionCurveClip>();
        readonly List<MotionWarpAuthoringIssue> m_Issues = new List<MotionWarpAuthoringIssue>();

        public MotionWarpClipInspectorView(Clip clip)
        {
            m_Warp = clip as MotionWarpClip ?? throw new ArgumentException("MotionWarp inspector requires MotionWarpClip.", nameof(clip));
            name = "motion-warp-inspector";
            Rebuild();
        }

        void Rebuild()
        {
            Clear();
            TimelineData timeline = m_Warp.Timeline;
            MotionWarpAuthoring.CollectMotionCurveSources(timeline, m_Sources);
            var labels = new List<string> { "None" };
            int selectedIndex = 0;
            for (int i = 0; i < m_Sources.Count; i++)
            {
                MotionCurveClip source = m_Sources[i];
                labels.Add($"{source.CurveId} [{source.StartFrame}..{source.CurveEndFrame}] ({ShortId(source.AuthoringId)})");
                if (string.Equals(source.AuthoringId, m_Warp.SourceMotionClipId, StringComparison.Ordinal))
                    selectedIndex = i + 1;
            }

            var sourceField = new PopupField<string>("Source Motion Curve", labels, selectedIndex);
            sourceField.RegisterValueChangedCallback(evt =>
            {
                int index = labels.IndexOf(evt.newValue);
                timeline.ApplyModify(() =>
                {
                    if (index <= 0)
                        MotionWarpAuthoring.ClearSource(timeline, m_Warp);
                    else
                        MotionWarpAuthoring.BindSource(timeline, m_Warp, m_Sources[index - 1]);
                }, "Configure MotionWarp Source");
                m_Warp.RepaintInspector();
            });
            Add(sourceField);

            if (!string.IsNullOrEmpty(m_Warp.SourceMotionClipId) && selectedIndex == 0)
            {
                Add(new HelpBox(
                    $"Missing MotionCurve source: {m_Warp.SourceMotionClipId}",
                    HelpBoxMessageType.Error));
            }

            m_Issues.Clear();
            MotionWarpAuthoring.ValidateClip(timeline, m_Warp, m_Issues);
            for (int i = 0; i < m_Issues.Count; i++)
                Add(new HelpBox(m_Issues[i].Message, HelpBoxMessageType.Error));
        }

        static string ShortId(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= 8)
                return value ?? string.Empty;
            return value.Substring(0, 8);
        }
    }
}
