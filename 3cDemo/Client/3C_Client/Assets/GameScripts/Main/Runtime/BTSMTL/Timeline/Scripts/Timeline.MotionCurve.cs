using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
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
        public override string ContractKind => TimelineContractKinds.MotionCurveTrack;

        public void Sample(
            float previousTimelineTime,
            float timelineTime,
            string sourceId,
            string sourceName,
            ICollection<TimelineMotionCurveContribution> contributions,
            Func<Clip, bool> clipFilter = null)
        {
            if (m_PersistentMuted || contributions == null)
                return;

            foreach (var clip in Clips)
            {
                if (clip is not MotionCurveClip motionCurveClip)
                    continue;
                if (clipFilter != null && !clipFilter(motionCurveClip))
                {
                    continue;
                }

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

            float weightNormalizedTime = Mathf.Clamp01(selfTime / duration);
            float remainTime = Mathf.Max(0f, clip.EndTime - timelineTime);
            float weight = SampleWeight(clip.WeightCurve, clip.EaseInCurve, clip.EaseOutCurve, weightNormalizedTime, selfTime, remainTime, clip.EaseInTime, clip.EaseOutTime);
            if (weight <= 0f)
                return false;

            Vector3 previousPosition = clip.EvaluatePositionAtTimelineTime(previousTimelineTime);
            Vector3 currentPosition = clip.EvaluatePositionAtTimelineTime(timelineTime);
            contribution = new TimelineMotionCurveContribution(
                string.Empty,
                string.Empty,
                string.Empty,
                clip.CurveId,
                clip.Space,
                clip.Channel,
                clip.BlendMode,
                currentPosition - previousPosition,
                clip.EvaluateYawAtTimelineTime(timelineTime) - clip.EvaluateYawAtTimelineTime(previousTimelineTime),
                clip.Priority,
                weight,
                clip.ConsumeLowerChannels,
                weightNormalizedTime);
            return true;
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

        public override Clip AddClip(UnityEngine.Object referenceObject, int frame)
        {
            if (referenceObject is not RootMotionCurveAsset source)
                throw new ArgumentException("MotionCurveClip requires a RootMotionCurveAsset source.", nameof(referenceObject));
            MotionCurveClip clip = new MotionCurveClip(this, frame, source);
            clip.RegenerateAuthoringIdentity();
            m_Clips.Add(clip);
            return clip;
        }

        public override Clip AddClip(int frame)
        {
            throw new InvalidOperationException("MotionCurveClip requires a RootMotionCurveAsset source.");
        }
#endif
    }

    [ScriptGuid("6f2a51d8c9b34d5f8a0e7b4c2d9f136a"), Color(126, 220, 146)]
    [TimelineAuthoringProperty("curveId", TimelineAuthoringPropertyKind.Text, Trimmed = true)]
    [TimelineAuthoringProperty("sourceCurve", TimelineAuthoringPropertyKind.Object)]
    [TimelineAuthoringProperty("sourceStartTime", TimelineAuthoringPropertyKind.Float, HasMinimum = true, Minimum = 0, Finite = true)]
    [TimelineAuthoringProperty("sourceEndTime", TimelineAuthoringPropertyKind.Float, HasMinimum = true, Minimum = 0, Finite = true)]
    [TimelineAuthoringProperty("space", typeof(TimelineMotionContributionSpace))]
    [TimelineAuthoringProperty("channel", typeof(TimelineMotionChannel))]
    [TimelineAuthoringProperty("blendMode", typeof(TimelineMotionBlendMode))]
    [TimelineAuthoringProperty("priority", TimelineAuthoringPropertyKind.Integer)]
    [TimelineAuthoringProperty("consumeLowerChannels", TimelineAuthoringPropertyKind.Boolean)]
    public sealed partial class MotionCurveClip : Clip, ITimelineContentClosureSource
    {
        public override string ContractKind => TimelineContractKinds.MotionCurveClip;

        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public string CurveId = "MotionCurve";
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
        [SerializeField, ShowInInspector, OnValueChanged("RebindTimeline")]
        RootMotionCurveAsset m_SourceCurve;
        [SerializeField, ShowInInspector, OnValueChanged("RebindTimeline")]
        float m_SourceStartTime;
        [SerializeField, ShowInInspector, OnValueChanged("RebindTimeline")]
        float m_SourceEndTime;
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public AnimationCurve WeightCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public AnimationCurve EaseInCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public AnimationCurve EaseOutCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public RootMotionCurveAsset SourceCurve => m_SourceCurve;
        public float SourceStartTime => m_SourceStartTime;
        public float SourceEndTime => m_SourceEndTime;
        public float SourceDuration => Mathf.Max(0f, m_SourceEndTime - m_SourceStartTime);

        public void CollectContentClosure(TimelineContentClosureBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (!m_SourceCurve)
            {
                builder.AddError("timeline_motion_curve_missing", AuthoringId, "MotionCurveClip缺少RootMotionCurveAsset正式源引用。");
                return;
            }
            string contentHash = SourceContentHasher.Hash(JsonUtility.ToJson(m_SourceCurve));
            builder.AddDependency(
                $"motion-curve:{contentHash}",
                "timeline.motion-curve",
                m_SourceCurve.name,
                contentHash);
        }

        public int CurveEndFrame => StartFrame + Mathf.RoundToInt(SourceDuration * TimelineUtility.FrameRate);
        public AnimationCurve ProgramPositionX => ProgramCurve(SourcePositionX);
        public AnimationCurve ProgramPositionY => ProgramCurve(SourcePositionY);
        public AnimationCurve ProgramPositionZ => ProgramCurve(SourcePositionZ);
        public AnimationCurve ProgramYaw => ProgramCurve(RequireSource().LocalYaw);

        public AnimationCurve SourcePositionX => RequireSource().EvaluationMode == RootMotionCurveEvaluationMode.ForwardDistanceYaw
            ? ZeroCurve()
            : RequireSource().LocalPositionX;

        public AnimationCurve SourcePositionY => RequireSource().EvaluationMode == RootMotionCurveEvaluationMode.ForwardDistanceYaw
            ? ZeroCurve()
            : RequireSource().LocalPositionY;

        public AnimationCurve SourcePositionZ => RequireSource().EvaluationMode == RootMotionCurveEvaluationMode.ForwardDistanceYaw
            ? RequireSource().ForwardDistance
            : RequireSource().LocalPositionZ;

        public AnimationCurve SourceYaw => RequireSource().LocalYaw;

        RootMotionCurveAsset RequireSource()
        {
            if (!m_SourceCurve)
                throw new InvalidOperationException($"MotionCurveClip '{CurveId}' requires a RootMotionCurveAsset source.");
            return m_SourceCurve;
        }

        public void ConfigureSource(RootMotionCurveAsset source, float sourceStartTime, float sourceEndTime)
        {
            if (!source)
                throw new ArgumentNullException(nameof(source));
            if (!source.TryValidate(out string error))
                throw new InvalidOperationException(error);
            if (!float.IsFinite(sourceStartTime) || !float.IsFinite(sourceEndTime) ||
                sourceStartTime < 0f || sourceEndTime <= sourceStartTime || sourceEndTime > source.Duration)
                throw new ArgumentException("Motion curve source range is invalid.");
            m_SourceCurve = source;
            m_SourceStartTime = sourceStartTime;
            m_SourceEndTime = sourceEndTime;
            if (Track?.Timeline != null)
                RebindTimeline();
        }

        public Vector3 EvaluatePositionAtTimelineTime(float timelineTime)
        {
            float sourceTime = Mathf.Clamp(
                m_SourceStartTime + Mathf.Max(0f, timelineTime - StartTime),
                m_SourceStartTime,
                m_SourceEndTime);
            return RequireSource().EvaluatePosition(sourceTime);
        }

        public float EvaluateYawAtTimelineTime(float timelineTime)
        {
            float sourceTime = Mathf.Clamp(
                m_SourceStartTime + Mathf.Max(0f, timelineTime - StartTime),
                m_SourceStartTime,
                m_SourceEndTime);
            return RequireSource().EvaluateYaw(sourceTime);
        }

        public AnimationCurve CreateSourceDisplayCurve(AnimationCurve source, float timelineDuration)
        {
            return CreateSourceDisplayCurve(source, m_SourceStartTime, m_SourceEndTime, timelineDuration);
        }

        public AnimationCurve CreateSourceDisplayCurve(
            AnimationCurve source,
            float sourceStartTime,
            float sourceEndTime,
            float timelineDuration)
        {
            if (source == null)
                return new AnimationCurve();
            if (!float.IsFinite(sourceStartTime) || !float.IsFinite(sourceEndTime) ||
                sourceStartTime < 0f || sourceEndTime <= sourceStartTime ||
                sourceEndTime > RequireSource().Duration)
                throw new InvalidOperationException($"MotionCurveClip '{CurveId}' has an invalid source display range.");
            float sourceDuration = sourceEndTime - sourceStartTime;
            if (sourceDuration <= 0f)
                throw new InvalidOperationException($"MotionCurveClip '{CurveId}' has an invalid source duration.");

            var result = new AnimationCurve();
            Keyframe[] sourceKeys = source.keys;
            bool hasStartKey = false;
            bool hasEndKey = false;
            int firstKeyAfterStart = -1;
            int lastKeyBeforeEnd = -1;
            for (int index = 0; index < sourceKeys.Length; index++)
            {
                Keyframe key = sourceKeys[index];
                if (Mathf.Approximately(key.time, sourceStartTime))
                {
                    key.time = 0f;
                    result.AddKey(key);
                    hasStartKey = true;
                    continue;
                }
                if (Mathf.Approximately(key.time, sourceEndTime))
                {
                    key.time = sourceDuration;
                    result.AddKey(key);
                    hasEndKey = true;
                    continue;
                }
                if (key.time > sourceStartTime && firstKeyAfterStart < 0)
                    firstKeyAfterStart = index;
                if (key.time < sourceEndTime)
                    lastKeyBeforeEnd = index;
                if (key.time <= sourceStartTime || key.time >= sourceEndTime)
                    continue;
                key.time -= sourceStartTime;
                result.AddKey(key);
            }
            if (!hasStartKey)
            {
                float tangent = firstKeyAfterStart >= 0 ? sourceKeys[firstKeyAfterStart].inTangent : 0f;
                result.AddKey(new Keyframe(0f, source.Evaluate(sourceStartTime), tangent, tangent));
            }
            if (!hasEndKey)
            {
                float tangent = lastKeyBeforeEnd >= 0 ? sourceKeys[lastKeyBeforeEnd].outTangent : 0f;
                result.AddKey(new Keyframe(sourceDuration, source.Evaluate(sourceEndTime), tangent, tangent));
            }
            if (timelineDuration > sourceDuration)
                result.AddKey(new Keyframe(timelineDuration, source.Evaluate(sourceEndTime), 0f, 0f));
            result.preWrapMode = WrapMode.ClampForever;
            result.postWrapMode = WrapMode.ClampForever;
            return result;
        }

        static AnimationCurve ZeroCurve() => AnimationCurve.Linear(0f, 0f, 1f, 0f);

        public override void Init(Track track)
        {
            base.Init(track);
            RootMotionCurveAsset source = RequireSource();
            if (!source.TryValidate(out string error))
                throw new InvalidOperationException(error);
            if (!float.IsFinite(m_SourceStartTime) || !float.IsFinite(m_SourceEndTime) ||
                m_SourceStartTime < 0f || m_SourceEndTime <= m_SourceStartTime ||
                m_SourceEndTime > source.Duration)
                throw new InvalidOperationException($"MotionCurveClip '{CurveId}' has an invalid source range.");
            if (CurveEndFrame <= StartFrame || CurveEndFrame > EndFrame)
                throw new InvalidOperationException($"MotionCurveClip '{CurveId}' requires StartFrame < CurveEndFrame <= EndFrame.");
        }

        AnimationCurve ProgramCurve(AnimationCurve source)
        {
            AnimationCurve result = TimelineCurveAuthoring.CopyCurve(source);
            float duration = SourceDuration;
            if (duration <= 0f)
                throw new InvalidOperationException($"MotionCurveClip '{CurveId}' has an invalid source duration.");
            Keyframe[] keys = result.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                key.time = (key.time - m_SourceStartTime) / duration;
                key.inTangent *= duration;
                key.outTangent *= duration;
                keys[i] = key;
            }
            result.keys = keys;
            return result;
        }

#if UNITY_EDITOR
        public override ClipCapabilities Capabilities => ClipCapabilities.Resizable | ClipCapabilities.Mixable;

        public MotionCurveClip(Track track, int frame, RootMotionCurveAsset source) : base(track, frame)
        {
            if (!source)
                throw new ArgumentNullException(nameof(source));
            if (!source.TryValidate(out string error))
                throw new InvalidOperationException(error);
            if (source.Duration <= 0f)
                throw new ArgumentException("MotionCurve source duration must be positive.", nameof(source));
            EndFrame = StartFrame + Mathf.RoundToInt(source.Duration * TimelineUtility.FrameRate);
            m_SourceCurve = source;
            m_SourceStartTime = 0f;
            m_SourceEndTime = source.Duration;
        }
#endif
    }

    public static class MotionCurveTimelineContracts
    {
        public static readonly ITimelineContractProvider Provider = new TimelineContractProvider(
            new[]
            {
                new TimelineTrackContract(
                    TimelineContractKinds.MotionCurveTrack,
                    TimelineTrackOverlapPolicy.Blend,
                    TimelineCapability.BodyMotion,
                    TimelineExecutionDomain.Logic,
                    TimelineOutputKind.GameplayFact,
                    TimelineContractKinds.MotionCurveClip)
            },
            new[]
            {
                new TimelineClipContract(
                    TimelineContractKinds.MotionCurveClip,
                    TimelineContractKinds.MotionCurveTrack,
                    TimelineClipExecutionPhase.Commit,
                    TimelineCapability.BodyMotion,
                    true,
                    true,
                    TimelineExecutionDomain.Logic,
                    TimelineOutputKind.GameplayFact)
            });
    }
}
