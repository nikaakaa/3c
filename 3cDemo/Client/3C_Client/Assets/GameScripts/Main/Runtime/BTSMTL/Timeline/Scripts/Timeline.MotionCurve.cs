using System;
using System.Collections.Generic;
using UnityEngine;

namespace BTSMTL.Timeline
{
    public enum TimelineMotionContributionSpace
    {
        Local,
        World
    }

    public enum TimelineMotionChannel
    {
        Locomotion,
        Action,
        GameplayResult
    }

    public enum TimelineMotionBlendMode
    {
        Additive,
        WeightedBlend,
        Override
    }

    public readonly struct TimelineMotionCurveContribution
    {
        public TimelineMotionCurveContribution(
            string sourceId,
            string sourceName,
            string trackName,
            string curveId,
            TimelineMotionContributionSpace space,
            TimelineMotionChannel channel,
            TimelineMotionBlendMode blendMode,
            Vector3 displacement,
            float yawDegrees,
            int priority,
            float weight,
            bool consumeLowerChannels,
            float normalizedTime)
        {
            SourceId = sourceId ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            TrackName = trackName ?? string.Empty;
            CurveId = curveId ?? string.Empty;
            Space = space;
            Channel = channel;
            BlendMode = blendMode;
            Displacement = displacement;
            YawDegrees = yawDegrees;
            Priority = priority;
            Weight = Mathf.Clamp01(weight);
            ConsumeLowerChannels = consumeLowerChannels;
            NormalizedTime = Mathf.Clamp01(normalizedTime);
        }

        public string SourceId { get; }
        public string SourceName { get; }
        public string TrackName { get; }
        public string CurveId { get; }
        public TimelineMotionContributionSpace Space { get; }
        public TimelineMotionChannel Channel { get; }
        public TimelineMotionBlendMode BlendMode { get; }
        public Vector3 Displacement { get; }
        public float YawDegrees { get; }
        public int Priority { get; }
        public float Weight { get; }
        public bool ConsumeLowerChannels { get; }
        public float NormalizedTime { get; }
        public bool HasDelta => Weight > 0f && (Displacement.sqrMagnitude > 0.0000001f || Mathf.Abs(YawDegrees) > 0.0001f);
        public bool ClaimsLowerChannels => Weight > 0f && BlendMode == TimelineMotionBlendMode.Override && ConsumeLowerChannels;
        public bool CanResolve => HasDelta || ClaimsLowerChannels;
    }

    [TrackGroup("Base"), ScriptGuid("6f2a51d8c9b34d5f8a0e7b4c2d9f136a"), Ordered(1.5f), Color(126, 220, 146)]
    public sealed class MotionCurveTrack : Track
    {
        public void Sample(
            float previousTimelineTime,
            float timelineTime,
            string sourceId,
            string sourceName,
            ICollection<TimelineMotionCurveContribution> contributions)
        {
            if (m_PersistentMuted || contributions == null)
                return;

            foreach (var clip in Clips)
            {
                if (clip is not MotionCurveClip motionCurveClip)
                    continue;

                if (!TrySampleClip(motionCurveClip, previousTimelineTime, timelineTime, out TimelineMotionCurveContribution contribution))
                    continue;

                if (!contribution.CanResolve)
                    continue;

                contributions.Add(new TimelineMotionCurveContribution(
                    sourceId,
                    sourceName,
                    Name,
                    motionCurveClip.CurveId,
                    contribution.Space,
                    contribution.Channel,
                    contribution.BlendMode,
                    contribution.Displacement,
                    contribution.YawDegrees,
                    contribution.Priority,
                    contribution.Weight,
                    contribution.ConsumeLowerChannels,
                    contribution.NormalizedTime));
            }
        }

        static bool TrySampleClip(
            MotionCurveClip clip,
            float previousTimelineTime,
            float timelineTime,
            out TimelineMotionCurveContribution contribution)
        {
            contribution = default;
            if (timelineTime <= clip.StartTime || previousTimelineTime >= clip.EndTime)
                return false;

            float duration = Mathf.Max(0.0001f, clip.DurationTime);
            float previousSelfTime = Mathf.Clamp(previousTimelineTime - clip.StartTime, 0f, clip.DurationTime);
            float selfTime = Mathf.Clamp(timelineTime - clip.StartTime, 0f, clip.DurationTime);
            if (Mathf.Approximately(previousSelfTime, selfTime))
                return false;

            float curveDuration = Mathf.Max(
                0.0001f,
                (clip.CurveEndFrame - clip.StartFrame) / (float)TimelineUtility.FrameRate);
            float previousCurveTime = Mathf.Clamp(previousTimelineTime - clip.StartTime, 0f, curveDuration);
            float curveTime = Mathf.Clamp(timelineTime - clip.StartTime, 0f, curveDuration);
            float previousNormalizedTime = Mathf.Clamp01(previousCurveTime / curveDuration);
            float normalizedTime = Mathf.Clamp01(curveTime / curveDuration);
            float weightNormalizedTime = Mathf.Clamp01(selfTime / duration);
            float remainTime = Mathf.Max(0f, clip.EndTime - timelineTime);
            float weight = SampleWeight(clip.WeightCurve, clip.EaseInCurve, clip.EaseOutCurve, weightNormalizedTime, selfTime, remainTime, clip.EaseInTime, clip.EaseOutTime);
            if (weight <= 0f)
                return false;

            Vector3 previousPosition = SamplePosition(clip, previousNormalizedTime);
            Vector3 currentPosition = SamplePosition(clip, normalizedTime);
            contribution = new TimelineMotionCurveContribution(
                string.Empty,
                string.Empty,
                string.Empty,
                clip.CurveId,
                clip.Space,
                clip.Channel,
                clip.BlendMode,
                currentPosition - previousPosition,
                EvaluateCurve(clip.Yaw, normalizedTime, 0f) - EvaluateCurve(clip.Yaw, previousNormalizedTime, 0f),
                clip.Priority,
                weight,
                clip.ConsumeLowerChannels,
                normalizedTime);
            return true;
        }

        static Vector3 SamplePosition(MotionCurveClip clip, float normalizedTime)
        {
            return new Vector3(
                EvaluateCurve(clip.PositionX, normalizedTime, 0f),
                EvaluateCurve(clip.PositionY, normalizedTime, 0f),
                EvaluateCurve(clip.PositionZ, normalizedTime, 0f));
        }

        static float SampleWeight(
            AnimationCurve weightCurve,
            AnimationCurve easeInCurve,
            AnimationCurve easeOutCurve,
            float normalizedTime,
            float selfTime,
            float remainTime,
            float easeInTime,
            float easeOutTime)
        {
            float fadeInWeight = 1f;
            if (easeInTime > 0f && selfTime < easeInTime)
                fadeInWeight = EvaluateCurve(easeInCurve, Mathf.Clamp01(selfTime / easeInTime), 1f);

            float fadeOutWeight = 1f;
            if (easeOutTime > 0f && remainTime < easeOutTime)
                fadeOutWeight = 1f - EvaluateCurve(easeOutCurve, Mathf.Clamp01(1f - remainTime / easeOutTime), 0f);

            return Mathf.Clamp01(EvaluateCurve(weightCurve, normalizedTime, 1f) * fadeInWeight * fadeOutWeight);
        }

        static float EvaluateCurve(AnimationCurve curve, float time, float defaultValue)
        {
            return curve != null && curve.length > 0 ? curve.Evaluate(time) : defaultValue;
        }

#if UNITY_EDITOR
        public override Type ClipType => typeof(MotionCurveClip);
#endif
    }

    [ScriptGuid("6f2a51d8c9b34d5f8a0e7b4c2d9f136a"), Color(126, 220, 146)]
    public sealed partial class MotionCurveClip : Clip
    {
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public string CurveId = "MotionCurve";
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public int CurveEndFrame;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public TimelineMotionContributionSpace Space = TimelineMotionContributionSpace.Local;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public TimelineMotionChannel Channel = TimelineMotionChannel.Action;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public TimelineMotionBlendMode BlendMode = TimelineMotionBlendMode.Override;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public int Priority = 100;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public bool ConsumeLowerChannels = true;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public AnimationCurve WeightCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public AnimationCurve PositionX = AnimationCurve.Linear(0f, 0f, 1f, 0f);
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public AnimationCurve PositionY = AnimationCurve.Linear(0f, 0f, 1f, 0f);
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public AnimationCurve PositionZ = AnimationCurve.Linear(0f, 0f, 1f, 0f);
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public AnimationCurve Yaw = AnimationCurve.Linear(0f, 0f, 1f, 0f);
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public AnimationCurve EaseInCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public AnimationCurve EaseOutCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public override void Init(Track track)
        {
            base.Init(track);
            if (CurveEndFrame <= StartFrame || CurveEndFrame > EndFrame)
                throw new InvalidOperationException($"MotionCurveClip '{CurveId}' requires StartFrame < CurveEndFrame <= EndFrame.");
        }

#if UNITY_EDITOR
        public override ClipCapabilities Capabilities => ClipCapabilities.Resizable | ClipCapabilities.Mixable;

        public MotionCurveClip(Track track, int frame) : base(track, frame)
        {
            CurveEndFrame = EndFrame;
        }
#endif
    }
}
