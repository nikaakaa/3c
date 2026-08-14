using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BTSMTL.Timeline.Editor
{
    internal sealed class AnimationTimeDocumentTrackView : VisualElement
    {
        readonly TimelineFieldView m_Field;
        readonly IAnimationTimeDocumentAdapter m_Document;

        public AnimationTimeDocumentTrackView(
            TimelineFieldView field,
            IAnimationTimeDocumentAdapter document)
        {
            m_Field = field ?? throw new ArgumentNullException(nameof(field));
            m_Document = document ?? throw new ArgumentNullException(nameof(document));
            VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>("VisualTree/TimelineTrackView");
            if (!visualTree)
                throw new InvalidOperationException("TimelineTrackView visual tree is unavailable.");
            visualTree.CloneTree(this);
            StyleSheet styleSheet = Resources.Load<StyleSheet>("StyleSheet/TimelineTrackView");
            if (!styleSheet)
                throw new InvalidOperationException("TimelineTrackView style sheet is unavailable.");
            styleSheets.Add(styleSheet);
            AddToClassList("timelineTrack");
            style.height = AnimationTimeDocumentLayout.ContentHeight(document);
            Build();
        }

        void Build()
        {
            AnimationTimeLaneDescriptor span = FindLane(AnimationTimeLaneKind.Span, 0);
            if (span != null)
                Add(new AnimationTimeSpanView(m_Field, span.Span));
            AddPointLane(FindLane(AnimationTimeLaneKind.Point, 0), TimelineTrackLayout.ClipRowHeight);
            AddPointLane(
                FindLane(AnimationTimeLaneKind.Point, 1),
                TimelineTrackLayout.ClipRowHeight + AnimationTimeDocumentLayout.PointLaneHeight);
            AddCurveLanes();
        }

        void AddPointLane(AnimationTimeLaneDescriptor lane, float top)
        {
            var container = new VisualElement();
            container.AddToClassList("animationMarkerSyncLane");
            container.style.top = top;
            container.style.height = AnimationTimeDocumentLayout.PointLaneHeight;
            if (lane != null && lane.Points.Count > 0)
                container.AddToClassList("animationMarkerSyncLane--enabled");
            if (lane != null)
            {
                for (int i = 0; i < lane.Points.Count; i++)
                {
                    var point = new AnimationTimePointView(m_Field, lane.Identity, lane.Points[i])
                    {
                        SelectionContainer = m_Field
                    };
                    point.style.top = 3f;
                    point.style.height = 22f;
                    m_Field.RegisterSelectable(point);
                    container.Add(point);
                }
            }
            Add(container);
        }

        void AddCurveLanes()
        {
            int curveCount = AnimationTimeDocumentLayout.CurveCount(m_Document);
            if (curveCount == 0)
                return;
            var header = new VisualElement { name = "animation-curves-header" };
            header.AddToClassList("animationCurvesHeader");
            header.style.top = AnimationTimeDocumentLayout.CurveHeaderTop;
            header.pickingMode = PickingMode.Position;
            var fold = new Label(AnimationTimeEditorSession.CurvesExpanded(m_Document) ? "v" : ">");
            fold.AddToClassList("animationCurvesHeaderFold");
            fold.pickingMode = PickingMode.Ignore;
            var title = new Label("CURVES");
            title.AddToClassList("animationCurvesHeaderLabel");
            title.pickingMode = PickingMode.Ignore;
            int visibleCount = VisibleCurveCount();
            var range = new Label($"{visibleCount}/{curveCount}");
            range.AddToClassList("animationCurvesHeaderRange");
            range.pickingMode = PickingMode.Ignore;
            header.Add(fold);
            header.Add(title);
            header.Add(range);
            header.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0)
                    return;
                AnimationTimeEditorSession.ToggleCurves(m_Document);
                m_Field.schedule.Execute(m_Field.PopulateView);
                evt.StopImmediatePropagation();
            });
            Add(header);
            if (!AnimationTimeEditorSession.CurvesExpanded(m_Document))
                return;
            int visibleIndex = 0;
            for (int i = 0; i < m_Document.Lanes.Count; i++)
            {
                AnimationTimeLaneDescriptor lane = m_Document.Lanes[i];
                if (lane.Kind != AnimationTimeLaneKind.Curve ||
                    !AnimationTimeEditorSession.IsChannelVisible(m_Document, lane.Identity))
                    continue;
                Add(new AnimationCurveChannelLaneView(
                    new AnimationSequenceCurveLaneBinding(m_Field, m_Document, lane),
                    AnimationTimeDocumentLayout.CurveLaneTop(visibleIndex++)));
            }
        }

        int VisibleCurveCount()
        {
            int count = 0;
            for (int i = 0; i < m_Document.Lanes.Count; i++)
            {
                AnimationTimeLaneDescriptor lane = m_Document.Lanes[i];
                if (lane.Kind == AnimationTimeLaneKind.Curve &&
                    AnimationTimeEditorSession.IsChannelVisible(m_Document, lane.Identity))
                    count++;
            }
            return count;
        }

        AnimationTimeLaneDescriptor FindLane(AnimationTimeLaneKind kind, int occurrence)
        {
            for (int i = 0; i < m_Document.Lanes.Count; i++)
            {
                AnimationTimeLaneDescriptor lane = m_Document.Lanes[i];
                if (lane.Kind != kind)
                    continue;
                if (occurrence-- == 0)
                    return lane;
            }
            return null;
        }
    }

    internal sealed class AnimationTimeSpanView : VisualElement
    {
        public AnimationTimeSpanView(TimelineFieldView field, AnimationTimeSpanDescriptor span)
        {
            VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>("VisualTree/TimelineClipView");
            visualTree.CloneTree(this);
            AddToClassList("timelineClip");
            style.left = field.Geometry.FrameToPosition(span.StartFrame);
            style.width = Mathf.Max(1f,
                field.Geometry.FrameToPosition(span.EndFrame) - field.Geometry.FrameToPosition(span.StartFrame));
            this.Q<Label>("clip-name").text = span.Label;
            this.Q("content").style.backgroundColor = new Color(0.13f, 0.5f, 0.58f, 0.9f);
            this.Q("left-clip-in").style.display = DisplayStyle.None;
            this.Q("right-clip-in").style.display = DisplayStyle.None;
            this.Q("left-mixer").style.display = DisplayStyle.None;
            this.Q("right-mixer").style.display = DisplayStyle.None;
            this.Q("bottom-line").style.backgroundColor = new Color(0.27f, 0.8f, 0.85f, 1f);
            tooltip = $"{span.Label} · {span.StartFrame}-{span.EndFrame}F";
        }
    }

    internal sealed class AnimationTimeDocumentTrackHandle : VisualElement
    {
        readonly TimelineEditorView m_Editor;
        readonly IAnimationTimeDocumentAdapter m_Document;
        readonly VisualElement m_CurveLabels;

        public AnimationTimeDocumentTrackHandle(
            TimelineEditorView editor,
            IAnimationTimeDocumentAdapter document)
        {
            m_Editor = editor ?? throw new ArgumentNullException(nameof(editor));
            m_Document = document ?? throw new ArgumentNullException(nameof(document));
            VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>("VisualTree/TimelineTrackHandle");
            visualTree.CloneTree(this);
            AddToClassList("timelineTrackHandle");
            style.height = AnimationTimeDocumentLayout.ContentHeight(document);
            style.borderLeftColor = new Color(0.13f, 0.62f, 0.7f, 1f);
            TextField name = this.Q<TextField>("name-field");
            name.SetValueWithoutNotify(document.DisplayName);
            name.isReadOnly = true;
            VisualElement icon = this.Q("icon");
            icon.style.backgroundImage = EditorGUIUtility.IconContent("AnimationClip Icon").image as Texture2D;
            AddLaneLabel("SYNC MARKERS", TimelineTrackLayout.ClipRowHeight);
            AddLaneLabel(
                "NOTIFIES",
                TimelineTrackLayout.ClipRowHeight + AnimationTimeDocumentLayout.PointLaneHeight);
            VisualElement curveHeader = this.Q("animation-curves-header");
            Label curveFold = this.Q<Label>("animation-curves-fold");
            m_CurveLabels = this.Q("curve-channel-labels");
            int curveCount = AnimationTimeDocumentLayout.CurveCount(document);
            if (curveCount == 0)
            {
                curveHeader.style.display = DisplayStyle.None;
                return;
            }
            curveHeader.style.display = DisplayStyle.Flex;
            curveHeader.style.top = AnimationTimeDocumentLayout.CurveHeaderTop;
            curveHeader.pickingMode = PickingMode.Position;
            curveFold.text = AnimationTimeEditorSession.CurvesExpanded(document) ? "v" : ">";
            this.Q<Label>("animation-curves-value").text = $"{VisibleCurveCount()}/{curveCount}";
            curveHeader.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 0)
                    ToggleCurves();
                else if (evt.button == 1)
                    ShowCurveChannelMenu();
                else
                    return;
                evt.StopImmediatePropagation();
            });
            m_CurveLabels.style.display = AnimationTimeEditorSession.CurvesExpanded(document)
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            m_CurveLabels.style.top = AnimationTimeDocumentLayout.CurveHeaderTop + TimelineTrackLayout.CurveHeaderHeight;
            PopulateCurveLabels();
        }

        void AddLaneLabel(string text, float top)
        {
            var row = new Label(text);
            row.style.position = Position.Absolute;
            row.style.left = 5f;
            row.style.right = 5f;
            row.style.top = top;
            row.style.height = AnimationTimeDocumentLayout.PointLaneHeight;
            row.style.paddingLeft = 8f;
            row.style.unityTextAlign = TextAnchor.MiddleLeft;
            row.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.style.fontSize = 10f;
            row.style.color = new Color(0.78f, 0.78f, 0.78f, 0.8f);
            row.style.backgroundColor = new Color(0f, 0f, 0f, 0.2f);
            row.style.borderTopWidth = 1f;
            row.style.borderTopColor = new Color(1f, 1f, 1f, 0.08f);
            Add(row);
        }

        void PopulateCurveLabels()
        {
            m_CurveLabels.Clear();
            for (int i = 0; i < m_Document.Lanes.Count; i++)
            {
                AnimationTimeLaneDescriptor lane = m_Document.Lanes[i];
                if (lane.Kind != AnimationTimeLaneKind.Curve ||
                    !AnimationTimeEditorSession.IsChannelVisible(m_Document, lane.Identity))
                    continue;
                var row = new VisualElement();
                row.AddToClassList("curveChannelRow");
                var swatch = new VisualElement();
                swatch.AddToClassList("curveChannelSwatch");
                swatch.style.backgroundColor = lane.Curve.Color;
                var label = new Label(lane.Curve.Label);
                label.AddToClassList("curveChannelLabel");
                var range = new Label(DomainSummary(lane.Curve.ValueDomain));
                range.AddToClassList("curveChannelRange");
                row.Add(swatch);
                row.Add(label);
                row.Add(range);
                m_CurveLabels.Add(row);
            }
        }

        int VisibleCurveCount()
        {
            int count = 0;
            for (int i = 0; i < m_Document.Lanes.Count; i++)
            {
                AnimationTimeLaneDescriptor lane = m_Document.Lanes[i];
                if (lane.Kind == AnimationTimeLaneKind.Curve &&
                    AnimationTimeEditorSession.IsChannelVisible(m_Document, lane.Identity))
                    count++;
            }
            return count;
        }

        void ToggleCurves()
        {
            AnimationTimeEditorSession.ToggleCurves(m_Document);
            m_Editor.RefreshTimeDocument(true);
        }

        void ShowCurveChannelMenu()
        {
            var menu = new GenericMenu();
            for (int i = 0; i < m_Document.Lanes.Count; i++)
            {
                AnimationTimeLaneDescriptor lane = m_Document.Lanes[i];
                if (lane.Kind != AnimationTimeLaneKind.Curve)
                    continue;
                string identity = lane.Identity;
                bool visible = AnimationTimeEditorSession.IsChannelVisible(m_Document, identity);
                menu.AddItem(new GUIContent(lane.Curve.Label), visible, () =>
                {
                    AnimationTimeEditorSession.ToggleChannel(m_Document, identity);
                    m_Editor.RefreshTimeDocument(true);
                });
            }
            menu.ShowAsContext();
        }

        static string DomainSummary(AnimationSequenceCurveValueDomain domain) => domain switch
        {
            AnimationSequenceCurveValueDomain.Normalized01 => "0 - 1",
            AnimationSequenceCurveValueDomain.SignedNormalized => "-1 - 1",
            AnimationSequenceCurveValueDomain.Unbounded => "Auto",
            _ => string.Empty
        };
    }
}
