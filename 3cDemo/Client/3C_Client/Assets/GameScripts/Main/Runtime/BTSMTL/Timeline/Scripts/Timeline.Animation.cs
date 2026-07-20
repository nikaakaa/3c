using System;
using System.Collections.Generic;
using UnityEngine;

namespace BTSMTL.Timeline
{
    public readonly struct TimelineAnimationContribution
    {
        public TimelineAnimationContribution(
            int trackIndex,
            int clipIndex,
            string timelineAuthoringId,
            string trackAuthoringId,
            string clipAuthoringId,
            string sourceId,
            string sourceName,
            string trackName,
            UnityEngine.AnimationClip clip,
            string layerId,
            float clipTime,
            float normalizedTime,
            float weight,
            bool isLooping,
            float clipLoopStartTime,
            float clipLoopDuration,
            int cycleIndex)
        {
            TrackIndex = trackIndex;
            ClipIndex = clipIndex;
            TimelineAuthoringId = timelineAuthoringId ?? string.Empty;
            TrackAuthoringId = trackAuthoringId ?? string.Empty;
            ClipAuthoringId = clipAuthoringId ?? string.Empty;
            SourceId = sourceId;
            SourceName = sourceName;
            TrackName = trackName;
            Clip = clip;
            LayerId = layerId;
            ClipTime = clipTime;
            NormalizedTime = normalizedTime;
            Weight = weight;
            IsLooping = isLooping && clipLoopDuration > 0f;
            ClipLoopStartTime = clipLoopStartTime;
            ClipLoopDuration = Mathf.Max(0f, clipLoopDuration);
            ContinuousClipTime = IsLooping ? clipTime + Mathf.Max(0, cycleIndex) * ClipLoopDuration : clipTime;
        }

        public int TrackIndex { get; }
        public int ClipIndex { get; }
        public string TimelineAuthoringId { get; }
        public string TrackAuthoringId { get; }
        public string ClipAuthoringId { get; }
        public string SourceId { get; }
        public string SourceName { get; }
        public string TrackName { get; }
        public UnityEngine.AnimationClip Clip { get; }
        public string LayerId { get; }
        public float ClipTime { get; }
        public float NormalizedTime { get; }
        public float Weight { get; }
        public bool IsLooping { get; }
        public float ClipLoopStartTime { get; }
        public float ClipLoopDuration { get; }
        public float ContinuousClipTime { get; }
    }

    [TrackGroup("Base"), ScriptGuid("3f0d14cafa6f2c84389c42789ec00083"), IconGuid("e6435fa591ae4414eb0f26dc6410086e"), Ordered(0), Color(127, 253, 228)]
    public partial class AnimationTrack : Track, ITimelineTrackOwnedAuthoringIdentity
    {
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public string LayerId = "Base";

        public void Sample(float timelineTime, int trackIndex, string sourceId, string sourceName, ICollection<TimelineAnimationContribution> contributions)
        {
            Sample(timelineTime, timelineTime, trackIndex, sourceId, sourceName, contributions);
        }

        public void Sample(
            float previousTimelineTime,
            float timelineTime,
            int trackIndex,
            string sourceId,
            string sourceName,
            ICollection<TimelineAnimationContribution> animationContributions)
        {
            Sample(previousTimelineTime, timelineTime, trackIndex, sourceId, sourceName, animationContributions, false, 0);
        }

        public void Sample(
            float previousTimelineTime,
            float timelineTime,
            int trackIndex,
            string sourceId,
            string sourceName,
            ICollection<TimelineAnimationContribution> animationContributions,
            bool isLooping,
            int cycleIndex)
        {
            if (m_PersistentMuted)
                return;

            for (int clipIndex = 0; clipIndex < Clips.Count; clipIndex++)
            {
                Clip clip = Clips[clipIndex];
                if (clip is not AnimationClip animationClip || !animationClip.Clip)
                    continue;

                if (!TrySampleClip(animationClip, timelineTime, out float clipTime, out float normalizedTime, out float weight))
                    continue;

                animationContributions?.Add(new TimelineAnimationContribution(
                    trackIndex,
                    clipIndex,
                    Timeline != null ? Timeline.AuthoringId : string.Empty,
                    AuthoringId,
                    animationClip.AuthoringId,
                    sourceId,
                    sourceName,
                    Name,
                    animationClip.Clip,
                    LayerId,
                    clipTime,
                    normalizedTime,
                    weight,
                    isLooping,
                    animationClip.ClipInTime,
                    animationClip.DurationTime,
                    cycleIndex));
            }
        }

        static bool TrySampleClip(AnimationClip clip, float timelineTime, out float clipTime, out float normalizedTime, out float weight)
        {
            clipTime = 0f;
            normalizedTime = 0f;
            weight = 0f;

            if (timelineTime < clip.StartTime)
                return false;

            bool hold = timelineTime > clip.EndTime && clip.ExtraPolationMode == ExtraPolationMode.Hold;
            if (timelineTime > clip.EndTime && !hold)
                return false;

            float duration = Mathf.Max(0.0001f, clip.DurationTime);
            float selfTime = hold ? clip.DurationTime : Mathf.Clamp(timelineTime - clip.StartTime, 0f, clip.DurationTime);
            float remainTime = Mathf.Max(0f, clip.EndTime - timelineTime);
            normalizedTime = Mathf.Clamp01(selfTime / duration);
            clipTime = selfTime + clip.ClipInTime;

            float fadeInWeight = 1f;
            if (!hold && clip.EaseInTime > 0f && selfTime < clip.EaseInTime)
                fadeInWeight = EvaluateCurve(clip.EaseInCurve, Mathf.Clamp01(selfTime / clip.EaseInTime), 1f);

            float fadeOutWeight = 1f;
            if (!hold && clip.EaseOutTime > 0f && remainTime < clip.EaseOutTime)
                fadeOutWeight = 1f - EvaluateCurve(clip.EaseOutCurve, Mathf.Clamp01(1f - remainTime / clip.EaseOutTime), 0f);

            float curveWeight = EvaluateCurve(clip.WeightCurve, normalizedTime, 1f);
            weight = Mathf.Clamp01(curveWeight * fadeInWeight * fadeOutWeight);
            return weight > 0f;
        }

        static float EvaluateCurve(AnimationCurve curve, float time, float fallback)
        {
            return curve != null && curve.length > 0 ? curve.Evaluate(time) : fallback;
        }

#if UNITY_EDITOR

        public override Type ClipType => typeof(AnimationClip);
        public override Clip AddClip(UnityEngine.Object referenceObject, int frame)
        {
            AnimationClip clip = new AnimationClip(referenceObject as UnityEngine.AnimationClip, this, frame);
            clip.RegenerateAuthoringIdentity();
            m_Clips.Add(clip);
            return clip;
        }
        public override bool DragValid()
        {
            return UnityEditor.DragAndDrop.objectReferences.Length == 1 && UnityEditor.DragAndDrop.objectReferences[0] as UnityEngine.AnimationClip;
        }
#endif
    }

    [ScriptGuid("3f0d14cafa6f2c84389c42789ec00083"), Color(127, 253, 228)]
    public partial class AnimationClip : Clip
    {
        [ShowInInspector, OnValueChanged("OnClipChanged", "RebindTimeline")]
        public UnityEngine.AnimationClip Clip;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public ExtraPolationMode ExtraPolationMode;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public AnimationCurve WeightCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public AnimationCurve EaseInCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public AnimationCurve EaseOutCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [ShowInInspector, Group("Foot Placement"), OnValueChanged("RebindTimeline"), InspectorName("Foot Placement Weight")]
        public AnimationCurve FootPlacementCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        public void RequireFootPlacementWeightCurve()
        {
            RequireNormalizedCurve(FootPlacementCurve, nameof(FootPlacementCurve));
        }

#if UNITY_EDITOR
        public void ConfigureFootPlacementWeightCurve(AnimationCurve curve)
        {
            FootPlacementCurve = CopyNormalizedCurve(curve, nameof(FootPlacementCurve));
            RebindTimeline();
        }

        static AnimationCurve CopyNormalizedCurve(AnimationCurve source, string field)
        {
            RequireNormalizedCurve(source, field);
            return new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
        }
#endif

        static void RequireNormalizedCurve(AnimationCurve curve, string field)
        {
            if (curve == null || curve.length == 0)
                throw new InvalidOperationException($"Animation Clip '{field}' requires at least one key.");
            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                if (!IsNormalized(key.time) || !IsNormalized(key.value) ||
                    i > 0 && key.time < keys[i - 1].time)
                    throw new InvalidOperationException($"Animation Clip '{field}' key {i} is outside normalized ordered range.");
            }
        }

        static bool IsNormalized(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f && value <= 1f;

#if UNITY_EDITOR

        public override string Name => Clip ? Clip.name : base.Name;
        public override int Length => Clip ? Mathf.RoundToInt(Clip.length * TimelineUtility.FrameRate) : base.Length;
        public override ClipCapabilities Capabilities => ClipCapabilities.Resizable | ClipCapabilities.Mixable | ClipCapabilities.ClipInable;
        public AnimationClip(Track track, int frame) : base(track, frame) { }
        public AnimationClip(UnityEngine.AnimationClip clip, Track track, int frame) : base(track, frame)
        {
            Clip = clip;
            EndFrame = Length + frame;
        }
        void OnClipChanged()
        {
            OnNameChanged?.Invoke();
        }
#endif
    }
}
