using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public sealed class CharacterAnimationPresentationBindings
    {
        internal CharacterAnimationPresentationBindings(
            CharacterPresentationProjection projection,
            ActionAnimationBindingIndex actionPlayback,
            PoseSourceProviderBindingIndex poseSourceProviders)
        {
            Projection = projection ??
                throw new ArgumentNullException(nameof(projection));
            ActionPlayback = actionPlayback ??
                throw new ArgumentNullException(nameof(actionPlayback));
            PoseSourceProviders = poseSourceProviders ??
                throw new ArgumentNullException(nameof(poseSourceProviders));
            if (!ReferenceEquals(
                    Projection,
                    ActionPlayback.Projection) ||
                !ReferenceEquals(
                    Projection,
                    PoseSourceProviders.Projection))
            {
                throw new InvalidOperationException(
                    "Animation Presentation binding modules do not share one Projection.");
            }
        }

        public CharacterPresentationProjection Projection { get; }
        public ActionAnimationBindingIndex ActionPlayback { get; }
        public PoseSourceProviderBindingIndex PoseSourceProviders { get; }
    }

    public static class CharacterAnimationPresentationBindingFactory
    {
        public static CharacterAnimationPresentationBindings Build(
            CharacterPresentationProjection projection)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            projection.RequirePosePayload();
            return new CharacterAnimationPresentationBindings(
                projection,
                ActionAnimationBindingIndex.Build(
                    projection),
                PoseSourceProviderBindingIndex.Build(projection));
        }
    }
}
