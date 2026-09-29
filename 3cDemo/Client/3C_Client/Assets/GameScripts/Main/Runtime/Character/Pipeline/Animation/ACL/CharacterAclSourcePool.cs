using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.Resources;
using Unity.Collections;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ThirdPersonCharacter.Pipeline.Presentation.Animancer
{
    internal sealed class CharacterAclSourcePool : IDisposable
    {
        readonly PlayableGraph m_Graph;
        readonly CharacterAnimationRigPayload m_Rig;
        readonly CharacterAclResourceStore m_Store;
        readonly CharacterAclResourceLeaseTable m_LeaseTable;
        readonly NativeArray<TransformStreamHandle> m_Handles;
        readonly NativeArray<AnimationLocalBonePose> m_ReferencePose;
        readonly NativeArray<int> m_PhysicalParentIndices;
        readonly NativeArray<CharacterVirtualBoneDescriptor> m_VirtualBones;
        readonly string[] m_PoseBoneReferenceIdentities;
        readonly CharacterAclSourceInstance[] m_Instances;
        readonly AnimationPhysicalSourceIdentity[] m_Identities;
        readonly byte[] m_Pending;
        readonly byte[] m_Retired;
        bool m_Disposed;

        internal CharacterAclSourcePool(
            PlayableGraph graph,
            CharacterAnimationRigPayload rig,
            CharacterAclResourceStore store,
            NativeArray<TransformStreamHandle> handles,
            NativeArray<AnimationLocalBonePose> referencePose,
            NativeArray<int> physicalParentIndices,
            NativeArray<CharacterVirtualBoneDescriptor> virtualBones,
            string[] poseBoneReferenceIdentities,
            int capacity,
            int clipCapacity,
            int parameterCapacity)
        {
            if (!graph.IsValid() || rig == null || store == null ||
                !handles.IsCreated || !referencePose.IsCreated ||
                !physicalParentIndices.IsCreated || !virtualBones.IsCreated ||
                poseBoneReferenceIdentities == null ||
                poseBoneReferenceIdentities.Length != rig.PoseBoneCount ||
                capacity <= 0 || clipCapacity <= 0 || parameterCapacity <= 0)
                throw new ArgumentException("ACL source pool input is invalid.");
            m_Graph = graph;
            m_Rig = rig;
            m_Store = store;
            m_LeaseTable = new CharacterAclResourceLeaseTable(
                checked(capacity * clipCapacity));
            m_Handles = handles;
            m_ReferencePose = referencePose;
            m_PhysicalParentIndices = physicalParentIndices;
            m_VirtualBones = virtualBones;
            m_PoseBoneReferenceIdentities = poseBoneReferenceIdentities;
            m_Instances = new CharacterAclSourceInstance[capacity];
            m_Identities = new AnimationPhysicalSourceIdentity[capacity];
            m_Pending = new byte[capacity];
            m_Retired = new byte[capacity];
            try
            {
                for (int i = 0; i < capacity; i++)
                    m_Instances[i] = new CharacterAclSourceInstance(
                        graph,
                        rig,
                        store,
                        m_LeaseTable,
                        handles,
                        referencePose,
                        physicalParentIndices,
                        virtualBones,
                        poseBoneReferenceIdentities,
                        clipCapacity,
                        parameterCapacity);
            }
            catch (Exception exception)
            {
                Exception cleanupFailure = null;
                for (int i = 0; i < m_Instances.Length; i++)
                {
                    if (m_Instances[i] == null)
                        continue;
                    try
                    {
                        m_Instances[i].Dispose();
                    }
                    catch (Exception cleanupException)
                    {
                        RecordFailure(ref cleanupFailure, cleanupException);
                    }
                }
                try
                {
                    m_LeaseTable.Dispose();
                }
                catch (Exception cleanupException)
                {
                    RecordFailure(ref cleanupFailure, cleanupException);
                }
                if (cleanupFailure != null)
                    throw new AggregateException(
                        "ACL source pool construction cleanup failed.",
                        exception,
                        cleanupFailure);
                throw;
            }
        }

        internal CharacterAclSourceInstance Prepare(
            in AnimationPhysicalSourceIdentity physicalIdentity,
            in CharacterAclSourceKey key,
            in AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding> catalog,
            in AnimationPoseSourceCaptureBinding capture)
        {
            RequireAlive();
            int index = RequireIndex(physicalIdentity);
            if (m_Instances[index] == null || m_Pending[index] != 0 || m_Retired[index] != 0)
                throw new InvalidOperationException("ACL source physical slot is already occupied.");
            CharacterAclSourceInstance instance = m_Instances[index];
            instance.Configure(key, catalog, in capture);
            m_Instances[index] = instance;
            m_Identities[index] = physicalIdentity;
            m_Pending[index] = 1;
            return instance;
        }

        internal CharacterAclSourceInstance RequireCommitted(
            in AnimationPhysicalSourceIdentity physicalIdentity,
            in CharacterAclSourceKey key)
        {
            int index = RequireIndex(physicalIdentity);
            CharacterAclSourceInstance instance = m_Instances[index];
            if (instance == null || m_Pending[index] != 0 || m_Retired[index] != 0 ||
                m_Identities[index] != physicalIdentity ||
                !instance.Key.Equals(in key))
                throw new InvalidOperationException("ACL source physical identity is not committed.");
            return instance;
        }

        internal bool ContainsCommitted(
            in AnimationPoseSourceId sourceId,
            in PoseNodeId playerNodeId)
        {
            RequireAlive();
            CharacterAclSourceKey key = new CharacterAclSourceKey(
                in sourceId,
                in playerNodeId);
            for (int i = 0; i < m_Instances.Length; i++)
            {
                if (m_Pending[i] == 0 && m_Retired[i] == 0 &&
                    m_Instances[i] != null &&
                    m_Instances[i].Key.Equals(in key))
                    return true;
            }
            return false;
        }

        internal CharacterAclSourceInstance RequireCommitted(
            in AnimationPoseSourceId sourceId,
            in PoseNodeId playerNodeId)
        {
            RequireAlive();
            CharacterAclSourceKey key = new CharacterAclSourceKey(
                in sourceId,
                in playerNodeId);
            for (int i = 0; i < m_Instances.Length; i++)
            {
                if (m_Pending[i] == 0 && m_Retired[i] == 0 &&
                    m_Instances[i] != null &&
                    m_Instances[i].Key.Equals(in key))
                    return m_Instances[i];
            }
            throw new InvalidOperationException("ACL source key is not committed.");
        }

        internal void Commit(AnimationPhysicalSourceIdentity physicalIdentity)
        {
            int index = RequireIndex(physicalIdentity);
            if (m_Instances[index] == null || m_Pending[index] == 0 ||
                m_Identities[index] != physicalIdentity)
                throw new InvalidOperationException("ACL source pending slot cannot be committed.");
            m_Pending[index] = 0;
        }

        internal void RollbackCommit(AnimationPhysicalSourceIdentity physicalIdentity)
        {
            int index = RequireIndex(physicalIdentity);
            if (m_Instances[index] == null || m_Retired[index] != 0 ||
                m_Identities[index] != physicalIdentity)
                throw new InvalidOperationException("ACL source committed slot cannot be rolled back.");
            m_Pending[index] = 1;
        }

        internal CharacterAclSourceInstance DiscardPending(
            AnimationPhysicalSourceIdentity physicalIdentity)
        {
            int index = RequireIndex(physicalIdentity);
            if (m_Instances[index] == null || m_Pending[index] == 0 ||
                m_Retired[index] != 0 || m_Identities[index] != physicalIdentity)
                throw new InvalidOperationException("ACL source pending slot cannot be discarded.");
            CharacterAclSourceInstance instance = m_Instances[index];
            instance.ResetForReuse();
            m_Identities[index] = default;
            m_Pending[index] = 0;
            return instance;
        }

        internal CharacterAclSourceInstance Retire(
            in AnimationPhysicalSourceIdentity physicalIdentity,
            in CharacterAclSourceKey key)
        {
            int index = RequireIndex(physicalIdentity);
            CharacterAclSourceInstance instance = m_Instances[index];
            if (instance == null || m_Pending[index] != 0 || m_Retired[index] != 0 ||
                m_Identities[index] != physicalIdentity ||
                !instance.Key.Equals(in key))
                throw new InvalidOperationException("ACL source retirement identity is stale.");
            m_Identities[index] = default;
            m_Retired[index] = 1;
            return instance;
        }

        internal void Recycle(CharacterAclSourceInstance instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            int index = -1;
            for (int i = 0; i < m_Instances.Length; i++)
            {
                if (!ReferenceEquals(m_Instances[i], instance))
                    continue;
                if (m_Retired[i] == 0)
                    throw new InvalidOperationException("ACL source instance is not retired.");
                index = i;
                break;
            }
            if (index < 0)
                throw new InvalidOperationException("ACL source instance does not belong to this pool.");
            instance.ResetForReuse();
            m_Retired[index] = 0;
        }

        internal void Clear()
        {
            RequireAlive();
            for (int i = 0; i < m_Instances.Length; i++)
            {
                m_Instances[i]?.ResetForReuse();
                m_Identities[i] = default;
                m_Pending[i] = 0;
                m_Retired[i] = 0;
            }
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            Exception failure = null;
            for (int i = 0; i < m_Instances.Length; i++)
            {
                CharacterAclSourceInstance instance = m_Instances[i];
                if (instance == null)
                    continue;
                try
                {
                    instance.Dispose();
                }
                catch (Exception exception)
                {
                    RecordFailure(ref failure, exception);
                    continue;
                }
                m_Instances[i] = null;
                m_Identities[i] = default;
                m_Pending[i] = 0;
                m_Retired[i] = 0;
            }
            try
            {
                m_LeaseTable.Dispose();
            }
            catch (Exception exception)
            {
                RecordFailure(ref failure, exception);
            }
            if (failure != null)
                throw new AggregateException(
                    "ACL source pool disposal failed.",
                    failure);
            m_Disposed = true;
        }

        int RequireIndex(AnimationPhysicalSourceIdentity physicalIdentity)
        {
            RequireAlive();
            if (!physicalIdentity.IsValid ||
                (uint)physicalIdentity.Index.Value >= (uint)m_Instances.Length)
                throw new ArgumentOutOfRangeException(nameof(physicalIdentity));
            return physicalIdentity.Index.Value;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterAclSourcePool));
        }

        static void RecordFailure(ref Exception failure, Exception exception)
        {
            failure = failure == null
                ? exception
                : new AggregateException(failure, exception);
        }
    }
}
