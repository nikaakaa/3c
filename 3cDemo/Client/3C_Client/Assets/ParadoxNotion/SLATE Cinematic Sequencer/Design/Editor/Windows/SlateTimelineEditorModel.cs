#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Slate
{
    public enum SlateTimelineEditorElementKind : byte
    {
        None,
        Group,
        Track,
        Clip,
        Section,
        Curve,
        Key
    }

    public readonly struct SlateTimelineEditorSelection : IEquatable<SlateTimelineEditorSelection>
    {
        public SlateTimelineEditorSelection(
            SlateTimelineEditorElementKind kind,
            string groupId,
            string trackId,
            string clipId,
            string sectionId,
            string curveId,
            int keyIndex)
        {
            Kind = kind;
            GroupId = groupId ?? string.Empty;
            TrackId = trackId ?? string.Empty;
            ClipId = clipId ?? string.Empty;
            SectionId = sectionId ?? string.Empty;
            CurveId = curveId ?? string.Empty;
            KeyIndex = keyIndex;
        }

        public SlateTimelineEditorElementKind Kind { get; }
        public string GroupId { get; }
        public string TrackId { get; }
        public string ClipId { get; }
        public string SectionId { get; }
        public string CurveId { get; }
        public int KeyIndex { get; }

        public bool Equals(SlateTimelineEditorSelection other)
        {
            return Kind == other.Kind &&
                   string.Equals(GroupId, other.GroupId, StringComparison.Ordinal) &&
                   string.Equals(TrackId, other.TrackId, StringComparison.Ordinal) &&
                   string.Equals(ClipId, other.ClipId, StringComparison.Ordinal) &&
                   string.Equals(SectionId, other.SectionId, StringComparison.Ordinal) &&
                   string.Equals(CurveId, other.CurveId, StringComparison.Ordinal) &&
                   KeyIndex == other.KeyIndex;
        }

        public override bool Equals(object obj) =>
            obj is SlateTimelineEditorSelection other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(GroupId);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(TrackId);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(ClipId);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(SectionId);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(CurveId);
                return (hash * 397) ^ KeyIndex;
            }
        }

        public static SlateTimelineEditorSelection None =>
            new SlateTimelineEditorSelection(
                SlateTimelineEditorElementKind.None,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                -1);
    }

    public sealed class SlateTimelineEditorCurveView
    {
        public SlateTimelineEditorCurveView(
            string curveId,
            string displayName,
            AnimationCurve curve,
            int startFrame,
            int endFrame)
        {
            CurveId = curveId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Curve = curve ?? throw new ArgumentNullException(nameof(curve));
            StartFrame = startFrame;
            EndFrame = Mathf.Max(startFrame + 1, endFrame);
        }

        public string CurveId { get; }
        public string DisplayName { get; }
        public AnimationCurve Curve { get; }
        public int StartFrame { get; }
        public int EndFrame { get; }
    }

    public sealed class SlateTimelineEditorClipView
    {
        public SlateTimelineEditorClipView(
            string clipId,
            string trackId,
            string displayName,
            string contractKind,
            int startFrame,
            int endFrame,
            int blendInFrame,
            int blendOutFrame,
            IReadOnlyList<SlateTimelineEditorCurveView> curves,
            bool runtimeActive = false,
            string runtimeLabel = "")
        {
            ClipId = clipId ?? string.Empty;
            TrackId = trackId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            ContractKind = contractKind ?? string.Empty;
            StartFrame = startFrame;
            EndFrame = Mathf.Max(startFrame + 1, endFrame);
            BlendInFrame = Mathf.Clamp(blendInFrame, 0, EndFrame - StartFrame);
            BlendOutFrame = Mathf.Clamp(blendOutFrame, 0, EndFrame - StartFrame - BlendInFrame);
            Curves = curves ?? Array.Empty<SlateTimelineEditorCurveView>();
            RuntimeActive = runtimeActive;
            RuntimeLabel = runtimeLabel ?? string.Empty;
        }

        public string ClipId { get; }
        public string TrackId { get; }
        public string DisplayName { get; }
        public string ContractKind { get; }
        public int StartFrame { get; }
        public int EndFrame { get; }
        public int BlendInFrame { get; }
        public int BlendOutFrame { get; }
        public IReadOnlyList<SlateTimelineEditorCurveView> Curves { get; }
        public bool RuntimeActive { get; }
        public string RuntimeLabel { get; }
    }

    public sealed class SlateTimelineEditorTrackView
    {
        public SlateTimelineEditorTrackView(
            string trackId,
            string groupId,
            string displayName,
            string contractKind,
            Color color,
            bool isActive,
            bool isLocked,
            bool isCollapsed,
            bool showCurves,
            IReadOnlyList<SlateTimelineEditorClipView> clips,
            bool runtimeActive = false)
        {
            TrackId = trackId ?? string.Empty;
            GroupId = groupId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            ContractKind = contractKind ?? string.Empty;
            Color = color;
            IsActive = isActive;
            IsLocked = isLocked;
            IsCollapsed = isCollapsed;
            ShowCurves = showCurves;
            Clips = clips ?? Array.Empty<SlateTimelineEditorClipView>();
            RuntimeActive = runtimeActive;
        }

        public string TrackId { get; }
        public string GroupId { get; }
        public string DisplayName { get; }
        public string ContractKind { get; }
        public Color Color { get; }
        public bool IsActive { get; }
        public bool IsLocked { get; }
        public bool IsCollapsed { get; }
        public bool ShowCurves { get; }
        public IReadOnlyList<SlateTimelineEditorClipView> Clips { get; }
        public bool RuntimeActive { get; }
    }

    public sealed class SlateTimelineEditorSectionView
    {
        public SlateTimelineEditorSectionView(string sectionId, string displayName, int frame)
        {
            SectionId = sectionId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Frame = Mathf.Max(0, frame);
        }

        public string SectionId { get; }
        public string DisplayName { get; }
        public int Frame { get; }
    }

    public sealed class SlateTimelineEditorGroupView
    {
        public SlateTimelineEditorGroupView(
            string groupId,
            string displayName,
            bool isCollapsed,
            IReadOnlyList<SlateTimelineEditorTrackView> tracks,
            IReadOnlyList<SlateTimelineEditorSectionView> sections = null)
        {
            GroupId = groupId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            IsCollapsed = isCollapsed;
            Tracks = tracks ?? Array.Empty<SlateTimelineEditorTrackView>();
            Sections = sections ?? Array.Empty<SlateTimelineEditorSectionView>();
        }

        public string GroupId { get; }
        public string DisplayName { get; }
        public bool IsCollapsed { get; }
        public IReadOnlyList<SlateTimelineEditorTrackView> Tracks { get; }
        public IReadOnlyList<SlateTimelineEditorSectionView> Sections { get; }
    }

    public sealed class SlateTimelineEditorContentView
    {
        public SlateTimelineEditorContentView(
            string displayName,
            int frameRate,
            int lengthFrame,
            int viewStartFrame,
            int viewEndFrame,
            int currentFrame,
            IReadOnlyList<SlateTimelineEditorGroupView> groups,
            int runtimeFrame = -1,
            IReadOnlyCollection<string> activeTrackIds = null,
            IReadOnlyCollection<string> activeClipIds = null)
        {
            DisplayName = displayName ?? string.Empty;
            FrameRate = Mathf.Max(1, frameRate);
            LengthFrame = Mathf.Max(1, lengthFrame);
            ViewStartFrame = Mathf.Clamp(viewStartFrame, 0, LengthFrame);
            ViewEndFrame = Mathf.Max(ViewStartFrame + 1, Mathf.Min(LengthFrame, viewEndFrame));
            CurrentFrame = Mathf.Clamp(currentFrame, 0, LengthFrame);
            Groups = groups ?? Array.Empty<SlateTimelineEditorGroupView>();
            RuntimeFrame = runtimeFrame;
            ActiveTrackIds = activeTrackIds ?? Array.Empty<string>();
            ActiveClipIds = activeClipIds ?? Array.Empty<string>();
        }

        public string DisplayName { get; }
        public int FrameRate { get; }
        public int LengthFrame { get; }
        public int ViewStartFrame { get; }
        public int ViewEndFrame { get; }
        public int CurrentFrame { get; }
        public IReadOnlyList<SlateTimelineEditorGroupView> Groups { get; }
        public int RuntimeFrame { get; }
        public IReadOnlyCollection<string> ActiveTrackIds { get; }
        public IReadOnlyCollection<string> ActiveClipIds { get; }
    }

    public interface ISlateTimelineEditorHost
    {
        void RequestRepaint();
        void ShowNotification(string message);
        void SetTitle(string title);
    }

    public interface ISlateTimelineEditorCommandPort
    {
        void BeginGesture(string undoName);
        void CommitGesture();
        void CancelGesture();
        void SetCurrentFrame(int frame);
        void SetClipRange(string clipId, int startFrame, int endFrame, int blendInFrame, int blendOutFrame);
        void ReplaceCurve(string clipId, string curveId, AnimationCurve curve);
        void SetGroupCollapsed(string groupId, bool collapsed);
        void RequestAddTrack(int frame);
        void RequestAddClip(string trackId, int frame);
        void SetSectionFrame(string sectionId, int frame);
        void OpenSource(string clipId);
        void Select(SlateTimelineEditorSelection selection);
    }
}
#endif
