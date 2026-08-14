using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct CharacterAnimationBlendSpaceMarkerPlan
    {
        public CharacterAnimationBlendSpaceMarkerPlan(string markerId, float normalizedTime)
        {
            if (string.IsNullOrWhiteSpace(markerId) || !float.IsFinite(normalizedTime) || normalizedTime < 0f || normalizedTime >= 1f)
                throw new ArgumentException("Blend Space marker plan is invalid.");
            MarkerId = markerId.Trim();
            NormalizedTime = normalizedTime;
        }

        public string MarkerId { get; }
        public float NormalizedTime { get; }
    }

    public sealed class CharacterAnimationBlendSpaceSamplePhasePlan
    {
        readonly CharacterAnimationBlendSpaceMarkerPlan[] m_Markers;

        public CharacterAnimationBlendSpaceSamplePhasePlan(
            CharacterAnimationBlendSpaceSampleId sampleId,
            CharacterAnimationBlendSpaceSampleRole role,
            float clipLength,
            float stationaryNormalizedTime,
            CharacterAnimationBlendSpaceMarkerPlan[] markers,
            AnimationFootPhaseTimeWarpPlan footPhaseWarp)
        {
            if (!sampleId.IsValid || !Enum.IsDefined(typeof(CharacterAnimationBlendSpaceSampleRole), role) ||
                !float.IsFinite(clipLength) || clipLength <= 0f ||
                !float.IsFinite(stationaryNormalizedTime) || stationaryNormalizedTime < 0f || stationaryNormalizedTime > 1f)
                throw new ArgumentException("Blend Space Sample phase plan is invalid.");
            SampleId = sampleId;
            Role = role;
            ClipLength = clipLength;
            StationaryNormalizedTime = role == CharacterAnimationBlendSpaceSampleRole.StationaryPose ? stationaryNormalizedTime : 0f;
            m_Markers = markers == null ? Array.Empty<CharacterAnimationBlendSpaceMarkerPlan>() : (CharacterAnimationBlendSpaceMarkerPlan[])markers.Clone();
            FootPhaseWarp = footPhaseWarp;
            FootPhaseWarp?.RequireValid();
        }

        public CharacterAnimationBlendSpaceSampleId SampleId { get; }
        public CharacterAnimationBlendSpaceSampleRole Role { get; }
        public float ClipLength { get; }
        public float StationaryNormalizedTime { get; }
        public int MarkerCount => m_Markers.Length;
        public CharacterAnimationBlendSpaceMarkerPlan GetMarker(int index) => m_Markers[index];
        public AnimationFootPhaseTimeWarpPlan FootPhaseWarp { get; }
    }

    public sealed class CharacterAnimationBlendSpacePhasePlan
    {
        readonly CharacterAnimationBlendSpaceSamplePhasePlan[] m_Samples;

        public CharacterAnimationBlendSpacePhasePlan(
            CharacterAnimationBlendSpacePhasePolicy policy,
            int referenceSampleIndex,
            CharacterAnimationBlendSpaceSamplePhasePlan[] samples)
        {
            if (!Enum.IsDefined(typeof(CharacterAnimationBlendSpacePhasePolicy), policy) || samples == null || samples.Length == 0)
                throw new ArgumentException("Blend Space phase plan is invalid.");
            if ((policy == CharacterAnimationBlendSpacePhasePolicy.MarkerSegmentPhase ||
                 policy == CharacterAnimationBlendSpacePhasePolicy.GeneratedFootPhase) &&
                (referenceSampleIndex < 0 || referenceSampleIndex >= samples.Length ||
                 samples[referenceSampleIndex].Role != CharacterAnimationBlendSpaceSampleRole.DynamicCycle))
                throw new ArgumentException("Marker synchronized Blend Space reference sample is invalid.");
            if (policy == CharacterAnimationBlendSpacePhasePolicy.SharedNormalizedPhase && referenceSampleIndex != -1)
                throw new ArgumentException("Shared normalized Blend Space cannot retain a reference sample.");
            Policy = policy;
            ReferenceSampleIndex = referenceSampleIndex;
            m_Samples = (CharacterAnimationBlendSpaceSamplePhasePlan[])samples.Clone();
            for (int i = 0; i < m_Samples.Length; i++)
            {
                CharacterAnimationBlendSpaceSamplePhasePlan sample = m_Samples[i] ??
                    throw new ArgumentException("Blend Space Sample phase plan is missing.", nameof(samples));
                bool requiresWarp =
                    policy == CharacterAnimationBlendSpacePhasePolicy.GeneratedFootPhase &&
                    i != referenceSampleIndex &&
                    sample.Role == CharacterAnimationBlendSpaceSampleRole.DynamicCycle;
                if (requiresWarp != (sample.FootPhaseWarp != null))
                    throw new ArgumentException("Blend Space Foot Phase Warp presence is invalid.", nameof(samples));
            }
        }

        public CharacterAnimationBlendSpacePhasePolicy Policy { get; }
        public int ReferenceSampleIndex { get; }
        public int SampleCount => m_Samples.Length;
        public CharacterAnimationBlendSpaceSamplePhasePlan GetSample(int index) => m_Samples[index];

        public int FindSampleIndex(CharacterAnimationBlendSpaceSampleId sampleId)
        {
            for (int i = 0; i < m_Samples.Length; i++)
            {
                if (m_Samples[i].SampleId.Equals(sampleId))
                    return i;
            }
            return -1;
        }
    }

    public readonly struct CharacterAnimationBlendSpaceCanonicalPhase
    {
        public CharacterAnimationBlendSpaceCanonicalPhase(
            float normalizedPhase,
            int cycle,
            string previousMarkerId,
            string nextMarkerId,
            float segmentFraction,
            bool markerMapped)
        {
            if (!float.IsFinite(normalizedPhase) || normalizedPhase < 0f || normalizedPhase > 1f || cycle < 0 ||
                !float.IsFinite(segmentFraction) || segmentFraction < 0f || segmentFraction > 1f ||
                markerMapped && (string.IsNullOrWhiteSpace(previousMarkerId) || string.IsNullOrWhiteSpace(nextMarkerId)))
                throw new ArgumentException("Blend Space canonical phase is invalid.");
            NormalizedPhase = normalizedPhase;
            Cycle = cycle;
            PreviousMarkerId = previousMarkerId ?? string.Empty;
            NextMarkerId = nextMarkerId ?? string.Empty;
            SegmentFraction = segmentFraction;
            MarkerMapped = markerMapped;
        }

        public float NormalizedPhase { get; }
        public int Cycle { get; }
        public string PreviousMarkerId { get; }
        public string NextMarkerId { get; }
        public float SegmentFraction { get; }
        public bool MarkerMapped { get; }
    }

    public readonly struct CharacterAnimationBlendSpaceSampleTime
    {
        public CharacterAnimationBlendSpaceSampleTime(CharacterAnimationBlendSpaceSampleId sampleId, float clipTime, float normalizedTime, int cycle)
        {
            if (!sampleId.IsValid || !float.IsFinite(clipTime) || clipTime < 0f ||
                !float.IsFinite(normalizedTime) || normalizedTime < 0f || normalizedTime > 1f || cycle < 0)
                throw new ArgumentException("Blend Space Sample time is invalid.");
            SampleId = sampleId;
            ClipTime = clipTime;
            NormalizedTime = normalizedTime;
            Cycle = cycle;
        }

        public CharacterAnimationBlendSpaceSampleId SampleId { get; }
        public float ClipTime { get; }
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
        public CharacterAnimationBlendSpaceSampleTime Get(int index) => index >= 0 && index < Count ? m_Values[index] : throw new ArgumentOutOfRangeException(nameof(index));

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
        MissingMarkerSegment = 3,
        CapacityExceeded = 4
    }

    public static class CharacterAnimationBlendSpacePhaseMapper
    {
        public static bool Map(
            CharacterAnimationBlendSpacePhasePlan plan,
            double effectiveTime,
            int cycle,
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
            if (double.IsNaN(effectiveTime) || double.IsInfinity(effectiveTime) || effectiveTime < 0d || cycle < 0)
            {
                failure = CharacterAnimationBlendSpacePhaseFailure.InvalidTime;
                return false;
            }
            if (output.Capacity < plan.SampleCount)
            {
                failure = CharacterAnimationBlendSpacePhaseFailure.CapacityExceeded;
                return false;
            }
            if (plan.Policy == CharacterAnimationBlendSpacePhasePolicy.SharedNormalizedPhase)
                return MapNormalized(plan, effectiveTime, cycle, output, out canonicalPhase, out failure);
            return MapMarkers(plan, effectiveTime, cycle, output, out canonicalPhase, out failure);
        }

        static bool MapNormalized(
            CharacterAnimationBlendSpacePhasePlan plan,
            double effectiveTime,
            int cycle,
            CharacterAnimationBlendSpaceTimePage output,
            out CharacterAnimationBlendSpaceCanonicalPhase canonicalPhase,
            out CharacterAnimationBlendSpacePhaseFailure failure)
        {
            CharacterAnimationBlendSpaceSamplePhasePlan clockSample = FirstDynamic(plan);
            if (clockSample == null)
            {
                canonicalPhase = default;
                failure = CharacterAnimationBlendSpacePhaseFailure.InvalidPlan;
                return false;
            }
            float normalized = Repeat01(effectiveTime / clockSample.ClipLength);
            canonicalPhase = new CharacterAnimationBlendSpaceCanonicalPhase(normalized, cycle, string.Empty, string.Empty, normalized, false);
            for (int i = 0; i < plan.SampleCount; i++)
                WriteSampleTime(plan.GetSample(i), normalized, cycle, output);
            failure = CharacterAnimationBlendSpacePhaseFailure.None;
            return true;
        }

        static bool MapMarkers(
            CharacterAnimationBlendSpacePhasePlan plan,
            double effectiveTime,
            int cycle,
            CharacterAnimationBlendSpaceTimePage output,
            out CharacterAnimationBlendSpaceCanonicalPhase canonicalPhase,
            out CharacterAnimationBlendSpacePhaseFailure failure)
        {
            CharacterAnimationBlendSpaceSamplePhasePlan reference = plan.GetSample(plan.ReferenceSampleIndex);
            if (reference.MarkerCount < 2)
            {
                canonicalPhase = default;
                failure = CharacterAnimationBlendSpacePhaseFailure.InvalidPlan;
                return false;
            }
            float normalized = Repeat01(effectiveTime / reference.ClipLength);
            int segmentIndex = FindSegment(reference, normalized);
            CharacterAnimationBlendSpaceMarkerPlan previous = reference.GetMarker(segmentIndex);
            CharacterAnimationBlendSpaceMarkerPlan next = reference.GetMarker((segmentIndex + 1) % reference.MarkerCount);
            int occurrence = GetDirectedOccurrence(reference, segmentIndex, previous.MarkerId, next.MarkerId);
            float fraction = SegmentFraction(previous.NormalizedTime, next.NormalizedTime, normalized);
            canonicalPhase = new CharacterAnimationBlendSpaceCanonicalPhase(normalized, cycle, previous.MarkerId, next.MarkerId, fraction, true);
            for (int i = 0; i < plan.SampleCount; i++)
            {
                CharacterAnimationBlendSpaceSamplePhasePlan sample = plan.GetSample(i);
                if (sample.Role == CharacterAnimationBlendSpaceSampleRole.StationaryPose)
                {
                    WriteSampleTime(sample, sample.StationaryNormalizedTime, cycle, output);
                    continue;
                }
                int targetSegment = FindDirectedSegment(sample, previous.MarkerId, next.MarkerId, occurrence);
                if (targetSegment < 0)
                {
                    output.Reset();
                    canonicalPhase = default;
                    failure = CharacterAnimationBlendSpacePhaseFailure.MissingMarkerSegment;
                    return false;
                }
                CharacterAnimationBlendSpaceMarkerPlan targetPrevious = sample.GetMarker(targetSegment);
                CharacterAnimationBlendSpaceMarkerPlan targetNext = sample.GetMarker((targetSegment + 1) % sample.MarkerCount);
                float targetFraction = fraction;
                if (plan.Policy == CharacterAnimationBlendSpacePhasePolicy.GeneratedFootPhase &&
                    i != plan.ReferenceSampleIndex)
                {
                    if (sample.FootPhaseWarp == null)
                    {
                        output.Reset();
                        canonicalPhase = default;
                        failure = CharacterAnimationBlendSpacePhaseFailure.InvalidPlan;
                        return false;
                    }
                    targetFraction = sample.FootPhaseWarp.RequireSegment(
                        segmentIndex,
                        targetSegment,
                        previous.MarkerId,
                        next.MarkerId).Evaluate(fraction);
                }
                float targetNormalized = LerpSegment(targetPrevious.NormalizedTime, targetNext.NormalizedTime, targetFraction);
                WriteSampleTime(sample, targetNormalized, cycle, output);
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

        static int FindSegment(CharacterAnimationBlendSpaceSamplePhasePlan sample, float normalized)
        {
            for (int i = 0; i < sample.MarkerCount - 1; i++)
            {
                if (normalized >= sample.GetMarker(i).NormalizedTime && normalized < sample.GetMarker(i + 1).NormalizedTime)
                    return i;
            }
            return sample.MarkerCount - 1;
        }

        static int FindDirectedSegment(CharacterAnimationBlendSpaceSamplePhasePlan sample, string previous, string next, int preferredOccurrence)
        {
            int matchCount = 0;
            for (int i = 0; i < sample.MarkerCount; i++)
            {
                CharacterAnimationBlendSpaceMarkerPlan a = sample.GetMarker(i);
                CharacterAnimationBlendSpaceMarkerPlan b = sample.GetMarker((i + 1) % sample.MarkerCount);
                if (!string.Equals(a.MarkerId, previous, StringComparison.Ordinal) || !string.Equals(b.MarkerId, next, StringComparison.Ordinal))
                    continue;
                if (matchCount == preferredOccurrence)
                    return i;
                matchCount++;
            }
            return -1;
        }

        static int GetDirectedOccurrence(
            CharacterAnimationBlendSpaceSamplePhasePlan sample,
            int segmentIndex,
            string previous,
            string next)
        {
            int occurrence = 0;
            for (int i = 0; i < sample.MarkerCount; i++)
            {
                CharacterAnimationBlendSpaceMarkerPlan a = sample.GetMarker(i);
                CharacterAnimationBlendSpaceMarkerPlan b = sample.GetMarker((i + 1) % sample.MarkerCount);
                if (!string.Equals(a.MarkerId, previous, StringComparison.Ordinal) ||
                    !string.Equals(b.MarkerId, next, StringComparison.Ordinal))
                    continue;
                if (i == segmentIndex)
                    return occurrence;
                occurrence++;
            }
            throw new InvalidOperationException("Blend Space reference marker segment is inconsistent.");
        }

        static float SegmentFraction(float start, float end, float value)
        {
            if (end > start)
                return Clamp01((value - start) / (end - start));
            float length = 1f - start + end;
            float offset = value >= start ? value - start : 1f - start + value;
            return Clamp01(offset / length);
        }

        static float LerpSegment(float start, float end, float fraction)
        {
            float value = end > start ? start + (end - start) * fraction : start + (1f - start + end) * fraction;
            return value >= 1f ? value - 1f : value;
        }

        static void WriteSampleTime(CharacterAnimationBlendSpaceSamplePhasePlan sample, float normalized, int cycle, CharacterAnimationBlendSpaceTimePage output)
        {
            float value = sample.Role == CharacterAnimationBlendSpaceSampleRole.StationaryPose ? sample.StationaryNormalizedTime : normalized;
            output.Add(new CharacterAnimationBlendSpaceSampleTime(sample.SampleId, value * sample.ClipLength, value, cycle));
        }

        static float Repeat01(double value)
        {
            double result = value - Math.Floor(value);
            return (float)result;
        }

        static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }
}
