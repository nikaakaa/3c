using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class CharacterPoseResourceAuthoringService
    {
        public static CharacterPoseResourceSlot CreateSlot(
            CharacterPresentationPoseGraphAsset graph,
            string displayName,
            CharacterPoseResourceKind kind)
        {
            if (!graph || string.IsNullOrWhiteSpace(displayName) ||
                !Enum.IsDefined(typeof(CharacterPoseResourceKind), kind))
                throw new ArgumentException("Pose Resource Slot creation inputs are incomplete.");
            CharacterPoseResourceSlot slot = CreateSlot(displayName, kind);
            var transaction = new CharacterPresentationMutationTransaction(
                Guid.NewGuid().ToString("N"),
                "Create Pose Resource Slot");
            transaction.Add(new CreatePoseResourceSlotMutation(
                RequireAssetOwnerId(graph),
                slot));
            new CharacterPresentationMutationService().Apply(
                new CharacterPoseGraphAssetMutationOwner(graph),
                transaction);
            return slot;
        }

        public static CharacterPoseResourceSlot CreateSlot(
            string displayName,
            CharacterPoseResourceKind kind)
        {
            if (string.IsNullOrWhiteSpace(displayName) ||
                !Enum.IsDefined(typeof(CharacterPoseResourceKind), kind))
                throw new ArgumentException("Pose Resource Slot creation inputs are incomplete.");
            CharacterPoseResourceSlot slot = CharacterPoseResourceSlot.Create(kind);
            slot.name = displayName.Trim();
            return slot;
        }

        static string RequireAssetOwnerId(CharacterPresentationPoseGraphAsset graph)
        {
            string path = UnityEditor.AssetDatabase.GetAssetPath(graph);
            string assetGuid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
            return string.IsNullOrWhiteSpace(assetGuid)
                ? throw new InvalidOperationException("Pose Graph asset has no stable Asset identity.")
                : assetGuid;
        }
    }
}
