using System;
using System.Collections.Generic;
using ThirdPersonSimulation.Fixed;
using UnityEngine;

namespace BTSMTL.Timeline.Runtime
{
    internal abstract class TimelineRuntimeCurve
    {
        public abstract FixedScalar Evaluate(FixedScalar time);

        public static TimelineRuntimeCurve Prepare(AnimationCurve curve, TimelineRuntimeNumericTarget target)
        {
            if (curve == null || curve.length == 0)
                throw new InvalidOperationException("Timeline motion requires a nonempty curve.");
            return target switch
            {
                TimelineRuntimeNumericTarget.Fixed => new FixedCurve(curve),
                TimelineRuntimeNumericTarget.Float32 => new FloatCurve(curve),
                _ => throw new ArgumentOutOfRangeException(nameof(target))
            };
        }

        sealed class FixedCurve : TimelineRuntimeCurve
        {
            readonly FixedGameplayAbilityCurve m_Curve;
            readonly bool[] m_SteppedSegments;

            public FixedCurve(AnimationCurve source)
            {
                Keyframe[] sourceKeys = source.keys;
                var keys = new FixedGameplayAbilityCurveKey[sourceKeys.Length];
                for (int index = 0; index + 1 < sourceKeys.Length; index++)
                {
                    if (!float.IsInfinity(sourceKeys[index].outTangent) && !float.IsInfinity(sourceKeys[index + 1].inTangent))
                        continue;
                    m_SteppedSegments ??= new bool[sourceKeys.Length - 1];
                    m_SteppedSegments[index] = true;
                }
                for (int index = 0; index < sourceKeys.Length; index++)
                {
                    Keyframe key = sourceKeys[index];
                    if (!float.IsFinite(key.time) || !float.IsFinite(key.value) ||
                        float.IsNaN(key.inTangent) || float.IsNaN(key.outTangent) ||
                        !float.IsFinite(key.inWeight) || !float.IsFinite(key.outWeight) ||
                        key.inWeight < 0f || key.inWeight > 1f || key.outWeight < 0f || key.outWeight > 1f)
                        throw new InvalidOperationException("Fixed Timeline motion contains an invalid curve key.");
                    keys[index] = new FixedGameplayAbilityCurveKey(
                        FixedScalar.FromSingle(key.time), FixedScalar.FromSingle(key.value),
                        float.IsInfinity(key.inTangent) ? FixedScalar.Zero : FixedScalar.FromSingle(key.inTangent),
                        float.IsInfinity(key.outTangent) ? FixedScalar.Zero : FixedScalar.FromSingle(key.outTangent),
                        FixedScalar.FromSingle(key.inWeight), FixedScalar.FromSingle(key.outWeight),
                        (int)key.weightedMode);
                }
                m_Curve = new FixedGameplayAbilityCurve((int)source.preWrapMode, (int)source.postWrapMode, keys);
            }

            public override FixedScalar Evaluate(FixedScalar time)
            {
                if (m_SteppedSegments != null && time > m_Curve.Keys[0].Time && time < m_Curve.Keys[m_Curve.Keys.Count - 1].Time)
                {
                    int low = 0;
                    int high = m_Curve.Keys.Count - 1;
                    while (high - low > 1)
                    {
                        int middle = low + (high - low) / 2;
                        if (m_Curve.Keys[middle].Time <= time)
                            low = middle;
                        else
                            high = middle;
                    }
                    if (m_SteppedSegments[low])
                        return m_Curve.Keys[low].Value;
                }
                return m_Curve.Evaluate(time, FixedScalar.Zero);
            }
        }

        sealed class FloatCurve : TimelineRuntimeCurve
        {
            readonly AnimationCurve m_Curve;

            public FloatCurve(AnimationCurve source)
            {
                m_Curve = new AnimationCurve(source.keys)
                {
                    preWrapMode = source.preWrapMode,
                    postWrapMode = source.postWrapMode
                };
            }

            public override FixedScalar Evaluate(FixedScalar time) => FixedScalar.FromSingle(m_Curve.Evaluate(time.ToSingle()));
        }
    }

    public readonly struct TimelineRuntimeMotionPosition
    {
        public TimelineRuntimeMotionPosition(FixedScalar x, FixedScalar y, FixedScalar z, FixedScalar yaw)
        {
            X = x;
            Y = y;
            Z = z;
            Yaw = yaw;
        }

        public FixedScalar X { get; }
        public FixedScalar Y { get; }
        public FixedScalar Z { get; }
        public FixedScalar Yaw { get; }
    }

    internal sealed class TimelineRuntimeMotionCurve
    {
        readonly MotionCurveClip m_Clip;
        readonly FixedScalar m_SourceStart;
        readonly FixedScalar m_SourceEnd;
        readonly TimelineRuntimeCurve m_X;
        readonly TimelineRuntimeCurve m_Y;
        readonly TimelineRuntimeCurve m_Z;
        readonly TimelineRuntimeCurve m_Yaw;
        readonly TimelineRuntimeCurve m_Weight;
        readonly TimelineRuntimeCurve m_EaseIn;
        readonly TimelineRuntimeCurve m_EaseOut;

        public TimelineRuntimeMotionCurve(MotionCurveClip clip, TimelineRuntimeNumericTarget target)
        {
            m_Clip = clip;
            m_SourceStart = FixedScalar.FromSingle(clip.SourceStartTime);
            m_SourceEnd = FixedScalar.FromSingle(clip.SourceEndTime);
            m_X = TimelineRuntimeCurve.Prepare(clip.SourcePositionX, target);
            m_Y = TimelineRuntimeCurve.Prepare(clip.SourcePositionY, target);
            m_Z = TimelineRuntimeCurve.Prepare(clip.SourcePositionZ, target);
            m_Yaw = TimelineRuntimeCurve.Prepare(clip.SourceYaw, target);
            m_Weight = TimelineRuntimeCurve.Prepare(clip.WeightCurve, target);
            m_EaseIn = TimelineRuntimeCurve.Prepare(clip.EaseInCurve, target);
            m_EaseOut = TimelineRuntimeCurve.Prepare(clip.EaseOutCurve, target);
        }

        public TimelineRuntimeMotionPosition Evaluate(FixedScalar timelineTime)
        {
            FixedScalar sourceTime = FixedScalar.Clamp(
                m_SourceStart + FixedScalar.Clamp(timelineTime - m_Clip.StartTime, FixedScalar.Zero, m_Clip.DurationTime),
                m_SourceStart, m_SourceEnd);
            return new TimelineRuntimeMotionPosition(
                m_X.Evaluate(sourceTime), m_Y.Evaluate(sourceTime), m_Z.Evaluate(sourceTime), m_Yaw.Evaluate(sourceTime));
        }

        public bool TrySample(
            FixedScalar previousTime, FixedScalar currentTime, string sourceId, string sourceName,
            out TimelineMotionCurveContribution contribution)
        {
            contribution = default;
            if (currentTime <= m_Clip.StartTime || previousTime >= m_Clip.EndTime)
                return false;
            FixedScalar previousLocal = FixedScalar.Clamp(previousTime - m_Clip.StartTime, FixedScalar.Zero, m_Clip.DurationTime);
            FixedScalar local = FixedScalar.Clamp(currentTime - m_Clip.StartTime, FixedScalar.Zero, m_Clip.DurationTime);
            if (previousLocal == local)
                return false;
            FixedScalar normalized = local / m_Clip.DurationTime;
            FixedScalar weight = m_Weight.Evaluate(normalized);
            if (m_Clip.EaseInTime > FixedScalar.Zero && local < m_Clip.EaseInTime)
                weight *= m_EaseIn.Evaluate(local / m_Clip.EaseInTime);
            FixedScalar remaining = FixedScalar.Max(FixedScalar.Zero, m_Clip.EndTime - currentTime);
            if (m_Clip.EaseOutTime > FixedScalar.Zero && remaining < m_Clip.EaseOutTime)
                weight *= FixedScalar.One - m_EaseOut.Evaluate(FixedScalar.One - remaining / m_Clip.EaseOutTime);
            weight = FixedScalar.Clamp(weight, FixedScalar.Zero, FixedScalar.One);
            if (weight == FixedScalar.Zero)
                return false;
            TimelineRuntimeMotionPosition previous = Evaluate(previousTime);
            TimelineRuntimeMotionPosition current = Evaluate(currentTime);
            contribution = new TimelineMotionCurveContribution(
                sourceId, sourceName, m_Clip.Track.Name, m_Clip.CurveId,
                m_Clip.Space, m_Clip.Channel, m_Clip.BlendMode,
                current.X - previous.X, current.Y - previous.Y, current.Z - previous.Z,
                current.Yaw - previous.Yaw, m_Clip.Priority, weight, m_Clip.ConsumeLowerChannels, normalized);
            return contribution.CanResolve;
        }
    }

    internal sealed class TimelineRuntimeMotionWarpCurve
    {
        readonly MotionWarpClip m_Clip;
        readonly TimelineRuntimeMotionCurve m_Source;
        readonly TimelineRuntimeCurve m_PositionProgress;
        readonly TimelineRuntimeCurve m_YawProgress;
        readonly TimelineRuntimeCurve m_YawResponse;
        readonly TimelineRuntimeCurve m_InputYawResponse;
        readonly TimelineRuntimeMotionPosition m_Start;
        readonly TimelineRuntimeMotionPosition m_End;

        public TimelineRuntimeMotionWarpCurve(MotionWarpClip clip, TimelineRuntimeMotionCurve source, TimelineRuntimeNumericTarget target)
        {
            m_Clip = clip;
            m_Source = source;
            m_Start = source.Evaluate(clip.StartTime);
            m_End = source.Evaluate(clip.EndTime);
            if (clip.TranslationMode is MotionWarpTranslationMode.SkewToTarget or MotionWarpTranslationMode.LinearToTarget)
                m_PositionProgress = TimelineRuntimeCurve.Prepare(clip.PositionProgressCurve, target);
            if (clip.RotationMode != MotionWarpRotationMode.Disabled && clip.RotationMethod == MotionWarpRotationMethod.ProgressCurve)
                m_YawProgress = TimelineRuntimeCurve.Prepare(clip.YawProgressCurve, target);
            if (clip.UsesYawResponse)
            {
                m_YawResponse = TimelineRuntimeCurve.Prepare(clip.YawResponseCurve, target);
                m_InputYawResponse = TimelineRuntimeCurve.Prepare(clip.InputYawResponseCurve, target);
            }
        }

        public TimelineRuntimeMotionWarpRequest Sample(FixedScalar previousTime, FixedScalar time, int cycle)
        {
            previousTime = FixedScalar.Max(previousTime, m_Clip.StartTime);
            time = FixedScalar.Min(time, m_Clip.EndTime);
            FixedScalar previousNormalized = (previousTime - m_Clip.StartTime) / m_Clip.DurationTime;
            FixedScalar normalized = (time - m_Clip.StartTime) / m_Clip.DurationTime;
            return new TimelineRuntimeMotionWarpRequest(
                m_Clip.AuthoringId, m_Clip.SourceMotionClipId, previousTime, time, cycle,
                m_Clip.StartTime, m_Clip.EndTime, m_Start, m_End,
                m_Source.Evaluate(previousTime), m_Source.Evaluate(time),
                EvaluateProgress(m_PositionProgress, previousNormalized), EvaluateProgress(m_YawProgress, previousNormalized),
                EvaluateProgress(m_PositionProgress, normalized), EvaluateProgress(m_YawProgress, normalized),
                m_YawResponse == null ? FixedScalar.Zero : m_YawResponse.Evaluate(normalized),
                m_Clip.SteeringInputId, m_InputYawResponse == null ? FixedScalar.Zero : m_InputYawResponse.Evaluate(normalized),
                m_Clip.TranslationMode, m_Clip.TargetOffsetSpace, m_Clip.RotationMode, m_Clip.RotationMethod,
                m_Clip.TargetPlanarOffset, m_Clip.TargetYawOffsetDegrees, m_Clip.MaxTotalPositionCorrection,
                m_Clip.MaxTotalYawCorrectionDegrees, m_Clip.MaximumYawRateDegreesPerSecond, m_Clip.LimitPolicy);
        }

        static FixedScalar EvaluateProgress(TimelineRuntimeCurve curve, FixedScalar normalized) =>
            curve == null ? normalized : FixedScalar.Clamp(curve.Evaluate(normalized), FixedScalar.Zero, FixedScalar.One);
    }

    internal sealed class TimelineRuntimeMotionSampling
    {
        readonly Dictionary<string, TimelineRuntimeMotionCurve> m_Curves = new(StringComparer.Ordinal);
        readonly Dictionary<string, TimelineRuntimeMotionWarpCurve> m_Warps = new(StringComparer.Ordinal);

        public TimelineRuntimeMotionSampling(TimelineData timeline, TimelineRuntimeNumericTarget target)
        {
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                if (track is not MotionCurveTrack || track.PersistentMuted || track.ExecutionDomain != TimelineExecutionDomain.Logic)
                    continue;
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    if (track.Clips[clipIndex] is MotionCurveClip clip)
                        m_Curves.Add(clip.AuthoringId, new TimelineRuntimeMotionCurve(clip, target));
                }
            }
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                if (track is not MotionWarpTrack || track.PersistentMuted || track.ExecutionDomain != TimelineExecutionDomain.Logic)
                    continue;
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    if (track.Clips[clipIndex] is not MotionWarpClip clip)
                        continue;
                    if (!m_Curves.TryGetValue(clip.SourceMotionClipId, out TimelineRuntimeMotionCurve source))
                    {
                        if (!MotionWarpAuthoring.TryResolveSource(timeline, clip.SourceMotionClipId, out MotionCurveClip sourceClip))
                            throw new InvalidOperationException($"MotionWarp '{clip.AuthoringId}' has no source curve '{clip.SourceMotionClipId}'.");
                        source = new TimelineRuntimeMotionCurve(sourceClip, target);
                        m_Curves.Add(sourceClip.AuthoringId, source);
                    }
                    m_Warps.Add(clip.AuthoringId, new TimelineRuntimeMotionWarpCurve(clip, source, target));
                }
            }
        }

        public TimelineRuntimeMotionWarpRequest SampleWarp(string clipId, FixedScalar previousTime, FixedScalar time, int cycle) =>
            m_Warps[clipId].Sample(previousTime, time, cycle);

        public void Sample(
            MotionCurveTrack track, FixedScalar previousTime, FixedScalar currentTime,
            string sourceId, string sourceName, TimelineRuntimeSampleBuffer<TimelineMotionCurveContribution> contributions)
        {
            for (int index = 0; index < track.Clips.Count; index++)
            {
                if (track.Clips[index] is MotionCurveClip clip &&
                    m_Curves[clip.AuthoringId].TrySample(previousTime, currentTime, sourceId, sourceName, out var contribution))
                    contributions.Add(contribution);
            }
        }
    }
}
