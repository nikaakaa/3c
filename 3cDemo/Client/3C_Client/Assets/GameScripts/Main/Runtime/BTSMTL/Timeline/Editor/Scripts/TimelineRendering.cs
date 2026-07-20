using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BTSMTL.Timeline.Editor
{
    internal enum TimelinePlayheadMode
    {
        Empty,
        AuthoringPreview,
        LiveDebug
    }

    internal readonly struct TimelinePlayheadRenderInput
    {
        public TimelinePlayheadRenderInput(TimelinePlayheadMode mode, float time, int frame)
        {
            Mode = mode;
            Time = time;
            Frame = frame;
        }

        public TimelinePlayheadMode Mode { get; }
        public float Time { get; }
        public int Frame { get; }
    }

    internal readonly struct TimelineTrackRenderInput
    {
        public TimelineTrackRenderInput(float top, float height)
        {
            Top = top;
            Height = height;
        }

        public float Top { get; }
        public float Height { get; }
    }

    internal static class TimelineTrackLayout
    {
        public const float ClipRowHeight = 30f;
        public const float MarkerHeaderHeight = 22f;
        public const float MarkerLaneHeight = 24f;
        public const float CurveHeaderHeight = 22f;
        public const float CurveLaneHeight = 72f;
        public const float VerticalMargin = 5f;

        public static float ContentHeight(Track track)
        {
            float height = ClipRowHeight;
            if (track is AnimationTrack)
                height += MarkerHeaderHeight + (MarkersExpanded(track) ? MarkerLaneHeight : 0f);
            int channelCount = VisibleCurveChannelCount(track);
            if (RegisteredCurveChannelCount(track) > 0)
                height += CurveHeaderHeight + (CurvesExpanded(track) ? channelCount * CurveLaneHeight : 0f);
            return height;
        }

        public static float MarkerHeaderTop => ClipRowHeight;
        public static float MarkerLaneTop => ClipRowHeight + MarkerHeaderHeight;

        public static float CurveHeaderTop(Track track)
        {
            return ClipRowHeight + (track is AnimationTrack
                ? MarkerHeaderHeight + (MarkersExpanded(track) ? MarkerLaneHeight : 0f)
                : 0f);
        }

        public static float CurveLaneTop(Track track, int visibleChannelIndex)
        {
            return CurveHeaderTop(track) + CurveHeaderHeight + visibleChannelIndex * CurveLaneHeight;
        }

        public static bool CurvesExpanded(Track track) => TimelineCurveEditorSession.CurvesExpanded(track);
        public static bool MarkersExpanded(Track track) => track is AnimationTrack && TimelineCurveEditorSession.MarkersExpanded(track);

        public static void ToggleCurves(Track track) => TimelineCurveEditorSession.ToggleCurves(track);
        public static void ToggleMarkers(Track track) => TimelineCurveEditorSession.ToggleMarkers(track);

        public static int RegisteredCurveChannelCount(Track track)
        {
            var channels = new List<TimelineCurveChannelDescriptor>();
            TimelineCurveChannelCatalog.CollectForTrack(track, channels);
            return channels.Count;
        }

        public static int VisibleCurveChannelCount(Track track)
        {
            var channels = new List<TimelineCurveChannelDescriptor>();
            TimelineCurveChannelCatalog.CollectForTrack(track, channels);
            int count = 0;
            for (int i = 0; i < channels.Count; i++)
            {
                if (TimelineCurveEditorSession.IsChannelVisible(track, channels[i].ChannelId))
                    count++;
            }
            return count;
        }

        public static float Stride(Track track)
        {
            return ContentHeight(track) + VerticalMargin * 2f;
        }

        public static float Top(IReadOnlyList<Track> tracks, int index)
        {
            float top = 0f;
            for (int i = 0; i < index; i++)
                top += Stride(tracks[i]);
            return top;
        }

        public static float TotalHeight(IReadOnlyList<Track> tracks)
        {
            return Top(tracks, tracks.Count);
        }

        public static int IndexAt(IReadOnlyList<Track> tracks, float centerY)
        {
            float top = 0f;
            for (int i = 0; i < tracks.Count; i++)
            {
                float stride = Stride(tracks[i]);
                if (centerY < top + stride * 0.5f)
                    return i;
                top += stride;
            }
            return Mathf.Max(0, tracks.Count - 1);
        }

    }

    internal readonly struct TimelineClipRenderInput
    {
        public TimelineClipRenderInput(TimelineClipView view)
        {
            StartFrame = view.StartFrame;
            EndFrame = view.EndFrame;
            ClipInFrame = view.ClipInFrame;
            Length = view.Clip.Length;
            Invalid = view.Clip.Invalid;
            EaseInFrame = view.EaseInFrame;
            EaseOutFrame = view.EaseOutFrame;
            OtherEaseInFrame = view.OtherEaseInFrame;
            OtherEaseOutFrame = view.OtherEaseOutFrame;
            SelfEaseInFrame = view.SelfEaseInFrame;
            SelfEaseOutFrame = view.SelfEaseOutFrame;
        }

        public int StartFrame { get; }
        public int EndFrame { get; }
        public int ClipInFrame { get; }
        public int Length { get; }
        public bool Invalid { get; }
        public int EaseInFrame { get; }
        public int EaseOutFrame { get; }
        public int OtherEaseInFrame { get; }
        public int OtherEaseOutFrame { get; }
        public int SelfEaseInFrame { get; }
        public int SelfEaseOutFrame { get; }
        public int WidthFrame => EndFrame - StartFrame;
    }

    internal sealed class TimelineRuntimeOverlayModel
    {
        public TimelineRuntimeOverlayModel(
            float visualTime,
            IReadOnlyDictionary<string, string> activeTracks,
            IReadOnlyDictionary<string, string> activeClips)
        {
            VisualTime = Mathf.Max(0f, visualTime);
            ActiveTracks = activeTracks;
            ActiveClips = activeClips;
        }

        public float VisualTime { get; }
        public IReadOnlyDictionary<string, string> ActiveTracks { get; }
        public IReadOnlyDictionary<string, string> ActiveClips { get; }
    }

    internal sealed class TimelineRendering
    {
        readonly TimelineFrameGeometry m_Geometry;
        int[] m_EditFrames = Array.Empty<int>();
        Color m_FieldLineColor;
        Font m_MarkerTextFont;

        public TimelineRendering(TimelineFrameGeometry geometry)
        {
            m_Geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
        }

        public void SetFieldLineColor(Color color)
        {
            m_FieldLineColor = color;
        }

        public void SetMarkerTextFont(Font font)
        {
            m_MarkerTextFont = font;
        }

        public void DrawMarker(MeshGenerationContext context, int minimumFrame, int maximumFrame)
        {
            Painter2D painter = context.painter2D;
            painter.strokeColor = Color.white;
            painter.BeginPath();
            int showInterval = Mathf.CeilToInt(1f / m_Geometry.Scale);
            int startFrame = minimumFrame;
            int endFrame = maximumFrame;
            bool drawEveryLabel = m_Geometry.OneFrameWidth >
                                  TextWidth(m_Geometry.MaxFrame.ToString(), m_MarkerTextFont, 14) * 1.5f;
            for (int frame = startFrame; frame <= endFrame; frame++)
            {
                float x = m_Geometry.FrameToPosition(frame);
                if (frame % (showInterval * 5) == 0)
                {
                    painter.MoveTo(new Vector2(x, 10f));
                    painter.LineTo(new Vector2(x, 25f));
                    context.DrawText(frame.ToString(), new Vector2(x + 5f, 5f), 14, Color.white);
                }
                else if (frame % showInterval == 0)
                {
                    painter.MoveTo(new Vector2(x, 20f));
                    painter.LineTo(new Vector2(x, 25f));
                    if (drawEveryLabel)
                        context.DrawText(frame.ToString(), new Vector2(x + 5f, 5f), 14, Color.white);
                }
            }
            painter.Stroke();
        }

        public void DrawTrackGrid(
            MeshGenerationContext context,
            int minimumFrame,
            int maximumFrame,
            float viewportHeight)
        {
            Painter2D painter = context.painter2D;
            painter.strokeColor = m_FieldLineColor;
            painter.BeginPath();
            int showInterval = Mathf.CeilToInt(1f / m_Geometry.Scale);
            for (int frame = minimumFrame; frame <= maximumFrame; frame++)
            {
                if (frame % (showInterval * 5) != 0)
                    continue;
                float x = m_Geometry.FrameToPosition(frame);
                painter.MoveTo(new Vector2(x, 0f));
                painter.LineTo(new Vector2(x, viewportHeight));
            }
            painter.Stroke();
        }

        public void DrawPlayhead(MeshGenerationContext context, float viewportHeight)
        {
            Painter2D painter = context.painter2D;
            painter.strokeColor = Color.white;
            painter.BeginPath();
            painter.MoveTo(new Vector2(0f, 25f));
            painter.LineTo(new Vector2(0f, viewportHeight));
            painter.Stroke();
        }

        public void ApplyPlayhead(
            TimelinePlayheadRenderInput input,
            VisualElement timeLocator,
            Label frameLabel)
        {
            float frame = input.Mode == TimelinePlayheadMode.Empty
                ? 0f
                : input.Mode == TimelinePlayheadMode.LiveDebug
                    ? input.Time * TimelineUtility.FrameRate
                    : input.Frame;
            timeLocator.style.left = frame * m_Geometry.OneFrameWidth + m_Geometry.FieldOffsetX;
            timeLocator.MarkDirtyRepaint();
            frameLabel.text = input.Mode == TimelinePlayheadMode.Empty
                ? string.Empty
                : Mathf.RoundToInt(frame).ToString();
        }

        public void SetEditFrames(params int[] frames)
        {
            m_EditFrames = frames ?? Array.Empty<int>();
            for (int i = 0; i < m_EditFrames.Length; i++)
                m_Geometry.EnsureFrameCapacity(m_EditFrames[i]);
        }

        public void DrawEditOverlay(MeshGenerationContext context, float viewportHeight)
        {
            Painter2D painter = context.painter2D;
            painter.strokeColor = new Color(1f, 0.6f, 0f, 1f);
            painter.BeginPath();
            for (int frameIndex = 0; frameIndex < m_EditFrames.Length; frameIndex++)
            {
                int frame = m_EditFrames[frameIndex];
                float x = m_Geometry.FrameToPosition(frame);
                int count = Mathf.CeilToInt(viewportHeight / 5f);
                for (int line = 0; line < count; line += 2)
                {
                    painter.MoveTo(new Vector2(x, line * 5f));
                    painter.LineTo(new Vector2(x, line * 5f + 5f));
                }
                context.DrawText(frame.ToString(), new Vector2(x + 5f, 5f), 14, Color.white);
            }
            painter.Stroke();
        }

        public void ApplyTrackAuthoring(TimelineTrackView view, TimelineTrackRenderInput input)
        {
            view.style.height = input.Height;
            view.transform.position = new Vector3(0f, input.Top, 0f);
        }

        public void ApplyClipAuthoring(TimelineClipView view, TimelineClipRenderInput input)
        {
            view.style.left = m_Geometry.FrameToPosition(input.StartFrame);
            view.style.width = m_Geometry.FrameToPosition(input.EndFrame) - m_Geometry.FrameToPosition(input.StartFrame);
            view.LeftClipInElement.style.display = input.ClipInFrame > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            view.RightClipInElement.style.display = input.ClipInFrame + input.WidthFrame < input.Length
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            SetClass(view, "invalid", input.Invalid);
            SetClass(view, "mixLeft", input.EaseInFrame > 0);
            SetClass(view, "mixRight", input.EaseOutFrame > 0);
            if (input.OtherEaseInFrame > 0)
                view.SelfEaseIn = false;
            if (input.OtherEaseOutFrame > 0)
                view.SelfEaseOut = false;
            if (input.Invalid)
            {
                view.ContentElement.style.left = 0f;
                view.ContentElement.style.width = view.TitleElement.style.width = input.WidthFrame * m_Geometry.OneFrameWidth;
                view.LeftMixerElement.style.width = view.RightMixerElement.style.width = 0f;
                return;
            }
            int offset = input.OtherEaseInFrame > 0 ? (input.OtherEaseOutFrame > 0 ? 0 : -2) : 0;
            view.ContentElement.style.left = input.OtherEaseInFrame > 0
                ? input.OtherEaseInFrame * 0.5f * m_Geometry.OneFrameWidth + offset
                : 0f;
            view.ContentElement.style.width = (input.WidthFrame -
                (input.OtherEaseInFrame > 0 ? input.OtherEaseInFrame * 0.5f : 0f) -
                (input.OtherEaseOutFrame > 0 ? input.OtherEaseOutFrame * 0.5f : 0f)) * m_Geometry.OneFrameWidth;
            view.TitleElement.style.width = (input.WidthFrame - input.EaseInFrame - input.EaseOutFrame) * m_Geometry.OneFrameWidth;
            view.LeftMixerElement.style.width = (input.OtherEaseInFrame > 0
                ? input.OtherEaseInFrame * 0.5f
                : input.SelfEaseInFrame) * m_Geometry.OneFrameWidth;
            view.RightMixerElement.style.width = (input.OtherEaseOutFrame > 0
                ? input.OtherEaseOutFrame * 0.5f
                : input.SelfEaseOutFrame) * m_Geometry.OneFrameWidth;
        }

        public void DrawClipSelection(TimelineClipView view, MeshGenerationContext context)
        {
            if (!view.Hovered)
                return;
            Painter2D painter = context.painter2D;
            painter.strokeColor = new Color(68f / 255f, 192f / 255f, 1f, 1f);
            painter.BeginPath();
            painter.MoveTo(Vector2.zero);
            painter.LineTo(new Vector2(view.worldBound.width, 0f));
            painter.LineTo(new Vector2(view.worldBound.width, view.worldBound.height));
            painter.LineTo(new Vector2(0f, view.worldBound.height));
            painter.LineTo(Vector2.zero);
            painter.Stroke();
        }

        public void ApplyRuntimeOverlay(TimelineRuntimeOverlayModel model, IReadOnlyList<TimelineTrackView> tracks)
        {
            for (int trackIndex = 0; trackIndex < tracks.Count; trackIndex++)
            {
                TimelineTrackView trackView = tracks[trackIndex];
                string trackStatus = string.Empty;
                bool trackActive = model.ActiveTracks != null &&
                                   model.ActiveTracks.TryGetValue(trackView.Track.AuthoringId, out trackStatus);
                ApplyTrackOverlay(trackView, trackActive, trackStatus);
                for (int clipIndex = 0; clipIndex < trackView.ClipViews.Count; clipIndex++)
                {
                    TimelineClipView clipView = trackView.ClipViews[clipIndex];
                    string clipStatus = string.Empty;
                    bool clipActive = model.ActiveClips != null &&
                                      model.ActiveClips.TryGetValue(clipView.Clip.AuthoringId, out clipStatus);
                    ApplyClipOverlay(clipView, clipActive, clipStatus);
                }
            }
        }

        public void ClearRuntimeOverlay(IReadOnlyList<TimelineTrackView> tracks)
        {
            for (int trackIndex = 0; trackIndex < tracks.Count; trackIndex++)
            {
                TimelineTrackView trackView = tracks[trackIndex];
                ApplyTrackOverlay(trackView, false, string.Empty);
                for (int clipIndex = 0; clipIndex < trackView.ClipViews.Count; clipIndex++)
                    ApplyClipOverlay(trackView.ClipViews[clipIndex], false, string.Empty);
            }
        }

        static void ApplyTrackOverlay(TimelineTrackView view, bool active, string status)
        {
            view.style.borderLeftWidth = active ? 3f : 0f;
            view.style.borderLeftColor = active ? new Color(0.25f, 0.9f, 0.55f, 1f) : Color.clear;
            view.tooltip = active ? status ?? string.Empty : string.Empty;
        }

        static void ApplyClipOverlay(TimelineClipView view, bool active, string status)
        {
            view.BottomLineElement.style.height = active ? 4f : 1f;
            view.BottomLineElement.style.backgroundColor = active ? new Color(0.25f, 0.9f, 0.55f, 1f) : view.Clip.Color();
            view.tooltip = active ? status ?? string.Empty : string.Empty;
        }

        static void SetClass(VisualElement element, string className, bool enabled)
        {
            if (enabled)
                element.AddToClassList(className);
            else
                element.RemoveFromClassList(className);
        }

        static int TextWidth(string text, Font font, int fontSize)
        {
            if (string.IsNullOrEmpty(text) || font == null)
                return 0;
            font.RequestCharactersInTexture(text, fontSize, FontStyle.Normal);
            int width = 0;
            for (int i = 0; i < text.Length; i++)
            {
                font.GetCharacterInfo(text[i], out CharacterInfo info, fontSize);
                width += info.advance;
            }
            return width;
        }
    }

}
