using System;
using BTSMTL.Timeline;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    internal sealed class MarkerSegmentRelationCursor
    {
        internal bool Initialized;
        internal long LeaderOrdinal;
        internal long FollowerOrdinal;
    }

    internal readonly struct MarkerMappedTime
    {
        internal MarkerMappedTime(
            double continuousTime,
            string previousMarkerId,
            string nextMarkerId,
            float segmentFraction)
        {
            ContinuousTime = continuousTime;
            PreviousMarkerId = previousMarkerId ?? string.Empty;
            NextMarkerId = nextMarkerId ?? string.Empty;
            SegmentFraction = segmentFraction;
        }

        internal double ContinuousTime { get; }
        internal string PreviousMarkerId { get; }
        internal string NextMarkerId { get; }
        internal float SegmentFraction { get; }
    }

    internal static class MarkerSegmentTimeMapper
    {
        internal static double Map(
            AnimationMarkerSyncBinding leaderBinding,
            double leaderContinuousTime,
            AnimationMarkerSyncBinding followerBinding,
            double followerContinuousTime,
            MarkerSegmentRelationCursor cursor) =>
            MapDetailed(
                leaderBinding,
                leaderContinuousTime,
                followerBinding,
                followerContinuousTime,
                cursor).ContinuousTime;

        internal static MarkerMappedTime MapDetailed(
            AnimationMarkerSyncBinding leaderBinding,
            double leaderContinuousTime,
            AnimationMarkerSyncBinding followerBinding,
            double followerContinuousTime,
            MarkerSegmentRelationCursor cursor)
        {
            if (leaderBinding == null ||
                followerBinding == null ||
                cursor == null ||
                !leaderBinding.IsMarkerGroup ||
                !followerBinding.IsMarkerGroup ||
                !double.IsFinite(leaderContinuousTime) ||
                leaderContinuousTime < 0d ||
                !double.IsFinite(followerContinuousTime) ||
                followerContinuousTime < 0d ||
                !TryLocateSegment(
                    leaderBinding,
                    leaderContinuousTime,
                    out SegmentPosition leader))
            {
                throw new InvalidOperationException(
                    "Marker segment mapping input is invalid.");
            }

            if (!cursor.Initialized)
            {
                cursor.FollowerOrdinal = SelectInitialFollowerOrdinal(
                    followerBinding,
                    leader.Segment.PreviousMarkerId,
                    leader.Segment.NextMarkerId,
                    leader.Fraction,
                    followerContinuousTime);
                cursor.Initialized = true;
            }
            else if (leader.Ordinal != cursor.LeaderOrdinal)
            {
                if (leader.Ordinal < cursor.LeaderOrdinal)
                {
                    throw new InvalidOperationException(
                        "Marker leader time regressed.");
                }
                for (long ordinal = cursor.LeaderOrdinal + 1;
                     ordinal <= leader.Ordinal;
                     ordinal++)
                {
                    AnimationMarkerSyncSegmentOccurrence segment =
                        SegmentAtOrdinal(
                            leaderBinding,
                            ordinal,
                            out _);
                    cursor.FollowerOrdinal = AdvanceFollowerOrdinal(
                        followerBinding,
                        cursor.FollowerOrdinal,
                        segment.PreviousMarkerId,
                        segment.NextMarkerId);
                }
            }

            cursor.LeaderOrdinal = leader.Ordinal;
            AnimationMarkerSyncSegmentOccurrence follower =
                SegmentAtOrdinal(
                    followerBinding,
                    cursor.FollowerOrdinal,
                    out long followerCycle);
            double mapped =
                followerCycle * followerBinding.DurationSeconds +
                follower.StartTimeSeconds +
                leader.Fraction * follower.DurationSeconds;
            if (!double.IsFinite(mapped) || mapped < 0d)
            {
                throw new InvalidOperationException(
                    "Marker segment mapping produced an invalid time.");
            }
            return new MarkerMappedTime(
                mapped,
                leader.Segment.PreviousMarkerId,
                leader.Segment.NextMarkerId,
                leader.Fraction);
        }

        static bool TryLocateSegment(
            AnimationMarkerSyncBinding binding,
            double continuousTime,
            out SegmentPosition position)
        {
            position = default;
            if (binding == null ||
                !binding.IsMarkerGroup ||
                binding.Segments.Count == 0)
            {
                return false;
            }
            if (binding.SequenceTopology ==
                AnimationMarkerSequenceTopology.Finite)
            {
                if (continuousTime < 0d ||
                    continuousTime > binding.DurationSeconds)
                {
                    return false;
                }
                for (int i = 0; i < binding.Segments.Count; i++)
                {
                    AnimationMarkerSyncSegmentOccurrence segment =
                        binding.Segments[i];
                    if (continuousTime < segment.StartTimeSeconds ||
                        continuousTime > segment.EndTimeSeconds ||
                        i < binding.Segments.Count - 1 &&
                        continuousTime == segment.EndTimeSeconds)
                    {
                        continue;
                    }
                    position = new SegmentPosition(
                        segment,
                        i,
                        Fraction(
                            continuousTime,
                            segment.StartTimeSeconds,
                            segment.EndTimeSeconds));
                    return true;
                }
                return false;
            }

            double duration = binding.DurationSeconds;
            if (duration <= 0d || continuousTime < 0d)
                return false;
            long cycle = (long)Math.Floor(
                continuousTime / duration);
            double local =
                continuousTime - cycle * duration;
            int lastIndex = binding.Segments.Count - 1;
            AnimationMarkerSyncSegmentOccurrence wrap =
                binding.Segments[lastIndex];
            float firstMarkerTime =
                binding.Markers[0].TimeSeconds;
            if (local < firstMarkerTime)
            {
                double start =
                    (cycle - 1) * duration +
                    wrap.StartTimeSeconds;
                double end =
                    (cycle - 1) * duration +
                    wrap.EndTimeSeconds;
                position = new SegmentPosition(
                    wrap,
                    (cycle - 1) * binding.Segments.Count +
                    lastIndex,
                    Fraction(continuousTime, start, end));
                return true;
            }
            for (int i = 0; i < lastIndex; i++)
            {
                AnimationMarkerSyncSegmentOccurrence segment =
                    binding.Segments[i];
                if (local < segment.StartTimeSeconds ||
                    local >= segment.EndTimeSeconds)
                {
                    continue;
                }
                position = new SegmentPosition(
                    segment,
                    cycle * binding.Segments.Count + i,
                    Fraction(
                        local,
                        segment.StartTimeSeconds,
                        segment.EndTimeSeconds));
                return true;
            }
            double wrapStart =
                cycle * duration + wrap.StartTimeSeconds;
            double wrapEnd =
                cycle * duration + wrap.EndTimeSeconds;
            position = new SegmentPosition(
                wrap,
                cycle * binding.Segments.Count + lastIndex,
                Fraction(
                    continuousTime,
                    wrapStart,
                    wrapEnd));
            return true;
        }

        static long SelectInitialFollowerOrdinal(
            AnimationMarkerSyncBinding binding,
            string previousMarkerId,
            string nextMarkerId,
            float fraction,
            double rawTime)
        {
            if (!binding.TryGetOccurrences(
                    previousMarkerId,
                    nextMarkerId,
                    out AnimationMarkerSyncSegmentOccurrence[]
                        occurrences) ||
                occurrences.Length == 0)
            {
                throw new InvalidOperationException(
                    "Marker follower has no matching segment.");
            }

            long bestOrdinal = -1;
            double bestDistance = double.MaxValue;
            AnimationMarkerSyncSegmentOccurrence best = null;
            for (int i = 0; i < occurrences.Length; i++)
            {
                AnimationMarkerSyncSegmentOccurrence occurrence =
                    occurrences[i];
                if (binding.SequenceTopology ==
                    AnimationMarkerSequenceTopology.Finite)
                {
                    double candidate =
                        occurrence.StartTimeSeconds +
                        fraction * occurrence.DurationSeconds;
                    SelectCandidate(
                        binding,
                        occurrence,
                        occurrence.OccurrenceIndex,
                        candidate,
                        rawTime,
                        ref bestOrdinal,
                        ref bestDistance,
                        ref best);
                    continue;
                }
                double baseTime =
                    occurrence.StartTimeSeconds +
                    fraction * occurrence.DurationSeconds;
                long center = (long)Math.Round(
                    (rawTime - baseTime) /
                    binding.DurationSeconds,
                    MidpointRounding.AwayFromZero);
                for (long cycle = center - 1;
                     cycle <= center + 1;
                     cycle++)
                {
                    double candidate =
                        cycle * binding.DurationSeconds +
                        baseTime;
                    if (candidate < 0d)
                        continue;
                    long ordinal =
                        cycle * binding.Segments.Count +
                        occurrence.OccurrenceIndex;
                    SelectCandidate(
                        binding,
                        occurrence,
                        ordinal,
                        candidate,
                        rawTime,
                        ref bestOrdinal,
                        ref bestDistance,
                        ref best);
                }
            }
            if (bestOrdinal < 0)
            {
                throw new InvalidOperationException(
                    "Marker follower has no reachable matching segment.");
            }
            return bestOrdinal;
        }

        static void SelectCandidate(
            AnimationMarkerSyncBinding binding,
            AnimationMarkerSyncSegmentOccurrence occurrence,
            long ordinal,
            double candidate,
            double rawTime,
            ref long bestOrdinal,
            ref double bestDistance,
            ref AnimationMarkerSyncSegmentOccurrence best)
        {
            double distance = Math.Abs(candidate - rawTime);
            bool replace =
                distance < bestDistance - 0.0000001d;
            if (!replace &&
                Math.Abs(distance - bestDistance) <=
                0.0000001d &&
                best != null)
            {
                AnimationMarkerSyncMarkerBinding candidateMarker =
                    binding.Markers[
                        occurrence.PreviousMarkerIndex];
                AnimationMarkerSyncMarkerBinding bestMarker =
                    binding.Markers[
                        best.PreviousMarkerIndex];
                replace =
                    candidateMarker.Frame < bestMarker.Frame ||
                    candidateMarker.Frame == bestMarker.Frame &&
                    string.CompareOrdinal(
                        candidateMarker.AuthoringId,
                        bestMarker.AuthoringId) < 0;
            }
            if (!replace && best != null)
                return;
            bestOrdinal = ordinal;
            bestDistance = distance;
            best = occurrence;
        }

        static long AdvanceFollowerOrdinal(
            AnimationMarkerSyncBinding binding,
            long currentOrdinal,
            string previousMarkerId,
            string nextMarkerId)
        {
            int segmentCount = binding.Segments.Count;
            int attempts =
                binding.SequenceTopology ==
                AnimationMarkerSequenceTopology.Cyclic
                    ? segmentCount
                    : segmentCount -
                      (int)currentOrdinal - 1;
            for (int offset = 1;
                 offset <= attempts;
                 offset++)
            {
                long ordinal = currentOrdinal + offset;
                AnimationMarkerSyncSegmentOccurrence segment =
                    SegmentAtOrdinal(
                        binding,
                        ordinal,
                        out _);
                if (string.Equals(
                        segment.PreviousMarkerId,
                        previousMarkerId,
                        StringComparison.Ordinal) &&
                    string.Equals(
                        segment.NextMarkerId,
                        nextMarkerId,
                        StringComparison.Ordinal))
                {
                    return ordinal;
                }
            }
            throw new InvalidOperationException(
                "Marker finite follower coverage was exceeded.");
        }

        static AnimationMarkerSyncSegmentOccurrence SegmentAtOrdinal(
            AnimationMarkerSyncBinding binding,
            long ordinal,
            out long cycle)
        {
            int count = binding.Segments.Count;
            if (binding.SequenceTopology ==
                AnimationMarkerSequenceTopology.Finite)
            {
                if (ordinal < 0 || ordinal >= count)
                {
                    throw new InvalidOperationException(
                        "Marker finite segment ordinal is out of range.");
                }
                cycle = 0;
                return binding.Segments[(int)ordinal];
            }
            cycle = FloorDiv(ordinal, count);
            int index =
                (int)(ordinal - cycle * count);
            return binding.Segments[index];
        }

        static long FloorDiv(long value, int divisor)
        {
            long result = value / divisor;
            if (value < 0 && value % divisor != 0)
                result--;
            return result;
        }

        static float Fraction(
            double value,
            double start,
            double end)
        {
            double duration = end - start;
            if (duration <= 0d)
            {
                throw new InvalidOperationException(
                    "Marker segment duration is invalid.");
            }
            return (float)Math.Clamp(
                (value - start) / duration,
                0d,
                1d);
        }

        readonly struct SegmentPosition
        {
            internal SegmentPosition(
                AnimationMarkerSyncSegmentOccurrence segment,
                long ordinal,
                float fraction)
            {
                Segment = segment;
                Ordinal = ordinal;
                Fraction = fraction;
            }

            internal AnimationMarkerSyncSegmentOccurrence Segment { get; }
            internal long Ordinal { get; }
            internal float Fraction { get; }
        }
    }
}
