using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal static class AnimationPoseRequestWorkspaceLayoutFactory
    {
        internal static AnimationPoseRequestWorkspaceLayout Create(
            CharacterAnimationPresentationBindingIndex bindings)
        {
            if (bindings == null)
                throw new ArgumentNullException(nameof(bindings));
            if (!bindings.IsValid)
                throw new InvalidOperationException("Animation Presentation Binding Index is invalid.");
            CharacterPresentationProjection projection = bindings.Projection ??
                throw new InvalidOperationException("Animation Presentation Projection is missing.");
            projection.RequirePosePayload();

            if (bindings.Slots.Count == 0 ||
                bindings.Slots.Count != projection.PoseProgram.Slots.Count ||
                bindings.Channels.Count != bindings.Slots.Count)
            {
                throw new InvalidOperationException("Animation Pose Request workspace requires every compiled Pose Slot exactly once.");
            }
            if (bindings.Bindings.Count == 0)
                throw new InvalidOperationException("Animation Pose Request workspace requires at least one animation binding.");

            try
            {
                int sourceCapacity = 0;
                foreach (var pair in bindings.Slots)
                {
                    ResolvedAnimationPoseSlot slot = pair.Value;
                    if (!pair.Key.IsValid || pair.Key != slot.PoseSlotId || slot.Index < 0 ||
                        slot.Index >= projection.PoseProgram.Slots.Count ||
                        !slot.PoseSlotId.IsValid || !slot.AnimationChannelId.IsValid ||
                        !Enum.IsDefined(typeof(PoseSlotOutputPolicy), slot.OutputPolicy) ||
                        slot.BlendPayload == null || slot.BlendPayload.StackPolicy == null ||
                        slot.BlendPayload.PoseSlotId != slot.PoseSlotId ||
                        slot.BlendPayload.AnimationChannelId != slot.AnimationChannelId ||
                        slot.BlendPayload.OutputPolicy != slot.OutputPolicy ||
                        slot.BlendPayload.StackPolicy.MaxActiveSourceEntries <= 0)
                    {
                        throw new InvalidOperationException(
                            $"Animation Pose Request workspace Pose Slot '{pair.Key}' is invalid.");
                    }
                    sourceCapacity = checked(
                        sourceCapacity + checked(slot.BlendPayload.StackPolicy.MaxActiveSourceEntries + 1));
                }
                if (sourceCapacity <= 0)
                    throw new InvalidOperationException("Animation Pose Request workspace source capacity must be positive.");

                int clipStride = 0;
                foreach (var pair in bindings.Bindings)
                {
                    ResolvedAnimationProducerBinding binding = pair.Value;
                    if (!pair.Key.IsValid || !pair.Key.Equals(binding.ProducerId) ||
                        !binding.IsValid || binding.AuthoredClipCount <= 0)
                    {
                        throw new InvalidOperationException(
                            $"Animation Pose Request workspace binding '{pair.Key}' is invalid.");
                    }
                    clipStride = Math.Max(clipStride, binding.AuthoredClipCount);
                }
                if (clipStride <= 0)
                    throw new InvalidOperationException("Animation Pose Request workspace clip stride must be positive.");

                int parameterStride = projection.PoseProgram.Parameters.Count;
                if (parameterStride <= 0)
                    throw new InvalidOperationException("Animation Pose Request workspace parameter stride must be positive.");
                int footPlacementWeightParameterIndex = projection.PoseProgram.RequireParameterIndex(
                    AnimationPoseParameterIds.FootPlacementWeight);

                return new AnimationPoseRequestWorkspaceLayout(
                    sourceCapacity,
                    clipStride,
                    parameterStride,
                    footPlacementWeightParameterIndex);
            }
            catch (OverflowException exception)
            {
                throw new InvalidOperationException(
                    "Animation Pose Request workspace capacity overflowed Int32.",
                    exception);
            }
        }
    }
}
