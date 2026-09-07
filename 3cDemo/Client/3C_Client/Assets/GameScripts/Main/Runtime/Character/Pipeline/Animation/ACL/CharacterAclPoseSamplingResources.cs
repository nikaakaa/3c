using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Resources;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ThirdPersonCharacter.Pipeline.Presentation.Animancer
{
    internal sealed class CharacterAclPoseSamplingResources : IDisposable
    {
        NativeArray<TransformStreamHandle> m_Handles;
        NativeArray<AnimationLocalBonePose> m_ReferencePose;
        NativeArray<int> m_PhysicalParentIndices;
        NativeArray<CharacterVirtualBoneDescriptor> m_VirtualBones;
        readonly string[] m_PoseBoneReferenceIdentities;
        readonly CharacterAclSourcePool m_Pool;
        bool m_Disposed;

        internal CharacterAclPoseSamplingResources(
            PlayableGraph graph,
            Animator animator,
            CharacterAnimationRigBinding rigBinding,
            CharacterAnimationRigPayload rig,
            CharacterAclResourceStore store,
            int sourceCapacity,
            int clipCapacity,
            int parameterCapacity)
        {
            try
            {
                m_PoseBoneReferenceIdentities = new string[rig.PoseBoneCount];
                for (int i = 0; i < m_PoseBoneReferenceIdentities.Length; i++)
                    m_PoseBoneReferenceIdentities[i] =
                        CharacterAclAnimationIdentity.ComputePoseBoneReferenceIdentity(rig, i);
                m_Handles = new NativeArray<TransformStreamHandle>(
                    rig.PhysicalBoneCount,
                    Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
                m_ReferencePose = new NativeArray<AnimationLocalBonePose>(
                    rig.PoseBoneCount,
                    Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
                m_PhysicalParentIndices = new NativeArray<int>(
                    rig.PhysicalBoneCount,
                    Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
                m_VirtualBones = new NativeArray<CharacterVirtualBoneDescriptor>(
                    rig.VirtualBoneCount,
                    Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
                for (int i = 0; i < rig.PhysicalBoneCount; i++)
                {
                    m_Handles[i] = animator.BindStreamTransform(
                        rigBinding.PhysicalBones[i]);
                    CharacterAnimationPhysicalBonePayload bone = rig.PhysicalBones[i];
                    m_ReferencePose[i] = new AnimationLocalBonePose(
                        bone.ReferenceLocalPosition,
                        bone.ReferenceLocalRotation,
                        bone.ReferenceLocalScale);
                    m_PhysicalParentIndices[i] = bone.ParentPhysicalIndex;
                }
                for (int i = 0; i < rig.VirtualBoneCount; i++)
                {
                    CharacterAnimationVirtualBonePayload bone = rig.VirtualBones[i];
                    m_ReferencePose[bone.PoseBoneIndex] = new AnimationLocalBonePose(
                        bone.ReferenceLocalPosition,
                        bone.ReferenceLocalRotation,
                        Vector3.one);
                    m_VirtualBones[i] = new CharacterVirtualBoneDescriptor(
                        new CharacterPoseBoneRuntimeId(bone.VirtualBoneId),
                        bone.SourcePhysicalBoneIndex,
                        bone.TargetPhysicalBoneIndex,
                        bone.PoseBoneIndex);
                }
                m_Pool = new CharacterAclSourcePool(
                    graph,
                    rig,
                    store,
                    m_Handles,
                    m_ReferencePose,
                    m_PhysicalParentIndices,
                    m_VirtualBones,
                    m_PoseBoneReferenceIdentities,
                    sourceCapacity,
                    clipCapacity,
                    parameterCapacity);
            }
            catch (Exception exception)
            {
                Exception cleanupFailure = null;
                DisposeArray(ref m_VirtualBones, ref cleanupFailure);
                DisposeArray(ref m_PhysicalParentIndices, ref cleanupFailure);
                DisposeArray(ref m_ReferencePose, ref cleanupFailure);
                DisposeArray(ref m_Handles, ref cleanupFailure);
                if (cleanupFailure != null)
                    throw new AggregateException(
                        "ACL pose sampling resources construction cleanup failed.",
                        exception,
                        cleanupFailure);
                throw;
            }
        }

        internal CharacterAclSourcePool Pool => m_Pool;

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Pool.Dispose();
            Exception failure = null;
            DisposeArray(ref m_VirtualBones, ref failure);
            DisposeArray(ref m_PhysicalParentIndices, ref failure);
            DisposeArray(ref m_ReferencePose, ref failure);
            DisposeArray(ref m_Handles, ref failure);
            if (failure != null)
                throw new AggregateException(
                    "ACL pose sampling resources disposal failed.",
                    failure);
            m_Disposed = true;
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

        static void RecordFailure(ref Exception failure, Exception exception)
        {
            failure = failure == null
                ? exception
                : new AggregateException(failure, exception);
        }
    }
}
