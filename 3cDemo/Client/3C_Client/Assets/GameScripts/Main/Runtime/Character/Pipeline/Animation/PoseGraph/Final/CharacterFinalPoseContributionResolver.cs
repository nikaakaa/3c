using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.Sources;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal static class CharacterFinalPoseContributionResolver
    {
        internal static AnimationPoseSourceContribution Resolve(
            in AnimationPrimitivePoseContribution primitive,
            CharacterPoseSourceModule sourceModule,
            IReadOnlyList<PoseNodeId> playerNodeIds)
        {
            if (sourceModule == null)
                throw new ArgumentNullException(nameof(sourceModule));
            if (playerNodeIds == null ||
                primitive.PhysicalPlayerIndex < 0 ||
                primitive.PhysicalPlayerIndex >= playerNodeIds.Count ||
                !IsContributionKind(primitive.Kind) ||
                primitive.ContributionContinuityIdentity == 0 ||
                !IsWeight(primitive.Weight) ||
                !IsWeight(primitive.LeftFootWeight) ||
                !IsWeight(primitive.RightFootWeight))
            {
                throw new InvalidOperationException(
                    "Final Animation Pose primitive contribution is invalid.");
            }

            PoseNodeId playerNodeId = playerNodeIds[primitive.PhysicalPlayerIndex];
            if (!playerNodeId.IsValid)
                throw new InvalidOperationException(
                    "Final Animation Pose primitive contribution player identity is invalid.");

            AnimationPoseSourceId sourceId = default;
            if (primitive.Kind == AnimationPoseContributionKind.Live)
            {
                if (primitive.PhysicalSourceIndex < 0 ||
                    primitive.PhysicalSourceGeneration == 0 ||
                    primitive.SourceOwnerIndex < 0)
                {
                    throw new InvalidOperationException(
                        "Final Animation Pose Live contribution identity is invalid.");
                }
                var physicalIdentity = new AnimationPhysicalSourceIdentity(
                    new AnimationPhysicalSourceIndex(primitive.PhysicalSourceIndex),
                    primitive.PhysicalSourceGeneration);
                sourceId = sourceModule.RequireSourceId(physicalIdentity);
                if (!sourceModule.RequirePoseNodeId(physicalIdentity).Equals(playerNodeId) ||
                    sourceModule.RequireSourceOwnerIndex(physicalIdentity) !=
                    primitive.SourceOwnerIndex)
                {
                    throw new InvalidOperationException(
                        $"Final Animation Pose Live contribution metadata is stale: player={primitive.PhysicalPlayerIndex}, " +
                        $"expectedNode={playerNodeId}, actualNode={sourceModule.RequirePoseNodeId(physicalIdentity)}, " +
                        $"expectedOwner={primitive.SourceOwnerIndex}, actualOwner={sourceModule.RequireSourceOwnerIndex(physicalIdentity)}.");
                }
            }
            else if (primitive.PhysicalSourceIndex != -1 ||
                     primitive.PhysicalSourceGeneration != 0 ||
                     primitive.SourceOwnerIndex != -1)
            {
                throw new InvalidOperationException(
                    "Final Animation Pose stored contribution carries a Live identity.");
            }

            return new AnimationPoseSourceContribution(
                playerNodeId,
                primitive.Kind,
                sourceId,
                primitive.SourceOwnerIndex,
                primitive.ContributionContinuityIdentity,
                primitive.Weight,
                primitive.LeftFootWeight,
                primitive.RightFootWeight);
        }

        static bool IsWeight(float value) =>
            float.IsFinite(value) && value >= 0f && value <= 1f;

        static bool IsContributionKind(AnimationPoseContributionKind value) =>
            (int)value >= (int)AnimationPoseContributionKind.Live &&
            (int)value <= (int)AnimationPoseContributionKind.Stored;
    }
}
