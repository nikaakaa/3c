using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public static class AnimationPhaseSynchronization
    {
        public static double Map(
            AnimationClipPhasePlan leader,
            double leaderTime,
            AnimationClipPhasePlan follower,
            double followerContinuation,
            AnimationPhaseCoverage allowedEntry,
            bool entering,
            ref double cycleOffset)
        {
            if (leader == null)
                throw new ArgumentNullException(nameof(leader));
            if (follower == null)
                throw new ArgumentNullException(nameof(follower));
            if (!double.IsFinite(followerContinuation) || followerContinuation < 0d)
                throw new ArgumentOutOfRangeException(nameof(followerContinuation));
            if (!entering && (!double.IsFinite(cycleOffset) || cycleOffset != Math.Truncate(cycleOffset)))
                throw new ArgumentOutOfRangeException(nameof(cycleOffset));

            double leaderPhase = leader.Forward(leaderTime);
            if (follower.Loop)
            {
                if (!entering)
                    return follower.Inverse(leaderPhase + cycleOffset, followerContinuation);
                double desiredOffset = follower.Forward(followerContinuation) - leaderPhase;
                double lowerTime = follower.Inverse(leaderPhase + Math.Floor(desiredOffset), followerContinuation);
                double upperTime = follower.Inverse(leaderPhase + Math.Ceiling(desiredOffset), followerContinuation);
                double time = Math.Abs(lowerTime - followerContinuation) <= Math.Abs(upperTime - followerContinuation)
                    ? lowerTime : upperTime;
                cycleOffset = Math.Round(follower.Forward(time) - leaderPhase);
                return time;
            }

            if (entering)
            {
                if (!allowedEntry.IsValid ||
                    !follower.CurveCoverage.Contains(allowedEntry.StartSeconds) ||
                    !follower.CurveCoverage.Contains(allowedEntry.EndSeconds))
                {
                    throw new ArgumentException("Finite Animation Phase entry coverage is invalid.", nameof(allowedEntry));
                }

                double minimumOffset = Math.Ceiling(follower.Forward(allowedEntry.StartSeconds) - leaderPhase);
                double maximumOffset = Math.Floor(follower.Forward(allowedEntry.EndSeconds) - leaderPhase);
                if (minimumOffset > maximumOffset)
                    throw new InvalidOperationException("Finite Animation Phase has no matching phase inside entry coverage.");

                double continuation = Math.Max(allowedEntry.StartSeconds,
                    Math.Min(allowedEntry.EndSeconds, followerContinuation));
                double desiredOffset = follower.Forward(continuation) - leaderPhase;
                double lowerOffset = Math.Max(minimumOffset, Math.Min(maximumOffset, Math.Floor(desiredOffset)));
                double upperOffset = Math.Max(minimumOffset, Math.Min(maximumOffset, Math.Ceiling(desiredOffset)));
                double lowerTime = follower.Inverse(leaderPhase + lowerOffset, followerContinuation);
                double upperTime = follower.Inverse(leaderPhase + upperOffset, followerContinuation);
                bool useLower = Math.Abs(lowerTime - followerContinuation) <= Math.Abs(upperTime - followerContinuation);
                double result = useLower ? lowerTime : upperTime;
                if (!allowedEntry.Contains(result))
                    throw new InvalidOperationException("Finite Animation Phase mapped time is outside entry coverage.");

                cycleOffset = useLower ? lowerOffset : upperOffset;
                return result;
            }

            return follower.Inverse(leaderPhase + cycleOffset, followerContinuation);
        }
    }
}
