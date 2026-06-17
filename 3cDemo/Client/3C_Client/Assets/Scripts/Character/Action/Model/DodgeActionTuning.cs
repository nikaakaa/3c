using System;
using UnityEngine;

namespace ThirdPersonAction
{
    [Serializable]
    public readonly struct DodgeActionTuning
    {
        public DodgeActionTuning(float duration, float distance, int priority, int resistance, bool rotateToDirection)
            : this(duration, distance, duration, distance, priority, resistance, rotateToDirection, false)
        {
        }

        public DodgeActionTuning(
            float directionalDuration,
            float directionalDistance,
            float backstepDuration,
            float backstepDistance,
            int priority,
            int resistance,
            bool directionalRotateToDirection,
            bool backstepRotateToDirection)
        {
            DirectionalDuration = Mathf.Max(0f, directionalDuration);
            DirectionalDistance = Mathf.Max(0f, directionalDistance);
            BackstepDuration = Mathf.Max(0f, backstepDuration);
            BackstepDistance = Mathf.Max(0f, backstepDistance);
            Priority = Mathf.Max(0, priority);
            Resistance = Mathf.Max(0, resistance);
            DirectionalRotateToDirection = directionalRotateToDirection;
            BackstepRotateToDirection = backstepRotateToDirection;
        }

        public float DirectionalDuration { get; }
        public float DirectionalDistance { get; }
        public float BackstepDuration { get; }
        public float BackstepDistance { get; }
        public int Priority { get; }
        public int Resistance { get; }
        public bool DirectionalRotateToDirection { get; }
        public bool BackstepRotateToDirection { get; }
        public float Duration => DirectionalDuration;
        public float Distance => DirectionalDistance;
        public bool RotateToDirection => DirectionalRotateToDirection;

        public float ResolveDuration(DodgeActionVariant variant)
        {
            return variant == DodgeActionVariant.Backstep ? BackstepDuration : DirectionalDuration;
        }

        public float ResolveDistance(DodgeActionVariant variant)
        {
            return variant == DodgeActionVariant.Backstep ? BackstepDistance : DirectionalDistance;
        }

        public bool ShouldRotateToDirection(DodgeActionVariant variant)
        {
            return variant == DodgeActionVariant.Backstep ? BackstepRotateToDirection : DirectionalRotateToDirection;
        }
    }
}
