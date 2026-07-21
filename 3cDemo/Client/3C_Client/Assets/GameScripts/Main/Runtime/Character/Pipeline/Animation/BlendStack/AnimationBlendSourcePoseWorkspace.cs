using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.BlendStack
{
    internal sealed class AnimationBlendSourcePoseWorkspace
    {
        readonly CharacterAnimationRigPayload m_Rig;
        readonly int m_BoneCount;
        readonly int m_ParameterCount;
        readonly int m_RootBoneIndex;
        readonly AnimationPlaybackId[] m_PlaybackIds;
        readonly int[] m_ProgramProducerIndices;
        readonly AnimationLocalBonePose[] m_Poses;
        readonly AnimationBlendBoneVelocity[] m_Velocities;
        readonly float[] m_Parameters;
        readonly AnimationFootFeatureSample[] m_LeftFootFeatures;
        readonly AnimationFootFeatureSample[] m_RightFootFeatures;
        readonly bool[] m_HasFootFeatures;
        readonly float[] m_VisualTimeScales;
        readonly AnimationLocalBonePose[] m_ReferencePose;

        int m_Count;
        ulong m_CompletionIdentity;

        public AnimationBlendSourcePoseWorkspace(
            CharacterAnimationRigPayload rig,
            int parameterCount,
            int sourceCapacity)
        {
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            rig.RequireValid();
            if (parameterCount < 0 || sourceCapacity < 2)
                throw new ArgumentOutOfRangeException();
            m_Rig = rig;
            m_BoneCount = rig.Bones.Count;
            m_ParameterCount = parameterCount;
            m_RootBoneIndex = rig.RootBoneIndex;
            m_PlaybackIds = new AnimationPlaybackId[sourceCapacity];
            m_ProgramProducerIndices = new int[sourceCapacity];
            m_Poses = new AnimationLocalBonePose[sourceCapacity * m_BoneCount];
            m_Velocities = new AnimationBlendBoneVelocity[sourceCapacity * m_BoneCount];
            m_Parameters = new float[sourceCapacity * m_ParameterCount];
            m_LeftFootFeatures = new AnimationFootFeatureSample[sourceCapacity];
            m_RightFootFeatures = new AnimationFootFeatureSample[sourceCapacity];
            m_HasFootFeatures = new bool[sourceCapacity];
            m_VisualTimeScales = new float[sourceCapacity];
            m_ReferencePose = new AnimationLocalBonePose[m_BoneCount];
            for (int i = 0; i < m_BoneCount; i++)
            {
                CharacterAnimationRigBonePayload bone = rig.Bones[i];
                m_ReferencePose[i] = new AnimationLocalBonePose(
                    bone.ReferenceLocalPosition,
                    bone.ReferenceLocalRotation,
                    bone.ReferenceLocalScale);
            }
        }

        public int Count => m_Count;
        public int BoneCount => m_BoneCount;
        public int ParameterCount => m_ParameterCount;
        public ulong CompletionIdentity => m_CompletionIdentity;

        public void BeginFrame(ulong completionIdentity)
        {
            if (completionIdentity == 0 || completionIdentity == m_CompletionIdentity)
                throw new ArgumentException("Animation source pose frame identity is invalid.", nameof(completionIdentity));
            m_CompletionIdentity = completionIdentity;
            m_Count = 0;
        }

        public void WriteSource(
            AnimationPlaybackId playbackId,
            int programProducerIndex,
            IReadOnlyList<AnimationLocalBonePose> denseLocalPose,
            IReadOnlyList<AnimationBlendBoneVelocity> denseVelocity,
            IReadOnlyList<float> poseParameters,
            AnimationFootFeatureSample leftFootFeatures,
            AnimationFootFeatureSample rightFootFeatures,
            bool hasFootFeatures,
            float visualTimeScale)
        {
            if (m_CompletionIdentity == 0)
                throw new InvalidOperationException("Animation source pose workspace has not begun a frame.");
            if (!playbackId.IsValid || programProducerIndex < 0 ||
                denseLocalPose == null || denseLocalPose.Count != m_BoneCount ||
                denseVelocity == null || denseVelocity.Count != m_BoneCount ||
                poseParameters == null || poseParameters.Count != m_ParameterCount ||
                !float.IsFinite(visualTimeScale) || visualTimeScale < 0f ||
                hasFootFeatures && (!leftFootFeatures.IsValid || !rightFootFeatures.IsValid))
                throw new ArgumentException("Animation source pose write is invalid.");
            if (m_Count == m_PlaybackIds.Length)
                throw new InvalidOperationException("Animation source pose workspace capacity was exceeded.");
            if (TryFind(playbackId, out _))
                throw new InvalidOperationException($"Animation source pose '{playbackId}' was written twice in one frame.");

            for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
            {
                if (!denseLocalPose[boneIndex].IsValid || !denseVelocity[boneIndex].IsValid)
                    throw new ArgumentException($"Animation source pose Bone #{boneIndex} is invalid.");
            }
            for (int parameterIndex = 0; parameterIndex < m_ParameterCount; parameterIndex++)
            {
                if (!float.IsFinite(poseParameters[parameterIndex]))
                    throw new ArgumentException($"Animation source pose parameter #{parameterIndex} is invalid.");
            }

            int sourceIndex = m_Count++;
            m_PlaybackIds[sourceIndex] = playbackId;
            m_ProgramProducerIndices[sourceIndex] = programProducerIndex;
            m_LeftFootFeatures[sourceIndex] = leftFootFeatures;
            m_RightFootFeatures[sourceIndex] = rightFootFeatures;
            m_HasFootFeatures[sourceIndex] = hasFootFeatures;
            m_VisualTimeScales[sourceIndex] = visualTimeScale;
            int poseOffset = sourceIndex * m_BoneCount;
            for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
            {
                AnimationLocalBonePose pose = denseLocalPose[boneIndex];
                AnimationBlendBoneVelocity velocity = denseVelocity[boneIndex];
                if (m_Rig.RootBonePolicy == CharacterAnimationRootBonePolicy.ExcludeSourceRoot &&
                    boneIndex == m_RootBoneIndex)
                {
                    pose = m_ReferencePose[boneIndex];
                    velocity = default;
                }
                else if (m_Rig.ScalePolicy == CharacterAnimationScalePolicy.PreserveReferenceScale)
                {
                    pose = new AnimationLocalBonePose(pose.Position, pose.Rotation, m_ReferencePose[boneIndex].Scale);
                    velocity = new AnimationBlendBoneVelocity(velocity.Linear, velocity.Angular, Vector3.zero);
                }
                m_Poses[poseOffset + boneIndex] = pose;
                m_Velocities[poseOffset + boneIndex] = velocity;
            }
            int parameterOffset = sourceIndex * m_ParameterCount;
            for (int parameterIndex = 0; parameterIndex < m_ParameterCount; parameterIndex++)
                m_Parameters[parameterOffset + parameterIndex] = poseParameters[parameterIndex];
        }

        public bool TryGet(AnimationPlaybackId playbackId, out AnimationBlendSourcePoseFrame source)
        {
            if (!TryFind(playbackId, out int index))
            {
                source = default;
                return false;
            }
            source = new AnimationBlendSourcePoseFrame(
                m_PlaybackIds[index],
                m_ProgramProducerIndices[index],
                new AnimationReadOnlyBuffer<AnimationLocalBonePose>(m_Poses, index * m_BoneCount, m_BoneCount),
                new AnimationReadOnlyBuffer<AnimationBlendBoneVelocity>(m_Velocities, index * m_BoneCount, m_BoneCount),
                new AnimationReadOnlyBuffer<float>(m_Parameters, index * m_ParameterCount, m_ParameterCount),
                m_LeftFootFeatures[index],
                m_RightFootFeatures[index],
                m_HasFootFeatures[index],
                m_VisualTimeScales[index]);
            return true;
        }

        public AnimationLocalBonePose GetReferencePose(int boneIndex)
        {
            if ((uint)boneIndex >= (uint)m_BoneCount)
                throw new ArgumentOutOfRangeException(nameof(boneIndex));
            return m_ReferencePose[boneIndex];
        }

        bool TryFind(AnimationPlaybackId playbackId, out int index)
        {
            for (int i = 0; i < m_Count; i++)
            {
                if (!m_PlaybackIds[i].Equals(playbackId))
                    continue;
                index = i;
                return true;
            }
            index = -1;
            return false;
        }
    }
}
