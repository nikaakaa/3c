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

            public FixedCurve(AnimationCurve source)
            {
                Keyframe[] sourceKeys = source.keys;
                var keys = new FixedGameplayAbilityCurveKey[sourceKeys.Length];
                for (int index = 0; index < sourceKeys.Length; index++)
                {
                    Keyframe key = sourceKeys[index];
                    if (key.weightedMode != WeightedMode.None ||
                        !float.IsFinite(key.inTangent) || !float.IsFinite(key.outTangent))
                        throw new InvalidOperationException("Fixed Timeline motion requires finite, unweighted curve tangents.");
                    keys[index] = new FixedGameplayAbilityCurveKey(
                        FixedScalar.FromSingle(key.time), FixedScalar.FromSingle(key.value),
                        FixedScalar.FromSingle(key.inTangent), FixedScalar.FromSingle(key.outTangent),
                        FixedScalar.FromSingle(key.inWeight), FixedScalar.FromSingle(key.outWeight),
                        (int)key.weightedMode);
                }
                m_Curve = new FixedGameplayAbilityCurve((int)source.preWrapMode, (int)source.postWrapMode, keys);
            }

            public override FixedScalar Evaluate(FixedScalar time) => m_Curve.Evaluate(time, FixedScalar.Zero);
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

    internal readonly struct TimelineRuntimeMotionPosition
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

    internal sealed class TimelineRuntimeMotionSampling
    {
        readonly Dictionary<string, TimelineRuntimeMotionCurve> m_Curves = new(StringComparer.Ordinal);

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
        }

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
