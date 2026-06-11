using System;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAnimation
{
    [Serializable]
    public struct LocomotionPhaseMotionProfileBinding
    {
        [SerializeField] BasicMovementPhase phase;
        [SerializeField] BasicMovementGait gait;
        [SerializeField] string aliasKey;
        [SerializeField] LocomotionMotionProfileSO profile;
        [SerializeField] LocomotionMotionProfileMode motionMode;

        public LocomotionPhaseMotionProfileBinding(BasicMovementPhase phase, string aliasKey, LocomotionMotionProfileSO profile)
            : this(phase, BasicMovementGait.Run, aliasKey, profile, LocomotionMotionProfileMode.AdditiveBakedMotion)
        {
        }

        public LocomotionPhaseMotionProfileBinding(
            BasicMovementPhase phase,
            string aliasKey,
            LocomotionMotionProfileSO profile,
            LocomotionMotionProfileMode motionMode)
            : this(phase, BasicMovementGait.Run, aliasKey, profile, motionMode)
        {
        }

        public LocomotionPhaseMotionProfileBinding(
            BasicMovementPhase phase,
            BasicMovementGait gait,
            string aliasKey,
            LocomotionMotionProfileSO profile)
            : this(phase, gait, aliasKey, profile, LocomotionMotionProfileMode.AdditiveBakedMotion)
        {
        }

        public LocomotionPhaseMotionProfileBinding(
            BasicMovementPhase phase,
            BasicMovementGait gait,
            string aliasKey,
            LocomotionMotionProfileSO profile,
            LocomotionMotionProfileMode motionMode)
        {
            this.phase = phase;
            this.gait = gait;
            this.aliasKey = aliasKey ?? string.Empty;
            this.profile = profile;
            this.motionMode = motionMode;
        }

        public BasicMovementPhase Phase => phase;
        public BasicMovementGait Gait => gait;
        public string AliasKey => aliasKey;
        public LocomotionMotionProfileSO Profile => profile;
        public LocomotionMotionProfileMode MotionMode => motionMode;
        public bool IsEnabled => motionMode == LocomotionMotionProfileMode.AdditiveBakedMotion;

        public bool Matches(BasicMovementPhase phase, string aliasKey)
        {
            return Matches(phase, BasicMovementGait.Run, aliasKey);
        }

        public bool Matches(BasicMovementPhase phase, BasicMovementGait gait, string aliasKey)
        {
            return this.phase == phase &&
                   this.gait == gait &&
                   string.Equals(this.aliasKey, aliasKey ?? string.Empty, StringComparison.Ordinal);
        }
    }
}
