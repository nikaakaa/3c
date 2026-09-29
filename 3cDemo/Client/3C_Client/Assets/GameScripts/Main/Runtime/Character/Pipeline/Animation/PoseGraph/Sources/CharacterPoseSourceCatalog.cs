using System;
using ThirdPersonCharacter.Pipeline.Presentation.Animancer;

namespace ThirdPersonCharacter.Pipeline.Animation.Sources
{
    internal sealed class CharacterPoseSourceCatalog
    {
        readonly int m_ClipCapacity;

        internal CharacterPoseSourceCatalog(int clipCapacity)
        {
            if (clipCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(clipCapacity));
            m_ClipCapacity = clipCapacity;
        }

        internal AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding>
            Build(in AnimationReadOnlyBuffer<ClipSamplePlan> clips)
        {
            if (clips.Count <= 0 || clips.Count > m_ClipCapacity)
                throw new ArgumentException(
                    "Animation pose source clip plans exceed their compiled capacity.",
                    nameof(clips));
            var bindings = new AnimationPoseSourceClipBinding[clips.Count];
            for (int i = 0; i < bindings.Length; i++)
            {
                ref readonly ClipSamplePlan sample = ref clips.ElementAt(i);
                if (!sample.IsValid || sample.ClipBindingIndex != i)
                    throw new InvalidOperationException(
                        "Animation pose source clip plans are not contiguous or contain an invalid sample.");
                bindings[i] = sample.IsAcl
                    ? new AnimationPoseSourceClipBinding(
                        i,
                        sample.ResourceCatalogIndex,
                        sample.GroupClipIndex)
                    : new AnimationPoseSourceClipBinding(i, sample.Clip);
            }
            return new AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding>(
                bindings,
                0,
                bindings.Length);
        }
    }
}
