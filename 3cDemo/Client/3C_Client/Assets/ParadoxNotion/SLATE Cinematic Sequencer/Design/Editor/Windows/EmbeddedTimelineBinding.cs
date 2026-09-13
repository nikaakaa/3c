#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Slate
{
    public interface IEmbeddedTimelineProxyIdentity
    {
        string AuthoringId { get; }
    }

    public interface IEmbeddedTimelineBinding
    {
        string DisplayName { get; }
        int FrameRate { get; }
        float Length { get; }
        int CurrentFrame { get; set; }
        float ViewTimeMin { get; set; }
        float ViewTimeMax { get; set; }
        bool IsReadOnly { get; }
        IReadOnlyList<IEmbeddedTimelineGroupBinding> Groups { get; }
        IReadOnlyList<IEmbeddedTimelineSectionBinding> Sections { get; }
        IEmbeddedTimelineElementBinding Selected { get; }
        bool TryGetTrack(string authoringId, out IEmbeddedTimelineTrackBinding track);
        void Select(IEmbeddedTimelineElementBinding element);
        void AddTrack();
        void AddClip(IEmbeddedTimelineTrackBinding track, int frame);
        void DeleteTrack(IEmbeddedTimelineTrackBinding track);
        void DeleteClip(IEmbeddedTimelineClipBinding clip);
        void DeleteClips(IReadOnlyList<IEmbeddedTimelineClipBinding> clips);
        void SplitClip(IEmbeddedTimelineClipBinding clip, int frame);
        void MoveTrack(IEmbeddedTimelineTrackBinding track, int index);
        void MoveClip(IEmbeddedTimelineClipBinding clip, int startFrame);
        void ConfigureSection(IEmbeddedTimelineSectionBinding section, string name, int frame);
        void DeleteSection(IEmbeddedTimelineSectionBinding section);
        void AddSection(int frame);
        void CopyClip(IEmbeddedTimelineClipBinding clip);
        void PasteClip(IEmbeddedTimelineTrackBinding track, int frame);
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
        Color Color { get; }
        float StartTime { get; }
        float EndTime { get; }
        float DefaultHeight { get; }
        float FinalHeight { get; }
        IReadOnlyList<IEmbeddedTimelineClipBinding> Clips { get; }
        IEmbeddedTimelineClipBinding SelectedClip { get; }
    }

    public interface IEmbeddedTimelineClipBinding : IEmbeddedTimelineElementBinding
    {
        string Info { get; }
        bool IsActive { get; }
        bool IsValid { get; }
        bool IsCollapsed { get; set; }
        float StartTime { get; set; }
        float EndTime { get; set; }
        float Length { get; }
        float BlendIn { get; set; }
        float BlendOut { get; set; }
        bool CanScale { get; }
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
        bool Enabled { get; set; }
        float CurrentValue { get; }
        IReadOnlyList<IEmbeddedTimelineCurveBinding> Curves { get; }
        void AddKey(float localTime);
        void RemoveKey(float localTime);
        void SelectPreviousKey(float localTime);
        void SelectNextKey(float localTime);
    }

    public interface IEmbeddedTimelineCurveBinding
    {
        string ChannelId { get; }
        string DisplayName { get; }
        AnimationCurve Curve { get; }
        int StartFrame { get; }
        int EndFrame { get; }
        float Duration { get; }
        void Replace(AnimationCurve curve);
    }

    public interface IEmbeddedTimelineSectionBinding : IEmbeddedTimelineElementBinding
    {
        string Name { get; set; }
        float Time { get; set; }
        Color Color { get; }
    }
}
#endif
