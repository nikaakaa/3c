using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public sealed class CharacterClipPoseSourceSlot : CharacterPresentationPoseSourceSlot
    {
        public override PresentationPoseSourceKind SourceKind => PresentationPoseSourceKind.Clip;
        public override Type BindingType => typeof(CharacterClipPoseSourceBinding);
    }
}
