using System;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    internal static class AnimationClipPlayerFactory
    {
        internal static AnimationClipPlayerRuntime Create(
            CharacterPresentationProjection projection,
            CharacterPresentationClipPlayerDescriptor descriptor)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));
            projection.RequirePosePayload();
            if (!projection.TryGetPoseSource(descriptor.PresentationPoseSourceIndex, out CharacterPresentationPoseSourcePlan source))
            {
                throw new InvalidOperationException(
                    $"Clip Player '{descriptor.NodeId}' cannot resolve Presentation Pose source index '{descriptor.PresentationPoseSourceIndex}'.");
            }
            return new AnimationClipPlayerRuntime(
                descriptor,
                source,
                projection.PosePlan,
                projection.Rig);
        }
    }
}
