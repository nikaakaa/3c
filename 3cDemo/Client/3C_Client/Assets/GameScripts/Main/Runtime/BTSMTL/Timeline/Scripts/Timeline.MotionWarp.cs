using System;
using System.Collections.Generic;
using UnityEngine;

namespace BTSMTL.Timeline
{
    public readonly struct TimelineMotionWarpWindow
    {
        public TimelineMotionWarpWindow(
            string sourceId,
            string sourceName,
            string trackName,
            string targetKey,
            float normalizedTime,
            float weight,
            float positionWeight,
            float yawWeight,
            float maxPositionCorrection,
            float maxYawCorrectionDegrees)
        {
            SourceId = sourceId;
            SourceName = sourceName;
            TrackName = trackName;
            TargetKey = targetKey;
            NormalizedTime = normalizedTime;
            Weight = weight;
            PositionWeight = positionWeight;
            YawWeight = yawWeight;
            MaxPositionCorrection = maxPositionCorrection;
            MaxYawCorrectionDegrees = maxYawCorrectionDegrees;
        }

        public string SourceId { get; }
        public string SourceName { get; }
        public string TrackName { get; }
        public string TargetKey { get; }
        public float NormalizedTime { get; }
        public float Weight { get; }
        public float PositionWeight { get; }
        public float YawWeight { get; }
        public float MaxPositionCorrection { get; }
        public float MaxYawCorrectionDegrees { get; }
    }

    [TrackGroup("Base"), ScriptGuid("79b8da4acfeb4d1994d019eacf6d5de3"), Ordered(1), Color(248, 177, 91)]
    public sealed class MotionWarpTrack : Track
    {
        public void Sample(
            float timelineTime,
            string sourceId,
            string sourceName,
            ICollection<TimelineMotionWarpWindow> windows)
        {
            if (m_PersistentMuted || windows == null)
                return;

            foreach (var clip in Clips)
            {
                if (clip is not MotionWarpClip motionWarpClip)
                    continue;

                if (!TrySampleClip(motionWarpClip, timelineTime, out float normalizedTime, out float weight))
                    continue;

                windows.Add(new TimelineMotionWarpWindow(
                    sourceId,
                    sourceName,
                    Name,
                    motionWarpClip.TargetKey,
                    normalizedTime,
                    weight,
                    motionWarpClip.PositionWeight,
                    motionWarpClip.YawWeight,
                    motionWarpClip.MaxPositionCorrection,
                    motionWarpClip.MaxYawCorrectionDegrees));
            }
        }

        static bool TrySampleClip(MotionWarpClip clip, float timelineTime, out float normalizedTime, out float weight)
        {
            normalizedTime = 0f;
            weight = 0f;

            if (timelineTime < clip.StartTime || timelineTime > clip.EndTime)
                return false;

            float duration = Mathf.Max(0.0001f, clip.DurationTime);
            float selfTime = Mathf.Clamp(timelineTime - clip.StartTime, 0f, clip.DurationTime);
            float remainTime = Mathf.Max(0f, clip.EndTime - timelineTime);
            normalizedTime = Mathf.Clamp01(selfTime / duration);

            float fadeInWeight = 1f;
            if (clip.EaseInTime > 0f && selfTime < clip.EaseInTime)
                fadeInWeight = EvaluateCurve(clip.EaseInCurve, Mathf.Clamp01(selfTime / clip.EaseInTime), 1f);

            float fadeOutWeight = 1f;
            if (clip.EaseOutTime > 0f && remainTime < clip.EaseOutTime)
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
        public override Type ClipType => typeof(MotionWarpClip);
#endif
    }

    [ScriptGuid("79b8da4acfeb4d1994d019eacf6d5de3"), Color(248, 177, 91)]
    public sealed class MotionWarpClip : Clip
    {
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public string TargetKey = "motion.target";
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public float PositionWeight = 1f;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public float YawWeight = 1f;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public float MaxPositionCorrection = 1f;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public float MaxYawCorrectionDegrees = 45f;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public AnimationCurve WeightCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public AnimationCurve EaseInCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public AnimationCurve EaseOutCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

#if UNITY_EDITOR
        public override ClipCapabilities Capabilities => ClipCapabilities.Resizable | ClipCapabilities.Mixable;

        public MotionWarpClip(Track track, int frame) : base(track, frame)
        {
        }
#endif
    }
}
