using System;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Animation.Resources;
using ThirdPersonCharacter.Pipeline.Presentation.Animancer;

namespace ThirdPersonCharacter.Pipeline.Animation.Sources
{
    internal sealed class CharacterPoseSourceCatalog
    {
        readonly AnimationPoseSourceClipBinding[][] m_Action;
        readonly AnimationPoseSourceClipBinding[][] m_Clip;
        readonly AnimationPoseSourceClipBinding[][] m_BlendSpace;
        readonly AnimationPoseSourceClipBinding[][] m_MotionMatching;
        readonly CharacterAnimationCompiledResourceDescriptor[] m_Resources;

        internal CharacterPoseSourceCatalog(
            CharacterPresentationProjection projection,
            int clipCapacity)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            if (clipCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(clipCapacity));
            m_Resources = new CharacterAnimationCompiledResourceDescriptor[
                projection.AnimationResources.Count];
            for (int i = 0; i < m_Resources.Length; i++)
                m_Resources[i] = projection.AnimationResources[i] ??
                    throw new InvalidOperationException(
                        $"Animation resource descriptor #{i} is missing.");

            m_Action = new AnimationPoseSourceClipBinding[
                projection.Producers.Count][];
            for (int producerIndex = 0;
                 producerIndex < projection.Producers.Count;
                 producerIndex++)
            {
                CharacterPresentationAnimationBinding animation =
                    projection.Producers[producerIndex]?.Animation;
                if (animation == null)
                    continue;
                if (animation.Clips.Count == 0 ||
                    animation.Clips.Count > clipCapacity)
                {
                    throw new InvalidOperationException(
                        $"Animation producer #{producerIndex} clip catalog exceeds its compiled capacity.");
                }
                var bindings = new AnimationPoseSourceClipBinding[
                    animation.Clips.Count];
                for (int i = 0; i < bindings.Length; i++)
                {
                    CharacterPresentationAnimationClipBinding binding =
                        animation.Clips[i] ??
                        throw new InvalidOperationException(
                            $"Animation producer #{producerIndex} clip binding #{i} is missing.");
                    bindings[i] = binding.IsAcl
                        ? new AnimationPoseSourceClipBinding(
                            i,
                            binding.ResourceCatalogIndex,
                            binding.GroupClipIndex)
                        : new AnimationPoseSourceClipBinding(i, binding.Clip);
                }
                m_Action[producerIndex] = bindings;
            }

            int clipSourceCapacity = 0;
            for (int i = 0; i < projection.PoseSources.Count; i++)
            {
                CharacterPresentationPoseSourcePlan source =
                    projection.PoseSources[i];
                if (source == null || !source.SourceIndex.IsValid)
                {
                    throw new InvalidOperationException(
                        $"Sequence source #{i} has no compiled clip catalog.");
                }
                clipSourceCapacity = Math.Max(
                    clipSourceCapacity,
                    checked(source.SourceIndex.Value + 1));
            }
            m_Clip = new AnimationPoseSourceClipBinding[
                clipSourceCapacity][];
            for (int i = 0; i < projection.PoseSources.Count; i++)
            {
                CharacterPresentationPoseSourcePlan source =
                    projection.PoseSources[i];
                m_Clip[source.SourceIndex.Value] =
                    source.IsAcl
                        ? new[]
                        {
                            new AnimationPoseSourceClipBinding(
                                0,
                                source.ResourceCatalogIndex,
                                source.GroupClipIndex)
                        }
                        : new[] { new AnimationPoseSourceClipBinding(0, source.Clip) };
            }

            m_BlendSpace = new AnimationPoseSourceClipBinding[
                projection.BlendSpacePlayers.Count][];
            for (int playerIndex = 0;
                 playerIndex < projection.BlendSpacePlayers.Count;
                 playerIndex++)
            {
                CharacterAnimationBlendSpacePlayerPlan player =
                    projection.BlendSpacePlayers[playerIndex];
                CharacterAnimationBlendSpacePlan plan =
                    projection.BlendSpaces[
                        player.BlendSpacePlanIndex];
                if (plan.Samples.Count == 0 ||
                    plan.Samples.Count > clipCapacity)
                {
                    throw new InvalidOperationException(
                        $"Blend Space Player '{player.NodeId}' clip catalog exceeds its compiled capacity.");
                }
                var bindings = new AnimationPoseSourceClipBinding[
                    plan.Samples.Count];
                for (int i = 0; i < bindings.Length; i++)
                {
                    CharacterAnimationBlendSpaceSamplePlan sample =
                        plan.Samples[i] ??
                        throw new InvalidOperationException(
                            $"Blend Space Player '{player.NodeId}' sample #{i} is missing.");
                    bindings[i] = sample.IsAcl
                        ? new AnimationPoseSourceClipBinding(
                            i,
                            sample.ResourceCatalogIndex,
                            sample.GroupClipIndex)
                        : new AnimationPoseSourceClipBinding(i, sample.Clip);
                }
                m_BlendSpace[playerIndex] = bindings;
            }

            MotionMatchingProjectionPayload motionMatching =
                projection.MotionMatching;
            m_MotionMatching = new AnimationPoseSourceClipBinding[
                motionMatching?.DatabaseCount ?? 0][];
            for (int databaseIndex = 0;
                 databaseIndex < m_MotionMatching.Length;
                 databaseIndex++)
            {
                MotionMatchingDatabasePayload database =
                    motionMatching.GetDatabase(databaseIndex);
                if (database == null ||
                    database.ClipBindingCount == 0 ||
                    database.ClipBindingCount > clipCapacity)
                {
                    throw new InvalidOperationException(
                        $"Motion Matching Database #{databaseIndex} clip catalog exceeds its compiled capacity.");
                }
                var bindings = new AnimationPoseSourceClipBinding[
                    database.ClipBindingCount];
                for (int i = 0; i < bindings.Length; i++)
                {
                    MotionMatchingClipBindingPayload binding =
                        database.GetClipBinding(i) ??
                        throw new InvalidOperationException(
                            $"Motion Matching Database #{databaseIndex} clip binding #{i} is missing.");
                    if (!binding.IsValid)
                        throw new InvalidOperationException(
                            $"Motion Matching Database #{databaseIndex} clip binding #{i} is invalid.");
                    bindings[i] = binding.IsAcl
                        ? new AnimationPoseSourceClipBinding(
                            i,
                            binding.ResourceCatalogIndex,
                            binding.GroupClipIndex)
                        : new AnimationPoseSourceClipBinding(i, binding.Clip);
                }
                m_MotionMatching[databaseIndex] = bindings;
            }
        }

        internal AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding>
            RequireAction(int producerIndex)
        {
            if ((uint)producerIndex >= (uint)m_Action.Length ||
                m_Action[producerIndex] == null)
            {
                throw new InvalidOperationException(
                    $"Animation producer #{producerIndex} has no clip catalog.");
            }
            return Buffer(m_Action[producerIndex]);
        }

        internal bool HasAclResources
        {
            get
            {
                return ContainsAcl(m_Action) ||
                       ContainsAcl(m_Clip) ||
                       ContainsAcl(m_BlendSpace) ||
                       ContainsAcl(m_MotionMatching);
            }
        }

        internal AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding>
            RequireClip(AnimationPoseSourceId sourceId)
        {
            int sourceIndex = sourceId.PresentationPoseSourceIndex.Value;
            if (sourceId.SourceKind != AnimationPoseSourceKind.Clip ||
                (uint)sourceIndex >= (uint)m_Clip.Length ||
                m_Clip[sourceIndex] == null)
            {
                throw new InvalidOperationException(
                    $"Sequence source '{sourceId}' has no compiled clip catalog.");
            }
            return Buffer(m_Clip[sourceIndex]);
        }

        internal AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding>
            RequireBlendSpace(int playerIndex)
        {
            if ((uint)playerIndex >= (uint)m_BlendSpace.Length ||
                m_BlendSpace[playerIndex] == null)
            {
                throw new InvalidOperationException(
                    $"Blend Space Player #{playerIndex} has no compiled clip catalog.");
            }
            return Buffer(m_BlendSpace[playerIndex]);
        }

        internal AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding>
            RequireMotionMatching(
                in PresentationPoseSourceSample sample)
        {
            int databaseIndex = sample?.ProjectionDatabaseIndex ?? -1;
            if (sample?.SourceKind !=
                    AnimationPoseSourceKind.MotionMatching ||
                (uint)databaseIndex >=
                    (uint)m_MotionMatching.Length ||
                m_MotionMatching[databaseIndex] == null)
            {
                throw new InvalidOperationException(
                    $"Motion Matching source '{sample?.SourceIndex}' has no exact Database catalog.");
            }
            return Buffer(m_MotionMatching[databaseIndex]);
        }

        internal void RegisterAclResources(CharacterAclResourceStore store)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));
            RegisterAclResources(m_Action, store);
            RegisterAclResources(m_Clip, store);
            RegisterAclResources(m_BlendSpace, store);
            RegisterAclResources(m_MotionMatching, store);
        }

        void RegisterAclResources(
            AnimationPoseSourceClipBinding[][] catalogs,
            CharacterAclResourceStore store)
        {
            for (int i = 0; i < catalogs.Length; i++)
            {
                AnimationPoseSourceClipBinding[] catalog = catalogs[i];
                if (catalog == null)
                    continue;
                for (int j = 0; j < catalog.Length; j++)
                {
                    if (catalog[j].IsAcl)
                    {
                        if ((uint)catalog[j].ResourceCatalogIndex >= (uint)m_Resources.Length)
                            throw new InvalidOperationException("ACL animation source references an invalid resource catalog index.");
                        int resourceIndex = store.Register(m_Resources[catalog[j].ResourceCatalogIndex]);
                        store.Request(resourceIndex);
                    }
                }
            }
        }

        static bool ContainsAcl(AnimationPoseSourceClipBinding[][] catalogs)
        {
            for (int i = 0; i < catalogs.Length; i++)
            {
                AnimationPoseSourceClipBinding[] catalog = catalogs[i];
                if (catalog == null)
                    continue;
                for (int j = 0; j < catalog.Length; j++)
                {
                    if (catalog[j].IsAcl)
                        return true;
                }
            }
            return false;
        }

        static AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding>
            Buffer(AnimationPoseSourceClipBinding[] bindings) =>
            new AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding>(
                bindings,
                0,
                bindings.Length);
    }
}
