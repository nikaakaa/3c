using System;
using System.Collections.Generic;
using ThirdPersonCamera;
using UnityEngine;

namespace BTSMTL.Timeline
{
    public readonly struct TimelineCameraEffectSample
    {
        public TimelineCameraEffectSample(
            string sourceId,
            string sourceName,
            string trackName,
            CameraEffectKind kind,
            string resourceId,
            float weight,
            int priority)
        {
            SourceId = sourceId ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            TrackName = trackName ?? string.Empty;
            Kind = kind;
            ResourceId = resourceId ?? string.Empty;
            Weight = Mathf.Max(0f, weight);
            Priority = priority;
        }

        public string SourceId { get; }
        public string SourceName { get; }
        public string TrackName { get; }
        public CameraEffectKind Kind { get; }
        public string ResourceId { get; }
        public float Weight { get; }
        public int Priority { get; }
    }

    public abstract class CameraResourceTrack : Track
    {
        protected void SampleClip(
            float timelineTime,
            string sourceId,
            string sourceName,
            CameraEffectKind kind,
            string resourceId,
            int priority,
            AnimationCurve weightCurve,
            AnimationCurve easeInCurve,
            AnimationCurve easeOutCurve,
            float easeInTime,
            float easeOutTime,
            ICollection<TimelineCameraEffectSample> samples)
        {
            if (timelineTime < 0f || samples == null || string.IsNullOrWhiteSpace(resourceId))
                return;
            float duration = Mathf.Max(0.0001f, DurationTime);
            float normalizedTime = Mathf.Clamp01(timelineTime / duration);
            float weight = CameraTimelineSampling.SampleWeight(
                weightCurve,
                easeInCurve,
                easeOutCurve,
                normalizedTime,
                timelineTime,
                Mathf.Max(0f, duration - timelineTime),
                easeInTime,
                easeOutTime);
            if (weight <= 0f)
                return;
            samples.Add(new TimelineCameraEffectSample(
                sourceId,
                sourceName,
                Name,
                kind,
                resourceId,
                weight,
                priority));
        }

        float DurationTime => Clips.Count == 0
            ? 0f
            : Clips[Clips.Count - 1].EndTime;
    }

    [TrackGroup("Camera"), ScriptGuid("de0a9b796b3c4d1a8f5e02af91d63c74"), Ordered(7), Color(255, 196, 130)]
    public sealed class CameraOverrideTrack : CameraResourceTrack
    {
        public void Sample(float timelineTime, string sourceId, string sourceName, ICollection<TimelineCameraEffectSample> samples)
        {
            if (m_PersistentMuted || samples == null)
                return;
            foreach (Clip value in Clips)
            {
                if (!(value is CameraOverrideClip clip) || !clip.OverrideTrack)
                    continue;
                SampleClip(
                    timelineTime,
                    sourceId,
                    sourceName,
                    CameraEffectKind.Override,
                    clip.OverrideTrack.TrackId,
                    clip.OverrideTrack.Priority,
                    clip.WeightCurve,
                    clip.EaseInCurve,
                    clip.EaseOutCurve,
                    clip.EaseInTime,
                    clip.EaseOutTime,
                    samples);
            }
        }

#if UNITY_EDITOR
        public override Type ClipType => typeof(CameraOverrideClip);
#endif
    }

    [TrackGroup("Camera"), ScriptGuid("5f5bb8b56d0d4a49b9d7b31db7dd2c10"), Ordered(8), Color(255, 210, 130)]
    public sealed class CameraZoomTrack : CameraResourceTrack
    {
        public void Sample(float timelineTime, string sourceId, string sourceName, ICollection<TimelineCameraEffectSample> samples)
        {
            if (m_PersistentMuted || samples == null)
                return;
            foreach (Clip value in Clips)
            {
                if (!(value is CameraZoomClip clip) || !clip.Zoom)
                    continue;
                SampleClip(
                    timelineTime,
                    sourceId,
                    sourceName,
                    CameraEffectKind.Zoom,
                    clip.Zoom.ZoomId,
                    clip.Zoom.DataPriority,
                    clip.WeightCurve,
                    clip.EaseInCurve,
                    clip.EaseOutCurve,
                    clip.EaseInTime,
                    clip.EaseOutTime,
                    samples);
            }
        }

#if UNITY_EDITOR
        public override Type ClipType => typeof(CameraZoomClip);
#endif
    }

    [TrackGroup("Camera"), ScriptGuid("f0e09aaf6ec44896b63ac2ad7e2661e4"), Ordered(9), Color(255, 180, 130)]
    public sealed class CameraStretchTrack : CameraResourceTrack
    {
        public void Sample(float timelineTime, string sourceId, string sourceName, ICollection<TimelineCameraEffectSample> samples)
        {
            if (m_PersistentMuted || samples == null)
                return;
            foreach (Clip value in Clips)
            {
                if (!(value is CameraStretchClip clip) || !clip.Stretch)
                    continue;
                SampleClip(
                    timelineTime,
                    sourceId,
                    sourceName,
                    CameraEffectKind.Stretch,
                    clip.Stretch.StretchId,
                    clip.Stretch.DataPriority,
                    clip.WeightCurve,
                    clip.EaseInCurve,
                    clip.EaseOutCurve,
                    clip.EaseInTime,
                    clip.EaseOutTime,
                    samples);
            }
        }

#if UNITY_EDITOR
        public override Type ClipType => typeof(CameraStretchClip);
#endif
    }

    [TrackGroup("Camera"), ScriptGuid("a7dc7e5292c84b4584317a9f4f071f6d"), Ordered(10), Color(220, 180, 255)]
    public sealed class CameraShotTrack : CameraResourceTrack
    {
        public void Sample(float timelineTime, string sourceId, string sourceName, ICollection<TimelineCameraEffectSample> samples)
        {
            if (m_PersistentMuted || samples == null)
                return;
            foreach (Clip value in Clips)
            {
                if (!(value is CameraShotClip clip) || !clip.Shot)
                    continue;
                SampleClip(
                    timelineTime,
                    sourceId,
                    sourceName,
                    CameraEffectKind.Shot,
                    clip.Shot.ShotId,
                    clip.Shot.Priority,
                    clip.WeightCurve,
                    clip.EaseInCurve,
                    clip.EaseOutCurve,
                    clip.EaseInTime,
                    clip.EaseOutTime,
                    samples);
            }
        }

#if UNITY_EDITOR
        public override Type ClipType => typeof(CameraShotClip);
#endif
    }

    public abstract class CameraResourceClip : Clip
    {
        public AnimationCurve WeightCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        public AnimationCurve EaseInCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public AnimationCurve EaseOutCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public float EaseInTime;
        public float EaseOutTime;

#if UNITY_EDITOR
        protected CameraResourceClip(Track track, int frame) : base(track, frame)
        {
        }
#endif
    }

    [ScriptGuid("de0a9b796b3c4d1a8f5e02af91d63c74"), Color(255, 196, 130)]
    public sealed class CameraOverrideClip : CameraResourceClip
    {
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public CameraOverrideTrackAsset OverrideTrack;

#if UNITY_EDITOR
        public override ClipCapabilities Capabilities => ClipCapabilities.Resizable | ClipCapabilities.Mixable;
        public CameraOverrideClip(Track track, int frame) : base(track, frame) { }
#endif
    }

    [ScriptGuid("5f5bb8b56d0d4a49b9d7b31db7dd2c10"), Color(255, 210, 130)]
    public sealed class CameraZoomClip : CameraResourceClip
    {
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public CameraZoomAsset Zoom;

#if UNITY_EDITOR
        public override ClipCapabilities Capabilities => ClipCapabilities.Resizable | ClipCapabilities.Mixable;
        public CameraZoomClip(Track track, int frame) : base(track, frame) { }
#endif
    }

    [ScriptGuid("f0e09aaf6ec44896b63ac2ad7e2661e4"), Color(255, 180, 130)]
    public sealed class CameraStretchClip : CameraResourceClip
    {
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public CameraStretchAsset Stretch;

#if UNITY_EDITOR
        public override ClipCapabilities Capabilities => ClipCapabilities.Resizable | ClipCapabilities.Mixable;
        public CameraStretchClip(Track track, int frame) : base(track, frame) { }
#endif
    }

    [ScriptGuid("a7dc7e5292c84b4584317a9f4f071f6d"), Color(220, 180, 255)]
    public sealed class CameraShotClip : CameraResourceClip
    {
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public CameraShotAsset Shot;

#if UNITY_EDITOR
        public override ClipCapabilities Capabilities => ClipCapabilities.Resizable | ClipCapabilities.Mixable;
        public CameraShotClip(Track track, int frame) : base(track, frame) { }
#endif
    }
}
