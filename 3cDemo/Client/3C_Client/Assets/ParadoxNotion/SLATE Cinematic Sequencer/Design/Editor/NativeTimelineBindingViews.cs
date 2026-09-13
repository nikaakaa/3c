#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Slate
{
    sealed class NativeTimelineGroupBinding : IEmbeddedTimelineGroupBinding
    {
        readonly List<IEmbeddedTimelineTrackBinding> m_Tracks = new List<IEmbeddedTimelineTrackBinding>();
        public NativeTimelineGroupBinding(CutsceneGroup source)
        {
            Source = source;
            for (int index = 0; index < source.tracks.Count; index++)
                m_Tracks.Add(new NativeTimelineTrackBinding(source.tracks[index], this));
        }

        public CutsceneGroup Source { get; }
        public string AuthoringId => $"slate-group:{Source.GetInstanceID()}";
        public string DisplayName => Source.name;
        public bool IsActive { get => Source.isActive; set => Source.isActive = value; }
        public bool IsCollapsed { get => Source.isCollapsed; set => Source.isCollapsed = value; }
        public bool IsLocked { get => Source.isLocked; set => Source.isLocked = value; }
        public IReadOnlyList<IEmbeddedTimelineTrackBinding> Tracks => m_Tracks;
    }

    sealed class NativeTimelineTrackBinding : IEmbeddedTimelineTrackBinding
    {
        readonly List<IEmbeddedTimelineClipBinding> m_Clips = new List<IEmbeddedTimelineClipBinding>();
        readonly NativeTimelineGroupBinding m_Group;

        public NativeTimelineTrackBinding(CutsceneTrack source, NativeTimelineGroupBinding group)
        {
            Source = source;
            m_Group = group;
            for (int index = 0; index < source.clips.Count; index++)
                m_Clips.Add(new NativeTimelineClipBinding(source.clips[index], this));
        }

        public CutsceneTrack Source { get; }
        public string AuthoringId => $"slate-track:{Source.GetInstanceID()}";
        public string DisplayName => Source.name;
        public bool IsActive { get => Source.isActive; set => Source.isActive = value; }
        public bool IsLocked { get => Source.isLocked; set => Source.isLocked = value; }
        public bool ShowCurves { get => Source.showCurves; set => Source.showCurves = value; }
        public Color Color => Source.color;
        public float StartTime => Source.startTime;
        public float EndTime => Source.endTime;
        public float DefaultHeight => Source.defaultHeight;
        public float FinalHeight => Source.finalHeight;
        public IReadOnlyList<IEmbeddedTimelineClipBinding> Clips => m_Clips;
        public IEmbeddedTimelineClipBinding SelectedClip =>
            m_Clips.FirstOrDefault(clip => clip is NativeTimelineClipBinding native &&
                                            ReferenceEquals(CutsceneUtility.selectedObject, native.Source));
    }

    sealed class NativeTimelineClipBinding : IEmbeddedTimelineClipBinding
    {
        public NativeTimelineClipBinding(ActionClip source, NativeTimelineTrackBinding track)
        {
            Source = source;
            Track = track;
        }

        public ActionClip Source { get; }
        public NativeTimelineTrackBinding Track { get; }
        IEmbeddedTimelineTrackBinding IEmbeddedTimelineClipBinding.Track => Track;
        public string AuthoringId => $"slate-clip:{Source.GetInstanceID()}";
        public string DisplayName => Source.info;
        public string Info => Source.info;
        public bool IsActive => Source.isActive;
        public bool IsValid => Source.isValid;
        public bool IsCollapsed { get => Source.isCollapsed; set { } }
        public bool IsLocked { get => Source.isLocked; set { } }
        public float StartTime { get => Source.startTime; set => Source.startTime = value; }
        public float EndTime { get => Source.endTime; set => Source.endTime = value; }
        public float Length => Source.length;
        public float BlendIn { get => Source.blendIn; set => Source.blendIn = value; }
        public float BlendOut { get => Source.blendOut; set => Source.blendOut = value; }
        public bool CanScale => Source.CanScale();
        public bool CanBlendIn => Source.CanBlendIn();
        public bool CanBlendOut => Source.CanBlendOut();
        public IReadOnlyList<IEmbeddedTimelineParameterBinding> Parameters => Array.Empty<IEmbeddedTimelineParameterBinding>();
        public IReadOnlyList<IEmbeddedTimelineCurveBinding> Curves => Array.Empty<IEmbeddedTimelineCurveBinding>();
        public bool CanCrossBlend(IEmbeddedTimelineClipBinding other) =>
            other is NativeTimelineClipBinding native && Source.CanCrossBlend(native.Source);
        public void AddIdentityKey(float time) => Source.TryAddIdentityKey(Source.ToLocalTime(time));
        public void Split(float time) { }
        public void StretchFit() { }
        public void CleanKeysOffRange()
        {
            if (!Source.hasParameters)
                return;
            foreach (AnimatedParameter parameter in Source.animationData.animatedParameters)
            {
                if (parameter.HasAnyKey())
                {
                    if (parameter.GetKeyPrevious(0f) < 0f)
                        parameter.TryKeyIdentity(0f);
                    if (parameter.GetKeyNext(Source.length) > Source.length)
                        parameter.TryKeyIdentity(Source.length);
                }
                foreach (AnimationCurve curve in parameter.curves ?? Array.Empty<AnimationCurve>())
                {
                    curve.RemoveKeysOffRange(0f, Source.length);
                    curve.UpdateTangentsFromMode();
                }
            }
        }
        public void ResetAnimation() => Source.ResetAnimatedParameters();
    }

    sealed class NativeTimelineSectionBinding : IEmbeddedTimelineSectionBinding
    {
        public NativeTimelineSectionBinding(Section source)
        {
            Source = source;
        }

        public Section Source { get; }
        public string AuthoringId => $"slate-section:{Source.UID}";
        public string DisplayName => Source.name;
        public bool IsLocked { get; set; }
        public string Name { get => Source.name; set => Source.name = value; }
        public float Time { get => Source.time; set => Source.time = value; }
        public Color Color => Source.color;
    }
}
#endif
