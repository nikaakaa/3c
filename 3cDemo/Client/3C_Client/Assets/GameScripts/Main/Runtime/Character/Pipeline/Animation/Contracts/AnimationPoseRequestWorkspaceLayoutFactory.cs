using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal static class AnimationPoseRequestWorkspaceLayoutFactory
    {
        internal static AnimationPoseRequestWorkspaceLayout Create(CharacterAnimationPresentationBindingIndex bindings)
        {
            if (bindings == null)
                throw new ArgumentNullException(nameof(bindings));
            if (!bindings.IsValid)
                throw new InvalidOperationException("Animation Presentation Binding Index is invalid.");
            CharacterPresentationProjection projection = bindings.Projection ??
                throw new InvalidOperationException("Animation Presentation Projection is missing.");
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
                            AnimationBlendNodePayload blendNode = projection.PosePlan.RequireBlendNode(operation.NodeId);
                            if (blendNode.StackPolicy == null || blendNode.StackPolicy.MaxActiveSourceEntries <= 0)
                                throw new InvalidOperationException($"Blend Stack '{operation.NodeId}' has an invalid source capacity.");
                            sourceCapacity = checked(sourceCapacity + blendNode.StackPolicy.MaxActiveSourceEntries + 1);
                            playerCount++;
                            break;
                    }
                }
                if (sourceCapacity <= 0 || playerCount <= 0)
                    throw new InvalidOperationException("Pose Plan requires at least one explicit Player node.");

                int clipStride = 0;
                foreach (var pair in bindings.Bindings)
                {
                    ResolvedAnimationProducerBinding binding = pair.Value;
                    if (!pair.Key.IsValid || !pair.Key.Equals(binding.ProducerId) || !binding.IsValid)
                        throw new InvalidOperationException($"Animation binding '{pair.Key}' is invalid.");
                    clipStride = Math.Max(clipStride, binding.AuthoredClipCount);
                }
                if (clipStride <= 0)
                    throw new InvalidOperationException("Animation Selection workspace has no clip binding capacity.");

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
    }
}
