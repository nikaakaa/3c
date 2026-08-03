using System;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal static class AnimationPoseRequestWorkspaceLayoutFactory
    {
        internal static AnimationPoseRequestWorkspaceLayout Create(
            CharacterPresentationProjection projection)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            projection.RequirePosePayload();

            try
            {
                int sourceCapacity = 0;
                int playerCount = 0;
                for (int i = 0; i < projection.PosePlan.Operations.Count; i++)
                {
                    CharacterPresentationPoseOperation operation = projection.PosePlan.Operations[i];
                    switch (operation.Code)
                    {
                        case CharacterPoseOperationCode.SelectedPosePlayer:
                        case CharacterPoseOperationCode.BlendSpacePlayer:
                            sourceCapacity = checked(sourceCapacity + 1);
                            playerCount++;
                            break;
                        case CharacterPoseOperationCode.BlendStack:
                        case CharacterPoseOperationCode.AnimationSlot:
                            AnimationBlendNodePayload blendNode = projection.PosePlan.RequireBlendNode(operation.NodeId);
                            if (blendNode.StackPolicy == null || blendNode.StackPolicy.MaxActiveSourceEntries <= 0)
                                throw new InvalidOperationException($"Blend Stack '{operation.NodeId}' has an invalid source capacity.");
                            sourceCapacity = checked(sourceCapacity + blendNode.StackPolicy.MaxActiveSourceEntries + 1);
                            playerCount++;
                            break;
                        case CharacterPoseOperationCode.SequencePlayer:
                            playerCount++;
                            break;
                    }
                }
                if (sourceCapacity <= 0 || playerCount <= 0)
                    throw new InvalidOperationException("Pose Plan requires at least one explicit Player node.");

                int clipStride = RequireClipCatalogCapacity(projection);

                int parameterStride = projection.PosePlan.Parameters.Count;
                if (parameterStride <= 0)
                    throw new InvalidOperationException("Animation Selection workspace parameter stride must be positive.");
                int footPlacementWeightParameterIndex = projection.PosePlan.RequireParameterIndex(
                    AnimationPoseParameterIds.FootPlacementWeight);
                return new AnimationPoseRequestWorkspaceLayout(
                    sourceCapacity,
                    clipStride,
                    parameterStride,
                    footPlacementWeightParameterIndex);
            }
            catch (OverflowException exception)
            {
                throw new InvalidOperationException("Animation Selection workspace capacity overflowed Int32.", exception);
            }
        }

        internal static int RequireClipCatalogCapacity(
            CharacterPresentationProjection projection)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            projection.RequirePosePayload();

            int capacity = 0;
            for (int i = 0; i < projection.Producers.Count; i++)
            {
                CharacterPresentationProducerEntry producer =
                    projection.Producers[i];
                if (producer?.Kind !=
                    CharacterPresentationProducerKind.Animation)
                {
                    continue;
                }
                CharacterPresentationAnimationBinding animation =
                    producer.Animation;
                if (animation == null ||
                    !animation.Source ||
                    !animation.Source.IsValid ||
                    animation.Clips.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Animation producer '{producer.ProgramProducerIdentity}' has an invalid source binding.");
                }
                capacity = Math.Max(capacity, animation.Clips.Count);
            }

            if (projection.PoseSources.Count > 0)
                capacity = Math.Max(capacity, 1);

            for (int i = 0; i < projection.BlendSpaces.Count; i++)
            {
                CharacterAnimationBlendSpacePlan blendSpace =
                    projection.BlendSpaces[i];
                if (blendSpace == null || blendSpace.Samples.Count == 0)
                    throw new InvalidOperationException(
                        $"Blend Space catalog #{i} is invalid.");
                capacity = Math.Max(capacity, blendSpace.Samples.Count);
            }

            MotionMatchingProjectionPayload motionMatching =
                projection.MotionMatching;
            if (motionMatching != null)
            {
                for (int i = 0; i < motionMatching.DatabaseCount; i++)
                {
                    MotionMatchingDatabasePayload database =
                        motionMatching.GetDatabase(i);
                    if (database == null || database.ClipBindingCount <= 0)
                        throw new InvalidOperationException(
                            $"Motion Matching Database catalog #{i} is invalid.");
                    capacity = Math.Max(
                        capacity,
                        database.ClipBindingCount);
                }
            }

            if (capacity <= 0)
                throw new InvalidOperationException(
                    "Animation Selection workspace has no formal clip catalog capacity.");
            return capacity;
        }
    }
}
