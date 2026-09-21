#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Slate
{
    public interface IEmbeddedTimelineBinding
    {
        string DisplayName { get; }
        int FrameRate { get; }
        float SnapTime(float time);
        string SnapLabel { get; }
        void ShowSnapSettings(Rect rect);
        float Length { get; }
        float CurrentTime { get; set; }
        bool DisplayFrames { get; set; }
        float SnapInterval { get; }
        float StepInterval { get; }
        float ViewTimeMin { get; set; }
        float ViewTimeMax { get; set; }
        bool IsReadOnly { get; }
        IReadOnlyList<IEmbeddedTimelineGroupBinding> Groups { get; }
        IReadOnlyList<IEmbeddedTimelineSectionBinding> Sections { get; }
        IEmbeddedTimelineElementBinding Selected { get; }
        bool TryGetTrack(string authoringId, out IEmbeddedTimelineTrackBinding track);
        bool TryGetClip(string authoringId, out IEmbeddedTimelineClipBinding clip);
        void Select(IEmbeddedTimelineElementBinding element);
        void AddTrack();
        void AddClip(IEmbeddedTimelineTrackBinding track, double time);
        void AddMarker(IEmbeddedTimelineTrackBinding track, double time);
        void DeleteMarker(IEmbeddedTimelineMarkerBinding marker);
        void MoveMarker(IEmbeddedTimelineMarkerBinding marker, double time);
        void OpenMarker(IEmbeddedTimelineMarkerBinding marker);
        void SetTrackActive(IEmbeddedTimelineTrackBinding track, bool active);
        void DeleteTrack(IEmbeddedTimelineTrackBinding track);
        void DeleteClip(IEmbeddedTimelineClipBinding clip);
        void DeleteClips(IReadOnlyList<IEmbeddedTimelineClipBinding> clips);
        void SplitClip(IEmbeddedTimelineClipBinding clip, double time);
        void MoveTrack(IEmbeddedTimelineTrackBinding track, int index);
        void ConfigureSection(IEmbeddedTimelineSectionBinding section, string name, double time);
        void DeleteSection(IEmbeddedTimelineSectionBinding section);
        void AddSection(double time);
        bool CanPasteClip { get; }
        void CopyClip(IEmbeddedTimelineClipBinding clip);
        void PasteClip(IEmbeddedTimelineTrackBinding track, double time);
        void OpenSource(IEmbeddedTimelineClipBinding clip);
        void BeginEdit(string undoName);
        void CommitEdit();
        void CancelEdit();
        void RequestRepaint();
    }

    public interface IEmbeddedTimelineElementBinding
    {
        string AuthoringId { get; }
        string DisplayName { get; }
        bool IsLocked { get; set; }
    }

    public interface IEmbeddedTimelineGroupBinding : IEmbeddedTimelineElementBinding
    {
        bool IsActive { get; set; }
        bool IsCollapsed { get; set; }
        IReadOnlyList<IEmbeddedTimelineTrackBinding> Tracks { get; }
    }

    public interface IEmbeddedTimelineTrackBinding : IEmbeddedTimelineElementBinding
    {
        bool IsActive { get; set; }
        bool ShowCurves { get; set; }
        float CustomHeight { get; set; }
        Color Color { get; }
        float StartTime { get; }
        float EndTime { get; }
        float DefaultHeight { get; }
        float ClipHeight { get; }
        bool AllowsParallelClips { get; }
        float FinalHeight { get; }
        float GetFinalHeight(string inspectedParameterId);
        IReadOnlyList<IEmbeddedTimelineClipBinding> Clips { get; }
        IEmbeddedTimelineClipBinding SelectedClip { get; }
    }

    public interface IEmbeddedTimelineMarkerBinding : IEmbeddedTimelineElementBinding
    {
        IEmbeddedTimelineTrackBinding Track { get; }
        double Time { get; }
    }

    public interface IEmbeddedTimelineMarkerTrackBinding : IEmbeddedTimelineTrackBinding
    {
        IReadOnlyList<IEmbeddedTimelineMarkerBinding> Markers { get; }
    }

    public interface IEmbeddedTimelineClipBinding : IEmbeddedTimelineElementBinding
    {
        IEmbeddedTimelineTrackBinding Track { get; }
        string Info { get; }
        int LaneIndex { get; }
        bool IsActive { get; }
        bool IsTimeQuantized { get; }
        bool IsValid { get; }
        bool IsCollapsed { get; set; }
        float StartTime { get; set; }
        float EndTime { get; set; }
        float Length { get; }
        float BlendIn { get; set; }
        float BlendOut { get; set; }
        bool CanScale { get; }
        bool CanClipIn { get; }
        double ClipInTime { get; set; }
        bool CanBlendIn { get; }
        bool CanBlendOut { get; }
        IReadOnlyList<IEmbeddedTimelineParameterBinding> Parameters { get; }
        IReadOnlyList<IEmbeddedTimelineCurveBinding> Curves { get; }
        bool CanCrossBlend(IEmbeddedTimelineClipBinding other);
        void AddIdentityKey(float time);
        void Split(float time);
        void StretchFit();
        void CleanKeysOffRange();
        void ResetAnimation();
    }

    public interface IEmbeddedTimelineParameterBinding
    {
        string ParameterId { get; }
        string DisplayName { get; }
        string ValueDomainSummary { get; }
        bool Enabled { get; set; }
        float CurrentValue { get; }
        IReadOnlyList<IEmbeddedTimelineCurveBinding> Curves { get; }
        void AddKey(float localTime);
        void RemoveKey(float localTime);
        void SelectPreviousKey(float localTime);
        void SelectNextKey(float localTime);
    }

    public interface IEmbeddedTimelineReferenceParameterBinding
    {
        string ReferenceLabel { get; }
    }

    public interface IEmbeddedTimelineSourceRangeBinding
    {
        void AdjustSourceRange(double originalStartTime, double currentStartTime, double currentEndTime, bool trimStart);
    }

    public interface IEmbeddedTimelineCurveBinding
    {
        string ChannelId { get; }
        string DisplayName { get; }
        AnimationCurve Curve { get; }
        float Duration { get; }
        void Replace(AnimationCurve curve);
    }

    public interface IEmbeddedTimelineSectionBinding : IEmbeddedTimelineElementBinding
    {
        string Name { get; set; }
        float Time { get; set; }
        Color Color { get; }
    }

    interface IClipEditorBinding
    {
        string AuthoringId { get; }
        string Info { get; }
        ActionClip NativeAction { get; }
        IEmbeddedTimelineClipBinding FormalClip { get; }
        IEmbeddedTimelineTrackBinding Track { get; }
        bool IsCollapsed { get; set; }
        bool IsLocked { get; set; }
        bool IsValid { get; }
        bool IsTimeQuantized { get; }
        bool HasParameters { get; }
        bool HasActiveParameters { get; }
        bool CanScale { get; }
        bool CanBlendIn { get; }
        bool CanBlendOut { get; }
        float StartTime { get; set; }
        float EndTime { get; set; }
        float Length { get; }
        float BlendIn { get; set; }
        float BlendOut { get; set; }
        AnimationCurve[] Curves { get; }
        bool CanCrossBlend(IClipEditorBinding other);
        void DrawClipGUI(Rect rect);
        void DrawClipGUIExternal(Rect leftRect, Rect rightRect);
    }

    sealed class NativeClipEditorBinding : IClipEditorBinding
    {
        public NativeClipEditorBinding(ActionClip source)
        {
            NativeAction = source;
        }

        public ActionClip NativeAction { get; }
        public IEmbeddedTimelineClipBinding FormalClip => null;
        public string AuthoringId => NativeAction.GetInstanceID().ToString();
        public string Info => NativeAction.info;
        public IEmbeddedTimelineTrackBinding Track => null;
        public bool IsTimeQuantized => true;
        public bool IsCollapsed { get => NativeAction.isCollapsed; set { } }
        public bool IsLocked { get => NativeAction.isLocked; set { } }
        public bool IsValid => NativeAction.isValid;
        public bool HasParameters => NativeAction.hasParameters;
        public bool HasActiveParameters => NativeAction.hasActiveParameters;
        public bool CanScale => NativeAction.CanScale();
        public bool CanBlendIn => NativeAction.CanBlendIn();
        public bool CanBlendOut => NativeAction.CanBlendOut();
        public float StartTime { get => NativeAction.startTime; set => NativeAction.startTime = value; }
        public float EndTime { get => NativeAction.endTime; set => NativeAction.endTime = value; }
        public float Length => NativeAction.length;
        public float BlendIn { get => NativeAction.blendIn; set => NativeAction.blendIn = value; }
        public float BlendOut { get => NativeAction.blendOut; set => NativeAction.blendOut = value; }
        public AnimationCurve[] Curves => NativeAction.GetCurvesAll();
        public bool CanCrossBlend(IClipEditorBinding other) => other?.NativeAction != null && NativeAction.CanCrossBlend(other.NativeAction);
        public void DrawClipGUI(Rect rect) => NativeAction.ShowClipGUI(rect);
        public void DrawClipGUIExternal(Rect leftRect, Rect rightRect) => NativeAction.ShowClipGUIExternal(leftRect, rightRect);
    }

    sealed class FormalClipEditorBinding : IClipEditorBinding
    {
        public FormalClipEditorBinding(IEmbeddedTimelineClipBinding source)
        {
            FormalClip = source ?? throw new ArgumentNullException(nameof(source));
        }

        public ActionClip NativeAction => null;
        public IEmbeddedTimelineClipBinding FormalClip { get; }
        public string AuthoringId => FormalClip.AuthoringId;
        public string Info => FormalClip.Info;
        public IEmbeddedTimelineTrackBinding Track => FormalClip.Track;
        public bool IsCollapsed { get => FormalClip.IsCollapsed; set => FormalClip.IsCollapsed = value; }
        public bool IsLocked { get => FormalClip.IsLocked; set => FormalClip.IsLocked = value; }
        public bool IsValid => FormalClip.IsValid;
        public bool HasParameters => FormalClip.Parameters != null && FormalClip.Parameters.Count != 0;
        public bool HasActiveParameters
        {
            get
            {
                if (!HasParameters)
                    return false;
                for (int index = 0; index < FormalClip.Parameters.Count; index++)
                    if (FormalClip.Parameters[index].Enabled)
                        return true;
                return false;
            }
        }
        public bool CanScale => FormalClip.CanScale;
        public bool IsTimeQuantized => FormalClip.IsTimeQuantized;
        public bool CanBlendIn => FormalClip.CanBlendIn;
        public bool CanBlendOut => FormalClip.CanBlendOut;
        public float StartTime { get => FormalClip.StartTime; set => FormalClip.StartTime = value; }
        public float EndTime { get => FormalClip.EndTime; set => FormalClip.EndTime = value; }
        public float Length => FormalClip.Length;
        public float BlendIn { get => FormalClip.BlendIn; set => FormalClip.BlendIn = value; }
        public float BlendOut { get => FormalClip.BlendOut; set => FormalClip.BlendOut = value; }
        public AnimationCurve[] Curves
        {
            get
            {
                if (FormalClip.Curves == null)
                    return Array.Empty<AnimationCurve>();
                var curves = new AnimationCurve[FormalClip.Curves.Count];
                for (int index = 0; index < curves.Length; index++)
                    curves[index] = FormalClip.Curves[index].Curve;
                return curves;
            }
        }
        public bool CanCrossBlend(IClipEditorBinding other) => other?.FormalClip != null && FormalClip.CanCrossBlend(other.FormalClip);
        public void DrawClipGUI(Rect rect)
        {
        }
        public void DrawClipGUIExternal(Rect leftRect, Rect rightRect) { }
    }
}
#endif
