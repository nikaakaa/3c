using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct AnimationFootPlacementHistorySample
    {
        public AnimationFootPlacementHistorySample(in AnimationFootPlacementSample source)
        {
            if (!source.IsValid)
                throw new ArgumentException("Animation Foot Placement history source is invalid.");
            Weight = source.Weight;
            ref readonly AnimationFootFeatureSample left = ref source.Left;
            ref readonly AnimationFootFeatureSample right = ref source.Right;
            Left = new AnimationBiomechanicalStepReadPage(
                in left,
                global::ThirdPersonCharacter.Pipeline.Presentation.CharacterFootSide.Left);
            Right = new AnimationBiomechanicalStepReadPage(
                in right,
                global::ThirdPersonCharacter.Pipeline.Presentation.CharacterFootSide.Right);
            m_IsSpecified = 1;
        }

        readonly byte m_IsSpecified;
        public float Weight { get; }
        public AnimationBiomechanicalStepReadPage Left { get; }
        public AnimationBiomechanicalStepReadPage Right { get; }
        public bool IsValid => m_IsSpecified != 0 && Left.IsValid && Right.IsValid;
    }
}
