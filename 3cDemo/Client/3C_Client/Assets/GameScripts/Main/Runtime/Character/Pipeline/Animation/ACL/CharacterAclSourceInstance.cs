using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Resources;
using Unity.Collections;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ThirdPersonCharacter.Pipeline.Presentation.Animancer
{
    internal readonly struct CharacterAclSourceKey : IEquatable<CharacterAclSourceKey>
    {
        internal CharacterAclSourceKey(
            in AnimationPoseSourceId sourceId,
            in PoseNodeId playerNodeId)
        {
            if (!sourceId.IsValid || !playerNodeId.IsValid)
                throw new ArgumentException("ACL source key is invalid.");
            SourceId = sourceId;
            PlayerNodeId = playerNodeId;
        }

        internal readonly AnimationPoseSourceId SourceId;
        internal readonly PoseNodeId PlayerNodeId;
        internal bool IsValid => SourceId.IsValid && PlayerNodeId.IsValid;
        public bool Equals(CharacterAclSourceKey other) =>
            Equals(in other);

        internal bool Equals(in CharacterAclSourceKey other) =>
            SourceId == other.SourceId && PlayerNodeId == other.PlayerNodeId;
        public override bool Equals(object obj) =>
            obj is CharacterAclSourceKey other && Equals(other);
        public override int GetHashCode() =>
            SourceId.GetHashCode() * 397 ^ PlayerNodeId.GetHashCode();
    }

    internal sealed class CharacterAclSourceInstance : IDisposable
    {
        CharacterAclSourceKey m_Key;
        readonly CharacterAclResourceStore m_Store;
        readonly CharacterAclResourceLeaseTable m_LeaseTable;
        readonly CharacterAnimationRigPayload m_Rig;
        readonly string[] m_PoseBoneReferenceIdentities;
        readonly int m_MaxClipCapacity;
        readonly CharacterAclResourceLease[] m_Leases;
        readonly int[] m_ResourceIndices;
        readonly int[] m_GroupClipIndices;
        readonly CharacterAclDecoder[] m_Decoders;
        readonly CharacterAclScalarTrackBinding[][] m_ScalarBindings;
        readonly int[] m_ScalarBindingCounts;
        readonly NativeArray<CharacterAclNativeTransformSample>[] m_TransformValues;
        readonly NativeArray<float>[] m_ScalarValues;
        NativeArray<int> m_TrackByPoseBone;
        readonly CharacterClipSampleBatch m_SampleBatch;
        readonly CharacterAnimationScalarMixer m_ScalarMixer;
        readonly CharacterAclSourceGraph m_Graph;
        int m_LeaseCount;
        int m_ClipCount;
        bool m_Disposed;

        internal CharacterAclSourceInstance(
            PlayableGraph graph,
            CharacterAnimationRigPayload rig,
            CharacterAclResourceStore store,
            CharacterAclResourceLeaseTable leaseTable,
            NativeArray<TransformStreamHandle> handles,
            NativeArray<AnimationLocalBonePose> referencePose,
            NativeArray<int> physicalParentIndices,
            NativeArray<CharacterVirtualBoneDescriptor> virtualBones,
            string[] poseBoneReferenceIdentities,
            int clipCapacity,
            int parameterCapacity)
        {
            if (!graph.IsValid() || rig == null || store == null || leaseTable == null ||
                !handles.IsCreated || !referencePose.IsCreated ||
                !physicalParentIndices.IsCreated || !virtualBones.IsCreated ||
                poseBoneReferenceIdentities == null ||
                poseBoneReferenceIdentities.Length != rig.PoseBoneCount ||
                clipCapacity <= 0 || parameterCapacity <= 0)
                throw new ArgumentException("ACL source instance input is invalid.");
            m_Store = store;
            m_LeaseTable = leaseTable;
            m_Rig = rig;
            m_PoseBoneReferenceIdentities = poseBoneReferenceIdentities;
            m_MaxClipCapacity = clipCapacity;
            m_Leases = new CharacterAclResourceLease[clipCapacity];
            m_ResourceIndices = new int[clipCapacity];
            m_GroupClipIndices = new int[clipCapacity];
            m_Decoders = new CharacterAclDecoder[clipCapacity];
            m_ScalarBindings = new CharacterAclScalarTrackBinding[clipCapacity][];
            m_ScalarBindingCounts = new int[clipCapacity];
            m_TransformValues = new NativeArray<CharacterAclNativeTransformSample>[clipCapacity];
            m_ScalarValues = new NativeArray<float>[clipCapacity];
            m_TrackByPoseBone = new NativeArray<int>(
                rig.PhysicalBoneCount,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < m_TrackByPoseBone.Length; i++)
                m_TrackByPoseBone[i] = -1;
            m_SampleBatch = new CharacterClipSampleBatch(clipCapacity);
            m_ScalarMixer = new CharacterAnimationScalarMixer(parameterCapacity);
            try
            {
                for (int i = 0; i < clipCapacity; i++)
                {
                    m_Decoders[i] = new CharacterAclDecoder();
                    m_ScalarBindings[i] = new CharacterAclScalarTrackBinding[parameterCapacity];
                    m_TransformValues[i] = new NativeArray<CharacterAclNativeTransformSample>(
                        rig.PhysicalBoneCount,
                        Allocator.Persistent,
                        NativeArrayOptions.UninitializedMemory);
                    m_ScalarValues[i] = new NativeArray<float>(
                        parameterCapacity,
                        Allocator.Persistent,
                        NativeArrayOptions.UninitializedMemory);
                }
                m_Graph = new CharacterAclSourceGraph(
                    graph,
                    rig,
                    handles,
                    referencePose,
                    physicalParentIndices,
                    virtualBones,
                    m_TrackByPoseBone,
                    m_TransformValues,
                    clipCapacity);
            }
            catch (Exception exception)
            {
                Exception cleanupFailure = null;
                for (int i = 0; i < m_Decoders.Length; i++)
                {
                    if (m_Decoders[i] == null)
                        continue;
                    try
                    {
                        m_Decoders[i].Dispose();
                    }
                    catch (Exception cleanupException)
                    {
                        RecordFailure(ref cleanupFailure, cleanupException);
                    }
                }
                DisposeArrays(ref cleanupFailure);
                DisposeArray(ref m_TrackByPoseBone, ref cleanupFailure);
                try
                {
                    m_ScalarMixer.Dispose();
                }
                catch (Exception cleanupException)
                {
                    RecordFailure(ref cleanupFailure, cleanupException);
                }
                if (cleanupFailure != null)
                    throw new AggregateException(
                        "ACL source instance construction cleanup failed.",
                        exception,
                        cleanupFailure);
                throw;
            }
        }

        internal ref readonly CharacterAclSourceKey Key => ref m_Key;
        internal AnimationScriptPlayable Output => m_Graph.Output;
        internal CharacterClipSampleBatch Samples => m_SampleBatch;
        internal int ClipCount => m_ClipCount;

        internal void Configure(
            in CharacterAclSourceKey key,
            in AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding> catalog,
            in AnimationPoseSourceCaptureBinding capture)
        {
            RequireAlive();
            if (!key.IsValid || catalog.Count <= 0 || catalog.Count > m_MaxClipCapacity)
                throw new ArgumentException("ACL source catalog input is invalid.");
            ResetForReuse();
            try
            {
                for (int i = 0; i < catalog.Count; i++)
                {
                    ref readonly AnimationPoseSourceClipBinding binding =
                        ref catalog.ElementAt(i);
                    if (!binding.IsValid || !binding.IsAcl || binding.ClipBindingIndex != i)
                        throw new InvalidOperationException("ACL source catalog contains a non-ACL binding.");
                    int resourceIndex = binding.ResourceCatalogIndex;
                    CharacterAnimationCompiledResourceDescriptor descriptor =
                        m_Store.RequireDescriptorByCatalogIndex(resourceIndex);
                    CharacterAclAnimationResourceManifest manifest =
                        descriptor.RequireManifest(binding.GroupClipIndex);
                    if (manifest == null || manifest.PhysicalBoneCount != m_Rig.PhysicalBoneCount ||
                        manifest.TransformTrackCount != m_Rig.PhysicalBoneCount ||
                        manifest.PoseBoneCount != m_Rig.PoseBoneCount)
                        throw new InvalidOperationException("ACL source resource Rig dimensions do not match the active Rig.");
                    CharacterAclResourceReadinessResult readiness = m_Store.TryAcquire(
                        m_Store.RequireStoreIndex(resourceIndex),
                        m_LeaseTable,
                        out CharacterAclResourceLease lease);
                    if (!readiness.IsReady)
                        throw new InvalidOperationException(readiness.Message);
                    m_ResourceIndices[i] = resourceIndex;
                    m_GroupClipIndices[i] = binding.GroupClipIndex;
                    m_Leases[i] = lease;
                    m_LeaseCount++;
                    m_Decoders[i].Bind(
                        lease.Group,
                        binding.GroupClipIndex,
                        manifest.TransformTrackCount,
                        manifest.ScalarTrackCount);
                    m_ScalarBindingCounts[i] = manifest.ScalarBindings.Count;
                    if (m_ScalarBindingCounts[i] > m_ScalarBindings[i].Length)
                        throw new InvalidOperationException("ACL scalar binding capacity is insufficient.");
                    for (int bindingIndex = 0; bindingIndex < m_ScalarBindingCounts[i]; bindingIndex++)
                    {
                        CharacterAclScalarTrackBinding scalar = manifest.ScalarBindings[bindingIndex];
                        m_ScalarBindings[i][bindingIndex] = scalar;
                        m_ScalarMixer.RegisterParameter(scalar.ParameterIndex);
                    }
                    BindTransformTracks(manifest, i == 0);
                }
                m_ClipCount = catalog.Count;
                m_Key = key;
            }
            catch (Exception exception)
            {
                Exception cleanupFailure = null;
                try
                {
                    ResetForReuse();
                }
                catch (Exception cleanupException)
                {
                    cleanupFailure = cleanupException;
                }
                if (cleanupFailure != null)
                    throw new AggregateException(
                        "ACL source configuration cleanup failed.",
                        exception,
                        cleanupFailure);
                throw;
            }
        }

        internal bool Matches(
            int clipBindingIndex,
            int resourceCatalogIndex,
            int groupClipIndex) =>
            (uint)clipBindingIndex < (uint)m_ClipCount &&
            m_ResourceIndices[clipBindingIndex] == resourceCatalogIndex &&
            m_GroupClipIndices[clipBindingIndex] == groupClipIndex;

        internal void ApplySamples(
            in AnimationReadOnlyBuffer<ClipSamplePlan> plans,
            in AnimationPoseSourceCaptureBinding capture)
        {
            RequireAlive();
            if (m_ClipCount <= 0)
                throw new InvalidOperationException("ACL source instance has no configured catalog.");
            m_SampleBatch.CopyFrom(in plans);
            m_ScalarMixer.Clear();
            for (int i = 0; i < m_SampleBatch.Count; i++)
            {
                ref readonly ClipSamplePlan plan = ref m_SampleBatch.ElementAt(i);
                if (!plan.IsAcl || (uint)plan.ClipBindingIndex >= (uint)m_ClipCount ||
                    !Matches(
                        plan.ClipBindingIndex,
                        plan.ResourceCatalogIndex,
                        plan.GroupClipIndex))
                    throw new InvalidOperationException("ACL source sample plan does not match its catalog.");
                int clipIndex = plan.ClipBindingIndex;
                m_Decoders[clipIndex].SampleTransforms(
                    plan.ClipTime,
                    m_TransformValues[clipIndex]);
                m_Decoders[clipIndex].SampleScalars(
                    plan.ClipTime,
                    m_ScalarValues[clipIndex]);
                m_ScalarMixer.Accumulate(
                    m_ScalarBindings[clipIndex],
                    m_ScalarBindingCounts[clipIndex],
                    m_ScalarValues[clipIndex],
                    m_SampleBatch.GetNormalizedWeight(i));
            }
            m_ScalarMixer.CopyTo(
                capture.PoseParameters,
                capture.PoseParameterAvailability);
            m_Graph.Apply(m_SampleBatch, in capture);
        }

        internal void ResetForReuse()
        {
            RequireAlive();
            m_Graph.ClearInputs();
            int leaseCount = m_LeaseCount;
            for (int i = 0; i < leaseCount; i++)
            {
                m_Decoders[i].Unbind();
                m_Leases[i].Dispose();
                ClearLeaseSlot(i);
            }
            for (int i = leaseCount; i < m_MaxClipCapacity; i++)
            {
                m_Decoders[i].Unbind();
                ClearLeaseSlot(i);
            }
            m_ScalarMixer.ClearTargets();
            m_SampleBatch.Clear();
            for (int i = 0; i < m_TrackByPoseBone.Length; i++)
                m_TrackByPoseBone[i] = -1;
            m_LeaseCount = 0;
            m_ClipCount = 0;
            m_Key = default;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            if (m_LeaseCount != 0 || m_ClipCount != 0 || Key.IsValid)
                ResetForReuse();
            for (int i = 0; i < m_Decoders.Length; i++)
                m_Decoders[i]?.Dispose();
            m_Graph.Dispose();
            Exception failure = null;
            DisposeArrays(ref failure);
            DisposeArray(ref m_TrackByPoseBone, ref failure);
            try
            {
                m_ScalarMixer.Dispose();
            }
            catch (Exception exception)
            {
                RecordFailure(ref failure, exception);
            }
            if (failure != null)
                throw new AggregateException(
                    "ACL source instance disposal failed.",
                    failure);
            m_Disposed = true;
        }

        void BindTransformTracks(
            CharacterAclAnimationResourceManifest manifest,
            bool first)
        {
            for (int i = 0; i < manifest.TransformBindings.Count; i++)
            {
                CharacterAclTransformTrackBinding binding = manifest.TransformBindings[i];
                binding.RequireValid(m_Rig.PhysicalBoneCount);
                CharacterAnimationPhysicalBonePayload bone = m_Rig.PhysicalBones[binding.PoseBoneIndex];
                if (!string.Equals(binding.BoneId, bone.BoneId.Value, StringComparison.Ordinal) ||
                    !string.Equals(
                        binding.ReferenceIdentity,
                        m_PoseBoneReferenceIdentities[binding.PoseBoneIndex],
                        StringComparison.Ordinal))
                    throw new InvalidOperationException("ACL transform binding does not match the active Rig.");
                if (first)
                    m_TrackByPoseBone[binding.PoseBoneIndex] = binding.TrackIndex;
                else if (m_TrackByPoseBone[binding.PoseBoneIndex] != binding.TrackIndex)
                    throw new InvalidOperationException("ACL transform bindings disagree across resources.");
            }
            for (int i = 0; i < m_TrackByPoseBone.Length; i++)
            {
                if (m_TrackByPoseBone[i] < 0)
                    throw new InvalidOperationException("ACL transform bindings do not cover the Rig.");
            }
        }

        void ClearLeaseSlot(int index)
        {
            m_Leases[index] = default;
            m_ResourceIndices[index] = -1;
            m_GroupClipIndices[index] = -1;
            m_ScalarBindingCounts[index] = 0;
            Array.Clear(
                m_ScalarBindings[index],
                0,
                m_ScalarBindings[index].Length);
        }

        void DisposeArrays(ref Exception failure)
        {
            for (int i = 0; i < m_TransformValues.Length; i++)
            {
                DisposeArray(ref m_TransformValues[i], ref failure);
                DisposeArray(ref m_ScalarValues[i], ref failure);
            }
        }

        static void DisposeArray<T>(
            ref NativeArray<T> values,
            ref Exception failure)
            where T : struct
        {
            if (!values.IsCreated)
                return;
            try
            {
                values.Dispose();
                values = default;
            }
            catch (Exception exception)
            {
                RecordFailure(ref failure, exception);
            }
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterAclSourceInstance));
        }

        static void RecordFailure(ref Exception failure, Exception exception)
        {
            failure = failure == null
                ? exception
                : new AggregateException(failure, exception);
        }
    }
}
