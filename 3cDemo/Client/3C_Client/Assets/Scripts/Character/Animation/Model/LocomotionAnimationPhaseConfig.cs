using System;
using UnityEngine;

namespace ThirdPersonAnimation
{
    [Serializable]
    public struct LocomotionAnimationPhaseConfig
    {
        [SerializeField] string aliasKey;
        [SerializeField] LocomotionAnimationExitPolicy exitPolicy;
        [SerializeField] float exitDuration;

        public LocomotionAnimationPhaseConfig(string aliasKey, LocomotionAnimationExitPolicy exitPolicy, float exitDuration)
        {
            this.aliasKey = aliasKey;
            this.exitPolicy = exitPolicy;
            this.exitDuration = exitDuration;
        }

        public string AliasKey => aliasKey;
        public LocomotionAnimationExitPolicy ExitPolicy => exitPolicy;
        public float ExitDuration => exitDuration;

        public static LocomotionAnimationPhaseConfig Manual(string aliasKey)
        {
            return new LocomotionAnimationPhaseConfig(aliasKey, LocomotionAnimationExitPolicy.Manual, 0f);
        }

        public static LocomotionAnimationPhaseConfig AfterDuration(string aliasKey, float exitDuration)
        {
            return new LocomotionAnimationPhaseConfig(aliasKey, LocomotionAnimationExitPolicy.AfterDuration, exitDuration);
        }

        public static LocomotionAnimationPhaseConfig OnAnimationEnd(string aliasKey)
        {
            return new LocomotionAnimationPhaseConfig(aliasKey, LocomotionAnimationExitPolicy.OnAnimationEnd, 0f);
        }
    }
}
