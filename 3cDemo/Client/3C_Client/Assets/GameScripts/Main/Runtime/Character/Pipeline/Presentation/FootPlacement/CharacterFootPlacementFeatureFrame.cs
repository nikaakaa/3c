using System;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{

    internal readonly struct CharacterFootPlacementFeatureFrame
    {
        public CharacterFootPlacementFeatureFrame(
            float value,
            AnimationFootFeatureSample left,
            AnimationFootFeatureSample right)
        {
            if (!left.IsValid || !right.IsValid)
                throw new ArgumentException("Foot Placement feature frame requires both feet.");
            Value = Mathf.Clamp01(value);
            Left = left;
            Right = right;
        }

        public float Value { get; }
        public AnimationFootFeatureSample Left { get; }
        public AnimationFootFeatureSample Right { get; }
    }
}









