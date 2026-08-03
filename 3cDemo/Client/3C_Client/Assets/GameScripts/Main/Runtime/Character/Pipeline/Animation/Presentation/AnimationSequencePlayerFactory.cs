using System;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    internal static class AnimationSequencePlayerFactory
    {
        internal static AnimationSequencePlayerRuntime Create(
            CharacterPresentationProjection projection,
            CharacterPresentationSequencePlayerDescriptor descriptor)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));
            projection.RequirePosePayload();
            if (!projection.TryGetPoseSource(descriptor.PresentationPoseSourceIndex, out CharacterPresentationPoseSourcePlan source))
            {
                throw new InvalidOperationException(
                    $"Sequence Player '{descriptor.NodeId}' cannot resolve Presentation Pose source index '{descriptor.PresentationPoseSourceIndex}'.");
            }
            return new AnimationSequencePlayerRuntime(
                descriptor,
                source,
                projection.PosePlan,
                projection.Rig);
        }
    }
}
