using System;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{

    internal readonly struct CharacterFootPlacementFeatureFrame
    {
        public CharacterFootPlacementFeatureFrame(
            float value,
            float visibleWeight,
            AnimationFootFeatureSample left,
            AnimationFootFeatureSample right)
        {
            if (!left.IsValid || !right.IsValid)
                throw new ArgumentException("Foot Placement feature frame requires both feet.");
            Value = Mathf.Clamp01(value);
            VisibleWeight = Mathf.Clamp01(visibleWeight);
            Left = left;
            Right = right;
        }

        public float Value { get; }
        public float VisibleWeight { get; }
        public AnimationFootFeatureSample Left { get; }
        public AnimationFootFeatureSample Right { get; }
    }
}
