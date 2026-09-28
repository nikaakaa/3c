using System;
using ThirdPersonSimulation.Fixed;
using System.Collections.Generic;
using UnityEngine;

namespace BTSMTL.Timeline
{
    public enum TimelineCameraMode
    {
        FreeLook,
        Aim,
        LockOn,
        ActionFocus,
        SkillCloseup
    }

    public enum TimelineCameraLookResponseMode
    {
        Full,
        Suppressed,
        Weighted
    }

    public enum TimelineCameraInterruptPolicy
    {
        BlendOut,
        Cut,
        HoldUntilSourceEnds
    }

    public readonly struct TimelineCameraStateSample
    {
        public TimelineCameraStateSample(
            string sourceId,
            string sourceName,
            string trackName,
            string trackAuthoringId,
            string clipAuthoringId,
            string sequenceId,
            TimelineCameraMode mode,
            int priority,
            float weight,
            float blendInSeconds,
            float blendOutSeconds,
            string targetKey,
            TimelineCameraInterruptPolicy interruptPolicy)
        {
            SourceId = sourceId ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            TrackName = trackName ?? string.Empty;
            TrackAuthoringId = trackAuthoringId;
            ClipAuthoringId = clipAuthoringId;
            SequenceId = sequenceId ?? string.Empty;
            Mode = mode;
            Priority = priority;
            Weight = Mathf.Clamp01(weight);
            BlendInSeconds = Mathf.Max(0f, blendInSeconds);
            BlendOutSeconds = Mathf.Max(0f, blendOutSeconds);
            TargetKey = targetKey ?? string.Empty;
            InterruptPolicy = interruptPolicy;
        }

        public string SourceId { get; }
        public string SourceName { get; }
        public string TrackName { get; }
        public string TrackAuthoringId { get; }
        public string ClipAuthoringId { get; }
        public string SequenceId { get; }
        public TimelineCameraMode Mode { get; }
        public int Priority { get; }
        public float Weight { get; }
        public float BlendInSeconds { get; }
        public float BlendOutSeconds { get; }
        public string TargetKey { get; }
        public TimelineCameraInterruptPolicy InterruptPolicy { get; }
    }

    public readonly struct TimelineCameraResponseSample
    {
        public TimelineCameraResponseSample(
            string sourceId,
            string sourceName,
            string trackName,
            string trackAuthoringId,
            string clipAuthoringId,
            TimelineCameraLookResponseMode lookResponse,
            float manualOrbitWeight,
            float pitchResponseWeight,
            float yawResponseWeight,
            int priority,
            float weight)
        {
            SourceId = sourceId ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            TrackName = trackName ?? string.Empty;
            TrackAuthoringId = trackAuthoringId;
            ClipAuthoringId = clipAuthoringId;
            LookResponse = lookResponse;
            ManualOrbitWeight = Mathf.Clamp01(manualOrbitWeight);
            PitchResponseWeight = Mathf.Clamp01(pitchResponseWeight);
            YawResponseWeight = Mathf.Clamp01(yawResponseWeight);
            Priority = priority;
            Weight = Mathf.Clamp01(weight);
        }

        public string SourceId { get; }
        public string SourceName { get; }
        public string TrackName { get; }
        public string TrackAuthoringId { get; }
        public string ClipAuthoringId { get; }
        public TimelineCameraLookResponseMode LookResponse { get; }
        public float ManualOrbitWeight { get; }
        public float PitchResponseWeight { get; }
        public float YawResponseWeight { get; }
        public int Priority { get; }
        public float Weight { get; }
    }

    [TrackGroup("Base"), ScriptGuid("54a348faecf94a2ea8ec2b06146e74c2"), Ordered(4), Color(180, 160, 255)]
    public sealed class CameraStateTrack : Track
    {
        public override string ContractKind => TimelineContractKinds.CameraStateTrack;

        public void Sample(
            FixedScalar timelineTime,
            string sourceId,
            string sourceName,
            ICollection<TimelineCameraStateSample> states)
        {
            if (m_PersistentMuted || states == null)
                return;

            foreach (var clip in Clips)
            {
                if (clip is not CameraStateClip cameraClip)
                    continue;

                if (!TrySampleClip(cameraClip, timelineTime, out float weight))
                    continue;

                states.Add(new TimelineCameraStateSample(
                    sourceId,
                    sourceName,
                    Name,
                    AuthoringId,
                    cameraClip.AuthoringId,
                    cameraClip.SequenceId,
                    cameraClip.Mode,
                    cameraClip.Priority,
                    weight,
                    cameraClip.BlendInSeconds,
                    cameraClip.BlendOutSeconds,
                    cameraClip.TargetKey,
                    cameraClip.InterruptPolicy));
            }
        }

        static bool TrySampleClip(CameraStateClip clip, FixedScalar timelineTime, out float weight)
        {
            weight = 0f;
            if (timelineTime < clip.StartTime || timelineTime > clip.EndTime)
                return false;

            float duration = Mathf.Max(0.0001f, clip.DurationTime.ToSingle());
            float selfTime = FixedScalar.Clamp(timelineTime - clip.StartTime, FixedScalar.Zero, clip.DurationTime).ToSingle();
            float remainTime = FixedScalar.Max(FixedScalar.Zero, clip.EndTime - timelineTime).ToSingle();
            float normalizedTime = Mathf.Clamp01(selfTime / duration);
            weight = CameraTimelineSampling.SampleWeight(clip.WeightCurve, clip.EaseInCurve, clip.EaseOutCurve, normalizedTime, selfTime, remainTime, clip.EaseInTime.ToSingle(), clip.EaseOutTime.ToSingle());
            return weight > 0f;
        }

#if UNITY_EDITOR
        public override Type ClipType => typeof(CameraStateClip);
#endif
    }

    [ScriptGuid("54a348faecf94a2ea8ec2b06146e74c2"), Color(180, 160, 255)]
#if UNITY_EDITOR
    [TimelineAuthoringProperty("mode", typeof(TimelineCameraMode))]
    [TimelineAuthoringProperty("sequenceId", TimelineAuthoringPropertyKind.Text, Trimmed = true)]
    [TimelineAuthoringProperty("priority", TimelineAuthoringPropertyKind.Integer)]
    [TimelineAuthoringProperty("blendInSeconds", TimelineAuthoringPropertyKind.Float, HasMinimum = true, Minimum = 0d, Finite = true)]
    [TimelineAuthoringProperty("blendOutSeconds", TimelineAuthoringPropertyKind.Float, HasMinimum = true, Minimum = 0d, Finite = true)]
    [TimelineAuthoringProperty("targetKey", TimelineAuthoringPropertyKind.Text, Optional = true, Trimmed = true)]
    [TimelineAuthoringProperty("interruptPolicy", typeof(TimelineCameraInterruptPolicy))]
#endif
    public sealed class CameraStateClip : Clip
    {
        public override string ContractKind => TimelineContractKinds.CameraStateClip;

        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public TimelineCameraMode Mode = TimelineCameraMode.SkillCloseup;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public string SequenceId;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public int Priority = 100;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public float BlendInSeconds = 0.15f;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public float BlendOutSeconds = 0.2f;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public string TargetKey;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public TimelineCameraInterruptPolicy InterruptPolicy = TimelineCameraInterruptPolicy.BlendOut;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public AnimationCurve WeightCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public AnimationCurve EaseInCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public AnimationCurve EaseOutCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

#if UNITY_EDITOR
        public override ClipCapabilities Capabilities => ClipCapabilities.Resizable | ClipCapabilities.Mixable;

        public CameraStateClip(Track track, FixedScalar time) : base(track, time)
        {
        }
#endif
    }

    [TrackGroup("Base"), ScriptGuid("54a348faecf94a2ea8ec2b06146e74c2"), Ordered(6), Color(170, 225, 255)]
    public sealed class CameraResponseTrack : Track
    {
        public override string ContractKind => TimelineContractKinds.CameraResponseTrack;

        public void Sample(
            FixedScalar timelineTime,
            string sourceId,
            string sourceName,
            ICollection<TimelineCameraResponseSample> responses)
        {
            if (m_PersistentMuted || responses == null)
                return;

            foreach (var clip in Clips)
            {
                if (clip is not CameraResponseClip responseClip)
                    continue;

                if (!TrySampleClip(responseClip, timelineTime, out float weight))
                    continue;

                responses.Add(new TimelineCameraResponseSample(
                    sourceId,
                    sourceName,
                    Name,
                    AuthoringId,
                    responseClip.AuthoringId,
                    responseClip.LookResponse,
                    responseClip.ManualOrbitWeight,
                    responseClip.PitchResponseWeight,
                    responseClip.YawResponseWeight,
                    responseClip.Priority,
                    weight));
            }
        }

        static bool TrySampleClip(CameraResponseClip clip, FixedScalar timelineTime, out float weight)
        {
            weight = 0f;
            if (timelineTime < clip.StartTime || timelineTime > clip.EndTime)
                return false;

            float duration = Mathf.Max(0.0001f, clip.DurationTime.ToSingle());
            float selfTime = FixedScalar.Clamp(timelineTime - clip.StartTime, FixedScalar.Zero, clip.DurationTime).ToSingle();
            float remainTime = FixedScalar.Max(FixedScalar.Zero, clip.EndTime - timelineTime).ToSingle();
            float normalizedTime = Mathf.Clamp01(selfTime / duration);
            weight = CameraTimelineSampling.SampleWeight(clip.WeightCurve, clip.EaseInCurve, clip.EaseOutCurve, normalizedTime, selfTime, remainTime, clip.EaseInTime.ToSingle(), clip.EaseOutTime.ToSingle());
            return weight > 0f;
        }

#if UNITY_EDITOR
        public override Type ClipType => typeof(CameraResponseClip);
#endif
    }

    [ScriptGuid("54a348faecf94a2ea8ec2b06146e74c2"), Color(170, 225, 255)]
#if UNITY_EDITOR
    [TimelineAuthoringProperty("lookResponse", typeof(TimelineCameraLookResponseMode))]
    [TimelineAuthoringProperty("manualOrbitWeight", TimelineAuthoringPropertyKind.Float, HasMinimum = true, Minimum = 0d, HasMaximum = true, Maximum = 1d, Finite = true)]
    [TimelineAuthoringProperty("pitchResponseWeight", TimelineAuthoringPropertyKind.Float, HasMinimum = true, Minimum = 0d, HasMaximum = true, Maximum = 1d, Finite = true)]
    [TimelineAuthoringProperty("yawResponseWeight", TimelineAuthoringPropertyKind.Float, HasMinimum = true, Minimum = 0d, HasMaximum = true, Maximum = 1d, Finite = true)]
    [TimelineAuthoringProperty("priority", TimelineAuthoringPropertyKind.Integer)]
#endif
    public sealed class CameraResponseClip : Clip
    {
        public override string ContractKind => TimelineContractKinds.CameraResponseClip;

        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public TimelineCameraLookResponseMode LookResponse = TimelineCameraLookResponseMode.Suppressed;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public float ManualOrbitWeight;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public float PitchResponseWeight = 1f;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public float YawResponseWeight = 1f;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public int Priority = 100;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public AnimationCurve WeightCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public AnimationCurve EaseInCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public AnimationCurve EaseOutCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

#if UNITY_EDITOR
        public override ClipCapabilities Capabilities => ClipCapabilities.Resizable | ClipCapabilities.Mixable;

        public CameraResponseClip(Track track, FixedScalar time) : base(track, time)
        {
        }
#endif
    }

    public static class CameraTimelineContracts
    {
        public static readonly ITimelineContractProvider Provider = new TimelineContractProvider(
            new[]
            {
                new TimelineTrackContract(
                    TimelineContractKinds.CameraStateTrack,
                    TimelineTrackOverlapPolicy.Blend,
                    TimelineCapability.Camera,
                    TimelineExecutionDomain.Presentation,
                    TimelineOutputKind.PresentationEvent,
                    TimelineContractKinds.CameraStateClip),
                new TimelineTrackContract(
                    TimelineContractKinds.CameraResponseTrack,
                    TimelineTrackOverlapPolicy.Blend,
                    TimelineCapability.Camera,
                    TimelineExecutionDomain.Presentation,
                    TimelineOutputKind.PresentationEvent,
                    TimelineContractKinds.CameraResponseClip),
                new TimelineTrackContract(
                    TimelineContractKinds.CameraEffectTrack,
                    TimelineTrackOverlapPolicy.Parallel,
                    TimelineCapability.Camera,
                    TimelineExecutionDomain.Presentation,
                    TimelineOutputKind.PresentationEvent,
                    TimelineContractKinds.CameraEffectClip)
            },
            new[]
            {
                new TimelineClipContract(
                    TimelineContractKinds.CameraStateClip,
                    TimelineContractKinds.CameraStateTrack,
                    TimelineClipExecutionPhase.Commit,
                    TimelineCapability.Camera,
                    true,
                    true,
                    TimelineExecutionDomain.Presentation,
                    TimelineOutputKind.PresentationEvent),
                new TimelineClipContract(
                    TimelineContractKinds.CameraResponseClip,
                    TimelineContractKinds.CameraResponseTrack,
                    TimelineClipExecutionPhase.Commit,
                    TimelineCapability.Camera,
                    true,
                    true,
                    TimelineExecutionDomain.Presentation,
                    TimelineOutputKind.PresentationEvent),
                new TimelineClipContract(
                    TimelineContractKinds.CameraEffectClip,
                    TimelineContractKinds.CameraEffectTrack,
                    TimelineClipExecutionPhase.Commit,
                    TimelineCapability.Camera,
                    true,
                    true,
                    TimelineExecutionDomain.Presentation,
                    TimelineOutputKind.PresentationEvent)
            });
    }

    static class CameraTimelineSampling
    {
        public static float SampleWeight(
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

        static float EvaluateCurve(AnimationCurve curve, float time, float fallback)
        {
            return curve != null && curve.length > 0 ? curve.Evaluate(time) : fallback;
        }
    }
}
