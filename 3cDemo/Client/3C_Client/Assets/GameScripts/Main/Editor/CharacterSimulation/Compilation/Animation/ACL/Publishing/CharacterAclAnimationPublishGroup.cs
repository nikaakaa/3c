using System;
using ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation;
using ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation.ACL;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class CharacterAclAnimationPublishGroup
    {
        internal CharacterAclAnimationPublishGroup(
            CharacterAclAnimationGroupArtifact artifact,
            CharacterAnimationCompiledResourceDescriptor descriptor,
            string assetStem)
        {
            Artifact = artifact ?? throw new ArgumentNullException(nameof(artifact));
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            AssetStem = assetStem ?? throw new ArgumentNullException(nameof(assetStem));
        }

        internal CharacterAclAnimationGroupArtifact Artifact { get; }
        internal CharacterAnimationCompiledResourceDescriptor Descriptor { get; }
        internal string AssetStem { get; }
        internal CharacterAclAnimationResource Resource { get; set; }
    }

}
