using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;

namespace ThirdPersonCharacter.Pipeline.Presentation.Animancer
{
    internal sealed class CharacterAclFrameJournal
    {
        internal struct Mutation
        {
            internal AnimationPhysicalSourceIdentity PhysicalIdentity;
            internal CharacterAclSourceKey Key;
            internal CharacterAclSourceInstance Instance;
            internal AnimationPoseSourceCaptureBinding Capture;
            internal int PlanOffset;
            internal int PlanCount;
            internal AnimationPoseSourcePrepareKind Kind;

            internal bool IsValid => PhysicalIdentity.IsValid && Key.IsValid &&
                                      Instance != null && Capture.CompletionIdentity != 0 &&
                                      PlanCount > 0;
        }

        internal struct ReleasePermission
        {
            internal AnimationPhysicalSourceIdentity PhysicalIdentity;
            internal CharacterAclSourceKey Key;
            internal CharacterAclSourceInstance Instance;
            internal ulong Generation;
            internal bool Consumed;

            internal bool IsValid => PhysicalIdentity.IsValid && Key.IsValid &&
                                     Instance != null && Generation != 0;
        }

        readonly Mutation[] m_Mutations;
        readonly ReleasePermission[] m_Releases;
        int m_MutationCount;
        int m_ReleaseCount;
        int m_UnconsumedReleaseCount;
        ulong m_LastReleaseGeneration;

        internal CharacterAclFrameJournal(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_Mutations = new Mutation[capacity];
            m_Releases = new ReleasePermission[capacity];
        }

        internal int MutationCount => m_MutationCount;
        internal int ReleaseCount => m_ReleaseCount;
        internal int UnconsumedReleaseCount => m_UnconsumedReleaseCount;

        internal int FindMutation(in CharacterAclSourceKey key)
        {
            for (int i = 0; i < m_MutationCount; i++)
            {
                ref readonly Mutation mutation = ref m_Mutations[i];
                if (mutation.Key.Equals(in key))
                    return i;
            }
            return -1;
        }

        internal ref Mutation RequireMutation(int index)
        {
            if ((uint)index >= (uint)m_MutationCount || !m_Mutations[index].IsValid)
                throw new ArgumentOutOfRangeException(nameof(index));
            return ref m_Mutations[index];
        }

        internal void AddMutation(in Mutation mutation)
        {
            if (!mutation.IsValid || m_MutationCount >= m_Mutations.Length)
                throw new InvalidOperationException("ACL source mutation capacity was exceeded.");
            for (int i = 0; i < m_MutationCount; i++)
            {
                ref readonly Mutation existing = ref m_Mutations[i];
                if (existing.Key.Equals(in mutation.Key) ||
                    m_Mutations[i].PhysicalIdentity == mutation.PhysicalIdentity)
                    throw new InvalidOperationException("ACL source mutation identity is duplicated.");
            }
            m_Mutations[m_MutationCount++] = mutation;
        }

        internal AnimationPoseSourceReleaseToken AddRelease(
            in AnimationPhysicalSourceIdentity physicalIdentity,
            in CharacterAclSourceKey key,
            CharacterAclSourceInstance instance)
        {
            if (!physicalIdentity.IsValid || !key.IsValid || instance == null ||
                m_ReleaseCount >= m_Releases.Length)
                throw new InvalidOperationException("ACL source release capacity was exceeded.");
            for (int i = 0; i < m_ReleaseCount; i++)
            {
                ref readonly ReleasePermission existing = ref m_Releases[i];
                if (existing.IsValid && existing.Key.Equals(in key))
                    throw new InvalidOperationException("ACL source release mutation is duplicated.");
            }
            ulong generation = ++m_LastReleaseGeneration;
            if (generation == 0)
                throw new InvalidOperationException("ACL source release generation was exhausted.");
            int index = m_ReleaseCount++;
            m_Releases[index] = new ReleasePermission
            {
                PhysicalIdentity = physicalIdentity,
                Key = key,
                Instance = instance,
                Generation = generation
            };
            m_UnconsumedReleaseCount++;
            return new AnimationPoseSourceReleaseToken(
                index,
                generation,
                CharacterAnimationSamplingBackendKind.Acl,
                key.SourceId,
                key.PlayerNodeId);
        }

        internal bool ContainsRelease(in CharacterAclSourceKey key)
        {
            for (int i = 0; i < m_ReleaseCount; i++)
            {
                ref readonly ReleasePermission permission = ref m_Releases[i];
                if (permission.IsValid && permission.Key.Equals(in key))
                    return true;
            }
            return false;
        }

        internal ReleasePermission ConsumeRelease(
            in AnimationPoseSourceReleaseToken token)
        {
            if (!token.IsValid || token.Backend != CharacterAnimationSamplingBackendKind.Acl ||
                (uint)token.PermissionIndex >= (uint)m_ReleaseCount)
                throw new InvalidOperationException("ACL source release token is invalid.");
            ref ReleasePermission permission = ref m_Releases[token.PermissionIndex];
            if (!permission.IsValid || permission.Consumed || permission.Generation != token.Generation ||
                permission.Key.SourceId != token.SourceId || permission.Key.PlayerNodeId != token.PlayerNodeId)
                throw new InvalidOperationException("ACL source release token is stale.");
            permission.Consumed = true;
            m_UnconsumedReleaseCount--;
            return permission;
        }

        internal void ClearMutations()
        {
            Array.Clear(m_Mutations, 0, m_MutationCount);
            m_MutationCount = 0;
        }

        internal void ClearReleases()
        {
            Array.Clear(m_Releases, 0, m_ReleaseCount);
            m_ReleaseCount = 0;
            m_UnconsumedReleaseCount = 0;
        }
    }
}
