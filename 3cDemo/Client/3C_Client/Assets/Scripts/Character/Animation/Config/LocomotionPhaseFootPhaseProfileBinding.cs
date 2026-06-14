using System;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAnimation
{
    [Serializable]
    public struct LocomotionPhaseFootPhaseProfileBinding
    {
        [SerializeField] BasicMovementPhase phase;
        [SerializeField] BasicMovementGait gait;
        [SerializeField] string aliasKey;
        [SerializeField] LocomotionFootPhaseProfileSO profile;
        [SerializeField] bool enabled;

        public LocomotionPhaseFootPhaseProfileBinding(
            BasicMovementPhase phase,
            BasicMovementGait gait,
            string aliasKey,
            LocomotionFootPhaseProfileSO profile,
            bool enabled = true)
        {
            this.phase = phase;
            this.gait = gait;
            this.aliasKey = aliasKey ?? string.Empty;
            this.profile = profile;
            this.enabled = enabled;
        }

        public BasicMovementPhase Phase => phase;
        public BasicMovementGait Gait => gait;
        public string AliasKey => aliasKey ?? string.Empty;
        public LocomotionFootPhaseProfileSO Profile => profile;
        public bool IsEnabled => enabled;

        public bool Matches(BasicMovementPhase phase, BasicMovementGait gait, string aliasKey)
        {
            return enabled &&
                   this.phase == phase &&
                   this.gait == gait &&
                   string.Equals(this.aliasKey, aliasKey ?? string.Empty, StringComparison.Ordinal);
        }
    }
}

