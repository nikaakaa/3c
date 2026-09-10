using System;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed class CreatePoseResourceSlotMutation : CharacterPresentationMutation
    {
        public CreatePoseResourceSlotMutation(
            string graphAssetId,
            CharacterPoseResourceSlot slot)
            : base(CharacterPresentationMutationKind.CreatePoseResourceSlot, graphAssetId)
        {
            Slot = slot ? slot : throw new ArgumentNullException(nameof(slot));
        }

        public CharacterPoseResourceSlot Slot { get; }
    }

    public sealed class RenamePoseResourceSlotMutation : CharacterPresentationMutation
    {
        public RenamePoseResourceSlotMutation(
            string graphAssetId,
            CharacterPoseResourceSlot slot,
            string displayName)
            : base(CharacterPresentationMutationKind.RenamePoseResourceSlot, graphAssetId)
        {
            Slot = slot ? slot : throw new ArgumentNullException(nameof(slot));
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? throw new ArgumentException("Pose Resource Slot name is missing.", nameof(displayName))
                : displayName.Trim();
        }

        public CharacterPoseResourceSlot Slot { get; }
        public string DisplayName { get; }
    }

    public sealed class DeletePoseResourceSlotMutation : CharacterPresentationMutation
    {
        public DeletePoseResourceSlotMutation(
            string graphAssetId,
            CharacterPoseResourceSlot slot)
            : base(CharacterPresentationMutationKind.DeletePoseResourceSlot, graphAssetId)
        {
            Slot = slot ? slot : throw new ArgumentNullException(nameof(slot));
        }

        public CharacterPoseResourceSlot Slot { get; }
    }
}
