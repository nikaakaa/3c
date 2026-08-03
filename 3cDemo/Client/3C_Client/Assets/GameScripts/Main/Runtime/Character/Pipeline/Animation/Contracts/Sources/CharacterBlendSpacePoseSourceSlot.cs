using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public sealed class CharacterBlendSpacePoseSourceSlot : CharacterPresentationPoseSourceSlot
    {
        public override PresentationPoseSourceKind SourceKind => PresentationPoseSourceKind.BlendSpace;
        public override Type BindingType => typeof(CharacterBlendSpacePoseSourceBinding);
    }
}
