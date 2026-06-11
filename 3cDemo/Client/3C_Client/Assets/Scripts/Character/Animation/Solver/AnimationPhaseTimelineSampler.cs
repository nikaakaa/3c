using System;
using ThirdPersonMovement;

namespace ThirdPersonAnimation
{
    public static class AnimationPhaseTimelineSampler
    {
        public static AnimationPhaseTimelineFacts Sample(
            BasicMovementPhase phase,
            in LocomotionAnimationPhaseConfig phaseConfig,
            float phaseTime,
            in AnimationPhasePlaybackProgress playbackProgress)
        {
            return phaseConfig.ExitPolicy switch
            {
                LocomotionAnimationExitPolicy.AfterDuration => new AnimationPhaseTimelineFacts(
                    phaseConfig.ExitDuration >= 0f &&
                    BasicMovementPhaseTiming.AfterDuration(phaseConfig.ExitDuration).IsExitTimeReached(phaseTime)),
                LocomotionAnimationExitPolicy.OnAnimationEnd => new AnimationPhaseTimelineFacts(
                    MatchesPlayback(phase, in phaseConfig, in playbackProgress) && playbackProgress.IsEnded),
                _ => AnimationPhaseTimelineFacts.None
            };
        }

        static bool MatchesPlayback(
            BasicMovementPhase phase,
            in LocomotionAnimationPhaseConfig phaseConfig,
            in AnimationPhasePlaybackProgress playbackProgress)
        {
            return playbackProgress.HasValidPlayback &&
                   playbackProgress.Phase == phase &&
                   string.Equals(playbackProgress.AliasKey, phaseConfig.AliasKey, StringComparison.Ordinal);
        }
    }
}
