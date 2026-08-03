using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public sealed class CharacterMotionMatchingPoseSourceSlot : CharacterPresentationPoseSourceSlot
    {
        public override PresentationPoseSourceKind SourceKind => PresentationPoseSourceKind.MotionMatching;
        public override Type BindingType => typeof(CharacterMotionMatchingPoseSourceBinding);
    }
}
