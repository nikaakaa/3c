using System;
using UnityEngine;

namespace ThirdPersonAnimation
{
    [Serializable]
    public struct LocomotionFootPhaseMarker
    {
        [SerializeField] LocomotionFootPhase phase;
        [SerializeField] float normalizedTime;

        public LocomotionFootPhaseMarker(LocomotionFootPhase phase, float normalizedTime)
        {
            this.phase = phase;
            this.normalizedTime = normalizedTime;
        }

        public LocomotionFootPhase Phase => phase;
        public float NormalizedTime => normalizedTime;
        public bool HasKnownPhase => phase != LocomotionFootPhase.Unknown;
        public bool HasValidTime => normalizedTime >= 0f && normalizedTime <= 1f;
    }
}

