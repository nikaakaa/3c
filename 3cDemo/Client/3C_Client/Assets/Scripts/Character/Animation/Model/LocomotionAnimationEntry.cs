using System;
using Animancer;
using UnityEngine;

namespace ThirdPersonAnimation
{
    [Serializable]
    public struct LocomotionAnimationEntry
    {
        [SerializeField] string key;
        [SerializeField] float fadeDuration;
        [SerializeField] float speed;
        [SerializeField] float normalizedStartTime;
        [SerializeField] LocomotionAnimationExitDurationMode exitDurationMode;
        [SerializeField] float exitDurationOverride;

        public LocomotionAnimationEntry(
            string key,
            float fadeDuration = -1f,
            float speed = 1f,
            float normalizedStartTime = -1f,
            LocomotionAnimationExitDurationMode exitDurationMode = LocomotionAnimationExitDurationMode.FallbackToMovementConfig,
            float exitDurationOverride = -1f)
        {
            this.key = key;
            this.fadeDuration = fadeDuration;
            this.speed = speed;
            this.normalizedStartTime = normalizedStartTime;
            this.exitDurationMode = exitDurationMode;
            this.exitDurationOverride = exitDurationOverride;
        }

        public string Key => key;
        public StringReference KeyReference => StringReference.Get(key);
        public float FadeDuration => fadeDuration < 0f ? -1f : fadeDuration;
        public float Speed => speed <= 0f ? 1f : speed;
        public float NormalizedStartTime => normalizedStartTime < 0f ? -1f : Mathf.Clamp01(normalizedStartTime);
        public LocomotionAnimationExitDurationMode ExitDurationMode => exitDurationMode;
        public float ExitDurationOverride => exitDurationOverride < 0f ? -1f : exitDurationOverride;
        public bool HasValidSpeed => speed > 0f;

        public bool HasManualExitDuration => exitDurationMode == LocomotionAnimationExitDurationMode.Manual && exitDurationOverride >= 0f;

        public LocomotionAnimationTiming ResolveTiming(float fallbackExitDuration)
        {
            if (HasManualExitDuration)
                return new LocomotionAnimationTiming(exitDurationOverride);

            return new LocomotionAnimationTiming(fallbackExitDuration);
        }
    }
}
