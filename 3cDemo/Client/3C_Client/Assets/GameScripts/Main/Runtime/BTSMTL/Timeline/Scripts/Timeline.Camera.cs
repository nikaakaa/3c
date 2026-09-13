using System;
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

    public enum TimelineCameraCueKind
    {
        Shake,
        FovKick,
        Recoil,
        CollisionCorrection,
        Custom,
        Override,
        Shot
    }

    public readonly struct TimelineCameraStateSample
    {
        public TimelineCameraStateSample(
            string sourceId,
            string sourceName,
            string trackName,
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
        public TimelineCameraMode Mode { get; }
        public int Priority { get; }
        public float Weight { get; }
        public float BlendInSeconds { get; }
        public float BlendOutSeconds { get; }
        public string TargetKey { get; }
        public TimelineCameraInterruptPolicy InterruptPolicy { get; }
    }

    public readonly struct TimelineCameraCueSample
    {
        public TimelineCameraCueSample(
            string sourceId,
            string sourceName,
            string trackName,
            string cueId,
            TimelineCameraCueKind cueKind,
            string cueType,
            string resourceId,
            float intensity,
            float durationSeconds,
            int priority)
        {
            SourceId = sourceId ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            TrackName = trackName ?? string.Empty;
            CueId = cueId ?? string.Empty;
            CueKind = cueKind;
            CueType = cueType ?? string.Empty;
            ResourceId = resourceId ?? string.Empty;
            Intensity = Mathf.Max(0f, intensity);
            DurationSeconds = Mathf.Max(0f, durationSeconds);
            Priority = priority;
        }

        public string SourceId { get; }
        public string SourceName { get; }
        public string TrackName { get; }
        public string CueId { get; }
        public TimelineCameraCueKind CueKind { get; }
        public string CueType { get; }
        public string ResourceId { get; }
        public float Intensity { get; }
        public float DurationSeconds { get; }
        public int Priority { get; }
    }

    public readonly struct TimelineCameraResponseSample
    {
        public TimelineCameraResponseSample(
            string sourceId,
            string sourceName,
            string trackName,
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

        public void Sample(float timelineTime, string sourceId, string sourceName, ICollection<TimelineCameraStateSample> states)
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
                    cameraClip.Mode,
                    cameraClip.Priority,
                    weight,
                    cameraClip.BlendInSeconds,
                    cameraClip.BlendOutSeconds,
                    cameraClip.TargetKey,
                    cameraClip.InterruptPolicy));
            }
        }

        static bool TrySampleClip(CameraStateClip clip, float timelineTime, out float weight)
        {
            weight = 0f;
            if (timelineTime < clip.StartTime || timelineTime > clip.EndTime)
                return false;

            float duration = Mathf.Max(0.0001f, clip.DurationTime);
            float selfTime = Mathf.Clamp(timelineTime - clip.StartTime, 0f, clip.DurationTime);
            float remainTime = Mathf.Max(0f, clip.EndTime - timelineTime);
            float normalizedTime = Mathf.Clamp01(selfTime / duration);
            weight = CameraTimelineSampling.SampleWeight(clip.WeightCurve, clip.EaseInCurve, clip.EaseOutCurve, normalizedTime, selfTime, remainTime, clip.EaseInTime, clip.EaseOutTime);
            return weight > 0f;
        }

#if UNITY_EDITOR
        public override Type ClipType => typeof(CameraStateClip);
#endif
    }

    [ScriptGuid("54a348faecf94a2ea8ec2b06146e74c2"), Color(180, 160, 255)]
    [TimelineAuthoringProperty("mode", typeof(TimelineCameraMode))]
    [TimelineAuthoringProperty("sequenceId", TimelineAuthoringPropertyKind.Text, Trimmed = true)]
    [TimelineAuthoringProperty("priority", TimelineAuthoringPropertyKind.Integer)]
    [TimelineAuthoringProperty("blendInSeconds", TimelineAuthoringPropertyKind.Float, HasMinimum = true, Minimum = 0d, Finite = true)]
    [TimelineAuthoringProperty("blendOutSeconds", TimelineAuthoringPropertyKind.Float, HasMinimum = true, Minimum = 0d, Finite = true)]
    [TimelineAuthoringProperty("targetKey", TimelineAuthoringPropertyKind.Text, Optional = true, Trimmed = true)]
    [TimelineAuthoringProperty("interruptPolicy", typeof(TimelineCameraInterruptPolicy))]
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

        public CameraStateClip(Track track, int frame) : base(track, frame)
        {
        }
#endif
    }

    [TrackGroup("Base"), ScriptGuid("54a348faecf94a2ea8ec2b06146e74c2"), Ordered(5), Color(255, 168, 214)]
    public sealed class CameraCueTrack : Track
    {
        public override string ContractKind => TimelineContractKinds.CameraCueTrack;

        public void Sample(float previousTime, float timelineTime, string sourceId, string sourceName, ICollection<TimelineCameraCueSample> cues)
        {
            if (m_PersistentMuted || cues == null)
                return;

            foreach (var clip in Clips)
            {
                if (clip is not CameraCueClip cueClip)
                    continue;

                if (previousTime < cueClip.StartTime && cueClip.StartTime <= timelineTime)
                {
                    cues.Add(new TimelineCameraCueSample(
                        sourceId,
                        sourceName,
                        Name,
                        cueClip.CueId,
                        cueClip.CueKind,
                        cueClip.CueType,
                        cueClip.ResourceId,
                        cueClip.Intensity,
                        cueClip.DurationSeconds,
                        cueClip.Priority));
                }
            }
        }

#if UNITY_EDITOR
        public override Type ClipType => typeof(CameraCueClip);
#endif
    }

    [ScriptGuid("54a348faecf94a2ea8ec2b06146e74c2"), Color(255, 168, 214)]
    [TimelineAuthoringProperty("cueId", TimelineAuthoringPropertyKind.Text, Trimmed = true)]
    [TimelineAuthoringProperty("cueKind", typeof(TimelineCameraCueKind))]
    [TimelineAuthoringProperty("cueType", TimelineAuthoringPropertyKind.Text, Trimmed = true)]
    [TimelineAuthoringProperty("resourceId", TimelineAuthoringPropertyKind.Text, Trimmed = true)]
    [TimelineAuthoringProperty("intensity", TimelineAuthoringPropertyKind.Float, HasMinimum = true, Minimum = 0d, Finite = true)]
    [TimelineAuthoringProperty("durationSeconds", TimelineAuthoringPropertyKind.Float, HasMinimum = true, Minimum = 0d, Finite = true)]
    [TimelineAuthoringProperty("priority", TimelineAuthoringPropertyKind.Integer)]
    public sealed class CameraCueClip : SignalClip
    {
        public override string ContractKind => TimelineContractKinds.CameraCueClip;

        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public string CueId = "CameraCue";
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public TimelineCameraCueKind CueKind = TimelineCameraCueKind.Shake;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public string CueType = "Camera";
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public string ResourceId;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public float Intensity = 1f;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public float DurationSeconds = 0.2f;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public int Priority;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public AnimationCurve WeightCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public AnimationCurve EaseInCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public AnimationCurve EaseOutCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

#if UNITY_EDITOR
        public CameraCueClip(Track track, int frame) : base(track, frame)
        {
        }
#endif
    }

    [TrackGroup("Base"), ScriptGuid("54a348faecf94a2ea8ec2b06146e74c2"), Ordered(6), Color(170, 225, 255)]
    public sealed class CameraResponseTrack : Track
    {
        public override string ContractKind => TimelineContractKinds.CameraResponseTrack;

        public void Sample(float timelineTime, string sourceId, string sourceName, ICollection<TimelineCameraResponseSample> responses)
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
                    responseClip.LookResponse,
                    responseClip.ManualOrbitWeight,
                    responseClip.PitchResponseWeight,
                    responseClip.YawResponseWeight,
                    responseClip.Priority,
                    weight));
            }
        }

        static bool TrySampleClip(CameraResponseClip clip, float timelineTime, out float weight)
        {
            weight = 0f;
            if (timelineTime < clip.StartTime || timelineTime > clip.EndTime)
                return false;

            float duration = Mathf.Max(0.0001f, clip.DurationTime);
            float selfTime = Mathf.Clamp(timelineTime - clip.StartTime, 0f, clip.DurationTime);
            float remainTime = Mathf.Max(0f, clip.EndTime - timelineTime);
            float normalizedTime = Mathf.Clamp01(selfTime / duration);
            weight = CameraTimelineSampling.SampleWeight(clip.WeightCurve, clip.EaseInCurve, clip.EaseOutCurve, normalizedTime, selfTime, remainTime, clip.EaseInTime, clip.EaseOutTime);
            return weight > 0f;
        }

#if UNITY_EDITOR
        public override Type ClipType => typeof(CameraResponseClip);
#endif
    }

    [ScriptGuid("54a348faecf94a2ea8ec2b06146e74c2"), Color(170, 225, 255)]
    [TimelineAuthoringProperty("lookResponse", typeof(TimelineCameraLookResponseMode))]
    [TimelineAuthoringProperty("manualOrbitWeight", TimelineAuthoringPropertyKind.Float, HasMinimum = true, Minimum = 0d, HasMaximum = true, Maximum = 1d, Finite = true)]
    [TimelineAuthoringProperty("pitchResponseWeight", TimelineAuthoringPropertyKind.Float, HasMinimum = true, Minimum = 0d, HasMaximum = true, Maximum = 1d, Finite = true)]
    [TimelineAuthoringProperty("yawResponseWeight", TimelineAuthoringPropertyKind.Float, HasMinimum = true, Minimum = 0d, HasMaximum = true, Maximum = 1d, Finite = true)]
    [TimelineAuthoringProperty("priority", TimelineAuthoringPropertyKind.Integer)]
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

        public CameraResponseClip(Track track, int frame) : base(track, frame)
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
                    TimelineContractKinds.CameraStateClip),
                new TimelineTrackContract(
                    TimelineContractKinds.CameraCueTrack,
                    TimelineTrackOverlapPolicy.Parallel,
                    TimelineCapability.Camera,
                    TimelineContractKinds.CameraCueClip),
                new TimelineTrackContract(
                    TimelineContractKinds.CameraResponseTrack,
                    TimelineTrackOverlapPolicy.Blend,
                    TimelineCapability.Camera,
                    TimelineContractKinds.CameraResponseClip),
                new TimelineTrackContract(
                    TimelineContractKinds.CameraOverrideTrack,
                    TimelineTrackOverlapPolicy.Parallel,
                    TimelineCapability.Camera,
                    TimelineContractKinds.CameraOverrideClip),
                new TimelineTrackContract(
                    TimelineContractKinds.CameraZoomTrack,
                    TimelineTrackOverlapPolicy.Parallel,
                    TimelineCapability.Camera,
                    TimelineContractKinds.CameraZoomClip),
                new TimelineTrackContract(
                    TimelineContractKinds.CameraStretchTrack,
                    TimelineTrackOverlapPolicy.Parallel,
                    TimelineCapability.Camera,
                    TimelineContractKinds.CameraStretchClip),
                new TimelineTrackContract(
                    TimelineContractKinds.CameraShotTrack,
                    TimelineTrackOverlapPolicy.Parallel,
                    TimelineCapability.Camera,
                    TimelineContractKinds.CameraShotClip)
            },
            new[]
            {
                new TimelineClipContract(
                    TimelineContractKinds.CameraStateClip,
                    TimelineContractKinds.CameraStateTrack,
                    TimelineClipExecutionPhase.Commit,
                    TimelineCapability.Camera,
                    true,
                    true),
                new TimelineClipContract(
                    TimelineContractKinds.CameraCueClip,
                    TimelineContractKinds.CameraCueTrack,
                    TimelineClipExecutionPhase.Commit,
                    TimelineCapability.Camera,
                    false,
                    false),
                new TimelineClipContract(
                    TimelineContractKinds.CameraResponseClip,
                    TimelineContractKinds.CameraResponseTrack,
                    TimelineClipExecutionPhase.Commit,
                    TimelineCapability.Camera,
                    true,
                    true),
                new TimelineClipContract(
                    TimelineContractKinds.CameraOverrideClip,
                    TimelineContractKinds.CameraOverrideTrack,
                    TimelineClipExecutionPhase.Commit,
                    TimelineCapability.Camera,
                    true,
                    true),
                new TimelineClipContract(
                    TimelineContractKinds.CameraZoomClip,
                    TimelineContractKinds.CameraZoomTrack,
                    TimelineClipExecutionPhase.Commit,
                    TimelineCapability.Camera,
                    true,
                    true),
                new TimelineClipContract(
                    TimelineContractKinds.CameraStretchClip,
                    TimelineContractKinds.CameraStretchTrack,
                    TimelineClipExecutionPhase.Commit,
                    TimelineCapability.Camera,
                    true,
                    true),
                new TimelineClipContract(
                    TimelineContractKinds.CameraShotClip,
                    TimelineContractKinds.CameraShotTrack,
                    TimelineClipExecutionPhase.Commit,
                    TimelineCapability.Camera,
                    true,
                    true)
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
