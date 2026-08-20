using System;
using System.Collections.Generic;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public sealed class CharacterAnimationBlendSpaceSamplePhasePlan
    {
        public CharacterAnimationBlendSpaceSamplePhasePlan(
            CharacterAnimationBlendSpaceSampleId sampleId,
            CharacterAnimationBlendSpaceSampleRole role,
            float sourceDurationSeconds,
            bool loop,
            float stationaryNormalizedTime,
            int clipPhasePlanIndex)
        {
            if (!sampleId.IsValid || !Enum.IsDefined(typeof(CharacterAnimationBlendSpaceSampleRole), role) ||
                !float.IsFinite(sourceDurationSeconds) || sourceDurationSeconds <= 0f ||
                !float.IsFinite(stationaryNormalizedTime) || stationaryNormalizedTime < 0f || stationaryNormalizedTime > 1f ||
                role == CharacterAnimationBlendSpaceSampleRole.StationaryPose && clipPhasePlanIndex >= 0)
                throw new ArgumentException("Blend Space Sample phase plan is invalid.");
            SampleId = sampleId;
            Role = role;
            SourceDurationSeconds = sourceDurationSeconds;
            Loop = loop;
            StationaryNormalizedTime = role == CharacterAnimationBlendSpaceSampleRole.StationaryPose ? stationaryNormalizedTime : 0f;
            ClipPhasePlanIndex = clipPhasePlanIndex;
        }

        public CharacterAnimationBlendSpaceSampleId SampleId { get; }
        public CharacterAnimationBlendSpaceSampleRole Role { get; }
        public float SourceDurationSeconds { get; }
        public bool Loop { get; }
        public float StationaryNormalizedTime { get; }
        public int ClipPhasePlanIndex { get; }
    }

    public sealed class CharacterAnimationBlendSpacePhasePlan
    {
        readonly CharacterAnimationBlendSpaceSamplePhasePlan[] m_Samples;
        readonly AnimationClipPhasePlan[] m_ClipPhasePlans;

        CharacterAnimationBlendSpacePhasePlan(
            CharacterAnimationBlendSpacePhasePolicy policy,
            int referenceSampleIndex,
            CharacterAnimationBlendSpaceSamplePhasePlan[] samples,
            AnimationClipPhasePlan[] clipPhasePlans)
        {
            Policy = policy;
            ReferenceSampleIndex = referenceSampleIndex;
            m_Samples = samples;
            m_ClipPhasePlans = clipPhasePlans;
        }

        public CharacterAnimationBlendSpacePhasePolicy Policy { get; }
        public int ReferenceSampleIndex { get; }
        public int SampleCount => m_Samples.Length;
        public CharacterAnimationBlendSpaceSamplePhasePlan GetSample(int index) => m_Samples[index];
        public AnimationClipPhasePlan GetClipPhasePlan(int index) => m_ClipPhasePlans[index];

        public static CharacterAnimationBlendSpacePhasePlan Create(
            CharacterAnimationBlendSpacePlan plan,
            IReadOnlyList<AnimationClipPhasePlan> clipPhasePlans)
        {
            if (plan == null || clipPhasePlans == null)
                throw new ArgumentNullException();
            var samples = new CharacterAnimationBlendSpaceSamplePhasePlan[plan.Samples.Count];
            var phases = new AnimationClipPhasePlan[clipPhasePlans.Count];
            for (int i = 0; i < phases.Length; i++)
            {
                phases[i] = clipPhasePlans[i] ?? throw new InvalidOperationException($"Animation Clip Phase plan #{i} is missing.");
                phases[i].RequireValid();
            }
            for (int i = 0; i < samples.Length; i++)
            {
                CharacterAnimationBlendSpaceSamplePlan sample = plan.Samples[i];
                int phaseIndex = -1;
                if (plan.PhasePolicy == CharacterAnimationBlendSpacePhasePolicy.LocomotionPhase &&
                    sample.Role == CharacterAnimationBlendSpaceSampleRole.DynamicCycle)
                {
                    for (int phase = 0; phase < phases.Length; phase++)
                    {
                        if (string.Equals(phases[phase].ClipIdentity, sample.ClipIdentity, StringComparison.Ordinal))
                        {
                            phaseIndex = phase;
                            break;
                        }
                    }
                    if (phaseIndex < 0)
                        throw new InvalidOperationException($"Blend Space Sample '{sample.SampleId}' has no compiled Locomotion Phase plan.");
                }
                samples[i] = new CharacterAnimationBlendSpaceSamplePhasePlan(
                    sample.SampleId,
                    sample.Role,
                    sample.SourceDurationSeconds,
                    sample.Clip.isLooping,
                    sample.StationaryNormalizedTime,
                    phaseIndex);
            }
            if (plan.PhasePolicy == CharacterAnimationBlendSpacePhasePolicy.LocomotionPhase &&
                (plan.PhaseReferenceSampleIndex < 0 ||
                 samples[plan.PhaseReferenceSampleIndex].ClipPhasePlanIndex < 0))
                throw new InvalidOperationException("Blend Space Locomotion Phase reference is invalid.");
            return new CharacterAnimationBlendSpacePhasePlan(
                plan.PhasePolicy,
                plan.PhaseReferenceSampleIndex,
                samples,
                phases);
        }
    }

    public readonly struct CharacterAnimationBlendSpaceCanonicalPhase
    {
        public CharacterAnimationBlendSpaceCanonicalPhase(float normalizedPhase, int cycle, double unwrappedPhase)
        {
            if (!float.IsFinite(normalizedPhase) || normalizedPhase < 0f || normalizedPhase > 1f ||
                cycle < 0 || !double.IsFinite(unwrappedPhase))
                throw new ArgumentException("Blend Space canonical phase is invalid.");
            NormalizedPhase = normalizedPhase;
            Cycle = cycle;
            UnwrappedPhase = unwrappedPhase;
        }

        public float NormalizedPhase { get; }
        public int Cycle { get; }
        public double UnwrappedPhase { get; }
    }

    public readonly struct CharacterAnimationBlendSpaceSampleTime
    {
        public CharacterAnimationBlendSpaceSampleTime(
            CharacterAnimationBlendSpaceSampleId sampleId,
            float clipTime,
            double rawContinuousTime,
            float normalizedTime,
            int cycle)
        {
            if (!sampleId.IsValid || !float.IsFinite(clipTime) || clipTime < 0f ||
                !double.IsFinite(rawContinuousTime) || rawContinuousTime < 0d ||
                !float.IsFinite(normalizedTime) || normalizedTime < 0f || normalizedTime > 1f || cycle < 0)
                throw new ArgumentException("Blend Space Sample time is invalid.");
            SampleId = sampleId;
            ClipTime = clipTime;
            RawContinuousTime = rawContinuousTime;
            NormalizedTime = normalizedTime;
            Cycle = cycle;
        }

        public CharacterAnimationBlendSpaceSampleId SampleId { get; }
        public float ClipTime { get; }
        public double RawContinuousTime { get; }
        public float NormalizedTime { get; }
        public int Cycle { get; }
    }

    public sealed class CharacterAnimationBlendSpaceTimePage
    {
        readonly CharacterAnimationBlendSpaceSampleTime[] m_Values;

        public CharacterAnimationBlendSpaceTimePage(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_Values = new CharacterAnimationBlendSpaceSampleTime[capacity];
        }

        public int Capacity => m_Values.Length;
        public int Count { get; private set; }
        public CharacterAnimationBlendSpaceSampleTime Get(int index) =>
            index >= 0 && index < Count ? m_Values[index] : throw new ArgumentOutOfRangeException(nameof(index));
        internal void Reset() => Count = 0;
        internal void Add(CharacterAnimationBlendSpaceSampleTime value)
        {
            if (Count >= Capacity)
                throw new InvalidOperationException("Blend Space Sample time page capacity exceeded.");
            m_Values[Count++] = value;
        }
    }

    public enum CharacterAnimationBlendSpacePhaseFailure : byte
    {
        None = 0,
        InvalidPlan = 1,
        InvalidTime = 2,
        MissingPhasePlan = 3,
        OutsideCoverage = 4,
        CapacityExceeded = 5
    }

    public static class CharacterAnimationBlendSpacePhaseMapper
    {
        public static bool Map(
            CharacterAnimationBlendSpacePhasePlan plan,
            double effectiveTime,
            int cycle,
            CharacterAnimationBlendSpaceTimePage output,
            out CharacterAnimationBlendSpaceCanonicalPhase canonicalPhase,
            out CharacterAnimationBlendSpacePhaseFailure failure) =>
            Map(plan, effectiveTime, cycle, null, output, out canonicalPhase, out failure);

        public static bool Map(
            CharacterAnimationBlendSpacePhasePlan plan,
            double effectiveTime,
            int cycle,
            double[] previousRawTimes,
            CharacterAnimationBlendSpaceTimePage output,
            out CharacterAnimationBlendSpaceCanonicalPhase canonicalPhase,
            out CharacterAnimationBlendSpacePhaseFailure failure)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            output.Reset();
            canonicalPhase = default;
            if (plan == null || plan.SampleCount == 0)
            {
                failure = CharacterAnimationBlendSpacePhaseFailure.InvalidPlan;
                return false;
            }
            if (!double.IsFinite(effectiveTime) || effectiveTime < 0d || cycle < 0)
            {
                failure = CharacterAnimationBlendSpacePhaseFailure.InvalidTime;
                return false;
            }
            if (output.Capacity < plan.SampleCount || previousRawTimes != null && previousRawTimes.Length < plan.SampleCount)
            {
                failure = CharacterAnimationBlendSpacePhaseFailure.CapacityExceeded;
                return false;
            }
            try
            {
                return plan.Policy == CharacterAnimationBlendSpacePhasePolicy.SharedNormalizedPhase
                    ? MapNormalized(plan, effectiveTime, previousRawTimes, output, out canonicalPhase, out failure)
                    : MapLocomotionPhase(plan, effectiveTime, previousRawTimes, output, out canonicalPhase, out failure);
            }
            catch (InvalidOperationException)
            {
                output.Reset();
                canonicalPhase = default;
                failure = CharacterAnimationBlendSpacePhaseFailure.OutsideCoverage;
                return false;
            }
        }

        static bool MapNormalized(
            CharacterAnimationBlendSpacePhasePlan plan,
            double effectiveTime,
            double[] previousRawTimes,
            CharacterAnimationBlendSpaceTimePage output,
            out CharacterAnimationBlendSpaceCanonicalPhase canonicalPhase,
            out CharacterAnimationBlendSpacePhaseFailure failure)
        {
            CharacterAnimationBlendSpaceSamplePhasePlan clock = FirstDynamic(plan);
            if (clock == null)
            {
                canonicalPhase = default;
                failure = CharacterAnimationBlendSpacePhaseFailure.InvalidPlan;
                return false;
            }
            double unwrapped = effectiveTime / clock.SourceDurationSeconds;
            float normalized = Repeat01(unwrapped);
            int phaseCycle = checked((int)Math.Floor(unwrapped));
            canonicalPhase = new CharacterAnimationBlendSpaceCanonicalPhase(normalized, phaseCycle, unwrapped);
            for (int i = 0; i < plan.SampleCount; i++)
            {
                CharacterAnimationBlendSpaceSamplePhasePlan sample = plan.GetSample(i);
                double raw = sample.Role == CharacterAnimationBlendSpaceSampleRole.StationaryPose
                    ? sample.StationaryNormalizedTime * sample.SourceDurationSeconds
                    : unwrapped * sample.SourceDurationSeconds;
                WriteSampleTime(sample, raw, previousRawTimes, i, output);
            }
            failure = CharacterAnimationBlendSpacePhaseFailure.None;
            return true;
        }

        static bool MapLocomotionPhase(
            CharacterAnimationBlendSpacePhasePlan plan,
            double effectiveTime,
            double[] previousRawTimes,
            CharacterAnimationBlendSpaceTimePage output,
            out CharacterAnimationBlendSpaceCanonicalPhase canonicalPhase,
            out CharacterAnimationBlendSpacePhaseFailure failure)
        {
            CharacterAnimationBlendSpaceSamplePhasePlan reference = plan.GetSample(plan.ReferenceSampleIndex);
            if (reference.ClipPhasePlanIndex < 0)
            {
                canonicalPhase = default;
                failure = CharacterAnimationBlendSpacePhaseFailure.MissingPhasePlan;
                return false;
            }
            AnimationClipPhasePlan referenceClip = plan.GetClipPhasePlan(reference.ClipPhasePlanIndex);
            double unwrappedPhase = referenceClip.Forward(effectiveTime);
            float normalized = Repeat01(unwrappedPhase);
            int phaseCycle = checked((int)Math.Floor(unwrappedPhase));
            canonicalPhase = new CharacterAnimationBlendSpaceCanonicalPhase(normalized, phaseCycle, unwrappedPhase);
            for (int i = 0; i < plan.SampleCount; i++)
            {
                CharacterAnimationBlendSpaceSamplePhasePlan sample = plan.GetSample(i);
                if (sample.Role == CharacterAnimationBlendSpaceSampleRole.StationaryPose)
                {
                    WriteSampleTime(sample, sample.StationaryNormalizedTime * sample.SourceDurationSeconds, previousRawTimes, i, output);
                    continue;
                }
                if (sample.ClipPhasePlanIndex < 0)
                {
                    output.Reset();
                    canonicalPhase = default;
                    failure = CharacterAnimationBlendSpacePhaseFailure.MissingPhasePlan;
                    return false;
                }
                AnimationClipPhasePlan clipPhase = plan.GetClipPhasePlan(sample.ClipPhasePlanIndex);
                double continuation = previousRawTimes != null && previousRawTimes[i] >= 0d
                    ? previousRawTimes[i]
                    : effectiveTime * sample.SourceDurationSeconds / reference.SourceDurationSeconds;
                double raw = clipPhase.Inverse(unwrappedPhase, continuation);
                WriteSampleTime(sample, raw, previousRawTimes, i, output);
            }
            failure = CharacterAnimationBlendSpacePhaseFailure.None;
            return true;
        }

        static CharacterAnimationBlendSpaceSamplePhasePlan FirstDynamic(CharacterAnimationBlendSpacePhasePlan plan)
        {
            for (int i = 0; i < plan.SampleCount; i++)
            {
                CharacterAnimationBlendSpaceSamplePhasePlan sample = plan.GetSample(i);
                if (sample.Role == CharacterAnimationBlendSpaceSampleRole.DynamicCycle)
                    return sample;
            }
            return null;
        }

        static void WriteSampleTime(
            CharacterAnimationBlendSpaceSamplePhasePlan sample,
            double raw,
            double[] previousRawTimes,
            int index,
            CharacterAnimationBlendSpaceTimePage output)
        {
            raw = Math.Max(0d, raw);
            int cycle;
            float clipTime;
            if (sample.Loop && sample.Role == CharacterAnimationBlendSpaceSampleRole.DynamicCycle)
            {
                cycle = checked((int)Math.Floor(raw / sample.SourceDurationSeconds));
                clipTime = (float)(raw - cycle * sample.SourceDurationSeconds);
                if (clipTime >= sample.SourceDurationSeconds)
                    clipTime = 0f;
            }
            else
            {
                cycle = 0;
                clipTime = (float)Math.Min(raw, sample.SourceDurationSeconds);
            }
            float normalized = Math.Min(1f, clipTime / sample.SourceDurationSeconds);
            output.Add(new CharacterAnimationBlendSpaceSampleTime(sample.SampleId, clipTime, raw, normalized, cycle));
            if (previousRawTimes != null)
                previousRawTimes[index] = raw;
        }

        static float Repeat01(double value)
        {
            double result = value - Math.Floor(value);
            return (float)result;
        }
    }
}
