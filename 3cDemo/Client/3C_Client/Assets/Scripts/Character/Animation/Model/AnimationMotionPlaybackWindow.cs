using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAnimation
{
    public readonly struct AnimationMotionPlaybackWindow
    {
        public AnimationMotionPlaybackWindow(
            BasicMovementPhase phase,
            string aliasKey,
            float previousNormalizedTime,
            float currentNormalizedTime,
            bool hasValidPlayback)
            : this(phase, BasicMovementGait.Run, aliasKey, previousNormalizedTime, currentNormalizedTime, hasValidPlayback)
        {
        }

        public AnimationMotionPlaybackWindow(
            BasicMovementPhase phase,
            BasicMovementGait gait,
            string aliasKey,
            float previousNormalizedTime,
            float currentNormalizedTime,
            bool hasValidPlayback)
        {
            Phase = phase;
            Gait = gait;
            AliasKey = aliasKey ?? string.Empty;
            PreviousNormalizedTime = Mathf.Max(0f, previousNormalizedTime);
            CurrentNormalizedTime = Mathf.Max(0f, currentNormalizedTime);
            HasValidPlayback = hasValidPlayback;
        }

        public BasicMovementPhase Phase { get; }
        public BasicMovementGait Gait { get; }
        public string AliasKey { get; }
        public float PreviousNormalizedTime { get; }
        public float CurrentNormalizedTime { get; }
        public bool HasValidPlayback { get; }

        public static AnimationMotionPlaybackWindow Invalid(BasicMovementPhase phase)
        {
            return Invalid(phase, BasicMovementGait.Run);
        }

        public static AnimationMotionPlaybackWindow Invalid(BasicMovementPhase phase, BasicMovementGait gait)
        {
            return new AnimationMotionPlaybackWindow(phase, gait, string.Empty, 0f, 0f, false);
        }
    }
}
