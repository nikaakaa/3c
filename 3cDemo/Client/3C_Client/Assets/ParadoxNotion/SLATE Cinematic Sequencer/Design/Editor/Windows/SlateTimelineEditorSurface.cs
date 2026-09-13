#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Slate
{
    public sealed class SlateTimelineEditorSurface : IDisposable
    {
        const float TopHeight = 38f;
        const float GroupHeight = 22f;
        const float TrackHeight = 26f;
        const float CurveHeight = 72f;

        ISlateTimelineEditorCommandPort m_Commands;
        ISlateTimelineEditorHost m_Host;
        SlateTimelineEditorContentView m_Content;
        readonly Dictionary<string, CurveEditor.CurveRenderer> m_CurveRenderers =
            new Dictionary<string, CurveEditor.CurveRenderer>(StringComparer.Ordinal);
        Vector2 m_ScrollPosition;
        int m_ViewStartFrame;
        int m_ViewEndFrame;
        bool m_HasView;
        bool m_Disposed;
        bool m_ResizingLeftMargin;
        float m_LeftMargin = 280f;
        string m_Search = string.Empty;
        SlateTimelineEditorSelection m_Selection;
        string m_DragClipId;
        int m_DragClipStart;
        int m_DragClipEnd;
        int m_DragClipBlendIn;
        int m_DragClipBlendOut;
        int m_DragClipAnchorFrame;
        int m_DragClipDelta;

        public SlateTimelineEditorContentView Content => m_Content;
        public SlateTimelineEditorSelection Selection => m_Selection;
        public Vector2 ScrollPosition => m_ScrollPosition;
        public int ViewStartFrame => m_ViewStartFrame;
        public int ViewEndFrame => m_ViewEndFrame;

        public void Bind(
            SlateTimelineEditorContentView content,
            ISlateTimelineEditorCommandPort commands,
            ISlateTimelineEditorHost host)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            m_Content = content;
            m_Commands = commands ?? throw new ArgumentNullException(nameof(commands));
            m_Host = host ?? throw new ArgumentNullException(nameof(host));
            m_ViewStartFrame = content.ViewStartFrame;
            m_ViewEndFrame = content.ViewEndFrame;
            m_HasView = true;
            Styles.Load();
            m_Disposed = false;
        }

        public void SetContent(SlateTimelineEditorContentView content)
        {
            if (m_Disposed)
                return;
            DisposeCurveRenderers();
            m_Content = content ?? throw new ArgumentNullException(nameof(content));
            if (!m_HasView)
            {
                m_ViewStartFrame = content.ViewStartFrame;
                m_ViewEndFrame = content.ViewEndFrame;
                m_HasView = true;
            }
            m_ViewStartFrame = Mathf.Clamp(m_ViewStartFrame, 0, content.LengthFrame - 1);
            m_ViewEndFrame = Mathf.Clamp(m_ViewEndFrame, m_ViewStartFrame + 1, content.LengthFrame);
        }

        void DisposeCurveRenderers()
        {
            foreach (CurveEditor.CurveRenderer renderer in m_CurveRenderers.Values)
                renderer.Dispose();
            m_CurveRenderers.Clear();
        }

        public void SetSelection(SlateTimelineEditorSelection selection)
        {
            m_Selection = selection;
        }

        public void SetView(int startFrame, int endFrame, Vector2 scrollPosition)
        {
            if (m_Content == null)
                return;
            m_ViewStartFrame = Mathf.Clamp(startFrame, 0, m_Content.LengthFrame - 1);
            m_ViewEndFrame = Mathf.Clamp(endFrame, m_ViewStartFrame + 1, m_Content.LengthFrame);
            m_ScrollPosition = scrollPosition;
        }

        public void DrawGUI(float width, float height)
        {
            if (m_Disposed || m_Content == null)
                return;

            Styles.Load();
            Rect surface = new Rect(0f, 0f, Mathf.Max(1f, width), Mathf.Max(1f, height));
            DrawToolbar(surface);
            DrawBody(surface);
            HandleGlobalEvents(surface);
        }

        void DrawToolbar(Rect surface)
        {
            Rect toolbar = new Rect(surface.x, surface.y, surface.width, TopHeight);
            GUI.Box(toolbar, GUIContent.none, Styles.timeBoxStyle ?? GUI.skin.box);
            Rect fitRect = new Rect(4f, 4f, 42f, 20f);
            if (GUI.Button(fitRect, "Fit", EditorStyles.toolbarButton))
            {
                m_ViewStartFrame = 0;
                m_ViewEndFrame = Mathf.Max(1, m_Content.LengthFrame);
                m_Host.RequestRepaint();
            }
            Rect addTrackRect = new Rect(50f, 4f, 78f, 20f);
            if (GUI.Button(addTrackRect, "+ Track", EditorStyles.toolbarButton))
                m_Commands.RequestAddTrack(m_Content.CurrentFrame);

            Rect sliderRect = new Rect(m_LeftMargin + 4f, 7f, Mathf.Max(30f, surface.width - m_LeftMargin - 12f), 18f);
            float start = m_ViewStartFrame;
            float end = m_ViewEndFrame;
            EditorGUI.MinMaxSlider(sliderRect, ref start, ref end, 0f, Mathf.Max(1, m_Content.LengthFrame));
            m_ViewStartFrame = Mathf.Clamp(Mathf.RoundToInt(start), 0, m_Content.LengthFrame - 1);
            m_ViewEndFrame = Mathf.Clamp(Mathf.RoundToInt(end), m_ViewStartFrame + 1, m_Content.LengthFrame);
        }

        void DrawBody(Rect surface)
        {
            Rect body = new Rect(surface.x, TopHeight, surface.width, Mathf.Max(1f, surface.height - TopHeight));
            float contentHeight = CalculateContentHeight();
            Rect content = new Rect(0f, 0f, body.width, Mathf.Max(body.height, contentHeight));
            m_ScrollPosition = GUI.BeginScrollView(body, m_ScrollPosition, content, false, true);
            DrawRuler(body.width);
            DrawGroupsAndTracks(body.width, contentHeight);
            DrawTimeMarkers(body.width, contentHeight);
            GUI.EndScrollView();
        }

        void DrawRuler(float width)
        {
            Rect ruler = new Rect(0f, 0f, width, 28f);
            GUI.Box(ruler, GUIContent.none, Styles.timeBoxStyle ?? GUI.skin.box);
            float timelineWidth = Mathf.Max(1f, width - m_LeftMargin);
            int interval = SelectFrameInterval(timelineWidth);
            int first = Mathf.Max(0, m_ViewStartFrame / interval * interval);
            for (int frame = first; frame <= m_ViewEndFrame; frame += interval)
            {
                float x = FrameToX(frame, timelineWidth);
                GUI.color = frame == m_Content.CurrentFrame ? Color.red : Color.white;
                GUI.DrawTexture(new Rect(m_LeftMargin + x, 22f, 1f, 6f), Styles.whiteTexture);
                string label = $"{frame}F";
                GUI.Label(new Rect(m_LeftMargin + x - 18f, 5f, 40f, 16f), label, EditorStyles.label);
            }
            GUI.color = Color.white;
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && ruler.Contains(Event.current.mousePosition))
            {
                int frame = PositionToFrame(Event.current.mousePosition.x - m_LeftMargin, Mathf.Max(1f, ruler.width - m_LeftMargin));
                m_Commands.SetCurrentFrame(frame);
                Event.current.Use();
            }
        }

        void DrawGroupsAndTracks(float width, float contentHeight)
        {
            Rect left = new Rect(0f, 28f, m_LeftMargin, contentHeight);
            GUI.Box(left, GUIContent.none, Styles.timeBoxStyle ?? GUI.skin.box);
            Rect search = new Rect(20f, 32f, Mathf.Max(20f, m_LeftMargin - 38f), 18f);
            m_Search = EditorGUI.TextField(search, m_Search, GUI.skin.FindStyle("ToolbarSearchTextField") ?? EditorStyles.toolbarTextField);
            Rect splitter = new Rect(m_LeftMargin - 4f, 28f, 8f, contentHeight);
            EditorGUIUtility.AddCursorRect(splitter, MouseCursor.ResizeHorizontal);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && splitter.Contains(Event.current.mousePosition))
            {
                m_ResizingLeftMargin = true;
                Event.current.Use();
            }
            if (m_ResizingLeftMargin)
                m_LeftMargin = Mathf.Clamp(Event.current.mousePosition.x, 230f, 420f);
            if (Event.current.rawType == EventType.MouseUp)
                m_ResizingLeftMargin = false;

            float y = 54f;
            for (int groupIndex = 0; groupIndex < m_Content.Groups.Count; groupIndex++)
            {
                SlateTimelineEditorGroupView group = m_Content.Groups[groupIndex];
                if (!MatchesSearch(group))
                    continue;
                Rect groupRect = new Rect(4f, y, m_LeftMargin - 8f, GroupHeight - 3f);
                GUI.color = IsSelected(SlateTimelineEditorElementKind.Group, group.GroupId, string.Empty)
                    ? new Color(0.5f, 0.5f, 1f, 0.3f)
                    : new Color(0f, 0f, 0f, 0.25f);
                GUI.Box(groupRect, GUIContent.none, Styles.headerBoxStyle ?? GUI.skin.box);
                GUI.color = Color.white;
                GUI.Label(new Rect(groupRect.x + 18f, groupRect.y + 2f, groupRect.width - 22f, 18f), group.DisplayName, EditorStyles.boldLabel);
                if (GUI.Button(new Rect(groupRect.x + 2f, groupRect.y + 2f, 14f, 16f), group.IsCollapsed ? "▶" : "▼", GUIStyle.none))
                {
                    m_Commands.SetGroupCollapsed(group.GroupId, !group.IsCollapsed);
                    m_Host.RequestRepaint();
                }
                y += GroupHeight;
                if (group.IsCollapsed)
                    continue;
                for (int trackIndex = 0; trackIndex < group.Tracks.Count; trackIndex++)
                {
                    SlateTimelineEditorTrackView track = group.Tracks[trackIndex];
                    if (!MatchesSearch(track))
                        continue;
                    DrawTrack(group, track, ref y, width);
                }
            }
        }

        void DrawTrack(
            SlateTimelineEditorGroupView group,
            SlateTimelineEditorTrackView track,
            ref float y,
            float width)
        {
            float timelineWidth = Mathf.Max(1f, width - m_LeftMargin);
            float rowHeight = TrackHeight + (track.ShowCurves ? CurveHeight : 0f);
            Rect leftRect = new Rect(4f, y, m_LeftMargin - 8f, rowHeight - 3f);
            Rect timelineRect = new Rect(m_LeftMargin, y, timelineWidth, rowHeight - 3f);
            bool selected = IsSelected(SlateTimelineEditorElementKind.Track, track.TrackId, string.Empty);
            GUI.color = selected ? new Color(0.5f, 0.5f, 1f, 0.3f) : new Color(0f, 0f, 0f, 0.16f);
            GUI.Box(leftRect, GUIContent.none, Styles.headerBoxStyle ?? GUI.skin.box);
            GUI.Box(timelineRect, GUIContent.none, Styles.timeBoxStyle ?? GUI.skin.box);
            GUI.color = track.IsActive ? Color.white : Color.grey;
            if (track.RuntimeActive)
                GUI.color = Color.yellow;
            GUI.Label(new Rect(leftRect.x + 8f, leftRect.y + 3f, leftRect.width - 10f, 18f), track.DisplayName, EditorStyles.label);
            GUI.color = Color.white;
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && leftRect.Contains(Event.current.mousePosition))
            {
                Select(new SlateTimelineEditorSelection(
                    SlateTimelineEditorElementKind.Track,
                    group.GroupId,
                    track.TrackId,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    -1));
                Event.current.Use();
            }
            if (Event.current.type == EventType.ContextClick && timelineRect.Contains(Event.current.mousePosition))
            {
                m_Commands.RequestAddClip(track.TrackId, PositionToFrame(Event.current.mousePosition.x - m_LeftMargin, timelineWidth));
                Event.current.Use();
            }
            for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                DrawClip(track, track.Clips[clipIndex], timelineRect, timelineWidth);
            y += rowHeight;
        }

        void DrawClip(
            SlateTimelineEditorTrackView track,
            SlateTimelineEditorClipView clip,
            Rect timelineRect,
            float timelineWidth)
        {
            int offset = string.Equals(m_DragClipId, clip.ClipId, StringComparison.Ordinal) ? m_DragClipDelta : 0;
            int start = clip.StartFrame + offset;
            int end = clip.EndFrame + offset;
            float x = timelineRect.x + FrameToX(start, timelineWidth);
            float width = Mathf.Max(3f, FrameToX(end, timelineWidth) - FrameToX(start, timelineWidth));
            Rect clipRect = new Rect(x, timelineRect.y + 2f, width, TrackHeight - 7f);
            bool selected = IsSelected(SlateTimelineEditorElementKind.Clip, clip.TrackId, clip.ClipId);
            GUI.color = clip.RuntimeActive
                ? new Color(0.95f, 0.75f, 0.2f, 0.95f)
                : selected
                    ? new Color(0.35f, 0.65f, 0.95f, 0.9f)
                    : new Color(0.25f, 0.45f, 0.55f, 0.8f);
            GUI.Box(clipRect, GUIContent.none, Styles.clipBoxStyle ?? GUI.skin.box);
            GUI.color = Color.white;
            GUI.Label(new Rect(clipRect.x + 4f, clipRect.y + 2f, clipRect.width - 8f, 18f), clip.DisplayName, EditorStyles.label);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && clipRect.Contains(Event.current.mousePosition))
            {
                Select(new SlateTimelineEditorSelection(
                    SlateTimelineEditorElementKind.Clip,
                    string.Empty,
                    clip.TrackId,
                    clip.ClipId,
                    string.Empty,
                    string.Empty,
                    -1));
                if (Event.current.clickCount == 2)
                    m_Commands.OpenSource(clip.ClipId);
                else
                {
                    m_DragClipId = clip.ClipId;
                    m_DragClipStart = clip.StartFrame;
                    m_DragClipEnd = clip.EndFrame;
                    m_DragClipBlendIn = clip.BlendInFrame;
                    m_DragClipBlendOut = clip.BlendOutFrame;
                    m_DragClipAnchorFrame = PositionToFrame(Event.current.mousePosition.x - timelineRect.x, timelineWidth);
                    m_DragClipDelta = 0;
                    m_Commands.BeginGesture("Move Timeline Clip");
                }
                Event.current.Use();
            }
            if (track.ShowCurves)
            {
                Rect curveRect = new Rect(timelineRect.x, timelineRect.y + TrackHeight, timelineRect.width, CurveHeight);
                DrawCurves(clip, curveRect, timelineWidth);
            }
        }

        void DrawCurves(SlateTimelineEditorClipView clip, Rect rect, float timelineWidth)
        {
            GUI.Box(rect, GUIContent.none, Styles.timeBoxStyle ?? GUI.skin.box);
            if (clip.Curves.Count == 0)
                return;
            float curveHeight = rect.height / clip.Curves.Count;
            for (int curveIndex = 0; curveIndex < clip.Curves.Count; curveIndex++)
            {
                SlateTimelineEditorCurveView curve = clip.Curves[curveIndex];
                if (curve.Curve == null || curve.Curve.length == 0)
                    continue;
                string key = $"{clip.ClipId}:{curve.CurveId}";
                if (!m_CurveRenderers.TryGetValue(key, out CurveEditor.CurveRenderer renderer))
                {
                    renderer = CurveEditor.CreatePureCurveRenderer(
                        new[] { TimelineCurveAuthoringCopy(curve.Curve) },
                        Rect.zero,
                        1f / Mathf.Max(1, curve.EndFrame - curve.StartFrame),
                        curves =>
                        {
                            if (curves == null || curves.Length == 0 || curves[0] == null)
                                return;
                            m_Commands.BeginGesture("Edit Timeline Curve");
                            m_Commands.ReplaceCurve(clip.ClipId, curve.CurveId, curves[0]);
                            m_Commands.CommitGesture();
                            m_Host.RequestRepaint();
                        });
                    m_CurveRenderers.Add(key, renderer);
                }
                Rect curveRect = new Rect(rect.x, rect.y + curveIndex * curveHeight, rect.width, curveHeight);
                renderer.Draw(curveRect, Rect.MinMaxRect(0f, 0f, 1f, 1f));
            }
        }

        static AnimationCurve TimelineCurveAuthoringCopy(AnimationCurve source)
        {
            return new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
        }


        void HandleGlobalEvents(Rect surface)
        {
            if (m_DragClipId != null)
            {
                if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
                {
                    float timelineWidth = Mathf.Max(1f, surface.width - m_LeftMargin);
                    m_DragClipDelta = PositionToFrame(Event.current.mousePosition.x - m_LeftMargin, timelineWidth) - m_DragClipAnchorFrame;
                    m_Host.RequestRepaint();
                    Event.current.Use();
                }
                if (Event.current.rawType == EventType.MouseUp)
                {
                    int maxDelta = m_Content.LengthFrame - m_DragClipEnd;
                    int minDelta = -m_DragClipStart;
                    int delta = Mathf.Clamp(m_DragClipDelta, minDelta, maxDelta);
                    m_Commands.SetClipRange(
                        m_DragClipId,
                        m_DragClipStart + delta,
                        m_DragClipEnd + delta,
                        m_DragClipBlendIn,
                        m_DragClipBlendOut);
                    m_Commands.CommitGesture();
                    m_DragClipId = null;
                    m_DragClipDelta = 0;
                    Event.current.Use();
                }
            }
        }

        void DrawTimeMarkers(float width, float contentHeight)
        {
            float timelineWidth = Mathf.Max(1f, width - m_LeftMargin);
            if (m_Content.CurrentFrame >= 0)
            {
                float x = m_LeftMargin + FrameToX(m_Content.CurrentFrame, timelineWidth);
                GUI.color = Color.red;
                GUI.DrawTexture(new Rect(x - 1f, 28f, 2f, contentHeight - 28f), Styles.whiteTexture);
            }
            if (m_Content.RuntimeFrame >= 0)
            {
                float x = m_LeftMargin + FrameToX(m_Content.RuntimeFrame, timelineWidth);
                GUI.color = Color.yellow;
                GUI.DrawTexture(new Rect(x, 28f, 1f, contentHeight - 28f), Styles.whiteTexture);
            }
            for (int groupIndex = 0; groupIndex < m_Content.Groups.Count; groupIndex++)
            {
                SlateTimelineEditorGroupView group = m_Content.Groups[groupIndex];
                for (int sectionIndex = 0; sectionIndex < group.Sections.Count; sectionIndex++)
                {
                    SlateTimelineEditorSectionView section = group.Sections[sectionIndex];
                    float x = m_LeftMargin + FrameToX(section.Frame, timelineWidth);
                    GUI.color = new Color(0.4f, 0.8f, 1f, 0.65f);
                    GUI.DrawTexture(new Rect(x, 28f, 1f, contentHeight - 28f), Styles.whiteTexture);
                    GUI.Label(new Rect(x + 3f, 30f, 120f, 18f), section.DisplayName, EditorStyles.miniLabel);
                }
            }
            GUI.color = Color.white;
        }

        void Select(SlateTimelineEditorSelection selection)
        {
            m_Selection = selection;
            m_Commands.Select(selection);
            m_Host.RequestRepaint();
        }

        bool IsSelected(SlateTimelineEditorElementKind kind, string primaryId, string secondaryId)
        {
            if (m_Selection.Kind != kind)
                return false;
            if (kind == SlateTimelineEditorElementKind.Track)
                return string.Equals(m_Selection.TrackId, primaryId, StringComparison.Ordinal);
            if (kind == SlateTimelineEditorElementKind.Clip)
                return string.Equals(m_Selection.ClipId, secondaryId, StringComparison.Ordinal);
            return string.Equals(m_Selection.GroupId, primaryId, StringComparison.Ordinal);
        }

        bool MatchesSearch(SlateTimelineEditorGroupView group)
        {
            return string.IsNullOrEmpty(m_Search) || group.DisplayName.IndexOf(m_Search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        bool MatchesSearch(SlateTimelineEditorTrackView track)
        {
            return string.IsNullOrEmpty(m_Search) || track.DisplayName.IndexOf(m_Search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        float CalculateContentHeight()
        {
            float height = 58f;
            for (int groupIndex = 0; groupIndex < m_Content.Groups.Count; groupIndex++)
            {
                SlateTimelineEditorGroupView group = m_Content.Groups[groupIndex];
                if (!MatchesSearch(group))
                    continue;
                height += GroupHeight;
                if (group.IsCollapsed)
                    continue;
                for (int trackIndex = 0; trackIndex < group.Tracks.Count; trackIndex++)
                {
                    if (MatchesSearch(group.Tracks[trackIndex]))
                        height += TrackHeight + (group.Tracks[trackIndex].ShowCurves ? CurveHeight : 0f);
                }
            }
            return height;
        }

        int SelectFrameInterval(float width)
        {
            int[] intervals = { 1, 2, 5, 10, 15, 30, 60, 120, 300, 600 };
            float frameWidth = width / Mathf.Max(1f, m_ViewEndFrame - m_ViewStartFrame);
            for (int index = 0; index < intervals.Length; index++)
                if (frameWidth * intervals[index] >= 48f)
                    return intervals[index];
            return intervals[intervals.Length - 1];
        }

        float FrameToX(int frame, float width)
        {
            return Mathf.InverseLerp(m_ViewStartFrame, m_ViewEndFrame, frame) * width;
        }

        int PositionToFrame(float position, float width)
        {
            return Mathf.RoundToInt(Mathf.Lerp(m_ViewStartFrame, m_ViewEndFrame, Mathf.Clamp01(position / Mathf.Max(1f, width))));
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            if (m_DragClipId != null)
                m_Commands?.CancelGesture();
            DisposeCurveRenderers();
            m_Disposed = true;
            m_Content = null;
            m_Commands = null;
            m_Host = null;
            m_DragClipId = null;
        }
    }
}
#endif
