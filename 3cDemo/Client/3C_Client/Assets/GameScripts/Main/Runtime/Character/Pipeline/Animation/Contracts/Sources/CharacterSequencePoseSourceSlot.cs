using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public sealed class CharacterSequencePoseSourceSlot : CharacterPresentationPoseSourceSlot
    {
        public override PresentationPoseSourceKind SourceKind => PresentationPoseSourceKind.Sequence;
        public override Type BindingType => typeof(CharacterSequencePoseSourceBinding);
    }
}
