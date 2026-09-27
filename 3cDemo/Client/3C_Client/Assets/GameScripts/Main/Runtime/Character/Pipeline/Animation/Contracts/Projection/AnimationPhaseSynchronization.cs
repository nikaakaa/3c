using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public struct AnimationPhaseSynchronizationState
    {
        internal double CycleOffset;
        internal double LeaderPhaseFloor;
        internal AnimationPhaseCoverage NaturalSupportCoverage;
    }

    public static class AnimationPhaseSynchronization
    {
        public static double Map(
            AnimationClipPhasePlan leader,
            double leaderTime,
            AnimationClipPhasePlan follower,
            double followerContinuation,
            AnimationPhaseCoverage allowedEntry,
            double remainingBlendSeconds,
            double followerPlayRate,
            bool entering,
            ref AnimationPhaseSynchronizationState state)
        {
            if (leader == null)
                throw new ArgumentNullException(nameof(leader));
            if (follower == null)
                throw new ArgumentNullException(nameof(follower));
            if (!double.IsFinite(followerContinuation) || followerContinuation < 0d ||
                !double.IsFinite(remainingBlendSeconds) || remainingBlendSeconds < 0d ||
                !double.IsFinite(followerPlayRate) || followerPlayRate <= 0d)
                throw new ArgumentOutOfRangeException();

            double leaderPhase = leader.Forward(leaderTime);
            if (entering)
            {
                state = default;
                state.LeaderPhaseFloor = leader.IsDoubleSupported(leaderTime)
                    ? Math.Ceiling(leaderPhase * 2d) * 0.5d
                    : leaderPhase;
            }
            leaderPhase = Math.Max(leaderPhase, state.LeaderPhaseFloor);
            if (!entering)
            {
                if (state.NaturalSupportCoverage.IsValid)
                {
                    if (!state.NaturalSupportCoverage.Contains(followerContinuation))
                        throw new InvalidOperationException("Finite Animation Phase exhausted its selected double-support blend interval.");
                    return followerContinuation;
                }
                for (int i = 0; i < follower.DoubleSupportIntervals.Count; i++)
                {
                    AnimationPhaseCoverage support = follower.DoubleSupportIntervals[i];
                    if (!support.Contains(followerContinuation) ||
                        followerContinuation + remainingBlendSeconds * followerPlayRate > support.EndSeconds)
                        continue;
                    state.NaturalSupportCoverage = support;
                    return followerContinuation;
                }
                return follower.Inverse(leaderPhase + state.CycleOffset, followerContinuation);
            }

            if (follower.Loop)
            {
                double desiredOffset = follower.Forward(followerContinuation) - leaderPhase;
                double lowerTime = follower.Inverse(leaderPhase + Math.Floor(desiredOffset), followerContinuation);
                double upperTime = follower.Inverse(leaderPhase + Math.Ceiling(desiredOffset), followerContinuation);
                double time = Math.Abs(lowerTime - followerContinuation) <= Math.Abs(upperTime - followerContinuation)
                    ? lowerTime : upperTime;
                state.CycleOffset = Math.Round(follower.Forward(time) - leaderPhase);
                return time;
            }

            if (!allowedEntry.IsValid ||
                !follower.CurveCoverage.Contains(allowedEntry.StartSeconds) ||
                !follower.CurveCoverage.Contains(allowedEntry.EndSeconds))
                throw new ArgumentException("Finite Animation Phase entry coverage is invalid.", nameof(allowedEntry));

            double result = double.PositiveInfinity;
            double minimumOffset = Math.Ceiling(follower.Forward(allowedEntry.StartSeconds) - leaderPhase);
            double maximumOffset = Math.Floor(follower.Forward(allowedEntry.EndSeconds) - leaderPhase);
            if (minimumOffset <= maximumOffset)
            {
                double continuation = Math.Max(allowedEntry.StartSeconds,
                    Math.Min(allowedEntry.EndSeconds, followerContinuation));
                double desiredOffset = follower.Forward(continuation) - leaderPhase;
                double lowerOffset = Math.Max(minimumOffset, Math.Min(maximumOffset, Math.Floor(desiredOffset)));
                double upperOffset = Math.Max(minimumOffset, Math.Min(maximumOffset, Math.Ceiling(desiredOffset)));
                double lowerTime = follower.Inverse(leaderPhase + lowerOffset, followerContinuation);
                double upperTime = follower.Inverse(leaderPhase + upperOffset, followerContinuation);
                bool useLower = Math.Abs(lowerTime - followerContinuation) <= Math.Abs(upperTime - followerContinuation);
                result = useLower ? lowerTime : upperTime;
                state.CycleOffset = useLower ? lowerOffset : upperOffset;
            }

            for (int i = 0; i < follower.DoubleSupportIntervals.Count; i++)
            {
                AnimationPhaseCoverage support = follower.DoubleSupportIntervals[i];
                double start = Math.Max(allowedEntry.StartSeconds, support.StartSeconds);
                double end = Math.Min(allowedEntry.EndSeconds,
                    support.EndSeconds - remainingBlendSeconds * followerPlayRate);
                if (start > end)
                    continue;
                double candidate = Math.Max(start, Math.Min(end, followerContinuation));
                if (Math.Abs(candidate - followerContinuation) > Math.Abs(result - followerContinuation))
                    continue;
                result = candidate;
                state.NaturalSupportCoverage = support;
            }
            if (!double.IsFinite(result) || !allowedEntry.Contains(result))
                throw new InvalidOperationException("Finite Animation Phase has no matching phase or complete double-support blend interval inside entry coverage.");
            return result;
        }
    }
}
