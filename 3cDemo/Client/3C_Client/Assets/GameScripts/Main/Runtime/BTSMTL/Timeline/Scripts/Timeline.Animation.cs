using System;
using System.Collections.Generic;
using ThirdPersonSimulation;
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
            AnimationChannelId animationChannelId,
            string animationSlotId,
            string blendProfileId,
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
            Clip = clip ? clip : throw new ArgumentNullException(nameof(clip));
            AnimationChannelId = animationChannelId.IsValid
                ? animationChannelId
                : throw new ArgumentException("Animation Channel identity is invalid.", nameof(animationChannelId));
            AnimationSlotId = animationSlotId?.Trim() ?? string.Empty;
            BlendProfileId = blendProfileId?.Trim() ?? string.Empty;
            ClipTime = clipTime;
            NormalizedTime = normalizedTime;
            Weight = weight;
            IsLooping = isLooping && clipLoopDuration > 0f;
            ClipLoopStartTime = clipLoopStartTime;
            ClipLoopDuration = Mathf.Max(0f, clipLoopDuration);
            Cycle = Mathf.Max(0, cycleIndex);
            ContinuousClipTime = IsLooping ? clipTime + Cycle * ClipLoopDuration : clipTime;
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
        public AnimationChannelId AnimationChannelId { get; }
        public string AnimationSlotId { get; }
        public string BlendProfileId { get; }
        public float ClipTime { get; }
        public float NormalizedTime { get; }
        public float Weight { get; }
        public bool IsLooping { get; }
        public float ClipLoopStartTime { get; }
        public float ClipLoopDuration { get; }
        public int Cycle { get; }
        public float ContinuousClipTime { get; }
    }

    [TrackGroup("Base"), ScriptGuid("3f0d14cafa6f2c84389c42789ec00083"), IconGuid("e6435fa591ae4414eb0f26dc6410086e"), Ordered(0), Color(127, 253, 228)]
    [TimelineAuthoringTrackField("animationChannelId", "skill_timeline_animation_channel_invalid", "AnimationTrack必须声明稳定AnimationChannel identity。")]
    [TimelineAuthoringTrackField("animationSlotId", "skill_timeline_animation_slot_invalid", "AnimationTrack必须声明稳定AnimationSlot identity。")]
    public partial class AnimationTrack : Track, ITimelineAuthoringTrackFieldSink
    {
        public override string ContractKind => TimelineContractKinds.AnimationTrack;

        [SerializeField, ShowInInspector, OnValueChanged("RebindTimeline")]
        string m_AnimationChannelId = string.Empty;
        [SerializeField, ShowInInspector, OnValueChanged("RebindTimeline")]
        string m_AnimationSlotId = string.Empty;

        public AnimationChannelId AnimationChannelId => string.IsNullOrWhiteSpace(m_AnimationChannelId)
            ? default
            : new AnimationChannelId(m_AnimationChannelId);

        public string AnimationSlotId => m_AnimationSlotId ?? string.Empty;

#if UNITY_EDITOR
        public void SetAnimationChannelId(AnimationChannelId animationChannelId)
        {
            m_AnimationChannelId = animationChannelId.IsValid
                ? animationChannelId.Value
                : throw new ArgumentException("Animation Channel identity is invalid.", nameof(animationChannelId));
            RebindTimeline();
        }

        public void SetAnimationSlotId(string animationSlotId)
        {
            m_AnimationSlotId = animationSlotId?.Trim() ?? string.Empty;
            RebindTimeline();
        }

        public void ApplyAuthoringField(string fieldId, string value)
        {
            switch (fieldId)
            {
                case "animationChannelId":
                    SetAnimationChannelId(new AnimationChannelId(value));
                    return;
                case "animationSlotId":
                    SetAnimationSlotId(value);
                    return;
                default:
                    throw new ArgumentException($"Unknown AnimationTrack authoring field '{fieldId}'.", nameof(fieldId));
            }
        }

#endif

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
            int cycleIndex,
            Func<Clip, bool> clipFilter = null)
        {
            if (m_PersistentMuted)
                return;

            for (int clipIndex = 0; clipIndex < Clips.Count; clipIndex++)
            {
                Clip clip = Clips[clipIndex];
                if (clip is not AnimationClip animationClip || !animationClip.Clip)
                    continue;
                if (clipFilter != null && !clipFilter(animationClip))
                {
                    continue;
                }

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
                    AnimationChannelId,
                    AnimationSlotId,
                    animationClip.BlendProfileId,
                    clipTime,
                    normalizedTime,
                    weight,
                    isLooping,
                    animationClip.ClipInTime.ToSingle(),
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
            clipTime = selfTime + clip.ClipInTime.ToSingle();

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
            return UnityEditor.DragAndDrop.objectReferences.Length == 1 &&
                   UnityEditor.DragAndDrop.objectReferences[0] as UnityEngine.AnimationClip;
        }
#endif
    }

    [ScriptGuid("3f0d14cafa6f2c84389c42789ec00083"), Color(127, 253, 228)]
    [TimelineAuthoringProperty("extraPolationMode", typeof(ExtraPolationMode))]
    [TimelineAuthoringProperty(
        "blendProfileId",
        TimelineAuthoringPropertyKind.Text,
        Trimmed = true)]
    public partial class AnimationClip : Clip
    {
        public override string ContractKind => TimelineContractKinds.AnimationClip;

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
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public string BlendProfileId = string.Empty;
#if UNITY_EDITOR

        public override string Name => Clip ? Clip.name : base.Name;
        public override int Length => Clip
            ? Mathf.RoundToInt(Clip.length * TimelineUtility.FrameRate)
            : base.Length;
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

    public static class AnimationTimelineContracts
    {
        public static readonly ITimelineContractProvider Provider = new TimelineContractProvider(
            new[]
            {
                new TimelineTrackContract(
                    TimelineContractKinds.AnimationTrack,
                    TimelineTrackOverlapPolicy.Blend,
                    TimelineCapability.Animation,
                    TimelineExecutionDomain.Presentation,
                    TimelineOutputKind.PresentationEvent,
                    TimelineContractKinds.AnimationClip)
            },
            new[]
            {
                new TimelineClipContract(
                    TimelineContractKinds.AnimationClip,
                    TimelineContractKinds.AnimationTrack,
                    TimelineClipExecutionPhase.Commit,
                    TimelineCapability.Animation,
                    true,
                    true,
                    TimelineExecutionDomain.Presentation,
                    TimelineOutputKind.PresentationEvent)
            },
            ValidateAnimationContent);

        static void ValidateAnimationContent(TimelineData timeline, List<string> errors)
        {
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                if (timeline.Tracks[trackIndex] is not AnimationTrack track)
                    continue;
                if (!track.AnimationChannelId.IsValid)
                    errors?.Add($"Timeline '{timeline.Name}' AnimationTrack '{track.AuthoringId}' has no Animation Channel.");
                if (string.IsNullOrWhiteSpace(track.AnimationSlotId) || track.AnimationSlotId != track.AnimationSlotId.Trim())
                    errors?.Add($"Timeline '{timeline.Name}' AnimationTrack '{track.AuthoringId}' has no Animation Slot.");
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    if (track.Clips[clipIndex] is not AnimationClip clip)
                        continue;
                    if (string.IsNullOrWhiteSpace(clip.BlendProfileId) || clip.BlendProfileId != clip.BlendProfileId.Trim())
                        errors?.Add($"Timeline '{timeline.Name}' AnimationClip '{clip.AuthoringId}' has no Blend Profile.");
                }
            }
        }
    }
}
